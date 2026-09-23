# 24. HoloLens 2 이식 계획

> 🔴 **이 문서의 §1을 읽기 전에 아무것도 시작하지 말 것.**
> 실행 체크리스트: [`41-toolchain-archive.md`](../40-checklists/41-toolchain-archive.md)

## 1. 즉시 알아야 할 사실 — 버전 충돌이 이미 존재한다

| 항목 | 현재 프로젝트 | HoloLens 2 상한 |
|---|---|---|
| Unity 에디터 | **6000.3.10f1** | **6000.0.49f1** (또는 2022.3.62f1) |

Microsoft 공식 문서:

> *"After June 23, 2025, new versions of either the Unity editor or the Unity OpenXR Plugin package don't contain support for HoloLens 2. Unity editor and Unity OpenXR Plugin released after this date can't be used to build HoloLens 2 apps."*
> — [Choosing a Unity version](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/choosing-unity-version)

**따라서 HL2 이식은 업그레이드가 아니라 다운그레이드다.** 그리고 현재 프로젝트를 그대로 내리면 데스크톱 버전이 깨질 수 있다.

### 채택하는 전략: 별도 프로젝트 분기

```
MediapipeExample/               ← 데스크톱, Unity 6000.3.10f1, 손대지 않음
MediapipeExample-HL2/           ← 신규 분기, Unity 2022.3.62f1
```

- 씬과 에셋을 재사용하되 **프로젝트를 분리**한다
- 공유되는 것: `Assets/Scripts/` 런타임 코드(가능한 범위), 시나리오 JSON, 실험 프로토콜
- 분리되는 것: 프로젝트 설정, 패키지, 입력 계층, HUD 렌더 방식

**왜 2022.3.62f1인가:** Unity 6000.0.49f1도 가능하지만, MRTK3 v3.3.0이 Unity 2022.3 LTS를 요구사항으로 명시하고, 2022.3.62f1은 공개 다운로드 가능한 마지막 2022.3 패치이기도 하다. 이후 패치(2022.3.63f1+)는 Enterprise 라이선스 전용이다.

## 2. 고정할 버전 (이 조합에서 벗어나지 말 것)

