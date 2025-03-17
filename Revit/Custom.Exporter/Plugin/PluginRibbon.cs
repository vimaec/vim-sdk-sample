using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using Autodesk.Revit.UI;

namespace Custom.Exporter
{
    internal static class PluginRibbon
    {
        internal static readonly string AssemblyPath = typeof(PluginRibbon).Assembly.Location;

        internal static BitmapSource ToBitmapSource(this Bitmap bitmap)
            => Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

        internal static void Create(UIControlledApplication application)
        {
            const string tabName = "Custom Exporter";
            application.CreateRibbonTab(tabName);

            var exportPanel = application.CreateRibbonPanel(tabName, "Export");

            if (!(exportPanel.AddItem(new PushButtonData(
                "Export a VIM file",
                "Make a VIM",
                AssemblyPath,
                typeof(PluginWindowCommand).FullName)) is PushButton createButton))
            {
                throw new NullReferenceException();
            }

            var createImage = Resources.launch.ToBitmapSource();
            createButton.LargeImage = createImage;
            createButton.Image = createImage;
        }
    }
}
