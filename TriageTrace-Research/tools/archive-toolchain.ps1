<#
.SYNOPSIS
    HoloLens 2 개발 툴체인 아카이브 - 자동 다운로드 스크립트
.DESCRIPTION
    직접 URL로 받을 수 있는 항목만 자동으로 내려받는다.
    Visual Studio 레이아웃, Unity 설치/활성화, Feature Tool 실행은
    수동 단계이므로 41-toolchain-archive.md 를 따를 것.
.NOTES
    확인 시점: 2026-09-16
    Unity 2022.3.62f1 revision: 4af31df58517
    실행:  powershell -ExecutionPolicy Bypass -File .\archive-toolchain.ps1
    이어받기 지원 - 중간에 끊겨도 다시 실행하면 이어서 받는다.
#>

param(
    [string]$Root = "D:\Archive\HoloLens2-Toolchain-2026-09"
)

$ErrorActionPreference = "Continue"
$UnityRev = "4af31df58517"
$UnityVer = "2022.3.62f1"
$MrtkTag  = "core-v3.3.0"

# ---------- 준비 ----------
Write-Host "`n=== HoloLens 2 툴체인 아카이브 ===" -ForegroundColor Cyan
Write-Host "저장 위치: $Root`n"

$dirs = @("unity","mrtk","mrfeaturetool","visualstudio","sdk","emulator","remoting","docs","_logs")
foreach ($d in $dirs) { New-Item -ItemType Directory -Force -Path (Join-Path $Root $d) | Out-Null }

$drive = (Get-Item $Root).PSDrive
$freeGB = [math]::Round($drive.Free / 1GB, 1)
Write-Host "여유 공간: $freeGB GB" -ForegroundColor Yellow
if ($freeGB -lt 25) {
    Write-Host "경고: Visual Studio 레이아웃까지 포함하면 25GB 이상 필요합니다 (2026-09-23 실측 합계 약 15~20GB)." -ForegroundColor Red
    Write-Host "      이 스크립트만으로는 약 4.6GB. VS 레이아웃(~9.2GB)은 별도 단계입니다.`n"
}

$log = Join-Path $Root "_logs\download-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
function Log($m) { $line = "$(Get-Date -Format 'HH:mm:ss')  $m"; Write-Host $line; Add-Content -Path $log -Value $line }

# ---------- 다운로드 함수 (curl.exe, 이어받기) ----------
function Get-File {
    param([string]$Url, [string]$OutPath, [string]$Label)

    if (Test-Path $OutPath) {
        $sz = [math]::Round((Get-Item $OutPath).Length / 1MB, 1)
        Log "SKIP  $Label  (이미 있음, $sz MB)"
        return $true
    }
    Log "GET   $Label"
    Log "      $Url"
    # .part 에 받고 성공 시 이름 변경 - 부분 파일이 완료로 SKIP 되지 않게
    $partPath = "$OutPath.part"
    # -f HTTP 오류 시 실패 처리, -L 리디렉션 추적, -C - 이어받기, --retry 재시도
    & curl.exe -f -L -C - --retry 3 --retry-delay 5 --progress-bar -o "$partPath" "$Url"
    if ($LASTEXITCODE -eq 0 -and (Test-Path $partPath)) {
        Move-Item -Force $partPath $OutPath
        $sz = [math]::Round((Get-Item $OutPath).Length / 1MB, 1)
        Log "OK    $Label  ($sz MB)"
        return $true
    } else {
        Log "FAIL  $Label  (exit $LASTEXITCODE) - 수동으로 받을 것"
        return $false
    }
}

$failed = @()

# ---------- 1. Unity 2022.3.62f1 ----------
Write-Host "`n--- 1/6  Unity $UnityVer ---" -ForegroundColor Green
$unityBase = "https://download.unity3d.com/download_unity/$UnityRev"

if (-not (Get-File "$unityBase/Windows64EditorInstaller/UnitySetup64-$UnityVer.exe" `
    (Join-Path $Root "unity\UnitySetup64-$UnityVer.exe") "Unity 에디터 (~3.5GB)")) { $failed += "Unity 에디터" }

if (-not (Get-File "$unityBase/TargetSupportInstaller/UnitySetup-Universal-Windows-Platform-Support-for-Editor-$UnityVer.exe" `
    (Join-Path $Root "unity\UnitySetup-UWP-Support-$UnityVer.exe") "UWP Build Support (~293MB)")) { $failed += "UWP 모듈" }

