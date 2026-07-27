# Task 10. Unity AR Pointer Visualization

## Goal

Unity가 Task 09의 검증된 pose v2 수신 상태를 사용해 모의 AR 포인터 레이를 화면에 표시한다. 이 Task는 실제 의료 판단 없이 사용자의 오른팔 방향을 Unity 시뮬레이션의 시각적 pointer로 보여 주는 단계다.

## Scope

- `PosePointerLineRenderer` 구현
- `PoseReceiverBehaviour`와 연결
- `CurrentDirection` 노출
- pointer visible/hidden 처리
- `LineRenderer` 기반 포인터 시각화
- `PARTIAL`, `LOST`, 데이터 만료, 연결 끊김, `pointing=false`에서 pointer 숨김

## Out of Scope

- WebSocket `pose_pointer` v2 DTO 구조 변경
- Python Pose 계산 변경
- Patient raycast, hover, dwell selection
- 실제 의료 분류, 진단, 중증도 판단

## Implementation Notes

- `PoseReceiverBehaviour`가 제공하는 최신 v2 상태를 읽어 pointer가 유효할 때만 `LineRenderer`를 활성화한다.
- `CurrentDirection`은 후속 `PointerRaycaster`가 사용할 Unity world-space ray 방향으로 노출한다.
- pointer가 숨겨질 때는 `CurrentDirection`을 후속 상호작용에 사용하지 않도록 invalid 상태를 함께 제공한다.
- 레이 색상은 interaction 입력 표현이므로 cyan/blue/white 계열을 사용한다.
- triage 색상 red/yellow/green/black은 pointer 또는 interaction state 표현에 사용하지 않는다.

## Acceptance Criteria

- 유효한 `pointing=true` 상태에서 `LineRenderer` 기반 pointer가 표시된다.
- 유효하지 않은 pose 상태에서는 pointer가 즉시 숨겨진다.
- `PosePointerLineRenderer.CurrentDirection`을 다른 Unity component가 읽을 수 있다.
- WebSocket/DTO/fixture 구조가 변경되지 않는다.
- 화면 표현이 의료 판단이나 자동 분류처럼 보이지 않는다.

## Test Notes

- Unity EditMode/PlayMode에서 synthetic pose state로 visible/hidden 전환을 검증한다.
- `PARTIAL`, `LOST`, 데이터 만료, 연결 끊김에서 line이 비활성화되는지 확인한다.
- 실제 Python WebSocket 입력과 함께 수동으로 pointer 방향과 숨김 동작을 확인한다.

## Status

완료. `PosePointerLineRenderer`가 구현되어 `PoseReceiverBehaviour`와 연결되었고, `CurrentDirection`을 후속 raycast 입력으로 사용할 수 있다. WebSocket `pose_pointer` v2 DTO 구조는 변경하지 않았다.
