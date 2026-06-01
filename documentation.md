# Survival List Overlay Documentation

Last reviewed: 2026-06-01

This document describes the current implementation modules, public interfaces, data models, view models, services, UI workflow, persistence behavior, and test harness.

## Project Structure

```text
survival_list_overlay.sln
survival_list_overlay/
  App.xaml
  App.xaml.cs
  AssemblyInfo.cs
  MainWindow.xaml
  MainWindow.xaml.cs
  survival_list_overlay.csproj
  Commands/
  Models/
  Services/
  Settings/
  ViewModels/
survival_list_overlay.Tests/
  Program.cs
  survival_list_overlay.Tests.csproj
MANUAL_QA.md
documentation.md
```

The main app is a WPF `WinExe` targeting `net8.0-windows`. The test project is a console executable targeting `net8.0-windows` and references the main WPF project.

## Application Entry And Shell

### `App.xaml` / `App.xaml.cs`

Defines the WPF application and starts `MainWindow.xaml` through `StartupUri`.

Current `App` code-behind is empty beyond inheriting from `Application`.

### `MainWindow.xaml`

Defines the overlay UI:

- Borderless transparent window.
- Always-on-top behavior.
- Hover-revealed title bar.
- Header with active game/list and entry count.
- Sort selector.
- Favorites filter toggle.
- Search box and quantity input.
- Search result list.
- Tracked entry list.
- Per-entry quantity, priority, favorite, sticky, remove, and recipe expansion controls.

Important bindings:

- Window title binds to `MainViewModel.Title`.
- Header binds to `HeaderText`.
- Count binds to `EntryCountText`.
- Sort combo binds to `SortModes` and `SelectedSortMode`.
- Favorites toggle binds to `ShowFavoritesOnly`.
- Search input binds to `SearchQuery`.
- Quantity input binds to `NewTargetQuantity`.
- Search results bind to `SearchResults` / `SelectedSearchResult`.
- Entry list binds to `Entries` / `SelectedEntry`.

### `MainWindow.xaml.cs`

Constructs the runtime dependencies and assigns the view model:

```csharp
new MainViewModel(
    new WpfUserNotificationService(),
    new SystemAudioCueService(),
    new OverlaySettings())
```

Handles window-only concerns:

- Dragging the borderless overlay.
- Showing/hiding the title bar on hover.
- Closing the window.
- Keyboard routing.

Keyboard behavior:

- `Ctrl+F`: focus and select search box text.
- `+`: increment selected entry.
- `-`: decrement selected entry.
- `Delete`: remove selected entry.
- `F`: toggle selected entry favorite.
- `S`: toggle selected entry sticky.
- `E`: expand/collapse selected recipe.
- Search box `Down` / `Up`: move search selection.
- Search box `Enter`: add selected search result.
- Search box `Escape`: clear search.

## Commands

### `RelayCommand`

Path: `survival_list_overlay/Commands/RelayCommand.cs`

Implements WPF `ICommand`.

Constructor:

```csharp
RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
```

Members:

- `CanExecute(object? parameter)`.
- `Execute(object? parameter)`.
- `CanExecuteChanged`, backed by `CommandManager.RequerySuggested`.
- `RefreshCanExecute()`, a static helper that calls `CommandManager.InvalidateRequerySuggested()`.

Used throughout view models to expose UI commands.

## Models

Path: `survival_list_overlay/Models/TrackingModels.cs`

### Limits

```csharp
public static class OverlayLimits
```

Constants:

- `MaxListsPerGame = 10`.
- `MaxEntriesPerList = 30`.

These are the authoritative workflow limits. The live add/remove logic uses `MaxEntriesPerList`.

### Enums

```csharp
public enum RegistryEntryType
```

Values:

- `Item`.
- `Recipe`.

```csharp
public enum TrackedListType
```

Values:

- `Standard`.
- `Counting`.

Counting mode is represented in the model but is not yet exposed as a full independent UI workflow.

```csharp
public enum TrackedListSortMode
```

Values:

- `Priority`.
- `Alphabetical`.

### Registry Models

```csharp
public sealed class GameRegistry
```

Represents game-level registry data.

Properties:

- `SchemaVersion`.
- `GameId`.
- `GameName`.
- `Items`.
- `Recipes`.

```csharp
public sealed class RegistryItem
```

Represents a trackable item in a game registry.

Properties:

- `Id`.
- `Name`.
- `Icon`.
- `Tags`.

```csharp
public sealed class RegistryRecipe
```

Represents a craftable recipe in a game registry.

Properties:

- `Id`.
- `Name`.
- `Icon`.
- `OutputItemId`.
- `Ingredients`.
- `Tags`.

