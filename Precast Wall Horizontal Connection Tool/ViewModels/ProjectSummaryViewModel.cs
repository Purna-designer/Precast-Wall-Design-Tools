using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Threading;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrecastConnectionApp.Models;
using PrecastConnectionApp.Services;

namespace PrecastConnectionApp.ViewModels
{
    public partial class ProjectSummaryViewModel : ObservableObject
    {
        private ProjectData _projectData;

        public ProjectData ProjectData
        {
            get => _projectData;
            set
            {
                _projectData = value;
                OnPropertyChanged(nameof(EtabsFilePath));
                OnPropertyChanged(nameof(Walls));
                ExtractCommand?.NotifyCanExecuteChanged();

                if (Walls != null)
                {
                    WallView = CollectionViewSource.GetDefaultView(Walls);
                    WallView.Filter = FilterPredicate;
                    OnPropertyChanged(nameof(WallView));
                    UpdateFilteredCount();
                }
            }
        }

        public string EtabsFilePath
        {
            get => _projectData?.EtabsFilePath;
            set
            {
                if (_projectData != null && _projectData.EtabsFilePath != value)
                {
                    _projectData.EtabsFilePath = value;
                    OnPropertyChanged();
                    ExtractCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<PrecastWall> Walls => _projectData?.Walls;

        public ICollectionView WallView { get; private set; }

        private int _filteredCount;
        public int FilteredCount
        {
            get => _filteredCount;
            set
            {
                if (SetProperty(ref _filteredCount, value))
                {
                    OnPropertyChanged(nameof(FilterCountText));
                }
            }
        }

        private int _safeCount;
        public int SafeCount { get => _safeCount; set => SetProperty(ref _safeCount, value); }

        private int _reviewCount;
        public int ReviewCount { get => _reviewCount; set => SetProperty(ref _reviewCount, value); }

        private int _unsafeCount;
        public int UnsafeCount { get => _unsafeCount; set => SetProperty(ref _unsafeCount, value); }

        private int _pendingCount;
        public int PendingCount { get => _pendingCount; set => SetProperty(ref _pendingCount, value); }

        public string FilterCountText
        {
            get
            {
                int total = Walls?.Count ?? 0;
                if (total == 0) return "0 members";
                if (FilteredCount == total) return $"{total} members";
                return $"{FilteredCount} / {total} members";
            }
        }

        public ObservableCollection<FilterGroup> FilterGroups { get; } = new ObservableCollection<FilterGroup>();

        private DispatcherTimer _searchDebounceTimer;

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _searchDebounceTimer?.Stop();
                    _searchDebounceTimer?.Start();
                }
            }
        }

        private bool _isFilterPopupOpen;
        public bool IsFilterPopupOpen
        {
            get => _isFilterPopupOpen;
            set => SetProperty(ref _isFilterPopupOpen, value);
        }

        private FilterGroup _selectedFilterGroup;
        public FilterGroup SelectedFilterGroup
        {
            get => _selectedFilterGroup;
            set => SetProperty(ref _selectedFilterGroup, value);
        }

        private bool _isImportExcel;
        public bool IsImportExcel
        {
            get => _isImportExcel;
            set => SetProperty(ref _isImportExcel, value);
        }

        private bool _isExtractEtabs = true;
        public bool IsExtractEtabs
        {
            get => _isExtractEtabs;
            set => SetProperty(ref _isExtractEtabs, value);
        }

        private bool _isRunAndExtract = true;
        public bool IsRunAndExtract
        {
            get => _isRunAndExtract;
            set 
            {
                if (SetProperty(ref _isRunAndExtract, value))
                    OnPropertyChanged(nameof(ExtractModeIndex));
            }
        }

        private bool _isExtractOnly;
        public bool IsExtractOnly
        {
            get => _isExtractOnly;
            set 
            {
                if (SetProperty(ref _isExtractOnly, value))
                    OnPropertyChanged(nameof(ExtractModeIndex));
            }
        }

        public int ExtractModeIndex
        {
            get => IsExtractOnly ? 1 : 0;
            set
            {
                if (value == 0)
                {
                    IsRunAndExtract = true;
                    IsExtractOnly = false;
                }
                else
                {
                    IsRunAndExtract = false;
                    IsExtractOnly = true;
                }
                OnPropertyChanged();
            }
        }

        public IRelayCommand ViewRawDataCommand { get; }
        public IRelayCommand<object> ViewDetailedDesignCommand { get; }
        public IRelayCommand BrowseCommand { get; }
        public IRelayCommand ExtractCommand { get; }
        public IRelayCommand ToggleFilterPopupCommand { get; }
        public IRelayCommand ClearFiltersCommand { get; }

