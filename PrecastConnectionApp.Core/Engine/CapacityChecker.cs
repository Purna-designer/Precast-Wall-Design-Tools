using System;
using System.Linq;
using PrecastConnectionApp.Models;

namespace PrecastConnectionApp.Core.Engine
{
    public class CapacityResult
    {
        public bool IsSafe { get; set; }
        public double DemandP { get; set; }
        public double DemandM { get; set; }
        public double CapacityM { get; set; }
        public double Ratio => DemandM == 0 ? 0 : CapacityM / DemandM;
    }
    
    public static class CapacityChecker
    {
        public static CapacityResult CheckUniaxial(SectionProperties props, ForceItem force, bool isMajorAxis)
        {
            double b = isMajorAxis ? props.Width : props.Depth;
            double D = isMajorAxis ? props.Depth : props.Width;
            double demandP_N = Math.Abs(force.P) * 1000.0; 
            double demandM_Nmm = (isMajorAxis ? Math.Abs(force.M3) : Math.Abs(force.M2)) * 1e6;
            
            // Generate Curve
            var curveProps = new SectionProperties
            {
                Width = b,
                Depth = D,
                Fck = props.Fck,
                Fy = props.Fy,
                ClearCover = props.ClearCover,
                NumMinorBars = props.NumMinorBars,
                NumMajorBars = props.NumMajorBars,
                PercentageSteel = props.PercentageSteel
            };
            var curve = InteractionCurveGenerator.Generate(curveProps);

            // Compute Design Eccentricities
            double eAcc = props.EffectiveHeight / 500.0 + D / 30.0;
            if (eAcc < 20) eAcc = 20;

            double mMin_Nmm = demandP_N * eAcc;
            double ecc_mm = isMajorAxis ? props.ExtEccentricityMajor : props.ExtEccentricityMinor;
            double mU_Nmm = Math.Max(demandM_Nmm + demandP_N * ecc_mm, mMin_Nmm);
            
            var sortedCurve = curve.OrderBy(p => p.P).ToList();
            double capacityM_Nmm = 0;

            for (int i = 0; i < sortedCurve.Count - 1; i++)
            {
                if (demandP_N >= sortedCurve[i].P && demandP_N <= sortedCurve[i+1].P)
                {
                    double x0 = sortedCurve[i].P;
                    double x1 = sortedCurve[i+1].P;
                    double y0 = sortedCurve[i].M;
                    double y1 = sortedCurve[i+1].M;

                    double m = y0 + (y1 - y0) * (demandP_N - x0) / (x1 - x0);
                    if (m > capacityM_Nmm)
                    {
                        capacityM_Nmm = m;
                    }
                }
            }

            return new CapacityResult
            {
                IsSafe = capacityM_Nmm >= mU_Nmm,
                DemandP = demandP_N / 1000.0,
                DemandM = mU_Nmm / 1e6,
                CapacityM = capacityM_Nmm / 1e6
            };
        }

