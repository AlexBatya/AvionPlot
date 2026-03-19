using AvionPlot.Models;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace AvionPlot.Views
{
    public partial class MainWindow
    {
        private void MenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml"
            };

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

            var fileInfo = new FileInfo(path);

            plotModel.Title =
                $"Размер: {(fileInfo.Length / 1024.0):F2} KB | " +
                $"Создан: {fileInfo.CreationTime:dd.MM.yyyy HH:mm:ss} | " +
                $"Строк: {data?.Count ?? 0}";

            Title = $"AvionTables — {Path.GetFileName(path)}";

            BuildSeries();
        }
    }
}
