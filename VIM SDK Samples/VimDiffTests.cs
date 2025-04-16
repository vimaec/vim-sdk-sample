using System.Linq;
using NUnit.Framework;
using Vim.Format;
using Vim.Diff;
using Vim.Util.Logging.Serilog;

namespace Vim.Sdk.Samples;

[TestFixture]
public static class VimDiffTests
{
    [Test]
    public static void TestVimDiff()
    {
        // Note: these VIM files are copied to the output directory.
        var vimA = VimScene.LoadVim("vim/RoomTest.vim"); 
        var vimB = VimScene.LoadVim("vim/RoomTestModified.vim");

        // Perform the actual diff calculation.
        var diff = VimDiffService.Diff(vimA, vimB, new VimDiffOptions(CompareGeometry: true, CompareParameters: true));

        // Log the diff results
        var logFilePath = $"{nameof(TestVimDiff)}.log";
        Util.IO.Delete(logFilePath); // Reset the log file.
        var logger = Log.CreateLogger(nameof(TestVimDiff), logFilePath);

        //----------------------------------------------------------------
        // LOG REMOVED
        //----------------------------------------------------------------
        var removed = diff.Removed;
        logger.Log($"[{removed.DiffType:G}: {removed.Count}]");
        foreach (var item in removed.DiffElements)
            logger.Log($"-- {DiffElementAsString(item)}");

        logger.Log("");

        //----------------------------------------------------------------
        // LOG ADDED
        //----------------------------------------------------------------
        var added = diff.Added;
        logger.Log($"[{added.DiffType:G}: {added.Count}]");
        foreach (var item in added.DiffElements)
            logger.Log($"++ {DiffElementAsString(item)}");

        logger.Log("");

        //----------------------------------------------------------------
        // LOG MODIFIED
        //----------------------------------------------------------------
        var modified = diff.Modified;
        logger.Log($"[{modified.DiffType:G}: {modified.Count}]");

        // Group the modifications.
        var groupedModifications = modified.DiffElementComparisons
            .GroupBy(d => (d.HasModifiedGeometry, d.HasModifiedParameters))
            .SelectMany(g => g);

        foreach (var mod in groupedModifications)
        {
            var annotation = string.Join(" ",
                mod.HasModifiedGeometry 
                    ? "🔸 geo" // (orange lozenge)
                    : "      ",
                mod.HasModifiedParameters
                    ? "🔹 params" // (blue lozenge)
                    : "         "
            );

            logger.Log($"~~ [{annotation}] {DiffElementAsString(mod.DiffElement_B)}"); // we use the "B" element's string representation for simplicity.
            foreach (var p in mod.ModifiedParameters)
            {
                var (nativeValueA, displayValueA) = p.Parameter_A?.Values ?? ("", "");
                var (nativeValueB, displayValueB) = p.Parameter_B?.Values ?? ("", "");

                logger.Log($"  '{p.ParameterKey.Name}' ({p.ParameterKey.Group})");
                logger.Log($"    - ({nativeValueA}, {displayValueA})");
                logger.Log($"    + ({nativeValueB}, {displayValueB})");
            }
        }

        //----------------------------------------------------------------
        // CREATE A VIM CONTAINING ONLY REMOVED ITEMS
        //----------------------------------------------------------------
        var elementIndicesToKeep = removed.DiffElements.Select(e => e.ElementIndex).ToHashSet();
        var transformService = new TransformService(nameof(VimDiffTests), "0.0.0");
        var documentBuilder = transformService.Filter(vimA, n => elementIndicesToKeep.Contains(n.ElementIndex));
        documentBuilder.Write("removed_elements.vim");
    }

    public static string DiffElementAsString(DiffElement d)
        => $"'{d.BimDocumentFileName}' > '{d.CategoryName}' {(string.IsNullOrEmpty(d.CategoryBuiltInName) ? "" : $"({d.CategoryBuiltInName})")} > '{d.FamilyName}' > '{d.FamilyTypeName}' > '{d.ElementName}' ('{d.ElementId}', '{d.ElementUniqueId}')";
}