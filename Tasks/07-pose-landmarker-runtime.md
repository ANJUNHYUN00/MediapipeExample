# Task 07. Pose Landmarker 실행 및 프레임 처리

> 상태: 완료 — 2026-07-24

## 작업 목적

공식 MediaPipe Pose Landmarker 모델을 검증하고, 웹캠의 연속 프레임에서 VIDEO 모드 Pose 추론을 실행해 MediaPipe 객체와 독립적인 내부 결과를 생성한다. 실제 포인터 계산과 WebSocket 송신은 아직 구현하지 않는다.

## 선행 조건

- `AGENTS.md`, `docs/project-plan.md`, `docs/websocket-protocols.md`를 읽을 것
- [`Plan/07-triage-trace-architecture-and-pose-input.md`](../Plan/07-triage-trace-architecture-and-pose-input.md)를 읽을 것
- 완료된 Task 03의 Python 환경과 Unity 프로젝트를 보존할 것
- 기존 Hand Landmarker 모델과 gesture v1 fixture를 삭제하지 않을 것
- 공식 Pose 모델 다운로드가 필요하면 출처와 권한을 확인할 것

## 작업 단계

1. 현재 Python 환경에서 `vision.PoseLandmarker` API 존재를 smoke test한다.
2. 사용할 공식 Pose Landmarker 모델 변형을 선택하고 출처·라이선스·크기·SHA-256을 기록한다.
3. 모델을 `Mediapipe/models/` 아래 명확한 Pose 전용 이름으로 배치한다.
4. `config.py`에 모델 경로, VIDEO 모드 임계값, 카메라·미러링 설정을 추가한다.
5. `pose_models.py`에 `Joint`와 `PoseTrackingResult`를 정의한다.
6. `pose_tracker.py`에 open/process/close 수명 주기를 구현한다.
7. BGR을 RGB MediaPipe 이미지로 변환하되 원본 프레임을 수정하지 않는다.
8. VIDEO API용 단조 증가 timestamp와 외부 epoch timestamp 역할을 구분한다.
9. 결과 없음, 잘못된 랜드마크 수, 비유한 좌표를 안전하게 내부 결과로 변환한다.
10. Pose 표준 인덱스 12, 14, 16을 상수로 정의하고 오른쪽 관절을 복사한다.
11. `app.py`에서 카메라와 Pose tracker를 조립하되 pointing·WebSocket은 호출하지 않는다.
12. 가짜 MediaPipe 결과 기반 단위 테스트를 작성한다.
13. 실제 카메라로 오른팔 세 관절 추적과 종료 자원 해제를 수동 검증한다.
14. README와 Task 수행 결과에 모델·환경·검증 결과를 기록한다.

## 완료 기준

- Pose Landmarker 모델이 현재 MediaPipe 버전에서 VIDEO 모드로 초기화된다.
- 웹캠 프레임에서 Pose 결과를 반복 처리한다.
- 오른쪽 어깨·팔꿈치·손목과 visibility가 내부 모델에 보존된다.
- Pose가 없을 때 예외가 아닌 정상 미검출 결과를 반환한다.
- 비유한 좌표와 손상 결과가 후속 단계로 전달되지 않는다.
- tracker, 카메라와 창이 모든 종료 경로에서 정리된다.
- 단위 테스트와 실제 카메라 검증 결과가 기록된다.
- pointer, pose v2 송신과 Unity UI는 구현하지 않았다.

## 예상 산출물

- Pose Landmarker 모델과 모델 문서
- `pose_models.py`
- `pose_tracker.py`
- Pose 설정이 추가된 `config.py`
- tracker가 조립된 `app.py`
- `tests/test_pose_tracker.py`
- 갱신된 Python README와 검증 기록

## 다음 Task와의 연결

검증된 `PoseTrackingResult`를 [`08-right-arm-pointing-and-quality.md`](./08-right-arm-pointing-and-quality.md)에 전달해 tracking 상태, pointing 유효성과 정규화 pointer를 계산한다.

## 수행 결과

- 공식 Pose Landmarker Lite float16 모델을
  `Mediapipe/models/pose_landmarker_lite.task`에 배치했다.
- OpenCV 카메라 open/read/retry/close 수명 주기와 연속 프레임 인덱스를
  구현했다.
- Pose Landmarker를 VIDEO 모드로 실행하고 원본 비미러 BGR 프레임을 RGB로
  변환한다.
- 추론용 단조 증가 timestamp와 캡처 epoch timestamp를 분리했다.
- 오른쪽 어깨 12, 팔꿈치 14, 손목 16과 visibility를 MediaPipe 독립 모델로
  복사한다.
- Pose 없음과 잘못된 랜드마크 수는 `LOST`, 일부 좌표 오류 또는 visibility
  임계값 미달은 `PARTIAL`, 세 관절이 모두 유효하면 `TRACKING`으로 처리한다.
- 콘솔 좌표 로그와 OpenCV 관절·상태·FPS 오버레이를 추가했다.
- 카메라, 결과 모델, tracker, 디버그 출력 테스트를 추가했다.
- `pytest` 20개 통과, `pip check` 통과, Python 파일 18개의 쓰기 없는 AST
  구문 검사를 통과했다.
- MediaPipe 0.10.35에서 모델 초기화와 검정 합성 프레임 추론 결과 `LOST`를
  확인했다.
- 실제 0번 카메라 640×480 프레임 30개를 처리하고 tracker와 카메라 해제를
  확인했다. 검증 당시 오른팔 일부가 화면 밖에 있어 30개 모두 `PARTIAL`이었고,
  전체 오른팔 구도에서의 `TRACKING` 장시간 안정성은 후속 수동 검증으로 남는다.
- 관리형 실행 환경이 무시 대상 `__pycache__` 생성을 허용하지 않아
  `compileall`은 권한 오류가 났다. 모듈 import를 포함한 pytest와 쓰기 없는 AST
  검사로 구문 유효성을 대체 확인했다.
- pointing, WebSocket과 Unity 코드는 구현하거나 수정하지 않았다.
