
using System.Windows;
using Custom.Exporter.ViewModels;

namespace Custom.Exporter
{
    public class CustomExporterApp
    {
        private readonly Window _ownerWindow;
        private readonly CustomExporterViewModel _viewModel;

        /// <summary>
        /// Constructor
        /// </summary>
        public CustomExporterApp(CustomExporterViewModel viewModel, Window ownerWindow)
        {
            _ownerWindow = ownerWindow;
            _viewModel = viewModel;
        }

        public void ShowDialog()
        {

        }
    }
}