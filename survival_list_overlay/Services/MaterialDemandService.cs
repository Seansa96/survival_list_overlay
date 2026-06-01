using survival_list_overlay.Models;

namespace survival_list_overlay.Services;

public sealed class MaterialDemandService
{
    private readonly RegistryResolver resolver;

    public MaterialDemandService(RegistryResolver resolver)
    {
        this.resolver = resolver;
    }

    public MaterialDemand GetDemand(string itemId, TrackedList list)
    {
        var directTarget = list.Type == TrackedListType.Counting
            ? 0
            : list.Entries
                .Where(entry => entry.EntryType == RegistryEntryType.Item && IsSameRef(entry.RefId, itemId))
                .Sum(entry => entry.TargetQuantity);

        var recipeDemand = list.Type == TrackedListType.Counting
            ? 0
            : list.Entries
                .Where(entry => entry.EntryType == RegistryEntryType.Recipe)
                .SelectMany(entry => GetRecipeIngredients(entry)
                    .Where(ingredient => IsSameRef(ingredient.ItemId, itemId))
                    .Select(ingredient => ingredient.Quantity * entry.TargetQuantity))
                .Sum();

        var collected = list.Entries
            .Where(entry => entry.EntryType == RegistryEntryType.Item && IsSameRef(entry.RefId, itemId))
            .Sum(entry => entry.CurrentQuantity);

        return new MaterialDemand(directTarget, recipeDemand, collected);
    }

    private IEnumerable<RecipeIngredient> GetRecipeIngredients(TrackedEntry entry)
    {
        return resolver.FindRecipe(entry.RefId)?.Ingredients ?? Enumerable.Empty<RecipeIngredient>();
    }

    private static bool IsSameRef(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record MaterialDemand(int DirectTarget, int RecipeDemand, int Collected)
{
    public int TotalRequired => DirectTarget + RecipeDemand;
    public int Remaining => Math.Max(0, TotalRequired - Collected);
}
