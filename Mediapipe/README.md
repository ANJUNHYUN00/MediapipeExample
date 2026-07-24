# Triage Trace Python 애플리케이션

이 디렉터리는 웹캠 프레임을 MediaPipe Pose Landmarker로 처리하고, 오른쪽 어깨·팔꿈치·손목에서 모의 UI 포인터 상태를 계산해 Unity에 게시하는 **데이터 생산자**다.

Triage Trace는 교육·시연용 시뮬레이션이며 실제 환자 평가, 응급도 분류, 진단, 치료 또는 의료 자문을 수행하지 않는다. Pose 정보는 Unity의 가상 인터페이스 입력에만 사용한다.

## 현재 상태

- Python 3.11.9 가상환경과 OpenCV, MediaPipe, websockets, pytest 설치 완료
- 기존 Hand Landmarker 모델 초기화 검증 완료
- Unity 프로젝트 환경 구성 완료
- gesture v1 계약과 fixture 완료
- Pose v2 문서와 fixture 완료
- Pose Landmarker 모델 선정·설치와 실제 추적 코드는 아직 미구현

다음 활성 작업은 [`Tasks/07-pose-landmarker-runtime.md`](../Tasks/07-pose-landmarker-runtime.md)다.

## 활성 책임

- 웹캠 열기, 프레임 읽기와 자원 해제
- MediaPipe Pose Landmarker VIDEO 모드 실행
- 오른쪽 어깨(12), 팔꿈치(14), 손목(16) 좌표와 visibility 추출
- `TRACKING`, `PARTIAL`, `LOST` 결정
- pointing 유효성 검사와 정규화 pointer 계산
- `pose_pointer` version 2 JSON 생성
- `127.0.0.1:8765` WebSocket 서버와 최신 상태 게시
- Python 단위·통합 테스트와 진단 로그

담당하지 않는 범위:

- Unity Scene, GameObject, AR UI 제어
- Pose 입력의 의료적 해석
- 환자 우선순위, 진단 또는 치료 추천
- 영상 프레임의 외부 전송이나 기본 저장

## 프로젝트 구조

```text
Mediapipe/
  README.md
  pyproject.toml
  models/
    hand_landmarker.task       # legacy v1 자산
    pose_landmarker.task       # Task 07에서 공식 모델 확정 후 추가 예정
    README.md
  src/
    mediapipe_rps/             # 기존 패키지 경로 보존
      app.py
      config.py
      camera.py
      models.py
      pose_models.py           # 예정
      pose_tracker.py          # 예정
      pointing.py              # 예정
      message_builder.py
      websocket_server.py
      hand_tracker.py          # legacy v1
      gesture_classifier.py    # legacy v1
      stabilizer.py            # legacy v1
  tests/
    test_environment.py
    fixtures/
      messages/
```

패키지명 `mediapipe_rps`는 기존 설치·import 경로 호환성을 위해 당분간 유지한다. Pose 전환 과정에서 무리하게 이름을 바꾸지 않고 별도 리팩터링 Task로 다룬다.

## 의존 방향

```text
app
 ├─> config
 ├─> camera ─> pose_tracker ─> pointing
 └─> message_builder ─> websocket_server
```

- MediaPipe 객체는 `pose_tracker.py` 경계 밖으로 노출하지 않는다.
- pointing 모듈은 카메라, 네트워크 또는 Unity 상태를 참조하지 않는다.
- 메시지 빌더는 Pose를 다시 추론하거나 포인터를 재계산하지 않는다.
- 카메라 루프는 WebSocket 송신 완료를 기다리지 않는다.
- 기존 Hand/RPS 모듈은 v1 호환 이력으로 보존하고 활성 Pose 흐름과 분리한다.

## MVP Pose 입력

| JSON 이름 | MediaPipe 인덱스 | 역할 |
|---|---:|---|
| `rightShoulder` | 12 | 상체 기준과 팔 길이 품질 |
| `rightElbow` | 14 | 포인팅 방향 시작 |
| `rightWrist` | 16 | 포인팅 방향 끝 |

