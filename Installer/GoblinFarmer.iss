#define MyAppName "GoblinFarmer"
#ifndef MyAppVersion
#define MyAppVersion GetFileVersion("D:\GoblinFarmer_Publish\GoblinFarmer.exe")
#endif
#ifndef SourceDir
#define SourceDir "..\artifacts\publish\GoblinFarmer"
#endif

[Setup]
AppId={{A48D8A29-3E74-4C0F-A13A-A2FC6E08448C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=GoblinFarmer
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