        public static void CheckBiaxial(SectionProperties props, ForceItem force)
        {
            double P_kN = Math.Abs(force.P);
            double Mu3_kNm = Math.Abs(force.M3);
            double Mu2_kNm = Math.Abs(force.M2);

            // Step 1: Min eccentricity (major axis)
            // L/500 + D/30 (in mm). D is depth for major axis.
            double eMinMajor = props.EffectiveHeight / 500.0 + props.Depth / 30.0;
            if (eMinMajor < 20.0) eMinMajor = 20.0;
            double minEccM3_kNm = P_kN * eMinMajor / 1000.0;

            // Step 2: Gov moment (major axis)
            double appliedM3_kNm = Mu3_kNm + P_kN * (props.ExtEccentricityMajor / 1000.0);
            double govM3_kNm = Math.Max(appliedM3_kNm, minEccM3_kNm);

            // Step 3: Min eccentricity (minor axis)
            double eMinMinorAcc = props.EffectiveHeight / 500.0 + props.Width / 30.0;
            if (eMinMinorAcc < 20.0) eMinMinorAcc = 20.0;
            double minEccM2_kNm = P_kN * (eMinMinorAcc / 1000.0);

            // Step 4: Gov moment (minor axis)
            // In the VBA, they do appliedM2 = Mu2 + Pu * ExtEccMinor. 
            double appliedM2_kNm = Mu2_kNm + P_kN * (props.ExtEccentricityMinor / 1000.0);
            double govM2_kNm = Math.Max(appliedM2_kNm, minEccM2_kNm);

            // Generate Major Curve to find CapM3
            var curvePropsMajor = new SectionProperties
            {
                Width = props.Width, Depth = props.Depth,
                Fck = props.Fck, Fy = props.Fy, ClearCover = props.ClearCover,
                NumMinorBars = props.NumMinorBars, NumMajorBars = props.NumMajorBars,
                PercentageSteel = props.PercentageSteel
            };
            var curveMajor = InteractionCurveGenerator.Generate(curvePropsMajor).OrderBy(p => p.P).ToList();
            double capM3_Nmm = 0;
            double demandP_N = P_kN * 1000.0;
            for (int i = 0; i < curveMajor.Count - 1; i++)
            {
                if (demandP_N >= curveMajor[i].P && demandP_N <= curveMajor[i+1].P)
                {
                    double m = curveMajor[i].M + (curveMajor[i+1].M - curveMajor[i].M) * (demandP_N - curveMajor[i].P) / (curveMajor[i+1].P - curveMajor[i].P);
                    if (m > capM3_Nmm) capM3_Nmm = m;
                }
            }
            double capM3_kNm = capM3_Nmm / 1e6;

            // Generate Minor Curve to find CapM2
            var curvePropsMinor = new SectionProperties
            {
                Width = props.Depth, Depth = props.Width, // swap for minor axis
                Fck = props.Fck, Fy = props.Fy, ClearCover = props.ClearCover,
                NumMinorBars = props.NumMinorBars, NumMajorBars = props.NumMajorBars,
                PercentageSteel = props.PercentageSteel
            };

            var curveMinor = InteractionCurveGenerator.Generate(curvePropsMinor).OrderBy(p => p.P).ToList();
            double capM2_Nmm = 0;
            for (int i = 0; i < curveMinor.Count - 1; i++)
            {
                if (demandP_N >= curveMinor[i].P && demandP_N <= curveMinor[i+1].P)
                {
                    double m = curveMinor[i].M + (curveMinor[i+1].M - curveMinor[i].M) * (demandP_N - curveMinor[i].P) / (curveMinor[i+1].P - curveMinor[i].P);
                    if (m > capM2_Nmm) capM2_Nmm = m;
                }
            }
            double capM2_kNm = capM2_Nmm / 1e6;

            // Step 5: Biaxial Ratio = (Mu2/M2_cap) + (Mu3/M3_cap)
            double ratioM3 = capM3_kNm > 0 ? govM3_kNm / capM3_kNm : 0;
            double ratioM2 = capM2_kNm > 0 ? govM2_kNm / capM2_kNm : 0;
            double biaxialRatio = ratioM2 + ratioM3;

            // Populate tracking data back to ForceItem
            force.MinEccM3 = minEccM3_kNm;
            force.GovM3 = govM3_kNm;
            force.MinEccM2 = minEccM2_kNm;
            force.GovM2 = govM2_kNm;
            force.BiaxialRatio = biaxialRatio;
            
            // Set conventional capacities back
            force.FinalM3 = capM3_kNm;
            force.FinalM2 = capM2_kNm;

            // The 'Ratio' is generally considered Capacity / Demand by this application's uniaxial display
            // but for Biaxial, we use BiaxialRatio (Demand/Capacity sum).
            // Let's set Ratio to BiaxialRatio for UI sorting, but it means lower is better!
            force.Ratio = biaxialRatio; 
            force.IsSafe = biaxialRatio <= 1.0;
        }
    }
}
