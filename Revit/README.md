# Custom Revit Exporter Sample

This directory contains a sample Revit Exporter project which targets multiple versions of Revit using the same codebase.

The project is a simple WPF application which illustrates how to get started with the VIM Exporter for Revit.

## Requirements

To get started with the Revit Exporter Sample, yo uwill need:

- `Vim.Revit.Core.$YearVersion.x.x.nupkg` for each version of Revit you support. Please contact us for these packages as they are not freely distributed via NuGet.

- A valid VIM license key which we provide as part of our software development partnership agreement.

## Contents

TODO: description of each file.

## Making it Your Own

To fully customize the plugin in this directory:

- In `Custom.Exporter.addin`, generate new GUIDs for the `AddinIds` elements. You can use random GUIDs generated from https://guidgenerator.com/.

- Search and replace the following strings among the filenames and files in this folder.
  - `Custom.Exporter`
  - `CustomExporter`
  - `Custom Exporter`
  - `Custom_Exporter`
  - `com.custom-exporter-vendor-id`
  - `custom-exporter-company`

Before publishing to customers, make sure you code-sign the DLL files(s) designated by your `.addin` file.

## Installation Instructions

TODO: installation instructions (where to install when developing, where to install when publishing via installer)