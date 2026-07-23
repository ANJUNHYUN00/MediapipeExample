# MediaPipe Python 애플리케이션

이 디렉터리는 웹캠 프레임을 처리해 손 랜드마크와 가위바위보 제스처를 계산하고, 결과를 WebSocket으로 게시하는 **데이터 생산자**를 구현한다. Unity 화면이나 Unity 수명 주기를 참조하지 않는다.

현재 Python 3.11 가상환경과 손 추적용 런타임 의존성, 공식 Hand Landmarker 모델 자산까지 준비됐다. 카메라·추적·제스처·WebSocket 기능 구현은 아직 수행하지 않았다.

## 책임 범위

Python 애플리케이션이 담당하는 범위:

- 웹캠 열기, 프레임 읽기와 자원 해제
- MediaPipe를 이용한 한 손의 21개 랜드마크 검출
- 랜드마크 기반 손가락 상태와 가위바위보 분류
- 프레임 간 결과 안정화
- 내부 결과를 `hand_gesture` 버전 1 메시지로 변환
- `127.0.0.1:8765` WebSocket 서버와 비동기 송신
- Python 단위·통합 테스트와 진단 로그

Python 애플리케이션이 담당하지 않는 범위:

- Unity Scene, GameObject 또는 UI 제어
- 수신 결과의 한글 표시
- Unity 재연결 화면과 메인 스레드 처리
- 카메라 영상의 외부 전송 또는 기본 저장

## 프로젝트 구조

```text
Mediapipe/
  README.md
  pyproject.toml
  models/
    hand_landmarker.task
    README.md
  src/
    mediapipe_rps/
      __init__.py
      app.py
      config.py
      camera.py
      hand_tracker.py
      models.py
      gesture_classifier.py
      stabilizer.py
      message_builder.py
      websocket_server.py
  tests/
    test_environment.py
    fixtures/
      README.md
```

## 모듈 책임과 의존 제한

| 모듈 | 책임 | 직접 참조하지 않을 대상 |
|---|---|---|
| `app.py` | 설정 로드, 객체 조립, 실행과 정상 종료 | 세부 검출·판정 알고리즘 |
| `config.py` | 카메라, MediaPipe, 안정화, WebSocket 설정의 단일 소유 | 실행 루프와 UI |
| `camera.py` | 카메라 열기, 프레임 읽기, 해제 | 제스처 판정과 WebSocket |
| `hand_tracker.py` | 프레임을 21개 랜드마크와 handedness로 변환 | Unity UI와 WebSocket |
| `models.py` | MediaPipe 객체와 독립적인 내부 데이터 모델 | 카메라 및 네트워크 구현 |
| `gesture_classifier.py` | 단일 프레임 손가락 상태와 제스처 분류 | 카메라와 네트워크 |
| `stabilizer.py` | 최근 프레임 결과의 시간적 안정화 | MediaPipe API와 UI |
| `message_builder.py` | 안정화 결과를 버전 1 메시지 모델로 변환 | 랜드마크 재분류 |
| `websocket_server.py` | 연결 관리와 비동기 메시지 송신 | 카메라와 제스처 계산 |

## 의존 방향

```text
app
 ├─> config
 ├─> camera ─> hand_tracker ─> gesture_classifier ─> stabilizer
 └─> message_builder ─> websocket_server
```

- `app.py`는 구성 요소를 조립하지만 알고리즘을 소유하지 않는다.
- 처리 단계는 뒤 단계의 결과나 Unity 상태를 역참조하지 않는다.
- 카메라 루프는 WebSocket 송신 완료를 기다리지 않는다.
- MediaPipe 라이브러리 객체는 `hand_tracker.py` 경계 밖으로 노출하지 않는다.
- `models.py`의 내부 모델이 처리 단계 사이의 계약이 된다.

## 확정 개발 환경

2026-07-23 Windows 11 x64 환경에서 검증한 조합:

| 구성 요소 | 버전 |
|---|---|
| Python | 3.11.9 |
| pip | 26.1.2 |
| MediaPipe | 0.10.35 |
| OpenCV contrib | 5.0.0.93 (`cv2.__version__ == 5.0.0`) |
| websockets | 16.1.1 |
| pytest | 8.4.2 |