```csharp
public sealed class RecipeIngredient
```

Represents an item requirement inside a recipe.

Properties:

- `ItemId`.
- `Quantity`.

### User Data Models

```csharp
public sealed class UserProfile
```

Represents user state separate from registry data.

Properties:

- `SchemaVersion`.
- `ActiveGameId`.
- `ActiveListId`.
- `Lists`.

```csharp
public sealed class TrackedList
```

Represents a user-created tracking list.

Properties:

- `Id`.
- `Name`.
- `Type`.
- `SortMode`.
- `Entries`.

```csharp
public sealed class TrackedEntry : INotifyPropertyChanged
```

Represents a tracked item or recipe entry inside a list.

Properties:

- `Id`.
- `EntryType`.
- `RefId`.
- `TargetQuantity`.
- `CurrentQuantity`.
- `RemainingQuantity`.
- `Priority`.
- `Favorite`.
- `Sticky`.
- `Expanded`.

Normalization rules:

- `TargetQuantity` is clamped to at least `1`.
- `CurrentQuantity` is clamped to at least `0`.
- `RemainingQuantity` is never negative.

Events:

- `PropertyChanged`.

## Services

### `IOverlayDataStore`

Path: `survival_list_overlay/Services/OverlayDataStore.cs`

Interface:

```csharp
public interface IOverlayDataStore
{
    OverlayData Load();
    void Save(OverlayData data);
}
```

Purpose:

- Abstracts persistence for app data.
- Allows production JSON persistence and in-memory test persistence.

### `OverlayData`

Container for both persisted data sets:

- `GameRegistry Registry`.
- `UserProfile Profile`.

### `JsonOverlayDataStore`

Production implementation of `IOverlayDataStore`.

Default storage directory:

```text
%APPDATA%\SurvivalListOverlay
```

Files:

- `registry.json`.
- `profile.json`.

Behavior:

- Creates the data directory if missing.
- Loads registry/profile JSON when present.
- Falls back to default registry/profile when files are missing.
- Writes indented JSON.
- Enforces basic profile shape on load:
  - schema version at least `1`;
  - at least one list;
  - no more than 10 lists;
  - no more than 30 entries per list;
  - valid active list id.

### `DefaultGameRegistryFactory`

Creates a built-in manual registry.

Current built-in items:

- Wood.
- Stone.
- Iron Ore.
- Leather.
- Fiber.
- Workbench.
- Wood Arrow.

Current built-in recipes:

- Workbench: requires 10 Wood.
- Wood Arrow: requires 8 Wood and 2 Fiber.

### `DefaultUserProfileFactory`

Creates a default profile and default list.

Default list:

- `Id = "main"`.
- `Name = "Main"`.
- `Type = Standard`.
- `SortMode = Priority`.

### `RegistryResolver`

Path: `survival_list_overlay/Services/RegistryResolver.cs`

Provides registry lookup and search behavior.

Public members:

- `FindItem(string itemId)`.
- `FindRecipe(string recipeId)`.
- `GetDisplayName(TrackedEntry entry)`.
- `GetItemName(string itemId)`.
- `Search(string query)`.
- `AddCustomItem(string name)`.

Search behavior:

- Searches item and recipe names.
- Searches ids.
- Searches tag summaries.
- Returns up to 8 candidates.
- Empty search returns the first 8 candidates alphabetically.

Custom item behavior:

- Creates a normalized id from the provided name.
- Adds suffixes when ids collide.
- Tags custom items with `custom`.

### `RegistrySearchResult`

Record returned from registry search.

Fields:

- `EntryType`.
- `Id`.
- `DisplayName`.
- `Summary`.

### `TrackedEntrySorter`

Path: `survival_list_overlay/Services/TrackedEntrySorter.cs`

Public method:

```csharp
Sort(IEnumerable<TrackedEntry> entries, TrackedListSortMode sortMode, RegistryResolver resolver)
```

Sort modes:

- `Priority`: highest priority first, then display name.
- `Alphabetical`: display name first, then priority descending.

### `IUserNotificationService`

Path: `survival_list_overlay/Services/UserNotificationService.cs`

Interface:

```csharp
public interface IUserNotificationService
{
    bool Confirm(string message, string title);
    void ShowError(string message, string title);
}
```

### `WpfUserNotificationService`

Production implementation using WPF `MessageBox`.

Behavior:

- `Confirm` displays an OK/Cancel warning message box.
- `ShowError` displays an error message box.

### `IAudioCueService`

Path: `survival_list_overlay/Services/AudioCueService.cs`

Interface:

