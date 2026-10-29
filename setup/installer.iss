#define AppName "TROCKI Motion Studio PRO"
#define AppVersion "1.0.0"
#define AppExe "TrockiMotionStudio.exe"

[Setup]
AppId={{57AD99DD-63AC-42C5-BD87-D23F7D608949}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=TROCKI STUDIO
DefaultDirName={localappdata}\Programs\TROCKI Motion Studio PRO
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
OutputDir=..\installer-output
OutputBaseFilename=TROCKI-Motion-Studio-PRO-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExe}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Tasks]
Name: "desktopicon"; Description: "Masaüstüne kısayol oluştur"; GroupDescription: "Kısayollar:"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "TROCKI Motion Studio PRO programını çalıştır"; Flags: nowait postinstall skipifsilent
