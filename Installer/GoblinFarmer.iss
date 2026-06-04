#define MyAppName "GoblinFarmer"
#ifndef SourceDir
#define SourceDir "..\artifacts\publish\GoblinFarmer"
#endif
#define SourceExe AddBackslash(SourceDir) + "GoblinFarmer.exe"
#define SourceDebugPackageScript AddBackslash(SourceDir) + "Scripts\create-debug-package.ps1"
#if !FileExists(SourceExe)
#error "Published GoblinFarmer.exe was not found. Publish first with Scripts\\publish-release.ps1 or the Visual Studio GoblinFarmerRelease profile, then compile Installer\\GoblinFarmer.iss."
#endif
#if !FileExists(SourceDebugPackageScript)
#error "Published Scripts\\create-debug-package.ps1 was not found. Publish first with Scripts\\publish-release.ps1 or the Visual Studio GoblinFarmerRelease profile, then compile Installer\\GoblinFarmer.iss."
#endif
#define MyAppFileVersion GetFileVersion(SourceExe)
#if MyAppFileVersion == ""
#error "Unable to read GoblinFarmer.exe file version from the published executable."
#endif
#if Copy(MyAppFileVersion, Len(MyAppFileVersion) - 1, 2) == ".0"
#define MyAppVersion Copy(MyAppFileVersion, 1, Len(MyAppFileVersion) - 2)
#else
#define MyAppVersion MyAppFileVersion
#endif

[Setup]
AppId={{A48D8A29-3E74-4C0F-A13A-A2FC6E08448C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Eskimo Dirka
DefaultDirName={localappdata}\Programs\GoblinFarmer
DefaultGroupName=GoblinFarmer
DisableDirPage=no
DisableProgramGroupPage=yes
UsePreviousAppDir=no
AlwaysShowDirOnReadyPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=GoblinFarmerSetup-{#MyAppVersion}
SetupIconFile=..\GoblinFarmerIcon.ico
UninstallDisplayIcon={app}\GoblinFarmer.exe
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "Config\AppSettings.json,ScanRegions.json,Logs\*,Screenshots\*,debug-screenshots\*,session-info.txt"
Source: "{#SourceDir}\Config\AppSettings.json"; DestDir: "{app}\Config"; Flags: ignoreversion onlyifdoesntexist
Source: "{#SourceDir}\ScanRegions.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist skipifsourcedoesntexist

[Icons]
Name: "{group}\GoblinFarmer"; Filename: "{app}\GoblinFarmer.exe"; WorkingDir: "{app}"; IconFilename: "{app}\GoblinFarmerIcon.ico"
Name: "{autodesktop}\GoblinFarmer"; Filename: "{app}\GoblinFarmer.exe"; WorkingDir: "{app}"; IconFilename: "{app}\GoblinFarmerIcon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\GoblinFarmer.exe"; Description: "Launch GoblinFarmer"; Flags: nowait postinstall skipifsilent
