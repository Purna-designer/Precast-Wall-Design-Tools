# Precast Wall Horizontal Connection Tool — Architecture Document

**Version:** 1.0
**Author:** Possibuild Building Technologies
**Last Updated:** 2026-06-20
**Developed By:** Purna Medakayala - Application Engineer

---

## 1. Overview

### 1.1 Purpose
The Precast Wall Horizontal Connection Tool is a WPF desktop application designed to streamline the structural design of horizontal connections for precast walls. It is used by structural engineers to extract pier forces directly from ETABS, process them using predefined calculation engines, and generate comprehensive PDF design reports. 

### 1.2 Scope of This Document
This document describes the technical architecture of the application — how it's structured, how data flows through it, and the key decisions behind its design. It is intended for developers who will maintain or extend the codebase.

---

## 2. Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | WPF (.NET Framework 4.8) |
| MVVM Framework | CommunityToolkit.Mvvm (v8.4.2) |
| Language | C# 10.0 |
| Data Storage | JSON (Newtonsoft.Json) / Excel |
| Excel Processing | ExcelDataReader (v3.9.0) |
| Report Generation | QuestPDF (v2026.6.0) |
| Charting | OxyPlot.Wpf (v2.2.0) |
| API Integration | ETABSv1 COM API |
| Source Control | Git |

---

## 3. Architectural Pattern: MVVM

This application follows the **Model-View-ViewModel (MVVM)** pattern, implemented using the modern **CommunityToolkit.Mvvm** library.

```
┌─────────────────────────────────────────────┐
│                    View                     │
│    (XAML — .xaml files & Code-Behind)       │
│   Bindings, DataTemplates, UserControls     │
└───────────────────┬─────────────────────────┘
                    │ Data Binding / Commands
┌───────────────────▼─────────────────────────┐
│                 ViewModel                   │
│   Uses [ObservableProperty], [RelayCommand] │
│   Exposes properties & commands to View     │
└───────────────────┬─────────────────────────┘
                    │ Calls
┌───────────────────▼─────────────────────────┐
│                   Model / Services          │
│   Located in PrecastConnectionApp.Core      │
│   Business logic, ETABS interop, File I/O   │
└─────────────────────────────────────────────┘
```

### 3.1 Why MVVM & CommunityToolkit?
- **Separation of Concerns**: Decouples the WPF UI from the complex structural calculation and ETABS interaction logic, making the app maintainable.
- **CommunityToolkit.Mvvm**: Utilizes C# source generators to vastly reduce boilerplate code (e.g., automatically generating properties from fields via `[ObservableProperty]`).

### 3.2 Key Conventions
- Every View (`.xaml`) has a corresponding ViewModel (`XViewModel.cs`).
- The solution is strictly divided into a UI Project and a Core Library Project.
- Services are injected or instantiated in ViewModels to handle complex operations (like extracting from ETABS or building PDFs).

---

## 4. Project Structure

The solution consists of two primary projects to separate the UI from business logic:

```
Precast Wall Design Tools/
│
├── Precast Wall Horizontal Connection Tool/  # WPF UI Layer
│   ├── Views/                                # XAML screens, Windows & Popups
│   │   ├── DashboardView.xaml
│   │   ├── DesignWorkspaceView.xaml
│   │   ├── ProjectSummaryView.xaml
│   │   ├── ProjectInformationWindow.xaml
│   │   ├── ForcesPopupView.xaml
│   │   └── CommandLogWindow.xaml
│   │
│   ├── ViewModels/                           # One ViewModel per View
│   │   ├── MainViewModel.cs
│   │   ├── DashboardViewModel.cs
│   │   ├── DesignWorkspaceViewModel.cs
│   │   └── ...
│   │
│   └── App.xaml                              # Application Startup
│
└── PrecastConnectionApp.Core/                # Business Logic & Services Layer
    ├── Models/                               # Plain data classes & Entities
    │
    ├── Services/                             # Core services
    │   ├── EtabsService.cs                   # Interacts with ETABS COM API
    │   ├── ExcelService.cs                   # Reads/processes Excel data
    │   ├── CalculationEngine.cs              # Executes structural calculations
    │   ├── PdfReportService.cs               # Generates QuestPDF reports
    │   ├── JsonDataService.cs                # Handles JSON serialization/storage
    │   └── RecentProjectsService.cs          # Manages recent project history
    │
    └── Engine/                               # Additional calculation/logic engines
```

