# 내 PC에서 직접 설치 파일 만들기
# 필요: .NET 10 SDK (https://dotnet.microsoft.com/download), Inno Setup 6 (https://jrsoftware.org/isdl.php)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  Write-Host ".NET SDK not found. Install .NET 10 SDK: https://dotnet.microsoft.com/download" -ForegroundColor Red
  exit 1
}
$iscc = Get-ChildItem "C:\Program Files*\Inno Setup*\ISCC.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $iscc) {
  Write-Host "Inno Setup not found. Install it: https://jrsoftware.org/isdl.php" -ForegroundColor Red
  exit 1
}

if (Test-Path publish) { Remove-Item publish -Recurse -Force }
dotnet publish src/PlotView/PlotView.csproj -c Release -o publish
if ($LASTEXITCODE -ne 0) { exit 1 }
& $iscc.FullName installer\setup.iss
if ($LASTEXITCODE -ne 0) { exit 1 }

$out = Get-ChildItem installer\Output\*.exe | Sort-Object LastWriteTime | Select-Object -Last 1
Write-Host ""
Write-Host "Done: $($out.FullName)" -ForegroundColor Green
explorer.exe /select,"$($out.FullName)"
