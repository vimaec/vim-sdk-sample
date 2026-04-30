# VIM Revit Custom Exporter

The instructions in this file pertain to running the Visual Studio 2026 solution `VIM Revit Custom Exporter.slnx`. This solution illustrates how to use the same codebase to create yearly Revit plugins using C# and WPF. The code is structured as a simple WPF View & ViewModel plugin which exports a VIM file from the current Revit document and lets the user choose where to save the file and which 3D view to export.

## Requirements

Before proceeding, please ensure you have met the following criteria:

- Autodesk Revit should be installed on your machine for testing purposes.

- If you have a software development partnership with VIM, please contact us to download the NuGet packages `Vim.Revit.Core.$YearVersion$.x.x.x.nupkg` (one for each supported version of Revit). These packages contain the VIM libraries required to export VIM files from Revit.

  - Copy these .nupkg files into the `nuget_local` folder as follows: `Vim.Revit.Core.$YearVersion$.x.x.x.nupkg`

  - The `nuget_local` directory is referenced by the [nuget.config](./nuget.config) file at the root of this repository. This allows the solution to use the `nuget_local` folder when resolving packages.

- As part of your software development partnership with VIM, you will also be granted a **VIM license key** which you will need to successfully export a VIM file using the provided libraries.

  - To get started, replace the contents of [VIM Revit Custom Exporter/Custom.Exporter/Resources/vim_license.txt](./VIM%20Revit%20Custom%20Exporter/Custom.Exporter/Resources/vim_license.txt) with this license key.

  - You may modify the code to adapt to your own VIM license key distribution process, for example to download the key from a central server you manage prior to exporting. In this sample code, the license key is embedded as a resource into the application itself. The main disadvantage to this approach is that you would need to re-compile the application and distribute a new version to your users before the embedded VIM license key expires.

## Usage

To successfully build the solution `VIM Revit Custom Exporter.slnx`:

- Ensure the requirements above have been met.

- Open `VIM Revit Custom Exporter.slnx` in Visual Studio 2026.

- Click on "Build" > "Build Solution".

- The projects contained in the solution should build successfully.

  - The yearly Revit plugin binary files will be copied to the user location `%USERPROFILE%\AppData\Roaming\Autodesk\Revit\Addins\$YearVersion$`.
  
    - This location is used for development purposes and is prioritized by Revit when it attempts to load plugins.

    - As mentioned in the Installer Notes below, publishing your plugin files under `C:\ProgramData\Autodesk\Revit\Addins\$YearVersion$` will make it available to all Revit users on that machine.

    - **Important**: If this Custom Exporter and the standard VIM Revit Exporter plugin are both installed simultaneously in the Revit Addins folder, you may encounter assembly loading issues if their underlying assembly versions do not match exactly.

- Open Revit and accept to load the "Custom Exporter" plugin and its command(s) when prompted.

- Open a Revit file to test with.

- In the ribbon, click on "Custom Exporter" > "Make a VIM"

  - Choose where you want to save your VIM file.

  - Choose the 3D view you want to export into the VIM file.

  - Click on "START"

  - The VIM file should successfully export.

  - If you encounter a licensing error, make sure you have correctly updated the contents of `VIM Revit Custom Exporter/Custom.Exporter/Resources/vim_license.txt` with the VIM license key we have provided.

- Customize the plugin to make it your own (see below).

## Making it Your Own

To customize the VIM Revit exporter plugin in this solution:

