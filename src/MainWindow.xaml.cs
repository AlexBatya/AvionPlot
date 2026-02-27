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

        // Словарь для хранения анализа каждого Normal-графика
        private Dictionary<string, (double zeta, double omega_n, double omega_d)> modelPerGraph
            = new Dictionary<string, (double zeta, double omega_n, double omega_d)>();

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

            // Сетка и оси
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
            seriesList = new List<LineSeries>();

            MenuBarControl.BuildGraphList(graphNames);
            MenuBarControl.ApplySavedVisibility(config.GraphVisibility);

            MenuBarControl.GraphVisibilityChanged += Menu_GraphVisibilityChanged;
            MenuBarControl.GraphModeChanged += Menu_GraphModeChanged;
            MenuBarControl.OpenFileClicked += MenuOpenFile_Click;
            MenuBarControl.ExitClicked += (s, e) => Close();
            MenuBarControl.ResetZoomClicked += ResetZoom_Clicked;
            MenuBarControl.MathModelClicked += (s, e) => ToggleSidePanel();

            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => ToggleSidePanel()),
                new KeyGesture(Key.E, ModifierKeys.Control)
            ));

            RestoreGraphMode();

            if (args != null && args.Length > 0 && File.Exists(args[0]))
                LoadFile(args[0]);

            AllowDrop = true;
            Drop += MainWindow_Drop;
        }

        // ===============================
        // ПАНЕЛЬ МАТЕМАТИЧЕСКОЙ МОДЕЛИ
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
                // Только видимые Normal-графики
                if (!series.IsVisible || currentMode != GraphMode.Normal) continue;

                if (!modelPerGraph.TryGetValue(series.Title, out var model)) continue;

                sb.AppendLine($"График: {series.Title}");
                sb.AppendLine($"  Коэффициент демпфирования ζ = {model.zeta:F4}");
                sb.AppendLine($"  Собственная частота ω_n = {model.omega_n:F4}");
                sb.AppendLine($"  Затухающая частота ω_d = {model.omega_d:F4}");
                sb.AppendLine($"  Уравнение: x'' + {2 * model.zeta * model.omega_n:F4} x' + {model.omega_n * model.omega_n:F4} x = 0");
                sb.AppendLine($"  Решение: x(t) = A * e^(-ζ*ω_n*t) * sin(ω_d * t + φ)");
                sb.AppendLine();
            }

            platformModelTextBlock.Text = sb.Length > 0 ? sb.ToString() : "Нет видимых Normal-графиков.";
        }

        private void CalculateModelForGraph(string graphTitle, Func<RowData, int> selector)
        {
            if (data == null || data.Count < 3) return;

            var values = new List<double>();
            foreach (var row in data) values.Add(selector(row));

            // Определяем активную область сигнала
            int startIndex = 0;
            double threshold = 1.0; // порог минимального изменения
            for (int i = 1; i < values.Count; i++)
            {
                if (Math.Abs(values[i] - values[0]) > threshold)
                {
                    startIndex = i;
                    break;
                }
            }

            var activeValues = values.GetRange(startIndex, values.Count - startIndex);

            // Находим первые два пика активной области
            var peaks = new List<(int index, double value)>();
            for (int i = 1; i < activeValues.Count - 1; i++)
            {
                if (activeValues[i] > activeValues[i - 1] && activeValues[i] > activeValues[i + 1])
                    peaks.Add((i, activeValues[i]));
            }

            double zeta = 0, omega_n = 0, omega_d = 0;
            if (peaks.Count >= 2)
            {
                double A1 = peaks[0].value;
                double A2 = peaks[1].value;
                double delta = Math.Log(A1 / A2);
                zeta = delta / Math.Sqrt(4 * Math.PI * Math.PI + delta * delta);

                double T = peaks[1].index - peaks[0].index;
                omega_d = 2 * Math.PI / T;
                omega_n = omega_d / Math.Sqrt(1 - zeta * zeta);
            }

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

        // ===============================
        // ГРАФИКИ
        // ===============================

        private void BuildSeries()
        {
            if (data == null || data.Count == 0) return;

            plotModel.Series.Clear();
            seriesList.Clear();
            // Модели сохраняем для Normal, не трогаем при переключении
            if (currentMode == GraphMode.Normal)
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
            {
                if (series.Title == graphName)
                    series.IsVisible = MenuBarControl.IsGraphChecked(graphName);
            }

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
            Title = $"AvionTables — {Path.GetFileName(path)}";
            plotModel.Title = $"Строк: {data?.Count ?? 0}";

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
