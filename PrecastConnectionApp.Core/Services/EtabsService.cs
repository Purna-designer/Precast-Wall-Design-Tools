using System;
using System.Collections.Generic;
using PrecastConnectionApp.Models;
using PrecastConnectionApp.ViewModels;
using ETABSv1;

namespace PrecastConnectionApp.Services
{
    public class EtabsService
    {
        public List<ForceItem> ExtractForces(string filePath, bool isExtractOnly)
        {
            var extractedForces = new List<ForceItem>();
            cOAPI myETABSObject = null;

            try
            {
                StatusNotifier.Instance.SetStatus("Connecting to ETABS API...");
                
                cHelper myHelper = new Helper();
                bool attachedToActive = false;

                try 
                {
                    myETABSObject = myHelper.GetObject("CSI.ETABS.API.ETABSObject");
                    attachedToActive = (myETABSObject != null);
                }
                catch { }

                if (attachedToActive)
                {
                    StatusNotifier.Instance.SetStatus("Attached to active ETABS instance.");
                }
                else
                {
                    StatusNotifier.Instance.SetStatus("Starting new ETABS Application...");
                    try 
                    {
                        myETABSObject = myHelper.CreateObjectProgID("CSI.ETABS.API.ETABSObject");
                        myETABSObject.ApplicationStart();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Failed to start or connect to ETABS. Ensure ETABS is installed and both apps have matching Administrator privileges. Details: " + ex.Message);
                    }
                }

                if (myETABSObject == null)
                {
                     throw new Exception("ETABS API returned a null object. Connection failed due to COM/Bitness mismatch or permissions.");
                }

                cSapModel sapModel = myETABSObject.SapModel;
                
                string currentFile = "";
                try { currentFile = sapModel.GetModelFilename(); } catch { }

                int ret = 0;
                if (string.IsNullOrWhiteSpace(currentFile) || !currentFile.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    StatusNotifier.Instance.SetStatus("Initializing ETABS internal database...");
                    sapModel.InitializeNewModel();
                    
                    StatusNotifier.Instance.SetStatus($"Opening file: {filePath}...");
                    ret = sapModel.File.OpenFile(filePath);
                    if (ret != 0)
                    {
                        if (!attachedToActive) myETABSObject.ApplicationExit(false);
                        throw new Exception("ETABS failed to open the specified .EDB file. Ensure the file is not already open.");
                    }
                }
                else
                {
                    StatusNotifier.Instance.SetStatus("Target ETABS model is already active.");
                }

                if (isExtractOnly)
                {
                    StatusNotifier.Instance.SetStatus("Checking if model is locked...");
                    bool isLocked = sapModel.GetModelIsLocked();
                    if (!isLocked)
                    {
                        var msgResult = System.Windows.MessageBox.Show(
                            "The ETABS model is currently unlocked. Extracting forces requires running the analysis, which will overwrite existing results or take significant time.\n\nDo you want to run the analysis and extract data anyway?",
                            "Model Unlocked",
                            System.Windows.MessageBoxButton.OKCancel,
                            System.Windows.MessageBoxImage.Warning);
                        
                        if (msgResult == System.Windows.MessageBoxResult.Cancel)
                        {
                            if (!attachedToActive) myETABSObject.ApplicationExit(false);
                            StatusNotifier.Instance.SetStatus("Extraction cancelled by user.");
                            return null;
                        }
                        
                        StatusNotifier.Instance.SetStatus("Running analysis...");
                        sapModel.Analyze.RunAnalysis();
                    }
                    else
                    {
                        StatusNotifier.Instance.SetStatus("Model is locked. Proceeding with extraction...");
                    }
                }
                else
                {
                    StatusNotifier.Instance.SetStatus("Unlocking model and running fresh analysis...");
                    sapModel.SetModelIsLocked(false);
                    sapModel.Analyze.RunAnalysis();
                }
                
                StatusNotifier.Instance.SetStatus("Setting output units to kN-m-C...");
                sapModel.SetPresentUnits(eUnits.kN_m_C);

                StatusNotifier.Instance.SetStatus("Setting output options...");
                sapModel.Results.Setup.SetOptionMultiStepStatic(1);
                sapModel.Results.Setup.SetOptionNLStatic(1);
                
                StatusNotifier.Instance.SetStatus("Extracting 'Pier Forces' table...");
                
                string tableKey = "Pier Forces";
                string[] fieldKeyList = new string[0];
                string groupName = "";
                int tableVersion = 0;
                string[] fieldsKeysIncluded = new string[0];
                int numRecords = 0;
                string[] tableData = new string[0];

                ret = sapModel.DatabaseTables.GetTableForDisplayArray(
                    tableKey, 
                    ref fieldKeyList, 
                    groupName, 
                    ref tableVersion, 
                    ref fieldsKeysIncluded, 
                    ref numRecords, 
                    ref tableData);

                if (ret == 0 && numRecords > 0 && fieldsKeysIncluded != null && tableData != null)
                {
                    int numCols = fieldsKeysIncluded.Length;
                    var colIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    
                    for (int i = 0; i < numCols; i++)
                    {
                        colIndices[fieldsKeysIncluded[i].Trim()] = i;
                    }

                    for (int i = 0; i < numRecords; i++)
                    {
                        int rowStart = i * numCols;
                        
                        string GetVal(string colName)
                        {
                            if (colIndices.TryGetValue(colName, out int idx))
                                return tableData[rowStart + idx];
                            return "";
                        }

                        double ParseVal(string colName)
                        {
                            if (double.TryParse(GetVal(colName), out double val))
                                return val;
                            return 0;
                        }

                        string caseType = GetVal("CaseType");
                        if (string.IsNullOrWhiteSpace(caseType))
                            caseType = GetVal("StepType"); // Fallback

                        string location = GetVal("Location");

                        bool isCombo = !string.IsNullOrWhiteSpace(caseType) && 
                                       (caseType.IndexOf("Combination", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        caseType.IndexOf("Combo", StringComparison.OrdinalIgnoreCase) >= 0);

                        bool isBottom = !string.IsNullOrWhiteSpace(location) && 
                                        location.IndexOf("Bottom", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (isCombo && isBottom)
                        {
                            extractedForces.Add(new ForceItem
                            {
                                Story = GetVal("Story"),
                                Pier = GetVal("Pier"),
                                OutputCase = GetVal("OutputCase"),
                                Location = location,
                                P = ParseVal("P"),
                                V2 = ParseVal("V2"),
                                V3 = ParseVal("V3"),
                                T = ParseVal("T"),
                                M2 = ParseVal("M2"),
                                M3 = ParseVal("M3")
                            });
                        }
                    }
                    StatusNotifier.Instance.SetStatus($"Successfully mapped {extractedForces.Count} force items.");
                }
                else
                {
                    StatusNotifier.Instance.SetStatus("No data found in 'Pier Forces' table. Loading demo data...");
                    AddDemoData(extractedForces);
                }

                if (!attachedToActive)
                {
                    StatusNotifier.Instance.SetStatus("Closing ETABS...");
                    myETABSObject.ApplicationExit(false);
                }
                else
                {
                    StatusNotifier.Instance.SetStatus("ETABS left open as it was actively running before extraction.");
                }
            }
            catch (Exception ex)
            {
                StatusNotifier.Instance.SetStatus($"ETABS Error: {ex.Message}. Loading demo data...");
                AddDemoData(extractedForces);
                
                if (myETABSObject != null)
                {
                    try { myETABSObject.ApplicationExit(false); } catch { }
                }
                
                throw new Exception(ex.Message);
            }

            return extractedForces;
        }

        private void AddDemoData(List<ForceItem> extractedForces)
        {
            if (extractedForces.Count == 0)
            {
                extractedForces.Add(new ForceItem { Story = "Story 1", Pier = "P1", OutputCase = "COMB1", Location = "Bottom", P = 1500, V2 = 200, V3 = 300, T = 10, M2 = 400, M3 = 500 });
                extractedForces.Add(new ForceItem { Story = "Story 2", Pier = "P1", OutputCase = "COMB1", Location = "Bottom", P = 1200, V2 = 180, V3 = 250, T = 8, M2 = 350, M3 = 400 });
            }
        }
    }
}
