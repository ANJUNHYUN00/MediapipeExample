# Task 13. Patient State Machine

## Goal

가상 Patient의 interaction state를 `Unseen`, `Highlighted`, `InProgress`, `Checked`로 분리해 hover, dwell selected, 확인 완료 상태를 명확히 관리한다. 이 상태는 실제 의료 중증도나 진단 결과가 아니라 Unity 시뮬레이션의 진행 상태다.

## Scope

- Patient interaction state 모델 정의
- `Unseen`, `Highlighted`, `InProgress`, `Checked` 상태 전이 구현
- hover와 dwell selected를 상태 전이에 연결
- checked 완료 상태 관리
- interaction state 색상과 triage severity 색상 분리 원칙 적용

## Out of Scope

- WebSocket `pose_pointer` v2 DTO 구조 변경
- Python Pose 또는 pointing 계산 변경
- 실제 중증도 판단 AI, 자동 triage 결과 산출
- Patient status card의 최종 HUD 디자인
- 실제 환자 데이터 저장 또는 병원 시스템 연동

## Implementation Notes

- `Highlighted`는 현재 hover 중인 상태, `InProgress`는 dwell selected 이후 확인 중인 상태, `Checked`는 사용자가 확인 완료로 표시한 상태로 둔다.
- `Unseen`은 아직 pointer interaction을 받지 않은 기본 상태다.
- interaction state 색상은 cyan/blue/white 계열을 사용한다.
- triage severity 색상 red/yellow/green/black은 가상 시나리오 라벨의 의료 중증도 의미로만 조심스럽게 다루며, interaction state와 같은 material slot에 섞지 않는다.
- severity 값이 필요하면 사전 정의된 가상 scenario data로만 제공하고 자동 추론하지 않는다.

## Acceptance Criteria

- Patient state가 `Unseen`, `Highlighted`, `InProgress`, `Checked` 중 하나로 명확히 표현된다.
- hover, dwell selected, checked 완료가 예측 가능한 전이를 만든다.
- 한 번에 하나의 Patient만 `Highlighted` 된다.
- `Checked` 상태가 hover highlight 때문에 사라지거나 severity 색상과 혼동되지 않는다.
- 실제 의료 판단이나 자동 분류 로직이 없다.
- WebSocket/DTO/fixture 구조가 변경되지 않는다.

## Test Notes

- 상태 전이 단위 테스트 또는 PlayMode 테스트를 작성한다.
- hover enter/exit, dwell selected, checked 완료, checked 대상 재hover 정책을 검증한다.
- severity 색상과 interaction 색상이 별도 경로로 적용되는지 수동 확인한다.

## Status

완료. `PatientInteractionState` enum과 `PatientView` 상태 전이를 추가했고, hover와 dwell selection을 `Highlighted`와 `InProgress` 상태로 연결했다.

구현 결과:

- `PatientInteractionState` enum을 `Presentation` 계층에 추가했다.
- `PatientView`가 `Unseen`, `Highlighted`, `InProgress`, `Checked` 중 하나의 `InteractionState`를 가진다.
- `PatientView.SetState(PatientInteractionState state)`와 `MarkChecked()`를 추가했다.
- 기존 `HighlightOn()`, `HighlightOff()`, `SelectOn()`, `SelectOff()`, `SetSelected(bool)`는 유지하되 내부적으로 상태 전이에 연결했다.
- `PointerRaycaster`의 기존 hover 호출은 `Unseen -> Highlighted`, `Highlighted -> Unseen` 전이로 동작한다.
- `PatientDwellSelector`의 dwell 완료는 selected 표시 대신 `InProgress` 상태로 연결된다.
- 새 Patient가 `InProgress`가 되면 이전 `InProgress` Patient는 `Checked`가 아닌 경우 `Unseen`으로 돌아간다.
- `Checked` Patient는 hover와 dwell selection으로 `Unseen` 또는 `InProgress`로 되돌아가지 않는다.
- 상태별 Inspector 색상 필드를 `Unseen Color`, `Highlighted Color`, `In Progress Color`, `Checked Color`로 분리했다.
- red/yellow/green/black triage severity 색상과 interaction state 색상을 섞지 않는 원칙을 코드 주석과 Unity README에 기록했다.
- WebSocket 수신 구조와 `PosePointerState` DTO는 변경하지 않았다.

검증 결과:

- PlayMode 테스트를 추가·갱신해 기본 `Unseen`, hover `Highlighted`, hover 해제 `Unseen`, dwell `InProgress`, 단일 `InProgress`, `MarkChecked()`, checked 보호 규칙을 검증하도록 했다.
- Unity batchmode PlayMode 테스트는 라이선스 초기화 실패로 완료하지 못했다. 상세 로그는 `MediapipeUnity/Logs/task13-playmode.log` 또는 최신 Unity test log를 확인한다.
