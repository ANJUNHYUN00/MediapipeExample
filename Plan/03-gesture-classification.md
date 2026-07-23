# 03. 가위바위보 제스처 분류 설계

## 목적

MediaPipe의 21개 손 랜드마크를 손 방향과 화면 기울기에 가능한 한 강인한 특징으로 변환하고, 손가락 상태를 기반으로 주먹, 가위, 보를 분류한다. 프레임별 결과 흔들림을 줄여 Unity에 전달할 안정된 상태를 만든다.

## 구현 범위

- 랜드마크 유효성 및 손 크기 검사
- 손가락별 관절 벡터와 굽힘 각도 계산
- 엄지를 포함한 다섯 손가락의 펼침 상태 산출
- `ROCK`, `SCISSORS`, `PAPER`, `UNKNOWN`, `NO_HAND` 판정
- 판정 신뢰도 계산
- 최근 프레임 다수결 기반 안정화
- 상태 변경 우선 처리와 손 미검출 지연 처리
- 합성 랜드마크 및 기록 샘플 기반 단위 테스트

커스텀 학습 모델, 새로운 제스처, 다중 손 조합 판정은 초기 구현 범위가 아니다.

## 설계 내용

### 처리 흐름

```text
HandTrackingResult
  -> landmark validation
  -> palm scale / vectors
  -> joint angle features
  -> FingerStates
  -> raw gesture
  -> confidence
  -> temporal stabilizer
  -> GestureResult
```

제스처 분류와 시간 안정화는 별도 클래스로 둔다. 이 분리는 고정 랜드마크 단위 테스트와 시퀀스 단위 안정화 테스트를 각각 가능하게 한다.

### 특징 계산

각 손가락 관절의 세 점 `A-B-C`에서 다음 각도를 구한다.

```text
angle(A, B, C) =
  arccos(
    dot(A-B, C-B) /
    (length(A-B) * length(C-B))
  )
```

각도 계산 전 코사인 값을 `[-1, 1]`로 제한하고, 길이가 거의 0인 벡터는 유효하지 않은 특징으로 처리한다.

검지·중지·약지·소지는 다음 특징을 조합한다.

- MCP-PIP-DIP 관절 각도
- PIP-DIP-TIP 관절 각도
- 손가락 끝에서 MCP까지의 거리
- 손가락 끝과 손목 사이 거리
- 손바닥 크기로 정규화한 손가락 길이

손바닥 크기는 손목과 중지 MCP 거리 또는 검지 MCP와 소지 MCP 폭을 기준으로 계산한다. 하나의 좌표축 방향이 아니라 거리와 각도를 사용해 회전에 대한 민감도를 낮춘다.

엄지는 다음 특징을 보조적으로 사용한다.

- CMC-MCP-IP 및 MCP-IP-TIP 각도
- 엄지 TIP과 검지 MCP 사이의 정규화 거리
- handedness를 고려한 손바닥 평면 내 벌어짐 방향

초기 가위바위보 결과는 엄지 하나 때문에 배제하지 않는다. 엄지 상태는 `fingerStates`에 포함하고 신뢰도 보정에만 제한적으로 사용한다.

### 손가락 상태 모델

```text
FingerStates:
  thumb: bool
  index: bool
  middle: bool
  ring: bool
  pinky: bool
```

각 손가락에는 내부적으로 `extended_score`를 계산하고 임계값을 넘을 때 `true`로 변환할 수 있다. 임계값 주변의 애매한 손가락이 많으면 최종 제스처를 `UNKNOWN`으로 낮춘다.

### 기본 분류 규칙

| 결과 | 검지 | 중지 | 약지 | 소지 | 엄지 |
|---|---:|---:|---:|---:|---|
| `ROCK` | 접힘 | 접힘 | 접힘 | 접힘 | 판정 제외 |
| `SCISSORS` | 펼침 | 펼침 | 접힘 | 접힘 | 판정 제외 |
| `PAPER` | 펼침 | 펼침 | 펼침 | 펼침 | 판정 제외 |
| `UNKNOWN` | 위 패턴 이외 또는 특징 품질 부족 |  |  |  |  |
| `NO_HAND` | 손 미검출 |  |  |  |  |

가위에서 검지와 중지가 서로 벌어져 있는지는 보조 특징으로 사용할 수 있으나, 카메라 방향에 따라 거리 변화가 크면 필수 조건으로 두지 않는다.

### 원시 판정 결과

```text
RawGestureResult:
  hand_detected: bool
  handedness: Left | Right | Unknown
  gesture: ROCK | SCISSORS | PAPER | UNKNOWN | NO_HAND
  confidence: float
  finger_states: FingerStates
  frame_timestamp_ms: int
```

