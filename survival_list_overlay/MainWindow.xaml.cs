using System.Windows;
using System.Windows.Input;
using survival_list_overlay.Services;
using survival_list_overlay.Settings;
using survival_list_overlay.ViewModels;

namespace survival_list_overlay
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel(
                new WpfUserNotificationService(),
                new SystemAudioCueService(),
                new OverlaySettings());
            DataContext = ViewModel;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void RootGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            TitleBar.Visibility = Visibility.Visible;
        }

        private void RootGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!TitleBar.IsMouseOver)
            {
                TitleBar.Visibility = Visibility.Collapsed;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
