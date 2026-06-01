using survival_list_overlay.Models;
using survival_list_overlay.Services;
using survival_list_overlay.Settings;
using survival_list_overlay.ViewModels;

var tests = new (string Name, Action Test)[]
{
    ("entry limit is 30", EntryLimitIsThirty),
    ("sticky removal requires confirmation", StickyRemovalRequiresConfirmation),
    ("sorting supports priority and alphabetical", SortingSupportsPriorityAndAlphabetical),
    ("json persistence round trips registry and profile separately", JsonPersistenceRoundTrips),
    ("recipes expose expandable ingredient requirements", RecipesExposeIngredients),
    ("duplicate adds update existing entry by default", DuplicateAddsUpdateExistingEntry),
    ("explicit duplicate adds create a second entry", ExplicitDuplicateAddsCreateSecondEntry),
    ("counting mode tracks collected quantity", CountingModeTracksCollectedQuantity),
    ("recipe demand aggregates into item rows", RecipeDemandAggregatesIntoItemRows),
    ("overlay settings persist", OverlaySettingsPersist)
};

var failures = 0;

foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL {name}");
        Console.WriteLine(ex.Message);
    }
}

if (failures > 0)
{
    Environment.Exit(1);
}

static void EntryLimitIsThirty()
{
    var (viewModel, notifications, _) = CreateViewModel();

    for (var index = 0; index < OverlayLimits.MaxEntriesPerList; index++)
    {
        AddSearchEntry(viewModel, $"custom item {index}", 1);
    }

    Assert.Equal(OverlayLimits.MaxEntriesPerList, viewModel.Entries.Count, "Expected list to fill to 30 entries.");

    AddSearchEntry(viewModel, "overflow item", 1);

    Assert.Equal(OverlayLimits.MaxEntriesPerList, viewModel.Entries.Count, "Expected overflow add to be rejected.");
    Assert.True(notifications.ErrorCount > 0, "Expected full-list add to show an error.");
}

static void StickyRemovalRequiresConfirmation()
{
    var (viewModel, notifications, _) = CreateViewModel();
    AddSearchEntry(viewModel, "wood", 10);

    var entry = viewModel.Entries.Single();
    entry.ToggleStickyCommand.Execute(null);

    notifications.ConfirmResult = false;
    viewModel.RemoveEntryCommand.Execute(entry);

    Assert.Equal(1, viewModel.Entries.Count, "Expected sticky entry to remain when confirmation is canceled.");
    Assert.Equal(1, notifications.ConfirmCount, "Expected sticky removal to ask for confirmation.");

    notifications.ConfirmResult = true;
    viewModel.RemoveEntryCommand.Execute(viewModel.Entries.Single());

    Assert.Equal(0, viewModel.Entries.Count, "Expected sticky entry to be removed after confirmation.");
}

static void SortingSupportsPriorityAndAlphabetical()
{
    var data = CreateData();
    data.Profile.Lists[0].Entries.Add(new TrackedEntry
    {
        EntryType = RegistryEntryType.Item,
        RefId = "stone",
        TargetQuantity = 1,
        Priority = 1
    });
    data.Profile.Lists[0].Entries.Add(new TrackedEntry
    {
        EntryType = RegistryEntryType.Item,
        RefId = "wood",
        TargetQuantity = 1,
        Priority = 5
    });

    var viewModel = CreateViewModelWithData(data).ViewModel;

    Assert.Equal("Wood", viewModel.Entries[0].DisplayName, "Expected priority sorting to put higher priority first.");

    viewModel.SelectedSortMode = TrackedListSortMode.Alphabetical;

    Assert.Equal("Stone", viewModel.Entries[0].DisplayName, "Expected alphabetical sorting to put Stone before Wood.");
}

