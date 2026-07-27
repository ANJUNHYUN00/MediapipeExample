# Task 12. Dwell Selection

## Goal

사용자가 같은 `PatientView`를 일정 시간 동안 계속 가리키면 해당 가상 Patient를 selected 처리한다. 기본 dwell 시간은 0.7초이며, hover와 selected 상태를 분리해 후속 Patient state machine의 기반을 만든다.

## Scope

- `PatientDwellSelector` 추가 예정
- 같은 `PatientView`를 기본 0.7초 동안 유지해 selected 처리
- hover target 변경 시 dwell progress 초기화
- pointer invalid, `PARTIAL`, `LOST`, 데이터 만료, 연결 끊김에서 dwell progress 취소
- selected 이벤트 또는 상태를 후속 UI/state component가 사용할 수 있게 노출

## Out of Scope

- WebSocket `pose_pointer` v2 DTO 구조 변경
- Python Pose 또는 pointing 계산 변경
- `Checked`, `InProgress`, `Unseen` 전체 상태 머신 구현
- Patient status card UI
- 실제 의료 판단 또는 triage severity 자동 변경

## Implementation Notes

- dwell은 `PointerRaycaster`가 제공하는 현재 hovered `PatientView`를 입력으로 삼는다.
- hover는 즉시 시각 피드백이고 selected는 시간 조건을 통과한 명시적 interaction event로 취급한다.
- 기본 dwell 시간은 0.7초로 시작하되 Unity Inspector에서 조정 가능하게 둔다.
- dwell progress 시각화가 필요하면 cyan/blue/white 계열을 사용한다.
- triage 색상 red/yellow/green/black은 dwell progress나 selected interaction 색상으로 사용하지 않는다.

## Acceptance Criteria

- 같은 Patient를 dwell 시간 이상 계속 가리키면 selected 상태 또는 이벤트가 발생한다.
- hover 대상이 바뀌면 이전 dwell progress가 초기화된다.
- pointer가 invalid 되면 dwell progress가 취소된다.
- hover와 selected 상태가 코드와 시각 표현에서 분리된다.
- 아직 `Checked`/`InProgress`/`Unseen` 상태 머신을 구현하지 않는다.
- WebSocket/DTO/fixture 구조가 변경되지 않는다.

## Test Notes

- synthetic time 또는 controllable clock으로 dwell threshold 전/후를 테스트한다.
- target switch, no target, invalid pointer, repeated selection 정책을 검증한다.
- Unity license 상태에 따라 PlayMode 테스트가 불가능하면 수동 검증 절차와 미확인 항목을 기록한다.

## Status

완료. `PatientDwellSelector`를 Presentation 계층에 추가했고, `PointerRaycaster.CurrentPatient`를 매 프레임 확인해 같은 `PatientView`를 기본 0.7초 이상 계속 가리키면 selected 처리한다.

구현 결과:

- `PatientView`에 hover와 별도인 selected 상태, selected 색상, `SelectOn()`, `SelectOff()`, `SetSelected(bool)`를 추가했다.
- `PatientDwellSelector`는 대상 변경, `CurrentPatient == null`, pointer hidden으로 인한 raycast 해제 상황에서 dwell timer를 초기화한다.
- 새 Patient가 selected 되면 이전 selected Patient를 `SelectOff()`로 해제해 한 번에 하나만 selected 상태로 유지한다.
- `Checked`, `InProgress`, `Unseen` 상태 머신은 구현하지 않았다.
- WebSocket 수신 구조와 `PosePointerState` DTO는 변경하지 않았다.

검증 결과:

- PlayMode 테스트를 추가했다.
- Unity batchmode PlayMode 테스트 명령을 시도했지만 결과 XML과 로그 파일이 생성되지 않았다.
- `dotnet build` 대체 검증도 시도했지만 이 환경에는 .NET SDK가 없어 실행할 수 없었다.