if (-not (Get-File "$unityBase/TargetSupportInstaller/UnitySetup-Windows-IL2CPP-Support-for-Editor-$UnityVer.exe" `
    (Join-Path $Root "unity\UnitySetup-Windows-IL2CPP-$UnityVer.exe") "Windows IL2CPP Support")) { $failed += "IL2CPP 모듈" }

# ---------- 2. MRTK3 Core v3.3.0 ----------
Write-Host "`n--- 2/6  MRTK3 $MrtkTag  (~57MB) ---" -ForegroundColor Green
$mrtkBase = "https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases/download/$MrtkTag"
$mrtkFiles = @(
    "org.mixedrealitytoolkit.core-3.3.0.tgz",
    "org.mixedrealitytoolkit.input-3.3.0.tgz",
    "org.mixedrealitytoolkit.uxcore-3.3.0.tgz",
    "org.mixedrealitytoolkit.uxcomponents-3.4.0.tgz",
    "org.mixedrealitytoolkit.uxcomponents.noncanvas-3.1.5.tgz",
    "org.mixedrealitytoolkit.spatialmanipulation-3.4.0.tgz",
    "org.mixedrealitytoolkit.standardassets-3.2.1.tgz",
    "org.mixedrealitytoolkit.extendedassets-3.0.4.tgz",
    "org.mixedrealitytoolkit.audio-3.0.5.tgz",
    "org.mixedrealitytoolkit.diagnostics-3.0.3.tgz",
    "org.mixedrealitytoolkit.tools-3.0.5.tgz",
    "org.mixedrealitytoolkit.windowsspeech-3.0.4.tgz"
)
foreach ($f in $mrtkFiles) {
    if (-not (Get-File "$mrtkBase/$f" (Join-Path $Root "mrtk\$f") $f)) { $failed += "MRTK: $f" }
}

# ---------- 3. Mixed Reality Feature Tool ----------
Write-Host "`n--- 3/6  Mixed Reality Feature Tool ---" -ForegroundColor Green
Log "NOTE  Feature Tool 은 Microsoft 다운로드 센터에서 수동으로 받는다."
Log "      https://www.microsoft.com/en-us/download/details.aspx?id=102778"
Log "      파일: MixedRealityFeatureTool.exe (68.1 MB) / 버전 1.0.2209.0"
Log "      -> $Root\mrfeaturetool\ 에 저장할 것"
Write-Host "  수동: https://www.microsoft.com/en-us/download/details.aspx?id=102778" -ForegroundColor Yellow

# ---------- 4. Visual Studio 2022 부트스트래퍼 ----------
Write-Host "`n--- 4/6  Visual Studio 2022 부트스트래퍼 ---" -ForegroundColor Green
if (-not (Get-File "https://aka.ms/vs/17/release/vs_community.exe" `
    (Join-Path $Root "visualstudio\vs_community.exe") "vs_community.exe (~4MB)")) { $failed += "VS 부트스트래퍼" }
Write-Host "  레이아웃(~45GB) 생성은 별도 단계 - 41 문서 STEP 2 참조" -ForegroundColor Yellow

# ---------- 5. Windows 10 SDK 10.0.19041.0 (ISO) ----------
Write-Host "`n--- 5/6  Windows 10 SDK 10.0.19041.0 ---" -ForegroundColor Green
if (-not (Get-File "https://go.microsoft.com/fwlink/?linkid=2312004" `
    (Join-Path $Root "sdk\winsdk-10.0.19041.0.iso") "Windows SDK ISO (~754MB)")) { $failed += "Windows SDK ISO" }
if (-not (Get-File "https://go.microsoft.com/fwlink/?linkid=2311805" `
    (Join-Path $Root "sdk\winsdksetup.exe") "winsdksetup.exe (부트스트래퍼)")) { $failed += "SDK 부트스트래퍼" }

# ---------- 6. HoloLens 2 에뮬레이터 ----------
Write-Host "`n--- 6/6  HoloLens 2 에뮬레이터 24H1 ---" -ForegroundColor Green
if (-not (Get-File "https://go.microsoft.com/fwlink/?linkid=2290700" `
    (Join-Path $Root "emulator\HoloLens2EmulatorSetup-24H1-10.0.22621.1402.exe") "에뮬레이터 24H1 웹 설치기 (~1.3MB)")) { $failed += "에뮬레이터" }
Write-Host "  주의: 웹 설치기(WiX Burn)일 뿐 본체가 아니다. 본체는 /layout 으로 따로 받는다 - 41 문서 STEP 1 참조" -ForegroundColor Yellow
Write-Host "  주의: 에뮬레이터는 Windows Pro/Enterprise/Education + Hyper-V 필요 (Home 불가)" -ForegroundColor Yellow

# ---------- 결과 ----------
Write-Host "`n=== 결과 ===" -ForegroundColor Cyan
$total = (Get-ChildItem $Root -Recurse -File | Measure-Object -Property Length -Sum).Sum
Log "총 다운로드 크기: $([math]::Round($total/1GB,2)) GB"

if ($failed.Count -gt 0) {
    Write-Host "`n실패한 항목 (수동으로 받을 것):" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Log "FAILED: $($failed -join ', ')"
} else {
    Write-Host "자동 다운로드 항목 전부 성공" -ForegroundColor Green
}

Write-Host "`n다음 수동 단계 (41-toolchain-archive.md):" -ForegroundColor Cyan
Write-Host "  STEP 2  Visual Studio 레이아웃 생성 (~45GB, 오래 걸림)"
Write-Host "  STEP 3  Unity 설치 + 라이선스 활성화 + .ulf 백업   <- 빠뜨리기 쉬움"
Write-Host "  STEP 4  Feature Tool 실행 -> OpenXR Plugin 1.11.2 tgz 확보   <- 가장 시급"
Write-Host "  STEP 5  문서 저장소 clone"
Write-Host "  STEP 6  HoloLens 기기 쪽 작업"
Write-Host "`n로그: $log`n"
