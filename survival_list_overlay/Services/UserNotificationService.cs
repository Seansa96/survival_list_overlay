using System.Windows;

namespace survival_list_overlay.Services;

public interface IUserNotificationService
{
    void ShowError(string message, string title);
}

public sealed class WpfUserNotificationService : IUserNotificationService
{
    public void ShowError(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
