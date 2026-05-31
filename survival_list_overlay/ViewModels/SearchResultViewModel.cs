using survival_list_overlay.Models;
using survival_list_overlay.Services;

namespace survival_list_overlay.ViewModels;

public sealed class SearchResultViewModel
{
    public SearchResultViewModel(RegistrySearchResult result)
    {
        EntryType = result.EntryType;
        RefId = result.Id;
        DisplayName = result.DisplayName;
        Summary = result.Summary;
    }

    public RegistryEntryType EntryType { get; }
    public string RefId { get; }
    public string DisplayName { get; }
    public string Summary { get; }
    public string EntryTypeLabel => EntryType == RegistryEntryType.Recipe ? "Recipe" : "Item";
}
