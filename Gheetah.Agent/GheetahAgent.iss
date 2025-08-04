[Setup]
AppName=Gheetah Agent
AppVersion=1.0.0
DefaultDirName={autopf}\GheetahAgent
DefaultGroupName=Gheetah Agent
OutputDir=.\output
OutputBaseFilename=GheetahAgentSetup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
UninstallDisplayName=Uninstall Gheetah Agent
UninstallDisplayIcon={app}\GheetahAgent.ico
SetupIconFile=.\GheetahAgent.ico

[Files]
Source: ".\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: ".\GheetahAgent.ico"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
Name: "{app}"; Permissions: users-modify

[Icons]
Name: "{group}\Gheetah Agent"; Filename: "{app}\Gheetah.Agent.exe"; IconFilename: "{app}\GheetahAgent.ico"
Name: "{group}\Uninstall Gheetah Agent"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\Gheetah.Agent.exe"; Description: "Start Gheetah Agent"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: files; Name: "{app}\*.config"