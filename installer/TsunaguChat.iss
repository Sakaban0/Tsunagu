#ifndef AppVersion
#error AppVersion is required
#endif
#ifndef PayloadDir
  #error PayloadDir is required
#endif
#ifndef OutputPath
  #error OutputPath is required
#endif
[Setup]
#ifdef TestBuild
AppId=TsunaguChat-Installer-Isolated-Test
DefaultDirName={#TestAppDir}
CreateUninstallRegKey=no
#else
AppId={{42F5A70B-1780-45A0-8811-79038453AD63}
DefaultDirName={localappdata}\Programs\TsunaguChat
#endif
AppName=TsunaguChat
AppVersion={#AppVersion}
AppVerName=TsunaguChat {#AppVersion}
UninstallDisplayIcon={app}\TsunaguChat.exe
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DefaultGroupName=TsunaguChat
DisableProgramGroupPage=yes
OutputDir={#OutputPath}
OutputBaseFilename=TsunaguChat-{#AppVersion}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\src\AiPromptHelper\Assets\TsunaguChat.ico
InfoBeforeFile=install-info.txt
CloseApplications=yes
RestartApplications=no
Uninstallable=yes
UsePreviousTasks=yes

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
#ifndef TestBuild
Name: "desktopicon"; Description: "デスクトップにショートカットを作成する"; GroupDescription: "ショートカット:"
#endif

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
#ifndef TestBuild
Name: "{userprograms}\TsunaguChat\TsunaguChat"; Filename: "{app}\TsunaguChat.exe"; WorkingDir: "{app}"
Name: "{userprograms}\TsunaguChat\使い方"; Filename: "{app}\docs\USER-GUIDE.md"
Name: "{userdesktop}\TsunaguChat"; Filename: "{app}\TsunaguChat.exe"; WorkingDir: "{app}"; Tasks: desktopicon
#endif

[Run]
Filename: "{app}\TsunaguChat.exe"; Description: "TsunaguChatを起動する"; Flags: nowait postinstall skipifsilent


