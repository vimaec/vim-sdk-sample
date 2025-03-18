using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Autodesk.Revit.UI;
using Vim.Revit.Core;
using Vim.Util.Logging;

namespace Custom.Exporter.ViewModels
{
    public class MainWindowViewModel : NotifyPropertyChanged
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindowViewModel(ILogger logger)
        {
            _logger = logger;
            UserSettings = new UserSettings(_logger);
            ExportVimCommand = CustomCommand.Create(ExportVim);
            UserSettings.PropertyChanged += UserSettingsOnPropertyChanged;
            Reset();
        }

        public UserSettings UserSettings { get; }

        public void Save() => UserSettings.Save();

        public void SetCommandData(ExternalCommandData commandData)
        {
            CommandData = commandData;
            DocumentName = commandData.Application.ActiveUIDocument.Document.Title;
            SaveFileName = $"{DocumentName}.vim";

            Reset();
            UpdateCanExport();
        }

        private ExternalCommandData _commandData;
        public ExternalCommandData CommandData
        {
            get => _commandData;
            set => SetField(ref _commandData, value);
        }

        private string _documentName;
        public string DocumentName
        {
            get => _documentName;
            set => SetField(ref _documentName, value);
        }

        private string _saveFileName;
        public string SaveFileName
        {
            get => _saveFileName;
            set
            {
                if (SetField(ref _saveFileName, value))
                    UpdateCanExport();
            } 
        }

        public void Reset()
        {
            PreflightVisibility = Visibility.Visible;
            ProcessingVisibility = Visibility.Collapsed;
            FinishedVisibility = Visibility.Collapsed;
            PreflightErrorMessage = "";
            FinishedSuccessMessageVisibility = Visibility.Collapsed;
            FinishedErrorMessageVisibility = Visibility.Collapsed;
            FinishedErrorMessage = "";
        }

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

        public string VimFilePath
            => Path.Combine(UserSettings.VimSaveDirectory, SaveFileName);

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

        private string _preflightErrorMessage;
        public string PreflightErrorMessage
        {
            get => _preflightErrorMessage;
            set
            {
                if (SetField(ref _preflightErrorMessage, value))
                    OnPropertyChanged(nameof(PreflightErrorMessageVisibility));
            }
        }
        public Visibility PreflightErrorMessageVisibility
            => string.IsNullOrWhiteSpace(PreflightErrorMessage) ? Visibility.Collapsed : Visibility.Visible;


        public CustomCommand ExportVimCommand { get; }

        public void ExportVim()
        {
            using (var _ = _logger.LogDuration(nameof(ExportVim)))
            {
                try
                {
                    var revitDocument = CommandData.Application.ActiveUIDocument.Document;
                    var vimFilePath = VimFilePath;
                    var viewName = UserSettings.ViewName;

                    PreflightVisibility = Visibility.Collapsed;
                    ProcessingVisibility = Visibility.Visible;

                    var exportOptions = new ExportOptions
                    {
                        View = viewName,
                        // TODO: Modify how your VIM license is obtained.
                        // In this sample, it is stored as a text resource in your plugin but this may cause problems when the license expires.
                        // As an alternative, you can implement a downloading mechanism to fetch the license from your server.
                        VimLicenseString = Properties.Resources.vim_license 
                    };

                    Vim.Revit.Core.Exporter.Export(revitDocument, vimFilePath, exportOptions);

                    ProcessingVisibility = Visibility.Collapsed;
                    FinishedVisibility = Visibility.Visible;

                    // TODO: make a little state machine for the exporter visual states...

                    var fileExists = File.Exists(vimFilePath);
                    FinishExport(fileExists, "");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                    FinishExport(false, ex.Message);
                }
            }
        }

        private string _finishedErrorMessage;
        public string FinishedErrorMessage
        {
            get => _finishedErrorMessage;
            set => SetField(ref _finishedErrorMessage, value);
        }

        private Visibility _finishedErrorMessageVisibility;
        public Visibility FinishedErrorMessageVisibility
        {
            get => _finishedErrorMessageVisibility;
            set => SetField(ref _finishedErrorMessageVisibility, value);
        }

        private Visibility _finishedSuccessVisibility;
        public Visibility FinishedSuccessMessageVisibility
        {
            get => _finishedSuccessVisibility;
            set => SetField(ref _finishedSuccessVisibility, value);
        }

        public void FinishExport(bool isSuccess, string errorMessage)
        {
            FinishedSuccessMessageVisibility = isSuccess ? Visibility.Visible : Visibility.Collapsed;
            FinishedErrorMessageVisibility = isSuccess ? Visibility.Collapsed : Visibility.Visible;
            FinishedErrorMessage = errorMessage;
        }
    }
}