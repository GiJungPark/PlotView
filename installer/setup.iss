; CAD식 PDF 뷰어 설치 파일 (Inno Setup 6.3 이상)
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define AppName "CAD식 PDF 뷰어"
#define AppExe "CADPDFViewer.exe"

[Setup]
AppId={{8F3C2A51-6B7D-4E2A-9C1F-3D5B7A9E1C42}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
DefaultDirName={autopf}\CADPDFViewer
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=Output
OutputBaseFilename=CADPDFViewer-Setup-{#AppVersion}
SetupIconFile=app.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
CloseApplications=yes

[Languages]
#if FileExists(AddBackslash(CompilerPath) + "Languages\Korean.isl")
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
#endif
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "바탕화면에 바로가기 만들기"; GroupDescription: "추가 작업:"
Name: "pdfassoc"; Description: "PDF 파일의 '연결 프로그램' 목록에 추가"; GroupDescription: "추가 작업:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
Root: HKA; Subkey: "Software\Classes\CADPDFViewer.pdf"; ValueType: string; ValueName: ""; ValueData: "PDF 도면"; Flags: uninsdeletekey; Tasks: pdfassoc
Root: HKA; Subkey: "Software\Classes\CADPDFViewer.pdf\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExe},0"; Tasks: pdfassoc
Root: HKA; Subkey: "Software\Classes\CADPDFViewer.pdf\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""; Tasks: pdfassoc
Root: HKA; Subkey: "Software\Classes\.pdf\OpenWithProgids"; ValueType: string; ValueName: "CADPDFViewer.pdf"; ValueData: ""; Flags: uninsdeletevalue; Tasks: pdfassoc
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "{#AppName}"; Flags: uninsdeletekey; Tasks: pdfassoc
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".pdf"; ValueData: ""; Tasks: pdfassoc
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""; Tasks: pdfassoc

[Run]
Filename: "{app}\{#AppExe}"; Description: "{#AppName} 실행"; Flags: nowait postinstall skipifsilent
