using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using AvionPlot.Models;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Wpf;

namespace AvionPlot.Views
{
    public partial class MainWindow : Window
    {
        private PlotModel plotModel;
        private List<LineSeries> seriesList;
        private List<RowData> data = new();
        private GraphMode currentMode = GraphMode.Normal;

        private static readonly string ConfigDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AvionPlot");
        private static readonly string ConfigFilePath =
            Path.Combine(ConfigDirectory, "config.json");

        private AppConfig config = new();
        private bool isSidePanelVisible = false;

        private Dictionary<string, (double zeta, double omega_n, double omega_d)> modelPerGraph = new();

        private readonly string[] graphNames =
        {
            "OSWES", "WES12", "WES34", "WES56",
            "WES78", "WES910", "WES1112"
        };

        public MainWindow(string[] args = null)
        {
            InitializeComponent();

            Title = "AvionTables";

            LoadConfig();
            InitializeHotkeys();

            plotModel = new PlotModel { Title = "Графики осей проезда" };

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
            seriesList = new();

            MenuBarControl.BuildGraphList(graphNames);
            MenuBarControl.ApplySavedVisibility(config.GraphVisibility);

            MenuBarControl.GraphVisibilityChanged += Menu_GraphVisibilityChanged;
            MenuBarControl.GraphModeChanged += Menu_GraphModeChanged;
            MenuBarControl.OpenFileClicked += MenuOpenFile_Click;
            MenuBarControl.ExitClicked += (s, e) => Close();
            MenuBarControl.ResetZoomClicked += ResetZoom_Clicked;
            MenuBarControl.MathModelClicked += (s, e) => ToggleSidePanel();

            MenuBarControl.PrintPdfClicked += PrintPdf_Clicked;
            MenuBarControl.PrintPngClicked += PrintPng_Clicked;

            RestoreGraphMode();

            if (args != null && args.Length > 0 && File.Exists(args[0]))
                LoadFile(args[0]);

            AllowDrop = true;
            Drop += MainWindow_Drop;
        }

        // ===============================
        // ЭКСПОРТ
        // ===============================

        private void PrintPdf_Clicked(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "PDF File (*.pdf)|*.pdf",
                FileName = "Graph.pdf"
            };

            if (dlg.ShowDialog() != true)
                return;

            try
            {
                using var stream = File.Create(dlg.FileName);
                OxyPlot.Pdf.PdfExporter.Export(plotModel, stream, 1200, 800);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка PDF");
            }
        }

        private void PrintPng_Clicked(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "PNG File (*.png)|*.png",
                FileName = "Graph.png"
            };

            if (dlg.ShowDialog() != true)
                return;

            var exporter = new PngExporter
            {
                Width = 1600,
                Height = 1000
            };

            exporter.ExportToFile(plotModel, dlg.FileName);
        }

        // ===============================
        // ПАНЕЛЬ МАТ. МОДЕЛИ
        // ===============================

        private void ToggleSidePanel()
        {
            if (SidePanel == null) return;

            isSidePanelVisible = !isSidePanelVisible;
            SidePanel.Visibility = isSidePanelVisible ? Visibility.Visible : Visibility.Collapsed;

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

        // ===============================
        // РЕЖИМЫ
        // ===============================

        private enum GraphMode { Normal, Derivative, SecondDerivative }

        private void SetMode(GraphMode mode)
        {
            currentMode = mode;
            BuildSeries();
        }

        private void RestoreGraphMode()
        {
            if (Enum.TryParse(config.GraphMode, out GraphMode savedMode))
            {
                currentMode = savedMode;
                SetMode(currentMode);
                MenuBarControl.SetModeChecked(currentMode.ToString());
            }
        }

        private void InitializeHotkeys()
        {
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => MenuOpenFile_Click(null, null)), new KeyGesture(Key.O, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => ResetZoom_Clicked(null, null)), new KeyGesture(Key.R, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => SetMode(GraphMode.Normal)), new KeyGesture(Key.D1, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => SetMode(GraphMode.Derivative)), new KeyGesture(Key.D2, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => SetMode(GraphMode.SecondDerivative)), new KeyGesture(Key.D3, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => ToggleSidePanel()), new KeyGesture(Key.E, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => Close()), new KeyGesture(Key.F4, ModifierKeys.Alt)));
        }

        // ===============================
        // ГРАФИКИ
        // ===============================

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
            var series = new LineSeries { Title = title };
            var values = new List<double>();

            foreach (var row in data)
                values.Add(selector(row));

            if (currentMode == GraphMode.Derivative)
                for (int i = 1; i < values.Count; i++)
                    series.Points.Add(new DataPoint(i - 1, values[i] - values[i - 1]));
            else if (currentMode == GraphMode.SecondDerivative)
                for (int i = 2; i < values.Count; i++)
                    series.Points.Add(new DataPoint(i - 2, values[i] - 2 * values[i - 1] + values[i - 2]));
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

        private void ResetZoom_Clicked(object sender, RoutedEventArgs e)
        {
            foreach (var axis in plotModel.Axes)
                axis.Reset();

            plotModel.InvalidatePlot(false);
        }

        // ===============================
        // ФАЙЛЫ
        // ===============================

        private void MenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "XML Files (*.xml)|*.xml" };
            if (dlg.ShowDialog() == true)
                LoadFile(dlg.FileName);
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
                LoadFile(files[0]);
        }

        private void LoadFile(string path)
        {
            data = DataLoader.LoadFromXml(path);
            modelPerGraph.Clear();

            var fileInfo = new FileInfo(path);

            plotModel.Title =
                $"Размер: {(fileInfo.Length / 1024.0):F2} KB | " +
                $"Создан: {fileInfo.CreationTime:dd.MM.yyyy HH:mm:ss} | " +
                $"Строк: {data?.Count ?? 0}";

            Title = $"AvionTables — {Path.GetFileName(path)}";

            BuildSeries();
        }

        // ===============================
        // КОНФИГ
        // ===============================

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            SaveConfig();
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch { config = new AppConfig(); }
        }

        private void SaveConfig()
        {
            try
            {
                config.GraphVisibility = MenuBarControl.GetGraphStates();
                config.GraphMode = currentMode.ToString();

                if (!Directory.Exists(ConfigDirectory))
                    Directory.CreateDirectory(ConfigDirectory);

                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch { }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        public RelayCommand(Action<object> execute) => this.execute = execute;
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => execute(parameter);
    }
}
