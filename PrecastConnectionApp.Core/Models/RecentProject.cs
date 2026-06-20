using System;
using System.Windows;

namespace PrecastConnectionApp.Models
{
    public class RecentProject
    {
        public Guid Id { get; set; }
        public string ProjectName { get; set; }
        public string Location { get; set; }
        public string FilePath { get; set; }
        public DateTime LastUpdated { get; set; }

        public int TotalColumns { get; set; }
        public int SafeColumns { get; set; }
        public int ReviewColumns { get; set; }
        public int UnsafeColumns { get; set; }

        public bool HasUnsafe => UnsafeColumns > 0;
        public bool IsAllSafe => TotalColumns > 0 && UnsafeColumns == 0 && ReviewColumns == 0;
        public double SafePercentage => TotalColumns == 0 ? 0 : (double)SafeColumns / TotalColumns * 100;
        public double UnsafePercentage => TotalColumns == 0 ? 0 : (double)UnsafeColumns / TotalColumns * 100;

        public GridLength SafeWidth => new GridLength(SafePercentage, GridUnitType.Star);
        public GridLength UnsafeWidth => new GridLength(UnsafePercentage, GridUnitType.Star);
        public GridLength ReviewWidth => new GridLength(TotalColumns == 0 ? 100 : (100 - SafePercentage - UnsafePercentage), GridUnitType.Star);

        public string RelativeTime
        {
            get
            {
                var timeSpan = DateTime.Now - LastUpdated;
                
                if (DateTime.Now.Date == LastUpdated.Date)
                {
                    if (timeSpan.TotalHours < 1)
                        return timeSpan.TotalMinutes < 1 ? "Updated just now" : $"Updated {(int)timeSpan.TotalMinutes} minute(s) ago";
                    
                    return $"Updated {(int)timeSpan.TotalHours} hour(s) ago";
                }
                
                if (DateTime.Now.Date.AddDays(-1) == LastUpdated.Date)
                {
                    return "Updated yesterday";
                }

                return $"Updated on {LastUpdated.ToString("MMM dd, yyyy")}";
            }
        }
    }
}
