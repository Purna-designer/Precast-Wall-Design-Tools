using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PrecastConnectionApp.ViewModels;

namespace PrecastConnectionApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                e.Handled = true;
            }
            else
            {
                DragMove();
            }
        }

        private void HamburgerMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void CommandLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // If OnClosing returns false, it means the user cancelled the save prompt. Cancel the close.
                if (!vm.OnClosing())
                {
                    e.Cancel = true;
                }
            }
            base.OnClosing(e);
        }
    }
}