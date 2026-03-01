using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AvionPlot.Views
{
    public partial class MenuBar : UserControl
    {
        public event EventHandler<string> GraphVisibilityChanged;
        public event EventHandler<string> GraphModeChanged;

        public event RoutedEventHandler OpenFileClicked;
        public event RoutedEventHandler ExitClicked;
        public event RoutedEventHandler ResetZoomClicked;

        // Новые события печати
        public event RoutedEventHandler PrintPdfClicked;
        public event RoutedEventHandler PrintPngClicked;

        // Событие мат. модели
        public event RoutedEventHandler MathModelClicked;

        public MenuBar()
        {
            InitializeComponent();

            OpenMenuItem.Click += (s, e) => OpenFileClicked?.Invoke(s, e);
            ExitMenuItem.Click += (s, e) => ExitClicked?.Invoke(s, e);
            ResetZoomMenuItem.Click += (s, e) => ResetZoomClicked?.Invoke(s, e);

            PrintPdfMenuItem.Click += (s, e) => PrintPdfClicked?.Invoke(s, e);
            PrintPngMenuItem.Click += (s, e) => PrintPngClicked?.Invoke(s, e);

            NormalModeItem.Click += Mode_Click;
            DerivativeModeItem.Click += Mode_Click;
            SecondDerivativeModeItem.Click += Mode_Click;

            MathModelMenuItem.Click += (s, e) => MathModelClicked?.Invoke(s, e);
        }

        private void Mode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem clickedItem)
                return;

            foreach (MenuItem item in FunctionMenu.Items)
                item.IsChecked = false;

            clickedItem.IsChecked = true;

            GraphModeChanged?.Invoke(this, clickedItem.Header.ToString());
        }

        public void BuildGraphList(string[] names)
        {
            GraphsMenuItem.Items.Clear();

            foreach (var name in names)
            {
                var item = new MenuItem
                {
                    Header = name,
                    IsCheckable = true,
                    IsChecked = true
                };

                item.Click += (s, e) =>
                {
                    GraphVisibilityChanged?.Invoke(this, name);
                };

                GraphsMenuItem.Items.Add(item);
            }
        }

        public bool IsGraphChecked(string name)
        {
            foreach (MenuItem item in GraphsMenuItem.Items)
                if (item.Header.ToString() == name)
                    return item.IsChecked;

            return true;
        }

        public Dictionary<string, bool> GetGraphStates()
        {
            var dict = new Dictionary<string, bool>();

            foreach (MenuItem item in GraphsMenuItem.Items)
                dict[item.Header.ToString()] = item.IsChecked;

            return dict;
        }

        public void ApplySavedVisibility(Dictionary<string, bool> saved)
        {
            if (saved == null)
                return;

            foreach (MenuItem item in GraphsMenuItem.Items)
                if (saved.TryGetValue(item.Header.ToString(), out bool value))
                    item.IsChecked = value;
        }

        public void SetModeChecked(string mode)
        {
            foreach (MenuItem item in FunctionMenu.Items)
                item.IsChecked = false;

            switch (mode)
            {
                case "Normal":
                    NormalModeItem.IsChecked = true;
                    break;
                case "Derivative":
                    DerivativeModeItem.IsChecked = true;
                    break;
                case "SecondDerivative":
                    SecondDerivativeModeItem.IsChecked = true;
                    break;
            }
        }
    }
}
