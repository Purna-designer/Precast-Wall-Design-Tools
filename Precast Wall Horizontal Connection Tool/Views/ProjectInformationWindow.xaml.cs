using System.Windows;
using PrecastConnectionApp.ViewModels;

namespace PrecastConnectionApp.Views
{
    public partial class ProjectInformationWindow : Window
    {
        public ProjectInformationWindow(ProjectInformationViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.CloseAction = () => Close();
        }
    }
}
