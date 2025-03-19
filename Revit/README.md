# Custom Revit Exporter Sample

This directory contains a sample Revit VIM Exporter project which targets multiple versions of Revit using the same codebase.

The project is a simple WPF plugin for Revit which exports a VIM file.

## Requirements

To get started with the Revit Exporter Sample, you will need:
- `Vim.Revit.Core.$YearVersion.x.x.nupkg` for each version of Revit you support. Please contact us for these packages as they are not freely distributed via NuGet.
- A valid **VIM license key** which we provide as part of our software development partnership agreement.
- Autodesk Revit installed on your developer machine for testing purposes.

## Usage

Usage instructions:

- To successfully build the projects in `Vim.Sdk.Sample.sln`:
- Copy the VIM Revit Core nuget package files (`Vim.Revit.Core.$YearVersion.x.x.nupkg`) into the `nuget_local` folder at the root of this repository.
- Open `Vim.Sdk.Sample.sln` in Visual Studio 2022.
- Right-click on the 

## Contents

TODO: description of each file.

## Making it Your Own

To fully customize the plugin in this directory:

- Update the contents of `Resources/vim_license.txt` with your VIM license key.
  NOTE: The VIM license will expire based on the negotiated contract, so you must make sure to provide a valid VIM license to the ExportOptions.
  You can implement a downloading mechanism to fetch the license key from your server to avoid embedding it into your plugin.

- Update the GUIDs (you can use https://guidgenerator.com/).
  - In `Custom.Exporter.addin`, generate new GUIDs for the `AddinIds` elements.
  - In `Properties/AssemblyInfo.cs`, generate a new Guid.

- Search and replace the following strings among the filenames and files in this folder. Don't do a blanket search and replace - that might cause unexpected problems. We recommend going file-by-file.
  - `Custom.Exporter`
  - `CustomExporter`
  - `Custom Exporter`
  - `Custom_Exporter`
  - `com.custom-exporter-vendor-id`
  - `custom-exporter-company`

- Depending on what you are renaming your project/assembly, you will need to update the usages of `/Custom.Expoter;component/...`
  among the .XAML files to reflect the name of your assembly. Specifically, these occurrences of `/Custom.Exporter;` must match your assembly name, otherwise those resources will fail to load.

- Update the color scheme defined in `StyleResources.xaml`

- Update graphics
  - `Resources/launch.png`: the little ribon icon (32x32 pixel png).
  - `Resources/in_progress.png`: the image displayed while the plugin is exporting.

## Installation Instructions

Before publishing to customers, make sure you code-sign the DLL files(s) designated by your `.addin` file.


TODO: installation instructions (where to install when developing, where to install when publishing via installer)