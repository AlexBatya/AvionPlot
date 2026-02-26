using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
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
        private List<RowData> data = new();
        private GraphMode currentMode = GraphMode.Normal;

        private readonly string[] graphNames =
        {
            "OSWES", "WES12", "WES34", "WES56",
            "WES78", "WES910", "WES1112"
        };

        public MainWindow()
        {
            InitializeComponent();

            Title = "AvionTables";

            InitializeHotkeys();

            plotModel = new PlotModel
            {
                Title = "Графики осей проезда"
            };

            seriesList = new List<LineSeries>();

            // Сетка
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Индекс строки",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineThickness = 0.5
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Значение",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineThickness = 0.5
            });

            PlotView.Model = plotModel;

            MenuBarControl.BuildGraphList(graphNames);
            MenuBarControl.GraphVisibilityChanged += Menu_GraphVisibilityChanged;
            MenuBarControl.GraphModeChanged += Menu_GraphModeChanged;
            MenuBarControl.OpenFileClicked += MenuOpenFile_Click;
            MenuBarControl.ExitClicked += (s, e) => Close();
            MenuBarControl.ResetZoomClicked += ResetZoom_Clicked;
        }

        private void InitializeHotkeys()
        {
            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => MenuOpenFile_Click(null, null)),
                new KeyGesture(Key.O, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => ResetZoom_Clicked(null, null)),
                new KeyGesture(Key.R, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => SetMode(GraphMode.Normal)),
                new KeyGesture(Key.D1, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => SetMode(GraphMode.Derivative)),
                new KeyGesture(Key.D2, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => SetMode(GraphMode.SecondDerivative)),
                new KeyGesture(Key.D3, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => Close()),
                new KeyGesture(Key.F4, ModifierKeys.Alt)));
        }

        private enum GraphMode
        {
            Normal,
            Derivative,
            SecondDerivative
        }

        private void SetMode(GraphMode mode)
        {
            currentMode = mode;
            BuildSeries();
        }

        private void ResetZoom_Clicked(object sender, RoutedEventArgs e)
        {
            plotModel.ResetAllAxes();
            plotModel.InvalidatePlot(false);
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

            BuildSeries();
        }

        private void Menu_GraphVisibilityChanged(object sender, string graphName)
        {
            foreach (var series in seriesList)
            {
                if (series.Title == graphName)
                {
                    series.IsVisible = MenuBarControl.IsGraphChecked(graphName);
                    break;
                }
            }

            plotModel.InvalidatePlot(true);
        }

        private void BuildSeries()
        {
            if (data == null || data.Count == 0)
                return;

            plotModel.Series.Clear();
            seriesList.Clear();

            AddSeries(d => d.OSWES, "OSWES");
            AddSeries(d => d.WES12, "WES12");
            AddSeries(d => d.WES34, "WES34");
            AddSeries(d => d.WES56, "WES56");
            AddSeries(d => d.WES78, "WES78");
            AddSeries(d => d.WES910, "WES910");
            AddSeries(d => d.WES1112, "WES1112");

            plotModel.InvalidatePlot(true);
        }

        private void AddSeries(Func<RowData, int> selector, string title)
        {
            var series = new LineSeries
            {
                Title = title,
                TrackerFormatString = "{0}\nX: {2}\nY: {4}"
            };

            var values = new List<double>();
            foreach (var row in data)
                values.Add(selector(row));

            if (currentMode == GraphMode.Derivative)
            {
                var deriv = new List<double>();
                for (int i = 1; i < values.Count; i++)
                    deriv.Add(values[i] - values[i - 1]);
                values = deriv;
            }
            else if (currentMode == GraphMode.SecondDerivative)
            {
                var acc = new List<double>();
                for (int i = 2; i < values.Count; i++)
                    acc.Add(values[i] - 2 * values[i - 1] + values[i - 2]);
                values = acc;
            }

            for (int i = 0; i < values.Count; i++)
                series.Points.Add(new DataPoint(i, values[i]));

            series.IsVisible = MenuBarControl.IsGraphChecked(title);

            seriesList.Add(series);
            plotModel.Series.Add(series);
        }

        private void MenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml"
            };

            if (dlg.ShowDialog() == true)
                LoadFile(dlg.FileName);
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
                LoadFile(files[0]);
        }

        private void LoadFile(string path)
        {
            data = DataLoader.LoadFromXml(path);

            // Обновляем Title окна
            string fileName = Path.GetFileName(path);
            Title = $"AvionTables — {fileName}";

            BuildSeries();
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> execute;

        public RelayCommand(Action<object> execute)
        {
            this.execute = execute;
        }

        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => execute(parameter);
    }
}
