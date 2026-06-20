using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrecastConnectionApp.Models;
using PrecastConnectionApp.Core.Engine;
using PrecastConnectionApp.Services;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.Annotations;

namespace PrecastConnectionApp.ViewModels
{
    public class PipelineStep : ObservableObject
    {
        private string _title;
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        
        private string _desc;
        public string Desc { get => _desc; set => SetProperty(ref _desc, value); }
        
        private bool _isDone;
        public bool IsDone { get => _isDone; set => SetProperty(ref _isDone, value); }
        
        private bool _isActive;
        public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
    }

    public partial class DesignWorkspaceViewModel : ObservableObject
    {
        private ProjectData _projectData;

        public ProjectData ProjectData
        {
            get => _projectData;
            set
            {
                _projectData = value;
                OnPropertyChanged(nameof(CurrentWall));
            }
        }

        private string _selectedPierLabel;
        public string SelectedPierLabel
        {
            get => _selectedPierLabel;
            set
            {
                if (SetProperty(ref _selectedPierLabel, value))
                {
                    UpdateCurrentWall();
                }
            }
        }

        private string _selectedStory;
        public string SelectedStory
        {
            get => _selectedStory;
            set
            {
                if (SetProperty(ref _selectedStory, value))
                {
                    UpdateCurrentWall();
                }
            }
        }

        private PrecastWall _currentWall;
        public PrecastWall CurrentWall
        {
            get => _currentWall;
            set
            {
                if (_currentWall != null && _currentWall.Section != null)
                {
                    _currentWall.Section.PropertyChanged -= Section_PropertyChanged;
                }
                
                if (SetProperty(ref _currentWall, value))
                {
                    if (_currentWall != null && _currentWall.Section != null)
                    {
                        _currentWall.Section.PropertyChanged += Section_PropertyChanged;
                        
                        LoadCombinationsView = System.Windows.Data.CollectionViewSource.GetDefaultView(_currentWall.LoadCombinations);
                        LoadCombinationsView.Filter = FilterCombinations;
                    }
                    Recalculate();
                }
            }
        }
        