        public ProjectSummaryViewModel(ProjectData projectData)
        {
            ProjectData = projectData;
            ViewRawDataCommand = new RelayCommand(ViewRawData);
            ViewDetailedDesignCommand = new RelayCommand<object>(ViewDetailedDesign);
            BrowseCommand = new RelayCommand(Browse);
            ExtractCommand = new RelayCommand(Extract, CanExecuteExtract);
            ToggleFilterPopupCommand = new RelayCommand(() => IsFilterPopupOpen = !IsFilterPopupOpen);
            ClearFiltersCommand = new RelayCommand(ClearFilters);

            _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                WallView?.Refresh();
                UpdateFilteredCount();
            };

            FilterGroups.Add(new FilterGroup("Wall Label", OnFilterChanged));
            FilterGroups.Add(new FilterGroup("Story", OnFilterChanged));
            FilterGroups.Add(new FilterGroup("Status", OnFilterChanged));
            FilterGroups.Add(new FilterGroup("PT Final", OnFilterChanged));
            
            SelectedFilterGroup = FilterGroups[0];

            if (Walls != null)
            {
                WallView = CollectionViewSource.GetDefaultView(Walls);
                WallView.Filter = FilterPredicate;
                OnPropertyChanged(nameof(WallView));
                UpdateFilterGroups();
            }
        }

        private void OnFilterChanged()
        {
            WallView?.Refresh();
            UpdateFilteredCount();
        }

        private void UpdateFilteredCount()
        {
            if (WallView == null) 
            {
                FilteredCount = 0;
            }
            else
            {
                int count = 0;
                foreach (var item in WallView) count++;
                FilteredCount = count;
            }
            
            if (Walls != null)
            {
                SafeCount = Walls.Count(w => w.Status == "SAFE");
                ReviewCount = Walls.Count(w => w.Status == "REVIEW");
                UnsafeCount = Walls.Count(w => w.Status == "UNSAFE");
                PendingCount = Walls.Count(w => w.Status == "PENDING");
            }
            else
            {
                SafeCount = 0;
                ReviewCount = 0;
                UnsafeCount = 0;
                PendingCount = 0;
            }
            
            OnPropertyChanged(nameof(FilterCountText));
        }

        private void ClearFilters()
        {
            foreach (var group in FilterGroups)
            {
                group.Clear();
            }
            SearchText = "";
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not PrecastWall item) return false;
            
            if (FilterGroups.Count < 4) return true;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.Trim();
                bool hit =
                    (item.Label?.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (item.Story?.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (item.Status?.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    item.FinalPt.ToString().IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!hit) return false;
            }

            if (!FilterGroups[0].IsAllowed(item.Label ?? "")) return false;
            if (!FilterGroups[1].IsAllowed(item.Story ?? "")) return false;
            if (!FilterGroups[2].IsAllowed(item.Status ?? "")) return false;
            if (!FilterGroups[3].IsAllowed(item.FinalPt.ToString())) return false;

            return true;
        }

        private void ViewRawData()
        {
            // Gather forces from all walls to show in a raw view
            var clonedForces = new List<ForceItem>();
            foreach (var wall in Walls)
            {
                foreach (var force in wall.LoadCombinations)
                {
                    clonedForces.Add(force.Clone());
                }
            }

            var vm = new ForcesPopupViewModel(this, clonedForces);
            var popup = new PrecastConnectionApp.Views.ForcesPopupView();
            popup.DataContext = vm;
            
            vm.RequestClose = (result) => 
            {
                popup.DialogResult = result;
                popup.Close();
            };

            popup.ShowDialog();
        }

        private void ViewDetailedDesign(object parameter)
        {
            if (parameter is PrecastWall wall)
            {
                if (System.Windows.Application.Current.MainWindow.DataContext is MainViewModel mvm)
                {
                    mvm.NavigateToPierCommand.Execute(wall);
                }
            }
        }

        private void Browse()
        {
            var dialog = new OpenFileDialog();
            
            if (IsImportExcel)
            {
                dialog.Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*";
                dialog.Title = "Select Excel File";
            }
            else
            {
                dialog.Filter = "ETABS Files (*.edb)|*.edb|All Files (*.*)|*.*";
                dialog.Title = "Select ETABS Model";
            }

            if (dialog.ShowDialog() == true)
            {
                EtabsFilePath = dialog.FileName;
            }
        }

