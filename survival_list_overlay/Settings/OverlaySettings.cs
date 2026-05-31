namespace survival_list_overlay.Settings;

public sealed class OverlaySettings
{
    public const string DefaultTitle = "SG Overlay";
    public const int DefaultMaxVisibleItems = 12;

    public string Title { get; init; } = DefaultTitle;
    public int MaxVisibleItems { get; init; } = DefaultMaxVisibleItems;
}
