using System;
using System.IO;
using Custom.Exporter.ViewModels;
using Newtonsoft.Json;
using Vim.Util;
using Vim.Util.Logging;

namespace Custom.Exporter
{
    /// <summary>
    /// JSON-serializable POCO object (plain-old-class-object).
    /// </summary>
    public class PocoUserSettings
    {
        public string VimSaveDirectory { get; set; } = Constants.MyDocumentsPluginDir;
    }

    /// <summary>
    /// A NotifyPropertyChanged wrapper around the PocoUserSettings
    /// </summary>
    public class UserSettings : NotifyPropertyChanged
    {
        private readonly ILogger _logger;
        private readonly string _settingsFilePath;
        private readonly PocoUserSettings _pocoUserSettings;

        /// <summary>
        /// Constructor. Loads the user settings.
        /// </summary>
        public UserSettings(ILogger logger)
        {
            _logger = logger;
            _settingsFilePath = Constants.UserSettingsPath;
            _pocoUserSettings = Load();
        }
        
        /// <summary>
        /// Loads the user settings.
        /// </summary>
        private PocoUserSettings Load()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new PocoUserSettings();
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonConvert.DeserializeObject<PocoUserSettings>(json) ?? new PocoUserSettings();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }

            return new PocoUserSettings();

        }

        /// <summary>
        /// Saves the user settings.
        /// </summary>
        public void Save()
        {
            if (string.IsNullOrWhiteSpace(_settingsFilePath))
                return;

            try
            {
                IO.CreateFileDirectory(_settingsFilePath);
                var json = JsonConvert.SerializeObject(_pocoUserSettings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        /// <summary>
        /// The save directory in which VIM files will be saved.
        /// </summary>
        public string VimSaveDirectory
        {
            get => _pocoUserSettings.VimSaveDirectory;
            set
            {
                if (_pocoUserSettings.VimSaveDirectory == value)
                    return;

                _pocoUserSettings.VimSaveDirectory = value;

                Save(); // save on change

                OnPropertyChanged();
            }
        }
    }
}
