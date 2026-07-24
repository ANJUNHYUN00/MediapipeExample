# Task 09. Pose v2 게시자 및 Unity 수신 기반

> 상태: 대기

## 작업 목적

Python `PosePointerState`를 `pose_pointer` version 2 JSON으로 게시하고 Unity가 v1과 v2를 분리해 파싱·검증한 뒤 최신 v2 상태를 메인 스레드로 전달하도록 한다. AR 시나리오의 세부 화면은 후속 Task로 남긴다.

## 선행 조건

- Task 08이 완료되어 상태별 불변 조건을 만족하는 `PosePointerState`가 존재할 것
- [`Plan/08-pose-v2-protocol-and-unity-ar.md`](../Plan/08-pose-v2-protocol-and-unity-ar.md)를 읽을 것
- v1 fixture와 v2 fixture가 모두 JSON 검증을 통과할 것
- Unity용 WebSocket/JSON 라이브러리의 Editor·데스크톱 호환성을 작은 테스트로 확인할 것

## 작업 단계

1. Python `PosePointerMessageV2`와 명시적 camelCase 직렬화를 구현한다.
2. 모든 필드, enum, 유한 좌표, visibility와 상태 불변 조건을 검증한다.
3. 기존 v1 메시지 빌더와 fixture를 변경하지 않는다.
4. 최신 상태 우선 큐와 10~20Hz rate limit를 구현한다.
5. 로컬 WebSocket 연결·종료·재연결 시나리오를 테스트한다.
6. Unity에 v1/v2 라우터와 분리된 `PosePointerMessageV2` DTO를 구현한다.
7. Unity Parser에서 sequence, tracking, pointer, joints, visibility를 검증한다.
8. 수신 상태를 스레드 안전한 최신 메시지 큐로 전달한다.
9. 메인 스레드에서 임시 디버그 포인터와 상태 텍스트만 표시한다.
10. `PARTIAL`, `LOST`, 데이터 만료와 연결 끊김에서 포인터를 숨긴다.
11. 세 v2 fixture와 기존 v1 fixture로 Python·Unity 계약 테스트를 작성한다.
12. 화면에 `Simulation Only / 실제 의료 판단용이 아님` 고지를 추가한다.
13. 실행·종료·재연결과 알려진 제약을 양쪽 README에 기록한다.

## 완료 기준

- Python의 v2 JSON이 문서 및 fixture와 일치한다.
- Unity가 v1과 v2를 type/version으로 올바르게 분기한다.
- 잘못된 메시지가 UI나 연결 전체를 종료시키지 않는다.
- 네트워크 스레드에서 Unity API를 직접 호출하지 않는다.
- 포인터는 유효하고 최신인 v2 상태에서만 활성화된다.
- v1 계약과 fixture가 변경되지 않는다.
- 실제 의료 판단이나 환자 분류 로직이 없다.

## 예상 산출물

- Python v2 message builder와 publisher 테스트
- Unity v2 DTO, Parser, 라우터와 최신 상태 큐
- Unity EditMode·PlayMode 계약 테스트
- 임시 포인터·연결·추적 상태 UI
- 갱신된 실행 문서

## 다음 Task와의 연결

후속 Unity AR UI Task에서 정규화 pointer를 Canvas 또는 AR 상호작용 평면으로 변환하고, 비임상 가상 시나리오 hover와 시각 피드백을 구현한다.
