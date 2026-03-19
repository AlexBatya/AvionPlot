using AvionPlot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AvionPlot.Views
{
    public partial class MainWindow
    {
        private void ToggleSidePanel()
        {
            if (SidePanel == null) return;

            isSidePanelVisible = !isSidePanelVisible;
            SidePanel.Visibility = isSidePanelVisible
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;

            if (isSidePanelVisible)
                UpdateMathModelPanel();
        }

        private void UpdateMathModelPanel()
        {
            if (platformModelTextBlock == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== ОБЩАЯ МАТЕМАТИЧЕСКАЯ МОДЕЛЬ ===\n");

            foreach (var series in seriesList)
            {
                if (!series.IsVisible || currentMode != GraphMode.Normal)
                    continue;

                if (!modelPerGraph.TryGetValue(series.Title, out var model))
                    continue;

                sb.AppendLine($"График: {series.Title}");
                sb.AppendLine($"ζ = {model.zeta:F4}");
                sb.AppendLine($"ω_n = {model.omega_n:F4}");
                sb.AppendLine($"ω_d = {model.omega_d:F4}");
                sb.AppendLine($"x'' + {2 * model.zeta * model.omega_n:F4}x' + {model.omega_n * model.omega_n:F4}x = 0");
                sb.AppendLine();
            }

            platformModelTextBlock.Text = sb.Length > 0
                ? sb.ToString()
                : "Нет видимых Normal-графиков.";
        }

        private void CalculateModelForGraph(string graphTitle, Func<RowData, int> selector)
        {
            if (data == null || data.Count < 3) return;

            var values = new List<double>();
            foreach (var row in data)
                values.Add(selector(row));

            var peaks = new List<(int index, double value)>();

            for (int i = 1; i < values.Count - 1; i++)
                if (values[i] > values[i - 1] && values[i] > values[i + 1])
                    peaks.Add((i, values[i]));

            if (peaks.Count < 2) return;

            double A1 = peaks[0].value;
            double A2 = peaks[1].value;

            double delta = Math.Log(A1 / A2);
            double zeta = delta / Math.Sqrt(4 * Math.PI * Math.PI + delta * delta);
            double T = peaks[1].index - peaks[0].index;
            double omega_d = 2 * Math.PI / T;
            double omega_n = omega_d / Math.Sqrt(1 - zeta * zeta);

            modelPerGraph[graphTitle] = (zeta, omega_n, omega_d);
        }
    }
}
