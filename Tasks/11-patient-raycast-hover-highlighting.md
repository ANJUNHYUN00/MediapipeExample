# Task 11. Patient Raycast Hover Highlighting

## Goal

Task 10의 Unity pointer 방향을 사용해 가상 Patient 대상을 raycast로 감지하고, 한 번에 하나의 Patient만 hover highlight 한다. 이 기능은 실제 환자 판정이 아니라 Unity 시뮬레이션에서 "현재 사용자가 어느 가상 대상을 가리키는지" 보여 주기 위한 상호작용이다.

## Scope

- `PointerRaycaster` 구현
- `PatientView` 구현
- `PosePointerLineRenderer.CurrentDirection`을 ray 방향으로 사용
- Collider와 `LayerMask` 기반 Patient 감지
- 한 번에 하나의 `PatientView`만 hover highlight
- pointer invalid 상태에서 hover 해제

## Out of Scope

- WebSocket `pose_pointer` v2 DTO 구조 변경
- Python Pose 또는 pointing 계산 변경
- dwell selection과 selected 상태
- `Unseen`, `InProgress`, `Checked` 상태 머신
- triage severity 자동 산출

## Implementation Notes

- `PointerRaycaster`는 `CurrentDirection`이 유효할 때만 raycast를 수행한다.
- raycast 대상은 명시적 collider와 Patient 전용 `LayerMask`로 제한한다.
- hover highlight는 interaction state 표현이므로 cyan/blue/white 계열을 사용한다.
- triage severity 표시가 필요하더라도 red/yellow/green/black과 hover highlight 색상을 섞지 않는다.
- 새 Patient가 hover되면 이전 Patient의 hover highlight를 해제한다.
- pointer가 숨겨지거나 데이터가 만료되면 현재 hover도 해제한다.

## Acceptance Criteria

- pointer ray가 Patient collider를 향하면 해당 `PatientView`만 hover highlight 된다.
- ray가 아무 Patient도 맞히지 않으면 모든 hover highlight가 해제된다.
- 동시에 둘 이상의 Patient가 hover 상태로 남지 않는다.
- `LayerMask` 밖 collider는 hover 대상이 되지 않는다.
- WebSocket/DTO/fixture 구조가 변경되지 않는다.
- UI와 코드가 실제 의료 판단을 수행하지 않는다.

## Test Notes

- Unity license 문제로 PlayMode 테스트 실행은 미확인 상태다.
- 가능해지면 synthetic scene에서 단일 hit, no hit, target switch, invalid pointer, layer mask 제외를 PlayMode로 검증한다.
- 현재는 코드 구조 검토와 수동 Unity scene 검증을 우선 기록한다.

## Status

완료. `PointerRaycaster`와 `PatientView`가 구현되었고, `PosePointerLineRenderer.CurrentDirection`을 ray 방향으로 사용한다. Unity license 문제로 PlayMode 테스트 실행은 미확인 상태로 남아 있다.
