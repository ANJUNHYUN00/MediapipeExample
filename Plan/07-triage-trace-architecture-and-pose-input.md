# 07. Triage Trace 아키텍처 및 Pose 입력 설계

## 목적

기존 Python 서버 / Unity 클라이언트 구조를 유지하면서 활성 입력을 MediaPipe Pose Landmarker로 전환한다. 오른쪽 어깨·팔꿈치·손목만으로 추적 품질과 포인터 후보를 생성하되 의료적 의미를 계산하지 않는 책임 경계를 확정한다.

## 구현 범위

- Pose Landmarker 모델 자산과 VIDEO 모드 실행 기준
- OpenCV 웹캠 프레임 입력과 Pose 결과 변환
- 오른쪽 관절 3개의 내부 데이터 모델
- `TRACKING`, `PARTIAL`, `LOST` 품질 상태
- pointing 유효성 검사와 정규화 pointer 계산 경계
- Python 모듈 책임과 기존 파일 재사용 방식
- 카메라가 없는 자동 테스트와 실제 카메라 수동 검증 전략

WebSocket 송신, Unity DTO, AR 포인터 UI와 실제 의료 판단은 이 단계에서 구현하지 않는다.

## 설계 내용

### 활성 처리 흐름

```text
CameraCapture
  -> PoseLandmarkerAdapter
  -> PoseTrackingResult
  -> RightArmInputExtractor
  -> PointingResolver
  -> PosePointerState
```

### Python 구조 전환

기존 `src/mediapipe_rps` 패키지 경로는 삭제하지 않는다. 패키지 이름은 저장소 호환성을 위해 당분간 유지하고 다음 모듈을 추가한다.

```text
pose_tracker.py       # Pose Landmarker 수명 주기와 결과 변환
pose_models.py        # PoseTrackingResult, Joint, PosePointerState
pointing.py           # tracking 품질과 포인터 계산
```

기존 `hand_tracker.py`, `gesture_classifier.py`, `stabilizer.py`는 레거시 v1 경로로 보존하며 활성 Pose 흐름이 직접 의존하지 않는다. 공통 `camera.py`, `config.py`, `websocket_server.py`, `app.py`는 책임을 유지한 채 조립 지점만 확장한다.

### Pose Landmarker 실행

- MediaPipe Tasks `PoseLandmarker`를 사용한다.
- 연속 웹캠 입력은 `VIDEO` 모드로 처리한다.
- 모델 경로, 검출·존재·추적 임계값은 설정에서 관리한다.
- 모델 객체는 앱 수명 동안 한 번 생성하고 정상 종료한다.
- 공식 모델 자산의 출처, 라이선스, 파일 크기와 SHA-256을 설치 시 기록한다.
- 현재 저장소에는 Pose 모델이 아직 없으므로 Task 07이 실제 호환성을 검증한다.

### 내부 모델

```text
Joint:
  x: float
  y: float
  z: float
  visibility: float

PoseTrackingResult:
  pose_detected: bool
  right_shoulder: Joint | None
  right_elbow: Joint | None
  right_wrist: Joint | None
  frame_timestamp_ms: int
  frame_index: int

PosePointerState:
  tracking: TRACKING | PARTIAL | LOST
  pointing: bool
  pointer_x: float | None
  pointer_y: float | None
  joints: three optional Joint values
```

MediaPipe 라이브러리 객체를 후속 모듈에 노출하지 않는다.

### tracking 결정

1. Pose 결과가 없으면 `LOST`다.
2. 세 관절 중 하나라도 좌표가 누락되거나 비유한 값이면 `PARTIAL`이다.
3. visibility가 설정 임계값보다 낮은 관절이 있으면 `PARTIAL`이다.
4. 세 관절이 모두 유효하면 `TRACKING`이다.
5. `TRACKING` 이후에도 팔 벡터가 너무 짧거나 포인터 계산이 실패하면 `pointing=false`일 수 있다.

### 포인터 계산

- 2D 방향은 `(wrist.x - elbow.x, wrist.y - elbow.y)`다.
- 어깨–팔꿈치와 팔꿈치–손목 길이를 검사해 축소된 자세나 잘못된 좌표를 거부한다.
- 초기 포인터 후보는 손목에서 전완 방향으로 설정 가능한 계수만큼 연장한다.
- 출력은 Unity 화면 기준으로 합의한 정규화 좌표다.
- 화면 y축 변환을 Python과 Unity 양쪽에서 중복 적용하지 않는다.
- 경계 밖 값을 clamp할지 무효 처리할지는 Task 08에서 실제 사용성 검증 후 확정한다.

### 안전 경계

Pose 좌표는 UI 포인터 입력에만 사용한다. 자세로 부상, 의식, 호흡, 통증, 위험도, 치료 우선순위를 추론하지 않는다. 로그와 메시지에도 의료 평가 결과를 추가하지 않는다.

### 테스트 전략

- 가짜 Pose 결과의 관절 인덱스 매핑
- Pose 없음과 부분 관절 결과
- NaN·무한대·visibility 범위 오류
- timestamp와 frame index 보존
- 관절 벡터 길이 0과 경계 밖 포인터
- 실제 카메라에서 오른팔 가림, 화면 가장자리, 거리 변화

## 입출력

### 입력

- OpenCV BGR 프레임
- Pose Landmarker 모델과 설정
- 캡처 timestamp와 frame index

### 출력

- MediaPipe 객체와 독립적인 `PoseTrackingResult`
- 후속 pointing 단계가 사용하는 오른쪽 세 관절과 visibility
- 추적·초기화·종료 진단 로그

## 주의사항

- Hand Landmarker 모델을 Pose 모델로 잘못 재사용하지 않는다.
- `visibility`는 의료 신뢰도가 아니라 관절 관측 품질이다.
- 이미지 정규화 `z`를 미터 단위로 해석하지 않는다.
- 미러링 정책과 오른쪽 관절 의미를 실제 카메라로 검증한다.
- 카메라 루프에서 WebSocket 송신을 블로킹 호출하지 않는다.
- 이번 설계 문서 작업에서는 실제 Pose 코드를 구현하지 않는다.

## 다음 단계와의 연결

이 설계는 [`Tasks/07-pose-landmarker-runtime.md`](../Tasks/07-pose-landmarker-runtime.md)의 직접 구현 기준이다. Pose 실행과 내부 모델이 검증되면 Task 08에서 tracking 품질, pointing과 pointer 계산을 구현한다.
