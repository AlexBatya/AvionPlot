using AvionPlot.Models;
using System.Windows.Input;
using AvionPlot.Common;

namespace AvionPlot.Views
{
    public partial class MainWindow
    {
        private void InitializeHotkeys()
        {
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => MenuOpenFile_Click(null, null)),
                new KeyGesture(Key.O, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(new RelayCommand(_ => ResetZoom_Clicked(null, null)),
                new KeyGesture(Key.R, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(new RelayCommand(_ => SetMode(GraphMode.Normal)),
                new KeyGesture(Key.D1, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(new RelayCommand(_ => SetMode(GraphMode.Derivative)),
                new KeyGesture(Key.D2, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(new RelayCommand(_ => SetMode(GraphMode.SecondDerivative)),
                new KeyGesture(Key.D3, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(new RelayCommand(_ => ToggleSidePanel()),
                new KeyGesture(Key.E, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(new RelayCommand(_ => Close()),
                new KeyGesture(Key.F4, ModifierKeys.Alt)));
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
    }
}
