using System;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrecastConnectionApp.Models;

namespace PrecastConnectionApp.ViewModels
{
    public partial class ProjectInformationViewModel : ObservableObject, IDataErrorInfo
    {
        private readonly ProjectData _projectData;
        private bool _hasUserInteracted = false;

        private string _projectName;
        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (SetProperty(ref _projectName, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                    ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
                }
            }
        }

        private string _projectNumber;
        public string ProjectNumber
        {
            get => _projectNumber;
            set
            {
                if (SetProperty(ref _projectNumber, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                    ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
                }
            }
        }

        private string _location;
        public string Location
        {
            get => _location;
            set
            {
                if (SetProperty(ref _location, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                    ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
                }
            }
        }

        private string _client;
        public string Client { get => _client; set => SetProperty(ref _client, value); }
        
        private string _contractor;
        public string Contractor { get => _contractor; set => SetProperty(ref _contractor, value); }
        
        private string _consultant;
        public string Consultant { get => _consultant; set => SetProperty(ref _consultant, value); }
        
        private string _designedBy;
        public string DesignedBy { get => _designedBy; set => SetProperty(ref _designedBy, value); }
        
        private string _checkedBy;
        public string CheckedBy { get => _checkedBy; set => SetProperty(ref _checkedBy, value); }

        public IRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public Action CloseAction { get; set; }
        public bool IsSaved { get; private set; }

        public ProjectInformationViewModel(ProjectData projectData = null)
        {
            _projectData = projectData ?? new ProjectData();
            
            ProjectName = _projectData.ProjectName;
            ProjectNumber = _projectData.ProjectNumber;
            Location = _projectData.Location;
            Client = _projectData.Client;
            Contractor = _projectData.Contractor;
            Consultant = _projectData.Consultant;
            DesignedBy = _projectData.DesignedBy;
            CheckedBy = _projectData.CheckedBy;

            SaveCommand = new RelayCommand(Save, () => CanSave);
            CancelCommand = new RelayCommand(Cancel);
            
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(CanSave) && e.PropertyName != nameof(Error))
                {
                    _hasUserInteracted = true;
                }
            };
        }

        public ProjectData GetUpdatedProjectData()
        {
            _projectData.ProjectName = ProjectName;
            _projectData.ProjectNumber = ProjectNumber;
            _projectData.Location = Location;
            _projectData.Client = Client;
            _projectData.Contractor = Contractor;
            _projectData.Consultant = Consultant;
            _projectData.DesignedBy = DesignedBy;
            _projectData.CheckedBy = CheckedBy;
            return _projectData;
        }

        public bool CanSave => 
            !string.IsNullOrWhiteSpace(ProjectName) && 
            !string.IsNullOrWhiteSpace(ProjectNumber) && 
            !string.IsNullOrWhiteSpace(Location);

        private void Save()
        {
            if (CanSave)
            {
                IsSaved = true;
                CloseAction?.Invoke();
            }
        }

        private void Cancel()
        {
            IsSaved = false;
            CloseAction?.Invoke();
        }

        // IDataErrorInfo implementation for validation
        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                if (!_hasUserInteracted && string.IsNullOrEmpty(_projectData.ProjectName)) return null;

                string result = null;
                switch (columnName)
                {
                    case nameof(ProjectName):
                        if (string.IsNullOrWhiteSpace(ProjectName))
                            result = "Project Name is mandatory.";
                        break;
                    case nameof(ProjectNumber):
                        if (string.IsNullOrWhiteSpace(ProjectNumber))
                            result = "Project Number is mandatory.";
                        break;
                    case nameof(Location):
                        if (string.IsNullOrWhiteSpace(Location))
                            result = "Location is mandatory.";
                        break;
                }
                return result;
            }
        }
    }
}