`NO_HAND`일 때 모든 손가락은 `false`, 신뢰도는 `0.0`으로 통일한다. `UNKNOWN`은 손이 있으므로 `hand_detected=true`를 유지한다.

### 신뢰도 계산

최종 신뢰도는 MediaPipe 검출 신뢰도를 그대로 복사하지 않고 다음 요소를 조합한다.

- 랜드마크 검출 신뢰도
- 각 손가락의 펼침/접힘 점수가 임계값에서 떨어진 정도
- 제스처 규칙과의 일치도
- 랜드마크 품질과 손 크기
- 안정화 창에서 동일 결과가 차지하는 비율

초기 구현은 단순 가중 평균으로 시작하고, 실제 샘플로 보정한다. 값은 `0.0~1.0`으로 제한한다. 신뢰도 계산법과 임계값은 설정 및 테스트에서 재현 가능해야 한다.

### 시간 안정화

최근 5~10개 원시 판정 결과를 고정 길이 버퍼에 보관한다.

권장 초기 정책:

1. 유효한 최근 결과 중 가장 많은 제스처를 후보로 선택한다.
2. 후보가 최소 표 수와 최소 비율을 만족할 때만 확정한다.
3. 만족하지 않으면 직전 확정 결과를 짧게 유지하거나 `UNKNOWN`을 낸다.
4. 새 제스처가 충분한 표를 얻으면 즉시 상태 변경을 확정한다.
5. `NO_HAND`는 1프레임 누락으로 전환하지 않고 연속 누락 기준을 적용한다.
6. 손이 다시 나타나면 오래된 다른 손의 버퍼를 초기화한다.

실시간성을 유지하기 위해 지나치게 큰 창을 사용하지 않는다. 안정화 지연은 프레임 수와 실제 처리 FPS를 함께 고려한다.

### 테스트 데이터

- 주먹, 가위, 보를 나타내는 정규화 랜드마크 fixture
- 좌우 손 fixture
- 회전, 이동, 크기 변경을 적용한 파생 fixture
- 애매한 자세와 일부 접힌 손가락 fixture
- 손 없음 시퀀스
- `ROCK -> UNKNOWN -> PAPER`와 같은 전환 시퀀스

실제 카메라에서 얻은 좌표를 테스트 자산으로 보관할 경우 영상이나 개인 식별 데이터 없이 수치 랜드마크만 저장하는 것을 우선한다.

### 완료 판단

- 기본 fixture에서 세 제스처와 두 예외 상태가 구분된다.
- 동일 자세의 이동·크기 변화가 결과를 바꾸지 않는다.
- 일반적인 화면 내 회전에서 정확도가 유지된다.
- 1~2프레임의 오검출이 확정 상태를 불필요하게 바꾸지 않는다.
- 실제 좌우 손과 손바닥/손등 조건의 수동 테스트 결과가 기록된다.

## 입출력

### 입력

[`02-python-hand-tracking.md`](./02-python-hand-tracking.md)의 `HandTrackingResult`:

- `hand_detected`
- 21개 `x`, `y`, `z` 랜드마크
- `handedness`
- 검출 신뢰도
- 프레임 타임스탬프

### 출력

안정화된 `GestureResult`:

```text
GestureResult(
  hand_detected=True,
  handedness="Right",
  gesture="SCISSORS",
  confidence=0.94,
  finger_states={
    thumb: true,
    index: true,
    middle: true,
    ring: false,
    pinky: false
  },
  frame_timestamp_ms=...
)
```

## 주의사항

- 단순한 `TIP.y < PIP.y` 규칙만으로 판정하지 않는다. 손 회전과 손등 방향에서 쉽게 깨진다.
- 3차원 `z` 값은 실제 미터 단위가 아니므로 절대 깊이 기준으로 사용하지 않는다.
- handedness가 `Unknown`이어도 네 손가락 기반 제스처 판정은 가능해야 한다.
- `UNKNOWN`과 `NO_HAND`의 의미를 섞지 않는다.
- 안정화는 정확도를 새로 만드는 단계가 아니라 일시적인 흔들림을 억제하는 단계다. 지속적인 오분류는 특징과 임계값을 수정해야 한다.
- 임계값을 실제 샘플로 변경할 때 테스트 fixture와 기대 결과를 함께 갱신한다.

## 다음 단계와의 연결

안정화된 `GestureResult`는 [`04-websocket-protocol-and-python-publisher.md`](./04-websocket-protocol-and-python-publisher.md)의 메시지 빌더 입력이 된다. 다음 단계에서는 이 내부 모델을 스키마 버전 1 JSON으로 변환하고, 순서 번호와 송신 시각을 관리해 Unity에 제한된 빈도로 게시한다.
