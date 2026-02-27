using System.Windows;

namespace AvionPlot
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Передаём аргументы запуска (путь к XML)
            var mainWindow = new Views.MainWindow(e.Args);
            mainWindow.Show();
        }
    }
}
