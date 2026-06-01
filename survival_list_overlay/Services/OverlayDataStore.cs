using System.IO;
using System.Text.Json;
using survival_list_overlay.Models;

namespace survival_list_overlay.Services;

public sealed class OverlayData
{
    public GameRegistry Registry { get; init; } = DefaultGameRegistryFactory.Create();
    public UserProfile Profile { get; init; } = DefaultUserProfileFactory.Create("manual");
}

public interface IOverlayDataStore
{
    OverlayData Load();
    void Save(OverlayData data);
}

public sealed class JsonOverlayDataStore : IOverlayDataStore
{
    public const string RegistryFileName = "registry.json";
    public const string ProfileFileName = "profile.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string dataDirectory;

    public JsonOverlayDataStore(string? dataDirectory = null)
    {
        this.dataDirectory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SurvivalListOverlay");
    }

    public OverlayData Load()
    {
        Directory.CreateDirectory(dataDirectory);

        var registry = LoadJson<GameRegistry>(RegistryPath()) ?? DefaultGameRegistryFactory.Create();
        var profile = LoadJson<UserProfile>(ProfilePath()) ?? DefaultUserProfileFactory.Create(registry.GameId);

        EnsureProfileShape(profile, registry);

        return new OverlayData
        {
            Registry = registry,
            Profile = profile
        };
    }

    public void Save(OverlayData data)
    {
        Directory.CreateDirectory(dataDirectory);
        SaveJson(RegistryPath(), data.Registry);
        SaveJson(ProfilePath(), data.Profile);
    }

    private string RegistryPath() => Path.Combine(dataDirectory, RegistryFileName);

    private string ProfilePath() => Path.Combine(dataDirectory, ProfileFileName);

    private static T? LoadJson<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, SerializerOptions);
    }

    private static void SaveJson<T>(string path, T value)
    {
        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, value, SerializerOptions);
    }

    private static void EnsureProfileShape(UserProfile profile, GameRegistry registry)
    {
        profile.SchemaVersion = Math.Max(2, profile.SchemaVersion);
        profile.ActiveGameId = string.IsNullOrWhiteSpace(profile.ActiveGameId)
            ? registry.GameId
            : profile.ActiveGameId;
        profile.Overlay ??= new OverlayUserSettings();
        profile.Overlay.Theme ??= new OverlayThemeSettings();
        profile.Overlay.Keybinds ??= new OverlayKeybindSettings();
        profile.Overlay.Width = Math.Clamp(profile.Overlay.Width, 360, 1400);
        profile.Overlay.Height = Math.Clamp(profile.Overlay.Height, 240, 1000);
        profile.Overlay.Scale = Math.Clamp(profile.Overlay.Scale, 0.75, 2.0);
        profile.Overlay.Opacity = Math.Clamp(profile.Overlay.Opacity, 0.35, 1.0);

        if (profile.Lists.Count == 0)
        {
            profile.Lists.Add(DefaultUserProfileFactory.CreateDefaultList());
        }

        if (profile.Lists.Count > OverlayLimits.MaxListsPerGame)
        {
            profile.Lists = profile.Lists.Take(OverlayLimits.MaxListsPerGame).ToList();
        }

        foreach (var list in profile.Lists)
        {
            if (string.IsNullOrWhiteSpace(list.Id))
            {
                list.Id = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrWhiteSpace(list.Name))
            {
                list.Name = list.Type == TrackedListType.Counting ? "Counting" : "Main";
            }

            if (list.Entries.Count > OverlayLimits.MaxEntriesPerList)
            {
                list.Entries = list.Entries.Take(OverlayLimits.MaxEntriesPerList).ToList();
            }
        }

        if (string.IsNullOrWhiteSpace(profile.ActiveListId)
            || profile.Lists.All(list => list.Id != profile.ActiveListId))
        {
            profile.ActiveListId = profile.Lists[0].Id;
        }
    }
}

public static class DefaultGameRegistryFactory
{
    public static GameRegistry Create()
    {
        return new GameRegistry
        {
            SchemaVersion = 1,
            GameId = "manual",
            GameName = "Manual",
            Items =
            {
                new RegistryItem { Id = "wood", Name = "Wood", Tags = { "material", "building" } },
                new RegistryItem { Id = "stone", Name = "Stone", Tags = { "material", "building" } },
                new RegistryItem { Id = "iron_ore", Name = "Iron Ore", Tags = { "material", "ore" } },
                new RegistryItem { Id = "leather", Name = "Leather", Tags = { "material", "armor" } },
                new RegistryItem { Id = "fiber", Name = "Fiber", Tags = { "material", "plant" } },
                new RegistryItem { Id = "workbench", Name = "Workbench", Tags = { "crafting", "station" } },
                new RegistryItem { Id = "wood_arrow", Name = "Wood Arrow", Tags = { "ammo", "weapon" } }
            },
            Recipes =
            {
                new RegistryRecipe
                {
                    Id = "workbench",
                    Name = "Workbench",
                    OutputItemId = "workbench",
                    Tags = { "crafting", "station" },
                    Ingredients =
                    {
                        new RecipeIngredient { ItemId = "wood", Quantity = 10 }
                    }
                },
                new RegistryRecipe
                {
                    Id = "wood_arrow",
                    Name = "Wood Arrow",
                    OutputItemId = "wood_arrow",
                    Tags = { "ammo", "weapon" },
                    Ingredients =
                    {
                        new RecipeIngredient { ItemId = "wood", Quantity = 8 },
                        new RecipeIngredient { ItemId = "fiber", Quantity = 2 }
                    }
                }
            }
        };
    }
}

public static class DefaultUserProfileFactory
{
    public static UserProfile Create(string gameId)
    {
        var defaultList = CreateDefaultList();
        var countingList = CreateCountingList();
        return new UserProfile
        {
            SchemaVersion = 2,
            ActiveGameId = gameId,
            ActiveListId = defaultList.Id,
            Lists = { defaultList, countingList }
        };
    }

    public static TrackedList CreateDefaultList()
    {
        return new TrackedList
        {
            Id = "main",
            Name = "Main",
            Type = TrackedListType.Standard,
            SortMode = TrackedListSortMode.Priority
        };
    }

    public static TrackedList CreateCountingList()
    {
        return new TrackedList
        {
            Id = "counting",
            Name = "Counting",
            Type = TrackedListType.Counting,
            SortMode = TrackedListSortMode.Alphabetical
        };
    }
}
