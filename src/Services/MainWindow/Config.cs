using System.IO;
using AvionPlot.Models;
using System.Text.Json;

namespace AvionPlot.Views
{
    public partial class MainWindow
    {
        private static readonly string ConfigDirectory =
            Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "AvionPlot");

        private static readonly string ConfigFilePath =
            Path.Combine(ConfigDirectory, "config.json");

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
            catch
            {
                config = new AppConfig();
            }
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
}
