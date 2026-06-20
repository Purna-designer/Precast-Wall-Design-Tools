using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrecastConnectionApp.Models;

namespace PrecastConnectionApp.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        public ICommand NewProjectCommand => _mainViewModel.NewProjectCommand;
        public ICommand OpenProjectCommand => _mainViewModel.OpenProjectCommand;

        private ObservableCollection<RecentProject> _recentProjects;
        public ObservableCollection<RecentProject> RecentProjects
        {
            get => _recentProjects;
            set => SetProperty(ref _recentProjects, value);
        }

        public IRelayCommand<string> OpenRecentProjectCommand { get; }

        public DashboardViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            OpenRecentProjectCommand = new RelayCommand<string>(OpenRecentProject);
            LoadRecentProjects();
        }

        public void LoadRecentProjects()
        {
            var projects = _mainViewModel.RecentProjectsService.LoadRecentProjects();
            RecentProjects = new ObservableCollection<RecentProject>(projects);
        }

        private void OpenRecentProject(string filePath)
        {
            if (File.Exists(filePath))
            {
                _mainViewModel.OpenSpecificProject(filePath);
            }
            else
            {
                var result = MessageBox.Show($"The file could not be found at:\n{filePath}\n\nWould you like to remove it from the recent projects list?", 
                    "File Not Found", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    _mainViewModel.RecentProjectsService.RemoveProject(filePath);
                    LoadRecentProjects();
                }
            }
        }
    }
}