static void JsonPersistenceRoundTrips()
{
    var tempDirectory = Path.Combine(Path.GetTempPath(), $"survival_list_overlay_tests_{Guid.NewGuid():N}");

    try
    {
        var store = new JsonOverlayDataStore(tempDirectory);
        var data = CreateData();
        data.Profile.Lists[0].Entries.Add(new TrackedEntry
        {
            EntryType = RegistryEntryType.Item,
            RefId = "wood",
            TargetQuantity = 25,
            CurrentQuantity = 7,
            Favorite = true,
            Sticky = true,
            Priority = 3
        });

        store.Save(data);
        var loaded = store.Load();

        Assert.True(File.Exists(Path.Combine(tempDirectory, JsonOverlayDataStore.RegistryFileName)), "Expected registry file to be written.");
        Assert.True(File.Exists(Path.Combine(tempDirectory, JsonOverlayDataStore.ProfileFileName)), "Expected profile file to be written.");
        Assert.Equal("manual", loaded.Registry.GameId, "Expected registry to round trip.");
        Assert.Equal(1, loaded.Profile.Lists[0].Entries.Count, "Expected profile entries to round trip.");
        Assert.True(loaded.Profile.Lists[0].Entries[0].Favorite, "Expected favorite flag to persist.");
        Assert.True(loaded.Profile.Lists[0].Entries[0].Sticky, "Expected sticky flag to persist.");
    }
    finally
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}

static void RecipesExposeIngredients()
{
    var (viewModel, _, _) = CreateViewModel();

    AddSearchEntry(viewModel, "workbench", 1, RegistryEntryType.Recipe);

    var recipeEntry = viewModel.Entries.Single();

    Assert.True(recipeEntry.IsRecipe, "Expected workbench search result to add a recipe entry.");
    Assert.Contains("Wood", recipeEntry.IngredientSummary, "Expected recipe summary to include Wood.");

    recipeEntry.ToggleExpandedCommand.Execute(null);
    recipeEntry = viewModel.Entries.Single();

    Assert.True(recipeEntry.IsExpanded, "Expected recipe to expand.");
    Assert.Equal(10, recipeEntry.IngredientRows.Single().RequiredQuantity, "Expected workbench to require 10 wood.");
}

static void DuplicateAddsUpdateExistingEntry()
{
    var (viewModel, _, _) = CreateViewModel();

    AddSearchEntry(viewModel, "wood", 10);
    AddSearchEntry(viewModel, "wood", 5);

    Assert.Equal(1, viewModel.Entries.Count, "Expected duplicate add to update the existing entry.");
    Assert.Equal(15, viewModel.Entries.Single().TargetQuantity, "Expected duplicate add to increase target quantity.");
}

static void ExplicitDuplicateAddsCreateSecondEntry()
{
    var (viewModel, _, _) = CreateViewModel();

    AddSearchEntry(viewModel, "wood", 10);
    viewModel.SearchQuery = "wood";
    viewModel.NewTargetQuantity = "5";
    viewModel.SelectedSearchResult = viewModel.SearchResults.First(result => result.EntryType == RegistryEntryType.Item);
    viewModel.AddDuplicateSelectedResultCommand.Execute(null);

    Assert.Equal(2, viewModel.Entries.Count, "Expected explicit duplicate add to create a second entry.");
}

static void CountingModeTracksCollectedQuantity()
{
    var (viewModel, _, _) = CreateViewModel();

    viewModel.SwitchListTypeCommand.Execute(null);
    AddSearchEntry(viewModel, "copper", 2);
    AddSearchEntry(viewModel, "copper", 3);
    AddSearchEntry(viewModel, "copper", 1);

    Assert.True(viewModel.IsCountingList, "Expected switch command to activate the counting list.");
    Assert.Equal(1, viewModel.Entries.Count, "Expected counting duplicates to merge into one row.");
    Assert.Equal(6, viewModel.Entries.Single().CurrentQuantity, "Expected counting mode to accumulate collected quantity.");
    Assert.Contains("Count: 6", viewModel.Entries.Single().ProgressText, "Expected counting row to show count language.");
}

