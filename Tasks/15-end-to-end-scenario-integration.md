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

계획.
