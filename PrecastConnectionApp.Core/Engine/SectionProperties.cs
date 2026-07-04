using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrecastConnectionApp.Core.Engine
{
    public class SectionProperties : INotifyPropertyChanged
    {
        private double _width;
        public double Width { get => _width; set { _width = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAreaSteel)); OnPropertyChanged(nameof(MainBarDia)); OnPropertyChanged(nameof(EffectiveCover)); } }
        
        private double _depth;
        public double Depth { get => _depth; set { _depth = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAreaSteel)); OnPropertyChanged(nameof(MainBarDia)); OnPropertyChanged(nameof(EffectiveCover)); } }
        
        private double _height = 3000.0;
        public double Height { get => _height; set { _height = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveHeight)); } }
        
        private double _keff = 1.0;
        public double Keff { get => _keff; set { _keff = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveHeight)); } }

        public double EffectiveHeight => Height * Keff;
        
        private double _fck;
        public double Fck { get => _fck; set { _fck = value; OnPropertyChanged(); } }
        
        private double _fy;
        public double Fy { get => _fy; set { _fy = value; OnPropertyChanged(); } }
        
        private double _clearCover;
        public double ClearCover { get => _clearCover; set { _clearCover = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveCover)); } }
        
        
        private double _percentageSteel;
        public double PercentageSteel { get => _percentageSteel; set { _percentageSteel = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAreaSteel)); OnPropertyChanged(nameof(MainBarDia)); OnPropertyChanged(nameof(EffectiveCover)); } }
        
        private int _numMinorBars;
        public int NumMinorBars { get => _numMinorBars; set { _numMinorBars = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalBars)); OnPropertyChanged(nameof(MainBarDia)); OnPropertyChanged(nameof(EffectiveCover)); } }
        
        private int _numMajorBars;
        public int NumMajorBars { get => _numMajorBars; set { _numMajorBars = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalBars)); OnPropertyChanged(nameof(MainBarDia)); OnPropertyChanged(nameof(EffectiveCover)); } }
        
        public double AxialForce { get; set; }
        public double Moment3 { get; set; }
        public double Moment2 { get; set; }
        public double ExtEccentricityMajor { get; set; }
        public double ExtEccentricityMinor { get; set; }
        public double BaseOffset { get; set; } // Same as ExtEccentricityMinor for the minor axis base offset 

        public double EffectiveCover => ClearCover + (MainBarDia / 2.0);
        public int TotalBars => 2 * (NumMinorBars + NumMajorBars) - 4;
        public double TotalAreaSteel => (Width * Depth * PercentageSteel) / 100.0;
        public double MainBarDia => TotalBars > 0 ? Math.Sqrt((4.0 * TotalAreaSteel) / (Math.PI * TotalBars)) : 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
