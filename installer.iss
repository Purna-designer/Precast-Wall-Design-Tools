[Setup]
AppName=Precast Wall Connection Tool
AppVersion=1.0.0
DefaultDirName={autopf}\Possibuild\Precast Wall Tool
DefaultGroupName=Possibuild Tools
UninstallDisplayIcon={app}\Precast Wall Horizontal Connection Tool.exe
Compression=lzma2
SolidCompression=yes
OutputDir=.\installer_output
OutputBaseFilename=PrecastWallTool_Setup

[Files]
; Grabs all compiled files from the GitHub Action build output
Source: "..\build_output\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{autodesktop}\Precast Wall Tool"; Filename: "{app}\Precast Wall Horizontal Connection Tool.exe"
Name: "{group}\Precast Wall Tool"; Filename: "{app}\Precast Wall Horizontal Connection Tool.exe"