using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;
using System.Windows.Threading;
using Custom.Exporter.ViewModels;
using Vim.Revit.Core;
using Vim.Util.Logging;

namespace Custom.Exporter
{
    // ReSharper disable once UnusedMember.Global
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]
    public sealed class Plugin : IExternalApplication
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private static ILogger _logger;

        /// <summary>
        /// The exporter view model.
        /// </summary>
        private static MainWindowViewModel _mainWindowViewModel;

        /// <summary>
        /// The exporter user interface.
        /// </summary>
        private static CustomExporterApp _customExporterApp;

        /// <summary>
        /// The main thread dispatcher. Using this dispatcher prevents a crash when pyRevit is simultaneously installed.
        /// </summary>
        private static Dispatcher _dispatcher;

        /// <summary>
        /// Code executed when Revit starts up.
        /// </summary>
        public Result OnStartup(UIControlledApplication revitApp)
        {
            // Initialize the logger.
            _logger = Logging.Initialize(RevitConstants.RevitYearVersion);

            InitializeExporterUi(revitApp);

            return Result.Succeeded;
        }

        /// <summary>
        /// Code executed when Revit shuts down.
        /// </summary>
        public Result OnShutdown(UIControlledApplication revitApp)
        {
            _mainWindowViewModel.Save();

            return Result.Succeeded;
        }

        private void InitializeExporterUi(UIControlledApplication revitApp)
        {
            // We are not in an automated setting, so initialize the UI.
            PluginRibbon.Create(revitApp);

            // The owner window should be the Revit main window.
            var ownerWindow = HwndSource.FromHwnd(revitApp.MainWindowHandle)?.RootVisual as Window;

            // Initialize the ui application on startup.
            // IMPORTANT: Any attempt to lazily instantiate the ui application elsewhere produces display issues
            // when this plugin is built in Release mode.
            _dispatcher = Dispatcher.CurrentDispatcher;
            _dispatcher.Invoke(() => InnerInitializeExporterApp(ownerWindow));
        }

        private static void InnerInitializeExporterApp(Window ownerWindow)
        {
            _mainWindowViewModel = new MainWindowViewModel(_logger);
            _customExporterApp = new CustomExporterApp(
                _mainWindowViewModel,
                ownerWindow); // Initializes the exporter app with the backing view model.
        }

        /// <summary>
        /// Shows the plugin window UI.
        /// </summary>
        internal static void ShowExporterDialog(ExternalCommandData commandData)
        {
            _dispatcher.Invoke(() => InnerShowExporterDialog(commandData));
        }

        private static void InnerShowExporterDialog(ExternalCommandData commandData)
        {
            // Assign the command data to the service's singleton. This reference is required for a valid export process.
            _mainWindowViewModel.SetCommandData(commandData);

            // Show the window as a modal dialog once it has gone through its initialization process.
            _customExporterApp.ShowDialog();
        }
    }
}
