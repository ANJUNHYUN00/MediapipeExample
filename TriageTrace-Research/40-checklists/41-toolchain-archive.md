# 41. HoloLens 2 툴체인 아카이브 — 따라하기

> 🔴 이번 주에 할 것 · 실제 작업 시간 1~2시간 + 다운로드 대기 몇 시간
> 링크 확인 시점: **2026-09-16** · 자동 스크립트: [`../tools/archive-toolchain.ps1`](../tools/archive-toolchain.ps1)

## 왜 하는가

HoloLens 2용 Unity 지원은 **2025년 6월 23일자로 동결**됐다. 이후 나온 Unity로는 HL2 빌드가 불가능하다. 지금은 필요한 파일이 전부 받아지지만, 내년 3월에도 그럴 보장이 없다.

조사 과정에서 **예상보다 나쁜 사실 3가지**가 나왔다. 이게 이 문서의 핵심이다.

| 발견 | 의미 |
|---|---|
| 🔴 **Unity Personal은 오프라인 활성화가 불가능하다** | 설치 파일을 다 받아둬도 미래에 에디터가 안 열릴 수 있다. → STEP 3 |
| 🔴 **OpenXR Plugin 1.11.2는 직접 다운로드 URL이 없다** | Feature Tool을 **지금** 돌려서 tgz를 뽑아둬야 한다. MS 카탈로그가 죽으면 끝. → STEP 4 |
| 🟠 **Visual Studio에 "UWP 워크로드"가 더 이상 없다** | 기존 안내대로 하면 실패한다. 컴포넌트 그룹으로 바꿔야 한다. → STEP 2 |

추가로: Microsoft Learn의 "PDF 다운로드" 기능이 사라졌다. 대신 문서를 GitHub에서 통째로 clone한다. → STEP 5

---

# STEP 0 · 저장 위치 정하기 ⏱ 5분

## 필요한 공간

2026-09-23 실측 (조사 당시 추정치와 크게 다르다 — 특히 VS 레이아웃):

| 항목 | 크기 |
|---|---|
| Unity 에디터 | 3.5 GB |
| UWP Build Support | 293 MB |
| Windows IL2CPP Support | 97.5 MB |
| Visual Studio 레이아웃 (7개 ID, en-US + ko-KR) | **9.2 GB** (~45GB 아님) |
| Windows SDK ISO | 754 MB |
| HoloLens 2 에뮬레이터 | 웹 설치기 1.3 MB + `/layout` 본체 **4.13 GB** (STEP 1 참조) |
| 문서 저장소 | ~1 GB (미측정) |
| MRTK3 + Feature Tool + 나머지 | ~0.13 GB |
| **합계** | **약 19 GB** (문서 저장소는 추정) |

32GB 이상 USB면 들어간다. 이중 보관을 생각하면 64GB 이상.

## USB 확인 — 이것부터

PowerShell에서:

```powershell
Get-Volume | Select-Object DriveLetter, FileSystemLabel, FileSystem, `
  @{n='SizeGB';e={[math]::Round($_.Size/1GB,1)}}, `
  @{n='FreeGB';e={[math]::Round($_.SizeRemaining/1GB,1)}}
```

- [ ] USB의 `FileSystem` 확인

| 결과 | 조치 |
|---|---|
| **exFAT** 또는 **NTFS** | 그대로 진행 |
| 🔴 **FAT32** | **포맷 필요.** 파일 하나가 4GB를 못 넘어서 에뮬레이터·ISO가 안 들어간다 |

FAT32라면 (USB를 비우고):

```powershell
Format-Volume -DriveLetter [USB드라이브문자] -FileSystem exFAT -NewFileSystemLabel "HL2Archive"
```

⚠️ 포맷하면 USB 내용이 전부 지워진다. 드라이브 문자를 두 번 확인할 것.

- [ ] USB 포트 확인 — **USB 3.0(파란색 포트)에 꽂을 것.** 2.0이면 60GB 복사에 6시간 넘게 걸린다

## 어디에 받을 것인가

**내장 디스크에 25GB 여유가 있으면:**

```
1차: D:\Archive\HoloLens2-Toolchain-2026-09\   ← 여기로 다운로드 (빠름)
2차: USB                                        ← 다 받은 뒤 복사 (이중 보관)
```

이게 낫다. VS 레이아웃은 작은 파일 수만 개라서 USB에 직접 쓰면 훨씬 느리다.

**내장에 공간이 없으면:** USB를 1차 저장소로 쓴다. 느릴 뿐 문제는 없다.

