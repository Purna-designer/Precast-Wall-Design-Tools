using System.ComponentModel;
using System.Collections.ObjectModel;
using System;
using PrecastConnectionApp.Core.Engine;

namespace PrecastConnectionApp.Models
{
    public class ProjectData : INotifyPropertyChanged
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        private string _projectName;
        public string ProjectName
        {
            get => _projectName;
            set
            {
                _projectName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProjectName)));
            }
        }

        private string _projectNumber;
        public string ProjectNumber
        {
            get => _projectNumber;
            set
            {
                _projectNumber = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProjectNumber)));
            }
        }

        private string _location;
        public string Location
        {
            get => _location;
            set
            {
                _location = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location)));
            }
        }

        public string Client { get; set; }
        public string Contractor { get; set; }
        public string Consultant { get; set; }
        public string DesignedBy { get; set; }
        public string CheckedBy { get; set; }

        public string EtabsFilePath { get; set; }
        
        public ObservableCollection<PrecastWall> Walls { get; set; } = new ObservableCollection<PrecastWall>();
        
        public ObservableCollection<EtabsPoint> EtabsPoints { get; set; } = new ObservableCollection<EtabsPoint>();

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class PrecastWall : INotifyPropertyChanged
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        private string _label;
        public string Label
        {
            get => _label;
            set { _label = value; OnPropertyChanged(nameof(Label)); }
        }

        private string _story;
        public string Story
        {
            get => _story;
            set { _story = value; OnPropertyChanged(nameof(Story)); }
        }

        private SectionProperties _section = new SectionProperties();
        public SectionProperties Section
        {
            get => _section;
            set { _section = value; OnPropertyChanged(nameof(Section)); }
        }

        public ObservableCollection<ForceItem> LoadCombinations { get; set; } = new ObservableCollection<ForceItem>();

        private double _finalPt;
        public double FinalPt
        {
            get => _finalPt;
            set { _finalPt = value; OnPropertyChanged(nameof(FinalPt)); }
        }

        private string _status = "PENDING";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private string _remarks;
        public string Remarks
        {
            get => _remarks;
            set { _remarks = value; OnPropertyChanged(nameof(Remarks)); }
        }

        private string _overrideNote;
        public string OverrideNote
        {
            get => _overrideNote;
            set { _overrideNote = value; OnPropertyChanged(nameof(OverrideNote)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ForceItem : INotifyPropertyChanged
    {
        public string Story { get; set; }
        public string Pier { get; set; }
        public string OutputCase { get; set; }
        public string Location { get; set; }
        public double P { get; set; }
        public double V2 { get; set; }
        public double V3 { get; set; }
        public double T { get; set; }
        public double M2 { get; set; }
        public double M3 { get; set; }

        // Interaction Results for this combo
        private double _finalM2;
        public double FinalM2
        {
            get => _finalM2;
            set { _finalM2 = value; OnPropertyChanged(nameof(FinalM2)); }
        }

        private double _finalM3;
        public double FinalM3
        {
            get => _finalM3;
            set { _finalM3 = value; OnPropertyChanged(nameof(FinalM3)); }
        }

        private double _ratio;
        public double Ratio
        {
            get => _ratio;
            set { _ratio = value; OnPropertyChanged(nameof(Ratio)); }
        }

        private bool _isSafe;
        public bool IsSafe
        {
            get => _isSafe;
            set { _isSafe = value; OnPropertyChanged(nameof(IsSafe)); }
        }

        private bool _isGoverning;
        public bool IsGoverning
        {
            get => _isGoverning;
            set { _isGoverning = value; OnPropertyChanged(nameof(IsGoverning)); }
        }

        // Biaxial Tracking (Steps 1 to 5)
        private double _minEccM3;
        public double MinEccM3
        {
            get => _minEccM3;
            set { _minEccM3 = value; OnPropertyChanged(nameof(MinEccM3)); }
        }

        private double _govM3;
        public double GovM3
        {
            get => _govM3;
            set { _govM3 = value; OnPropertyChanged(nameof(GovM3)); }
        }

        private double _minEccM2;
        public double MinEccM2
        {
            get => _minEccM2;
            set { _minEccM2 = value; OnPropertyChanged(nameof(MinEccM2)); }
        }

        private double _govM2;
        public double GovM2
        {
            get => _govM2;
            set { _govM2 = value; OnPropertyChanged(nameof(GovM2)); }
        }

        private double _biaxialRatio;
        public double BiaxialRatio
        {
            get => _biaxialRatio;
            set { _biaxialRatio = value; OnPropertyChanged(nameof(BiaxialRatio)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ForceItem Clone()
        {
            return new ForceItem
            {
                Story = this.Story,
                Pier = this.Pier,
                OutputCase = this.OutputCase,
                Location = this.Location,
                P = this.P,
                V2 = this.V2,
                V3 = this.V3,
                T = this.T,
                M2 = this.M2,
                M3 = this.M3,
                FinalM2 = this.FinalM2,
                FinalM3 = this.FinalM3,
                Ratio = this.Ratio,
                IsSafe = this.IsSafe,
                IsGoverning = this.IsGoverning,
                MinEccM3 = this.MinEccM3,
                GovM3 = this.GovM3,
                MinEccM2 = this.MinEccM2,
                GovM2 = this.GovM2,
                BiaxialRatio = this.BiaxialRatio
            };
        }
    }

    public class EtabsPoint
    {
        public string Story { get; set; }
        public string ColumnLabel { get; set; }
        public double Pt { get; set; }
    }
}
