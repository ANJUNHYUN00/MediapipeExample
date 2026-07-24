# 01. 아키텍처 및 개발 환경 설계

> Triage Trace 전환 안내 (2026-07-24): 이 문서의 Python/Unity 환경과 책임 분리는 완료된 재사용 기반이다. 활성 Pose 아키텍처는 [`07-triage-trace-architecture-and-pose-input.md`](./07-triage-trace-architecture-and-pose-input.md)를 따른다.

## 목적

MediaPipe 손 인식 프로세스와 Unity 표시 앱의 책임 경계를 정하고, 이후 구현 단계가 동일한 실행 환경과 데이터 흐름을 기준으로 진행되도록 한다. 개발자가 MediaPipe와 Unity를 독립적으로 실행·검증한 뒤 로컬 WebSocket으로 결합할 수 있는 프로젝트 기반을 마련한다.

## 구현 범위

- 전체 시스템 구성과 컴포넌트 책임 정의
- `Mediapipe/` Python 프로젝트 기본 구조 설계
- `MediapipeUnity/` Unity 프로젝트 기본 구조 설계
- Python 가상 환경, 의존성, 설정 및 실행 방식 정의
- Unity 버전과 패키지 선택 원칙 정의
- 로깅, 설정, 테스트, 개인정보 처리 원칙 정의
- 개발 단계별 독립 실행과 통합 지점 정의

다음 항목은 이 단계에서 구현하지 않는다.

- 실제 MediaPipe 랜드마크 추출
- 가위바위보 판정 알고리즘
- WebSocket 송수신 코드
- Unity 결과 화면의 세부 구현

## 설계 내용

### 전체 구조

```text
[Webcam]
    |
    v
[Camera Capture]
    |
    v
[MediaPipe Hand Tracker]
    |
    v
[Gesture Classifier + Stabilizer]
    |
    v
[Message Builder]
    |
    v
[WebSocket Server: ws://127.0.0.1:8765]
    |
    v
[Unity WebSocket Client]
    |
    v
[Thread-safe Message Queue]
    |
    v
[Unity Presenter / UI]
```

MediaPipe 프로세스가 데이터 생산자이자 WebSocket 서버이고, Unity가 소비자이자 클라이언트다. 영상 프레임 자체는 Unity로 보내지 않으며 손 판정 결과와 메타데이터만 JSON으로 전송한다.

### 컴포넌트 책임

| 컴포넌트 | 책임 | 금지되는 결합 |
|---|---|---|
| Camera Capture | 카메라 열기, 프레임 읽기, 자원 해제 | 제스처 규칙 포함 |
| Hand Tracker | 프레임을 21개 랜드마크와 handedness로 변환 | WebSocket 직접 호출 |
| Gesture Classifier | 랜드마크에서 손가락 상태와 제스처 계산 | 카메라 또는 Unity 상태 참조 |
| Stabilizer | 최근 프레임 결과를 시간적으로 안정화 | UI 문자열 생성 |
| Message Builder | 내부 결과를 버전 1 JSON 모델로 변환 | 랜드마크 재판정 |
| WebSocket Server | 클라이언트 연결과 메시지 전송 | 제스처 판정 |
| Unity Receiver | 연결, 재연결, 수신, 파싱 | 백그라운드 스레드에서 UI 변경 |
| Unity Presenter | 수신 상태를 한글 UI와 시각 요소로 표현 | 네트워크 세부 구현 |

### 권장 Python 프로젝트 구조

```text
Mediapipe/
  pyproject.toml
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
    fixtures/
    test_gesture_classifier.py
    test_stabilizer.py
    test_message_builder.py
    test_websocket_server.py
```

패키지 이름과 실제 파일 구성은 구현 중 조정할 수 있으나 책임 분리는 유지한다. 애플리케이션 진입점은 설정 로드, 컴포넌트 조립, 실행 및 정상 종료만 담당한다.

### 권장 Unity 프로젝트 구조

```text
MediapipeUnity/
  Assets/
    Scenes/
    Scripts/
      Networking/
      Models/
      Presentation/
      Configuration/
    Prefabs/
    UI/
    Tests/
      EditMode/
      PlayMode/
  Packages/
  ProjectSettings/
```

네트워크 수신 모델과 UI 표현 모델을 분리한다. Unity의 `.meta` 파일은 대응 자산과 함께 버전 관리하며, `Library/`, `Temp/`, `Logs/`, `obj/` 등 생성 산출물은 제외한다.

### 개발 환경

