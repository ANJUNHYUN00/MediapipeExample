# CLAUDE.md

> 이 저장소에서 작업을 시작하는 Claude가 가장 먼저 읽는 문서.
> 사람·AI 공통 규약은 [`AGENTS.md`](./AGENTS.md)에, 연구 계획은 [`TriageTrace-Research/`](./TriageTrace-Research/)에 있다.

## 1. 이 저장소는 무엇인가

**Triage Trace** — 웹캠 MediaPipe Pose로 사용자의 오른팔을 추적해, Unity 재난 시뮬레이션 안의 대상을 가리키고 0.7초 dwell로 확인하는 **비의료 인터랙션 프로토타입**.

현재 단계: 작동하는 프로토타입 → **연구용으로 계측하는 중**.

### 🔴 비의료 경계 — 절대 완화하지 않는다

이 프로젝트는 실제 환자를 평가하거나 분류하지 않는다. 진단·치료·응급도 판단·의료 자문·임상 의사결정을 제공하지 않는다. Unity 화면의 모든 분류·선택·상태는 **가상 시나리오를 위한 모의 인터페이스**다.

응급도 단서(urgency cue)를 추가하더라도 이 선언은 수정하지 않는다. 추가되는 것은 가상 시나리오의 속성이지 실제 판단 능력이 아니다.

코드·주석·커밋 메시지에서 쓸 표현은 `TriageTrace-Research/00-overview/01-project-definition.md` §5 용어 통제표를 따른다.

## 2. 먼저 읽을 것

| 상황 | 문서 |
|---|---|
| **지금 뭘 해야 하나** | [`TriageTrace-Research/START-HERE.md`](./TriageTrace-Research/START-HERE.md) |
| 코드에 뭐가 있나 | [`TriageTrace-Research/20-engineering/21-current-architecture.md`](./TriageTrace-Research/20-engineering/21-current-architecture.md) |
| 구현 사양 | [`TriageTrace-Research/20-engineering/`](./TriageTrace-Research/20-engineering/) |
| 작업 단위·수용 기준 | [`TriageTrace-Research/30-planning/32-task-backlog.md`](./TriageTrace-Research/30-planning/32-task-backlog.md) |
| 왜 이 방향인가 | [`TriageTrace-Research/00-overview/03-strategic-judgment.md`](./TriageTrace-Research/00-overview/03-strategic-judgment.md) |
| 기존 저장소 규약 | [`AGENTS.md`](./AGENTS.md) |

## 3. 아키텍처 한눈에

```
웹캠 → MediaPipe Pose (오른쪽 어깨12/팔꿈치14/손목16)
     → pointing.py (각도·품질·평활)
     → pose_pointer v2 JSON → ws://127.0.0.1:8765 @15Hz
     → Unity PoseWebSocketClient → PoseMessageParser → LatestPoseStateQueue
     → PoseReceiverBehaviour → PosePointerLineRenderer
     → PointerRaycaster (Patient 레이어, 10m)
     → PatientDwellSelector (0.7초)
     → PatientView (Unseen→Highlighted→InProgress→Checked)
     → WorldSpacePatientStatusCard / ARGuidanceHud(관찰 전용)
```

- Python = 데이터 생산자 + WebSocket **서버**
- Unity = 소비자 + WebSocket **클라이언트**
- **영상 프레임은 Unity나 외부로 전송하지 않는다.** 관절 좌표만 나간다

## 4. 환경

| 항목 | 값 |
|---|---|
| Unity | **6000.3.10f1** |
| Python | 3.11 (`Mediapipe/.venv`) |
| 어셈블리 | `TriageTrace.Runtime.asmdef` |
| 레이어 6 | `Patient` |
| 레이어 8 | `FirstPersonHands` |
| XR 패키지 | 없음 |

⚠️ **HoloLens 2 상한은 Unity 6000.0.49f1이다.** 현재 버전이 이를 초과하므로 HL2 이식은 업그레이드가 아니라 **별도 프로젝트 분기 + 2022.3.62f1 다운그레이드**다. 이 프로젝트를 다운그레이드하지 말 것. → [`24-hololens2-port-plan.md`](./TriageTrace-Research/20-engineering/24-hololens2-port-plan.md)

## 5. 🔴 깨지 말아야 할 계약

