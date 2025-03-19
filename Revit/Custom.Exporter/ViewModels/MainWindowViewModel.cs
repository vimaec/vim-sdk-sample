using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Vim.Revit.Core;
using Vim.Util;
using Vim.Util.Logging;

using Visibility = System.Windows.Visibility;

namespace Custom.Exporter.ViewModels
{
    public enum MainWindowVisualState
    {
        Preflight,
        Processing,
        Finished,
    }

    public class MainWindowViewModel : NotifyPropertyChanged
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindowViewModel(ILogger logger)
        {
            _logger = logger;
            
            ExportVimCommand = CustomCommand.Create(ExportVim);
            BrowseFileCommand = CustomCommand.Create(BrowseFile);
            OpenFolderCommand = CustomCommand.Create(OpenFolder);
            OpenFileCommand = CustomCommand.Create(OpenFile);

            UserSettings = new UserSettings(_logger);
            UserSettings.PropertyChanged += UserSettingsOnPropertyChanged;

            Reset();
        }

        public UserSettings UserSettings { get; }

        public void Save() => UserSettings.Save();

        //------------------------------------------------------------------------------
        // Setup
        //------------------------------------------------------------------------------

        public void SetCommandData(ExternalCommandData commandData)
        {
            CommandData = commandData;
            SaveFileName = $"{Document.Title}.vim";

            Reset();
            UpdateCanExport();
        }

        private ExternalCommandData _commandData;
        public ExternalCommandData CommandData
        {
            get => _commandData;
            set
            {
                SetField(ref _commandData, value);
                OnPropertyChanged(nameof(Document));
                UpdateAvailableView3Ds();
            }
        }

        public Document Document
            => CommandData?.Application?.ActiveUIDocument?.Document;

        //------------------------------------------------------------------------------
        // 3D View Selection
        //------------------------------------------------------------------------------

        public ObservableCollection<string> AvailableViewNames { get; } = new ObservableCollection<string>();

        private void SetAvailableView3Ds(IEnumerable<string> viewNames, string currentViewName)
        {
            var previousViewName = UserSettings.ViewName;

            AvailableViewNames.Clear(); // (!) A connected control will cause the CurrentView3D to be set to null, so we have to capture the previous3DViewName above.
            foreach (var viewName in viewNames)
            {
                AvailableViewNames.Add(viewName);
            }
            OnPropertyChanged(nameof(AvailableViewNames));

            // Synchronize the current 3D view.
            UserSettings.ViewName = AvailableViewNames.FirstOrDefault(v => v == previousViewName)
                ?? (!string.IsNullOrWhiteSpace(currentViewName)
                    ? AvailableViewNames.FirstOrDefault(v => v.Equals(currentViewName, StringComparison.InvariantCultureIgnoreCase)) ?? AvailableViewNames.FirstOrDefault()
                    : AvailableViewNames.FirstOrDefault());
        }

        private void UpdateAvailableView3Ds()
        {
            // Extract the view 3d's from the active document.
            var view3Ds =
                (GetAllView3ds(Document) ?? new List<View3D>())
                .Select(v => v.Name)
                .OrderBy(v => v);

            SetAvailableView3Ds(view3Ds, CommandData?.Application?.ActiveUIDocument?.ActiveView?.Name);
        }

        public static IEnumerable<View3D> GetAllView3ds(Document doc, bool includeTemplates = false)
            => GetElementsOfType<View3D>(doc).Where(view => !view.IsTemplate || includeTemplates);

        // https://thebuildingcoder.typepad.com/blog/2015/12/quick-slow-and-linq-element-filtering.html
        private static IEnumerable<T> GetElementsOfType<T>(Document doc) where T : Element
            => new FilteredElementCollector(doc).OfClass(typeof(T)).OfType<T>();

        //------------------------------------------------------------------------------
        // File Browsing
        //------------------------------------------------------------------------------

        public CustomCommand BrowseFileCommand { get; }

