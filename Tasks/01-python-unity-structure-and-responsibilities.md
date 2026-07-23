# Task 01. Python·Unity 구조 및 책임 확정

> 상태: 완료 (2026-07-23)

## 작업 목적

Python MediaPipe 애플리케이션과 Unity 애플리케이션의 디렉터리 구조, 모듈 경계, 의존 방향을 실제 저장소에 반영할 수 있는 수준으로 확정한다. 이후 Task에서 기능을 추가할 때 카메라, 손 추적, 분류, 통신, UI 책임이 서로 섞이지 않도록 프로젝트 골격과 책임 문서를 준비한다.

이 Task에서는 기능 코드를 구현하거나 외부 패키지를 설치하지 않는다.

## 선행 조건

- 루트 [`AGENTS.md`](../AGENTS.md)를 읽었을 것
- [`Plan/01-architecture-and-environment.md`](../Plan/01-architecture-and-environment.md)를 읽었을 것
- [`Plan/02-python-hand-tracking.md`](../Plan/02-python-hand-tracking.md)부터 [`Plan/05-unity-receiver-and-ui.md`](../Plan/05-unity-receiver-and-ui.md)까지의 컴포넌트 이름을 확인했을 것
- 기존 `Mediapipe/`, `MediapipeUnity/` 파일과 하위 `AGENTS.md` 유무를 확인했을 것
- 기존 사용자 파일이 있으면 덮어쓰거나 삭제하지 않고 현재 구조에 맞게 병합할 것

## 작업 단계

1. 현재 저장소 구조를 조사한다.

   - 루트, `Mediapipe/`, `MediapipeUnity/`의 파일 목록을 확인한다.
   - 기존 Python 패키지, Unity 프로젝트 파일, `.gitignore`, README와 테스트가 있는지 기록한다.
   - Unity 생성 폴더인 `Library/`, `Temp/`, `Logs/`, `obj/`가 추적되고 있는지 확인한다.
   - 조사 결과에 따라 아래 권장 구조를 그대로 만들지, 기존 구조에 책임만 매핑할지 결정한다.

