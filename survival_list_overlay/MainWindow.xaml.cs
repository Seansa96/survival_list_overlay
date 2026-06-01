using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using survival_list_overlay.Services;
using survival_list_overlay.Settings;
using survival_list_overlay.ViewModels;

namespace survival_list_overlay
{
    public partial class MainWindow : Window
    {
        private readonly KeybindService keybindService;

        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel(
                new WpfUserNotificationService(),
                new SystemAudioCueService(),
                new OverlaySettings());
            keybindService = new KeybindService(ViewModel.OverlaySettings.Keybinds);
            DataContext = ViewModel;
            ApplyOverlayWindowSettings();
            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.IsEditMode))
                {
                    ApplyInteractionMode();
                }
            };
            LocationChanged += (_, _) => SaveOverlayWindowState();
            SizeChanged += (_, _) => SaveOverlayWindowState();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.IsEditMode && e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (keybindService.IsToggleOverlay(e))
            {
                Opacity = Opacity > 0.1 ? 0.08 : ViewModel.OverlaySettings.Opacity;
                e.Handled = true;
                return;
            }

            if (keybindService.IsToggleInteractionMode(e))
            {
                ExecuteIfAvailable(ViewModel.ToggleInteractionModeCommand);
                e.Handled = true;
                return;
            }

            if (keybindService.IsFocusSearch(e))
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

            if (keybindService.IsIncrementSelected(e))
            {
                ExecuteIfAvailable(ViewModel.IncrementSelectedEntryCommand);
                e.Handled = true;
                return;
            }

            if (keybindService.IsDecrementSelected(e))
            {
                ExecuteIfAvailable(ViewModel.DecrementSelectedEntryCommand);
                e.Handled = true;
                return;
            }

            if (keybindService.IsToggleFavorites(e))
            {
                ExecuteIfAvailable(ViewModel.ToggleFavoritesFilterCommand);
                e.Handled = true;
                return;
            }

            if (keybindService.IsSwitchList(e))
            {
                ExecuteIfAvailable(ViewModel.SwitchListTypeCommand);
                e.Handled = true;
                return;
            }

            switch (e.Key)
            {
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
                case Key.Down:
                    ViewModel.SelectNextEntry();
                    e.Handled = true;
                    break;
                case Key.Up:
                    ViewModel.SelectPreviousEntry();
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
            if (ViewModel.IsEditMode)
            {
                TitleBar.Visibility = Visibility.Visible;
            }
        }

        private void RootGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!ViewModel.IsEditMode && !TitleBar.IsMouseOver)
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

        private void ApplyOverlayWindowSettings()
        {
            Left = ViewModel.OverlaySettings.Left;
            Top = ViewModel.OverlaySettings.Top;
            Width = ViewModel.OverlaySettings.Width;
            Height = ViewModel.OverlaySettings.Height;
            Opacity = ViewModel.OverlaySettings.Opacity;
            LayoutTransform = new ScaleTransform(ViewModel.OverlaySettings.Scale, ViewModel.OverlaySettings.Scale);
            FontFamily = new System.Windows.Media.FontFamily(ViewModel.OverlaySettings.Theme.FontFamily);
            FontSize = ViewModel.OverlaySettings.Theme.FontSize;
            ApplyInteractionMode();
        }

        private void ApplyInteractionMode()
        {
            ResizeMode = ViewModel.IsEditMode ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize;
            TitleBar.Visibility = ViewModel.IsEditMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SaveOverlayWindowState()
        {
            ViewModel.SaveOverlayWindowState(Left, Top, Width, Height);
        }
    }
}
