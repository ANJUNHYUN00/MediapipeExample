# 09. Unity 시나리오 상호작용 및 완료 계획

## 목적

Task 09에서 연결된 `pose_pointer` version 2 수신 상태를 Unity AR 시뮬레이션의 실제 상호작용 흐름으로 확장한다. 이 계획은 포인터 시각화, 가상 Patient hover, dwell 선택, interaction state, HUD 카드, 통합 시나리오와 포트폴리오 정리까지 Task 10~16을 프로젝트 완료 흐름으로 연결한다.

Triage Trace는 자동 의료 진단 시스템이나 중증도 판단 AI가 아니다. Unity 화면의 Patient는 실제 환자가 아니라 가상 훈련 대상이며, 이 단계의 목표는 AR 기반으로 누가 확인되었고 누가 아직 확인되지 않았는지 추적하는 시뮬레이션 MVP를 완성하는 것이다.

## 구현 원칙

- Task 10~16은 WebSocket `pose_pointer` v2 DTO 구조를 불필요하게 변경하지 않는다.
- Python은 계속 Pose 입력과 pointer 상태만 생산한다.
- Unity는 v2를 소비해 가상 시나리오 UI와 interaction state를 관리한다.
- 실제 의료 판단, 자동 진단, 환자 우선순위 산출, 치료 추천 로직을 추가하지 않는다.
- triage 색상 `red`, `yellow`, `green`, `black`은 가상 시나리오의 의료 중증도 라벨을 표시해야 할 때만 조심스럽게 사용한다.
- hover, selected, checked 같은 interaction state에는 의료 중증도 색상과 혼동되지 않도록 `cyan`, `blue`, `white` 계열을 사용한다.
- 한 번에 하나의 PatientView만 hover highlight 된다.
- 연결 끊김, 데이터 만료, `PARTIAL`, `LOST`, `pointing=false`에서는 pointer와 hover/selection 진행을 안전하게 비활성화한다.

## Task 흐름

| Task | 상태 | 역할 |
|---|---|---|
| Task 10 | 완료 | Unity `PosePointerLineRenderer`로 pose pointer 방향을 LineRenderer 레이로 시각화 |
| Task 11 | 완료 | `PointerRaycaster`와 `PatientView`로 가상 Patient hover highlight 구현 |
| Task 12 | 완료 | 같은 Patient를 기본 0.7초 동안 가리키면 selected 처리하는 dwell 선택 |
| Task 13 | 완료 | Patient interaction state를 `Unseen`, `Highlighted`, `InProgress`, `Checked`로 분리 |
| Task 14 | 완료 | 선택된 Patient 상태를 AR HUD 카드로 표시 |
| Task 15 | 계획 | Python Pose부터 Unity UI 카드까지 end-to-end 시나리오 통합 |
| Task 16 | 계획 | README, 실행 순서, 데모 시나리오, 발표·포트폴리오 설명과 known limitations 정리 |

## 설계 개요

```text
PoseReceiverBehaviour
  -> validated latest pose v2 state
  -> PosePointerLineRenderer
     -> visible/hidden LineRenderer
     -> CurrentDirection
  -> PointerRaycaster
     -> raycast by CurrentDirection + LayerMask
     -> one hovered PatientView
  -> PatientDwellSelector
     -> hover duration
     -> selected PatientView
  -> Patient state machine
     -> Unseen / Highlighted / InProgress / Checked
  -> Patient Status Card HUD
```

`PosePointerLineRenderer.CurrentDirection`은 Unity 시나리오 상호작용의 단일 ray 방향 입력으로 사용한다. Task 10~16은 pointer 방향을 Unity에서 다시 추론하지 않고, Task 09까지 검증된 v2 상태와 presenter 결과를 기반으로 동작한다.

## 완료 기준

- Unity에서 pose pointer가 레이로 보이고 유효하지 않은 입력에서는 숨겨진다.
- 레이가 가상 Patient collider를 가리킬 때 하나의 Patient만 hover highlight 된다.
- dwell 시간이 충족되면 hover와 분리된 selected 상태가 발생한다.
- Patient state가 interaction state로 명확히 관리되며 triage severity 색상과 섞이지 않는다.
- 선택된 Patient의 상태 카드가 비의료 HUD로 표시된다.
- Python MediaPipe Pose, WebSocket 수신, pointer visualization, hover, dwell selection, state update, UI card가 하나의 데모 시나리오로 연결된다.
- 문서와 README가 AR hardware 없이 Unity simulation MVP임을 명확히 설명한다.

## 다음 Task

다음 실제 구현 Task는 [`Tasks/15-end-to-end-scenario-integration.md`](../Tasks/15-end-to-end-scenario-integration.md)이다.
