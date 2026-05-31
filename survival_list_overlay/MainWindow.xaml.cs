using System.Windows;
using System.Windows.Controls;
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

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
                return;
            }

            if (Keyboard.FocusedElement is TextBox)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Add:
                case Key.OemPlus:
                    ExecuteIfAvailable(ViewModel.IncrementSelectedEntryCommand);
                    e.Handled = true;
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    ExecuteIfAvailable(ViewModel.DecrementSelectedEntryCommand);
                    e.Handled = true;
                    break;
                case Key.Delete:
                    ViewModel.RemoveSelectedEntryFromKeyboard();
                    e.Handled = true;
                    break;
                case Key.F:
                    ExecuteIfAvailable(ViewModel.ToggleSelectedFavoriteCommand);
                    e.Handled = true;
                    break;
                case Key.S:
                    ExecuteIfAvailable(ViewModel.ToggleSelectedStickyCommand);
                    e.Handled = true;
                    break;
                case Key.E:
                    ExecuteIfAvailable(ViewModel.ToggleSelectedExpandedCommand);
                    e.Handled = true;
                    break;
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    ViewModel.SelectNextSearchResult();
                    e.Handled = true;
                    break;
                case Key.Up:
                    ViewModel.SelectPreviousSearchResult();
                    e.Handled = true;
                    break;
                case Key.Enter:
                    ViewModel.AddSelectedSearchResultFromKeyboard();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    ExecuteIfAvailable(ViewModel.ClearSearchCommand);
                    e.Handled = true;
                    break;
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

        private static void ExecuteIfAvailable(ICommand command)
        {
            if (command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }
}