static void RecipeDemandAggregatesIntoItemRows()
{
    var (viewModel, _, _) = CreateViewModel();

    AddSearchEntry(viewModel, "workbench", 1, RegistryEntryType.Recipe);
    AddSearchEntry(viewModel, "wood", 7, RegistryEntryType.Item);

    var wood = viewModel.Entries.Single(entry => entry.DisplayName == "Wood");

    Assert.Contains("17", wood.ProgressText, "Expected item progress to include direct and recipe demand.");
    Assert.Contains("Recipes 10", wood.DemandText, "Expected item demand text to include recipe demand.");
}

static void OverlaySettingsPersist()
{
    var data = CreateData();
    data.Profile.Overlay.Left = 42;
    data.Profile.Overlay.Top = 84;
    data.Profile.Overlay.Width = 700;
    data.Profile.Overlay.Height = 420;
    data.Profile.Overlay.InteractionMode = OverlayInteractionMode.Edit;

    var viewModel = CreateViewModelWithData(data).ViewModel;

    Assert.Equal(42d, viewModel.OverlaySettings.Left, "Expected overlay left to load.");
    Assert.Equal(84d, viewModel.OverlaySettings.Top, "Expected overlay top to load.");
    Assert.Equal(700d, viewModel.OverlaySettings.Width, "Expected overlay width to load.");
    Assert.Equal(420d, viewModel.OverlaySettings.Height, "Expected overlay height to load.");
    Assert.True(viewModel.IsEditMode, "Expected edit mode to load.");
}

static void AddSearchEntry(
    MainViewModel viewModel,
    string query,
    int targetQuantity,
    RegistryEntryType? entryType = null)
{
    viewModel.SearchQuery = query;
    viewModel.NewTargetQuantity = targetQuantity.ToString();

    if (viewModel.SearchResults.Count > 0)
    {
        viewModel.SelectedSearchResult = entryType is null
            ? viewModel.SearchResults[0]
            : viewModel.SearchResults.First(result => result.EntryType == entryType);
    }

    if (!viewModel.AddSelectedResultCommand.CanExecute(null))
    {
        throw new InvalidOperationException($"Cannot add search entry for '{query}'.");
    }

    viewModel.AddSelectedResultCommand.Execute(null);
}

static (MainViewModel ViewModel, FakeNotificationService Notifications, FakeAudioCueService Audio) CreateViewModel()
{
    return CreateViewModelWithData(CreateData());
}

static (MainViewModel ViewModel, FakeNotificationService Notifications, FakeAudioCueService Audio) CreateViewModelWithData(OverlayData data)
{
    var notifications = new FakeNotificationService();
    var audio = new FakeAudioCueService();
    var store = new MemoryOverlayDataStore(data);
    var viewModel = new MainViewModel(notifications, audio, new OverlaySettings(), store);
    return (viewModel, notifications, audio);
}

static OverlayData CreateData()
{
    var registry = DefaultGameRegistryFactory.Create();
    return new OverlayData
    {
        Registry = registry,
        Profile = DefaultUserProfileFactory.Create(registry.GameId)
    };
}

internal sealed class MemoryOverlayDataStore : IOverlayDataStore
{
    public MemoryOverlayDataStore(OverlayData data)
    {
        Data = data;
    }

    public OverlayData Data { get; private set; }

    public OverlayData Load() => Data;

    public void Save(OverlayData data) => Data = data;
}

internal sealed class FakeNotificationService : IUserNotificationService
{
    public bool ConfirmResult { get; set; } = true;
    public int ConfirmCount { get; private set; }
    public int ErrorCount { get; private set; }

    public bool Confirm(string message, string title)
    {
        ConfirmCount++;
        return ConfirmResult;
    }

    public void ShowError(string message, string title)
    {
        ErrorCount++;
    }
}

internal sealed class FakeAudioCueService : IAudioCueService
{
    public int CompletedCount { get; private set; }

    public void PlayItemCompleted()
    {
        CompletedCount++;
    }
}

internal static class Assert
{
    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected '{expected}', got '{actual}'.");
        }
    }

    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Contains(string expectedSubstring, string actual, string message)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{message} Expected '{actual}' to contain '{expectedSubstring}'.");
        }
    }
}
