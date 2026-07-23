# MediaPipe Unity 애플리케이션

이 디렉터리는 Python MediaPipe 프로세스가 게시한 `hand_gesture` 메시지를 받아 연결 상태와 가위바위보 결과를 표시하는 **데이터 소비자**를 구현한다.

## 현재 프로젝트 상태

`MediapipeUnity/`는 Unity Editor 6000.3.10f1로 생성하고 batch mode에서 초기 import와 컴파일을 검증한 유효한 Unity 프로젝트다.

- Unity Hub: 3.16.3
- Unity Editor: 6000.3.10f1 (`e35f0c77bd8e`)
- 프로젝트 버전 기준: `ProjectSettings/ProjectVersion.txt`
- 패키지 기준: `Packages/manifest.json`, `Packages/packages-lock.json`
- batch mode 결과: 종료 코드 `0`, C# 컴파일 오류 없음
- TextMeshPro: `com.unity.ugui`는 포함돼 있으나 UI 구현과 필수 리소스 import는 후속 Unity Task에서 검증
- WebSocket 패키지: 아직 선택하거나 설치하지 않음

프로젝트는 설치된 Editor가 직접 생성했으며 `ProjectVersion.txt`의 버전과 실제 Editor 버전이 일치한다.

## 책임 범위

Unity 애플리케이션이 담당하는 범위:

- `ws://127.0.0.1:8765` WebSocket 연결과 종료
- 연결 실패 또는 종료 후 제한된 자동 재연결
- `hand_gesture` 버전 1 JSON 역직렬화와 유효성 검사
- 백그라운드 수신 결과를 스레드 안전한 큐로 전달
- Unity 메인 스레드에서 연결 상태와 결과 UI 갱신
- `주먹`, `가위`, `보`, 미인식, 손 없음 표시
- EditMode 파싱 테스트와 PlayMode 수명 주기·UI 테스트

Unity 애플리케이션이 담당하지 않는 범위:

- 웹캠 열기와 영상 처리
- MediaPipe 랜드마크 검출
- 손가락 상태 및 제스처 재판정
- Python 프로세스 내부 설정과 자원 관리

## Unity 프로젝트 생성 후 구조

현재 책임별 구조:

```text
MediapipeUnity/
  Assets/
    Scenes/
    Scripts/
      Configuration/
      Models/
      Networking/
      Presentation/
    Prefabs/
    UI/
    Tests/
      EditMode/
      PlayMode/
  Packages/
  ProjectSettings/
```

Unity의 `.meta` 파일은 대응하는 자산과 함께 보존한다. `Library/`, `Temp/`, `Logs/`, `obj/`와 사용자별 IDE 산출물은 버전 관리하지 않는다.

## 폴더별 책임

| 폴더 | 책임 | 직접 담당하지 않을 대상 |
|---|---|---|
| `Scripts/Configuration` | WebSocket URI, 재연결 간격, 데이터 만료 설정 | 연결 루프와 UI |
| `Scripts/Models` | 버전 1 네트워크 DTO와 검증된 도메인 상태 | WebSocket 연결 |
| `Scripts/Networking` | 연결, 수신, 취소, 재연결, 최신 메시지 큐 | 결과 한글 표시 |
| `Scripts/Presentation` | 검증된 상태를 텍스트, 아이콘, 신뢰도로 표현 | WebSocket 라이브러리 직접 호출 |
| `Tests/EditMode` | JSON 파싱, 값 검증, 표시 상태 매핑 | 실제 네트워크 연결 |
| `Tests/PlayMode` | 메인 스레드 큐, UI, Scene과 객체 수명 주기 | Python 판정 알고리즘 |

## 의존 방향

```text
Networking ─> Models
Presentation ─> Models
Configuration ─> Networking

Presentation -X-> WebSocket 라이브러리 직접 호출
Models -X-> Unity UI 또는 네트워크 구현
```

