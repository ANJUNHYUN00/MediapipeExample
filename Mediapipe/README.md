# Triage Trace Python 애플리케이션

이 디렉터리는 웹캠 프레임을 MediaPipe Pose Landmarker로 처리하고, 오른쪽 어깨·팔꿈치·손목에서 모의 UI 포인터 상태를 계산해 Unity에 게시하는 **데이터 생산자**다.

Triage Trace는 교육·시연용 시뮬레이션이며 실제 환자 평가, 응급도 분류, 진단, 치료 또는 의료 자문을 수행하지 않는다. Pose 정보는 Unity의 가상 인터페이스 입력에만 사용한다.

## 현재 상태

- Python 3.11.9 가상환경과 OpenCV, MediaPipe, websockets, pytest 설치 완료
- 기존 Hand Landmarker 모델 초기화 검증 완료
- Unity 프로젝트 환경 구성 완료
- gesture v1 계약과 fixture 완료
- Pose v2 문서와 fixture 완료
- Pose Landmarker Lite 모델 설치와 Python 단독 VIDEO 모드 추적 구현 완료
- 오른쪽 어깨(12), 팔꿈치(14), 손목(16) 추출과 `TRACKING`·`PARTIAL`·`LOST` 처리 완료
- 콘솔 좌표 로그와 OpenCV 디버그 오버레이 구현 완료
- 어깨→손목 방향, 팔꿈치 각도와 visibility 기반 pointing 판정 완료
- 관절 EMA와 안전한 활성화 디바운스, 정규화 pointer clamp 완료
- pose v2 명시적 직렬화와 최신 상태 우선 WebSocket publisher 완료
- 연결·종료·재연결·전송 순서와 실제 WebSocket 송수신 테스트 완료

Task 09까지 완료했으며 다음 권장 작업은 Unity의 비임상 AR 포인터·가상
시나리오 표시를 별도 Task로 설계하는 것이다.

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
    pose_landmarker_lite.task  # 활성 Pose MVP 자산
    README.md
  src/
    mediapipe_rps/             # 기존 패키지 경로 보존
      app.py
      config.py
      camera.py
      models.py
      pose_models.py
      pose_tracker.py
      pose_debug.py
      pointing.py
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

## Pointing 판정

기본값:

| 설정 | 값 | 의미 |
|---|---:|---|
| 최소 visibility | `0.5` | 세 관절이 모두 충족해야 함 |
| 최소 팔꿈치 각도 | `150°` | 팔이 충분히 펴졌는지 검사 |
| 최소 상완·전완 길이 | `0.05` | 정규화 화면의 퇴화 구간 거부 |
| 최소 어깨–손목 길이 | `0.10` | 짧은 방향 벡터 거부 |
| 구간 길이 비율 | `0.25~4.0` | 과도하게 불균형한 관절 배치 거부 |
| pointer 연장 | `0.25` | 손목에서 어깨→손목 벡터 연장 |
| EMA alpha | `0.35` | 관절 좌표 흔들림 완화 |
| 활성화 프레임 | `2` | 순간적인 false positive 억제 |

팔꿈치 각도와 길이는 프레임 종횡비를 반영해 계산한다. pointer 후보는
`[0.0, 1.0]`로 clamp하며 이미지 y축은 Python에서 뒤집지 않는다.
`PARTIAL`, `LOST` 또는 기하 검증 실패에서는 이전 pointer를 유지하지 않고
즉시 `pointing=false`, `pointer=null`로 전환한다.

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

핵심 버전은 `pyproject.toml`에 고정했다. Pose Landmarker Lite 모델은
MediaPipe 0.10.35의 VIDEO 모드에서 초기화와 합성 프레임 추론을 검증했다.

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

자동 검증은 환경, 모델 자산 무결성, 카메라 수명 주기, Pose 결과 변환,
pointing 기하와 시간 안정화, 디버그 출력, pose v2 직렬화와 실제 WebSocket
수명 주기를 포함한다. 최신 검증 결과는
[`Tasks/09-pose-v2-publisher-and-unity-receiver.md`](../Tasks/09-pose-v2-publisher-and-unity-receiver.md)를
기준으로 한다.

## 모델 자산

- `models/hand_landmarker.task`: 기존 gesture v1 실습 자산, 삭제하지 않음
- `models/pose_landmarker_lite.task`: 활성 Pose MVP 자산

Pose 모델의 공식 출처, 라이선스, 크기와 SHA-256은
[`models/README.md`](./models/README.md)에 기록했다. Hand 모델을 Pose API에
재사용하지 않는다.

## Python 단독 실행

PowerShell:

```powershell
Set-Location Mediapipe
.\.venv\Scripts\python.exe -m mediapipe_rps.app
```

기본 카메라가 0번이 아니면 `--camera-index 1`처럼 지정한다. 미리보기 없이
좌표 로그만 확인하려면 다음과 같이 실행한다.

```powershell
.\.venv\Scripts\python.exe -m mediapipe_rps.app --no-preview
```

기본 실행은 `ws://127.0.0.1:8765`에서 pose v2를 최대 15Hz로 게시한다.
포트나 전송률은 `--websocket-port`, `--publish-hz`로 변경하고 Python 단독
Pose 진단만 필요하면 `--no-websocket`을 사용한다.

주요 옵션은 `--width`, `--height`, `--model`,
`--visibility-threshold`, `--min-elbow-angle`, `--pointer-extension`,
`--smoothing-alpha`, `--activation-frames`, `--log-interval`,
`--max-frames`, `--no-mirror`, `--websocket-host`,
`--websocket-port`, `--publish-hz`, `--no-websocket`이다. `q` 또는 `Esc`로
종료하며 콘솔 전용 모드에서는 `Ctrl+C`로 종료한다. 유한 실행 smoke test는
`--no-preview --max-frames 30`으로 수행할 수 있다.

추론에는 원본 비미러 프레임을 사용하므로 12·14·16은 사람의 해부학적
오른쪽을 뜻한다. 수평 미러링은 사용자 친화적인 디버그 미리보기에만 적용한다.
카메라 프레임은 저장하거나 네트워크로 전송하지 않는다.

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

Task 09에서 pose v2 게시와 Unity 수신 기반을 연결했다. Unity를 먼저 실행한
경우 수신기는 제한된 간격으로 재연결하며, 잘못된 단일 메시지는 연결 루프를
종료하지 않는다.

## 개인정보와 안전

- 카메라 영상은 로컬 메모리에서만 처리한다.
- 영상 저장과 외부 전송은 기본 기능에 포함하지 않는다.
- WebSocket에는 세 관절 좌표와 UI 포인터 상태만 보낸다.
- 실제 사람의 의료 상태를 추론하거나 기록하지 않는다.
- 샘플과 fixture는 합성 수치 데이터를 우선한다.

## 알려진 위험

- 오른팔 가림과 화면 밖 관절에서 `PARTIAL` 전환이 잦을 수 있다.
- 2D 포인터는 카메라 위치와 원근에 민감하다.
- 기본 `150°`, visibility `0.5` 임계값은 대상 카메라의 거리·화각에 맞춰
  수동 조정이 필요할 수 있다.
- 실제 검증 당시 손목이 화면 밖에 있어 `pointing=true` 장시간 실측은
  전체 오른팔 구도에서 추가로 확인해야 한다.
- Lite 모델은 Full·Heavy보다 빠르지만 실제 조명·거리·가림 조건의 정확도는
  대상 장치에서 추가 측정해야 한다.
- 기존 패키지명과 Hand 모델이 남아 있어 활성 경로를 혼동할 수 있으므로 문서와 모듈 이름으로 분리한다.