- Update the GUIDs (you can use https://guidgenerator.com/).
  - In `Custom.Exporter.addin`, generate new GUIDs for the `AddinIds` elements.
  - In `Properties/AssemblyInfo.cs`, generate a new Guid.

- Search and replace the following strings among the filenames and files in this folder. **Do not** perform a blanket search and replace; this might produce unexpected issues. We recommend going file-by-file.
  - `Custom.Exporter`
    - Depending on your own plugin's assembly name, you will need to update the usages of `/Custom.Expoter;component/...` in the .XAML files to match the name of your assembly. Your resources (images, icons, style dictionaries) will fail to load if these occurrences do not match the name of your assembly.
  - `CustomExporter`
  - `Custom Exporter`
  - `Custom_Exporter`
  - `com.custom-exporter-vendor-id`
  - `custom-exporter-company`

- Update the color scheme and styles defined in `StyleResources.xaml`

- Update graphics
  - `Resources/launch.png`: used as the ribon icon (32x32 pixel png) in Revit.
  - `Resources/in_progress.png`: displayed while the plugin is exporting.

## Installer Notes

When creating your own plugin installer (not included in this sample), you will need to ensure the following:

- Make sure you code-sign the DLL files(s) referenced by the `.addin` file, preferably with an extended validation (EV) code signing certificate.

- To install the plugin for all users on a machine, ensure the `.addin` and its DLL folder are installed under `C:\ProgramData\Autodesk\Revit\Addins\$YearVersion$`

## Code Structure

### C# Project Structure

To ensure the same plugin code can be used across different versions of Revit, we opted to define multiple versions of `Custom.Exporter.v$YearVersion$.csproj` alongside the same collection of source files. There are a few details worth mentioning with this setup:

- The file `VIM Revit Custom Exporter/Custom.Exporter/Directory.Build.props` is necessary to make this whole project structure work properly:

  - It implements the `<YearVersion>` detection mechanism, which is based on the .csproj file's naming scheme. Specifically, .csproj files names containing strings like "v2024" will have the `<YearVersion>` variable assigned to `2024`, and so on. This is necessary to correctly reference the required dependencies for that version of Revit (both in `VIM Revit Custom Exporter/lib/$YearVersion$` and the `VIM.Revit.Core.$YearVersion$` local NuGet package reference)

  - It implements the `<PropertyGroup Label="obj path redirect" ...`, which re-paths the `obj` folder with the `YearVersion`. This is necessary to make sure that neighboring .csproj files maintain their own subfolder in the `obj` folder.

- Each .csproj file defines its .NET `<TargetFramework>` and imports the same files:
  - `Custom.Exporter._Common.Build.props`: defines the properties which vary per year.
  - `Custom.Exporter._References.Build.props`: defines the references which vary per year and per .NET version.
  - `Custom.Exporter._Targets.Build.targets`: defines the post-build targets which vary per year.

  Example .csproj file (Custom.Exporter.v2025.csproj):
  ```
  <Project Sdk="Microsoft.NET.Sdk">

    <Import Project=".\Custom.Exporter._Common.Build.props" />

    <PropertyGroup>
      <TargetFramework>net8.0-windows</TargetFramework>
    </PropertyGroup>

    <Import Project=".\Custom.Exporter._References.Build.props" />

    <Import Project=".\Custom.Exporter._Targets.Build.targets" />

  </Project>
  ```

### C# Code Structure

The following is a high-level summary of the main C# files involved in this plugin:

- Custom.Exporter.addin: File copied to `%USERPROFILE%\AppData\Roaming\Autodesk\Revit\Addins\$YearVersion$` upon project build and detected by Revit to bootstrap the plugin's loading process.

- Plugin/
  - Plugin.cs: The entrypoint of the plugin in Revit. Referenced by Custom.Exporter.addin
  - PluginCommands.cs: A list of commands included in the plugin. Referenced by Custom.Exporter.addin.
  - PluginRibbon.cs: Defines the plugin's ribbon interface in Revit.

- Properties/
  - Resources.resx: Registers the resources available to the plugin.

- Resources/: Contains the resources (images, fonts, etc) used by the plugin.
  - vim_license.txt: Must be updated to contain your valid VIM license key.

- Views/
  - MainWindow.xaml|.xaml.cs: The WPF window which defines the plugin user interface.
  - WidgetTests.xaml|.xaml.cs: A user control which can be used to test various widgets.

- ViewModels/
  - MainWindowViewModel.cs: The logic which runs in the MainWindow.
  - UserSettings.cs: Wraps the serializable JSON user settings with change notification events.

- StyleResources.xaml: Styling for WPF components
