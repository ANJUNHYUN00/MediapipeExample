# 08. Pose v2 프로토콜 및 Unity AR 설계

## 목적

Python의 오른팔 포인터 상태를 `pose_pointer` version 2 메시지로 게시하고 Unity가 이를 안전하게 검증해 모의 AR 포인터에 반영하는 구조를 확정한다. 기존 `hand_gesture` version 1은 변경 없이 보존한다.

## 구현 범위

- v1/v2 메시지 라우팅과 호환성 원칙
- pose v2 DTO, 유효성, 상태별 불변 조건
- 최신 상태 우선 WebSocket 게시
- Unity v2 Parser와 메인 스레드 전달
- 포인터 표시·숨김·데이터 만료 정책
- 모의 인터페이스 안전 고지
- Python 계약 테스트와 Unity EditMode·PlayMode 테스트

Pose 추론과 오른팔 포인터 계산 자체는 Plan 07 범위이며 이 단계에서 재계산하지 않는다.

## 설계 내용

### Dual-protocol 라우팅

```text
JSON text
  -> inspect type + version
     -> hand_gesture + 1 -> HandGestureMessageV1 parser (legacy)
     -> pose_pointer + 2 -> PosePointerMessageV2 parser (active)
     -> otherwise reject with limited warning
```

한 DTO에 v1과 v2의 선택 필드를 모두 넣지 않는다.

### Pose v2 모델

```text
PosePointerMessageV2
  type: "pose_pointer"
  version: 2
  timestamp: long
  sequence: long
  tracking: "TRACKING" | "PARTIAL" | "LOST"
  pointing: bool
  pointer: PointerDto | null
  joints: RightArmJointsDto
  visibility: RightArmVisibilityDto
```

정확한 JSON 계약은 [`docs/websocket-protocols.md`](../docs/websocket-protocols.md)를 단일 기준으로 사용한다.

### Python 게시

- `PosePointerState`를 메시지 빌더에서 검증·직렬화한다.
- 큐는 크기 1 또는 작은 제한 크기로 최신 상태를 우선한다.
- 기본 송신 빈도는 10~20Hz 이하다.
- `TRACKING`에서 `PARTIAL`/`LOST`로 바뀌면 다음 허용 시점에 우선 전송한다.
- 연결된 클라이언트가 없어도 Pose 루프를 막지 않는다.
- v1 생성기를 유지하되 활성 앱 조립은 v2 게시자를 선택한다.

### Unity 수신

- `PosePointerMessageV2`와 검증된 도메인 상태를 분리한다.
- 수신 스레드에서 Unity API를 호출하지 않는다.
- 최신 유효 메시지만 메인 스레드 큐로 전달한다.
- sequence 역행 메시지를 무시한다.
- `timestamp` 기반 데이터 만료를 연결 상태와 별도로 관리한다.

### Unity 포인터 상태

| 조건 | 표시 |
|---|---|
| 연결됨 + 최신 + `pointing=true` | 포인터 활성 |
| `TRACKING` + `pointing=false` | 추적 중이나 포인터 비활성 안내 |
| `PARTIAL` | 필요한 오른팔 관절을 보여 달라는 안내 |
| `LOST` | 사용자를 찾을 수 없음 안내 |
| 데이터 만료 | 포인터 숨김, 데이터 대기 |
| 연결 끊김 | 포인터 숨김, 재연결 상태 |

### 좌표 변환

- v2 pointer는 정규화 이미지 좌표다.
- Unity Presenter가 Canvas 또는 AR 상호작용 평면 좌표로 한 번만 변환한다.
- 카메라 영상 미러링과 Unity 화면 미러링을 별도 설정으로 둔다.
- Python이 pointer를 `[0.0, 1.0]`로 clamp하므로 Unity는 범위 밖 pointer를
  계약 위반으로 거부하고 별도로 다시 clamp하거나 재계산하지 않는다.

### 모의 시나리오 UI

- 제목과 화면에 `Simulation Only / 실제 의료 판단용이 아님`을 표시한다.
- 포인터가 가상 표적을 가리키는 hover는 허용한다.
- 실제 환자 상태나 응급도 등급을 자동 계산하지 않는다.
- 훈련용 선택은 사용자의 명시적 dwell/click 정책이 정의되기 전까지 hover 시각화로 제한한다.

### 테스트

Python:

- 세 v2 fixture의 직렬화와 의미 검증
- 상태별 불변 조건, 비유한 좌표, visibility 범위
- sequence 증가와 최신 상태 큐

Unity EditMode:

- v1과 v2 라우팅
- v2 정상·LOST·PARTIAL 파싱
- 잘못된 type/version/state 조합 거부
- 추가 필드 허용

Unity PlayMode:

- 메인 스레드 포인터 적용
- tracking 변화와 데이터 만료에서 포인터 숨김
- 연결 끊김·재연결
- 비의료 안전 고지 존재

## 입출력

### 입력

- Plan 07의 `PosePointerState`
- WebSocket 설정
- v1 및 v2 fixture

### 출력

- `pose_pointer` version 2 UTF-8 JSON
- Unity 검증 도메인 상태
- 모의 AR 포인터, 추적·연결 상태 UI

## 주의사항

- version 2가 version 1의 필드를 덮어쓰는 마이그레이션이 아니다.
- `visibility`와 `tracking`을 의료 신뢰도처럼 표시하지 않는다.
- 연결이 살아 있어도 데이터가 오래되면 포인터를 숨긴다.
- Unity에서 Pose 기하를 다시 계산해 Python 결과와 다른 의미를 만들지 않는다.
- 실제 의료 분류 필드나 결과 화면을 추가하지 않는다.

## 다음 단계와의 연결

Task 08에서 포인터 상태가 준비됐다.
[`Tasks/09-pose-v2-publisher-and-unity-receiver.md`](../Tasks/09-pose-v2-publisher-and-unity-receiver.md)에서
v2 메시지 빌더, 게시자와 Unity 수신 기반을 구현한다. 그 후 별도 Unity AR UI
Task에서 모의 포인터와 시나리오 화면을 완성한다.
