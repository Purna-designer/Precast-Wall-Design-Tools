# Design Workspace Analysis & Gap Report

Based on the review of the VBA calculations summary, the new HTML UI prototype (`precast-wall-designer.html`), and the existing WPF implementation (`DesignWorkspaceView.xaml` / `DesignWorkspaceViewModel.cs`), there are significant gaps in the current desktop application.

The goal is to modernize the workflow from the original Excel file ("Precast Horizontal Connection - 10.02.2020.xlsm") by providing a live, interactive, and transparent design process rather than a "black-box" button-click approach.

Here is a detailed breakdown of what is missing or overlooked and how we should improve the design workflow.

---

## 1. Missing Entry Fields in UI and ViewModel

The current WPF application (`DesignWorkspaceView.xaml`) only exposes a handful of basic inputs: Width, Length, f'c, fy, and Dowel Dia. It completely misses the detailed reinforcement layout and geometric parameters required by the IS 456 interaction curve calculation.

**Missing Fields to Add:**
*   **Geometric Properties:**
    *   Effective Height ($H_{eff}$) [Currently missing in UI/VM]
    *   Effective Length Factor ($k_{eff}$) [Currently missing in UI/VM]
    *   Slenderness Class (auto-calculated/displayed)
*   **Reinforcement Details:**
    *   Clear Cover ($c_{clear}$)
    *   Shear Bar Diameter ($\phi_{shear}$)
    *   Main Bar Diameter ($\phi_{main}$) - *Currently simplified as "Dowel Dia"*
    *   Bars - Width face (Minor axis bars, $N_{minor}$)
    *   Bars - Depth face (Major axis bars, $N_{major}$)
    *   Percentage of Steel Provided ($p_t$) - *Needs to be an interactive slider/input.*

**ViewModel Updates Required:**
*   The `DesignWorkspaceViewModel` must contain bindable properties for all these fields.
*   These properties should notify the view and trigger a recalculation of the interaction curve whenever they change (Live feedback).

## 2. Load Combinations Data Grid

**Current State:** The WPF app uses a generic `DataGrid` bound to `Results`, which simply lists output after hitting "Run Calculations".
**Desired State:** As shown in the HTML mockup, we need a dedicated "Load Combinations" table that acts as the demand input for the column.

**Required Changes:**
*   Replace the generic `DataGrid` with a structured `ComboTable`.
*   Columns needed: `Combination Name`, `Pu (kN)`, `Mu2 (kNm)`, `Mu3 (kNm)`, `Final M2`, `Final M3`, `Status (SAFE/CHECK)`, and a `Review` flag (checkbox).
*   **Interactivity:** Clicking a row in this grid should set it as the "Active Demand Point" and update the interaction diagram live to show where this specific combination lies relative to the capacity envelope.

## 3. Design Pipeline Steps Visualization

**Current State:** There is no visibility into the design process. The user clicks "Run Calculations" and waits for a success/failure message.
**Desired State:** We need a visual tracker showing the current status of the design workflow.

**Required Changes:**
*   Implement a vertical stepper/pipeline UI element in the workspace.
*   Steps should include:
    1.  Fetch critical forces from ETABS export.
    2.  Confirm section geometry & reinforcement.
    3.  Generate major-axis (M3) interaction curve.
    4.  Generate minor-axis (M2) interaction curve.
    5.  Review interaction diagram & adjust Pt.
    6.  Confirm safety status & finalize Pt.
    7.  Add to design summary.

## 4. P-M Interaction Curve (Major & Minor Axis)

**Current State:** The WPF app has a placeholder `OxyPlot` with hardcoded dummy points (e.g., `(0, 1000), (100, 950)`). It lacks axis switching.
**Desired State:** A live, accurate interaction curve generator based on IS 456 strain compatibility.

**Required Changes:**
*   **Axis Toggle:** Add a toggle button group to switch between **Major Axis (M3)** and **Minor Axis (M2)**. The plot should redraw immediately when toggled.
*   **Plot Elements:**
    *   **Capacity Envelope:** The generated curve (Line).
    *   **Safe Zone:** A shaded polygon underneath the curve.
    *   **Demand Point:** A scatter point representing the active load combination (from the datagrid).
    *   **Balance Point:** Marked on the curve.
*   **Background Engine:** The `InteractionCurveGenerator` in the core engine must implement the strain compatibility sweep (varying neutral axis depth $x_u$ from $-0.01D$ to $10D$) to produce the proper envelope, matching the VBA calculations logic.

## 5. Final Steel Percentage ($p_t$) & Decision Panel

**Current State:** Completely missing.
**Desired State:** A dedicated panel at the bottom of the workspace to finalize the design.

**Required Changes:**
*   **Interactive Slider:** A slider to manually adjust the $p_t$ provided. Adjusting this slider should instantly scale the interaction capacity curve in the plot.
*   **Recommendation Engine:** The core should calculate and suggest a "Recommended $p_t$" to safely cover all load combinations.
*   **Finalization Panel:** Show the active $p_t$, governing interaction ratio, and final Status (SAFE/UNSAFE).
*   **Override Mechanism:** Provide a way for the engineer to override an unsafe status by adding an "Engineering Justification / Note" (e.g., if a specific wind combo is excluded by the project basis-of-design).

---

## Proposed Workflow Improvement (The "Columnar" Paradigm)

The old Excel workflow was procedural and opaque: 
1. Input data $\rightarrow$ 2. Click button to calculate $\rightarrow$ 3. Read static output.

**The improved workflow should be Reactive and State-Driven:**
1.  **Isolation:** Unlike the spreadsheet which has a single "current column" context, the WPF app should scope geometry, combinations, and curves to the specific column being viewed.
2.  **Live Binding:** As the engineer changes `Width`, `f'c`, or the `Pt Slider`, the Core Engine instantly recalculates the interaction points and updates the OxyPlot. There is no "Run" button for the curve.
3.  **Visual Demand/Capacity:** The engineer clicks through the load combinations table. For each click, the "Demand Point" jumps around on the interaction curve, instantly visualizing how close the member is to failure.
4.  **Traceability:** By replacing the silent "CHECK SAFETY" macro with a "Finalize" panel, we capture engineering intent (via Override notes) if a member is passed outside of standard parameters.