각 관절은 이미지 정규화 `x`, `y`, 상대 `z`, `visibility`를 가진다. `visibility`는 관절 관측 품질이며 의료 신뢰도가 아니다.

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

핵심 버전은 `pyproject.toml`에 고정했다. Pose Landmarker API와 선택 모델의 실제 호환성은 Task 07에서 별도로 검증한다.

## 환경 재현

PowerShell:

```powershell
Set-Location Mediapipe
$python311 = "$env:LOCALAPPDATA\Programs\Python\Python311\python.exe"
& $python311 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -e ".[dev]"
```

Python Launcher가 3.11을 인식하면 다음 명령으로 가상환경을 만들 수 있다.

```powershell
py -3.11 -m venv .venv
```

## 환경 검증

```powershell
.\.venv\Scripts\python.exe -c "import cv2; import mediapipe; import websockets; from mediapipe.tasks.python import vision; print(cv2.__version__, mediapipe.__version__, websockets.__version__, hasattr(vision, 'PoseLandmarker'))"
.\.venv\Scripts\python.exe -m pytest
.\.venv\Scripts\python.exe -m pip check
```

기존 환경 검증 결과는 `2 passed`, `No broken requirements found`다. 기존 테스트는 설치 환경과 Hand Landmarker 자산을 확인하므로 Task 07에서 Pose 전용 smoke test를 추가해야 한다.

## 모델 자산

- `models/hand_landmarker.task`: 기존 gesture v1 실습 자산, 삭제하지 않음
- `models/pose_landmarker.task`: 아직 없음

Pose 모델은 Task 07에서 공식 MediaPipe 출처와 라이선스를 확인하고, 현재 MediaPipe 0.10.35에서 VIDEO 모드 초기화한 뒤 경로·크기·SHA-256을 기록한다. Hand 모델을 Pose API에 재사용하지 않는다.

## WebSocket 계약

공통 기준:

- 서버: Python
- 클라이언트: Unity
- URI: `ws://127.0.0.1:8765`
- UTF-8 JSON 텍스트
- 기본 전송 목표: 10~20Hz 이하

### 보존 계약

`type="hand_gesture"`, `version=1`은 기존 필드와 fixture를 변경하지 않고 보존한다.

### 활성 계약

`type="pose_pointer"`, `version=2`는 다음 필드를 사용한다.

- `tracking`
- `pointing`
- `pointer`
- `joints`
- `visibility`

정확한 형식과 불변 조건은 [`docs/websocket-protocols.md`](../docs/websocket-protocols.md)를 따른다.

## 실행 순서

최종 통합 단계:

1. Python 가상환경을 활성화하고 Triage Trace 서버를 실행한다.
2. Pose 모델과 카메라 초기화, `127.0.0.1:8765` 시작을 확인한다.
3. Unity 프로젝트를 열고 Play Mode 또는 데스크톱 빌드를 실행한다.
4. 연결 상태와 pose v2 수신을 확인한다.
5. 종료 시 Unity 수신기를 먼저 정리하고 Python 카메라·Pose·서버를 종료한다.

Task 07~08에서는 WebSocket 없이 Python만 실행해 Pose와 포인터 계산을 검증한다.

## 개인정보와 안전

- 카메라 영상은 로컬 메모리에서만 처리한다.
- 영상 저장과 외부 전송은 기본 기능에 포함하지 않는다.
- WebSocket에는 세 관절 좌표와 UI 포인터 상태만 보낸다.
- 실제 사람의 의료 상태를 추론하거나 기록하지 않는다.
- 샘플과 fixture는 합성 수치 데이터를 우선한다.

## 알려진 위험

- Pose 모델 자산과 MediaPipe 0.10.35의 조합은 아직 검증되지 않았다.
- 오른팔 가림과 화면 밖 관절에서 `PARTIAL` 전환이 잦을 수 있다.
- 2D 포인터는 카메라 위치와 원근에 민감하다.
- 기존 패키지명과 Hand 모델이 남아 있어 활성 경로를 혼동할 수 있으므로 문서와 모듈 이름으로 분리한다.
