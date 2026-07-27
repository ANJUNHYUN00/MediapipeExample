# Task 15. End-to-End Scenario Integration

## Goal

Python MediaPipe Pose 실행부터 Unity WebSocket 수신, pointer visualization, raycast hover, dwell selection, patient state update, UI card 표시까지 하나의 데모 시나리오로 연결한다.

## Scope

- Python MediaPipe Pose 실행 절차 확인
- Unity WebSocket 수신과 최신 pose v2 상태 확인
- pointer visualization 연결 확인
- raycast hover 연결 확인
- dwell selection 연결 확인
- Patient state update 연결 확인
- Patient status card 표시 연결 확인
- 실패 상태에서 안전한 비활성화 확인

## Out of Scope

- WebSocket `pose_pointer` v2 DTO 구조 변경
- 새로운 Pose 입력 방식 추가
- AR hardware 최적화
- 실제 의료 판단, 자동 triage, 환자 데이터 연동
- 포트폴리오 문서 최종 정리

## Implementation Notes

- end-to-end 데모는 로컬 `ws://127.0.0.1:8765` 기준으로 시작한다.
- Python 앱과 Unity scene의 실행 순서를 문서화하면서 검증한다.
- 연결 끊김, 데이터 만료, `PARTIAL`, `LOST`, `pointing=false`에서 pointer, hover, dwell, selected progress가 안전하게 정지하는지 확인한다.
- Unity scene에는 가상 Patient만 사용하고 실제 환자 데이터나 의료 결론을 넣지 않는다.
- triage severity 표시가 포함될 경우 사전 정의된 가상 scenario label로 제한한다.

## Acceptance Criteria

- Python Pose publisher와 Unity receiver가 로컬 WebSocket으로 연결된다.
- 유효한 오른팔 pointing 입력이 Unity pointer line을 움직인다.
- pointer ray가 Patient를 hover highlight 한다.
- dwell 시간이 충족되면 selected 처리된다.
- selected Patient의 interaction state와 status card가 갱신된다.
- 실패 상태와 연결 문제에서 stale pointer 또는 stale hover가 남지 않는다.
- 데모가 자동 의료 판단 시스템으로 보이지 않는다.

## Test Notes

- 가능한 경우 Python publisher와 Unity PlayMode를 함께 실행해 end-to-end smoke test를 수행한다.
- Unity license 또는 환경 문제로 자동 테스트가 불가능하면 수동 검증 체크리스트와 미확인 항목을 기록한다.
- WebSocket/DTO 변경이 없음을 fixture와 기존 parser 테스트로 확인한다.

## Status

완료. Unity Presentation 계층에 `TriageTraceScenarioBootstrap` helper를 추가하고, 기존 receiver bootstrap을 Task 15 흐름에 맞게 확장했다.

구현 결과:

- `PoseReceiverBehaviour`, `PosePointerLineRenderer`, `PointerRaycaster`, `PatientDwellSelector`, `PatientStatusCardUI`를 한 scenario 흐름으로 연결할 수 있게 했다.
- `TriageTraceScenarioBootstrap`은 명시적 Inspector 연결을 우선하고, 비어 있는 참조만 같은 GameObject 또는 현재 Scene에서 찾아 연결한다.
- `TriageTraceScenarioBootstrap`은 status card가 없고 `Create Status Card If Missing`이 켜져 있으면 기본 Canvas 기반 card를 생성한다.
- 기존 자동 `PoseReceiverBootstrap`은 receiver object에 `PointerRaycaster`, `PatientDwellSelector`, `TriageTraceScenarioBootstrap`도 붙여 편의 연결을 제공한다.
- `PoseReceiverBehaviour.SetPointerLine()`을 추가해 scenario helper가 pointer line을 명시적으로 연결할 수 있게 했다.
- README에 Unity Scene 설정 체크리스트, Patient Collider/Renderer/Layer 요구사항, Patient Layer Mask 설정, Patient Status Card Canvas 설정, End-to-End 실행 순서와 known limitations를 추가했다.
- WebSocket pose v2 DTO 구조는 변경하지 않았다.
- 의료 중증도 판단, 자동 진단, triage severity 판정 로직은 추가하지 않았다.

검증 결과:

- PlayMode 테스트를 추가해 `TriageTraceScenarioBootstrap`이 pointer line, raycast, dwell selection, status card, Mark Checked, checked 보호 흐름을 연결하도록 검증한다.
- Unity batchmode PlayMode 테스트는 현재 환경의 Unity licensing 초기화 실패로 완료하지 못했다. 상세 로그는 `MediapipeUnity/Logs/task15-playmode.log`를 확인한다.
