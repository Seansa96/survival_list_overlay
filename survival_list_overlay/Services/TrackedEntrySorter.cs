using survival_list_overlay.Models;

namespace survival_list_overlay.Services;

public static class TrackedEntrySorter
{
    public static IEnumerable<TrackedEntry> Sort(
        IEnumerable<TrackedEntry> entries,
        TrackedListSortMode sortMode,
        RegistryResolver resolver)
    {
        return sortMode switch
        {
            TrackedListSortMode.Alphabetical => entries
                .OrderBy(entry => resolver.GetDisplayName(entry), StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(entry => entry.Priority),
            _ => entries
                .OrderByDescending(entry => entry.Priority)
                .ThenBy(entry => resolver.GetDisplayName(entry), StringComparer.OrdinalIgnoreCase)
        };
    }
}
