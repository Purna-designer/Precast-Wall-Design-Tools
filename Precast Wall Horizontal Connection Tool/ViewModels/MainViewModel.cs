using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrecastConnectionApp.Models;
using PrecastConnectionApp.Services;

namespace PrecastConnectionApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private ObservableCollection<ObservableObject> _workspaces;
        public ObservableCollection<ObservableObject> Workspaces
        {
            get => _workspaces;
            set => SetProperty(ref _workspaces, value);
        }

        private ObservableObject _selectedWorkspace;
        public ObservableObject SelectedWorkspace
        {
            get => _selectedWorkspace;
            set
            {
                if (SetProperty(ref _selectedWorkspace, value))
                {
                    OnPropertyChanged(nameof(IsProjectActive));
                    if (value is DashboardViewModel dvm)
                    {
                        dvm.LoadRecentProjects();
                    }
                }
            }
        }

        // Persistent Dashboard
        private DashboardViewModel _dashboardViewModel;

        private readonly ProjectService _projectService;
        public readonly RecentProjectsService RecentProjectsService;

        public bool IsProjectActive => SelectedWorkspace is ProjectWorkspaceViewModel;

        public IRelayCommand MinimizeCommand { get; }
        public IRelayCommand MaximizeCommand { get; }
        public IRelayCommand CloseCommand { get; }
        public IRelayCommand NewProjectCommand { get; }
        public IRelayCommand OpenProjectCommand { get; }
        public IRelayCommand ViewLogCommand { get; }
        public IRelayCommand SaveProjectCommand { get; }
        public IRelayCommand SaveAsCommand { get; }
        public IRelayCommand<ProjectWorkspaceViewModel> CloseProjectCommand { get; }
        public IRelayCommand<string> NavigateCommand { get; }
        public IRelayCommand<PrecastWall> NavigateToPierCommand { get; }
        public IRelayCommand EditProjectInformationCommand { get; }
        public IRelayCommand ExitCommand { get; }

        public MainViewModel()
        {
            _projectService = new ProjectService();
            RecentProjectsService = new RecentProjectsService();
            
            MinimizeCommand = new RelayCommand(Minimize);
            MaximizeCommand = new RelayCommand(Maximize);
            CloseCommand = new RelayCommand(Close);
            NewProjectCommand = new RelayCommand(NewProject);
            OpenProjectCommand = new RelayCommand(OpenProject);
            ViewLogCommand = new RelayCommand(ViewLog);
            SaveProjectCommand = new RelayCommand(SaveProject);
            SaveAsCommand = new RelayCommand(SaveAs);
            CloseProjectCommand = new RelayCommand<ProjectWorkspaceViewModel>(CloseProject);
            NavigateCommand = new RelayCommand<string>(Navigate);
            NavigateToPierCommand = new RelayCommand<PrecastWall>(NavigateToPier);
            EditProjectInformationCommand = new RelayCommand(EditProjectInformation, () => IsProjectActive);
            ExitCommand = new RelayCommand(Exit);

            Workspaces = new ObservableCollection<ObservableObject>();
            _dashboardViewModel = new DashboardViewModel(this);
            Workspaces.Add(_dashboardViewModel);
            SelectedWorkspace = _dashboardViewModel;
        }

        private void CloseProject(ProjectWorkspaceViewModel workspace)
        {
            if (workspace == null) return;
            
            if (workspace.IsDirty)
            {
                var result = MessageBox.Show($"Save changes to {workspace.ProjectName} before closing?", "Save Project", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel) return;
                if (result == MessageBoxResult.Yes)
                {
                    PerformSave(workspace);
                    if (workspace.IsDirty) return; // Save failed
                }
            }

            int index = Workspaces.IndexOf(workspace);
            Workspaces.Remove(workspace);
            
            if (Workspaces.Count > 1)
            {
                // Fallback to the tab before it, or if it was the first tab (after dashboard), the one after it
                if (index > 1)
                {
                    SelectedWorkspace = Workspaces[index - 1];
                }
                else
                {
                    SelectedWorkspace = Workspaces[1]; // The first actual project tab
                }
            }
            else
            {
                SelectedWorkspace = _dashboardViewModel;
            }
        }

        private bool PromptSaveIfDirty(ProjectWorkspaceViewModel workspace)
        {
            if (workspace == null || !workspace.IsDirty) return true;

            var result = MessageBox.Show($"You have unsaved changes in {workspace.ProjectName}. Do you want to save them?", "Save Project", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Cancel) return false;
            
            if (result == MessageBoxResult.Yes)
            {
                SaveProjectInternal(workspace);
                if (workspace.IsDirty) return false; 
            }

            return true;
        }

        private bool PromptSaveAllDirty()
        {
            foreach (var ws in Workspaces.OfType<ProjectWorkspaceViewModel>())
            {
                if (!PromptSaveIfDirty(ws)) return false;
            }
            return true;
        }

        private void Minimize() => Application.Current.MainWindow.WindowState = WindowState.Minimized;

        private void Maximize()
        {
            var window = Application.Current.MainWindow;
            if (window.WindowState == WindowState.Maximized)
                window.WindowState = WindowState.Normal;
            else
                window.WindowState = WindowState.Maximized;
        }

        private void Close() => Exit();

        private void NewProject()
        {
            if (Workspaces.Count >= 6)
            {
                MessageBox.Show("Maximum of 5 active projects allowed.", "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var vm = new ProjectInformationViewModel();
            var window = new PrecastConnectionApp.Views.ProjectInformationWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };
            
            window.ShowDialog();

            if (vm.IsSaved)
            {
                StatusNotifier.Instance.CommandLog = "Ready\n"; // Reset log
                var newProject = new ProjectWorkspaceViewModel(vm.GetUpdatedProjectData());
                newProject.IsDirty = true; // Mark as dirty since it's unsaved
                Workspaces.Add(newProject);
                SelectedWorkspace = newProject;
                StatusNotifier.Instance.SetStatus("Started new project.");
            }
        }

        private void OpenProject()
        {
            if (Workspaces.Count >= 6)
            {
                MessageBox.Show("Maximum of 5 active projects allowed.", "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Project"
            };

            if (dialog.ShowDialog() == true)
            {
                OpenSpecificProject(dialog.FileName);
            }
        }

        public void OpenSpecificProject(string filePath)
        {
            var existing = Workspaces.OfType<ProjectWorkspaceViewModel>().FirstOrDefault(w => w.FilePath == filePath);
            if (existing != null)
            {
                SelectedWorkspace = existing;
                return;
            }

            if (Workspaces.Count >= 6)
            {
                MessageBox.Show("Maximum of 5 active projects allowed.", "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var projectData = _projectService.LoadProject(filePath);
                RecentProjectsService.AddOrUpdateProject(filePath, projectData);

                var newWorkspace = new ProjectWorkspaceViewModel(projectData, filePath);
                newWorkspace.IsDirty = false;
                
                Workspaces.Add(newWorkspace);
                SelectedWorkspace = newWorkspace;

                StatusNotifier.Instance.CommandLog = "Ready\n"; // Reset log
                StatusNotifier.Instance.SetStatus($"Opened project: {filePath}");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to open project: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewLog()
        {
            var logWindow = new PrecastConnectionApp.Views.CommandLogWindow();
            logWindow.Owner = System.Windows.Application.Current.MainWindow;
            logWindow.ShowDialog();
        }

        private void SaveProject()
        {
            if (SelectedWorkspace is ProjectWorkspaceViewModel activeWorkspace)
            {
                SaveProjectInternal(activeWorkspace);
            }
        }

        private void SaveProjectInternal(ProjectWorkspaceViewModel workspace)
        {
            if (string.IsNullOrEmpty(workspace.FilePath))
            {
                SaveAsInternal(workspace);
            }
            else
            {
                PerformSave(workspace);
            }
        }

        private void SaveAs()
        {
            if (SelectedWorkspace is ProjectWorkspaceViewModel activeWorkspace)
            {
                SaveAsInternal(activeWorkspace);
            }
        }

        private void SaveAsInternal(ProjectWorkspaceViewModel workspace)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Save Project As"
            };

            if (dialog.ShowDialog() == true)
            {
                workspace.FilePath = dialog.FileName;
                PerformSave(workspace);
            }
        }

        private void PerformSave(ProjectWorkspaceViewModel workspace)
        {
            try
            {
                _projectService.SaveProject(workspace.FilePath, workspace.ProjectData);
                workspace.IsDirty = false;
                RecentProjectsService.AddOrUpdateProject(workspace.FilePath, workspace.ProjectData);
                _dashboardViewModel.LoadRecentProjects();
                StatusNotifier.Instance.SetStatus($"Project saved to {workspace.FilePath}");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to save project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Navigate(string viewName)
        {
            if (!string.IsNullOrEmpty(viewName))
            {
                switch (viewName)
                {
                    case "Dashboard":
                        _dashboardViewModel.LoadRecentProjects();
                        SelectedWorkspace = _dashboardViewModel;
                        break;
                    case "ProjectSummary":
                        if (SelectedWorkspace is ProjectWorkspaceViewModel p1)
                            p1.NavigateToSummary();
                        break;
                    case "DesignWorkspace":
                        // Should not be called generically without Pier, but handled in NavigateToPier
                        break;
                }
            }
        }

        private void NavigateToPier(PrecastWall item)
        {
            if (item != null && SelectedWorkspace is ProjectWorkspaceViewModel activeWorkspace)
            {
                activeWorkspace.NavigateToDesign(item.Label, item.Story);
            }
        }

        private void EditProjectInformation()
        {
            if (SelectedWorkspace is ProjectWorkspaceViewModel activeWorkspace)
            {
                var vm = new ProjectInformationViewModel(activeWorkspace.ProjectData);
                var window = new PrecastConnectionApp.Views.ProjectInformationWindow(vm)
                {
                    Owner = Application.Current.MainWindow
                };
                
                window.ShowDialog();

                if (vm.IsSaved)
                {
                    vm.GetUpdatedProjectData();
                    activeWorkspace.IsDirty = true;
                }
            }
        }

        private void Exit()
        {
            Application.Current.MainWindow.Close();
        }
        
        public bool OnClosing()
        {
            return PromptSaveAllDirty();
        }
    }
}
