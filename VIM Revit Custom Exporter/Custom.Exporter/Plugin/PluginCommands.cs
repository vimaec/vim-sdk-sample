using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Vim.Util;

namespace Custom.Exporter
{
    //-------------------------------------------------------------------------------------------
    // Export Window Command - Custom.Exporter.addin > <FullClassName>Custom.Exporter.PluginWindowCommand</FullClassName>
    //-------------------------------------------------------------------------------------------

    // ReSharper disable once UnusedMember.Global
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public sealed class PluginWindowCommand : IExternalCommand
    {
        /// <summary>
        /// The entry point for Revit's command system.
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Plugin.ShowExporterDialog(commandData);
                return Result.Succeeded;
            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }
        }
    }
}
