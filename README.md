<p align="center">
  <img src="installer/app.ico" width="96" alt="PlotView icon">
</p>

<h1 align="center">PlotView</h1>

<p align="center"><b>CAD처럼 조작하는 PDF 도면 뷰어</b> (Windows)<br><a href="https://github.com/GiJungPark/PlotView/releases/latest/download/PlotView-Setup.exe">설치 파일 바로 받기</a></p>

<p align="center">
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue"></a>
  <img alt="Windows 10/11" src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6">
</p>

일반 PDF 뷰어는 휠을 굴리면 스크롤되고 Ctrl+휠을 눌러야 확대됩니다.
CAD에 익숙한 손으로 도면 PDF를 보면 계속 엇나가죠.
PlotView는 AutoCAD와 같은 방식으로 움직입니다.
**휠은 커서 위치를 기준으로 확대/축소, 가운데 버튼 드래그는 화면 이동, 가운데 버튼 더블클릭은 전체 보기**입니다.

- 확대/축소와 이동이 즉시 반응하고, 손을 멈추면 화면에 보이는 부분만 다시 그려서 6000%까지 확대해도 선이 선명합니다.
- 커서 위치를 용지 기준 mm 좌표로 보여 주고, 용지 규격(A0~A5, B3~B5, ARCH 등)을 자동으로 알려 줍니다.
- 크롬과 같은 PDF 엔진(PDFium)을 써서 복잡한 도면도 정확하게 그립니다.
- 설치 파일에 실행 환경이 모두 들어 있어 따로 깔 것이 없고, 관리자 권한 없이도 설치됩니다.

## 설치

1. [**PlotView-Setup.exe 내려받기**](https://github.com/GiJungPark/PlotView/releases/latest/download/PlotView-Setup.exe) (항상 최신 버전, GitHub 로그인 필요 없음)
2. 실행하면 "Windows의 PC 보호" 창이 뜰 수 있습니다. 코드 서명 인증서가 없는 무료 프로그램이라 그렇습니다.
   **추가 정보 → 실행**을 누르세요.
3. 설치 마법사에서 원하는 항목을 고릅니다.
   - **바탕화면에 바로가기 만들기**
   - **PDF 파일의 '연결 프로그램' 목록에 추가**: PDF를 우클릭했을 때 PlotView로 열 수 있게 합니다.
4. 설치가 끝나면 시작 메뉴에서 **PlotView**를 실행합니다.

### PDF를 더블클릭으로 바로 열기

Windows는 설치 프로그램이 기본 앱을 마음대로 바꾸지 못하게 막고 있어서 한 번만 직접 지정해야 합니다.
PDF 파일 우클릭 → **연결 프로그램 → 다른 앱 선택** → **PlotView** 선택 → **항상 사용**.

### 제거

설정 → 앱 → 설치된 앱에서 PlotView를 찾아 제거합니다. 사용자 설정은 `%AppData%\PlotView`에 남아 있으니 필요하면 지워 주세요.

## 사용법

PDF를 창에 끌어다 놓거나, **열기**(Ctrl+O)를 누르거나, PDF 우클릭 → 연결 프로그램 → PlotView로 엽니다.
PDF를 여러 개 열면 창이 따로 떠서 도면을 나란히 비교할 수 있습니다.

### 마우스

| 동작 | 기능 |
|---|---|
| 휠 굴리기 | 커서 위치를 기준으로 확대/축소 |
| 가운데 버튼(휠) 누른 채 드래그 | 화면 이동 |
| 가운데 버튼 더블클릭 | 전체 보기 (줌 익스텐트) |
| 왼쪽 버튼 드래그 | 화면 이동 (노트북 터치패드용, 설정에서 끌 수 있음) |
| Space 누른 채 왼쪽 드래그 | 화면 이동 (위 설정을 껐을 때도 동작) |

### 키보드

| 키 | 기능 |
|---|---|
| Home, F | 전체 보기 |
| PageUp / PageDown | 이전 / 다음 페이지 |
| R | 90° 회전 (보기만 바뀌고 파일은 그대로) |
| + / - | 확대 / 축소 |
| 방향키 | 화면 이동 |
| Ctrl+O | 파일 열기 |

### 화면 구성

- **위쪽 도구 모음**: 열기, 페이지 이동, 전체 보기, 회전, 현재 배율(100% = 실제 크기), 설정
- **아래쪽 상태 표시줄**: 커서 좌표(mm, 용지 왼쪽 아래가 원점), 용지 크기와 규격, 렌더링 진행 표시

### 설정

도구 모음 오른쪽의 **설정**에서 바꿀 수 있고, 다음 실행 때도 유지됩니다.

| 항목 | 설명 |
|---|---|
| 휠 방향 반전 | 휠을 앞으로 굴리면 축소되게 합니다. CAD의 ZOOMWHEEL 설정과 같습니다. |
| 왼쪽 버튼 드래그로 화면 이동 | 가운데 버튼이 불편한 터치패드에서 켜 두면 편합니다. |
| CAD 십자선 커서 | 화면 전체를 가로지르는 십자선과 픽박스를 표시합니다. |
| 휠 확대 속도 | 휠 한 칸당 확대 비율을 조절합니다 (0.4×~2.5×). |

## 직접 빌드하기

### GitHub Actions (권장)

이 저장소에 push하면 [`.github/workflows/build.yml`](.github/workflows/build.yml)이 Windows 서버에서 설치 파일을 만듭니다.

- 일반 push: **Actions** 탭의 해당 실행 → **Artifacts**에서 `PlotView-Setup`을 받을 수 있습니다.
- `v1.2.0` 같은 태그를 push하면 **Releases**에 설치 파일이 자동으로 올라갑니다.
  웹에서는 Releases → **Draft a new release** → 새 태그 `v1.0.0` 입력 → **Publish release**로 할 수 있습니다.

### 내 PC에서

[.NET 10 SDK](https://dotnet.microsoft.com/download)와 [Inno Setup 6](https://jrsoftware.org/isdl.php)을 설치한 뒤 `build.bat`을 실행하면
`installer\Output`에 설치 파일이 생깁니다.

### 프로젝트 구조

```
src/PlotView/        C# (.NET 10, WinForms) 소스
  PdfView.cs         도면 표시와 CAD식 조작 (확대/이동, 고해상도 재렌더링)
  Pdfium.cs          PDFium 연결
  MainForm.cs        창, 도구 모음, 상태 표시줄
  Ui.cs              설정 저장, 설정/비밀번호 창, 색상
installer/setup.iss  Inno Setup 설치 스크립트
.github/workflows/   자동 빌드
```

## 라이선스

PlotView는 [MIT 라이선스](LICENSE)로 배포됩니다. 개인, 회사, 상업적 용도 모두 자유롭게 쓰고 고치고 나눠도 됩니다.
함께 배포되는 PDFium과 .NET 런타임의 라이선스는 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)에 정리되어 있습니다.