---

## 5. Core Modules / Screens

| Module / Screen | Responsibility | Key ViewModel |
|---|---|---|
| **Main Window** | Shell for the application, handles top-level navigation between modules. | `MainViewModel` |
| **Dashboard** | Start screen displaying recent projects and options to create/open projects. | `DashboardViewModel` |
| **Project Info** | Manages project metadata (client, project name, engineer). | `ProjectInformationViewModel` |
| **Design Workspace** | Core workspace. Users connect to ETABS, filter piers, view forces, and trigger designs. | `DesignWorkspaceViewModel` |
| **Forces Popup** | Detailed, popup view of forces for specific piers/walls. | `ForcesPopupViewModel` |
| **Project Summary** | Summary of all wall designs and entry point for generating the final PDF report. | `ProjectSummaryViewModel` |

---

## 6. Data Flow & Storage

### 6.1 Storage Mechanism
Data is primarily stored using **JSON** files for project metadata and application state, alongside **Excel** files which hold input templates and calculation logic parameters.

### 6.2 Data Flow Example: ETABS Extraction

```
User Clicks "Extract Forces" (DesignWorkspaceView)
      │
      ▼
RelayCommand in DesignWorkspaceViewModel
      │
      ▼
EtabsService.cs (PrecastConnectionApp.Core)
      │
      ▼
COM Interop call to active ETABS instance
      │
      ▼
ETABS returns Pier Forces → Mapped to Models
      │
      ▼
ViewModel ObservableCollections update → UI Updates
```

### 6.3 Key Services
- **`EtabsService`**: Manages connection to the running ETABS instance and queries pier forces/results.
- **`PdfReportService`**: Uses QuestPDF to dynamically draw text, tables, and charts into a formatted PDF document.
- **`CalculationEngine`**: Core structural logic bridging the data and the required calculations.

---

## 7. Key Design Decisions

| Decision | Reasoning | Alternatives Considered |
|---|---|---|
| **Two-Project Structure** | Separating `Core` from `WPF UI` ensures the business logic is entirely decoupled from Windows Presentation Foundation. This allows easier unit testing and future-proofs the application if migrating to MAUI or Blazor. | Monolithic WPF project (rejected due to tight coupling). |
| **QuestPDF for Reports** | QuestPDF provides a fluent, code-first API for generating complex PDFs extremely quickly without relying on external report viewers or interop. | iText7 / PDFsharp / Microsoft Report Viewer. |
| **ExcelDataReader over Interop** | Reading Excel files using `ExcelDataReader` is significantly faster and does not require Microsoft Excel to be installed on the machine running the app. | Excel COM Interop (rejected due to performance and dependency overhead). |

---

## 8. Dependencies (NuGet Packages)

| Package | Version | Purpose |
|---|---|---|
| CommunityToolkit.Mvvm | 8.4.2 | MVVM infrastructure, Source Generators |
| ExcelDataReader | 3.9.0 | Fast, zero-dependency Excel file reading |
| Newtonsoft.Json | 13.0.4 | JSON serialization for saving/loading projects |
| OxyPlot.Wpf | 2.2.0 | Generating structural charts and graphs in UI |
| QuestPDF | 2026.6.0 | Fluent API PDF report generation |

---

## 9. Error Handling & Logging

- **Command Logging**: The application features a `CommandLogWindow` and underlying logging mechanism to track operations, which aids in debugging structural extraction steps from ETABS.
- ViewModels wrap external calls (like ETABS COM calls or File I/O) in `try-catch` blocks, propagating user-friendly error messages to the UI.

---

## 10. Known Limitations / Technical Debt

- **ETABS Dependency**: `EtabsService` relies on the ETABS COM API (`ETABSv1.dll`). This requires ETABS 22 (or a compatible version) to be installed on the user's machine to run extractions.
- **.NET Framework 4.8**: The application currently targets .NET Framework 4.8, likely due to COM compatibility with ETABS. Future updates may consider migrating to .NET 6/8 if ETABS supports it seamlessly.

---

## Appendix: Glossary

| Term | Meaning |
|---|---|
| MVVM | Model-View-ViewModel — UI architectural pattern |
| ETABS | Structural software for building analysis and design |
| Pier Forces | Structural forces (axial, shear, moment) acting on wall segments (piers) |
| QuestPDF | Modern open-source .NET library for PDF generation |
