using Vim.Util.Logging;
using Vim.Util.Logging.Serilog;

namespace Custom.Exporter
{
    public static class Logging
    {
        /// <summary>
        /// Initializes logging for the application.
        /// </summary>
        public static ILogger Initialize(string revitYearVersion)
            => Log.Init("Custom Exporter", Constants.GetLogFilepath(revitYearVersion), true, true);
    }
}