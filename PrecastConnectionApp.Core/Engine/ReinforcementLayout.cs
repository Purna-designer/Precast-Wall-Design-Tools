using System;
using System.Collections.Generic;

namespace PrecastConnectionApp.Core.Engine
{
    public class RebarLayer
    {
        public double DistanceFromCentroid { get; set; } // y_i
        public double Area { get; set; } // A_si
    }

    public static class ReinforcementLayout
    {
        public static List<RebarLayer> CalculateLayers(SectionProperties props)
        {
            var layers = new List<RebarLayer>();
            double dPrime = props.EffectiveCover;
            double areaOneBar = Math.PI * Math.Pow(props.MainBarDia, 2) / 4.0;
            
            int nMajor = props.NumMajorBars;
            int nMinor = props.NumMinorBars;

            // Top Layer (1)
            layers.Add(new RebarLayer 
            { 
                DistanceFromCentroid = (props.Depth / 2.0) - dPrime, 
                Area = nMinor * areaOneBar 
            });

            // Intermediate Layers
            if (nMajor > 2)
            {
                double spacing = (props.Depth - 2 * dPrime) / (nMajor - 1);
                for (int i = 2; i <= nMajor - 1; i++)
                {
                    double y_i = (props.Depth / 2.0) - (dPrime + (i - 1) * spacing);
                    layers.Add(new RebarLayer
                    {
                        DistanceFromCentroid = y_i,
                        Area = 2 * areaOneBar
                    });
                }
            }

            // Bottom Layer (N_major)
            if (nMajor > 1)
            {
                layers.Add(new RebarLayer 
                { 
                    DistanceFromCentroid = -((props.Depth / 2.0) - dPrime), 
                    Area = nMinor * areaOneBar 
                });
            }

            return layers;
        }
    }
}
