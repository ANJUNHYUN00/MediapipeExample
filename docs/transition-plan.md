# Triage Trace 전환 계획

## 목적

기존 MediaPipe–Unity 가위바위보 실습의 완료 자산을 삭제하지 않고 Triage Trace Pose 기반 Unity AR 시뮬레이션으로 전환한다. 이 문서는 무엇을 보존하고, 무엇을 대체하며, 어떤 순서로 구현할지 기록한다.

## 전환 전 상태

- Task 01: Python·Unity 구조와 책임 분리 완료
- Task 02: `hand_gesture` version 1 계약과 fixture 완료
- Task 03: Python 3.11.9 환경, 의존성, Hand Landmarker 모델, Unity 프로젝트 완료
- Task 04~06: 문서만 존재하고 기능 코드는 미구현
- WebSocket 서버, Unity 수신기, 손 제스처 판정 기능은 미구현

## 재사용 결정

### 그대로 재사용

- `Mediapipe/`와 `MediapipeUnity/` 루트
- Python 서버 / Unity 클라이언트 방향
- `ws://127.0.0.1:8765`
- OpenCV, MediaPipe, websockets, pytest
- Unity Editor와 `Assets/Scripts/{Configuration,Models,Networking,Presentation}`
- 최신 상태 우선 큐, 재연결, 메인 스레드 UI 갱신 원칙
- 영상 로컬 처리와 비저장 원칙

### 호환성 자산으로 보존

- `hand_gesture` version 1 계약
- `Mediapipe/tests/fixtures/messages/hand_gesture_*.json`
- Hand Landmarker 모델과 체크섬 문서
- 기존 Plan 01~06과 Tasks 01~06

이 자산은 삭제하지 않지만 Triage Trace의 활성 구현 입력으로 사용하지 않는다.

### Pose 기준으로 대체

- Hand Landmarker → Pose Landmarker
- 21개 손 랜드마크 → 오른쪽 어깨·팔꿈치·손목
- 손가락 상태·RPS 분류 → tracking 품질·pointing·pointer
- `hand_gesture` v1 활성 송신 → `pose_pointer` v2 활성 송신
- RPS 결과 UI → AR 모의 포인터와 가상 시나리오 UI

## 안전 결정

- Triage Trace는 실제 의료 판단을 하지 않는다.
- 메시지에는 환자 등급, 진단, 치료, 위험도나 생체 정보 필드를 추가하지 않는다.
- 포인터는 UI 입력일 뿐이며 의미 있는 의료 결론을 생성하지 않는다.
- Unity 시작 화면과 지속 UI에 `Simulation Only` 고지를 둔다.
- 카메라 영상과 개인 식별 데이터는 저장하거나 전송하지 않는다.

## 문서와 Task 전환

1. `AGENTS.md`와 `docs/project-plan.md`를 Triage Trace 활성 기준으로 변경한다.
2. `docs/websocket-protocols.md`에 v1 보존과 v2 계약을 함께 기록한다.
3. Plan 07~08을 활성 Pose 설계로 추가한다.
4. 기존 미완료 Task 04~06은 대체 상태로 표시한다.
5. Task 07~09를 새 활성 구현 순서로 추가한다.
6. Python·Unity README를 Pose 중심 책임과 dual-protocol 기준으로 갱신한다.
7. 정상·실패·불완전 Pose v2 fixture를 추가한다.

## 전환 완료 조건

- 활성 문서가 Triage Trace 목적과 비의료 안전 경계를 일관되게 설명한다.
- MVP 관절과 v2 필드가 모든 활성 문서에서 같은 이름을 사용한다.
- 기존 v1 fixture가 수정·삭제되지 않는다.
- 다음 활성 Task가 Pose Landmarker 실행으로 시작한다.
- 이번 전환에서는 실제 추적 코드가 추가되지 않는다.
