# 02. Python 손 추적 설계

> 레거시 RPS 설계 (2026-07-24): Hand Landmarker 계획은 호환성 이력으로 보존한다. 활성 구현 기준은 [`07-triage-trace-architecture-and-pose-input.md`](./07-triage-trace-architecture-and-pose-input.md)의 Pose Landmarker와 오른쪽 어깨·팔꿈치·손목 입력이다.

## 목적

Python에서 웹캠 프레임을 안정적으로 수집하고 MediaPipe로 한 손의 21개 랜드마크를 검출해, 카메라와 MediaPipe API에 의존하지 않는 표준 내부 모델로 변환한다. 제스처 분류 단계가 영상 처리 세부사항 없이 랜드마크 데이터만 사용할 수 있게 한다.

## 구현 범위

- OpenCV 기반 웹캠 열기, 프레임 읽기, 종료 처리
- BGR 프레임을 MediaPipe 입력 형식으로 변환
- 한 손의 21개 정규화 랜드마크 검출
- 좌우 손 정보와 검출 신뢰도 추출
- 손 미검출 상태 생성
- 선택적인 디버그 랜드마크 오버레이
- 트래커 입력·출력 모델과 오류 처리
- 카메라를 사용하지 않는 변환 로직 테스트

이 단계는 손가락 펼침 여부, 가위바위보 판정, WebSocket 송신을 구현하지 않는다.

## 설계 내용

### 처리 파이프라인

```text
OpenCV VideoCapture
  -> frame read
  -> optional horizontal mirror
  -> BGR to RGB
  -> MediaPipe detect
  -> select one hand
  -> validate 21 landmarks
  -> HandTrackingResult
```

실제 판정용 좌표와 사용자에게 보여주는 디버그 영상의 좌우 반전 정책을 분명히 한다. 프레임을 반전한 뒤 검출한다면 handedness 해석도 동일한 기준에 맞춰 검증해야 한다.

### 랜드마크 인덱스

MediaPipe의 표준 21개 인덱스를 사용한다.

| 영역 | 인덱스 |
|---|---|
| 손목 | `0` |
| 엄지 | `1` CMC, `2` MCP, `3` IP, `4` TIP |
| 검지 | `5` MCP, `6` PIP, `7` DIP, `8` TIP |
| 중지 | `9` MCP, `10` PIP, `11` DIP, `12` TIP |
| 약지 | `13` MCP, `14` PIP, `15` DIP, `16` TIP |
| 소지 | `17` MCP, `18` PIP, `19` DIP, `20` TIP |

매직 넘버 대신 enum 또는 상수 집합으로 관리한다.

### 내부 데이터 모델

개념적인 모델은 다음과 같다.

```text
Landmark:
  x: float
  y: float
  z: float

HandTrackingResult:
  hand_detected: bool
  landmarks: list[Landmark]  # detected이면 정확히 21개
  handedness: Left | Right | Unknown
  detection_confidence: float
  frame_timestamp_ms: int
```

`hand_detected`가 `false`이면 `landmarks`는 빈 목록, `handedness`는 `Unknown`, 신뢰도는 `0.0`으로 통일한다. MediaPipe 객체를 후속 모듈에 그대로 노출하지 않아 테스트와 API 교체를 쉽게 한다.

### 카메라 캡처

- 카메라 인덱스는 설정에서 받는다.
- `isOpened()` 결과를 확인하고 실패 원인을 명확히 보고한다.
- 연속 프레임 읽기 실패 시 즉시 무한 반복하지 않고 제한 횟수 또는 종료 정책을 적용한다.
- 종료 경로에서 `VideoCapture.release()`와 디버그 창 정리를 보장한다.
- 필요할 경우 너비, 높이, FPS를 요청하되 실제 적용값을 로그로 확인한다.

카메라 프레임 타임스탬프는 가능한 한 캡처 직후 Unix epoch 밀리초로 생성해 최종 메시지 지연 측정에 사용한다.

### MediaPipe 설정

- MediaPipe Tasks `HandLandmarker` 0.10.35와 공식 full float16 모델 번들을 사용한다.
- 모델 기본 경로는 `Mediapipe/models/hand_landmarker.task`다.
- 연속 프레임은 `VIDEO` 모드와 단조 증가 밀리초 타임스탬프로 처리한다.
- 최대 손 수는 초기 버전에서 `1`이다.
- 정적 이미지 모드보다 연속 영상 추적에 적합한 설정을 사용한다.
- 검출, 존재, 추적 신뢰도 임계값은 설정으로 분리한다.
- 선택한 MediaPipe API가 모델 파일을 요구하면 자산 경로를 설정에 둔다.
- 매 프레임 객체를 불필요하게 재생성하지 않고 트래커 수명 주기를 애플리케이션과 함께 관리한다.

