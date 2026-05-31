using System.Windows;

namespace survival_list_overlay.Services;

public interface IUserNotificationService
{
    bool Confirm(string message, string title);
    void ShowError(string message, string title);
}

public sealed class WpfUserNotificationService : IUserNotificationService
{
    public bool Confirm(string message, string title)
    {
        return MessageBox.Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK;
    }

    public void ShowError(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
