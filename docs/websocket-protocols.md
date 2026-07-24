# Triage Trace WebSocket 프로토콜

## 1. 공통 연결 계약

- 서버: Python MediaPipe 애플리케이션
- 클라이언트: Unity 애플리케이션
- URI: `ws://127.0.0.1:8765`
- 형식: UTF-8 JSON 텍스트 프레임
- 방향: Python에서 Unity로 상태를 푸시
- 정수 시각: Unix epoch 밀리초
- `sequence`: 프로세스 수명 동안 실제 송신마다 증가
- 알 수 없는 추가 필드는 허용하되 알 수 없는 `type`/`version` 조합은 적용하지 않음

## 2. 레거시 gesture 프로토콜 v1

`type="hand_gesture"`, `version=1` 계약은 기존 호환성 기준으로 동결한다. 다음 필드와 의미를 삭제하거나 Pose 용도로 재해석하지 않는다.

- `handDetected`
- `handedness`
- `gesture`
- `confidence`
- `fingerStates`

지원 gesture는 `ROCK`, `SCISSORS`, `PAPER`, `UNKNOWN`, `NO_HAND`다. 기준 fixture는 `Mediapipe/tests/fixtures/messages/hand_gesture_*.json`이며 이번 전환에서 수정하지 않는다.

## 3. Pose 데이터 프로토콜 v2

### 메시지 예시

```json
{
  "type": "pose_pointer",
  "version": 2,
  "timestamp": 1750000100123,
  "sequence": 100,
  "tracking": "TRACKING",
  "pointing": true,
  "pointer": {
    "x": 0.72,
    "y": 0.34
  },
  "joints": {
    "rightShoulder": {"x": 0.58, "y": 0.36, "z": -0.12},
    "rightElbow": {"x": 0.66, "y": 0.43, "z": -0.16},
    "rightWrist": {"x": 0.76, "y": 0.38, "z": -0.20}
  },
  "visibility": {
    "rightShoulder": 0.99,
    "rightElbow": 0.97,
    "rightWrist": 0.95
  }
}
```

### 필드 정의

| 필드 | 형식 | 필수 | 규칙 |
|---|---|---:|---|
| `type` | string | 예 | 항상 `pose_pointer` |
| `version` | integer | 예 | 항상 `2` |
| `timestamp` | integer | 예 | 입력 프레임의 Unix epoch 밀리초 |
| `sequence` | integer | 예 | 실제 송신마다 증가하는 64비트 범위 정수 |
| `tracking` | string | 예 | `TRACKING`, `PARTIAL`, `LOST` |
| `pointing` | boolean | 예 | 포인터 입력 사용 가능 여부 |
| `pointer` | object 또는 null | 예 | 유효 시 정규화 `x`, `y`; 아니면 null |
| `joints` | object | 예 | 세 오른쪽 관절 키를 모두 포함 |
| `visibility` | object | 예 | 세 관절 각각 `0.0~1.0` |

### `pointer`

```text
pointer:
  x: finite number, normally 0.0~1.0
  y: finite number, normally 0.0~1.0
```

계산 결과가 화면 밖이면 Python의 경계 정책에 따라 clamp하거나 `pointing=false`로 거부한다. 정책은 구현 전에 하나로 고정해야 하며 Unity가 임의로 다른 값을 재계산하지 않는다.

### `joints`

각 키는 `{x, y, z}` 또는 `null`이다.

```text
joints:
  rightShoulder  # MediaPipe Pose index 12
  rightElbow     # MediaPipe Pose index 14
  rightWrist     # MediaPipe Pose index 16
```

`x`, `y`는 이미지 정규화 좌표, `z`는 MediaPipe 상대 깊이다. 세 값은 유한한 JSON number여야 한다. 좌표가 누락되거나 비유한 경우 해당 관절을 `null`로 보낸다. `NaN`과 무한대는 JSON에 넣지 않는다.

### `visibility`

`rightShoulder`, `rightElbow`, `rightWrist`의 MediaPipe visibility를 `0.0~1.0`로 전달한다. 누락 관절의 visibility는 `0.0`이다. 초기 사용 임계값은 설정으로 관리하고 Task 08에서 실제 환경으로 확정한다.

## 4. 상태별 불변 조건

| tracking | joints | pointing | pointer |
|---|---|---:|---|
| `TRACKING` | 세 관절 좌표가 모두 유효 | true 또는 false | pointing=true일 때만 object |
| `PARTIAL` | 하나 이상 null 또는 품질 미달 | false | null |
| `LOST` | 세 관절 모두 null | false | null |

추가 규칙:

- `pointing=true`이면 `tracking="TRACKING"`이어야 한다.
- `pointing=true`이면 `pointer`가 존재하고 `x`, `y`가 유한해야 한다.
- `pointing=false`이면 `pointer=null`이어야 한다.
- `PARTIAL`에서 유효한 관절 좌표는 진단과 시각화용으로 보존할 수 있다.
- `LOST`에서는 모든 visibility가 `0.0`이다.
- visibility가 범위 밖이면 메시지를 거부한다.

## 5. Fixture

| 파일 | 의미 | 기대 결과 |
|---|---|---|
| `pose_pointer_v2_tracking.json` | 정상 세 관절과 유효 포인터 | 수락 |
| `pose_pointer_v2_lost.json` | Pose 추적 실패 | 수락, 포인터 비활성 |
| `pose_pointer_v2_partial.json` | 손목 좌표 누락과 낮은 visibility | 수락, 포인터 비활성 |

위 파일은 `Mediapipe/tests/fixtures/messages/`에 두고 Python 메시지 테스트와 Unity EditMode 파서 테스트가 공유한다.

## 6. Python 검증

- type/version과 모든 필수 키를 명시적으로 생성한다.
- enum 문자열을 라이브러리 객체의 기본 문자열 변환에 맡기지 않는다.
- 좌표와 visibility가 유한하고 허용 범위인지 검사한다.
- 상태별 불변 조건을 통과하지 못한 내부 모델을 전송하지 않는다.
- MediaPipe 객체와 영상 프레임을 JSON에 포함하지 않는다.

## 7. Unity 검증

- v1과 v2 DTO·Parser를 분리한다.
- type/version을 확인한 뒤 올바른 Parser로 라우팅한다.
- 알 수 없는 타입, 버전, tracking과 잘못된 불변 조건을 UI에 적용하지 않는다.
- 네트워크 콜백에서 Unity API를 직접 호출하지 않는다.
- `PARTIAL`, `LOST`, 데이터 만료와 연결 끊김에서 포인터를 숨긴다.
- v2 데이터로 실제 의료 판단 또는 자동 환자 분류를 수행하지 않는다.
