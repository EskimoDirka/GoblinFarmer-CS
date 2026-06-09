# Release And Build

Source of truth: `README.md`, `Docs\Release_Checklist.md`, `Docs\Release_v1.4.md`, and `Docs\Project_Status.md`.

Do not infer a new release status from this summary. Use the current source docs for release decisions.

## Current Release Context

- Current release line in `Docs\Project_Status.md`: `v1.4.0`.
- v1.4 focuses on Goblin Tracker automatic-count readiness, Observation Mode diagnostics, Witch Doctor input reliability, startup validation, and package-size hardening.
- Existing release notes live in `Docs\Release_v1.4.md`.

## Build From Source

From the project root:

```powershell
dotnet build GoblinFarmer.csproj
```

Requirements from README:

- Windows.
- .NET SDK compatible with `net10.0-windows`.
- Diablo III and Battle.net installed for runtime use.

## Publish Workflow

Release versioning starts in `GoblinFarmer.csproj`:

```xml
<Version>1.4.0</Version>
<AssemblyVersion>1.4.0.0</AssemblyVersion>
<FileVersion>1.4.0.0</FileVersion>
<InformationalVersion>1.4.0</InformationalVersion>
```

Publish from Visual Studio with the `GoblinFarmerRelease` profile or run the equivalent command:

```powershell
dotnet publish .\GoblinFarmer.csproj --configuration Release --runtime win-x64 --self-contained true --output .\artifacts\publish\GoblinFarmer -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=true
```

The publish output goes to `artifacts\publish\GoblinFarmer`. Confirm executable version metadata, icon, config, Images output, and debug package scripts before compiling `Installer\GoblinFarmer.iss` with Inno Setup.

## Installer Workflow

- Compile `Installer\GoblinFarmer.iss` only after `artifacts\publish\GoblinFarmer\GoblinFarmer.exe` exists.
- If Inno Setup is installed, v1.4 expects `artifacts\installer\GoblinFarmerSetup-1.4.0.exe`.
- The installer target is `%LOCALAPPDATA%\Programs\GoblinFarmer`.
- The installer should show the install directory, create a Start Menu shortcut, and optionally create a desktop shortcut.

## Release Notes And GitHub

- Before publishing, update `Docs\Project_Status.md`, `README.md`, and release notes/checklists for the changes being shipped.
- For major app milestones, create or update the matching GitHub Release only when explicitly requested in the workflow.
- Use the matching `Docs\Release_v*.md` file as release body.
- Upload installer and portable ZIP assets only after release build artifacts are created and validated.

## Generated Output Rules

Do not commit generated executables, installer output, portable ZIPs, debug packages, Project Brain ZIPs, logs, screenshots, source-upload output, or retired upload folders. `artifacts\` and installer binaries are generated output.
