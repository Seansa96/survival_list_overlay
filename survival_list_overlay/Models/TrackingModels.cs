using System.ComponentModel;

namespace survival_list_overlay.Models;

public static class OverlayLimits
{
    public const int MaxListsPerGame = 10;
    public const int MaxEntriesPerList = 30;
}

public enum RegistryEntryType
{
    Item,
    Recipe
}

public enum TrackedListType
{
    Standard,
    Counting
}

public enum TrackedListSortMode
{
    Priority,
    Alphabetical
}

public sealed class GameRegistry
{
    public int SchemaVersion { get; set; } = 1;
    public string GameId { get; set; } = "manual";
    public string GameName { get; set; } = "Manual";
    public List<RegistryItem> Items { get; set; } = new();
    public List<RegistryRecipe> Recipes { get; set; } = new();
}

public sealed class RegistryItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public List<string> Tags { get; set; } = new();
}

public sealed class RegistryRecipe
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? OutputItemId { get; set; }
    public List<RecipeIngredient> Ingredients { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public sealed class RecipeIngredient
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class UserProfile
{
    public int SchemaVersion { get; set; } = 1;
    public string ActiveGameId { get; set; } = "manual";
    public string ActiveListId { get; set; } = "main";
    public List<TrackedList> Lists { get; set; } = new();
}

public sealed class TrackedList
{
    public string Id { get; set; } = "main";
    public string Name { get; set; } = "Main";
    public TrackedListType Type { get; set; } = TrackedListType.Standard;
    public TrackedListSortMode SortMode { get; set; } = TrackedListSortMode.Priority;
    public List<TrackedEntry> Entries { get; set; } = new();
}

public sealed class TrackedEntry : INotifyPropertyChanged
{
    private int currentQuantity;
    private bool expanded;
    private bool favorite;
    private int priority;
    private string refId = string.Empty;
    private bool sticky;
    private int targetQuantity = 1;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public RegistryEntryType EntryType { get; set; }

    public string RefId
    {
        get => refId;
        set
        {
            if (refId == value)
            {
                return;
            }

            refId = value;
            OnPropertyChanged(nameof(RefId));
        }
    }

    public int TargetQuantity
    {
        get => targetQuantity;
        set
        {
            var normalizedValue = Math.Max(1, value);
            if (targetQuantity == normalizedValue)
            {
                return;
            }

            targetQuantity = normalizedValue;
            OnPropertyChanged(nameof(TargetQuantity));
            OnPropertyChanged(nameof(RemainingQuantity));
        }
    }

    public int CurrentQuantity
    {
        get => currentQuantity;
        set
        {
            var normalizedValue = Math.Max(0, value);
            if (currentQuantity == normalizedValue)
            {
                return;
            }

            currentQuantity = normalizedValue;
            OnPropertyChanged(nameof(CurrentQuantity));
            OnPropertyChanged(nameof(RemainingQuantity));
        }
    }

    public int RemainingQuantity => Math.Max(0, TargetQuantity - CurrentQuantity);

    public int Priority
    {
        get => priority;
        set
        {
            if (priority == value)
            {
                return;
            }

            priority = value;
            OnPropertyChanged(nameof(Priority));
        }
    }

    public bool Favorite
    {
        get => favorite;
        set
        {
            if (favorite == value)
            {
                return;
            }

            favorite = value;
            OnPropertyChanged(nameof(Favorite));
        }
    }

    public bool Sticky
    {
        get => sticky;
        set
        {
            if (sticky == value)
            {
                return;
            }

            sticky = value;
            OnPropertyChanged(nameof(Sticky));
        }
    }

    public bool Expanded
    {
        get => expanded;
        set
        {
            if (expanded == value)
            {
                return;
            }

            expanded = value;
            OnPropertyChanged(nameof(Expanded));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
