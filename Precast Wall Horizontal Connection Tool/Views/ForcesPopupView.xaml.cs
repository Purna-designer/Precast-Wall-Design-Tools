using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PrecastConnectionApp.Models;
using PrecastConnectionApp.ViewModels;

namespace PrecastConnectionApp.Views
{
    public partial class ForcesPopupView : Window
    {
        public ForcesPopupView()
        {
            InitializeComponent();
            ForcesGrid.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, CutCommand_Executed));
        }

        private void CutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ApplicationCommands.Copy.Execute(null, ForcesGrid);
            
            if (this.DataContext is ForcesPopupViewModel vm)
            {
                foreach (var cellInfo in ForcesGrid.SelectedCells)
                {
                    if (cellInfo.Item is ForceItem forceItem)
                    {
                        var prop = typeof(ForceItem).GetProperty(cellInfo.Column.SortMemberPath);
                        if (prop != null && prop.CanWrite)
                        {
                            if (prop.PropertyType == typeof(string)) prop.SetValue(forceItem, string.Empty);
                            else if (prop.PropertyType == typeof(double)) prop.SetValue(forceItem, 0.0);
                        }
                    }
                }
                ForcesGrid.Items.Refresh();
            }
        }

        private void PasteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!(this.DataContext is ForcesPopupViewModel vm)) return;

            string clipboardText = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(clipboardText)) return;

            string[] rows = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length == 0) return;

            int startRowIndex = 0;
            int startColIndex = 0;
            if (ForcesGrid.SelectedCells.Count > 0)
            {
                var firstCell = ForcesGrid.SelectedCells[0];
                startRowIndex = ForcesGrid.Items.IndexOf(firstCell.Item);
                startColIndex = ForcesGrid.Columns.IndexOf(firstCell.Column);
                if (startRowIndex < 0) startRowIndex = 0;
                if (startColIndex < 0) startColIndex = 0;
            }

            int maxColsPasted = rows.Max(r => r.Split('\t').Length);
            if (startColIndex + maxColsPasted > 10)
            {
                MessageBox.Show("Pasted data exceeds the available number of columns.", "Paste Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                int targetRowIndex = startRowIndex + i;

                while (targetRowIndex >= vm.Forces.Count)
                {
                    vm.Forces.Add(new ForceItem());
                }

                var targetItem = vm.Forces[targetRowIndex];
                string[] cells = rows[i].Split('\t');

                for (int j = 0; j < cells.Length; j++)
                {
                    int targetColIndex = startColIndex + j;
                    if (targetColIndex >= 10) break;

                    string cellValue = cells[j].Trim();
                    
                    switch (targetColIndex)
                    {
                        case 0: targetItem.Story = cellValue; break;
                        case 1: targetItem.Pier = cellValue; break;
                        case 2: targetItem.OutputCase = cellValue; break;
                        case 3: targetItem.Location = cellValue; break;
                        case 4: if (double.TryParse(cellValue, out double p)) targetItem.P = p; break;
                        case 5: if (double.TryParse(cellValue, out double v2)) targetItem.V2 = v2; break;
                        case 6: if (double.TryParse(cellValue, out double v3)) targetItem.V3 = v3; break;
                        case 7: if (double.TryParse(cellValue, out double t)) targetItem.T = t; break;
                        case 8: if (double.TryParse(cellValue, out double m2)) targetItem.M2 = m2; break;
                        case 9: if (double.TryParse(cellValue, out double m3)) targetItem.M3 = m3; break;
                    }
                }
            }
            ForcesGrid.Items.Refresh();
        }
    }
}