- [ ] 1차 저장 경로 결정: `________________________`
- [ ] ⚠️ 경로를 **짧게** 유지할 것. Visual Studio 레이아웃은 경로가 **80자 미만**이어야 한다. `E:\HL2\` 정도가 안전하다

## PC 확인

- [ ] Windows 에디션 확인 (`winver`) — **Home이면 에뮬레이터를 못 쓴다** (Pro/Enterprise/Education + Hyper-V 필요). 실기가 있으니 Home이어도 나머지는 그대로 진행한다
- [ ] 노트북이면 **전원 연결**. 다운로드 도중 절전으로 들어가지 않게 설정

---

# STEP 1 · 자동 다운로드 ⏱ 5분 작업 + 대기

받을 수 있는 건 스크립트가 다 받는다. 중간에 끊겨도 **다시 실행하면 이어받는다.**

```powershell
# TriageTrace-Research\tools 폴더에서
# STEP 0 에서 정한 경로로
powershell -ExecutionPolicy Bypass -File .\archive-toolchain.ps1 -Root "E:\HL2"

# 기본값(D:\Archive\HoloLens2-Toolchain-2026-09)을 쓰려면 -Root 생략
powershell -ExecutionPolicy Bypass -File .\archive-toolchain.ps1
```

받는 것 (2026-09-23 실측):

| 항목 | 크기 |
|---|---|
| Unity 2022.3.62f1 에디터 | 3.5 GB |
| UWP Build Support 모듈 | 293 MB |
| Windows IL2CPP Support | 97.5 MB |
| MRTK3 패키지 12개 (.tgz) | ~57 MB |
| Visual Studio 부트스트래퍼 | 4.3 MB |
| Windows 10 SDK ISO | 754 MB |
| Windows SDK 부트스트래퍼 | 1.4 MB |
| HoloLens 2 에뮬레이터 | ⚠️ **웹 설치기 1.3 MB** — 본체 아님 (아래 참조) |

- [ ] 스크립트 실행
- [ ] 끝나고 "실패한 항목" 목록 확인 → 있으면 아래 URL로 수동 다운로드
- [ ] "전부 성공"이어도 **파일을 열어 확인**할 것 — exe는 서명(`Get-AuthenticodeSignature`), tgz는 gzip 헤더 `1F 8B`, ISO는 오프셋 0x8001의 `CD001`

### ⚠️ 에뮬레이터는 웹 설치기다

fwlink(`linkid=2290700`)는 msi가 아니라 `HoloLensEmulatorSetup.exe`(WiX Burn 번들, 1.3 MB)로 연결된다. 이것만 보관하면 링크가 죽는 순간 본체를 못 받는다. 본체는 `/layout`으로 따로 받는다:

```powershell
.\HoloLens2EmulatorSetup-24H1-10.0.22621.1402.exe /layout _Root_\emulator\layout /quiet /log _Root_\_logs\emulator-layout.log
```

Home 에디션에서도 `/layout`은 설치가 아니라 다운로드라서 실행된다 (에뮬레이터 실행만 불가).

2026-09-23 실측: 22분, 72개 파일 **4.13 GB**, 로그 `Apply complete, result: 0x0`, 에러(`e***`) 0건. 받은 패키지 6개 — .NET Framework 4.5, Kits Configuration Installer, Microsoft Emulator(x86/x64), HoloLens Emulator, HoloLens Emulator Image(`vh1~vh*.cab`). 로그의 `w343: Prompt for source of package`는 로컬 확인 후 다운로드로 넘어가는 정상 경고다.

- [ ] 에뮬레이터 `/layout` 실행 (Home이면 우선순위 낮음, 그래도 받아둘 것)

**수동으로 받아야 하는 것 1개 (다운로드 센터라 자동화 불가):**

- [ ] **Mixed Reality Feature Tool** → https://www.microsoft.com/en-us/download/details.aspx?id=102778
  - `MixedRealityFeatureTool.exe`, 68.1 MB, 버전 1.0.2209.0
  - `_Root_\mrfeaturetool\` 에 저장

> 💡 Unity 다운로드는 **로그인이 필요 없다.** `download.unity3d.com` 직접 URL을 쓰기 때문이다. 리비전 해시 `4af31df58517`을 기록해 둘 것 — 아카이브 목록에서 버전이 사라져도 이 URL은 살아있었던 전례가 있다.

---

# STEP 2 · Visual Studio 레이아웃 ⏱ 10분 작업 + 몇 시간 대기

제일 오래 걸리니 **STEP 1 끝나면 바로 걸어두고 나머지를 진행**한다.

### ⚠️ 기존 안내는 틀렸다

예전 문서들이 말하는 `Microsoft.VisualStudio.Workload.Universal`(UWP 워크로드)은 **현재 Visual Studio 2022 설치 관리자에 존재하지 않는다.** 그대로 실행하면 실패하거나 조용히 무시된다.

대신 **컴포넌트 그룹**을 쓴다.

### ⚠️ v142와 SDK 19041은 자동으로 들어오지 않는다

`ComponentGroup.UWP.VC`는 **v143 툴셋만** 가져온다. `--includeRecommended`를 붙여도 v142는 빠진다. Windows SDK도 최신 Win11 SDK(10.0.26100)만 들어오고 **10.0.19041은 들어오지 않는다.** 둘 다 별도 ID로 명시해야 한다. (2026-09-23 실측. 5개 ID만으로 받은 레이아웃에 두 패키지가 없었다.)

```powershell
cd D:\Archive\HoloLens2-Toolchain-2026-09\visualstudio

