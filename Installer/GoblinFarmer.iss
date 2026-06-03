#define MyAppName "GoblinFarmer"
#ifndef SourceDir
#define SourceDir "..\artifacts\publish\GoblinFarmer"
#endif
#define SourceExe SourceDir + "\GoblinFarmer.exe"
#define MyAppFileVersion GetFileVersion(SourceExe)
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
DisableProgramGroupPage=yes
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
