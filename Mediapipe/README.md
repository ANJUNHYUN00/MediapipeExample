# MediaPipe Python 애플리케이션

이 디렉터리는 웹캠 프레임을 처리해 손 랜드마크와 가위바위보 제스처를 계산하고, 결과를 WebSocket으로 게시하는 **데이터 생산자**를 구현한다. Unity 화면이나 Unity 수명 주기를 참조하지 않는다.

현재 상태는 프로젝트 골격만 준비된 단계다. 기능 구현과 외부 패키지 설치는 아직 수행하지 않았다.

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

## 예정 실행 환경

- Python 3.11
- OpenCV
- MediaPipe
- `websockets`
- `pytest`

정확한 의존성 버전과 설치 명령은 Task 03에서 실제 호환성을 확인한 뒤 `pyproject.toml`과 이 문서에 기록한다. 현재는 외부 패키지를 설치하지 않는다.

## 데이터 및 개인정보 원칙

- 카메라 영상은 로컬에서만 처리한다.
- 영상 프레임은 Unity로 전송하지 않는다.
- 영상 저장은 기본 기능에 포함하지 않는다.
- Unity에는 판정 결과와 필요한 손 메타데이터만 JSON으로 전송한다.

## 다음 작업

[`Tasks/02-message-spec-and-run-order.md`](../Tasks/02-message-spec-and-run-order.md)에서 Python 내부 모델과 Unity DTO가 공유할 버전 1 JSON 계약 및 실행 순서를 고정한다.
