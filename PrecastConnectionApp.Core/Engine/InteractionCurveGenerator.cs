using System;
using System.Collections.Generic;
using System.Linq;

namespace PrecastConnectionApp.Core.Engine
{
    public struct PMPoint
    {
        public double P { get; set; } // Axial Force
        public double M { get; set; } // Moment
        public double NormalizedP { get; set; } // P / (fck b D)
        public double NormalizedM { get; set; } // M / (fck b D^2)
    }

    public static class InteractionCurveGenerator
    {
        public static List<PMPoint> Generate(SectionProperties props)
        {
            var layers = ReinforcementLayout.CalculateLayers(props);
            var points = new List<PMPoint>();

            double b = props.Width;
            double D = props.Depth;
            double fck = props.Fck;

            double initialValue = -0.05 * D;
            double finalValue = 10 * D;
            double stepValue = (finalValue - initialValue) / 199.0;

            for (int i = 1; i <= 199; i++)
            {
                double xu = initialValue + i * stepValue;
                if (Math.Abs(xu) < 1e-6) xu = 1e-6; // Avoid division by zero

                double a, xBar;
                if (xu <= D)
                {
                    a = 0.362 * (xu / D);
                    xBar = 0.416 * xu;
                }
                else
                {
                    double g = 16.0 / Math.Pow((7.0 * xu / D - 3.0), 2);
                    a = 0.447 * (1 - (4.0 * g / 21.0));
                    xBar = (0.5 - (8.0 * g / 49.0)) * D / (1 - (4.0 * g / 21.0));
                }

                double Cc = a * fck * b * D;
                double Mc = Cc * (D / 2.0 - xBar);

                double Cs = 0;
                double Ms = 0;

                foreach (var layer in layers)
                {
                    double strainSi;
                    if (xu <= D)
                    {
                        strainSi = 0.0035 * (xu - (D / 2.0 - layer.DistanceFromCentroid)) / xu;
                    }
                    else
                    {
                        strainSi = 0.002 * (1 + (layer.DistanceFromCentroid - D / 14.0) / (xu - 3.0 * D / 7.0));
                    }

                    double fci = 0;
                    if (strainSi >= 0.002)
                        fci = 0.447 * fck;
                    else if (strainSi > 0)
                        fci = 0.447 * fck * (2 * (strainSi / 0.002) - Math.Pow(strainSi / 0.002, 2));

                    double fsi = GetSteelStress(strainSi, props.Fy);

                    double layerForce = (fsi - fci) * layer.Area;
                    Cs += layerForce;
                    Ms += layerForce * layer.DistanceFromCentroid;
                }

                double Pu = Cc + Cs;
                double Mu = Mc + Ms;

                points.Add(new PMPoint
                {
                    P = Pu,
                    M = Mu,
                    NormalizedP = Pu / (fck * b * D),
                    NormalizedM = Mu / (fck * b * Math.Pow(D, 2))
                });
            }

            return points;
        }

        private static double GetSteelStress(double strain, double fy)
        {
            double sign = Math.Sign(strain);
            double absStrain = Math.Abs(strain);

            if (absStrain > 0.0035 && sign < 0) return -0.87 * fy;

            double[] strainNodes;
            double[] stressNodes;

            if (Math.Abs(fy - 500.0) < 1.0)
            {
                // Fe 500 nodes from exact table
                strainNodes = new double[] { 0.0, 0.00174, 0.00195, 0.00226, 0.00277, 0.00312, 0.00417, 0.1 };
                stressNodes = new double[] { 0.0, 347.8, 369.6, 391.3, 413.0, 423.9, 434.8, 434.8 };
            }
            else
            {
                // Fe 415 nodes from exact table (used as default/fallback)
                strainNodes = new double[] { 0.0, 0.00144, 0.00163, 0.00192, 0.00241, 0.00276, 0.00380, 0.1 };
                stressNodes = new double[] { 0.0, 288.7, 306.7, 324.8, 342.8, 351.8, 360.9, 360.9 };
            }

            double maxDesignStress = stressNodes[6]; // Max stress is at index 6

            for (int i = 0; i < strainNodes.Length - 1; i++)
            {
                if (absStrain >= strainNodes[i] && absStrain <= strainNodes[i + 1])
                {
                    double x0 = strainNodes[i];
                    double x1 = strainNodes[i + 1];
                    double y0 = stressNodes[i];
                    double y1 = stressNodes[i + 1];

                    if (x1 == x0) return sign * y0;

                    double stress = y0 + (y1 - y0) * (absStrain - x0) / (x1 - x0);
                    return sign * stress;
                }
            }

            return sign * maxDesignStress;
        }
    }
}
