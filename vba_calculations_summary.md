# Precast Horizontal Connection Design Calculations

This document details the mathematical models and step-by-step calculations extracted from the VBA macro code for the Precast Horizontal Connection/Column Design tool. The tool primarily generates the P-M (Axial-Moment) interaction curve based on IS 456 provisions and calculates design eccentricities and section capacities.

## 1. Required Inputs

### Geometric Properties
- **Column Width** ($b$) and **Depth** ($D$)
- **Effective Height** ($H_{eff}$ or $Height$)
- **Effective Length Factor** ($k_{eff}$)

### Material Properties
- **Concrete Compressive Strength** ($f_{ck}$)
- **Steel Yield Strength** ($f_{y}$)

### Reinforcement Details
- **Clear Cover** ($c_{clear}$)
- **Shear Bar Diameter** ($\phi_{shear}$)
- **Main Bar Diameter** ($\phi_{main}$)
- **Number of Bars**: $N_{minor}$ (bars on minor face) and $N_{major}$ (bars on major face)
- **Percentage of Steel Provided** ($p_t$)

### Design Forces & Eccentricities
- **Axial Force** ($P$)
- **External Bending Moments** ($M_3$, $M_2$)
- **External Eccentricities** ($e_{ext, major}$, $e_{ext, minor}$)

---

## 2. Preliminary Calculations

### 2.1 Effective Cover & Total Reinforcement
The effective cover ($d'$) to the centroid of the main reinforcement is:
$$ d' = c_{clear} + \phi_{shear} + \frac{\phi_{main}}{2} $$

The total number of bars ($N_{total}$) in the rectangular arrangement:
$$ N_{total} = 2(N_{minor} + N_{major}) - 4 $$

The corresponding total area of steel ($A_{st}$):
$$ A_{st} = N_{total} \times \frac{\pi \phi_{main}^2}{4} $$

### 2.2 Rebar Layer Distribution
For the purpose of calculating the interaction curve along the major axis, the section is discretized into $N_{major}$ layers along the depth.
- **Top Layer (1):** Distance from centroid $y_1 = \frac{D}{2} - d'$ with area $A_{s1} = N_{minor} \times \frac{\pi \phi_{main}^2}{4}$
- **Bottom Layer ($N_{major}$):** Distance from centroid $y_{N_{major}} = -\left(\frac{D}{2} - d'\right)$ with area $A_{s, N_{major}} = N_{minor} \times \frac{\pi \phi_{main}^2}{4}$
- **Intermediate Layers ($i$):** Distributed linearly between the top and bottom covers with 2 bars per layer ($A_{si} = 2 \times \frac{\pi \phi_{main}^2}{4}$). The distance from the centroid is:
  $$ y_i = \frac{D}{2} - \left( d' + \frac{(i - 1)(D - 2d')}{N_{major} - 1} \right) $$

---

## 3. Eccentricities and Design Moments

As per minimum eccentricity requirements, the accidental eccentricity is computed as:
$$ e_{acc, major} = \frac{H}{500} + \frac{D}{30} $$
$$ e_{acc, minor} = \frac{H}{500} + \frac{b}{30} $$

The overall design eccentricities are the sum of the external and accidental eccentricities:
$$ e_{major} = e_{ext, major} + e_{acc, major} $$
$$ e_{minor} = e_{ext, minor} + e_{acc, minor} $$

The minimum allowable design moments based on the axial force ($P$) are:
$$ M_{min, 3} = P \times e_{acc, major} $$
$$ M_{min, 2} = P \times e_{acc, minor} $$

The final factored design moments ($M_{u3}$, $M_{u2}$) evaluated for interaction are:
$$ M_{u3} = \max(M_{3, initial} + P \cdot e_{ext, major}, M_{min, 3}) $$
$$ M_{u2} = \max(M_{2, initial} + P \cdot e_{ext, minor}, M_{min, 2}) $$

---

## 4. P-M Interaction Curve Generation

The VBA script evaluates the moment and axial capacity by iterating the neutral axis depth ($x_u$) from $-0.01D$ to $10D$ in 199 increments. For each $x_u$, it computes the corresponding internal forces.