- WebSocket 콜백에서 TextMeshPro, Image, GameObject를 직접 변경하지 않는다.
- 수신 데이터는 스레드 안전한 최신 메시지 큐를 거쳐 메인 스레드에서 적용한다.
- Unity는 Python의 제스처 결과를 다시 계산하지 않는다.
- 지원하지 않는 메시지는 UI에 적용하지 않되 앱 전체를 종료하지 않는다.

## WebSocket 메시지 계약

### 연결 기준

- 서버: Python MediaPipe 애플리케이션
- 클라이언트: Unity 애플리케이션
- URI: `ws://127.0.0.1:8765`
- 프레임: UTF-8 JSON 텍스트
- 메시지 종류와 버전: `hand_gesture`, 버전 `1`
- 최대 전송 빈도: Python 설정 기준 10~20Hz 이하

### Unity 버전 1 DTO

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

FingerStatesDto
  thumb: bool
  index: bool
  middle: bool
  ring: bool
  pinky: bool
```

JSON 필드명은 위 대소문자와 정확히 일치시킨다. 별도 명명 규칙을 쓰는 JSON 도구를 선택하면 직렬화 속성으로 명시 매핑한다.

### 필드 규격

| 필드 | JSON 형식 | 필수 | Unity 검증 |
|---|---|---:|---|
| `type` | string | 예 | `hand_gesture`인지 확인 |
| `version` | integer | 예 | `1`인지 확인 |
| `timestamp` | integer | 예 | C# `long`으로 처리 |
| `sequence` | integer | 예 | C# `long`, 마지막 적용 값보다 최신인지 확인 |
| `handDetected` | boolean | 예 | gesture 상태와 일치하는지 확인 |
| `handedness` | string | 예 | `Left`, `Right`, `Unknown` |
| `gesture` | string | 예 | 정의된 다섯 상태 중 하나 |
| `confidence` | number | 예 | `0.0~1.0` |
| `fingerStates` | object | 예 | 다섯 boolean 필드 존재 |

### 상태값과 화면 의미

| 값 | 상태 의미 | Unity 표시 |
|---|---|---|
| `ROCK` | 주먹 | `주먹` |
| `SCISSORS` | 가위 | `가위` |
| `PAPER` | 보 | `보` |
| `UNKNOWN` | 손은 검출됐지만 지원 자세로 확정하지 못함 | `손 모양을 확인해 주세요` |
| `NO_HAND` | 손이 검출되지 않음 | `손을 카메라에 보여 주세요` |

`NO_HAND`는 `handDetected=false`, `handedness="Unknown"`, `confidence=0.0`, 모든 손가락 `false`여야 한다. `UNKNOWN`은 `handDetected=true`이며 실제 손가락 상태를 유지한다. 엄지는 세 제스처의 필수 분류 조건이 아니다.

### 잘못된 메시지 처리

- 잘못된 JSON, 알 수 없는 `type`, 지원하지 않는 `version`과 enum은 UI에 적용하지 않는다.
- confidence 범위, 필수 객체와 상태 불변 조건을 검증한다.
- 알 수 없는 추가 필드는 무시해 버전 1의 호환 가능한 확장을 허용한다.
- 한 메시지 오류로 연결이나 앱 전체를 종료하지 않고 제한된 경고를 기록한다.
- 네트워크 콜백에서 Unity API를 직접 호출하지 않고 검증된 최신 메시지를 메인 스레드로 전달한다.

계약 fixture의 기준 위치는 `../Mediapipe/tests/fixtures/messages/`다. Unity EditMode 테스트는 별도 의미가 다른 사본을 만들지 말고 동일 fixture의 기대 결과를 기준으로 작성한다.

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

1. Python Task 04~06에서 웹캠과 랜드마크 검출을 먼저 완성한다.
2. Unity 수신기 구현 전 데스크톱 호환 WebSocket 패키지를 작은 연결 테스트로 선정한다.
3. 수신기 구현 시 버전 1 DTO와 fixture 기반 EditMode 테스트를 추가한다.
