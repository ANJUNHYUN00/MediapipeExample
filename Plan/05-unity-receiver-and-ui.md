# 05. Unity 수신기 및 UI 설계

## 목적

Unity가 Python WebSocket 서버에 안정적으로 연결해 버전 1 손 제스처 메시지를 수신·검증하고, Unity 메인 스레드에서 연결 상태와 주먹·가위·보 결과를 직관적으로 표시하도록 한다.

## 구현 범위

- Unity LTS 프로젝트와 필수 패키지 구성
- WebSocket 연결, 수신, 종료 및 자동 재연결
- 버전 1 JSON C# 데이터 모델
- 메시지 타입, 버전, enum 및 값 범위 검증
- 백그라운드 수신과 메인 스레드 사이의 안전한 큐
- 연결 상태와 제스처 결과 UI
- 마지막 수신 시각과 데이터 만료 처리
- EditMode 파싱 테스트와 PlayMode UI 테스트

게임 대전 규칙, 점수, 원격 서버, 모바일 플랫폼 최적화는 이 단계에 포함하지 않는다.

## 설계 내용

### Unity 컴포넌트 구조

```text
WebSocketConnection
  -> Raw message event
  -> HandGestureParser
  -> Validated HandGestureMessageV1
  -> Thread-safe latest-message queue
  -> HandGesturePresenter (main thread)
  -> Text / Icon / Confidence / Connection UI
```

권장 책임:

| 컴포넌트 | 책임 |
|---|---|
| `WebSocketConnection` | 연결, 수신 루프, 취소, 재연결 |
| `HandGestureMessageV1` | JSON 데이터 구조 |
| `HandGestureParser` | 역직렬화와 값 검증 |
| `MainThreadMessageQueue` | 스레드 경계를 넘는 최신 메시지 전달 |
| `HandGesturePresenter` | 메시지를 표시 상태로 변환 |
| `ConnectionStatusView` | 연결 상태 표시 |
| `HandGestureView` | 텍스트, 아이콘, 신뢰도 갱신 |
| `WebSocketSettings` | URI와 재연결 관련 설정 |

### C# 수신 모델

JSON 키와 일치하는 직렬화 필드를 정의한다.

```text
HandGestureMessageV1
  type: string
  version: int
  timestamp: long
  sequence: long
  handDetected: bool
  handedness: string
  gesture: string
  confidence: float
  fingerStates: FingerStatesDto
```

`FingerStatesDto`는 `thumb`, `index`, `middle`, `ring`, `pinky` boolean을 포함한다. 네트워크 DTO를 Unity 표시 로직에서 직접 수정하지 않고, 검증 후 읽기 전용 도메인 상태로 변환하는 방식을 권장한다.

### 메시지 검증

다음 조건을 만족한 메시지만 UI 입력으로 사용한다.

- JSON 역직렬화 성공
- `type == "hand_gesture"`
- `version == 1`
- gesture가 지원 enum 중 하나
- handedness가 `Left`, `Right`, `Unknown` 중 하나
- confidence가 `0.0~1.0`
- `fingerStates`가 존재
- 새 메시지의 sequence가 마지막 적용 메시지보다 최신

지원하지 않는 타입과 버전, 잘못된 JSON은 앱을 종료하지 않고 제한된 경고 로그를 남긴다. 알 수 없는 추가 JSON 필드는 무시한다.

### 연결 상태 기계

```text
Disconnected
  -> Connecting
  -> Connected
  -> Reconnecting
  -> Connected
  -> Closing
  -> Disconnected
```

각 상태는 UI 문자열과 색상에 대응한다.

| 상태 | 표시 예시 |
|---|---|
| `Connecting` | `MediaPipe에 연결 중...` |
| `Connected` | `연결됨` |
| `Reconnecting` | `연결이 끊어졌습니다. 재연결 중...` |
| `Disconnected` | `연결되지 않음` |
| `Closing` | `연결 종료 중...` |

재연결은 즉시 무한 반복하지 않고 상한이 있는 지수 백오프 또는 단순 제한 간격을 사용한다. 애플리케이션 종료이나 사용자가 명시적으로 중지한 경우 재연결하지 않는다.

### 스레드 처리

WebSocket 라이브러리의 콜백이나 수신 작업이 Unity 메인 스레드에서 실행된다고 가정하지 않는다.

1. 수신 스레드에서 JSON을 파싱하거나 원문을 큐에 넣는다.
2. 큐는 스레드 안전해야 하며 크기를 제한한다.
3. 실시간 상태이므로 오래된 메시지보다 가장 최신 메시지를 우선한다.
4. `MonoBehaviour.Update()`에서 큐를 비우고 최신 유효 상태를 Presenter에 전달한다.
5. TextMeshPro, Image, GameObject 등 Unity API는 메인 스레드에서만 변경한다.