| 구성요소 | 버전 | 출처 |
|---|---|---|
| Unity 에디터 | **2022.3.62f1** | Unity Archive |
| Unity OpenXR Plugin | **1.14.3** 이하 | HL2 지원 마지막 |
| Mixed Reality OpenXR Plugin | **1.11.2** (2024-12-05) | [MR Feature Tool](https://aka.ms/MRFeatureTool) |
| **MRTK3 Core** | **v3.3.0** (2025-11-21) | [GitHub](https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity) |
| XR Interaction Toolkit | 3.0.x | MRTK3 요구사항 |
| Visual Studio | 2022 | UWP 워크로드 필수 |
| Windows SDK | **10.0.19041.0** | UWP C++ (v142) 도구 포함 |

### 🔴 절대 금지

**MRTK v4.0.0-pre.\* 사용 금지.** v4.0.0-pre.3이 최소 에디터를 **6000.0.66f2**로 올렸는데, 이는 HL2 상한(6000.0.49f1)을 초과한다. Unity Hub나 Package Manager의 업그레이드 프롬프트를 절대 수락하지 말 것.

⚠️ MRTK가 HL2 지원을 공식적으로 중단한다는 발표는 확인하지 못했다. 위는 버전 요구사항으로부터의 추론이다. v4 계열을 쓰려면 직접 검증할 것.

### 필수 설정 — 첫날에 할 것

**Player Settings → Auto Graphics API 해제 → Direct3D11 추가, Direct3D12 제거**

HL2의 DX12 성능 저하는 Unity 2021.3.0f1부터 Unity 6까지 존재하며, 2024-05-23부터 "Investigating" 상태로 **영구 미해결**이다. 툴체인이 동결되었으므로 앞으로도 고쳐지지 않는다.

## 3. HoloLens 2 수명주기

| 항목 | 상태 |
|---|---|
| 생산 | **2024년 12월 종료** |
| 마지막 기능 업데이트 | 2024년 11월, 빌드 22621.1409 |
| 최신 OS 빌드 | Windows Holographic 24H1 — **2026년 8월 업데이트, 빌드 22621.1553** (보안 전용) |
| 보안 서비스 종료 | **2027년 12월** |
| 사이드로딩 | 로컬 전용, 클라우드 의존 없음 → **2027년 12월 이후에도 커스텀 앱 배포 가능** |

출처: [HoloLens release notes](https://learn.microsoft.com/en-us/hololens/hololens-release-notes)

**연구 기간(2026-12 ~ 2027-03) 동안 지원은 유지된다.** 문제는 지원이 아니라 하드웨어 대체 불가능성이다.

⚠️ HoloLens는 Microsoft의 표준 수명주기 데이터베이스에 등재되어 있지 않다. Dec 2027 날짜는 릴리스 노트에만 존재한다. 심사위원이 출처를 물으면 이 사실을 알고 있을 것.

## 4. 입력 계층 이식 — 여기가 작업의 본체

### 대응 관계

| 데스크톱 | HoloLens 2 |
|---|---|
| MediaPipe 팔 레이 (`PosePointerLineRenderer`) | **hand ray** (MRTK3 `ArticulatedHandController`) |
| — | **eye gaze** (선택적 추가 조건) |
| — | head gaze (폴백) |
| `PointerRaycaster` | **거의 그대로 재사용** — 레이 원점/방향 소스만 교체 |
| `PatientDwellSelector` | **그대로 재사용** |
| `PatientView` | **그대로 재사용** |
| 숫자키 판단 입력 | 손 제스처 또는 음성 명령 |

**설계상의 행운:** `PointerRaycaster`가 `pointerLine.CurrentDirection`과 `rayOrigin`만 소비하도록 이미 분리되어 있다. 인터페이스 하나만 추상화하면 나머지 선택 파이프라인은 손대지 않아도 된다.

### 권장 리팩터링 (데스크톱 단계에서 미리)

```csharp
public interface IPointerSource
{
    bool IsActive { get; }
    Vector3 Origin { get; }
    Vector3 Direction { get; }
}
```

`PosePointerLineRenderer`, `MousePointerSource`, (향후) `HandRayPointerSource`, `EyeGazePointerSource`가 이를 구현한다. **이 추상화를 Level 1 단계에서 미리 넣어두면 HL2 이식이 며칠 단축된다.** → [`32-task-backlog.md`](../30-planning/32-task-backlog.md) L1-7

## 5. HL2 네이티브 입력 사양

### 손 추적
- 양손 완전 관절 모델, 1MP ToF 깊이 센서 + 가시광 카메라 4개
- MRTK3의 `MRTKHandsAggregatorSubsystem`으로 관절 조회 (하위 subsystem 직접 접근 금지)
- 정확도 (Vicon 대비, *Virtual Worlds* 2025): **손끝 위치 오차 2~4mm** (검지 끝 3.9mm), Pearson r = 0.99. 관절 각도 오차 평균 5.36°±5.31°
- 에디터 내 입력 시뮬레이션: `SyntheticHandsSubsystem` — **파일럿 전 로직 검증에 유용**

### 시선 추적
- 표준 API: **~30 Hz, 결합 시선만**
- **Extended Eye Tracking SDK** (NuGet `Microsoft.MixedReality.EyeTracking`, Gaze Input capability 필요): 좌/우 개별 시선, **30/60/90 fps 선택**
- 정확도: **±1.5° 시야각.** Microsoft는 타겟에 **2.0~3.0° 여유**를 권장
- **캘리브레이션 필수이며 실패한다:** 옵트아웃 사용자, 일부 콘택트/안경, 안질환·수술 이력, 바이저 오염, 직사광, 머리카락 가림
- Microsoft 권장 폴백: 500~1500ms 타임아웃 후 head gaze로 전환

🔴 **실험 설계 영향:** 시선 캘리브레이션 실패는 실질적 제외 사유다. **참가자별 캘리브레이션 상태를 로그에 남기고**, 제외 규칙과 폴백을 사전 등록한다.

### FOV
**52° 대각** (약 43°H × 29°V).

⚠️ 52°는 공개 당시 Microsoft 발표 수치이며 보편적으로 인용되나, 현재 [HL2 하드웨어 페이지](https://learn.microsoft.com/en-us/hololens/hololens2-hardware)에는 기재되어 있지 않다. 그 페이지의 **96.1° 대각은 가시광 추적 카메라 수치이지 디스플레이 FOV가 아니다.** 혼동하지 말 것.

**리뷰어 대응:** FOV를 apparatus 절에 명시하고, 자극을 FOV 안에 들어오도록 설계하며, 그 제약을 사전 등록한다. 참가자가 FOV 밖을 탐색해야 하는 설계는 생태학적 타당성 비판을 자초한다. 단, RQ3(화면 밖 안내)은 이 제약을 **의도적으로 독립변수로 삼으므로** 예외다.

### 기타
- 음성: 온디바이스 command-and-control 동작 (Cortana 의존 경로는 제거됨)
- 공간 매핑: MRTK3에서는 ARFoundation의 `ARMeshManager`/`ARPlaneManager` 사용. **MRTK3는 MRTK2의 Scene Understanding observer를 제거했다** — 공간 메시가 필요하면 시간 예산에 반영
- Holographic Remoting: Unity Play Mode → HL2 실기 (Wi-Fi/USB). **반복 속도에 결정적.** Player는 Store에서 (`9nblggh4sv40`), v2.9.0은 QR 역방향 연결 지원. HL2용 remote 앱은 NuGet 2.x.x 사용 (1.x는 HL1 전용)

## 6. 이식 작업 분해

| # | 작업 | 예상 | 비고 |
|---|---|---|---|
| H-0 | **툴체인 아카이브** | 0.5일 | [`41`](../40-checklists/41-toolchain-archive.md) — 최우선 |
| H-1 | 빈 MRTK3 씬을 실기에 배포 검증 | 1일 | **다른 모든 작업의 선행 조건** |
| H-2 | 프로젝트 분기 + Unity 2022.3.62f1 다운그레이드 | 2일 | 에셋 재임포트 시간 포함 |
| H-3 | 씬 이식 (지하철/플랫폼/환자 10명) | 3일 | 외부 에셋 재설치 필요 |
| H-4 | `IPointerSource` 구현: hand ray | 2일 | 데스크톱에서 추상화 완료 시 단축 |
| H-5 | eye gaze 소스 + 캘리브레이션 상태 로깅 | 2일 | Extended SDK |
| H-6 | HUD를 Screen Space Overlay → World/Follow 방식으로 | 3일 | **52° FOV 대응 재설계 필요** |
| H-7 | 월드 카드 가독성 조정 (거리·크기·대비) | 2일 | 가산 디스플레이 특성 |
| H-8 | 판단 입력: 손 제스처/음성 | 2일 | |
| H-9 | 로깅 이식 (파일 경로, UWP 권한) | 1일 | UWP는 파일 접근이 다르다 |
| H-10 | 실기 파일럿 5명 | 3일 | 캘리브레이션 실패율 측정 |
| | **합계** | **약 21일 = 4~5주** | |

## 7. HUD 재설계 — 52°의 의미

데스크톱 HUD는 화면 가장자리를 자유롭게 쓴다. 52° 대각에서는 그 공간이 없다.

| 요소 | 데스크톱 | HL2 대응 |
|---|---|---|
| LINK/POSE 상태 | 좌상단 고정 | 가장자리 대신 **하단 중앙 소형**, 또는 요청 시에만 |
| 좌우 방향 안내 | 화면 좌우 끝 | **시야 가장자리 화살표** — 공간이 좁아 더 중요해짐 |
| NEARBY PATIENTS 4행 | 좌측 패널 | **2행으로 축소** 또는 head-locked 대신 body-locked follow |
| UNCONFIRMED/CHECKED | 상단 | 유지 (작게) |
| SIMULATION ONLY | 상단 | **반드시 유지** |

**연구적 기회:** 이 축소 자체가 RQ3(FOV 예산 하의 안내)의 자연스러운 실험 조건이 된다. 데스크톱에서 FOV를 소프트웨어로 제한한 조건과 HL2 실측을 대비시키면 강한 결과가 된다.

## 8. 리스크

| 리스크 | 영향 | 대응 |
|---|---|---|
| **기기 고장** | 연구 중단 (대체 구매 불가) | **모집 전 예비 기기 확보 경로 확인** |
| 툴체인 다운로드 링크 소멸 | 이식 불가 | [`41`](../40-checklists/41-toolchain-archive.md) 즉시 실행 |
| 버전 드리프트 | 빌드 실패 | `packages-lock.json` 커밋, 업그레이드 프롬프트 거부 |
| 외부 에셋 재설치 실패 | 씬 복원 불가 | `ASSET_SETUP.md` 작성, 라이선스 확인 |
| Unity 버그 (수정 없음) | 일정 지연 | 최소 1건은 직접 우회한다고 가정하고 여유 확보 |
| 시선 캘리브레이션 실패율 높음 | N 감소 | 파일럿 5명 선행, head gaze 폴백 사전 등록 |
| FOV 생태학적 타당성 비판 | 리뷰 리스크 | 사전 등록 + apparatus 명시 + FOV를 변수로 전환 |
| UWP 파일 권한 | 로그 유실 | H-9에서 조기 검증 |

## 9. Quest 3 대안 (참고)

HL2가 실패할 경우의 대비책으로 기록한다.

| | HoloLens 2 | Quest 3 / 3S |
|---|---|---|
| AR 방식 | **광학 투시** | 비디오 패스스루 (컬러) |
| FOV | 52° 대각 | ~110° 수평 |
| 손 추적 | 있음 | 있음 |
| **시선 추적** | **있음** (±1.5°, 30/60/90Hz) | **없음** (Quest Pro만) |
| 카메라 접근 | Research Mode | **Passthrough Camera API** (프로덕션, ≤1280×1280@60Hz) |
| Unity | **동결: 2022.3.62f1 / 6000.0.49f1** | **현행: 6000.0.66f2+** |
| 배포 | UWP → VS2022 → 인증서 | Android → ADB 사이드로드 |
| 하드웨어 | **구매 불가** | 생산 중, 저렴 |

🔴 **한 프로젝트로 둘 다 타깃할 수 없다.** Meta 현행 SDK는 Unity ≥6000.0.66f2를 요구하고 HL2 상한은 6000.0.49f1이다. 겹치지 않는다.

**판단:** 시선 추적 또는 광학 투시가 연구 질문에 필수면 HL2. 아니면 Quest 3이 명백히 마찰이 적다. 다만 광학 투시 vs 비디오 패스스루는 실제 구성 차이이므로, 논문의 기여가 OST AR에 관한 것이면 Quest 3은 대체재가 아니며 리뷰어가 지적한다.

현재 연구 질문(불확실성 표현)은 **시선 추적을 필수로 요구하지 않는다.** HL2를 쓰는 이유는 ① 이미 보유 ② 광학 투시가 "AR 글래스" 주장에 부합 ③ 시선 조건을 추가하면 기여가 커짐 — 이 셋이다. 파일럿에서 툴체인이 무너지면 Quest 3 구매(약 70만원)를 즉시 검토한다.