핵심 버전은 `pyproject.toml`에 고정했다. pytest cache provider는 관리형 샌드박스에서 파일 잠금 지연을 일으켜 프로젝트 설정에서 비활성화했다.

## 환경 재현

PowerShell에서:

```powershell
Set-Location Mediapipe
$python311 = "$env:LOCALAPPDATA\Programs\Python\Python311\python.exe"
& $python311 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -e ".[dev]"
```

Python Launcher가 설치된 인터프리터를 정상 인식하는 환경에서는 첫 두 줄 대신 다음 명령을 사용할 수 있다.

```powershell
py -3.11 -m venv .venv
```

현재 개발 PC에서는 Python 3.11.9가 설치돼 있지만 기존 `py` Launcher가 이를 열거하지 못해 `%LOCALAPPDATA%` 아래 인터프리터를 직접 사용했다.

## 환경 검증

```powershell
.\.venv\Scripts\python.exe -c "import cv2; import mediapipe; import websockets; from mediapipe.tasks.python import vision; print(cv2.__version__, mediapipe.__version__, websockets.__version__, hasattr(vision, 'HandLandmarker'))"
.\.venv\Scripts\python.exe -m pytest
.\.venv\Scripts\python.exe -m pip check
```

검증 결과:

- OpenCV, MediaPipe, websockets import 성공
- `vision.HandLandmarker` API 존재
- 환경 smoke test `2 passed`
- `pip check`: `No broken requirements found`

## Hand Landmarker 모델

- 경로: `models/hand_landmarker.task`
- 모델: 공식 HandLandmarker full float16 최신 모델 번들
- 파일 크기: 7,819,105 bytes
- SHA-256: `FBC2A30080C3C557093B5DDFC334698132EB341044CCEE322CCF8BCF3607CDE1`
- 라이선스: Apache License 2.0
- 구현 예정 모드: `VIDEO`
- 최대 손 수: `1`

모델 자산의 공식 출처와 모델 카드 링크는 [`models/README.md`](./models/README.md)에 기록했다. 설치 검증에서 이 파일로 `HandLandmarker`를 VIDEO 모드로 생성하고 정상 종료했다.

## 알려진 환경 이슈

- 이 개발 PC의 기존 `py` Launcher는 설치된 Python 3.11.9를 열거하지 못한다. 가상환경 재생성 시 `%LOCALAPPDATA%\Programs\Python\Python311\python.exe`를 사용한다.
- 관리형 샌드박스에서는 pytest의 `.pytest_cache` 파일 잠금이 종료를 지연시켜 cache provider를 비활성화했다. 테스트 결과에는 영향을 주지 않는다.
- 외부 통신이 제한된 환경에서 Hand Landmarker 종료 시 `portable_clearcut_uploader` 연결 실패 로그가 나타날 수 있다. 모델 생성·종료의 프로세스 종료 코드는 `0`이며 추론 모델 초기화에는 영향을 주지 않았다.

## 데이터 및 개인정보 원칙

- 카메라 영상은 로컬에서만 처리한다.
- 영상 프레임은 Unity로 전송하지 않는다.
- 영상 저장은 기본 기능에 포함하지 않는다.
- Unity에는 판정 결과와 필요한 손 메타데이터만 JSON으로 전송한다.

## WebSocket 메시지 계약

### 연결 기준

- 서버: Python MediaPipe 애플리케이션
- 클라이언트: Unity 애플리케이션
- URI: `ws://127.0.0.1:8765`
- 프레임: UTF-8 JSON 텍스트
- 방향: Python에서 Unity로 상태를 푸시하는 단방향 구조
- 메시지 종류: `hand_gesture`
- 스키마 버전: `1`
- 최대 전송 빈도: 설정 가능하며 기본 목표는 10~20Hz 이하

### 버전 1 메시지 예시

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

### 필드 규격

