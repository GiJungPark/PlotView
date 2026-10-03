# CAD식 PDF 뷰어 (Windows 설치판)

AutoCAD처럼 조작하는 도면용 PDF 뷰어입니다. C# (.NET 10, WinForms)과 크롬의 PDF 엔진인 PDFium으로 만들었습니다.

## 설치 파일 받는 법 (GitHub 자동 빌드, PC에 아무것도 설치할 필요 없음)

1. https://github.com 에 로그인하고 오른쪽 위 **+ → New repository**를 누릅니다.
   이름은 아무거나(예: `cad-pdf-viewer`), Private으로 해도 됩니다. **Create repository**.
2. 새 저장소 화면에서 **uploading an existing file** 링크를 누르고,
   이 폴더 안의 내용(`.github`, `src`, `installer` 폴더와 나머지 파일 전부)을 끌어다 놓은 뒤 **Commit changes**.
3. 위쪽 **Actions** 탭을 열면 "Build installer"가 자동으로 실행됩니다. 5~8분 정도 걸립니다.
4. 초록색 체크가 뜨면 그 실행을 눌러 맨 아래 **Artifacts**의 `CADPDFViewer-Setup`을 내려받습니다.
   압축을 풀면 `CADPDFViewer-Setup-1.0.x.exe`가 들어 있습니다.

코드를 고쳐서 다시 올리면 새 설치 파일이 자동으로 만들어집니다.
`v1.0.0`처럼 태그를 붙이면 Releases 페이지에 설치 파일이 올라가서, 다른 사람에게 링크로 나눠 주기 좋습니다.

## 내 PC에서 직접 만들기

.NET 10 SDK와 Inno Setup 6을 설치한 뒤 `build.bat`을 실행하면 `installer\Output`에 설치 파일이 생깁니다.

## 설치할 때

- 코드 서명이 없어서 처음 실행할 때 "Windows의 PC 보호" 창이 뜹니다. **추가 정보 → 실행**을 누르세요.
- 관리자 권한 없이 내 계정에만 설치할 수 있고, 모든 사용자용으로도 설치할 수 있습니다.
- "연결 프로그램 목록에 추가"를 체크하면 PDF 우클릭 → 연결 프로그램에 나타납니다.
  기본 앱으로 쓰려면 거기서 고른 뒤 "항상 이 앱 사용"을 누르세요. (Windows 정책상 설치 프로그램이 기본 앱을 강제로 바꿀 수는 없습니다.)

## 조작

| 동작 | 기능 |
|---|---|
| 마우스 휠 | 커서 위치 기준 확대/축소 |
| 가운데 버튼 드래그 | 화면 이동 |
| 가운데 버튼 더블클릭, Home, F | 전체 보기 |
| 왼쪽 드래그, Space+드래그 | 화면 이동 (설정에서 끌 수 있음) |
| PageUp / PageDown | 이전 / 다음 페이지 |
| R | 90° 회전 (파일은 바뀌지 않음) |
| + / -, 방향키 | 확대/축소, 이동 |
| Ctrl+O | 열기 (창에 끌어다 놓기도 됨) |

설정(휠 방향, 확대 속도, 왼쪽 드래그 이동, 십자선 커서)은 `%AppData%\CADPDFViewer\settings.ini`에 저장됩니다.
PDF를 여러 개 열면 창이 따로 떠서 도면을 나란히 볼 수 있습니다.

## 라이선스

PDFium은 BSD 계열 라이선스라 개인, 사내, 외부 배포 모두 자유롭습니다.
