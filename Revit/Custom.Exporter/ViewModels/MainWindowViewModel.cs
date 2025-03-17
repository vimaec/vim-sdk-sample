using Autodesk.Revit.UI;
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
        }

        public UserSettings UserSettings { get; }

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
            // TODO
        }
    }
}