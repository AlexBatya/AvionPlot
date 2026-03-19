using System;
using AvionPlot.Models;
using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace AvionPlot.Views
{
    public partial class MainWindow
    {
        private enum GraphMode { Normal, Derivative, SecondDerivative }

        private void SetMode(GraphMode mode)
        {
            currentMode = mode;
            BuildSeries();
        }

        private void BuildSeries()
        {
            if (data == null || data.Count == 0) return;

            plotModel.Series.Clear();
            seriesList.Clear();
            modelPerGraph.Clear();

            AddSeriesWithModel(d => d.OSWES, "OSWES");
            AddSeriesWithModel(d => d.WES12, "WES12");
            AddSeriesWithModel(d => d.WES34, "WES34");
            AddSeriesWithModel(d => d.WES56, "WES56");
            AddSeriesWithModel(d => d.WES78, "WES78");
            AddSeriesWithModel(d => d.WES910, "WES910");
            AddSeriesWithModel(d => d.WES1112, "WES1112");

            plotModel.InvalidatePlot(true);

            if (isSidePanelVisible)
                UpdateMathModelPanel();
        }

        private void AddSeriesWithModel(Func<RowData, int> selector, string title)
        {
            var series = new LineSeries
            {
                Title = title,
                StrokeThickness = 2,
                MarkerType = MarkerType.None,
                Color = OxyPalettes.HueDistinct(8).Colors[seriesList.Count] // автоцвет
            };

            var values = new List<double>();
            foreach (var row in data)
                values.Add(selector(row));

            if (currentMode == GraphMode.Derivative)
            {
                for (int i = 1; i < values.Count; i++)
                    series.Points.Add(new DataPoint(i - 1, values[i] - values[i - 1]));
            }
            else if (currentMode == GraphMode.SecondDerivative)
            {
                for (int i = 2; i < values.Count; i++)
                    series.Points.Add(new DataPoint(i - 2, values[i] - 2 * values[i - 1] + values[i - 2]));
            }
            else
            {
                for (int i = 0; i < values.Count; i++)
                    series.Points.Add(new DataPoint(i, values[i]));

                CalculateModelForGraph(title, selector);
            }

            series.IsVisible = MenuBarControl.IsGraphChecked(title);

            seriesList.Add(series);
            plotModel.Series.Add(series);
        }

        private void Menu_GraphModeChanged(object sender, string mode)
        {
            currentMode = mode switch
            {
                "Обычный" => GraphMode.Normal,
                "Производная" => GraphMode.Derivative,
                "Вторая производная" => GraphMode.SecondDerivative,
                _ => GraphMode.Normal
            };

            SetMode(currentMode);
        }

        private void Menu_GraphVisibilityChanged(object sender, string graphName)
        {
            foreach (var series in seriesList)
                if (series.Title == graphName)
                    series.IsVisible = MenuBarControl.IsGraphChecked(graphName);

            plotModel.InvalidatePlot(true);

            if (isSidePanelVisible)
                UpdateMathModelPanel();
        }

        private void ResetZoom_Clicked(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var axis in plotModel.Axes)
                axis.Reset();

            plotModel.InvalidatePlot(false);
        }
    }
}
