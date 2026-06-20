using CommunityToolkit.Mvvm.ComponentModel;
using PrecastConnectionApp.Models;

namespace PrecastConnectionApp.ViewModels
{
    public partial class ProjectWorkspaceViewModel : ObservableObject
    {
        private ProjectData _projectData;
        public ProjectData ProjectData
        {
            get => _projectData;
            private set => SetProperty(ref _projectData, value);
        }

        public string ProjectName => string.IsNullOrWhiteSpace(_projectData?.ProjectName) ? "Untitled Project" : _projectData.ProjectName;

        private string _filePath;
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }

        public ProjectSummaryViewModel SummaryViewModel { get; }
        public DesignWorkspaceViewModel DesignViewModel { get; }

        private ObservableObject _currentView;
        public ObservableObject CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public ProjectWorkspaceViewModel(ProjectData projectData, string filePath = "")
        {
            _projectData = projectData;
            _filePath = filePath;
            
            SummaryViewModel = new ProjectSummaryViewModel(_projectData);
            DesignViewModel = new DesignWorkspaceViewModel(_projectData);

            if (_projectData.Walls != null)
            {
                _projectData.Walls.CollectionChanged += (s, e) => IsDirty = true;
            }
            _projectData.PropertyChanged += (s, e) => 
            {
                if (e.PropertyName == nameof(ProjectData.ProjectName))
                {
                    OnPropertyChanged(nameof(ProjectName));
                }
                IsDirty = true;
            };

            CurrentView = SummaryViewModel;
        }

        public void NavigateToDesign(string pierLabel, string story)
        {
            DesignViewModel.SelectedPierLabel = pierLabel;
            DesignViewModel.SelectedStory = story;
            CurrentView = DesignViewModel;
        }

        public void NavigateToSummary()
        {
            CurrentView = SummaryViewModel;
        }
    }
}