여러 손 결과가 반환될 가능성에 대비해 선택 정책을 고정한다. 초기 정책은 신뢰도가 가장 높은 한 손을 선택하되, 프레임 간 손이 자주 바뀌는 문제가 발견되면 위치 연속성을 보조 기준으로 추가한다.

### 좌표와 handedness

- `x`, `y`는 영상 크기에 정규화된 좌표이며 일반적으로 좌상단이 원점이다.
- `z`는 화면 픽셀 단위가 아니므로 절대 거리로 해석하지 않는다.
- 제스처 분류기는 이동과 크기에 덜 민감하도록 손바닥 크기로 정규화된 벡터를 사용한다.
- 카메라 미러링 여부에 따라 `Left`와 `Right`가 실제 사용자 기준과 일치하는지 양손 테스트로 확인한다.
- handedness 신뢰도가 부족하거나 값이 없으면 `Unknown`으로 보존한다.

### 디버그 표시

디버그 모드에서만 다음을 오버레이한다.

- 21개 랜드마크와 연결선
- handedness와 검출 신뢰도
- 처리 FPS
- 이후 단계에서 제공되는 확정 제스처

디버그 창은 제품의 필수 출력이 아니며 끌 수 있어야 한다. 영상 저장 기능은 기본 제공하지 않는다.

### 오류 및 상태 처리

| 상황 | 처리 |
|---|---|
| 카메라 열기 실패 | 오류 로그 후 비정상 상태를 알리고 자원 정리 |
| 일시적 프레임 읽기 실패 | 제한된 재시도와 경고 |
| MediaPipe가 손을 찾지 못함 | 정상적인 `hand_detected=false` 결과 생성 |
| 랜드마크 수가 21개가 아님 | 손상된 결과로 간주하고 미검출 또는 오류 처리 |
| 트래커 예외 | 오류 기록, 자원 정리, 앱 정책에 따라 종료 |

손 미검출은 예외가 아니라 정상 상태다.

### 테스트 전략

- MediaPipe 결과를 모사해 21개 랜드마크가 내부 모델로 정확히 변환되는지 테스트한다.
- 손 없음, handedness 누락, 잘못된 랜드마크 개수를 테스트한다.
- 타임스탬프와 신뢰도 범위가 유지되는지 확인한다.
- 실제 카메라 테스트는 좌우 손, 손바닥/손등, 거리와 조명을 바꾸어 수동 수행한다.

## 입출력

### 입력

- OpenCV BGR 프레임
- 카메라 인덱스와 해상도 설정
- MediaPipe 검출·추적 임계값
- 미러링 및 디버그 표시 설정

### 출력

손 검출 시:

```text
HandTrackingResult(
  hand_detected=True,
  landmarks=[21 normalized landmarks],
  handedness="Right",
  detection_confidence=0.95,
  frame_timestamp_ms=...
)
```

손 미검출 시:

```text
HandTrackingResult(
  hand_detected=False,
  landmarks=[],
  handedness="Unknown",
  detection_confidence=0.0,
  frame_timestamp_ms=...
)
```

## 주의사항

- 화면 `y` 좌표가 작다는 이유만으로 손가락이 펼쳐졌다고 판정하지 않는다. 이 단계는 판정을 하지 않고 원본 정규화 좌표를 보존한다.
- 미러링 정책을 바꾸면 handedness와 엄지 판정에 직접 영향을 주므로 관련 테스트를 다시 수행한다.
- MediaPipe의 신뢰도는 제스처 분류 정확도와 동일하지 않다. 최종 `confidence` 계산과 구분한다.
- 손이 화면 가장자리에 잘리면 21개 점이 반환되어도 판정 품질이 낮을 수 있다. 후속 단계가 품질을 판단할 수 있도록 원본 값을 왜곡하지 않는다.
- 카메라 루프에서 블로킹 네트워크 송신을 직접 수행하지 않는다.

## 다음 단계와의 연결

`HandTrackingResult`는 [`03-gesture-classification.md`](./03-gesture-classification.md)의 제스처 분류기 입력이 된다. 다음 단계는 21개 좌표를 손바닥 기준으로 해석해 손가락별 펼침 상태를 계산하고, `ROCK`, `SCISSORS`, `PAPER`, `UNKNOWN`, `NO_HAND` 중 하나로 분류·안정화한다.
