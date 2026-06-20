using System.Windows;
using System.Windows.Controls;

namespace PrecastConnectionApp.Views
{
    public partial class CommandLogWindow : Window
    {
        public CommandLogWindow()
        {
            InitializeComponent();
        }

        private void CommandLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