        private System.ComponentModel.ICollectionView _loadCombinationsView;
        public System.ComponentModel.ICollectionView LoadCombinationsView
        {
            get => _loadCombinationsView;
            set => SetProperty(ref _loadCombinationsView, value);
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    LoadCombinationsView?.Refresh();
                }
            }
        }

        private bool FilterCombinations(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (obj is ForceItem force)
            {
                return force.OutputCase?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return false;
        }

        private void Section_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Removed automatic Recalculate() to prevent lag; user clicks Generate button instead.
        }

        private void UpdateCurrentWall()
        {
            if (ProjectData?.Walls != null && !string.IsNullOrEmpty(SelectedPierLabel) && !string.IsNullOrEmpty(SelectedStory))
            {
                CurrentWall = ProjectData.Walls.FirstOrDefault(w => w.Label == SelectedPierLabel && w.Story == SelectedStory);
            }
            else
            {
                CurrentWall = null;
            }
        }

        public IRelayCommand RunCommand { get; }
        public IRelayCommand ExportPdfCommand { get; }
        public IRelayCommand BackCommand { get; }
        public IRelayCommand ConfirmDesignCommand { get; }
        public IRelayCommand RecommendPtCommand { get; }
        public IRelayCommand ToggleOverrideCommand { get; }
        public IRelayCommand CancelOverrideCommand { get; }
        public IRelayCommand ApplyOverrideCommand { get; }

        private bool _showOverridePanel;
        public bool ShowOverridePanel
        {
            get => _showOverridePanel;
            set => SetProperty(ref _showOverridePanel, value);
        }

        private string _overrideJustification;
        public string OverrideJustification
        {
            get => _overrideJustification;
            set => SetProperty(ref _overrideJustification, value);
        }

        private bool _isOverrideInputVisible;
        public bool IsOverrideInputVisible
        {
            get => _isOverrideInputVisible;
            set => SetProperty(ref _isOverrideInputVisible, value);
        }

        private PlotModel _plotModel;
        public PlotModel PlotModel
        {
            get => _plotModel;
            set => SetProperty(ref _plotModel, value);
        }
        
        private ForceItem _selectedLoadCombination;
        public ForceItem SelectedLoadCombination
        {
            get => _selectedLoadCombination;
            set 
            {
                if(SetProperty(ref _selectedLoadCombination, value))
                {
                    UpdateDemandPoint();
                }
            }
        }

        private bool _isMajorAxis = true;
        public bool IsMajorAxis
        {
            get => _isMajorAxis;
            set
            {
                if (SetProperty(ref _isMajorAxis, value))
                {
                    Recalculate();
                }
            }
        }

        public ObservableCollection<PipelineStep> DesignPipeline { get; } = new ObservableCollection<PipelineStep>();

        public DesignWorkspaceViewModel(ProjectData projectData)
        {
            ProjectData = projectData;
            RunCommand = new RelayCommand(Recalculate);
            ExportPdfCommand = new RelayCommand(ExportPdf);
            BackCommand = new RelayCommand(Back);
            ConfirmDesignCommand = new RelayCommand(ConfirmDesign);
            RecommendPtCommand = new RelayCommand(RecommendPt);
            ToggleOverrideCommand = new RelayCommand(ToggleOverride);
            CancelOverrideCommand = new RelayCommand(CancelOverride);
            ApplyOverrideCommand = new RelayCommand(ApplyOverride);

            InitializePipeline();
            InitializePlot();
        }

        private void InitializePipeline()
        {
            DesignPipeline.Add(new PipelineStep { Title = "Fetch critical forces from ETABS export", Desc = "Pick worst-case combinations across two end sections + span", IsDone = false });
            DesignPipeline.Add(new PipelineStep { Title = "Confirm section geometry & reinforcement", Desc = "Width, depth, cover, bar layout, grades", IsDone = false });
            DesignPipeline.Add(new PipelineStep { Title = "Generate major-axis (M3) interaction curve", Desc = "Strain-compatibility sweep over neutral axis depth", IsDone = false });
            DesignPipeline.Add(new PipelineStep { Title = "Generate minor-axis (M2) interaction curve", Desc = "Repeat sweep for minor axis bending", IsDone = false });
            DesignPipeline.Add(new PipelineStep { Title = "Review interaction diagram & adjust Pt", Desc = "Engineer checks demand points against capacity envelope", IsDone = false });
            DesignPipeline.Add(new PipelineStep { Title = "Confirm safety status & finalize Pt", Desc = "Accept recommended Pt or override with justification", IsDone = false });
            DesignPipeline.Add(new PipelineStep { Title = "Add to design summary", Desc = "Push finalized result to project envelope", IsDone = false });
        }

        private void InitializePlot()
        {
            var model = new PlotModel { Title = "PM Interaction Curve" };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Moment (M)" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Axial Load (P)" });
            PlotModel = model;
        }

        private async void Recalculate()
        {
            if (CurrentWall == null || CurrentWall.Section == null)
            {
                return;
            }

            // Reset pipeline steps dynamically
            foreach (var step in DesignPipeline)
            {
                step.IsDone = false;
                step.IsActive = false;
            }

            // Animate pipeline steps
            if (DesignPipeline.Count > 0) DesignPipeline[0].IsActive = true;
            await Task.Delay(250);
            if (DesignPipeline.Count > 0) { DesignPipeline[0].IsActive = false; DesignPipeline[0].IsDone = true; }

            if (DesignPipeline.Count > 1) DesignPipeline[1].IsActive = true;
            await Task.Delay(250);
            if (DesignPipeline.Count > 1) { DesignPipeline[1].IsActive = false; DesignPipeline[1].IsDone = true; }

            if (DesignPipeline.Count > 2) DesignPipeline[2].IsActive = true;
            await Task.Delay(350); // Simulating heavier calculation for curve
            if (DesignPipeline.Count > 2) { DesignPipeline[2].IsActive = false; DesignPipeline[2].IsDone = true; }

            if (DesignPipeline.Count > 3) DesignPipeline[3].IsActive = true;
            await Task.Delay(250);
            if (DesignPipeline.Count > 3) { DesignPipeline[3].IsActive = false; DesignPipeline[3].IsDone = true; }

            if (DesignPipeline.Count > 4) DesignPipeline[4].IsActive = true;

            var model = new PlotModel { Title = IsMajorAxis ? "PM Interaction Curve (Major Axis)" : "PM Interaction Curve (Minor Axis)" };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Moment (kNm)" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Axial Load (kN)" });

            var curveProps = new SectionProperties
            {
                Width = IsMajorAxis ? CurrentWall.Section.Width : CurrentWall.Section.Depth,
                Depth = IsMajorAxis ? CurrentWall.Section.Depth : CurrentWall.Section.Width,
                Fck = CurrentWall.Section.Fck,
                Fy = CurrentWall.Section.Fy,
                ClearCover = CurrentWall.Section.ClearCover,
                NumMinorBars = CurrentWall.Section.NumMinorBars,
                NumMajorBars = CurrentWall.Section.NumMajorBars,
                MainBarDia = CurrentWall.Section.MainBarDia,
                ShearBarDia = CurrentWall.Section.ShearBarDia
            };
            var points = InteractionCurveGenerator.Generate(curveProps);

            if (points != null && points.Count > 0)
            {
                var series = new LineSeries
                {
                    Title = "Capacity",
                    Color = OxyColors.DarkSlateBlue,
                    StrokeThickness = 2,
                };

                foreach (var pt in points)
                {
                    series.Points.Add(new DataPoint(pt.M / 1e6, pt.P / 1000.0));
                }

                model.Series.Add(series);
            }

            // Demand Points
            if (CurrentWall.LoadCombinations != null)
            {
                var demandSeries = new ScatterSeries
                {
                    Title = "Demands",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 4,
                    MarkerFill = OxyColors.DarkRed
                };

                foreach (var combo in CurrentWall.LoadCombinations)
                {
                    double p = Math.Abs(combo.P); // P is compression
                    
                    // Run biaxial capacity check to flag Safe/Unsafe and compute governing moments
                    if (points != null && points.Count > 0)
                    {
                        CapacityChecker.CheckBiaxial(CurrentWall.Section, combo);
                    }

                    // Plot the governed moment instead of raw ETABS moment to reflect the IS 456 eccentricities
                    double m = IsMajorAxis ? combo.GovM3 : combo.GovM2;
                    demandSeries.Points.Add(new ScatterPoint(Math.Abs(m), p));
                }
                model.Series.Add(demandSeries);
            }

            PlotModel = model;
            UpdateDemandPoint();
        }

        private void UpdateDemandPoint()
        {
            if (PlotModel == null) return;

            // Remove existing highlight
            var existingHighlight = PlotModel.Series.FirstOrDefault(s => s.Title == "Selected Demand");
            if (existingHighlight != null)
            {
                PlotModel.Series.Remove(existingHighlight);
            }

            if (SelectedLoadCombination != null)
            {
                var highlightSeries = new ScatterSeries
                {
                    Title = "Selected Demand",
                    MarkerType = MarkerType.Cross,
                    MarkerSize = 8,
                    MarkerStroke = OxyColors.Blue,
                    MarkerStrokeThickness = 2
                };
                double m = IsMajorAxis ? SelectedLoadCombination.GovM3 : SelectedLoadCombination.GovM2;
                double p = Math.Abs(SelectedLoadCombination.P);
                highlightSeries.Points.Add(new ScatterPoint(Math.Abs(m), p));
                PlotModel.Series.Add(highlightSeries);
            }

            PlotModel.InvalidatePlot(true);
        }

        private void ConfirmDesign()
        {
            if (CurrentWall != null)
            {
                bool allSafe = CurrentWall.LoadCombinations.All(c => c.IsSafe);
                if (allSafe)
                {
                    CurrentWall.Status = "SAFE";
                    if (DesignPipeline.Count > 5) { DesignPipeline[5].IsDone = true; DesignPipeline[5].IsActive = false; }
                    if (DesignPipeline.Count > 6) { DesignPipeline[6].IsDone = true; DesignPipeline[6].IsActive = false; }
                    StatusNotifier.Instance.SetStatus($"Design confirmed. Status updated to SAFE.");
                    Back();
                }
                else
                {
                    ShowOverridePanel = true;
                }
            }
        }

        private void ToggleOverride()
        {
            IsOverrideInputVisible = !IsOverrideInputVisible;
        }

        private void CancelOverride()
        {
            IsOverrideInputVisible = false;
            OverrideJustification = string.Empty;
        }

        private void ApplyOverride()
        {
            if (CurrentWall != null && !string.IsNullOrWhiteSpace(OverrideJustification))
            {
                CurrentWall.Status = "REVIEW"; // Overridden designs should probably be marked for review
                CurrentWall.Remarks = OverrideJustification;
                if (DesignPipeline.Count > 5) { DesignPipeline[5].IsDone = true; DesignPipeline[5].IsActive = false; }
                if (DesignPipeline.Count > 6) { DesignPipeline[6].IsDone = true; DesignPipeline[6].IsActive = false; }
                StatusNotifier.Instance.SetStatus("Design overridden with justification. Status updated to REVIEW.");
                IsOverrideInputVisible = false;
                ShowOverridePanel = false;
                Back();
            }
            else
            {
                StatusNotifier.Instance.SetStatus("Justification is required to apply override.");
            }
        }

        private void RecommendPt()
        {
            if (CurrentWall == null || CurrentWall.LoadCombinations.Count == 0) return;
            
            // Simple heuristic for demonstration: find max ratio and scale Pt
            double maxRatio = CurrentWall.LoadCombinations.Max(c => c.Ratio);
            double newPt = CurrentWall.FinalPt * maxRatio * 1.05; // Add 5% safety margin
            
            // Clamp between bounds
            if (newPt < 0.8) newPt = 0.8;
            if (newPt > 4.0) newPt = 4.0;
            
            CurrentWall.FinalPt = newPt;
            Recalculate();
            StatusNotifier.Instance.SetStatus($"Recommended Pt% applied: {newPt:F2}%");
        }

        private void Back()
        {
            if (System.Windows.Application.Current.MainWindow.DataContext is MainViewModel mvm)
            {
                mvm.NavigateCommand.Execute("ProjectSummary");
            }
        }

        private void ExportPdf()
        {
            // PDF Export implementation
        }
    }
}
