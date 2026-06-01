using System.ComponentModel;
using System.Windows.Input;
using survival_list_overlay.Commands;
using survival_list_overlay.Models;
using survival_list_overlay.Services;

namespace survival_list_overlay.ViewModels;

public sealed class TrackedEntryViewModel : INotifyPropertyChanged
{
    private readonly Func<string, int> collectedQuantityProvider;
    private readonly Action<string, bool> entryChanged;
    private readonly Func<string, MaterialDemand> materialDemandProvider;
    private readonly RegistryResolver resolver;
    private readonly TrackedListType listType;

    public TrackedEntryViewModel(
        TrackedEntry entry,
        TrackedListType listType,
        RegistryResolver resolver,
        Func<string, int> collectedQuantityProvider,
        Func<string, MaterialDemand> materialDemandProvider,
        Action<string, bool> entryChanged)
    {
        Entry = entry;
        this.listType = listType;
        this.resolver = resolver;
        this.collectedQuantityProvider = collectedQuantityProvider;
        this.materialDemandProvider = materialDemandProvider;
        this.entryChanged = entryChanged;

        Entry.PropertyChanged += OnEntryPropertyChanged;

        IncrementCommand = new RelayCommand(_ => ChangeCurrentQuantity(1));
        DecrementCommand = new RelayCommand(_ => ChangeCurrentQuantity(-1), _ => CurrentQuantity > 0);
        IncreasePriorityCommand = new RelayCommand(_ => Priority++);
        DecreasePriorityCommand = new RelayCommand(_ => Priority--);
        ToggleFavoriteCommand = new RelayCommand(_ => IsFavorite = !IsFavorite);
        ToggleStickyCommand = new RelayCommand(_ => IsSticky = !IsSticky);
        ToggleExpandedCommand = new RelayCommand(_ => IsExpanded = !IsExpanded, _ => IsRecipe);
    }

    public TrackedEntry Entry { get; }

    public string Id => Entry.Id;

    public string DisplayName => resolver.GetDisplayName(Entry);

    public string EntryTypeLabel => Entry.EntryType == RegistryEntryType.Recipe ? "Recipe" : "Item";

    public bool IsRecipe => Entry.EntryType == RegistryEntryType.Recipe;

    public bool IsCountingList => listType == TrackedListType.Counting;

    public bool IsStandardItem => !IsRecipe && !IsCountingList;

    public bool IsExpanded
    {
        get => Entry.Expanded;
        set
        {
            if (Entry.Expanded == value)
            {
                return;
            }

            Entry.Expanded = value;
            NotifyChanged();
        }
    }

    public int TargetQuantity
    {
        get => Entry.TargetQuantity;
        set
        {
            if (Entry.TargetQuantity == value)
            {
                return;
            }

            Entry.TargetQuantity = value;
            NotifyChanged();
        }
    }

    public int CurrentQuantity
    {
        get => Entry.CurrentQuantity;
        set
        {
            if (Entry.CurrentQuantity == value)
            {
                return;
            }

            Entry.CurrentQuantity = value;
            NotifyChanged();
        }
    }

    public int RemainingQuantity => Entry.RemainingQuantity;

    public int Priority
    {
        get => Entry.Priority;
        set
        {
            if (Entry.Priority == value)
            {
                return;
            }

            Entry.Priority = value;
            NotifyChanged();
        }
    }

    public bool IsFavorite
    {
        get => Entry.Favorite;
        set
        {
            if (Entry.Favorite == value)
            {
                return;
            }

            Entry.Favorite = value;
            NotifyChanged();
        }
    }

    public bool IsSticky
    {
        get => Entry.Sticky;
        set
        {
            if (Entry.Sticky == value)
            {
                return;
            }

            Entry.Sticky = value;
            NotifyChanged();
        }
    }

    public MaterialDemand Demand => IsRecipe
        ? new MaterialDemand(0, 0, 0)
        : materialDemandProvider(Entry.RefId);

    public string ProgressText
    {
        get
        {
            if (IsRecipe)
            {
                return $"{CurrentQuantity} / {TargetQuantity} crafts";
            }

            return IsCountingList
                ? $"Count: {CurrentQuantity}"
                : $"{Demand.Collected} / {Demand.TotalRequired}";
        }
    }

    public string RemainingText
    {
        get
        {
            if (IsRecipe)
            {
                return $"{RemainingQuantity} crafts remaining";
            }

            return IsCountingList
                ? "Collected"
                : $"Need: {Demand.Remaining}";
        }
    }

    public string DemandText
    {
        get
        {
            if (!IsStandardItem)
            {
                return string.Empty;
            }

            return Demand.RecipeDemand > 0
                ? $"Direct {Demand.DirectTarget} + Recipes {Demand.RecipeDemand}"
                : $"Direct target {Demand.DirectTarget}";
        }
    }

    public string FavoriteText => IsFavorite ? "★" : "☆";

    public string StickyText => IsSticky ? "●" : "○";

    public string ExpandText => IsExpanded ? "⌄" : "›";

    public IReadOnlyList<IngredientRequirementViewModel> IngredientRows
    {
        get
        {
            if (!IsRecipe)
            {
                return Array.Empty<IngredientRequirementViewModel>();
            }

            var recipe = resolver.FindRecipe(Entry.RefId);
            if (recipe is null)
            {
                return Array.Empty<IngredientRequirementViewModel>();
            }

            return recipe.Ingredients
                .Select(ingredient => new IngredientRequirementViewModel(
                    resolver.GetItemName(ingredient.ItemId),
                    collectedQuantityProvider(ingredient.ItemId),
                    ingredient.Quantity * TargetQuantity))
                .ToList();
        }
    }

    public string IngredientSummary
    {
        get
        {
            if (!IsRecipe)
            {
                return string.Empty;
            }

            var ingredients = IngredientRows;
            return ingredients.Count == 0
                ? "No ingredients listed"
                : $"Need: {string.Join(", ", ingredients.Select(ingredient => $"{ingredient.RemainingQuantity} {ingredient.Name}"))}";
        }
    }

    public ICommand IncrementCommand { get; }
    public ICommand DecrementCommand { get; }
    public ICommand IncreasePriorityCommand { get; }
    public ICommand DecreasePriorityCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand ToggleStickyCommand { get; }
    public ICommand ToggleExpandedCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        Entry.PropertyChanged -= OnEntryPropertyChanged;
    }

    public void RefreshComputedProperties()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(Demand));
        OnPropertyChanged(nameof(DemandText));
        OnPropertyChanged(nameof(FavoriteText));
        OnPropertyChanged(nameof(StickyText));
        OnPropertyChanged(nameof(ExpandText));
        OnPropertyChanged(nameof(IngredientRows));
        OnPropertyChanged(nameof(IngredientSummary));
    }

    private void ChangeCurrentQuantity(int delta)
    {
        var wasComplete = CurrentQuantity >= TargetQuantity;
        var newQuantity = Math.Max(0, CurrentQuantity + delta);
        if (Entry.CurrentQuantity == newQuantity)
        {
            return;
        }

        Entry.CurrentQuantity = newQuantity;
        var isComplete = Entry.CurrentQuantity >= TargetQuantity;
        entryChanged(Entry.Id, !wasComplete && isComplete);
        RelayCommand.RefreshCanExecute();
    }

    private void NotifyChanged(bool playCompletionCue = false)
    {
        entryChanged(Entry.Id, playCompletionCue);
        RelayCommand.RefreshCanExecute();
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.PropertyName))
        {
            OnPropertyChanged(e.PropertyName);
        }

        RefreshComputedProperties();
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
