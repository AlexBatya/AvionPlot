using System;
using AvionPlot.Models;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using OxyPlot.Wpf;

namespace AvionPlot.Views
{
    public partial class MainWindow
    {
        private void PrintPdf_Clicked(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "PDF File (*.pdf)|*.pdf",
                FileName = "Graph.pdf"
            };

            if (dlg.ShowDialog() != true)
                return;

            try
            {
                using var stream = File.Create(dlg.FileName);
                OxyPlot.Pdf.PdfExporter.Export(plotModel, stream, 1200, 800);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка PDF");
            }
        }

        private void PrintPng_Clicked(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "PNG File (*.png)|*.png",
                FileName = "Graph.png"
            };

            if (dlg.ShowDialog() != true)
                return;

            var exporter = new PngExporter
            {
                Width = 1600,
                Height = 1000
            };

            exporter.ExportToFile(plotModel, dlg.FileName);
        }
    }
}