2. Python 프로젝트 골격을 `Mediapipe/`에 준비한다.

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
   ```

   - 빈 모듈에는 구현을 추측해 넣지 않는다.
   - `__init__.py`로 Python 패키지 경계를 만든다.
   - `app.py`는 향후 구성 요소 조립과 실행 수명 주기의 진입점으로 지정한다.
   - `config.py`는 카메라, MediaPipe, WebSocket, 안정화 설정의 단일 소유자로 지정한다.
   - `models.py`는 MediaPipe 객체와 독립적인 내부 데이터 모델을 소유하도록 지정한다.

3. 각 Python 모듈의 책임과 금지되는 의존성을 `Mediapipe/README.md`에 기록한다.

   | 모듈 | 책임 | 직접 참조하지 않을 대상 |
   |---|---|---|
   | `camera.py` | 카메라 열기, 프레임 읽기, 해제 | 제스처, WebSocket |
   | `hand_tracker.py` | 프레임을 랜드마크 결과로 변환 | UI, WebSocket |
   | `gesture_classifier.py` | 단일 프레임 제스처 분류 | 카메라, 네트워크 |
   | `stabilizer.py` | 프레임 간 결과 안정화 | MediaPipe API, UI |
   | `message_builder.py` | 내부 결과를 버전 1 메시지로 변환 | 랜드마크 재분류 |
   | `websocket_server.py` | 연결과 비동기 송신 | 제스처 계산 |
   | `app.py` | 설정 로드, 객체 조립, 실행·종료 | 세부 알고리즘 |

4. Unity 프로젝트 경계를 준비한다.

   - 이미 유효한 Unity 프로젝트가 있으면 기존 `Assets/`, `Packages/`, `ProjectSettings/`를 보존한다.
   - Unity 프로젝트가 아직 없다면 임의의 가짜 `ProjectSettings`를 만들지 말고, Task 03에서 Unity Editor로 프로젝트를 생성해야 한다는 상태를 `MediapipeUnity/README.md`에 기록한다.
   - 유효한 Unity 프로젝트가 있는 경우 다음 폴더를 Unity Editor 또는 파일 시스템에서 준비한다.

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

5. Unity 책임을 `MediapipeUnity/README.md`에 기록한다.

   - `Configuration`: WebSocket URI, 재연결 간격, 데이터 만료 시간
   - `Models`: 버전 1 네트워크 DTO와 검증된 도메인 상태
   - `Networking`: 연결, 수신, 취소, 재연결, 메시지 큐
   - `Presentation`: 네트워크 상태를 한글 UI로 변환
   - `Tests/EditMode`: JSON 파싱과 상태 매핑
   - `Tests/PlayMode`: 메인 스레드 큐, UI, 수명 주기

6. 저장소 ignore 규칙을 확인하고 보완한다.

   - Python: `.venv/`, `__pycache__/`, `.pytest_cache/`, 빌드·배포 산출물
   - Unity: `Library/`, `Temp/`, `Logs/`, `obj/`, 사용자별 IDE 파일
   - MediaPipe 모델 파일은 라이선스와 배포 정책이 정해질 때까지 무조건 ignore하지 않는다.
   - 기존 `.gitignore` 규칙을 보존하고 필요한 항목만 추가한다.

7. 의존 방향을 검토한다.

   ```text
   app
    -> config
    -> camera -> hand_tracker -> gesture_classifier -> stabilizer
    -> message_builder -> websocket_server

   Unity Networking -> Models
   Unity Presentation -> Models
   Unity Presentation -X-> WebSocket 라이브러리 직접 호출
   ```

   순환 참조가 생기는 구조를 문서 단계에서 제거한다.

8. 변경 검증을 수행한다.

   - 예상 디렉터리와 문서가 존재하는지 확인한다.
   - Python 파일이 올바른 `src/mediapipe_rps` 아래에 있는지 확인한다.
   - Unity 생성 산출물이 실수로 추가되지 않았는지 확인한다.
   - 구현하지 않은 파일에 임시 동작 코드나 서로 다른 메시지 enum이 들어가지 않았는지 검색한다.

9. Task 문서에 실제 수행 결과를 기록할 때는 생성·변경 파일과 구조 차이를 명시한다. 구조를 설계와 다르게 선택했다면 이유와 영향받는 후속 Task를 함께 기록한다.

## 완료 기준

- `Mediapipe/`에 Python `src` 레이아웃과 `tests/fixtures/` 경계가 존재한다.
- Python 모듈별 책임과 금지 의존성이 `Mediapipe/README.md`에 설명되어 있다.
- `MediapipeUnity/README.md`에 Unity 폴더별 책임과 프로젝트 생성 상태가 설명되어 있다.
- 유효한 Unity 프로젝트가 이미 있으면 권장 스크립트·테스트 폴더가 존재한다.
- Unity 프로젝트가 없으면 Task 03에서 Editor로 생성해야 한다는 사실이 명확히 기록되어 있다.
- Python과 Unity 생성 산출물에 대한 ignore 규칙이 준비되어 있다.
- 사용자 기존 파일이 삭제되거나 불필요하게 덮어써지지 않았다.
- 이 Task에서는 외부 의존성 설치나 기능 구현이 수행되지 않았다.

## 예상 산출물

- `Mediapipe/README.md`
- `Mediapipe/pyproject.toml` 기본 골격
- `Mediapipe/src/mediapipe_rps/` 패키지 골격
- `Mediapipe/tests/fixtures/`
- `MediapipeUnity/README.md`
- 유효한 Unity 프로젝트가 있을 경우 `Assets` 하위 책임별 폴더
- 프로젝트 루트 또는 각 프로젝트 범위의 `.gitignore` 보완
- 실제 구조와 책임 경계를 설명하는 검증 기록

## 다음 Task와의 연결

이 Task에서 확정한 모듈 경계를 기준으로 [`02-message-spec-and-run-order.md`](./02-message-spec-and-run-order.md)에서 Python과 Unity가 공유할 `hand_gesture` 버전 1 메시지 규격과 전체 실행·종료 순서를 고정한다. Task 02는 메시지 필드명을 Python과 Unity 구조에 각각 매핑하되 네트워크 구현은 아직 추가하지 않는다.

## 수행 결과

- 비어 있던 `Mediapipe/`에 `src/mediapipe_rps` 패키지와 `tests/fixtures` 골격을 생성했다.
- Python 모듈은 기능 코드 없이 책임을 나타내는 모듈 문서 문자열만 포함한다.
- `Mediapipe/README.md`에 Python 데이터 생산자의 책임, 금지 의존성, 처리 방향과 개인정보 원칙을 기록했다.
- `Mediapipe/pyproject.toml`에 Python 3.11 및 `src` 레이아웃의 기본 메타데이터를 추가했다. 외부 의존성은 Task 03에서 검증 후 추가하도록 비워 두었다.
- `MediapipeUnity/`에는 유효한 Unity 프로젝트가 없음을 확인했다. 가짜 `Assets`, `Packages`, `ProjectSettings`를 만들지 않고, `MediapipeUnity/README.md`에 Task 03의 Unity Editor 생성 절차와 향후 폴더 책임을 기록했다.
- 루트 `.gitignore`에 Python 가상 환경·캐시·빌드 산출물과 Unity 생성 폴더·IDE 산출물 제외 규칙을 추가했다.
- MediaPipe 모델 파일은 라이선스와 배포 방식을 확정하지 않았으므로 ignore 대상에 넣지 않았다.
- `.git` 디렉터리는 존재하지만 현재 작업 루트가 유효한 Git 저장소로 인식되지 않아 추적 파일 검사는 수행할 수 없었다. 파일 시스템 조사 결과 Unity 생성 산출물은 존재하지 않았다.
- Python Launcher는 있으나 설치된 Python 인터프리터가 없어 Python/TOML 구문 검사는 실행할 수 없었다. 모든 Python 파일이 단일 모듈 문서 문자열만 포함하고 있음을 확인했으며, Python 3.11 설치와 실행 검증은 Task 03에서 수행한다.
- 설계와 다른 구조 변경은 없으며, 외부 의존성 설치와 기능 구현은 수행하지 않았다.
