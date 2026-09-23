# 02. 현재까지 구현된 것

> 확인 방법: `C:\Projects\MediapipeExample` 실제 파일 열람 (2026-09-16)
> 이 문서는 **코드로 확인된 것만** 기록한다. 대화에서 언급되었으나 파일로 확인하지 못한 항목은 `⚠️ 미확인`으로 표시한다.

## 1. 저장소 구조

```
C:\Projects\MediapipeExample\
├─ AGENTS.md                    # 최상위 안내서 + 비의료 경계 선언 (11.7 KB)
├─ docs/                        # 확정 문서 4편
│   ├─ project-plan.md
│   ├─ transition-plan.md
│   ├─ unity-space-and-first-person-methodology.md
│   └─ websocket-protocols.md
├─ Plan/                        # 설계 문서 01~09 (01~06 레거시 RPS, 07~09 활성)
├─ Tasks/                       # 작업 절차 01~18
├─ mds/                         # 조사/메모 19~34
├─ Mediapipe/                   # Python 파이프라인
└─ MediapipeUnity/              # Unity 프로젝트
```

문서가 이미 **34개 Task 단위로 분해되어 있고** 각 Task에 목표·범위·수용기준이 기록되어 있다. 이 방법론 자체가 논문 §구현 절에 쓸 수 있는 자산이다.

## 2. Python 파이프라인 (`Mediapipe/`)

### 구성

| 파일 | 크기 | 역할 |
|---|---|---|
| `app.py` | 10.7 KB | 실행 진입점, 루프 |
| `camera.py` | 3.3 KB | OpenCV 카메라 |
| `config.py` | 6.4 KB | 전 설정 dataclass (frozen) |
| `pose_tracker.py` | 6.4 KB | MediaPipe Pose Landmarker 실행 |
| `pose_models.py` | 5.9 KB | Joint / PosePointerState / TrackingState |
| `pointing.py` | 11.9 KB | 오른팔 기하 + 포인터 계산 + 안정화 |
| `message_builder.py` | 5.7 KB | pose_pointer v2 직렬화 + 계약 검증 |
| `websocket_server.py` | 8.2 KB | 로컬 WebSocket 게시 |
| `pose_debug.py` | 7.8 KB | 프리뷰 창 렌더 |

`gesture_classifier.py`, `hand_tracker.py`, `stabilizer.py`는 각 81/81/63 바이트 — 레거시 RPS 스텁이다.

### 확정된 파라미터 (`config.py` 기본값)

**PoseConfig**
- 모델: `models/pose_landmarker_lite.task` (5.78 MB)
- `num_poses = 1` (MVP는 1명만 강제, 위반 시 ValueError)
- detection / presence / tracking confidence = 0.5
- `min_right_arm_visibility = 0.5`

**PointingConfig**
- `min_elbow_angle_degrees = 150.0` — 팔이 펴진 상태 판정
- `activation_frames = 2` — 연속 프레임 조건
- `smoothing_alpha = 0.35`, `smoothing_max_frame_gap = 2`
- `pointer_extension_factor = 0.25`
- `pointer_center_x/y = 0.5`, `pointer_gain_x/y = 1.0` (런타임 보정 가능)
- 관절 길이 위생 검사: upper arm / forearm / shoulder-wrist 최소값 + 비율 0.25~4.0

**WebSocketConfig**
- `ws://127.0.0.1:8765`, `publish_hz = 15.0`
- 호스트가 `127.0.0.1` 또는 `localhost`가 아니면 **ValueError로 거부** — 외부 송출 차단이 코드에 강제되어 있다

### 사용 관절 — 3개뿐

MediaPipe Pose 인덱스 12(rightShoulder), 14(rightElbow), 16(rightWrist). 오른팔만 사용한다.

### 테스트

`pytest` 기반 9개 테스트 파일 + `tests/fixtures/messages/` 11개 JSON 픽스처. `test_pointing.py`가 12.4 KB로 가장 크다. 프로토콜 계약 테스트가 픽스처로 고정되어 있다.

## 3. 통신 프로토콜 — `pose_pointer` v2

`message_builder.py`가 강제하는 계약:

```json
{
  "type": "pose_pointer",
  "version": 2,
  "timestamp": 0,
  "sequence": 0,
  "tracking": "TRACKING | PARTIAL | LOST",
  "pointing": true,
  "pointer": {"x": 0.5, "y": 0.5},
  "joints": {
    "rightShoulder": {"x":0,"y":0,"z":0},
    "rightElbow": null,
    "rightWrist": null
  },
  "visibility": {"rightShoulder": 0.9, "rightElbow": 0.0, "rightWrist": 0.0}
}
```

검증 규칙(위반 시 `MessageValidationError`):
- `TRACKING`은 세 관절이 모두 존재해야 한다
- `PARTIAL`은 누락 또는 저가시성 관절이 있어야 하고 pointer를 가질 수 없다
- `LOST`는 세 관절이 모두 null, visibility 전부 0, pointer 없음
- `pointing=true`면 pointer 필수, `false`면 pointer는 null이어야 한다

