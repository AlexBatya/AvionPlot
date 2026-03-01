using System.Text;
using System.Windows;

namespace AvionPlot
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 🔥 ВАЖНО: регистрация старых кодировок (1252 и др.)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            base.OnStartup(e);

            // Передаём аргументы запуска (путь к XML)
            var mainWindow = new Views.MainWindow(e.Args);
            mainWindow.Show();
        }
    }
}