### UI 구성

필수 화면 요소:

- 앱 제목
- 연결 상태 텍스트와 상태 색상
- 현재 제스처 한글 텍스트
- 제스처별 이미지 또는 아이콘
- 신뢰도 표시
- 선택적 handedness와 마지막 수신 시각

표시 규칙:

| gesture | 한글 표시 | UI 동작 |
|---|---|---|
| `ROCK` | `주먹` | 주먹 아이콘 활성화 |
| `SCISSORS` | `가위` | 가위 아이콘 활성화 |
| `PAPER` | `보` | 보 아이콘 활성화 |
| `UNKNOWN` | `손 모양을 확인해 주세요` | 결과 아이콘 비활성 또는 미인식 아이콘 |
| `NO_HAND` | `손을 카메라에 보여 주세요` | 결과 아이콘 비활성 |

연결이 끊기면 마지막 결과가 현재 상태로 오해되지 않도록 결과 영역을 비활성화하고 연결 오류를 우선 표시한다.

### 데이터 만료

소켓이 연결되어 있어도 일정 시간 메시지가 도착하지 않으면 데이터를 오래된 상태로 처리한다.

- 마지막 수신 시각을 메인 스레드에 기록한다.
- 설정된 제한 시간을 넘으면 `데이터 수신 대기 중`을 표시한다.
- 데이터 만료와 WebSocket 연결 끊김은 별도 상태로 진단한다.
- 새 유효 메시지가 오면 즉시 정상 표시로 복구한다.

### Unity 수명 주기

- `OnEnable` 또는 명시적 시작 시 연결 작업을 시작한다.
- 연결 작업은 `CancellationToken` 등 취소 가능한 구조를 사용한다.
- `OnDisable`, `OnDestroy`, 앱 종료에서 수신 및 재연결 작업을 취소한다.
- 도메인 리로드와 Play Mode 종료 후 백그라운드 작업이 남지 않게 한다.
- Scene 재진입 시 중복 연결 객체가 생기지 않도록 소유권을 하나로 정한다.

### 설정

WebSocket URI, 재연결 간격, 데이터 만료 시간은 Inspector에 노출하되 기본값을 한곳에서 관리한다. 빌드별로 바뀌는 값은 `ScriptableObject` 또는 설정 컴포넌트로 관리할 수 있다.

### 테스트 전략

EditMode:

- 정상 버전 1 JSON 파싱
- 다섯 제스처 상태 변환
- 잘못된 JSON, 타입, 버전, enum, confidence 거부
- 알 수 없는 추가 필드 허용
- sequence 역행 메시지 무시

PlayMode:

- 연결 상태별 UI
- 큐에 들어온 메시지가 다음 프레임에 UI에 반영
- 연결 끊김 시 결과 비활성
- 데이터 만료 및 복구
- 오브젝트 종료 후 연결 작업 취소

수동 테스트:

- Unity를 Python보다 먼저 실행
- Python 서버 재시작 후 자동 재연결
- 빠른 제스처 전환에서 UI가 최신 결과를 표시

## 입출력

### 입력

[`04-websocket-protocol-and-python-publisher.md`](./04-websocket-protocol-and-python-publisher.md)의 WebSocket UTF-8 JSON:

- URI `ws://127.0.0.1:8765`
- `hand_gesture` 버전 1 메시지
- 연결·종료 이벤트

### 출력

- 연결 상태 UI
- `주먹`, `가위`, `보`, 미인식, 손 없음 UI
- 결과 이미지 또는 아이콘
- 신뢰도와 선택적 handedness
- 파싱, 연결 및 재연결 진단 로그

## 주의사항

- WebSocket 콜백에서 Unity API를 직접 호출하지 않는다.
- C# JSON 도구가 필드, 프로퍼티, 중첩 객체를 어떤 방식으로 처리하는지 작은 계약 테스트로 먼저 확인한다.
- Editor에서만 동작하는 라이브러리를 선택하지 말고 대상 데스크톱 빌드에서도 검증한다.
- 재연결 작업과 Scene 수명 주기를 분리하지 않으면 중복 연결이나 종료 후 예외가 발생할 수 있다.
- 마지막으로 받은 제스처를 연결 끊김 후에도 활성 상태로 남겨두지 않는다.
- 네트워크 DTO 필드명 변경은 Python 계약 변경이므로 양쪽 문서와 테스트를 함께 수정한다.

## 다음 단계와의 연결

Python 게시자와 Unity 수신·표시 기능이 준비되면 [`06-integration-test-and-stabilization.md`](./06-integration-test-and-stabilization.md)에서 실제 웹캠부터 Unity UI까지 끝단 통합 테스트를 수행한다. 지연 시간, 재연결, 다양한 손 조건 및 장시간 안정성을 측정해 임계값과 송신 설정을 확정한다.