        private bool CanExecuteExtract()
        {
            return !string.IsNullOrWhiteSpace(EtabsFilePath);
        }

        private void Extract()
        {
            try
            {
                List<ForceItem> extractedForces = null;

                if (IsImportExcel)
                {
                    StatusNotifier.Instance.SetStatus($"Extracting forces from Excel: {EtabsFilePath}...");
                    var excelService = new ExcelService();
                    extractedForces = excelService.ExtractForces(EtabsFilePath);
                    
                    if (extractedForces == null)
                    {
                        return;
                    }
                }
                else
                {
                    StatusNotifier.Instance.SetStatus($"Extracting forces from ETABS: {EtabsFilePath}...");
                    var etabsService = new EtabsService();
                    extractedForces = etabsService.ExtractForces(EtabsFilePath, IsExtractOnly);
                }
                
                if (extractedForces == null)
                {
                    return; 
                }
                
                SyncExtractedForcesWithWalls(extractedForces);
                StatusNotifier.Instance.SetStatus($"Successfully synchronized {extractedForces.Count} records with {Walls.Count} walls.");
            }
            catch(Exception ex)
            {
                StatusNotifier.Instance.SetStatus($"Failed to extract data: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to extract data: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void SyncExtractedForcesWithWalls(List<ForceItem> extractedForces)
        {
            if (Walls == null || extractedForces == null) return;

            var incomingGroups = extractedForces
                .Where(f => !string.IsNullOrWhiteSpace(f.Pier) && !string.IsNullOrWhiteSpace(f.Story))
                .GroupBy(f => new { Pier = f.Pier.Trim(), Story = f.Story.Trim() })
                .ToList();

            // Smart-Update logic
            foreach (var incomingGroup in incomingGroups)
            {
                var existingWall = Walls.FirstOrDefault(w => 
                    (w.Label?.Trim() ?? "") == incomingGroup.Key.Pier && 
                    (w.Story?.Trim() ?? "") == incomingGroup.Key.Story);

                if (existingWall == null)
                {
                    // No Match Found (Add)
                    var newWall = new PrecastWall 
                    { 
                        Label = incomingGroup.Key.Pier, 
                        Story = incomingGroup.Key.Story,
                        Status = "PENDING"
                    };
                    foreach(var force in incomingGroup)
                    {
                        newWall.LoadCombinations.Add(force);
                    }
                    Walls.Add(newWall);
                }
                else
                {
                    // Match Found (Update)
                    // Check if forces have materially changed
                    bool forcesChanged = false;
                    if (existingWall.LoadCombinations.Count != incomingGroup.Count())
                    {
                        forcesChanged = true;
                    }
                    else
                    {
                        var incomingList = incomingGroup.ToList();
                        for (int i = 0; i < existingWall.LoadCombinations.Count; i++)
                        {
                            var oldForce = existingWall.LoadCombinations[i];
                            var newForce = incomingList[i];

                            if (oldForce.P != newForce.P || oldForce.V2 != newForce.V2 || oldForce.V3 != newForce.V3 ||
                                oldForce.T != newForce.T || oldForce.M2 != newForce.M2 || oldForce.M3 != newForce.M3 ||
                                oldForce.OutputCase != newForce.OutputCase)
                            {
                                forcesChanged = true;
                                break;
                            }
                        }
                    }

                    if (forcesChanged)
                    {
                        existingWall.LoadCombinations.Clear();
                        foreach(var force in incomingGroup)
                        {
                            existingWall.LoadCombinations.Add(force);
                        }
                        // Flag it as REVIEW_REQUIRED since forces changed
                        existingWall.Status = "REVIEW_REQUIRED";
                    }
                }
            }

            // Flag orphans as DELETED_IN_ETABS
            foreach(var wall in Walls)
            {
                if (!incomingGroups.Any(g => g.Key.Pier == (wall.Label?.Trim() ?? "") && g.Key.Story == (wall.Story?.Trim() ?? "")))
                {
                    wall.Status = "DELETED_IN_ETABS";
                }
            }

            UpdateFilterGroups();
            UpdateFilteredCount();
        }

        private void UpdateFilterGroups()
        {
            if (Walls != null && Walls.Any())
            {
                FilterGroups[0].Populate(Walls.Select(p => p.Label ?? "").Distinct());
                FilterGroups[1].Populate(Walls.Select(p => p.Story ?? "").Distinct());
                FilterGroups[2].Populate(Walls.Select(p => p.Status ?? "").Distinct());
                FilterGroups[3].Populate(Walls.Select(p => p.FinalPt.ToString()).Distinct());
            }
        }
    }
}
