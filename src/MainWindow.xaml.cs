using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AvionPlot.Models;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using AvionPlot.Common;

namespace AvionPlot.Views
{
    // Класс для элементов списка файлов
    public class FileItem
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public bool IsChecked { get; set; }
    }

    public partial class MainWindow : Window
    {
        private PlotModel plotModel;
        private List<LineSeries> seriesList;
        private List<RowData> data = new();
        private GraphMode currentMode = GraphMode.Normal;

        private AppConfig config = new();
        private bool isSidePanelVisible = false;

        private string currentFilePath;

        private Dictionary<string, (double zeta, double omega_n, double omega_d)> modelPerGraph = new();

        private readonly string[] graphNames =
        {
            "OSWES", "WES12", "WES34", "WES56",
            "WES78", "WES910", "WES1112"
        };

        private List<FileItem> folderFiles = new();

        public MainWindow(string[] args = null)
        {
            InitializeComponent();

            Title = "AvionTables";

            LoadConfig();
            InitializeHotkeys();

            InitPlot();

            MenuBarControl.BuildGraphList(graphNames);
            MenuBarControl.ApplySavedVisibility(config.GraphVisibility);

            HookMenu();

            RestoreGraphMode();

            if (args != null && args.Length > 0 && File.Exists(args[0]))
                LoadFile(args[0]);

            AllowDrop = true;
            Drop += MainWindow_Drop;
        }

        private void InitPlot()
        {
            plotModel = new PlotModel
            {
                Title = "Графики осей проезда",
                Background = OxyColors.White,
                IsLegendVisible = false
            };

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Индекс строки",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineThickness = 0.5
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Значение",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineThickness = 0.5
            });

            PlotView.Model = plotModel;
            seriesList = new List<LineSeries>();
        }

        private void HookMenu()
        {
            MenuBarControl.GraphVisibilityChanged += Menu_GraphVisibilityChanged;
            MenuBarControl.GraphModeChanged += Menu_GraphModeChanged;
            MenuBarControl.OpenFileClicked += MenuOpenFile_Click;
            MenuBarControl.ExitClicked += (s, e) => Close();
            MenuBarControl.ResetZoomClicked += ResetZoom_Clicked;

            MenuBarControl.MathModelClicked += (s, e) => ToggleSidePanel();

            // Заметки
            MenuBarControl.NotesClicked += (s, e) => ShowNotesPanel();

            MenuBarControl.PrintPdfClicked += PrintPdf_Clicked;
            MenuBarControl.PrintPngClicked += PrintPng_Clicked;
        }

        // 🔹 ОБРАБОТКА КЛИКА ПО ФАЙЛУ
        private void FilesListBox_Click(object sender, MouseButtonEventArgs e)
        {
            if (FilesListBox.SelectedItem is FileItem item && File.Exists(item.FullPath))
            {
                LoadFile(item.FullPath);
            }
        }

        // 🔹 Показ панели заметок с файлами
        private void ShowNotesPanel()
        {
            if (string.IsNullOrEmpty(currentFilePath) || !File.Exists(currentFilePath))
            {
                MessageBox.Show("Сначала загрузите файл");
                return;
            }

            string directory = Path.GetDirectoryName(currentFilePath);
            if (!Directory.Exists(directory)) return;

            var files = Directory.GetFiles(directory, "*.xml"); // только xml

            folderFiles = files.Select(f => new FileItem
            {
                FileName = Path.GetFileName(f),
                FullPath = f,
                IsChecked = false
            }).ToList();

            FilesListBox.ItemsSource = folderFiles;

            SidePanel.Visibility = Visibility.Visible;

            if (!isSidePanelVisible)
            {
                Width += 320;
                isSidePanelVisible = true;
            }
        }

        // Здесь должны быть уже существующие методы LoadFile, BuildSeries, SetMode и т.д.
        // Убедись, что внутри LoadFile у тебя есть:
        // currentFilePath = path;
    }
}