        private void BrowseFile()
        {
            using (var dialog = new SaveFileDialog
               {
                   Title = "Save As...",
                   DefaultExt = ".vim",
                   Filter = "VIM Files (*.vim)|*.vim",
                   OverwritePrompt = true,
                   ValidateNames = true,
                   FileName = SaveFileName,
                   InitialDirectory = UserSettings.VimSaveDirectory,
               })
            {
                var result = dialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    var directory = Directory.GetParent(dialog.FileName)?.FullName;
                    var filename = Path.GetFileName(dialog.FileName);
                    UserSettings.VimSaveDirectory = directory;
                    SaveFileName = filename;
                }
            }
        }

        private string _saveFileName;
        public string SaveFileName
        {
            get => _saveFileName;
            set
            {
                SetField(ref _saveFileName, value);
                UpdateCanExport();
            }
        }

        public string VimFilePath
            => Path.Combine(UserSettings.VimSaveDirectory, SaveFileName);

        public CustomCommand OpenFolderCommand { get; }

        private void OpenFolder()
        {
            try
            {
                IO.SelectFileInExplorer(VimFilePath);
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }
        }

        public CustomCommand OpenFileCommand { get; }

        private void OpenFile()
        {
            try
            {
                IO.OpenFile(VimFilePath);
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }
        }

        //------------------------------------------------------------------------------
        // Validation
        //------------------------------------------------------------------------------

        private bool _canExport;
        public bool CanExport
        {
            get => _canExport;
            set => SetField(ref _canExport, value);
        }

        /// <summary>
        /// Triggered when the user settings have been modified.
        /// </summary>
        private void UserSettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateCanExport();
        }

        /// <summary>
        /// Validates whether the user can export.
        /// </summary>
        private void UpdateCanExport()
        {
            var preflightErrorMessages = new List<string>();

            // Validate the save directory.
            if (string.IsNullOrWhiteSpace(UserSettings.VimSaveDirectory))
                preflightErrorMessages.Add("Save directory is empty.");

            // Validate the save filename.
            if (string.IsNullOrWhiteSpace(SaveFileName))
                preflightErrorMessages.Add("VIM file name is empty.");

            if (string.IsNullOrWhiteSpace(UserSettings.ViewName))
                preflightErrorMessages.Add("View name is empty.");

            CanExport = preflightErrorMessages.Count == 0;
            PreflightErrorMessage = CanExport
                ? ""
                : string.Join(Environment.NewLine, preflightErrorMessages);
        }

        //------------------------------------------------------------------------------
        // Visibility & Messaging
        //------------------------------------------------------------------------------

        private Visibility _preflightVisibility = Visibility.Visible;
        public Visibility PreflightVisibility
        {
            get => _preflightVisibility;
            set => SetField(ref _preflightVisibility, value);
        }

        private Visibility _processingVisibility = Visibility.Collapsed;
        public Visibility ProcessingVisibility
        {
            get => _processingVisibility;
            set => SetField(ref _processingVisibility, value);
        }

        private Visibility _finishedVisibility = Visibility.Collapsed;
        public Visibility FinishedVisibility
        {
            get => _finishedVisibility;
            set => SetField(ref _finishedVisibility, value);
        }

        private MainWindowVisualState _mainWindowVisualState;
        public MainWindowVisualState MainWindowVisualState
        {
            get => _mainWindowVisualState;
            set
            {
                SetField(ref _mainWindowVisualState, value);

                switch (_mainWindowVisualState)
                {
                    case MainWindowVisualState.Preflight:
                        PreflightVisibility = Visibility.Visible;
                        ProcessingVisibility = Visibility.Collapsed;
                        FinishedVisibility = Visibility.Collapsed;
                        break;
                    case MainWindowVisualState.Processing:
                        PreflightVisibility = Visibility.Collapsed;
                        ProcessingVisibility = Visibility.Visible;
                        FinishedVisibility = Visibility.Collapsed;
                        break;
                    case MainWindowVisualState.Finished:
                        PreflightVisibility = Visibility.Collapsed;
                        ProcessingVisibility = Visibility.Collapsed;
                        FinishedVisibility = Visibility.Visible;
                        break;
                }
            }
        }

        private string _preflightErrorMessage;
        public string PreflightErrorMessage
        {
            get => _preflightErrorMessage;
            set
            {
                SetField(ref _preflightErrorMessage, value);
                OnPropertyChanged(nameof(PreflightErrorMessageVisibility));
            }
        }

        public Visibility PreflightErrorMessageVisibility
            => string.IsNullOrWhiteSpace(PreflightErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

        private string _finishedTimeMessage;
        public string FinishedTimeMessage
        {
            get => _finishedTimeMessage;
            set => SetField(ref _finishedTimeMessage, value);
        }

        private string _finishedErrorMessage;
        public string FinishedErrorMessage
        {
            get => _finishedErrorMessage;
            set
            {
                SetField(ref _finishedErrorMessage, value);
                OnPropertyChanged(nameof(FinishedErrorMessageVisibility));
                OnPropertyChanged(nameof(FinishedSuccessMessageVisibility));
            }
        }

        public Visibility FinishedErrorMessageVisibility
            => string.IsNullOrWhiteSpace(FinishedErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility FinishedSuccessMessageVisibility
            => string.IsNullOrWhiteSpace(FinishedErrorMessage) ? Visibility.Visible : Visibility.Collapsed;

        public void Reset()
        {
            PreflightErrorMessage = "";
            FinishedErrorMessage = "";
            FinishedTimeMessage = "";
            MainWindowVisualState = MainWindowVisualState.Preflight;
        }

        //------------------------------------------------------------------------------
        // Export Logic
        //------------------------------------------------------------------------------

        public CustomCommand ExportVimCommand { get; }

        public void ExportVim()
        {
            using (var _ = _logger.LogDuration(nameof(ExportVim)))
            {
                MainWindowVisualState = MainWindowVisualState.Processing;

                Application.DoEvents(); // Explicitly call this to let Revit update the UI before it steps into the exporting process.

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var errorMessage = "";

                try
                {
                    var revitDocument = CommandData.Application.ActiveUIDocument.Document;
                    var outputVimFilePath = VimFilePath;
                    var viewName = UserSettings.ViewName;

                    var exportOptions = new ExportOptions
                    {
                        View = viewName,
                        // TODO: Modify how your VIM license is obtained.
                        // In this sample, it is stored as a text resource in your plugin but this may cause problems when the license expires.
                        // As an alternative, you can implement a downloading mechanism to fetch the license from your server.
                        VimLicenseString = Properties.Resources.vim_license
                    };

                    Vim.Revit.Core.Exporter.Export(revitDocument, outputVimFilePath, exportOptions);

                    errorMessage = File.Exists(outputVimFilePath) ? "" : "VIM file not found.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                    errorMessage = $"Error: {ex.Message}";
                }
                finally
                {
                    FinishExport(stopwatch.Elapsed, errorMessage);
                }
            }
        }

        public void FinishExport(TimeSpan elapsedTime, string errorMessage)
        {
            FinishedTimeMessage =
                elapsedTime.ToString(Math.Floor(elapsedTime.TotalSeconds) > 0 ? @"hh\:mm\:ss" : @"hh\:mm\:ss\.ff");
            FinishedErrorMessage = errorMessage;
            MainWindowVisualState = MainWindowVisualState.Finished;
        }
    }
}