.\vs_community.exe --layout D:\Archive\HoloLens2-Toolchain-2026-09\visualstudio\layout `
  --add Microsoft.VisualStudio.Workload.ManagedDesktop `
  --add Microsoft.VisualStudio.Workload.NativeDesktop `
  --add Microsoft.VisualStudio.Workload.ManagedGame `
  --add Microsoft.VisualStudio.ComponentGroup.UWP.VC `
  --add Microsoft.VisualStudio.ComponentGroup.UWP.Support `
  --add Microsoft.VisualStudio.ComponentGroup.UWP.VC.v142 `
  --add Microsoft.VisualStudio.Component.Windows10SDK.19041 `
  --includeRecommended --lang en-US ko-KR --passive --wait
```

| ID | 무엇 |
|---|---|
| `Workload.ManagedDesktop` | .NET 데스크톱 |
| `Workload.NativeDesktop` | C++ 데스크톱 |
| `Workload.ManagedGame` | **Unity 게임 개발** |
| `ComponentGroup.UWP.VC` | UWP용 C++ 도구 (**v143만**, ARM64 포함) |
| `ComponentGroup.UWP.Support` | UWP 지원 |
| `ComponentGroup.UWP.VC.v142` | UWP용 C++ v142 툴셋 — **별도 ID** |
| `Component.Windows10SDK.19041` | Windows 10 SDK 10.0.19041 — **별도 ID** (MANIFEST 버전 고정값) |

- `--passive`는 진행 창만 띄우고 입력을 요구하지 않는다. `--wait`가 없으면 부트스트래퍼가 즉시 종료되어 종료 코드를 받을 수 없다
- 이미 받은 레이아웃에 `--add`를 늘려 다시 실행하면 **빠진 것만 추가로 받는다.** 기존 `--add`는 빼지 말고 그대로 둘 것 (`Layout.json`에 전체 구성이 기록되게)

- [ ] 레이아웃 생성 실행
- [ ] 완료까지 대기 (실측 9.2GB. USB 3.0 직접 저장 기준 5개 ID 18분 + 2개 추가 7분)
- [ ] 완료 후 `layout\` 폴더 크기 확인
- [ ] 종료 코드 0이어도 `layout\` 에 `Win10SDK_10.0.19041*` 와 v142 패키지 폴더(`Microsoft.VC.14.29.16.11.*`, `*.v142,*`)가 있는지 확인

⚠️ 레이아웃 경로는 **80자 미만**이어야 한다 (Microsoft 제약).

⚠️ **왜 서두르는가:** Visual Studio Community는 "최신 안정 버전만 지원" 정책이고 VS 2026이 이미 나왔다. VS 2022 부트스트래퍼의 장기 가용성은 보장되지 않는다.

---

# STEP 3 · Unity 설치 + 라이선스 백업 ⏱ 30분 🔴

**이 단계가 가장 빠뜨리기 쉽고, 빠뜨리면 아카이브 전체가 무용지물이 된다.**

### 문제

Unity **Personal 라이선스는 오프라인 수동 활성화를 지원하지 않는다.** Unity 공식 문서에 명시되어 있고, 2023년 8월에 Personal의 `.alf`/`.ulf` 수동 활성화가 폐지됐다. 즉 **설치 파일을 모두 보관해도 Unity 라이선스 서버에 연결되지 않으면 에디터를 열 수 없다.**

### 대응

- [ ] `unity\UnitySetup64-2022.3.62f1.exe` 실행해서 설치
- [ ] `unity\UnitySetup-UWP-Support-2022.3.62f1.exe` 실행 (에디터 설치 후)
- [ ] `unity\UnitySetup-Windows-IL2CPP-2022.3.62f1.exe` 실행
- [ ] Unity Hub에서 로그인 → **지금 활성화**
- [ ] 🔴 **라이선스 파일 백업**
  ```powershell
  Copy-Item "C:\ProgramData\Unity\Unity_lic.ulf" `
    -Destination "D:\Archive\HoloLens2-Toolchain-2026-09\unity\Unity_lic.ulf.backup"
  ```
