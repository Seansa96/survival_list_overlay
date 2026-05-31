namespace survival_list_overlay.ViewModels;

public sealed class IngredientRequirementViewModel
{
    public IngredientRequirementViewModel(string name, int currentQuantity, int requiredQuantity)
    {
        Name = name;
        CurrentQuantity = currentQuantity;
        RequiredQuantity = requiredQuantity;
    }

    public string Name { get; }
    public int CurrentQuantity { get; }
    public int RequiredQuantity { get; }
    public int RemainingQuantity => Math.Max(0, RequiredQuantity - CurrentQuantity);
    public string ProgressText => $"{CurrentQuantity} / {RequiredQuantity}";
}