| 필드 | JSON 형식 | 필수 | 유효성 |
|---|---|---:|---|
| `type` | string | 예 | 항상 `hand_gesture` |
| `version` | integer | 예 | `1` |
| `timestamp` | integer | 예 | 프레임 캡처 기준 Unix epoch 밀리초 |
| `sequence` | integer | 예 | 프로세스 수명 동안 실제 전송마다 증가 |
| `handDetected` | boolean | 예 | 손 검출 여부 |
| `handedness` | string | 예 | `Left`, `Right`, `Unknown` |
| `gesture` | string | 예 | 정의된 다섯 상태 중 하나 |
| `confidence` | number | 예 | `0.0` 이상 `1.0` 이하 |
| `fingerStates` | object | 예 | `thumb`, `index`, `middle`, `ring`, `pinky` boolean 포함 |

`timestamp`와 `sequence`는 Unity에서 64비트 정수로 처리한다.

### 제스처 및 예외 상태

| 값 | 의미 | 필수 조건 |
|---|---|---|
| `ROCK` | 주먹 | 손 검출, 검지·중지·약지·소지 접힘 |
| `SCISSORS` | 가위 | 손 검출, 검지·중지 펼침, 약지·소지 접힘 |
| `PAPER` | 보 | 손 검출, 검지·중지·약지·소지 펼침 |
| `UNKNOWN` | 손은 있으나 지원 자세로 확정할 수 없음 | `handDetected=true`, 실제 손가락 상태 유지 |
| `NO_HAND` | 손이 검출되지 않음 | `handDetected=false`, `handedness="Unknown"`, `confidence=0.0`, 모든 손가락 `false` |

엄지 상태는 메시지에 포함하지만 `ROCK`, `SCISSORS`, `PAPER`의 필수 분류 조건으로 사용하지 않는다.

### 언어별 이름 매핑

| 의미 | Python 내부 | JSON | Unity DTO |
|---|---|---|---|
| 손 검출 | `hand_detected` | `handDetected` | `handDetected` |
| 좌우 손 | `handedness` | `handedness` | `handedness` |
| 제스처 | `gesture` | `gesture` | `gesture` |
| 신뢰도 | `confidence` | `confidence` | `confidence` |
| 손가락 상태 | `finger_states` | `fingerStates` | `fingerStates` |

Python enum의 기본 문자열 표현에 의존하지 않고 계약 문자열을 명시적으로 직렬화한다.

### Python 송신 전 검증

- `NaN`, 무한대와 `0.0~1.0` 밖의 confidence를 거부한다.
- 알 수 없는 handedness와 gesture를 전송하지 않는다.
- 필수 필드와 다섯 손가락 boolean이 모두 있어야 한다.
- `NO_HAND` 불변 조건과 세 제스처의 손가락 조건을 검사한다.
- Python 내부 snake_case는 JSON 경계에서 정확한 camelCase로 변환한다.

계약 fixture는 `tests/fixtures/messages/`에 있다.

## 실행 순서

### 표준 실행

1. Python 가상 환경을 활성화한다.
2. Python MediaPipe 앱을 실행한다.
3. 카메라 열기와 `127.0.0.1:8765` 서버 시작 로그를 확인한다.
4. Unity 프로젝트를 열고 Play Mode 또는 데스크톱 빌드를 실행한다.
5. Unity의 `연결됨` 상태와 메시지 수신을 확인한다.

### Unity를 먼저 실행하는 경우

1. Unity 앱을 실행한다.
2. `연결 중` 또는 `재연결 중` 상태를 확인한다.
3. Python MediaPipe 앱을 실행한다.
4. Unity Scene을 재시작하지 않고 자동 연결되는지 확인한다.

### 정상 종료

1. Unity Play Mode 또는 앱을 종료해 수신 작업과 소켓을 정리한다.
2. Python 앱에 종료 입력을 보내 프레임 루프를 멈춘다.
3. 카메라, OpenCV 창, WebSocket 서버와 포트가 해제됐는지 확인한다.

### Task 04~06 Python 단독 실행

- WebSocket 구현 전에는 Python 앱만 실행한다.
- 미리보기 창에서 `q` 또는 `Esc`로 종료한다.
- Unity 연결은 후속 WebSocket 구현 Task 이후에 검증한다.

## 다음 작업

[`Tasks/04-webcam-loop-and-preview.md`](../Tasks/04-webcam-loop-and-preview.md)에서 OpenCV 카메라 입력과 미리보기 루프를 구현한다.