### 4.1 Concrete Contribution

Depending on the location of the neutral axis, the concrete stress block properties differ:

**Case A: Neutral Axis within the section ($x_u \le D$)**
- Stress block depth factor ($a$): $a = 0.362 \left(\frac{x_u}{D}\right)$
- Depth of centroid of compressive force ($\bar{x}$): $\bar{x} = 0.416 x_u$

**Case B: Neutral Axis outside the section ($x_u > D$)**
- Shape parameter ($g$): $g = \frac{16}{\left(7 \frac{x_u}{D} - 3\right)^2}$
- Stress block depth factor: $a = 0.447 \left(1 - \frac{4g}{21}\right)$
- Depth of centroid: $\bar{x} = \left(0.5 - \frac{8g}{49}\right) \frac{D}{1 - \frac{4g}{21}}$

**Internal Concrete Forces:**
- Total concrete compressive force ($C_c$):
  $$ C_c = a \cdot f_{ck} \cdot b \cdot D $$
- Moment contribution of concrete about section centroid ($M_c$):
  $$ M_c = C_c \left(\frac{D}{2} - \bar{x}\right) $$

### 4.2 Steel Contribution

For each reinforcement layer $i$ at distance $y_i$ from the centroid, the strain ($\varepsilon_{si}$) is calculated using strain compatibility:

- **If $x_u \le D$:**
  $$ \varepsilon_{si} = 0.0035 \frac{x_u - \left(\frac{D}{2} - y_i\right)}{x_u} $$
- **If $x_u > D$:**
  $$ \varepsilon_{si} = 0.002 \left[1 + \frac{y_i - \frac{D}{14}}{x_u - \frac{3D}{7}}\right] $$

**Concrete Stress at Steel Level ($f_{ci}$):**
To avoid double counting the compressive force where the steel displaces concrete, the concrete stress is subtracted:
- $\varepsilon_{si} \le 0$: $f_{ci} = 0$
- $\varepsilon_{si} \ge 0.002$: $f_{ci} = 0.447 f_{ck}$
- $0 < \varepsilon_{si} < 0.002$: $f_{ci} = 0.447 f_{ck} \left[ 2\left(\frac{\varepsilon_{si}}{0.002}\right) - \left(\frac{\varepsilon_{si}}{0.002}\right)^2 \right]$

**Steel Stress ($f_{si}$):**
Derived by interpolating the predefined design stress-strain curve for $f_y$:
- Curve nodes $(strain, stress)$: `(0, 0), (0.00174, 347.8), (0.00195, 369.6), (0.00226, 391.3), (0.00277, 413.0), (0.00312, 423.9), (0.00417, 434.8), (0.1, 434.8)`
- For extreme tension ($\varepsilon_{si} < -0.0035$): $f_{si} = -0.87 f_y$

**Layer Force and Moment:**
- Net force in layer $i$: $C_{si} = (f_{si} - f_{ci}) A_{si}$
- Moment of layer $i$ about centroid: $M_{si} = C_{si} \cdot y_i$

**Total Steel Force and Moment:**
- $C_s = \sum C_{si}$
- $M_s = \sum M_{si}$

### 4.3 Normalized Interaction Values
For each $x_u$ iteration, the dimensionless axial load and moment capacities are stored to form the P-M interaction curve:
- **Axial Ratio:** $\frac{P_u}{f_{ck} b D} = \frac{C_c + C_s}{f_{ck} b D}$
- **Moment Ratio:** $\frac{M_u}{f_{ck} b D^2} = \frac{M_c + M_s}{f_{ck} b D^2}$

---

## 5. Decision Making Logic / Output
1. The tool iteratively evaluates the capacities to find the full continuous curve.
2. Given a design Axial Force $P$, it interpolates the exact moment capacity ($M_{capacity}$) from the generated dimensionless curve.
3. The derived moment capacities are checked against the Total Design Moments ($M_{u3}$ and $M_{u2}$).
4. Based on the capacity checks, the macro asserts if the selected section and percentage of reinforcement ($p_t$) are `SAFE` or `UNSAFE`.
