# 21. 현 시스템 기술 스펙

> 근거: `C:\Projects\MediapipeExample` 실제 코드 (2026-09-16 확인)
> 현황 요약은 [`02-current-state.md`](../00-overview/02-current-state.md)

## 1. 데이터 흐름

```
웹캠 (OpenCV)
  └→ MediaPipe Pose Landmarker (pose_landmarker_lite.task)
       └→ rightShoulder(12) / rightElbow(14) / rightWrist(16)
            └→ pointing.py : 관절 위생 검사 → 팔꿈치 각도 → 포인터 좌표 → EMA 평활
                 └→ message_builder.py : pose_pointer v2 직렬화 + 계약 검증
                      └→ websocket_server.py : ws://127.0.0.1:8765 @ 15 Hz
                           └→ PoseWebSocketClient (Unity, 백그라운드 스레드)
                                └→ PoseMessageParser : DTO 검증
                                     └→ LatestPoseStateQueue : 스레드 안전 최신값
                                          └→ PoseReceiverBehaviour : 메인 스레드 전달
                                               └→ PosePointerLineRenderer : 방향 계산
                                                    └→ PointerRaycaster : Patient 레이어 raycast
                                                         └→ PatientDwellSelector : 0.7초 dwell
                                                              └→ PatientView : 상태 전이 + StateChanged
                                                                   ├→ WorldSpacePatientStatusCard
                                                                   └→ ARGuidanceHud (관찰 전용)
```

**핵심 설계 원칙:** 각 단계가 단방향이며, HUD는 어떤 입력에도 참여하지 않는다. `ARGuidanceHud`의 클래스 주석이 이를 계약으로 선언한다.

## 2. Python 측 상세

### 관절 품질 판정 (`pointing.py` + `PointingConfig`)

포인팅으로 인정되려면 모두 통과해야 한다:

| 검사 | 기본값 |
|---|---|
| 관절 가시성 | ≥ 0.5 (3개 모두) |
| 상완 길이 | ≥ 0.05 |
| 전완 길이 | ≥ 0.05 |
| 어깨–손목 거리 | ≥ 0.10 |
| 분절 길이 비율 | 0.25 ~ 4.0 |
| 팔꿈치 각도 | ≥ 150° (펴짐) |
| 연속 프레임 | ≥ 2 (`activation_frames`) |

통과 후 포인터 좌표 = 손목 위치 + 전완 방향 × `pointer_extension_factor(0.25)`, 이후 `pointer_center`/`pointer_gain`으로 보정, EMA(`alpha=0.35`, 최대 프레임 갭 2) 평활.

**실험적 함의:** 이 임계들이 전부 `frozen dataclass`로 노출되어 있어 CLI 인자로 조건화할 수 있다. 실제 실행 예시:

```powershell
.\.venv\Scripts\python.exe -m mediapipe_rps.app `
  --min-elbow-angle 100 --activation-frames 3 `
  --pointer-gain-x 2.0 --pointer-gain-y 1.8
```

### 추적 상태 3단계

`TrackingState`: `TRACKING` / `PARTIAL` / `LOST`. 각 상태가 메시지 계약에서 강제되므로, 로그에서 추적 품질 저하 구간을 신뢰성 있게 식별할 수 있다. **실험 중 `PARTIAL`/`LOST` 구간은 분석에서 제외 후보로 표시한다.**

## 3. Unity 측 상세

### 환경

| 항목 | 값 | 비고 |
|---|---|---|
| Unity | **6000.3.10f1** | 🔴 HL2 상한 초과 |
| 어셈블리 | `TriageTrace.Runtime.asmdef` | |
| 테스트 | EditMode / PlayMode csproj 별도 | |
| 직렬화 | `com.unity.nuget.newtonsoft-json` 3.2.2 | |
| XR | **없음** | |

### 레이어

| 인덱스 | 이름 | 용도 |
|---|---|---|
| 6 | `Patient` | raycast 대상 전용 |
| 8 | `FirstPersonHands` | 1인칭 렌더링 분리 |

### 선택 파이프라인

**`PointerRaycaster`**
- `maxDistance = 10.0f`
- `LayerMask patientLayerMask` (Inspector 지정)
- `QueryTriggerInteraction.Ignore`
- 포인터 라인 비가시 시 즉시 `ClearCurrentPatient()` — **팔을 내렸을 때 오선택 방지**
- 방향 벡터의 NaN/Inf 검사 후 진행
- `GetComponentInParent<PatientView>()` — 자식 콜라이더 계층 허용

**`PatientDwellSelector`**
- `dwellSeconds = 0.7f` — `[SerializeField] [Min(0.05f)]`, **Inspector 조건화 이미 가능**
- 대상이 바뀌면 타이머 리셋
- `_selectedCurrentDwellPatient` 플래그로 동일 대상 중복 선택 차단
- `IsChecked`인 환자는 재선택 불가
- 이전 선택 대상은 `SelectOff()` 처리
- **`ConfigureForTests(raycaster, seconds, card)` 공개 메서드 존재** → 실험 조건 주입 훅으로 그대로 사용 가능

### 상태 머신

