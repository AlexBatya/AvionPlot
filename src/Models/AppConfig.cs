using System.Collections.Generic;

namespace AvionPlot.Models
{
    public class AppConfig
    {
        public Dictionary<string, bool> GraphVisibility { get; set; } = new();
        public string GraphMode { get; set; } = "Normal";
    }
}
