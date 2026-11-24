; Inno Setup script for Search for Windows. Built by CI (.github/workflows/windows.yml).
; Per-user install: no administrator prompt, as the Mac app needs none.
#define AppName "Search"
#ifndef AppVersion
  #define AppVersion "1.0.19"
#endif
#ifndef SourceDir
  #define SourceDir "..\out\win-x64"
#endif

[Setup]
AppId={{6C1F3E0B-6E5B-4C8D-9F43-5B2B7D8C1A19}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Search for Windows contributors
DefaultDirName={localappdata}\Programs\Search
DefaultGroupName=Search
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputBaseFilename=Search-Setup-{#AppVersion}-x64
OutputDir=..\out
SetupIconFile=..\src\Search.App\Assets\Search.ico
UninstallDisplayIcon={app}\Search.exe
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
WizardStyle=modern

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\Search"; Filename: "{app}\Search.exe"
Name: "{autodesktop}\Search"; Filename: "{app}\Search.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

; Registers Search as a browser Windows can offer under Settings > Apps > Default apps.
[Registry]
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\Search"; ValueType: string; ValueName: ""; ValueData: "Search"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\Search\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Search.exe,0"
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\Search\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Search.exe"""
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\Search\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "Search"
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\Search\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "A browser with nothing in the way."
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\Search\Capabilities\URLAssociations"; ValueType: string; ValueName: "http"; ValueData: "SearchHTML"
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\Search\Capabilities\URLAssociations"; ValueType: string; ValueName: "https"; ValueData: "SearchHTML"
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\Search\Capabilities\FileAssociations"; ValueType: string; ValueName: ".html"; ValueData: "SearchHTML"
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\Search\Capabilities\FileAssociations"; ValueType: string; ValueName: ".htm"; ValueData: "SearchHTML"
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\Search\Capabilities\FileAssociations"; ValueType: string; ValueName: ".pdf"; ValueData: "SearchHTML"
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "Search"; ValueData: "Software\Clients\StartMenuInternet\Search\Capabilities"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SearchHTML"; ValueType: string; ValueName: ""; ValueData: "Search Document"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SearchHTML\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\Search.exe,0"
Root: HKCU; Subkey: "Software\Classes\SearchHTML\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Search.exe"" ""%1"""

[Run]
Filename: "{app}\Search.exe"; Description: "Open Search"; Flags: nowait postinstall skipifsilent
