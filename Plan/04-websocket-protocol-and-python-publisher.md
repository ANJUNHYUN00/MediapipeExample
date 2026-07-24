# 04. WebSocket 프로토콜 및 Python 게시자 설계

> 레거시 v1 설계 (2026-07-24): `hand_gesture` version 1은 변경 없이 보존한다. 활성 `pose_pointer` version 2 설계는 [`08-pose-v2-protocol-and-unity-ar.md`](./08-pose-v2-protocol-and-unity-ar.md)와 [`docs/websocket-protocols.md`](../docs/websocket-protocols.md)를 따른다.

## 목적

Python의 안정화된 제스처 결과를 Unity가 일관되게 해석할 수 있는 버전 1 JSON 계약으로 변환하고, 로컬 WebSocket 서버를 통해 실시간으로 안전하게 게시한다. 연결 유무가 손 추적 루프를 막지 않도록 네트워크 책임을 분리한다.

## 구현 범위

- `hand_gesture` 버전 1 메시지 모델
- JSON 필드, enum, 기본값 및 유효성 규칙
- Unix epoch 밀리초 타임스탬프와 증가 순서 번호
- `ws://127.0.0.1:8765` WebSocket 서버
- 단일 또는 복수 로컬 클라이언트 연결 관리
- 최대 10~20Hz 송신 제한과 최신 상태 우선 큐
- 클라이언트 연결·해제 및 송신 오류 처리
- 직렬화 단위 테스트와 WebSocket 계약 테스트

인증, TLS, 외부 네트워크 공개, 원격 대전용 명령 프로토콜은 초기 범위에서 제외한다.

## 설계 내용

### 연결 계약

- 서버: Python MediaPipe 애플리케이션
- 클라이언트: Unity 애플리케이션
- 기본 URI: `ws://127.0.0.1:8765`
- 데이터 형식: UTF-8 JSON 텍스트 프레임
- 기본 메시지 종류: `hand_gesture`
- 연결당 서버가 클라이언트로 상태를 푸시하는 단방향 구조

Unity에서 별도 요청 메시지를 보내는 기능은 초기 버전에 필요하지 않다. WebSocket ping/pong과 정상 close 처리는 사용하는 라이브러리의 표준 동작을 따른다.

### 버전 1 메시지

```json
{
  "type": "hand_gesture",
  "version": 1,
  "timestamp": 1750000000123,
  "sequence": 42,
  "handDetected": true,
  "handedness": "Right",
  "gesture": "SCISSORS",
  "confidence": 0.94,
  "fingerStates": {
    "thumb": true,
    "index": true,
    "middle": true,
    "ring": false,
    "pinky": false
  }
}
```

### 필드 계약

| 필드 | JSON 형식 | 필수 | 규칙 |
|---|---|---:|---|
| `type` | string | 예 | 항상 `hand_gesture` |
| `version` | integer | 예 | 초기 버전은 `1` |
| `timestamp` | integer | 예 | 결과 프레임의 Unix epoch 밀리초 |
| `sequence` | integer | 예 | 프로세스 실행 동안 메시지마다 증가 |
| `handDetected` | boolean | 예 | 손 검출 여부 |
| `handedness` | string | 예 | `Left`, `Right`, `Unknown` |
| `gesture` | string | 예 | `ROCK`, `SCISSORS`, `PAPER`, `UNKNOWN`, `NO_HAND` |
| `confidence` | number | 예 | `0.0~1.0` |
| `fingerStates` | object | 예 | 다섯 boolean 필드를 모두 포함 |

`timestamp`는 프레임 캡처 시각을 우선 사용해 끝단 지연을 계산할 수 있게 한다. 필요할 경우 내부 로그에 별도 송신 시각을 기록하지만 버전 1 필드를 임의로 바꾸지 않는다.

### 상태별 불변 조건

| 상태 | 필수 조건 |
|---|---|
| `NO_HAND` | `handDetected=false`, `confidence=0.0`, 모든 손가락 `false`, handedness `Unknown` |
| `UNKNOWN` | `handDetected=true`, 규칙과 일치하지 않는 실제 `fingerStates` 유지 |
| 세 제스처 | `handDetected=true`, 분류 결과와 손가락 상태가 규칙에 부합 |

메시지 생성기는 위 조건을 검증하고 잘못된 내부 조합을 조용히 전송하지 않는다.

### Python 내부 메시지 모델

내부 필드는 Python 스타일을 사용할 수 있지만 JSON 경계에서는 문서에 정의된 camelCase를 정확히 사용한다.

```text
HandGestureMessageV1
  type
  version
  timestamp
  sequence
  hand_detected
  handedness
  gesture
  confidence
  finger_states
```

