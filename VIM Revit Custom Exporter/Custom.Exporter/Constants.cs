using System;
using System.IO;
//using Vim.Revit.Core;

namespace Custom.Exporter
{
    public static class Constants
    {
        /// <summary>
        /// The path to the temporary exporter directory.
        /// NOTE: This avoids the direct usage of Path.GetTempPath() because in Revit 2020, the Revit developers had the wise idea to append a custom GUID to the returned path.
        /// See: https://stackoverflow.com/q/56984043
        /// </summary>
        public static string TempPluginDir
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp",
                "Custom Exporter");

        /// <summary>
        /// Returns the log filepath based on the given Revit year version.
        /// </summary>
        public static string GetLogFilepath(string revitYearVersion)
            => Path.Combine(TempPluginDir, "Logs", $"CustomExporter-{revitYearVersion}.log");


        //public static string LocalAppDataPluginYearDir
        //    => Path.Combine(
        //        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        //        "Custom Exporter",
        //        RevitConstants.RevitYearVersion);

        //public static string UserSettingsPath => Path.Combine(LocalAppDataPluginYearDir, "settings.json");

        /// <summary>
        /// "My Documents/Custom Exporter" directory
        /// </summary>
        public static string MyDocumentsPluginDir
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Custom Exporter");
    }
}