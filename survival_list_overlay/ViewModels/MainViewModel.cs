using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using survival_list_overlay.Commands;
using survival_list_overlay.Models;
using survival_list_overlay.Services;
using survival_list_overlay.Settings;

namespace survival_list_overlay.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IAudioCueService audioCueService;
    private readonly OverlayData data;
    private readonly IOverlayDataStore dataStore;
    private readonly MaterialDemandService materialDemandService;
    private readonly IUserNotificationService notificationService;
    private readonly RegistryResolver resolver;
    private readonly OverlaySettings settings;
    private TrackedEntry? lastDeletedEntry;
    private int? lastDeletedEntryIndex;
    private string newTargetQuantity = "1";
    private string searchQuery = string.Empty;
    private TrackedEntryViewModel? selectedEntry;
    private SearchResultViewModel? selectedSearchResult;
    private bool showFavoritesOnly;

    public MainViewModel(
        IUserNotificationService notificationService,
        IAudioCueService audioCueService,
        OverlaySettings? settings = null,
        IOverlayDataStore? dataStore = null)
    {
        this.notificationService = notificationService;
        this.audioCueService = audioCueService;
        this.settings = settings ?? new OverlaySettings();
        this.dataStore = dataStore ?? new JsonOverlayDataStore();

        data = this.dataStore.Load();
        resolver = new RegistryResolver(data.Registry);
        materialDemandService = new MaterialDemandService(resolver);
        EnsureActiveList();

        AddSelectedResultCommand = new RelayCommand(_ => AddSelectedResult(createDuplicate: false), _ => CanAddSelectedResult());
        AddDuplicateSelectedResultCommand = new RelayCommand(_ => AddSelectedResult(createDuplicate: true), _ => CanAddSelectedResult());
        RemoveEntryCommand = new RelayCommand(RemoveEntry, parameter => parameter is TrackedEntryViewModel);
        UndoLastRemovalCommand = new RelayCommand(_ => UndoLastRemoval(), _ => lastDeletedEntry is not null);
        IncrementSelectedEntryCommand = new RelayCommand(_ => SelectedEntry?.IncrementCommand.Execute(null), _ => SelectedEntry is not null);
        DecrementSelectedEntryCommand = new RelayCommand(_ => SelectedEntry?.DecrementCommand.Execute(null), _ => SelectedEntry is not null);
        ToggleSelectedFavoriteCommand = new RelayCommand(_ => SelectedEntry?.ToggleFavoriteCommand.Execute(null), _ => SelectedEntry is not null);
        ToggleSelectedStickyCommand = new RelayCommand(_ => SelectedEntry?.ToggleStickyCommand.Execute(null), _ => SelectedEntry is not null);
        ToggleSelectedExpandedCommand = new RelayCommand(_ => SelectedEntry?.ToggleExpandedCommand.Execute(null), _ => SelectedEntry?.IsRecipe == true);
        IncreaseNewTargetQuantityCommand = new RelayCommand(_ => AdjustNewTargetQuantity(1));
        DecreaseNewTargetQuantityCommand = new RelayCommand(_ => AdjustNewTargetQuantity(-1), _ => TryGetTargetQuantity(out var quantity) && quantity > 1);
        ToggleFavoritesFilterCommand = new RelayCommand(_ => ShowFavoritesOnly = !ShowFavoritesOnly);
        ToggleInteractionModeCommand = new RelayCommand(_ => ToggleInteractionMode());
        SwitchListTypeCommand = new RelayCommand(_ => SwitchListType());
        ClearSearchCommand = new RelayCommand(_ => SearchQuery = string.Empty);

        RefreshSearchResults();
        RefreshEntries();
    }

    public ObservableCollection<TrackedEntryViewModel> Entries { get; } = new();
    public ObservableCollection<SearchResultViewModel> SearchResults { get; } = new();

    public IReadOnlyList<TrackedListSortMode> SortModes { get; } =
        new[] { TrackedListSortMode.Priority, TrackedListSortMode.Alphabetical };

    public string Title => settings.Title;

    public string HeaderText => $"{data.Registry.GameName} Tracking - {ActiveList.Name}";

    public string EntryCountText => $"{ActiveList.Entries.Count} / {OverlayLimits.MaxEntriesPerList}";

    public string ListModeText => ActiveList.Type == TrackedListType.Counting ? "Counting" : "Standard";

    public bool IsCountingList => ActiveList.Type == TrackedListType.Counting;

    public bool IsEditMode => data.Profile.Overlay.InteractionMode == OverlayInteractionMode.Edit;

    public string InteractionModeText => IsEditMode ? "Edit" : "Locked";

    public string InteractionModeGlyph => IsEditMode ? "Unlock" : "Lock";

    public OverlayUserSettings OverlaySettings => data.Profile.Overlay;

    public string EmptyStateText => ShowFavoritesOnly
        ? "No favorite entries tracked."
        : "No items tracked. Search to add an item or recipe.";

    public bool HasSearchQuery => !string.IsNullOrWhiteSpace(SearchQuery);

    public TrackedListSortMode SelectedSortMode
    {
        get => ActiveList.SortMode;
        set
        {
            if (ActiveList.SortMode == value)
            {
                return;
            }

            ActiveList.SortMode = value;
            Save();
            RefreshEntries();
            OnPropertyChanged(nameof(SelectedSortMode));
        }
    }

    public string SearchQuery
    {
        get => searchQuery;
        set
        {
            if (searchQuery == value)
            {
                return;
            }

            searchQuery = value;
            OnPropertyChanged(nameof(SearchQuery));
            OnPropertyChanged(nameof(HasSearchQuery));
            RefreshSearchResults();
            RelayCommand.RefreshCanExecute();
        }
    }

    public string NewTargetQuantity
    {
        get => newTargetQuantity;
        set
        {
            if (newTargetQuantity == value)
            {
                return;
            }

            newTargetQuantity = value;
            OnPropertyChanged(nameof(NewTargetQuantity));
            RelayCommand.RefreshCanExecute();
        }
    }

    public SearchResultViewModel? SelectedSearchResult
    {
        get => selectedSearchResult;
        set
        {
            if (selectedSearchResult == value)
            {
                return;
            }

            selectedSearchResult = value;
            OnPropertyChanged(nameof(SelectedSearchResult));
            RelayCommand.RefreshCanExecute();
        }
    }

    public TrackedEntryViewModel? SelectedEntry
    {
        get => selectedEntry;
        set
        {
            if (selectedEntry == value)
            {
                return;
            }

            selectedEntry = value;
            OnPropertyChanged(nameof(SelectedEntry));
            RelayCommand.RefreshCanExecute();
        }
    }

    public bool ShowFavoritesOnly
    {
        get => showFavoritesOnly;
        set
        {
            if (showFavoritesOnly == value)
            {
                return;
            }

            showFavoritesOnly = value;
            OnPropertyChanged(nameof(ShowFavoritesOnly));
            OnPropertyChanged(nameof(EmptyStateText));
            RefreshEntries();
        }
    }

    public ICommand AddSelectedResultCommand { get; }
    public ICommand AddDuplicateSelectedResultCommand { get; }
    public ICommand RemoveEntryCommand { get; }
    public ICommand UndoLastRemovalCommand { get; }
    public ICommand IncrementSelectedEntryCommand { get; }
    public ICommand DecrementSelectedEntryCommand { get; }
    public ICommand ToggleSelectedFavoriteCommand { get; }
    public ICommand ToggleSelectedStickyCommand { get; }
    public ICommand ToggleSelectedExpandedCommand { get; }
    public ICommand IncreaseNewTargetQuantityCommand { get; }
    public ICommand DecreaseNewTargetQuantityCommand { get; }
    public ICommand ToggleFavoritesFilterCommand { get; }
    public ICommand ToggleInteractionModeCommand { get; }
    public ICommand SwitchListTypeCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SelectNextSearchResult()
    {
        MoveSearchSelection(1);
    }

    public void SelectPreviousSearchResult()
    {
        MoveSearchSelection(-1);
    }

    public void AddSelectedSearchResultFromKeyboard()
    {
        if (AddSelectedResultCommand.CanExecute(null))
        {
            AddSelectedResultCommand.Execute(null);
        }
    }

    public void RemoveSelectedEntryFromKeyboard()
    {
        if (SelectedEntry is not null && RemoveEntryCommand.CanExecute(SelectedEntry))
        {
            RemoveEntryCommand.Execute(SelectedEntry);
        }
    }

    public void SelectNextEntry()
    {
        MoveEntrySelection(1);
    }

    public void SelectPreviousEntry()
    {
        MoveEntrySelection(-1);
    }

    public void SaveOverlayWindowState(double left, double top, double width, double height)
    {
        if (double.IsNaN(left) || double.IsNaN(top) || double.IsNaN(width) || double.IsNaN(height))
        {
            return;
        }

        data.Profile.Overlay.Left = left;
        data.Profile.Overlay.Top = top;
        data.Profile.Overlay.Width = Math.Clamp(width, 360, 1400);
        data.Profile.Overlay.Height = Math.Clamp(height, 240, 1000);
        Save();
    }

    private TrackedList ActiveList => data.Profile.Lists.First(list => list.Id == data.Profile.ActiveListId);

    private void AddSelectedResult(bool createDuplicate)
    {
        if (!TryGetTargetQuantity(out var targetQuantity))
        {
            notificationService.ShowError("Enter a quantity greater than zero.", "Invalid quantity");
            return;
        }

        var result = SelectedSearchResult ?? CreateCustomItemResult();
        if (result is null)
        {
            return;
        }

        var existingEntry = ActiveList.Entries.FirstOrDefault(entry =>
            entry.EntryType == result.EntryType
            && string.Equals(entry.RefId, result.RefId, StringComparison.OrdinalIgnoreCase));

        if (existingEntry is not null && !createDuplicate)
        {
            if (ActiveList.Type == TrackedListType.Counting)
            {
                existingEntry.CurrentQuantity += targetQuantity;
            }
            else
            {
                existingEntry.TargetQuantity += targetQuantity;
            }

            SearchQuery = string.Empty;
            NewTargetQuantity = "1";
            Save();
            RefreshSearchResults();
            RefreshEntries(existingEntry.Id);
            return;
        }

        if (ActiveList.Entries.Count >= OverlayLimits.MaxEntriesPerList)
        {
            notificationService.ShowError(
                $"This list already has {OverlayLimits.MaxEntriesPerList} entries. Remove an entry before adding another.",
                "List full");
            return;
        }

        var entry = new TrackedEntry
        {
            EntryType = result.EntryType,
            RefId = result.RefId,
            TargetQuantity = ActiveList.Type == TrackedListType.Counting ? 1 : targetQuantity,
            CurrentQuantity = ActiveList.Type == TrackedListType.Counting ? targetQuantity : 0,
            Expanded = false
        };

        ActiveList.Entries.Add(entry);
        SearchQuery = string.Empty;
        NewTargetQuantity = "1";
        Save();
        RefreshSearchResults();
        RefreshEntries(entry.Id);
    }

    private bool CanAddSelectedResult()
    {
        return TryGetTargetQuantity(out _)
            && (SelectedSearchResult is not null || !string.IsNullOrWhiteSpace(SearchQuery));
    }

    private SearchResultViewModel? CreateCustomItemResult()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return null;
        }

        var item = resolver.AddCustomItem(SearchQuery.Trim());
        return new SearchResultViewModel(new RegistrySearchResult(RegistryEntryType.Item, item.Id, item.Name, "custom"));
    }

    private void RemoveEntry(object? parameter)
    {
        if (parameter is not TrackedEntryViewModel viewModel)
        {
            return;
        }

        var entryIndex = ActiveList.Entries.FindIndex(entry => entry.Id == viewModel.Id);
        if (entryIndex < 0)
        {
            return;
        }

        var entry = ActiveList.Entries[entryIndex];
        if (entry.Sticky
            && !notificationService.Confirm($"{viewModel.DisplayName} is marked sticky. Remove it anyway?", "Remove sticky entry"))
        {
            return;
        }

        ActiveList.Entries.RemoveAt(entryIndex);
        lastDeletedEntry = entry;
        lastDeletedEntryIndex = entryIndex;
        Save();
        RefreshEntries();
    }

    private void UndoLastRemoval()
    {
        if (lastDeletedEntry is null || lastDeletedEntryIndex is null)
        {
            return;
        }

        if (ActiveList.Entries.Count >= OverlayLimits.MaxEntriesPerList)
        {
            notificationService.ShowError(
                $"This list already has {OverlayLimits.MaxEntriesPerList} entries. Remove an entry before restoring.",
                "List full");
            return;
        }

        var insertIndex = Math.Min(lastDeletedEntryIndex.Value, ActiveList.Entries.Count);
        ActiveList.Entries.Insert(insertIndex, lastDeletedEntry);
        var restoredEntryId = lastDeletedEntry.Id;
        lastDeletedEntry = null;
        lastDeletedEntryIndex = null;
        Save();
        RefreshEntries(restoredEntryId);
    }

    private void RefreshEntries(string? selectedEntryId = null)
    {
        foreach (var entry in Entries)
        {
            entry.Dispose();
        }

        Entries.Clear();

        var entries = ActiveList.Entries.AsEnumerable();
        if (ShowFavoritesOnly)
        {
            entries = entries.Where(entry => entry.Favorite);
        }

        foreach (var entry in TrackedEntrySorter.Sort(entries, ActiveList.SortMode, resolver))
        {
            Entries.Add(new TrackedEntryViewModel(
                entry,
                ActiveList.Type,
                resolver,
                GetCollectedQuantity,
                GetMaterialDemand,
                OnEntryChanged));
        }

        SelectedEntry = selectedEntryId is null
            ? Entries.FirstOrDefault()
            : Entries.FirstOrDefault(entry => entry.Id == selectedEntryId) ?? Entries.FirstOrDefault();

        OnPropertyChanged(nameof(EntryCountText));
        OnPropertyChanged(nameof(EmptyStateText));
        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(ListModeText));
        OnPropertyChanged(nameof(IsCountingList));
        RelayCommand.RefreshCanExecute();
    }

    private void RefreshSearchResults()
    {
        SearchResults.Clear();

        foreach (var result in resolver.Search(SearchQuery).Select(result => new SearchResultViewModel(result)))
        {
            SearchResults.Add(result);
        }

        SelectedSearchResult = SearchResults.FirstOrDefault();
    }

    private void OnEntryChanged(string selectedEntryId, bool playCompletionCue)
    {
        if (playCompletionCue)
        {
            audioCueService.PlayItemCompleted();
        }

        Save();
        RefreshEntries(selectedEntryId);
    }

    private int GetCollectedQuantity(string itemId)
    {
        return ActiveList.Entries
            .Where(entry => entry.EntryType == RegistryEntryType.Item && entry.RefId == itemId)
            .Sum(entry => entry.CurrentQuantity);
    }

    private MaterialDemand GetMaterialDemand(string itemId)
    {
        return materialDemandService.GetDemand(itemId, ActiveList);
    }

    private void MoveSearchSelection(int delta)
    {
        if (SearchResults.Count == 0)
        {
            return;
        }

        var selectedIndex = SelectedSearchResult is null
            ? -1
            : SearchResults.IndexOf(SelectedSearchResult);

        var nextIndex = Math.Clamp(selectedIndex + delta, 0, SearchResults.Count - 1);
        SelectedSearchResult = SearchResults[nextIndex];
    }

    private void MoveEntrySelection(int delta)
    {
        if (Entries.Count == 0)
        {
            return;
        }

        var selectedIndex = SelectedEntry is null ? -1 : Entries.IndexOf(SelectedEntry);
        var nextIndex = Math.Clamp(selectedIndex + delta, 0, Entries.Count - 1);
        SelectedEntry = Entries[nextIndex];
    }

    private void AdjustNewTargetQuantity(int delta)
    {
        var currentQuantity = TryGetTargetQuantity(out var quantity) ? quantity : 1;
        NewTargetQuantity = Math.Max(1, currentQuantity + delta).ToString();
    }

    private void ToggleInteractionMode()
    {
        data.Profile.Overlay.InteractionMode = IsEditMode
            ? OverlayInteractionMode.Locked
            : OverlayInteractionMode.Edit;
        Save();
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(InteractionModeText));
        OnPropertyChanged(nameof(InteractionModeGlyph));
    }

    private void SwitchListType()
    {
        var targetType = ActiveList.Type == TrackedListType.Counting
            ? TrackedListType.Standard
            : TrackedListType.Counting;

        var targetList = data.Profile.Lists.FirstOrDefault(list => list.Type == targetType);
        if (targetList is null)
        {
            targetList = targetType == TrackedListType.Counting
                ? DefaultUserProfileFactory.CreateCountingList()
                : DefaultUserProfileFactory.CreateDefaultList();
            targetList.Id = CreateUniqueListId(targetList.Id);
            data.Profile.Lists.Add(targetList);
        }

        data.Profile.ActiveListId = targetList.Id;
        Save();
        RefreshEntries();
        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(SelectedSortMode));
    }

    private bool TryGetTargetQuantity(out int targetQuantity)
    {
        return int.TryParse(NewTargetQuantity, out targetQuantity) && targetQuantity > 0;
    }

    private void EnsureActiveList()
    {
        data.Profile.Overlay ??= new OverlayUserSettings();
        data.Profile.Overlay.Theme ??= new OverlayThemeSettings();
        data.Profile.Overlay.Keybinds ??= new OverlayKeybindSettings();

        if (data.Profile.Lists.Count == 0)
        {
            data.Profile.Lists.Add(DefaultUserProfileFactory.CreateDefaultList());
        }

        if (data.Profile.Lists.Count > OverlayLimits.MaxListsPerGame)
        {
            data.Profile.Lists = data.Profile.Lists.Take(OverlayLimits.MaxListsPerGame).ToList();
        }

        foreach (var list in data.Profile.Lists)
        {
            if (list.Entries.Count > OverlayLimits.MaxEntriesPerList)
            {
                list.Entries = list.Entries.Take(OverlayLimits.MaxEntriesPerList).ToList();
            }
        }

        if (data.Profile.Lists.All(list => list.Id != data.Profile.ActiveListId))
        {
            data.Profile.ActiveListId = data.Profile.Lists[0].Id;
        }
    }

    private string CreateUniqueListId(string baseId)
    {
        var id = baseId;
        var suffix = 2;
        while (data.Profile.Lists.Any(list => string.Equals(list.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            id = $"{baseId}_{suffix}";
            suffix++;
        }

        return id;
    }

    private void Save()
    {
        dataStore.Save(data);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