- [ ] 에디터가 실제로 열리는지 확인 (빈 3D 프로젝트 하나 생성)

### 더 안전한 길

- [ ] **학교에 Unity Education/Enterprise 라이선스가 있는지 확인.** 이쪽은 오프라인 수동 활성화를 지원해서 훨씬 안전하다. 서강대 소프트웨어융합 쪽이나 학과 사무실에 문의해 볼 것

---

# STEP 4 · Feature Tool 실행 ⏱ 20분 🔴 **가장 시급**

### 왜 가장 시급한가

**Mixed Reality OpenXR Plugin 1.11.2는 직접 다운로드 URL이 없다.** GitHub 릴리스에 `.tgz`가 첨부되어 있지 않고, NuGet에도 없다. **Feature Tool로 Microsoft 카탈로그에서 받는 것이 확인된 유일한 경로**다. 그 카탈로그가 내려가면 영구히 못 받는다.

Feature Tool 실행 파일(68MB)을 보관하는 것으로는 부족하다. **카탈로그의 클라이언트일 뿐**이라, 보관해야 하는 건 실행 파일이 아니라 **뽑아낸 tgz 파일**이다.

### 절차

Feature Tool은 Unity 프로젝트 폴더를 요구한다 (`Assets`, `Packages`, `ProjectSettings` 세 폴더가 있어야 함).

- [ ] Unity Hub에서 **빈 3D 프로젝트** 생성 — 이름 `MRTK-Archive-Dummy`, 버전 2022.3.62f1
- [ ] `mrfeaturetool\MixedRealityFeatureTool.exe` 실행
- [ ] **Start** → 방금 만든 더미 프로젝트 폴더 지정 → **Discover Features**
- [ ] 아래를 체크:
  - [ ] **Platform Support → Mixed Reality OpenXR Plugin → 1.11.2** ← 🔴 이게 목적
  - [ ] Platform Support 아래의 다른 항목도 가능한 것 전부 (나중에 뭐가 필요할지 모른다)