```csharp
public interface IAudioCueService
{
    void PlayItemCompleted();
}
```

### `SystemAudioCueService`

Production implementation using `SystemSounds.Exclamation.Play()`.

## Settings

### `OverlaySettings`

Path: `survival_list_overlay/Settings/OverlaySettings.cs`

Properties:

- `Title`.
- `MaxVisibleItems`.

Constants:

- `DefaultTitle = "SG Overlay"`.
- `DefaultMaxVisibleItems = 30`.

Review note:

- `MaxVisibleItems` currently mirrors the 30-entry product limit, but the add/remove workflow uses `OverlayLimits.MaxEntriesPerList` directly.

## View Models

### `MainViewModel`

Path: `survival_list_overlay/ViewModels/MainViewModel.cs`

Primary workflow coordinator for the overlay.

Constructor dependencies:

```csharp
MainViewModel(
    IUserNotificationService notificationService,
    IAudioCueService audioCueService,
    OverlaySettings? settings = null,
    IOverlayDataStore? dataStore = null)
```

If no data store is provided, uses `JsonOverlayDataStore`.

Public collections:

- `ObservableCollection<TrackedEntryViewModel> Entries`.
- `ObservableCollection<SearchResultViewModel> SearchResults`.
- `IReadOnlyList<TrackedListSortMode> SortModes`.

Public state:

- `Title`.
- `HeaderText`.
- `EntryCountText`.
- `EmptyStateText`.
- `SelectedSortMode`.
- `SearchQuery`.
- `NewTargetQuantity`.
- `SelectedSearchResult`.
- `SelectedEntry`.
- `ShowFavoritesOnly`.

Commands:

- `AddSelectedResultCommand`.
- `RemoveEntryCommand`.
- `UndoLastRemovalCommand`.
- `IncrementSelectedEntryCommand`.
- `DecrementSelectedEntryCommand`.
- `ToggleSelectedFavoriteCommand`.
- `ToggleSelectedStickyCommand`.
- `ToggleSelectedExpandedCommand`.
- `ToggleFavoritesFilterCommand`.
- `ClearSearchCommand`.

Keyboard helper methods:

- `SelectNextSearchResult()`.
- `SelectPreviousSearchResult()`.
- `AddSelectedSearchResultFromKeyboard()`.
- `RemoveSelectedEntryFromKeyboard()`.

Behavior:

- Loads persisted `OverlayData` on construction.
- Ensures active list shape and max limits.
- Maintains visible entries sorted by selected sort mode.
- Filters visible entries when favorites-only mode is enabled.
- Searches registry as `SearchQuery` changes.
- Adds selected item/recipe search result.
- Creates a custom item if adding with search text and no selected result.
- Confirms duplicate entry adds.
- Confirms removal of sticky entries.
- Supports undo of last removal.
- Saves after data changes.
- Plays completion audio only when quantity crosses from incomplete to complete.

### `TrackedEntryViewModel`

Path: `survival_list_overlay/ViewModels/TrackedEntryViewModel.cs`

Wraps a `TrackedEntry` for display and interaction.

Constructor dependencies:

```csharp
TrackedEntryViewModel(
    TrackedEntry entry,
    RegistryResolver resolver,
    Func<string, int> collectedQuantityProvider,
    Action<string, bool> entryChanged)
```

Public state:

- `Entry`.
- `Id`.
- `DisplayName`.
- `EntryTypeLabel`.
- `IsRecipe`.
- `IsExpanded`.
- `TargetQuantity`.
- `CurrentQuantity`.
- `RemainingQuantity`.
- `Priority`.
- `IsFavorite`.
- `IsSticky`.
- `ProgressText`.
- `RemainingText`.
- `FavoriteText`.
- `StickyText`.
- `ExpandText`.
- `IngredientRows`.
- `IngredientSummary`.

Commands:

- `IncrementCommand`.
- `DecrementCommand`.
- `IncreasePriorityCommand`.
- `DecreasePriorityCommand`.
- `ToggleFavoriteCommand`.
- `ToggleStickyCommand`.
- `ToggleExpandedCommand`.

Behavior:

- For item entries, displays current/target quantities.
- For recipe entries, displays craft count and ingredient requirements.
- Uses `collectedQuantityProvider` to calculate current ingredient progress from tracked item entries.
- Calls `entryChanged` when mutations occur so the parent view model can save and refresh.
- Implements `Dispose()` to detach from `TrackedEntry.PropertyChanged`.

### `SearchResultViewModel`

Path: `survival_list_overlay/ViewModels/SearchResultViewModel.cs`

Display wrapper for `RegistrySearchResult`.

