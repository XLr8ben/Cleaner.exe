; ════════════════════════════════════════════════════════════════
;  OptiClean Pro — Inno Setup Installer Script
;  Compile with Inno Setup 6.x  (Ctrl+F9 in IDE, or ISCC.exe)
;
;  BEFORE compiling:
;    1. Run:  dotnet publish OptiCleanPro.csproj -c Release -r win-x64
;             --self-contained true /p:PublishSingleFile=true
;             /p:IncludeNativeLibrariesForSelfExtract=true -o publish
;    2. Open this .iss file in Inno Setup Compiler
;    3. Build → Compile  (Ctrl+F9)
;    4. Find output in:  installer_output\OptiCleanProSetup_v1.0.0.exe
; ════════════════════════════════════════════════════════════════

#define MyAppName      "OptiClean Pro"
#define MyAppVersion   "1.0.0"
#define MyAppPublisher "Ribhav"
#define MyAppURL       "https://about.google/"
#define MyAppExeName   "OptiCleanPro.exe"
#define SourceDir      "publish"
#define IconFile       "installation_application_software_10810.ico"

; ── [Setup] ──────────────────────────────────────────────────────
[Setup]
; Unique GUID — regenerate with Tools → Generate GUID if forking
AppId={{F3A7C12E-84B6-4D91-BE5A-7C3092A1F80D}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Install to Program Files\OptiClean Pro
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes

; Output
OutputDir=installer_output
OutputBaseFilename=OptiCleanProSetup_v{#MyAppVersion}
SetupIconFile={#IconFile}

; Compression (best ratio for a single-file EXE)
Compression=lzma2/ultra64
SolidCompression=yes

; Modern wizard UI
WizardStyle=modern
WizardSizePercent=120

; No admin elevation — runs as the current user
PrivilegesRequired=lowest

; Minimum OS: Windows 10
MinVersion=10.0.17763

; ── [Languages] ─────────────────────────────────────────────────
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

; ── [Tasks] ─────────────────────────────────────────────────────
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; \
  GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

; ── [Files] ─────────────────────────────────────────────────────
[Files]
; Single self-contained executable from dotnet publish output
Source: "{#SourceDir}\{#MyAppExeName}"; \
  DestDir: "{app}"; \
  Flags: ignoreversion

; App icon (used by shortcuts)
Source: "{#IconFile}"; \
  DestDir: "{app}"; \
  Flags: ignoreversion

; ── [Icons] ─────────────────────────────────────────────────────
[Icons]
; Start Menu
Name: "{group}\{#MyAppName}"; \
  Filename: "{app}\{#MyAppExeName}"; \
  IconFilename: "{app}\{#IconFile}"

; Start Menu uninstall entry
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; \
  Filename: "{uninstallexe}"

; Desktop shortcut (optional — unchecked by default above)
Name: "{autodesktop}\{#MyAppName}"; \
  Filename: "{app}\{#MyAppExeName}"; \
  IconFilename: "{app}\{#IconFile}"; \
  Tasks: desktopicon

; ── [Run] ────────────────────────────────────────────────────────
[Run]
; Offer to launch after install
Filename: "{app}\{#MyAppExeName}"; \
  Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent

; ── [UninstallDelete] ────────────────────────────────────────────
[UninstallDelete]
; Clean up any temp/log files the app may create next to the EXE
Type: filesandordirs; Name: "{app}"