- [ ] **Get Features** → **Import** → **Approve**
- [ ] 🔴 **결과물 복사**
  ```powershell
  Copy-Item "<더미프로젝트>\Packages\MixedReality\*" `
    -Destination "D:\Archive\HoloLens2-Toolchain-2026-09\mrfeaturetool\packages\" -Recurse
  ```
- [ ] `.tgz` 파일이 실제로 들어왔는지 눈으로 확인
- [ ] 더미 프로젝트의 `Packages\manifest.json`도 같이 백업 (버전 참조가 들어있다)

> 💡 MRTK3 v3.3.0(2025-11)은 Feature Tool 카탈로그에 없을 가능성이 높다 — Feature Tool 자체가 2022년 빌드다. 그래서 STEP 1에서 GitHub `.tgz`를 따로 받아둔 것이다. **두 경로 모두 확보해 두는 게 맞다.**

---

# STEP 5 · 문서 아카이브 ⏱ 5분

Microsoft Learn의 "Download PDF" 기능은 사라졌다. 인쇄도 빈 페이지가 나온다. 대신 **문서 원본이 GitHub에 공개되어 있다.**

```powershell
cd D:\Archive\HoloLens2-Toolchain-2026-09\docs
git clone --depth 1 https://github.com/MicrosoftDocs/mixed-reality.git
```

- [ ] clone 실행 (Mixed Reality 문서 전체가 Markdown + 이미지로 들어온다)
- [ ] 아래 핵심 문서는 브라우저에서 **인쇄 → PDF로 저장**도 해두기 (페이지를 끝까지 스크롤한 뒤 인쇄해야 빈 페이지가 안 나온다)
  - [ ] [Choosing a Unity version](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/choosing-unity-version) ← **버전 상한의 1차 근거, 논문에서 인용할 것**
  - [ ] [HoloLens release notes](https://learn.microsoft.com/en-us/hololens/hololens-release-notes) ← **수명주기 날짜의 유일한 출처**
  - [ ] [Known issues](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/known-issues) ← DX12 버그
  - [ ] [Eye tracking](https://learn.microsoft.com/en-us/windows/mixed-reality/design/eye-tracking)

---

# STEP 6 · HoloLens 기기 쪽 ⏱ 20분

### Holographic Remoting Player

이건 **아카이브가 불가능하다.** Microsoft Store 전용 UWP 앱이고 `.appx` 공개 다운로드가 없다. (참고로 2026-09-14에도 업데이트된 걸 보면 아직 살아 있다.)

**대응: 지금 기기에 설치해 두고, 공장 초기화를 하지 않는다.**

- [ ] HoloLens에서 Store 열기 → "Holographic Remoting Player" 설치
- [ ] 한 번 실행해서 대기 화면이 뜨는지 확인

### PC 쪽 Remoting 패키지

- [ ] NuGet 패키지 보관
  ```powershell
  cd D:\Archive\HoloLens2-Toolchain-2026-09\remoting
  nuget install Microsoft.Holographic.Remoting.OpenXr -Version 2.9.4
  ```
  (nuget.exe가 없으면 https://www.nuget.org/packages/Microsoft.Holographic.Remoting.OpenXr 에서 "Download package" 클릭)

### 기기 상태 기록

- [ ] 설정 → 시스템 → 정보 → **OS 빌드 번호** 기록: `________________`
      (기대값 **22621.1553** 또는 그 이전)
- [ ] 설정 → 업데이트 → 개발자 → **개발자 모드 켜기**
- [ ] **Device Portal** 접속 확인, IP 기록: `________________`
- [ ] **시선 캘리브레이션 1회 수행** — 본인 눈으로 성공하는지 (안경 쓰고도 되는지)
- [ ] 배터리 상태, 충전 케이블, 안면 패드 점검
- [ ] 보증/수리 경로 확인 (구매처, 학교 자산인지)

---

# STEP 7 · MANIFEST 작성 + 검증 ⏱ 15분

- [ ] 아카이브 폴더 루트에 `MANIFEST.md` 만들기 (아래 템플릿)
- [ ] **인스톨러 1개를 실제로 실행해 보기** — 설치까지 안 해도 되고, 실행되면 파일이 온전한 것
- [ ] **USB로 복사** (내장에 1차 저장한 경우)

```powershell
# 이어받기 가능한 복사. 중간에 끊겨도 다시 실행하면 된다
robocopy "D:\Archive\HoloLens2-Toolchain-2026-09" "E:\HL2" /E /Z /R:3 /W:5 /TEE /LOG+:copy.log
```

- [ ] 복사 후 크기 비교로 검증

```powershell
$a = (Get-ChildItem "D:\Archive\HoloLens2-Toolchain-2026-09" -Recurse -File | Measure-Object Length -Sum)
$b = (Get-ChildItem "E:\HL2" -Recurse -File | Measure-Object Length -Sum)
"원본: $($a.Count) 개, $([math]::Round($a.Sum/1GB,2)) GB"
"USB : $($b.Count) 개, $([math]::Round($b.Sum/1GB,2)) GB"
```

파일 개수와 크기가 같아야 한다. 다르면 robocopy를 다시 실행한다.

- [ ] USB를 **안전하게 제거**하고 보관 장소를 기록: `________________`

```markdown
# HoloLens 2 Toolchain Archive
아카이브 일자 : 2026-__-__
사유          : HL2용 Unity 지원이 2025-06-23 동결. 배포 링크 소멸 대비.

## 버전 고정
Unity 에디터        : 2022.3.62f1  (revision 4af31df58517)
Unity OpenXR Plugin : 1.14.3 이하
MR OpenXR Plugin    : 1.11.2 (2024-12-05)
MRTK3 Core          : v3.3.0 (2025-11-21, tag core-v3.3.0)
XR Interaction TK   : 3.0.x
Visual Studio       : 2022 Community
Windows SDK         : 10.0.19041.0
HL2 에뮬레이터      : 10.0.22621.1402 (24H1, 2024-10-08)
Remoting OpenXr     : 2.9.4

