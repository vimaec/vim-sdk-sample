using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Vim.Format.ObjectModel;
using Vim.LinqArray;
using Vim.Util;
using VimTools;

namespace Vim.Sdk.Samples;

[TestFixture]
public static class VimClashTests
{
    [Test]
    public static void TestVimClash()
    {
        var logDir = Path.Combine("vim_tool_logs");
        IO.CreateAndClearDirectory(logDir);

        var vimFilePath = VimTestUtils.RoomTestPath;

        //----------------------------------------------------------------
        // 1. Load the VIM file into managed memory to create a set of
        //    element indices to clash.
        //----------------------------------------------------------------

        var vim = VimScene.LoadVim(vimFilePath);
        var dm = vim.DocumentModel;

        var wallsBuiltInCategory = RevitBuiltInCategory.OST_Walls.ToString("G");
        var ceilingsBuiltInCategory = RevitBuiltInCategory.OST_Ceilings.ToString("G");
        var floorsBuiltInCategory = RevitBuiltInCategory.OST_Floors.ToString("G");

        var walls = dm.ElementList
            .Where(e => e.Category?.BuiltInCategory == wallsBuiltInCategory)
            .Select(e => e.Index)
            .ToArray();
        Assert.IsNotEmpty(walls);

        var ceilingsOrFloors = dm.ElementList
            .Where(e =>
            {
                var builtInCategory = e.Category?.BuiltInCategory;
                return builtInCategory == ceilingsBuiltInCategory || builtInCategory == floorsBuiltInCategory;
            })
            .Select(e => e.Index)
            .ToArray();
        Assert.IsNotEmpty(ceilingsOrFloors);

        //----------------------------------------------------------------
        // 2. Initialize the native C++ VIM Tools API.
        //----------------------------------------------------------------

        var initResult = Api.InitializeApi(logDir);
        AssertSuccess(initResult);

        //----------------------------------------------------------------
        // 3. Load the VIM file into the C++ VIM Tools API.
        //----------------------------------------------------------------

        var vimHandle = VimHandle.Invalid;
        var loadResult = Api.LoadVim(vimFilePath, ref vimHandle);
        AssertSuccess(loadResult);

        //----------------------------------------------------------------
        // 4. Run the clash on the elements.
        //----------------------------------------------------------------

        var clashResults = Array.Empty<ClashResult>();
        var clashResult = Api.VimClash(vimHandle, walls, ceilingsOrFloors, 0f, ref clashResults);
        AssertSuccess(clashResult);

        Assert.IsNotEmpty(clashResults);
        foreach (var item in clashResults)
        {
            Assert.That(item.mElementA.mVimIndex, Is.EqualTo(0));
            var elementA = dm.ElementList[(int)item.mElementA.mElementIndex];
            Assert.That(elementA.Category.BuiltInCategory, Is.EqualTo(wallsBuiltInCategory));

            Assert.That(item.mElementB.mVimIndex, Is.EqualTo(0));
            var elementB = dm.ElementList[(int)item.mElementB.mElementIndex];
            var elementBBuiltInCategory = elementB.Category.BuiltInCategory;
            Assert.IsTrue(elementBBuiltInCategory == ceilingsBuiltInCategory || elementBBuiltInCategory == floorsBuiltInCategory);
        }

        //----------------------------------------------------------------
        // 5. Cleanup: dispose the loaded VIM file in the C++ VIM Tools API.
        //----------------------------------------------------------------

        var disposeResult = Api.DisposeVim(vimHandle);
        AssertSuccess(disposeResult);

        //----------------------------------------------------------------
        // 6. Cleanup: dispose the native C++ VIM Tools API.
        //----------------------------------------------------------------

        var shutdownResult = Api.DisposeApi();
        AssertSuccess(shutdownResult);
    }

    private static void AssertSuccess(ErrorCodes errorCode)
        => Assert.That(errorCode, Is.EqualTo(ErrorCodes.Success));
}
