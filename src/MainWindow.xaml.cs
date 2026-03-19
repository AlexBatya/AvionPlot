using System;
using AvionPlot.Models;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using AvionPlot.Common;

namespace AvionPlot.Views
{
    public partial class MainWindow : Window
    {
        private PlotModel plotModel;
        private List<LineSeries> seriesList;
        private List<RowData> data = new();
        private GraphMode currentMode = GraphMode.Normal;

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
                IsLegendVisible = false // Легенда отключена
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

            MenuBarControl.PrintPdfClicked += PrintPdf_Clicked;
            MenuBarControl.PrintPngClicked += PrintPng_Clicked;
        }
    }
}
