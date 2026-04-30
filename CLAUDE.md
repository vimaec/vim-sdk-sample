# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Layout

This repo contains two independent Visual Studio 2026 solutions, each with its own usage doc:

- `VIM SDK Samples.slnx` → standalone NUnit samples that read/write VIM files and push them into SQL Server. See `VIM SDK Samples.md`.
- `VIM Revit Custom Exporter.slnx` → multi-targeted Revit plugin (one csproj per Revit year, 2020–2027) that exports a VIM from the active Revit document via a WPF UI. See `VIM Revit Custom Exporter.md`.

Both solutions depend on private VIM NuGet packages that are not on nuget.org. They must be dropped into `nuget_local/` (which is referenced by `nuget.config` as a local feed and is gitignored except for `.gitkeep`):

- Samples solution: `Vim.Sdk.Standalone.*.nupkg` (resolved as `Vim.Sdk.Standalone` 1.2.*).
- Revit exporter: one `Vim.Revit.Core.{Year}.*.nupkg` per supported Revit version (resolved per-year as `Vim.Revit.Core` `$(YearVersion).*`).

Without these packages neither solution will restore.

## Build / Test

Both solutions use the new `.slnx` format and are intended to be opened in Visual Studio 2026. From the CLI:

```
dotnet restore "VIM SDK Samples.slnx"
dotnet build   "VIM SDK Samples.slnx" -c Debug
dotnet test    "VIM SDK Samples.slnx" -c Debug
# Run a single NUnit test by FQN:
dotnet test    "VIM SDK Samples.slnx" --filter "FullyQualifiedName~VimSqlTests.TestVimSqlInsert"
```

```
dotnet restore "VIM Revit Custom Exporter.slnx"
dotnet build   "VIM Revit Custom Exporter.slnx" -c Debug
# Build a single year (faster iteration when working on one Revit version):
dotnet build   "VIM Revit Custom Exporter/Custom.Exporter/Custom.Exporter.v2027.csproj" -c Debug
```

The samples target `net8.0-windows`/`x64`. NUnit tests output `.log` files into `VIM SDK Samples/bin/Debug/net8.0-windows/`. `VimSqlTests` requires a working SQL Server Express instance — update the connection string in `VIM SDK Samples/VimSqlTests.cs` (`TestVimSqlInsert`) before running.

The Revit exporter does not produce a runnable test suite — it is verified by launching Revit and using the "Custom Exporter" → "Make a VIM" ribbon command. A valid VIM license key must be pasted into `VIM Revit Custom Exporter/Custom.Exporter/Resources/vim_license.txt` (it is embedded as a WPF resource at build time) or export will fail at runtime with a licensing error.

## Revit Exporter: Multi-Year Build Architecture

The non-obvious part of this repo is how a single source tree compiles into eight different Revit-version-specific plugin DLLs. Read this before touching any of the `.props`/`.targets`/`Directory.Build.props` files.

**One source folder, one csproj per year.** `VIM Revit Custom Exporter/Custom.Exporter/` contains `Custom.Exporter.v2020.csproj` through `Custom.Exporter.v2027.csproj`. They all sit alongside the same `.cs`/`.xaml` files, and each `.slnx` reference picks one. Each yearly csproj is a 10-line file that only sets `<TargetFramework>` and imports three shared MSBuild files:

- `Custom.Exporter._Common.Build.props` — output paths, WPF/WinForms toggles, resource globs. Sets `OutputPath=bin\$(Configuration)\$(YearVersion)\` and adds a `BUILD_YEAR_$(YearVersion)` `DefineConstants` symbol you can `#if` against in code.
- `Custom.Exporter._References.Build.props` — references `RevitAPI.dll` / `RevitAPIUI.dll` from `lib/$(YearVersion)/` and pulls the matching `Vim.Revit.Core` NuGet (`Version=$(YearVersion).*`). Also branches `FrameworkReference`/legacy `Reference` items by `TargetFramework`.
- `Custom.Exporter._Targets.Build.targets` — post-build step that wipes and re-populates `%AppData%\Autodesk\Revit\Addins\$(YearVersion)\` with the addin file and DLLs (Revit prefers AppData over ProgramData when both are present).

**`<YearVersion>` is parsed out of the csproj filename.** `Directory.Build.props` runs a regex `v(\d{4})` against `$(MSBuildProjectFullPath)` to derive `<YearVersion>`. **The `vYYYY` token in the csproj filename is load-bearing** — renaming a project file to drop or alter the `vYYYY` substring will break the year detection and cascade into wrong DLL references, wrong NuGet versions, and wrong AppData install path.

**`obj/` path redirect is mandatory and must live in `Directory.Build.props`.** Because multiple csprojs share a folder, their `obj/` artifacts would collide. `Directory.Build.props` sets `IntermediateOutputPath`, `BaseIntermediateOutputPath`, and `MSBuildProjectExtensionsPath` to `obj\$(YearVersion)\`. Per the comment in that file, `MSBuildProjectExtensionsPath` **must** be set inside `Directory.Build.props` — moving it to `_Common.Build.props` produces wrong NuGet restore behavior. Don't move it.

**Target frameworks differ by year.** Revit 2024 and earlier → `net48`; 2025–2026 → `net8.0-windows`; 2027 → `net10.0-windows`. The `_References.Build.props` `<ItemGroup Condition>` blocks branch on `TargetFramework` to pick the right framework references, so when adding a new year, copy a same-TFM csproj as the template.

**Adding a new Revit year** generally means: add `Custom.Exporter.v{Year}.csproj` (one-line TFM change), add `lib/{Year}/` with the matching `RevitAPI.dll`/`RevitAPIUI.dll`/`RevitNET.dll`, drop the matching `Vim.Revit.Core.{Year}.*.nupkg` into `nuget_local/`, and add the project to `VIM Revit Custom Exporter.slnx`.

## Customizing the Exporter for Redistribution

`VIM Revit Custom Exporter.md` documents the rename procedure for shipping this as your own plugin. The key constraint: WPF resource URIs in `.xaml` files use `/Custom.Exporter;component/...` paths that depend on `<AssemblyName>` in `_Common.Build.props`. If you rename the assembly without updating those URIs, resources load silently as nulls at runtime. Do the rename file-by-file rather than blanket search/replace. The GUIDs in `Custom.Exporter.addin` (`AddInId` × 2) and `Properties/AssemblyInfo.cs` must also be regenerated per redistribution.