```
Unseen ──HighlightOn──→ Highlighted ──SelectOn──→ InProgress ──MarkChecked──→ Checked
   ↑                         │                         │
   └────HighlightOff─────────┘                         │
   └────────────SelectOff──────────────────────────────┘
```

`public event Action<PatientView> StateChanged` 가 모든 전이에서 발화한다. **로깅 훅이 이미 존재한다** — 새 이벤트 시스템을 만들 필요가 없다.

### HUD 구조 (`ARGuidanceHud`)

- Screen Space Overlay Canvas, GraphicRaycaster 제거 + 전 graphic raycast target off → 입력 간섭 없음
- 텍스트 슬롯 8개: `zone`, `connection`, `pose`, `leftGuidance`, `rightGuidance`, `patientStatus`, `patientRows`, `teamSync`
- `maximumNearbyPatients = 4` — 카메라 기준 거리순 정렬 후 상위 4명
- `maximumSyncEvents = 3` — `PatientView.StateChanged` 구독으로 최근 확인 기록
- 미확인 amber / 확인 green
- 현재 문구: `LINK WAITING|CONNECTED`, `POSE WAITING|TRACKING`, `DIRECTION PLATFORM`, `SIMULATION ONLY`, `NEARBY PATIENTS`, `UNCONFIRMED`, `CHECKED`, `LOCAL TEAM SYNC`

**확장 시 유지할 계약:** HUD는 `poseReceiver`와 `PatientView` 상태를 **읽기만** 한다. 추정 결과 표시를 추가해도 이 원칙을 깨지 않는다.

### Editor 도구 — 실험 준비의 핵심 자산

| 도구 | 실험에서의 역할 |
|---|---|
| `StationEnvironmentGenerator` | 공간 재생성 |
| `TrainInteriorColliderGenerator` | 콜라이더 재생성 |
| `PrototypePatientPlacementMenu` | **시나리오 배치 재현** |
| `PrototypeDisasterDressingGenerator` | 재난 연출 재생성 (Undo 지원, 루트 교체 방식) |
| `PatientModelIntegrationMenu` | FBX 설치 (EditorWindow, 선택 기반 아님) |
| `TenPatientIdentityAndPlacementMenu` | **10명 ID 정규화 + collider/layer/anchor/overlap 검증** |
| `ARGuidanceHudInstaller` | HUD 재설치 |
| `FirstPersonPresentationInstaller` | 1인칭 표현 재적용 |

이 도구들이 있기 때문에 **"시나리오를 파일로 저장하고 메뉴로 복원"** 하는 기능을 비교적 적은 작업으로 만들 수 있다. 재현성 절의 근거가 된다.

## 4. 한국어 폰트 이슈

TMP 한국어 폰트 적용 시도 시 글리프 누락으로 □ 표시. 데모 안정성을 위해 영문 TMP 기본 폰트 + 영문 문구로 복구된 상태.

**국내 학회 투고 시 재발한다.** 대응:
- 스크린샷은 영문 HUD 그대로 사용하고 캡션으로 설명
- 또는 필요한 글리프만 포함한 TMP Font Asset을 정적 생성 (Dynamic 대신 Static, 필요 문자만)

## 5. 확장 시 깨지 않아야 할 계약

| 계약 | 근거 | 확장 시 주의 |
|---|---|---|
| 영상 프레임 외부 전송 금지 | `AGENTS.md` §3, `config.py` 호스트 검증 | 절대 완화하지 않는다 |
| HUD는 입력에 참여하지 않음 | `ARGuidanceHud` 클래스 주석 | 추정 표시 추가 시에도 유지 |
| 상호작용 상태 ≠ 트리아지 심각도 | `PatientView.cs` 주석 | **별도 컴포넌트로 분리** |
| pose_pointer v2 필드 의미 고정 | `message_builder.py` 검증 | 새 필드가 필요하면 v3으로 분기 |
| Patient 레이어 단일 raycast | `PointerRaycaster` | 단서 애니메이션이 콜라이더를 바꾸지 않게 |
| Editor 도구는 Undo 지원 | 전 Editor 파일 | 새 도구도 동일하게 |
| 씬 자동 저장 금지 | `AGENTS.md` 작업 규칙 | 유지 |

## 6. 확장 시 손대야 하는 지점

| 목표 | 파일 | 작업 |
|---|---|---|
| 단서 레이어 | 신규 `PatientUrgencyProfile.cs` | `PatientView`와 같은 GameObject, 별도 컴포넌트 |
| 애니메이션 | `PatientModelIntegrationMenu.cs` | Animator 비활성화 → 활성화 + Root Motion off |
| 로깅 | 신규 `ExperimentLogger.cs` | `PatientView.StateChanged` 구독 |
| 조건 전환 | 신규 `ExperimentConfig.cs` + `ExperimentRunner.cs` | ScriptableObject 또는 JSON |
| 추정 표시 | `ARGuidanceHud.cs` + `WorldSpacePatientStatusCard.cs` | 슬롯 추가 |
| 마우스 베이스라인 | 신규 `MousePointerSource.cs` | `PosePointerLineRenderer`와 동일 인터페이스 |
| dwell 조건화 | 이미 가능 | `ConfigureForTests` 또는 Inspector |