| 계약 | 근거 |
|---|---|
| 영상 프레임 외부 전송 금지 | `AGENTS.md` §3, `config.py`가 localhost 외 호스트를 ValueError로 거부 |
| `ARGuidanceHud`는 읽기 전용 — 입력·raycast·dwell에 관여하지 않음 | 클래스 주석에 계약으로 선언됨 |
| 상호작용 상태(`PatientInteractionState`)와 트리아지 심각도를 **합치지 않는다** | `PatientView.cs` 주석. 응급도는 **별도 컴포넌트**로 |
| `pose_pointer` v2 필드 의미 고정 | `message_builder.py` 검증. 새 필드가 필요하면 v3으로 분기 |
| `Patient` 레이어 단일 raycast | 단서 애니메이션이 collider를 바꾸지 않게 |
| Editor 도구는 Undo 지원 | 기존 Editor 파일 전부 |

## 6. 작업 규칙

- **씬을 임의로 저장하지 않는다. Play Mode를 임의로 실행하지 않는다.** 사람이 통제한다
- 기존 코드를 먼저 읽고 변경 범위를 파악한다
- 계측 작업(`ExperimentLogger` 등)은 기존 스크립트를 **구독만** 하고 수정하지 않는다. `PatientView.StateChanged` 이벤트가 이미 있다
- Editor 기능은 Undo 가능한 도구로 만든다
- 변경 후 변경 파일·실행할 메뉴·Inspector 확인 항목·검증 범위를 보고한다
- 정적 점검은 Unity Editor의 실제 컴파일/Play Mode 검증을 대체하지 못한다. 완료로 표현하지 않는다
- 새 Task 문서는 `mds/`에 35번부터 이어서 만든다 (기존 19~34 형식: 목표·범위·제외·구현 제약·메뉴 경로·수용 기준)

## 7. 지금의 최우선 작업

순서대로. 상세는 [`START-HERE.md`](./TriageTrace-Research/START-HERE.md).

1. **HoloLens 2 툴체인 아카이브** — [`41-toolchain-archive.md`](./TriageTrace-Research/40-checklists/41-toolchain-archive.md) + [`tools/archive-toolchain.ps1`](./TriageTrace-Research/tools/archive-toolchain.ps1)
   - 🔴 STEP 3의 `Unity_lic.ulf` 백업을 빠뜨리지 말 것 (Personal은 오프라인 활성화 불가)
   - 🔴 STEP 4의 Feature Tool 실행이 가장 시급 (OpenXR Plugin 1.11.2의 유일한 입수 경로)
2. **`ExperimentLogger`** — 현재 로그가 전무하다. 크리티컬 패스. [`22-instrumentation-spec.md`](./TriageTrace-Research/20-engineering/22-instrumentation-spec.md)
3. **`IPointerSource` 추상화 + 마우스 베이스라인** — HL2 이식을 며칠 단축한다
4. **`PatientUrgencyProfile`** — [`14-severity-cue-spec.md`](./TriageTrace-Research/10-research/14-severity-cue-spec.md)
5. **불확실성 표현 3조건** — 논문의 핵심 기여. [`23-uncertainty-hud-spec.md`](./TriageTrace-Research/20-engineering/23-uncertainty-hud-spec.md)

## 8. 하지 말 것

- ❌ rPPG·호흡수·SpO₂ 등 **실제 생체신호 추정 구현** — 문제 자체가 미해결. 근거는 [`12-literature-landscape.md`](./TriageTrace-Research/10-research/12-literature-landscape.md) §1b
- ❌ 응급도 **추정기** 만들기 — 가상 환자에서는 순환 논증. 기여는 *표현*에 있다
- ❌ 호흡수 같은 **수치** 표시 — "보이는가/빠른가"까지만
- ❌ Ray-Ban Meta를 HUD 장치로 — 디스플레이가 물리적으로 없다
- ❌ dwell 시간 비교를 주 기여로 — 문헌상 포화
- ❌ "진단·평가·판정·개선했다·입증했다" 같은 의료 주장 표현
- ❌ 기능 추가 — 지금 부족한 것은 기능이 아니라 **측정**이다
- ❌ MRTK v4.0.0-pre.*, Unity 2022.3.63f1+ (HL2 분기에서)

## 9. 외부 에셋 주의

아래는 Git에서 제외되어 있다. clone만으로는 씬이 복원되지 않는다.

`Subway Full Package`, `EmaceArt`, `TextMesh Pro` 원본, `MobileDependencyResolver`, `Characters`, `Fonts`, `FirstPerson`, `_Recovery`

→ `ASSET_SETUP.md`(미작성)에 출처·라이선스·버전·설치 경로를 기록할 것. 논문 재현성 절이 여기에 의존한다.
