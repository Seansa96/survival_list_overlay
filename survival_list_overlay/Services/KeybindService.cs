using System.Windows.Input;
using survival_list_overlay.Models;

namespace survival_list_overlay.Services;

public sealed class KeybindService
{
    private readonly OverlayKeybindSettings settings;

    public KeybindService(OverlayKeybindSettings settings)
    {
        this.settings = settings;
    }

    public bool IsToggleOverlay(KeyEventArgs e) => Matches(e, settings.ToggleOverlay);
    public bool IsToggleInteractionMode(KeyEventArgs e) => Matches(e, settings.ToggleInteractionMode);
    public bool IsFocusSearch(KeyEventArgs e) => Matches(e, settings.FocusSearch);
    public bool IsIncrementSelected(KeyEventArgs e) => Matches(e, settings.IncrementSelected);
    public bool IsDecrementSelected(KeyEventArgs e) => Matches(e, settings.DecrementSelected);
    public bool IsToggleFavorites(KeyEventArgs e) => Matches(e, settings.ToggleFavorites);
    public bool IsSwitchList(KeyEventArgs e) => Matches(e, settings.SwitchList);

    private static bool Matches(KeyEventArgs e, string gesture)
    {
        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var expectedModifiers = ModifierKeys.None;
        foreach (var modifier in parts.Take(parts.Length - 1))
        {
            expectedModifiers |= modifier.ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModifierKeys.Control,
                "shift" => ModifierKeys.Shift,
                "alt" => ModifierKeys.Alt,
                _ => ModifierKeys.None
            };
        }

        return Keyboard.Modifiers == expectedModifiers && KeyMatches(e.Key, parts[^1]);
    }

    private static bool KeyMatches(Key actualKey, string expectedKey)
    {
        return expectedKey.ToLowerInvariant() switch
        {
            "plus" => actualKey is Key.Add or Key.OemPlus,
            "minus" => actualKey is Key.Subtract or Key.OemMinus,
            "tab" => actualKey == Key.Tab,
            _ => Enum.TryParse<Key>(expectedKey, ignoreCase: true, out var parsedKey) && actualKey == parsedKey
        };
    }
}
