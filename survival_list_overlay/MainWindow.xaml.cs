using survival_list_overlay.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace survival_list_overlay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
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