Properties:

- `EntryType`.
- `RefId`.
- `DisplayName`.
- `Summary`.
- `EntryTypeLabel`.

### `IngredientRequirementViewModel`

Path: `survival_list_overlay/ViewModels/IngredientRequirementViewModel.cs`

Display model for a recipe ingredient row.

Properties:

- `Name`.
- `CurrentQuantity`.
- `RequiredQuantity`.
- `RemainingQuantity`.
- `ProgressText`.

## Data Flow

Startup flow:

1. `App.xaml` starts `MainWindow`.
2. `MainWindow` creates WPF services, settings, and `MainViewModel`.
3. `MainViewModel` loads `OverlayData` through `IOverlayDataStore`.
4. `JsonOverlayDataStore` loads or creates registry/profile JSON.
5. `MainViewModel` creates a `RegistryResolver`, refreshes search results, and refreshes visible entries.

Add entry flow:

1. User types search text.
2. `SearchQuery` refreshes `SearchResults`.
3. User selects a search result and enters target quantity.
4. `AddSelectedResultCommand` creates a `TrackedEntry`.
5. Entry is appended to the active list.
6. Data is saved.
7. Visible entries and search results refresh.

Mutate entry flow:

1. User changes quantity, favorite, sticky, priority, or expansion state.
2. `TrackedEntryViewModel` updates the underlying `TrackedEntry`.
3. Parent `MainViewModel` saves data.
4. Visible entries refresh to preserve sort/filter behavior.

Persistence flow:

1. Registry data is saved to `registry.json`.
2. User profile data is saved to `profile.json`.
3. Both files live in `%APPDATA%\SurvivalListOverlay` by default.

## Test Harness

Path: `survival_list_overlay.Tests/Program.cs`

This is a simple console test runner rather than an xUnit/NUnit project.

Reasons:

- No external test packages are required.
- It can run in restricted restore/network environments.

Covered scenarios:

- Entry limit is 30.
- Sticky removal requires confirmation.
- Sorting supports priority and alphabetical modes.
- JSON persistence round trips registry and profile separately.
- Recipes expose expandable ingredient requirements.

Run from repo root:

```powershell
dotnet run --project .\survival_list_overlay.Tests\survival_list_overlay.Tests.csproj
```

Expected output:

```text
PASS entry limit is 30
PASS sticky removal requires confirmation
PASS sorting supports priority and alphabetical
PASS json persistence round trips registry and profile separately
PASS recipes expose expandable ingredient requirements
```

## Manual QA

Path: `MANUAL_QA.md`

Manual checks cover:

- Overlay startup.
- Hover title bar.
- Drag behavior.
- Item add/increment/decrement/remove.
- Recipe add/expand.
- Favorites filter.
- Sticky removal confirmation.
- Sort mode changes.
- Keyboard workflow.
- Persistence after restart.

## Current Review Notes

- `Counting` list mode now has a switchable UI workflow. It tracks collected quantity rather than target/remaining quantity.
- Multiple lists are represented in the data model. The UI can switch between the first standard and counting lists, but does not yet expose full list creation, renaming, or deletion.
- Registry import/export is not implemented yet; the default registry is created in code.
- Recipe nesting depth is not enforced yet.
- Overlay window position, size, opacity, scale, interaction mode, theme defaults, and keybind defaults are persisted in `UserProfile.Overlay`.
- Locked mode prevents accidental drag/resize. Edit mode exposes the title/header controls and allows resize.
- `OverlaySettings.MaxVisibleItems` exists but is not the source of truth for entry limits; `OverlayLimits.MaxEntriesPerList` is.
- Completion audio is tied to quantity changes crossing the target threshold.
- Custom item creation happens through search text when no search result is selected.
- Normal Add updates an existing tracked item/recipe by increasing its target or count. Explicit duplicate creation is available through a separate command.
- Standard item rows show direct target plus recipe ingredient demand when recipes require the same material.

## Usability Refactor Notes

The 2026-06-01 usability refactor added the first pass of the gameplay-safe overlay model:

- `OverlayInteractionMode.Locked` and `OverlayInteractionMode.Edit`.
- `OverlayUserSettings`, `OverlayThemeSettings`, and `OverlayKeybindSettings`.
- persisted overlay bounds and display preferences in the user profile.
- explicit duplicate entry creation separate from normal Add.
- counting-list UI semantics for collected quantities.
- recipe-aware material demand aggregation for standard item rows.
- darker styled controls and icon-style row actions in the WPF shell.

True OS-level click-through, full keybind editing/conflict detection, theme editing UI, registry import/export, and recipe nesting expansion remain future work.
