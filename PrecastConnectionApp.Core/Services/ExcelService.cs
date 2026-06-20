using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using ExcelDataReader;
using PrecastConnectionApp.Models;
using PrecastConnectionApp.ViewModels;

namespace PrecastConnectionApp.Services
{
    public class ExcelService
    {
        public List<ForceItem> ExtractForces(string filePath)
        {
            var extractedForces = new List<ForceItem>();

            try
            {
                StatusNotifier.Instance.SetStatus($"Reading Excel file: {filePath}...");

                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = false
                            }
                        });

                        if (result.Tables.Count > 0)
                        {
                            var table = result.Tables[0];
                            
                            int headerRowIndex = -1;
                            List<string> originalColumns = new List<string>();

                            for (int i = 0; i < table.Rows.Count; i++)
                            {
                                var rowObj = table.Rows[i].ItemArray;
                                var stringValues = rowObj.Select(v => v?.ToString()?.Trim() ?? "").ToList();
                                
                                if (stringValues.Any(v => v.Equals("Story", StringComparison.OrdinalIgnoreCase)) &&
                                    stringValues.Any(v => v.Equals("Pier", StringComparison.OrdinalIgnoreCase)))
                                {
                                    headerRowIndex = i;
                                    originalColumns = stringValues;
                                    break;
                                }
                            }

                            if (headerRowIndex == -1)
                            {
                                StatusNotifier.Instance.SetStatus("Excel Error: Could not find header row containing 'Story' and 'Pier'.");
                                throw new Exception("Could not find header row containing 'Story' and 'Pier' columns. Please check your Excel file.");
                            }
                            
                            bool ColumnExists(string targetName)
                            {
                                var normalizedTarget = targetName.Replace(" ", "").Trim();
                                return originalColumns.Any(c => c.Replace(" ", "").Trim().Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase));
                            }

                            bool hasCaseType = ColumnExists("CaseType") || ColumnExists("StepType");
                            bool hasLocation = ColumnExists("Location");

                            bool forceBottom = false;
                            bool forceCombo = false;

                            if (!hasCaseType || !hasLocation)
                            {
                                string missing = "";
                                if (!hasCaseType) missing += "'CaseType' ";
                                if (!hasLocation) missing += "'Location' ";

                                var msgResult = MessageBox.Show(
                                    $"The following columns are missing from the Excel file: {missing}.\n\nAssume the data is already filtered for Combinations and Bottom locations?",
                                    "Missing Filter Columns",
                                    MessageBoxButton.OKCancel,
                                    MessageBoxImage.Warning);

                                if (msgResult == MessageBoxResult.Cancel)
                                {
                                    StatusNotifier.Instance.SetStatus("Excel import cancelled by user.");
                                    return null; // Return null to signal abort
                                }

                                if (!hasLocation) forceBottom = true;
                                if (!hasCaseType) forceCombo = true;
                            }

                            int mappedCount = 0;
                            for (int r = headerRowIndex + 1; r < table.Rows.Count; r++)
                            {
                                var row = table.Rows[r];

                                string GetVal(string colName)
                                {
                                    var normalizedTarget = colName.Replace(" ", "").Trim();
                                    for (int i = 0; i < originalColumns.Count; i++)
                                    {
                                        var normalizedSource = originalColumns[i].Replace(" ", "").Trim();
                                        if (string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (!row.IsNull(i)) return row[i].ToString();
                                            return "";
                                        }
                                    }
                                    return "";
                                }

                                double ParseVal(string colName)
                                {
                                    if (double.TryParse(GetVal(colName), out double val))
                                        return val;
                                    return 0;
                                }

                                string caseType = forceCombo ? "Combination" : GetVal("CaseType");
                                if (string.IsNullOrWhiteSpace(caseType)) caseType = GetVal("StepType");

                                string location = forceBottom ? "Bottom" : GetVal("Location");

                                bool isCombo = forceCombo || (!string.IsNullOrWhiteSpace(caseType) && 
                                               (caseType.IndexOf("Combination", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                caseType.IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0));

                                bool isBottom = forceBottom || (!string.IsNullOrWhiteSpace(location) && 
                                                location.IndexOf("Bottom", StringComparison.OrdinalIgnoreCase) >= 0);

                                string story = GetVal("Story");
                                string pier = GetVal("Pier");

                                // Skip empty rows (this automatically skips ETABS unit rows which have empty Story/Pier)
                                if (string.IsNullOrWhiteSpace(story) && string.IsNullOrWhiteSpace(pier)) continue;

                                if (isCombo && isBottom)
                                {
                                    extractedForces.Add(new ForceItem
                                    {
                                        Story = story,
                                        Pier = pier,
                                        OutputCase = GetVal("OutputCase"),
                                        Location = location,
                                        P = ParseVal("P"),
                                        V2 = ParseVal("V2"),
                                        V3 = ParseVal("V3"),
                                        T = ParseVal("T"),
                                        M2 = ParseVal("M2"),
                                        M3 = ParseVal("M3")
                                    });
                                    mappedCount++;
                                }
                            }
                            StatusNotifier.Instance.SetStatus($"Successfully mapped {mappedCount} force items from Excel.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusNotifier.Instance.SetStatus($"Excel Error: {ex.Message}");
                throw new Exception($"Failed to read Excel file: {ex.Message}");
            }

            return extractedForces;
        }
    }
}