## 금지
- MRTK v4.0.0-pre.*        : Unity 요구사항이 HL2 상한을 넘을 가능성
- com.microsoft.mrtk.*     : deprecated. org.mixedrealitytoolkit.* 를 쓸 것
- Unity 2022.3.63f1+       : Enterprise 라이선스 전용
- 2025-06-23 이후 Unity    : HL2 빌드 불가
- VS Workload.Universal    : 더 이상 존재하지 않음. ComponentGroup.UWP.* 사용

## 필수 설정
Player Settings → Auto Graphics API 해제 → D3D11만 남김 (DX12 영구 버그)

## 라이선스
Unity_lic.ulf 백업 : unity\Unity_lic.ulf.backup  (Personal은 오프라인 활성화 불가)
학교 Education 라이선스 확인 : ______________

## 기기
HL2 OS 빌드   : ________
개발자 모드   : ________
Device Portal : ________
시선 캘리브   : 성공 / 실패
예비 기기     : ________
```

---

# 완료 확인

```
D:\Archive\HoloLens2-Toolchain-2026-09\
├─ MANIFEST.md                          ← STEP 7
├─ unity\
│   ├─ UnitySetup64-2022.3.62f1.exe
│   ├─ UnitySetup-UWP-Support-2022.3.62f1.exe
│   ├─ UnitySetup-Windows-IL2CPP-2022.3.62f1.exe
│   └─ Unity_lic.ulf.backup             ← STEP 3 🔴
├─ mrtk\                                 12개 .tgz
├─ mrfeaturetool\
│   ├─ MixedRealityFeatureTool.exe
│   └─ packages\                        ← STEP 4 🔴 OpenXR Plugin tgz
├─ visualstudio\
│   ├─ vs_community.exe
│   └─ layout\                          ← STEP 2, ~9.2GB
├─ sdk\   winsdk-10.0.19041.0.iso
├─ emulator\
│   ├─ HoloLens2EmulatorSetup-24H1-10.0.22621.1402.exe   ← 웹 설치기 1.3MB
│   └─ layout\                          ← /layout 으로 받은 본체
├─ remoting\  Microsoft.Holographic.Remoting.OpenXr.2.9.4
└─ docs\  mixed-reality\                ← STEP 5
```

**🔴 표시 3개가 핵심이다.** 나머지는 나중에 다시 받을 수 있을지도 모르지만, 이 셋은 지금 아니면 못 만든다.

---

# 다음

- [ ] → **H-1 빈 MRTK3 씬을 실기에 배포** ([`../30-planning/32-task-backlog.md`](../30-planning/32-task-backlog.md))

10월 안에 끝내야 한다. 동결된 툴체인 프로젝트는 여기서 무너지고, **10월에 알면 복구되고 12월에 알면 안 된다.**

---

## 확인 상태

| 항목 | 상태 |
|---|---|
| Unity 2022.3.62f1 페이지·리비전 해시 | ✅ 확인 (2025-05-07 릴리스) |
| MRTK3 core-v3.3.0 태그·첨부파일·크기 | ✅ 확인 (2025-11-21) |
| Feature Tool 다운로드 센터 페이지 | ✅ 확인 (68.1MB, v1.0.2209.0) |
| OpenXR Plugin 1.11.2가 최종 버전 | ✅ 확인 (2024-12-05, 이후 21개월간 없음) |
| VS Universal 워크로드 부재 | ✅ 확인 (공식 워크로드 목록에 없음) |
| Windows SDK ISO fwlink | ✅ 확인 (2025-04-02 갱신) |
| 에뮬레이터 24H1 fwlink | ✅ 확인 (2024-10-08) · ⚠️ 2026-09-23 실측: msi가 아니라 웹 설치기. `/layout`으로 본체 4.13GB 확보됨 |
| Unity Personal 오프라인 활성화 불가 | ✅ 확인 (Unity 공식 문서) |
| Learn PDF 다운로드 폐지 | ✅ 확인 (2025-03 Q&A) |
| 각 파일의 실제 크기·HTTP 응답 | ✅ 2026-09-23 실측 — 전부 200, 크기는 STEP 0 표. 에뮬레이터 fwlink는 웹 설치기로 연결됨 |
| VS 레이아웃 명령의 실제 동작 | ✅ 2026-09-23 실측 — 7개 ID 조합 종료 코드 0, `_errors.log` 0바이트. 단 v142·SDK 19041은 별도 ID가 필요했음 |
| ⚠️ Feature Tool 카탈로그에 MRTK3 v3.3.0 존재 여부 | 미확인 — GitHub tgz를 따로 받는 이유 |
