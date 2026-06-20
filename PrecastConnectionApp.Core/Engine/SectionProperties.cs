using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrecastConnectionApp.Core.Engine
{
    public class SectionProperties : INotifyPropertyChanged
    {
        private double _width;
        public double Width { get => _width; set { _width = value; OnPropertyChanged(); OnPropertyChanged(nameof(PercentageSteel)); } }
        
        private double _depth;
        public double Depth { get => _depth; set { _depth = value; OnPropertyChanged(); OnPropertyChanged(nameof(PercentageSteel)); } }
        
        private double _effectiveHeight;
        public double EffectiveHeight { get => _effectiveHeight; set { _effectiveHeight = value; OnPropertyChanged(); } }
        
        private double _fck;
        public double Fck { get => _fck; set { _fck = value; OnPropertyChanged(); } }
        
        private double _fy;
        public double Fy { get => _fy; set { _fy = value; OnPropertyChanged(); } }
        
        private double _clearCover;
        public double ClearCover { get => _clearCover; set { _clearCover = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveCover)); } }
        
        private double _shearBarDia;
        public double ShearBarDia { get => _shearBarDia; set { _shearBarDia = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveCover)); } }
        
        private double _mainBarDia;
        public double MainBarDia { get => _mainBarDia; set { _mainBarDia = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveCover)); OnPropertyChanged(nameof(TotalAreaSteel)); OnPropertyChanged(nameof(PercentageSteel)); } }
        
        private int _numMinorBars;
        public int NumMinorBars { get => _numMinorBars; set { _numMinorBars = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalBars)); OnPropertyChanged(nameof(TotalAreaSteel)); OnPropertyChanged(nameof(PercentageSteel)); } }
        
        private int _numMajorBars;
        public int NumMajorBars { get => _numMajorBars; set { _numMajorBars = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalBars)); OnPropertyChanged(nameof(TotalAreaSteel)); OnPropertyChanged(nameof(PercentageSteel)); } }
        
        public double AxialForce { get; set; }
        public double Moment3 { get; set; }
        public double Moment2 { get; set; }
        public double ExtEccentricityMajor { get; set; }
        public double ExtEccentricityMinor { get; set; }
        public double BaseOffset { get; set; } // Same as ExtEccentricityMinor for the minor axis base offset 

        public double EffectiveCover => ClearCover + ShearBarDia + (MainBarDia / 2.0);
        public int TotalBars => 2 * (NumMinorBars + NumMajorBars) - 4;
        public double TotalAreaSteel => TotalBars * Math.PI * Math.Pow(MainBarDia, 2) / 4.0;
        public double PercentageSteel => (Width > 0 && Depth > 0) ? (TotalAreaSteel / (Width * Depth)) * 100.0 : 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
