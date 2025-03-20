using System.Windows;
using Custom.Exporter.ViewModels;

namespace Custom.Exporter
{
    public class CustomExporterApp
    {
        private readonly Window _mainWindow;

        /// <summary>
        /// Constructor
        /// </summary>
        public CustomExporterApp(MainWindowViewModel viewModel, Window ownerWindow)
        {
            _mainWindow = new MainWindow(viewModel)
            {
                Owner = ownerWindow
            };
        }

        public void ShowDialog()
        {
            _mainWindow.ShowDialog();
        }
    }
}