**연구적 가치:** 이 계약이 이미 엄격하므로, 실험 로그의 무결성을 논문에서 주장하기 쉽다.

**중요:** 영상 프레임은 Unity나 외부로 전송되지 않는다. 관절 좌표만 나간다. 이는 IRB·PIPA 대응에서 **설계상의 장점**으로 쓸 수 있다 → [`15-ethics-and-irb.md`](../10-research/15-ethics-and-irb.md)

## 4. Unity 프로젝트 (`MediapipeUnity/`)

### 🔴 환경 — 즉시 확인할 것

| 항목 | 값 | 비고 |
|---|---|---|
| **Unity 에디터** | **6000.3.10f1** | ⚠️ **HoloLens 2 지원 상한(6000.0.49f1)을 초과** |
| 어셈블리 | `TriageTrace.Runtime.asmdef` | EditMode / PlayMode 테스트 프로젝트 별도 존재 |
| XR 패키지 | **없음** | manifest.json에 OpenXR·XR Management·MRTK 전무 |
| 주요 패키지 | newtonsoft-json 3.2.2, test-framework 1.6.0, ugui 2.0.0, ai.navigation 2.0.10 | |

HL2 이식이 단순 패키지 추가가 아닌 이유가 여기 있다. → [`24-hololens2-port-plan.md`](../20-engineering/24-hololens2-port-plan.md)

### 레이어 (`TagManager.asset`)

| 인덱스 | 이름 |
|---|---|
| 6 | `Patient` |
| 8 | `FirstPersonHands` |

`Patient` 레이어 단일 raycast가 환경 오브젝트 간섭을 막는 핵심 설계다.

### 런타임 스크립트 (`Assets/Scripts/`)

| 파일 | 크기 | 역할 |
|---|---|---|
| `Models/PoseProtocolModels.cs` | 4.5 KB | v2 DTO |
| `Networking/PoseMessageParser.cs` | 15.5 KB | 수신 검증 |
| `Networking/PoseWebSocketClient.cs` | 9.2 KB | WebSocket 클라이언트 |
| `Networking/LatestPoseStateQueue.cs` | 1.1 KB | 스레드 안전 최신값 큐 |
| `Presentation/PoseReceiverBehaviour.cs` | 6.2 KB | 메인 스레드 전달 |
| `Presentation/PosePointerLineRenderer.cs` | 7.3 KB | 포인터 시각화 |
| `Presentation/PointerRaycaster.cs` | 3.7 KB | Patient raycast |
| `Presentation/PatientDwellSelector.cs` | 3.1 KB | dwell 선택 |
| `Presentation/PatientView.cs` | 10.3 KB | 환자 상태 + 색상 |
| `Presentation/PatientInteractionState.cs` | 164 B | enum 4상태 |
| `Presentation/PatientStatusCardUI.cs` | 6.8 KB | 스크린 카드 |
| `Presentation/WorldSpacePatientStatusCard.cs` | 6.0 KB | 월드 카드 |
| `Presentation/ARGuidanceHud.cs` | 8.9 KB | AR HUD |
| `Presentation/FirstPersonCameraController.cs` | 6.6 KB | 1인칭 이동 |
| `Presentation/FirstPersonPresentationController.cs` | 17.0 KB | 1인칭 팔/가방 |
| `Presentation/TriageTraceScenarioBootstrap.cs` | 12.1 KB | 시나리오 조립 |

### 확인된 핵심 동작

**`PatientDwellSelector.cs`**
- `dwellSeconds = 0.7f`, `[SerializeField] [Min(0.05f)]` → **이미 Inspector에서 조건화 가능**
- `ConfigureForTests(raycaster, seconds, card)` 존재 → 실험 조건 주입 훅이 이미 있다
- 같은 대상을 계속 가리켜도 1회만 선택 (`_selectedCurrentDwellPatient`)
- 이미 `Checked`인 환자는 재선택하지 않는다

**`PointerRaycaster.cs`**
- `maxDistance = 10.0f`, `QueryTriggerInteraction.Ignore`
- `hit.collider.GetComponentInParent<PatientView>()` — 자식 콜라이더 허용
- 포인터 라인이 보이지 않으면 즉시 클리어 (오선택 방지)

**`PatientView.cs`**
- 상태: `Unseen → Highlighted → InProgress → Checked`
- **`public event Action<PatientView> StateChanged`** → 로깅 훅이 이미 존재한다
- 머티리얼 색상 프로퍼티를 `_BaseColor` / `_Color` 폴백으로 처리
- 코드 주석에 **"상호작용 색상과 트리아지 심각도 색상을 분리하라"**는 설계 의도가 이미 명시되어 있음