직렬화 결과의 키 이름과 enum 문자열을 명시적으로 테스트한다. Python enum 객체의 기본 문자열 표현에 의존하지 않는다.

### 게시자 구조

```text
GestureResult
  -> MessageBuilder
  -> Latest-state channel
  -> Rate limiter
  -> JSON serializer
  -> Connected clients
```

손 추적 루프는 네트워크 송신 완료를 기다리지 않고 최신 `GestureResult`를 게시 채널에 넣는다. 채널은 크기 1 또는 작은 제한 크기를 사용하며 가득 찼을 때 오래된 상태를 최신 상태로 교체한다.

### 송신 정책

- 기본 최대 송신 빈도는 10~20Hz 범위에서 설정한다.
- 확정 제스처가 바뀌면 다음 허용 시점에 우선 송신한다.
- 상태가 같아도 연결 상태와 마지막 데이터 시각 확인을 위해 주기적으로 전송한다.
- 연결된 클라이언트가 없어도 손 추적은 계속하거나 설정에 따라 저비용 대기한다.
- 느리거나 끊어진 클라이언트가 다른 클라이언트와 추적 루프를 막지 않게 한다.
- `sequence`는 실제 전송용 메시지를 만들 때 증가시키고 재연결 후에도 같은 프로세스에서는 계속 증가한다.

### 연결 및 종료 처리

서버는 클라이언트 집합을 안전하게 관리한다.

1. 연결 시 원격 정보와 현재 연결 수를 기록한다.
2. 가능하면 최신 상태를 새 클라이언트에 빠르게 전달한다.
3. 정상 종료와 예외 종료 모두 집합에서 클라이언트를 제거한다.
4. 송신 중 닫힌 연결은 오류 수준을 구분해 처리한다.
5. 앱 종료 시 새 연결을 막고, 게시 작업을 취소한 뒤 연결을 정상적으로 닫는다.

### 스키마 호환성

- Unity는 버전 `1`을 명시적으로 지원한다.
- 알 수 없는 `type` 또는 지원하지 않는 `version`은 UI에 적용하지 않고 경고한다.
- 버전 1에 선택 필드를 추가하는 경우 Unity가 알 수 없는 필드를 무시할 수 있어야 한다.
- 기존 필드 삭제, 형식 변경, enum 의미 변경은 새 버전으로 올린다.
- 스키마 변경 시 Python, Unity, 테스트, `docs/project-plan.md`와 이 문서를 함께 변경한다.

### 테스트 전략

- 모든 제스처의 정확한 JSON 스냅샷 또는 필드 단위 테스트
- `NO_HAND` 불변 조건 테스트
- 신뢰도 범위와 enum 유효성 테스트
- 순서 번호 증가 테스트
- 테스트 WebSocket 클라이언트의 연결, 수신, 종료 테스트
- 클라이언트가 없는 상태와 재연결 상태 테스트
- 느린 소비자에서 큐가 무제한 증가하지 않는지 테스트
- 송신 빈도가 설정 상한을 크게 넘지 않는지 시간 기반 테스트

## 입출력

### 입력

[`03-gesture-classification.md`](./03-gesture-classification.md)의 안정화된 `GestureResult`와 다음 설정:

- 서버 호스트와 포트
- 최대 송신 빈도
- 종료 신호

### 출력

- WebSocket UTF-8 JSON 텍스트 메시지
- 연결 상태와 오류 로그
- 테스트에서 사용할 수 있는 직렬화된 버전 1 메시지

외부 출력의 기준은 `hand_gesture` 버전 1 JSON 계약이다.

## 주의사항

- 기본 바인딩은 `0.0.0.0`이 아니라 `127.0.0.1`로 제한한다.
- 프레임 처리 루프에서 `send()`를 직접 블로킹 호출하지 않는다.
- 연결되지 않은 상태는 오류 폭주가 아니라 정상 대기 상태로 처리한다.
- JSON에 `NaN`, 무한대 또는 라이브러리 고유 객체가 들어가지 않게 검증한다.
- 송신 빈도 제한으로 상태 변경을 지나치게 늦추지 않는다.
- Unity와 필드 대소문자가 다르면 역직렬화 실패 원인이 되므로 계약 테스트로 고정한다.

## 다음 단계와의 연결

버전 1 JSON 계약은 [`05-unity-receiver-and-ui.md`](./05-unity-receiver-and-ui.md)의 C# 수신 모델과 UI 상태 변환의 입력이다. Unity 단계에서는 연결 재시도, 지원 버전 검사, 메인 스레드 전달 및 한글 표시 규칙을 이 계약에 맞춰 구현한다.
