using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Vim.Sql;
using Vim.Util.Logging;
using Vim.Util.Logging.Serilog;

namespace Vim.Sdk.Samples;

/// <summary>
/// The following tests assume a SQL Express database is installed on the local machine.
/// </summary>
[TestFixture]
public static class VimSqlTests
{
    /// <summary>
    /// Sanity check: we can open the VIM file bundled with this package.
    /// </summary>
    [Test]
    public static void TestVimFileOpen()
    {
        var vim = VimScene.LoadVim("RoomTest.vim"); // Note: RoomTest.vim is copied to the output directory.
        Assert.IsTrue(vim.DocumentModel.NumElement > 0);
    }

    /// <summary>
    /// Inserts a VIM file into a local SQL Server Express database.
    /// </summary>
    [Test]
    public static async Task TestVimSqlInsert()
    {
        // Database connection string information (this example assumes we are using a local SQL express database)
        var dbName = "vim-sql-sample-insert";
        var connectionString = @$"Data Source=localhost\SQLEXPRESS;Initial Catalog={dbName};Integrated Security=SSPI;TrustServerCertificate=True;";

        // The log file is stored at "bin\$(Configuration)\net6.0\TestVimSqlInsert.log"
        var logFilePath = $"{nameof(TestVimSqlInsert)}.log";
        Util.IO.Delete(logFilePath); // Reset the log file.
        var logger = Log.CreateLogger(nameof(TestVimSqlInsert), logFilePath); 

        // Create a progress logger.
        var progress = new Progress<string>(p => logger.LogInformation(p));

        try
        {
            // Instantiate the VIM SQL service.
            var vimSqlService = new VimSqlService(
                connectionString,
                false // false: uses the '[dbo]' schema for our VIM SQL tables. true: uses the '[vimsql]' schema instead.
            );

            // Migrate the database (if required)
            var vimSqlSchemaVersion = await vimSqlService.GetDatabaseSchemaVersionAsync(logger, progress);
            if (string.IsNullOrWhiteSpace(vimSqlSchemaVersion))
            {
                // VIM SQL schema version not detected - migrate to latest.
                //
                // SPECIAL NOTES:
                // - If you are using SQL Server Express, the database will be created automatically here if it does not already exist.
                // - Otherwise, you will need to ensure the database already exists before attempting to migrate it.
                await vimSqlService.MigrateAsync(null, logger, progress);
            }
            else if (vimSqlService.CanMigrateToLatest(vimSqlSchemaVersion, out var vimSqlSchemaVersionLatest))
            {
                // VIM SQL schema version can be upgraded to latest.
                await vimSqlService.MigrateAsync(vimSqlSchemaVersionLatest, logger, progress);
            }

            // Delete any pre-existing VIM files in the database. For optimal results, use one database per VIM file.
            await vimSqlService.TruncateVimTablesAsync(logger, progress);

            // Insert the VIM file into the database.
            await vimSqlService.InsertVimAsync("RoomTest.vim", 4, logger, progress);

            // The code below illustrates a few SQL queries on the database.

            // SQL SAMPLE: Basic Element information from the [dbo].[Element] table
            {
                logger.LogInformation("------------------------------------------------------------");
                logger.LogInformation("BASIC ELEMENT INFORMATION");
                logger.LogInformation("------------------------------------------------------------");
                var sqlCommand = @"
SELECT
  [_key],
  [Index],
  [Id],
  [UniqueId],
  [Name]
FROM [dbo].[Element];";

                await ExecuteCommand(connectionString, sqlCommand, reader =>
                    logger.LogInformation(
                        $"Key: {SafeRead<long>(reader, 0)}, Index: {SafeRead<int>(reader, 1)}, Id: {SafeRead<long>(reader, 2)}, UniqueId: {SafeRead<string>(reader, 3)}, Name: {SafeRead<string>(reader, 4)}"));
            }

            // SQL SAMPLE: Join various tables to surface information for each Family Instance element.
            {
                logger.LogInformation("------------------------------------------------------------");
                logger.LogInformation("FAMILY INSTANCE INFORMATION");
                logger.LogInformation("------------------------------------------------------------");
                var sqlCommand = @"
SELECT
  FamilyInstance_Element.Id AS Element_Id,
  BimDocument.Title AS BimDocument_Title,
  FamilyInstance_Element.Name AS FamilyInstance_Element_Name,
  FamilyType_Element.Name AS FamilyType_Element_Name,
  Family_Element.Name as Family_Element_Name,
  Category.Name as Category_Name,
  Level_Element.Name AS Level_Element_Name,
  Level.Elevation AS Level_Elevation,
  Room_Element.Name AS Room_Element_Name,
  Room.Number AS Room_Number,
  Room.Area AS Room_Area
FROM
     [dbo].[FamilyInstance] AS FamilyInstance
JOIN [dbo].[Element] AS FamilyInstance_Element ON FamilyInstance_Element._key = FamilyInstance.Element
JOIN [dbo].[FamilyType] AS FamilyType ON FamilyType._key = FamilyInstance.FamilyType
JOIN [dbo].[Element] AS FamilyType_Element ON FamilyType_Element._key = FamilyType.Element
JOIN [dbo].[Family] as Family ON Family._key = FamilyType.Family
JOIN [dbo].[Element] AS Family_Element ON Family_Element._key = Family.Element
JOIN [dbo].[Category] AS Category ON Category._key = FamilyInstance_Element.Category
JOIN [dbo].[BimDocument] AS BimDocument ON BimDocument._key = FamilyInstance_Element.BimDocument
JOIN [dbo].[Room] AS Room ON Room._key = FamilyInstance_Element.Room
JOIN [dbo].[Element] as Room_Element ON Room_Element._key = Room.Element
JOIN [dbo].[Level] AS Level on Level._key = FamilyInstance_Element.Level
JOIN [dbo].[Element] AS Level_Element on Level_Element._key = Level.Element
ORDER BY Level.Elevation, Room_Number, Category_Name;
";
                await ExecuteCommand(connectionString, sqlCommand, reader =>
                    logger.LogInformation(
                        @$"Element_Id: {SafeRead<long>(reader, 0)}
  BimDocument_Title: {SafeRead<string>(reader, 1)}
  Name: {SafeRead<string>(reader, 2)}
  Family Type: {SafeRead<string>(reader, 3)}
  Family: {SafeRead<string>(reader, 4)}
  Category: {SafeRead<string>(reader, 5)}
  Level: {SafeRead<string>(reader, 6)}
  Level Elevation: {SafeRead<double>(reader, 7)}
  Room Name: {SafeRead<string>(reader, 8)}
  Room Number: {SafeRead<string>(reader, 9)}
  Room Area: {SafeRead<double>(reader, 10)}"));
            }

            // SQL SAMPLE: Extract Family Instance elements whose Family Type parameters contain the string 'thermal' in its name
            {
                logger.LogInformation("------------------------------------------------------------");
                logger.LogInformation("FAMILY INSTANCES WITH A FAMILY TYPE PARAMETER CONTAINING 'thermal'");
                logger.LogInformation("------------------------------------------------------------");

                var sqlCommand = @"
SELECT
  FamilyInstance_Element._key AS Element_key,
  FamilyInstance_Element.UniqueId AS Element_UniqueId,
  ParameterDescriptor.Name AS Parameter_Name,
  FamilyType_Element_Parameter.Value AS Parameter_Value,
  FamilyType_Element_Parameter.DisplayValue AS Parameter_DisplayValue
FROM
     [dbo].[FamilyInstance] AS FamilyInstance
JOIN [dbo].[Element] AS FamilyInstance_Element ON FamilyInstance_Element._key = FamilyInstance.Element
JOIN [dbo].[FamilyType] AS FamilyType ON FamilyType._key = FamilyInstance.FamilyType
JOIN [dbo].[Element] AS FamilyType_Element ON FamilyType_Element._key = FamilyType.Element
JOIN [dbo].[Parameter] AS FamilyType_Element_Parameter ON FamilyType_Element_Parameter.Element = FamilyType_Element._key
JOIN [dbo].[ParameterDescriptor] as ParameterDescriptor ON ParameterDescriptor._key = FamilyType_Element_Parameter.ParameterDescriptor
WHERE LOWER(ParameterDescriptor.Name) LIKE LOWER('%thermal%')
ORDER BY FamilyInstance_Element._key;";

                await ExecuteCommand(connectionString, sqlCommand, reader =>
                    logger.LogInformation(@$"Element Key: {SafeRead<long>(reader, 0)}
  Element UniqueId: {SafeRead<string>(reader, 1)}
  Parameter Name: {SafeRead<string>(reader, 2)}
  Parameter Value (raw): {SafeRead<string>(reader, 3)}
  Parameter DisplayValue: {SafeRead<string>(reader, 4)}
"));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            Assert.Fail(ex.Message);
        }
    }

    private static string SafeRead<T>(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return "";

        var type = typeof(T);

        try
        {
            if (type == typeof(int))
            {
                return reader.GetInt32(ordinal).ToString();
            }
            else if (type == typeof(long))
            {
                return reader.GetInt64(ordinal).ToString();
            }
            else if (type == typeof(float))
            {
                return reader.GetFloat(ordinal).ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (type == typeof(double))
            {
                return reader.GetDouble(ordinal).ToString(NumberFormatInfo.InvariantInfo);
            }
            else
            {
                return reader.GetString(ordinal);
            }
        }
        catch (Exception)
        {
            // do nothing
            return "";
        }
    }

    private static async Task ExecuteCommand(string connectionString, string sqlCommand, Action<SqlDataReader> onReadAction)
    {
        await using var sqlConnection = new SqlConnection(connectionString);
        await sqlConnection.OpenAsync();

        await using var command = sqlConnection.CreateCommand();
        command.CommandText = sqlCommand;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            onReadAction(reader);
        }
    }
}