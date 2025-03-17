using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Vim.Util.Logging;

namespace Custom.Exporter.ViewModels
{
    public class CustomExporterViewModel : NotifyPropertyChanged
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        public CustomExporterViewModel(ILogger logger)
        {
            _logger = logger;
            UserSettings = new CustomExporterUserSettings(_logger);
        }

        public CustomExporterUserSettings UserSettings { get; }

        public void Save()
            => UserSettings.Save();

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
            set => SetField(ref _saveFileName, value);
        }

        public void SetCommandData(ExternalCommandData commandData)
        {
            CommandData = commandData;
            DocumentName = commandData.Application.ActiveUIDocument.Document.Title;
            SaveFileName = $"{DocumentName}.vim";
        }

        public void Reset()
        {

        }
    }
}