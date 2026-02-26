using System;
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

        public MenuBar()
        {
            InitializeComponent();

            OpenMenuItem.Click += (s, e) => OpenFileClicked?.Invoke(s, e);
            ExitMenuItem.Click += (s, e) => ExitClicked?.Invoke(s, e);
            ResetZoomMenuItem.Click += (s, e) => ResetZoomClicked?.Invoke(s, e);

            NormalModeItem.Click += Mode_Click;
            DerivativeModeItem.Click += Mode_Click;
            SecondDerivativeModeItem.Click += Mode_Click;
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
            {
                if (item.Header.ToString() == name)
                    return item.IsChecked;
            }
            return true;
        }
    }
}