- Python 3.11.9 가상 환경을 사용한다.
- 검증된 핵심 의존성은 OpenCV contrib 5.0.0.93, MediaPipe 0.10.35, `websockets` 16.1.1, `pytest` 8.4.2다.
- 핵심 의존성 버전은 `Mediapipe/pyproject.toml`에 고정한다.
- Unity Editor 6000.3.10f1을 프로젝트 버전으로 사용한다.
- Unity용 WebSocket 라이브러리는 데스크톱 빌드, 비동기 수신, 종료 취소, 오류 콜백을 지원해야 한다.
- Python과 Unity 모두 UTF-8 JSON을 사용한다.

Unity용 WebSocket 패키지는 후속 수신기 구현 전 실제 데스크톱 호환성을 확인한 뒤 확정한다.

### 설정 관리

최소 설정 항목은 다음과 같다.

| 설정 | 기본값 | 소유 컴포넌트 |
|---|---:|---|
| 카메라 인덱스 | `0` | Python Camera Capture |
| 프레임 너비/높이 | 환경 기본값 | Python Camera Capture |
| 서버 호스트 | `127.0.0.1` | Python WebSocket Server |
| 서버 포트 | `8765` | Python/Unity 공통 |
| 최대 송신 빈도 | 10~20 Hz | Python Publisher |
| 검출 신뢰도 임계값 | 구현 시 조정 | Hand Tracker |
| 안정화 창 크기 | 5~10 프레임 | Stabilizer |
| 재연결 간격 | 구현 시 조정 | Unity Receiver |

비밀 정보는 초기 범위에 없으며, 로컬 주소를 기본값으로 사용한다. 포트와 임계값은 코드 여러 곳에 중복 하드코딩하지 않는다.

### 실행 수명 주기

1. Python이 설정과 로거를 초기화한다.
2. 카메라를 열고 실패하면 원인을 출력한 뒤 정상적으로 종료한다.
3. WebSocket 서버를 시작한다.
4. 프레임 처리 루프가 랜드마크, 판정, 안정화, 메시지 생성을 수행한다.
5. Unity가 서버에 연결하고 수신 루프를 시작한다.
6. 종료 신호가 오면 카메라, 비동기 작업, 소켓을 순서대로 정리한다.

Unity가 먼저 실행된 경우에는 재연결 상태를 표시하며 서버가 준비될 때까지 제한된 간격으로 다시 연결한다.

### 로깅과 개인정보

- 카메라 열기/닫기, 서버 시작/종료, 클라이언트 연결/해제, 제스처 상태 변경, 예외를 기록한다.
- 정상 프레임마다 상세 로그를 남겨 성능을 저하시키지 않는다.
- 영상은 기본적으로 저장하지 않고 로컬 처리한다.
- 외부 인터페이스 기본 바인딩을 피하고 `127.0.0.1`만 사용한다.

## 입출력

### 입력

- 프로젝트 요구사항과 `docs/project-plan.md`
- 웹캠 장치
- Python 3.11 실행 환경
- Unity LTS 에디터
- 로컬 TCP 포트 `8765`

### 출력

- 책임이 분리된 Python 및 Unity 프로젝트 골격
- 공통 설정 기준
- `Webcam -> MediaPipe -> WebSocket -> Unity UI` 데이터 흐름
- 이후 단계가 구현할 모듈 경계와 인터페이스

## 주의사항

- MediaPipe Python API는 설치 버전에 따라 사용 방식이 달라질 수 있으므로 선택한 API와 모델 자산을 실행 문서에 고정한다.
- Unity 패키지 선택 전 대상 에디터와 데스크톱 빌드에서 정상 동작하는지 작은 연결 테스트로 확인한다.
- 카메라와 포트는 다른 프로세스가 점유할 수 있으므로 진단 가능한 오류 메시지를 제공한다.
- Python 처리 루프와 WebSocket 비동기 루프 사이에 무제한 큐를 두지 않는다. 오래된 실시간 상태보다 최신 상태를 우선한다.
- 생성 폴더나 패키지 구조가 바뀌면 `AGENTS.md`와 관련 계획 문서도 갱신한다.

## 다음 단계와의 연결

이 단계에서 정한 Python 모듈 경계와 카메라 설정을 바탕으로 [`02-python-hand-tracking.md`](./02-python-hand-tracking.md)에서 웹캠 프레임을 MediaPipe의 21개 정규화 랜드마크, handedness 및 검출 신뢰도로 변환한다. 해당 출력 모델은 제스처 분류기의 직접 입력이 된다.
