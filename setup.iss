[Setup]
AppName=AvionPlot
AppVersion=1.0
DefaultDirName={autopf}\AvionPlot
DefaultGroupName=AvionPlot
OutputBaseFilename=AvionPlotSetup
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
DisableProgramGroupPage=no
DisableDirPage=no
DisableWelcomePage=no

[Components]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; Types: full; Flags: checkablealone
Name: "xmlassoc"; Description: "Сделать .xml файлы открываться AvionPlot по умолчанию"; Types: full; Flags: checkablealone

[Files]
; Основное приложение
Source: "C:\Users\user\Desktop\work\programs\desktop\AvionPlot\bin\x64\*"; DestDir: "{app}"; Check: Is64BitInstallMode; Flags: ignoreversion recursesubdirs
Source: "C:\Users\user\Desktop\work\programs\desktop\AvionPlot\bin\x86\*"; DestDir: "{app}"; Check: not Is64BitInstallMode; Flags: ignoreversion recursesubdirs

; .NET SDK
Source: "C:\Users\user\Desktop\work\programs\desktop\AvionPlot\redist\dotnet-sdk-8.0.124-win-x64.exe"; DestDir: "{tmp}"; Check: Is64BitInstallMode; Flags: deleteafterinstall
Source: "C:\Users\user\Desktop\work\programs\desktop\AvionPlot\redist\dotnet-sdk-8.0.124-win-x86.exe"; DestDir: "{tmp}"; Check: not Is64BitInstallMode; Flags: deleteafterinstall
Source: "C:\Users\user\Desktop\work\programs\desktop\AvionPlot\redist\dotnet-sdk-8.0.124-win-arm64.exe"; DestDir: "{tmp}"; Check: Is64BitInstallMode and IsARM64; Flags: deleteafterinstall

[Run]
; Установка .NET SDK
Filename: "{tmp}\dotnet-sdk-8.0.124-win-x64.exe"; Description: "Установить .NET SDK 8.0 (x64)"; Check: Is64BitInstallMode; Flags: nowait postinstall skipifsilent shellexec runascurrentuser
Filename: "{tmp}\dotnet-sdk-8.0.124-win-x86.exe"; Description: "Установить .NET SDK 8.0 (x86)"; Check: not Is64BitInstallMode; Flags: nowait postinstall skipifsilent shellexec runascurrentuser
Filename: "{tmp}\dotnet-sdk-8.0.124-win-arm64.exe"; Description: "Установить .NET SDK 8.0 (ARM64)"; Check: Is64BitInstallMode and IsARM64; Flags: nowait postinstall skipifsilent shellexec runascurrentuser

[Icons]
Name: "{group}\AvionPlot"; Filename: "{app}\AvionPlot.exe"
Name: "{userdesktop}\AvionPlot"; Filename: "{app}\AvionPlot.exe"; Components: desktopicon

[Registry]
; Ассоциация .xml файлов
Root: HKCR; Subkey: ".xml"; ValueType: string; ValueName: ""; ValueData: "AvionPlot.xmlfile"; Components: xmlassoc
Root: HKCR; Subkey: "AvionPlot.xmlfile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AvionPlot.exe"" ""%1"""; Components: xmlassoc

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"