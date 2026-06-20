using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PrecastConnectionApp.ViewModels
{
    public partial class FilterItem : ObservableObject
    {
        public Action<FilterItem> OnSelectionChanged { get; set; }

        public FilterItem(string name, Action<FilterItem> onSelectionChanged = null)
        {
            Name = name;
            OnSelectionChanged = onSelectionChanged;
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    OnIsSelectedChanged(value);
                }
            }
        }

        private void OnIsSelectedChanged(bool value)
        {
            OnSelectionChanged?.Invoke(this);
        }
    }

    public partial class FilterGroup : ObservableObject
    {
        public Action OnFilterChanged { get; set; }

        private string _columnName;
        public string ColumnName
        {
            get => _columnName;
            set => SetProperty(ref _columnName, value);
        }

        public ObservableCollection<FilterItem> Items { get; } = new ObservableCollection<FilterItem>();

        private bool _isUpdatingInternally;

        public FilterGroup(string columnName, Action onFilterChanged)
        {
            ColumnName = columnName;
            OnFilterChanged = onFilterChanged;
        }

        public void Populate(System.Collections.Generic.IEnumerable<string> values)
        {
            _isUpdatingInternally = true;

            // Detach callbacks from old items so UI virtualization tearing them down doesn't crash us
            foreach(var item in Items)
            {
                item.OnSelectionChanged = null;
            }
            
            Items.Clear();
            
            // Add Select All item
            var selectAllItem = new FilterItem("(Select All)", HandleItemSelectionChanged);
            Items.Add(selectAllItem);

            foreach (var val in values.OrderBy(v => v))
            {
                Items.Add(new FilterItem(val, HandleItemSelectionChanged));
            }
            _isUpdatingInternally = false;
        }

        public void Clear()
        {
            if (Items.Count == 0) return;

            _isUpdatingInternally = true;
            foreach (var item in Items)
            {
                item.IsSelected = true;
            }
            _isUpdatingInternally = false;
            
            OnFilterChanged?.Invoke();
        }

        private void HandleItemSelectionChanged(FilterItem sender)
        {
            if (_isUpdatingInternally || Items.Count == 0) return;

            _isUpdatingInternally = true;

            var selectAllItem = Items[0];
            int totalOther = Items.Count - 1;
            int checkedOther = Items.Skip(1).Count(i => i.IsSelected);

            if (sender == selectAllItem)
            {
                // User clicked (Select All)
                bool state = selectAllItem.IsSelected;
                foreach(var item in Items.Skip(1))
                {
                    item.IsSelected = state;
                }
            }
            else
            {
                // User clicked an individual item
                if (checkedOther == totalOther)
                {
                    selectAllItem.IsSelected = true;
                }
                else
                {
                    selectAllItem.IsSelected = false;
                }
            }

            _isUpdatingInternally = false;

            OnFilterChanged?.Invoke();
        }
        
        public bool IsAllowed(string value)
        {
            if (Items.Count == 0 || Items[0].IsSelected) return true;
            return Items.Skip(1).Any(i => i.IsSelected && i.Name == value);
        }
    }
}
