using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using AvionPlot.Models;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace AvionPlot.Views
{
    public partial class MainWindow : Window
    {
        private PlotModel plotModel;
        private List<LineSeries> seriesList;
        private List<RowData> data = new List<RowData>();
        private GraphMode currentMode = GraphMode.Normal;

        public MainWindow()
        {
            InitializeComponent();

            plotModel = new PlotModel { Title = "Графики осей проезда" };
            seriesList = new List<LineSeries>();

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Индекс строки",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Значение",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });

            PlotView.Model = plotModel;

            // Drag-and-drop
            this.AllowDrop = true;
            this.Drop += MainWindow_Drop;
        }

        private enum GraphMode { Normal, Derivative, Acceleration }

        private void BuildSeries(GraphMode mode)
        {
            if (data == null || data.Count == 0) return;

            plotModel.Series.Clear();
            seriesList.Clear();

            AddSeries(data, d => d.OSWES, "OSWES", mode);
            AddSeries(data, d => d.WES12, "WES12", mode);
            AddSeries(data, d => d.WES34, "WES34", mode);
            AddSeries(data, d => d.WES56, "WES56", mode);
            AddSeries(data, d => d.WES78, "WES78", mode);
            AddSeries(data, d => d.WES910, "WES910", mode);
            AddSeries(data, d => d.WES1112, "WES1112", mode);

            // Применяем состояние чекбоксов
            foreach (CheckBox cb in CheckBoxPanel.Children)
            {
                foreach (var series in seriesList)
                {
                    if (series.Title == cb.Content.ToString())
                        series.IsVisible = cb.IsChecked == true;
                }
            }

            plotModel.InvalidatePlot(true);
        }

        private void AddSeries(List<RowData> dataList, Func<RowData, int> selector, string title, GraphMode mode)
        {
            var series = new LineSeries
            {
                Title = title,
                TrackerFormatString = "{0}\nX: {2}\nY: {4}"
            };

            List<double> values = new List<double>();
            foreach (var row in dataList)
                values.Add(selector(row));

            if (mode == GraphMode.Derivative)
            {
                var deriv = new List<double>();
                for (int i = 1; i < values.Count; i++)
                    deriv.Add(values[i] - values[i - 1]);
                values = deriv;
            }
            else if (mode == GraphMode.Acceleration)
            {
                var acc = new List<double>();
                for (int i = 2; i < values.Count; i++)
                    acc.Add(values[i] - 2 * values[i - 1] + values[i - 2]);
                values = acc;
            }

            for (int i = 0; i < values.Count; i++)
                series.Points.Add(new DataPoint(i, values[i]));

            seriesList.Add(series);
            plotModel.Series.Add(series);
        }

        private void GraphTypeChanged(object sender, RoutedEventArgs e)
        {
            if (!(sender is RadioButton rb) || rb.IsChecked != true) return;

            currentMode = rb.Content.ToString() switch
            {
                "Обычный" => GraphMode.Normal,
                "Производная" => GraphMode.Derivative,
                "Ускорение" => GraphMode.Acceleration,
                _ => GraphMode.Normal
            };

            BuildSeries(currentMode);
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox cb)) return;

            foreach (var series in seriesList)
            {
                if (series.Title == cb.Content.ToString())
                {
                    series.IsVisible = cb.IsChecked == true;
                    break;
                }
            }

            plotModel.InvalidatePlot(true);
        }

        private void MenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "XML Files (*.xml)|*.xml" };
            if (dlg.ShowDialog() == true)
                LoadFile(dlg.FileName);
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                    LoadFile(files[0]);
            }
        }

        private void LoadFile(string path)
        {
            data = DataLoader.LoadFromXml(path);

            // Сохраняем состояние чекбоксов
            var checkStates = new Dictionary<string, bool>();
            foreach (CheckBox cb in CheckBoxPanel.Children)
            {
                if (cb.Content != null)
                    checkStates[cb.Content.ToString()] = cb.IsChecked == true;
            }

            // Очищаем панель и создаём новые чекбоксы
            CheckBoxPanel.Children.Clear();
            foreach (var col in new[] { "OSWES", "WES12", "WES34", "WES56", "WES78", "WES910", "WES1112" })
            {
                var cb = new CheckBox
                {
                    Content = col,
                    IsChecked = checkStates.TryGetValue(col, out bool val) ? val : true,
                    Margin = new Thickness(5)
                };
                cb.Checked += CheckBoxChanged;
                cb.Unchecked += CheckBoxChanged;
                CheckBoxPanel.Children.Add(cb);
            }

            BuildSeries(currentMode);
        }
    }
}
