using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrecastConnectionApp.Models;

namespace PrecastConnectionApp.ViewModels
{
    public class ForcesPopupViewModel : ObservableObject
    {
        public ObservableCollection<ForceItem> Forces { get; }
        public ICollectionView ForcesView { get; private set; }

        public ObservableCollection<FilterGroup> FilterGroups { get; } = new ObservableCollection<FilterGroup>();

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _searchDebounceTimer.Stop();
                    _searchDebounceTimer.Start();
                }
            }
        }

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

        public string FilterCountText
        {
            get
            {
                int total = Forces?.Count ?? 0;
                if (total == 0) return "0 members";
                if (FilteredCount == total) return $"{total} members";
                return $"{FilteredCount} / {total} members";
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        private FilterGroup _selectedFilterGroup;
        public FilterGroup SelectedFilterGroup
        {
            get => _selectedFilterGroup;
            set => SetProperty(ref _selectedFilterGroup, value);
        }

        private DispatcherTimer _searchDebounceTimer;
        private HashSet<ForceItem> _visibleItems;
        private bool _isFiltering;
        public bool IsFiltering
        {
            get => _isFiltering;
            set => SetProperty(ref _isFiltering, value);
        }

        private bool _isFilterPopupOpen;
        public bool IsFilterPopupOpen
        {
            get => _isFilterPopupOpen;
            set => SetProperty(ref _isFilterPopupOpen, value);
        }

        public ICommand ToggleFilterPopupCommand { get; }

        public Action<bool> RequestClose;
        private ProjectSummaryViewModel _parentViewModel;

        public ForcesPopupViewModel(ProjectSummaryViewModel parentViewModel, IEnumerable<ForceItem> clonedForces)
        {
            _parentViewModel = parentViewModel;
            Forces = new ObservableCollection<ForceItem>(clonedForces);

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ToggleFilterPopupCommand = new RelayCommand(() => IsFilterPopupOpen = !IsFilterPopupOpen);

            _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                _ = ApplyFilterAsync();
            };

            FilterGroups.Add(new FilterGroup("Story", OnFilterChanged));
            FilterGroups.Add(new FilterGroup("Pier", OnFilterChanged));
            FilterGroups.Add(new FilterGroup("Output Case", OnFilterChanged));
            FilterGroups.Add(new FilterGroup("Location", OnFilterChanged));

            if (FilterGroups.Count > 0) SelectedFilterGroup = FilterGroups[0];

            InitializeView();
        }

        private void InitializeView()
        {
            ForcesView = CollectionViewSource.GetDefaultView(Forces);
            ForcesView.Filter = FilterPredicate;
            UpdateFilterGroups();
            UpdateFilteredCount();
        }

        private void OnFilterChanged()
        {
            _ = ApplyFilterAsync();
        }

        private void ClearFilters()
        {
            foreach (var group in FilterGroups)
            {
                group.Clear();
            }
            SearchText = "";
        }

        private void UpdateFilterGroups()
        {
            if (Forces != null && Forces.Any())
            {
                FilterGroups[0].Populate(Forces.Select(p => p.Story ?? "").Distinct());
                FilterGroups[1].Populate(Forces.Select(p => p.Pier ?? "").Distinct());
                FilterGroups[2].Populate(Forces.Select(p => p.OutputCase ?? "").Distinct());
                FilterGroups[3].Populate(Forces.Select(p => p.Location ?? "").Distinct());
            }
        }

        private async Task ApplyFilterAsync()
        {
            IsFiltering = true;

            string search = SearchText?.Trim() ?? "";
            
            // Capture filter state on UI thread to pass to background thread
            var storyGroup = FilterGroups[0];
            var pierGroup = FilterGroups[1];
            var caseGroup = FilterGroups[2];
            var locGroup = FilterGroups[3];

            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            
            // Check if any filters are active. If not, we can skip the background processing.
            bool hasColumnFilters = false;
            foreach (var group in FilterGroups)
            {
                if (group.Items.Count > 0 && !group.Items[0].IsSelected)
                {
                    hasColumnFilters = true;
                    break;
                }
            }

            if (!hasSearch && !hasColumnFilters)
            {
                _visibleItems = null; // null means show everything
                ForcesView.Refresh();
                UpdateFilteredCount();
                IsFiltering = false;
                return;
            }

            // Snapshot the data to avoid CollectionChanged exceptions during iteration
            var allItems = Forces.ToList();
            
            // Build fast lookup sets for column filters
            var allowedStories = new HashSet<string>(storyGroup.Items.Skip(1).Where(i => i.IsSelected).Select(i => i.Name));
            bool filterStory = storyGroup.Items.Count > 0 && !storyGroup.Items[0].IsSelected;
            
            var allowedPiers = new HashSet<string>(pierGroup.Items.Skip(1).Where(i => i.IsSelected).Select(i => i.Name));
            bool filterPier = pierGroup.Items.Count > 0 && !pierGroup.Items[0].IsSelected;

            var allowedCases = new HashSet<string>(caseGroup.Items.Skip(1).Where(i => i.IsSelected).Select(i => i.Name));
            bool filterCase = caseGroup.Items.Count > 0 && !caseGroup.Items[0].IsSelected;

            var allowedLocs = new HashSet<string>(locGroup.Items.Skip(1).Where(i => i.IsSelected).Select(i => i.Name));
            bool filterLoc = locGroup.Items.Count > 0 && !locGroup.Items[0].IsSelected;

            // Run heavy string matching on background thread
            _visibleItems = await Task.Run(() =>
            {
                var visible = new HashSet<ForceItem>();
                foreach (var item in allItems)
                {
                    // 1. Column-specific filters (AND)
                    if (filterStory && !allowedStories.Contains(item.Story ?? "")) continue;
                    if (filterPier && !allowedPiers.Contains(item.Pier ?? "")) continue;
                    if (filterCase && !allowedCases.Contains(item.OutputCase ?? "")) continue;
                    if (filterLoc && !allowedLocs.Contains(item.Location ?? "")) continue;

                    // 2. Global search box (OR)
                    if (hasSearch)
                    {
                        bool hit = (item.Story?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                   (item.Pier?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                   (item.OutputCase?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                   (item.Location?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                   item.P.ToString().Contains(search) ||
                                   item.V2.ToString().Contains(search) ||
                                   item.V3.ToString().Contains(search) ||
                                   item.M2.ToString().Contains(search) ||
                                   item.M3.ToString().Contains(search) ||
                                   item.T.ToString().Contains(search);
                        if (!hit) continue;
                    }

                    visible.Add(item);
                }
                return visible;
            });

            // Back on UI thread
            ForcesView.Refresh();
            UpdateFilteredCount();
            IsFiltering = false;
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not ForceItem item) return false;
            
            // If _visibleItems is null, no filters are active
            if (_visibleItems == null) return true;
            
            // O(1) hashset lookup - lightning fast for WPF's synchronous rendering
            return _visibleItems.Contains(item);
        }

        private void UpdateFilteredCount()
        {
            if (ForcesView == null) 
            {
                FilteredCount = 0;
                return;
            }
            int count = 0;
            foreach (var item in ForcesView) count++;
            FilteredCount = count;
            OnPropertyChanged(nameof(FilterCountText));
        }

        private void Save()
        {
            RequestClose?.Invoke(true); // Return dialog result true
        }

        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }
    }
}
