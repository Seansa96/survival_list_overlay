using survival_list_overlay.Models;

namespace survival_list_overlay.Services;

public sealed class RegistryResolver
{
    private readonly GameRegistry registry;

    public RegistryResolver(GameRegistry registry)
    {
        this.registry = registry;
    }

    public RegistryItem? FindItem(string itemId)
    {
        return registry.Items.FirstOrDefault(item => string.Equals(item.Id, itemId, StringComparison.OrdinalIgnoreCase));
    }

    public RegistryRecipe? FindRecipe(string recipeId)
    {
        return registry.Recipes.FirstOrDefault(recipe => string.Equals(recipe.Id, recipeId, StringComparison.OrdinalIgnoreCase));
    }

    public string GetDisplayName(TrackedEntry entry)
    {
        return entry.EntryType == RegistryEntryType.Recipe
            ? FindRecipe(entry.RefId)?.Name ?? entry.RefId
            : FindItem(entry.RefId)?.Name ?? entry.RefId;
    }

    public string GetItemName(string itemId)
    {
        return FindItem(itemId)?.Name ?? itemId;
    }

    public IEnumerable<RegistrySearchResult> Search(string query)
    {
        var normalizedQuery = query.Trim();
        var candidates = registry.Items
            .Select(item => new RegistrySearchResult(RegistryEntryType.Item, item.Id, item.Name, CreateItemSummary(item)))
            .Concat(registry.Recipes.Select(recipe => new RegistrySearchResult(
                RegistryEntryType.Recipe,
                recipe.Id,
                recipe.Name,
                string.Join(", ", recipe.Tags))));

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return candidates.OrderBy(candidate => candidate.DisplayName).Take(8);
        }

        return candidates
            .Where(candidate => Matches(candidate, normalizedQuery))
            .OrderBy(candidate => candidate.DisplayName.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(candidate => candidate.DisplayName)
            .Take(8);
    }

    public RegistryItem AddCustomItem(string name)
    {
        var baseId = CreateId(name);
        var id = baseId;
        var suffix = 2;

        while (FindItem(id) is not null || FindRecipe(id) is not null)
        {
            id = $"{baseId}_{suffix}";
            suffix++;
        }

        var item = new RegistryItem
        {
            Id = id,
            Name = name.Trim(),
            Category = "Custom",
            Tags = { "custom" }
        };
        registry.Items.Add(item);
        return item;
    }

    private static bool Matches(RegistrySearchResult candidate, string query)
    {
        return candidate.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || candidate.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
            || candidate.Summary.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateItemSummary(RegistryItem item)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.Category))
        {
            parts.Add(item.Category);
        }

        parts.AddRange(item.Tags);
        parts.AddRange(item.Aliases);
        return string.Join(", ", parts.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string CreateId(string name)
    {
        var id = new string(name.Trim().ToLowerInvariant().Select(character =>
            char.IsLetterOrDigit(character) ? character : '_').ToArray());

        while (id.Contains("__", StringComparison.Ordinal))
        {
            id = id.Replace("__", "_", StringComparison.Ordinal);
        }

        return id.Trim('_') is { Length: > 0 } trimmedId ? trimmedId : $"item_{Guid.NewGuid():N}";
    }
}

public sealed record RegistrySearchResult(
    RegistryEntryType EntryType,
    string Id,
    string DisplayName,
    string Summary);