**`ARGuidanceHud.cs`**
- 클래스 주석: *"read-only peripheral operations display … never participates in input, raycasting, dwell selection"* — **관찰 전용 계약이 코드에 선언되어 있다.** 확장 시 이 계약을 유지해야 한다
- `maximumNearbyPatients = 4`, `maximumSyncEvents = 3`
- 텍스트 슬롯 8개: zone, connection, pose, leftGuidance, rightGuidance, patientStatus, patientRows, teamSync

### Editor 도구 (`Assets/Editor/`)

| 파일 | 크기 | 메뉴 |
|---|---|---|
| `StationEnvironmentGenerator.cs` | 16.1 KB | 지하철/플랫폼 생성 |
| `TrainInteriorColliderGenerator.cs` | 15.2 KB | 열차 내부 콜라이더 |
| `PrototypePatientPlacementMenu.cs` | 18.1 KB | 환자 배치 |
| `PrototypeDisasterDressingGenerator.cs` | 19.1 KB | 재난 연출 |
| `PatientModelIntegrationMenu.cs` | 12.0 KB | FBX 환자 모델 설치 (EditorWindow) |
| `TenPatientIdentityAndPlacementMenu.cs` | 11.7 KB | 10명 ID 정규화 + 배치 검증 |
| `ARGuidanceHudInstaller.cs` | 9.8 KB | HUD 설치 |
| `FirstPersonPresentationInstaller.cs` | 15.3 KB | 1인칭 표현 설치 |

**이 Editor 도구 세트가 실험 준비의 가장 큰 자산이다.** 시나리오 배치를 재현 가능하게 다시 만들 수 있다는 뜻이고, 이는 논문의 재현성 절에 직접 쓰인다.

### 씬

| 파일 | 크기 |
|---|---|
| `TriageTraceEnvironmentPrototype.unity` | 6.79 MB (주 씬) |
| `TriageTraceDemoScene.unity` | 1.13 MB |
| `Subway_train.unity` | 1.03 MB |

## 5. 기능 완료 현황

- [x] MediaPipe Pose 오른팔 추적 + 품질 판정
- [x] pose_pointer v2 프로토콜 + 계약 테스트
- [x] 로컬 WebSocket 게시/수신 + 스레드 안전 전달
- [x] Patient 레이어 raycast + hover highlight
- [x] 0.7초 dwell 선택 (Inspector 조건화 가능)
- [x] 환자 상태 머신 4상태 + StateChanged 이벤트
- [x] World Space 상태 카드 + billboard
- [x] AR HUD: LINK/POSE, 방향, 거리, 확인 수, 가까운 4명, 최근 기록
- [x] 1인칭 이동·look·충돌·점프, 팔/구급상자 표현
- [x] 지하철 내부 + 플랫폼 외부 공간, 재난 연출
- [x] 환자 10명 구조, ID 정규화(TR-001~TR-010), FBX 설치 도구
- [x] Editor 설치/갱신 메뉴 + Undo 지원
- [x] pointer center/gain 런타임 보정
- [x] Python pytest / Unity EditMode·PlayMode 테스트 프로젝트

## 6. 미완료 / 미확인

- [ ] ⚠️ **10명 전원 dwell→카드→Checked 전환 Play Mode 실검증** (최종 카드 위치 보정 이후 재검증 필요)
- [ ] ⚠️ 서로 다른 pose(seated/kneeling/lying)별 카드 offset 점검
- [ ] ⚠️ 재난 연출 오브젝트가 환자 raycast를 가리지 않는지 전수 확인
- [ ] 선택 이벤트 로깅 (**연구에 필수 — 현재 전무**)
- [ ] 실험 조건 전환 모드 (**연구에 필수 — 현재 전무**)
- [ ] 응급도 단서 레이어 (**연구에 필수 — 현재 전무**)
- [ ] 시나리오 파일 기반 재현 가능한 배치
- [ ] 마우스 입력 베이스라인 조건
- [ ] XR 패키지 일체

## 7. 재현성 리스크

Git에서 의도적으로 제외된 대용량 외부 에셋:

`Subway Full Package`, `EmaceArt`, `TextMesh Pro` 원본, `MobileDependencyResolver`, `Characters`, `Fonts`, `FirstPerson`, `_Recovery`

**다른 PC에서 clone하면 씬이 깨진다.** 논문 재현성 절과 `ASSET_SETUP.md`에 반드시 문서화해야 한다. → [`43-submission-evidence.md`](../40-checklists/43-submission-evidence.md)

## 8. 알려진 기술 부채

| 항목 | 내용 | 영향 |
|---|---|---|
| 한국어 TMP 폰트 | 글리프 누락으로 □ 표시 → 영문 HUD로 복구 | 국내 학회 스크린샷 시 재발 가능 |
| 구급상자 손 겹침 | 일부 잔존, 데모 수준 수용 | 낮음 |
| 레거시 RPS 스텁 | `gesture_classifier.py` 등 빈 파일 | 낮음, 문서상 보존 결정됨 |
| `num_poses = 1` 강제 | 다인 추적 불가 | 현 연구 범위에서는 무관 |
