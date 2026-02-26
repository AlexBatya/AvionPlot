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
        private List<LineSeries> seriesList = new List<LineSeries>();
        private List<RowData> data = new List<RowData>();

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация графика
            plotModel = new PlotModel { Title = "Графики осей проезда" };

            // Настройка осей с сеткой
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

            // Контроллер для масштабирования и панорамирования
            var controller = new PlotController();
            controller.UnbindAll();
            controller.BindMouseWheel(PlotCommands.ZoomWheel);
            controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
            controller.BindMouseDown(OxyMouseButton.Right, PlotCommands.ZoomRectangle);
            PlotView.Controller = controller;
            PlotView.Model = plotModel;

            // Подключаем кнопку "Открыть"
            if (MenuBarControl.MainMenu.FindName("OpenMenuItem") is MenuItem openItem)
                openItem.Click += OpenFile_Click;

            // Drag & Drop
            this.AllowDrop = true;
            this.DragOver += MainWindow_DragOver;
            this.Drop += MainWindow_Drop;
        }

        #region Drag & Drop

        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                e.Effects = (files.Length > 0 && files[0].EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    ? DragDropEffects.Copy
                    : DragDropEffects.None;
            }
            else e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && files[0].EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    LoadFile(files[0]);
                }
            }
        }

        #endregion

        #region Open File

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "XML файлы (*.xml)|*.xml",
                Title = "Выберите файл проезда"
            };

            if (dlg.ShowDialog() == true)
            {
                LoadFile(dlg.FileName);
            }
        }

        #endregion

        private void LoadFile(string path)
        {
            try
            {
                data = DataLoader.LoadFromXml(path);

                // Определяем текущий выбранный режим графика
                GraphMode mode = GetCurrentGraphMode();

                BuildSeries(mode);

                // Создаём чекбоксы или обновляем существующие
                if (CheckBoxPanel.Children.Count == 0)
                    CreateCheckBoxes();
                else
                    ApplyCheckBoxStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файла:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private enum GraphMode { Normal, Derivative, Acceleration }

        private GraphMode GetCurrentGraphMode()
        {
            foreach (var child in LogicalTreeHelper.GetChildren(this))
            {
                if (child is StackPanel panel)
                {
                    foreach (var rbObj in panel.Children)
                    {
                        if (rbObj is RadioButton rb && rb.IsChecked == true)
                        {
                            if (rb.Content.ToString() == "Обычный") return GraphMode.Normal;
                            if (rb.Content.ToString() == "Производная") return GraphMode.Derivative;
                            if (rb.Content.ToString() == "Ускорение") return GraphMode.Acceleration;
                        }
                    }
                }
            }
            return GraphMode.Normal;
        }

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

            // Применяем текущие состояния чекбоксов
            ApplyCheckBoxStates();

            plotModel.InvalidatePlot(true);
        }

        private void AddSeries(List<RowData> data, Func<RowData, int> selector, string title, GraphMode mode)
        {
            var series = new LineSeries
            {
                Title = title,
                TrackerFormatString = "{0}\nX: {2}\nY: {4}"
            };

            List<double> values = new List<double>();
            foreach (var row in data) values.Add(selector(row));

            if (mode == GraphMode.Derivative && values.Count > 1)
            {
                var deriv = new List<double>();
                for (int i = 1; i < values.Count; i++)
                    deriv.Add(values[i] - values[i - 1]);
                values = deriv;
            }
            else if (mode == GraphMode.Acceleration && values.Count > 2)
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

        private void CreateCheckBoxes()
        {
            CheckBoxPanel.Children.Clear();
            foreach (var series in seriesList)
            {
                var cb = new CheckBox
                {
                    Content = series.Title,
                    IsChecked = true,
                    Margin = new Thickness(5)
                };
                cb.Checked += CheckBoxChanged;
                cb.Unchecked += CheckBoxChanged;
                CheckBoxPanel.Children.Add(cb);
            }
        }

        private void ApplyCheckBoxStates()
        {
            foreach (CheckBox cb in CheckBoxPanel.Children)
            {
                foreach (var series in seriesList)
                {
                    if (series.Title == cb.Content.ToString())
                        series.IsVisible = cb.IsChecked == true;
                }
            }
        }

        private void GraphTypeChanged(object sender, RoutedEventArgs e)
        {
            if (!(sender is RadioButton rb) || rb.IsChecked != true) return;

            GraphMode mode = GraphMode.Normal;
            if (rb.Content.ToString() == "Обычный") mode = GraphMode.Normal;
            else if (rb.Content.ToString() == "Производная") mode = GraphMode.Derivative;
            else if (rb.Content.ToString() == "Ускорение") mode = GraphMode.Acceleration;

            BuildSeries(mode);
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
    }
}
