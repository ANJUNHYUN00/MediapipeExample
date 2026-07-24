# Task 03. 개발 환경 구성

> 상태: 완료 (2026-07-23)

> Triage Trace 전환 안내 (2026-07-24): 이 Task의 Python·Unity 환경 결과는 그대로 재사용한다. 기존 RPS 기준의 Task 04 연결은 이력이다. Pose 런타임 Task 07과 포인팅 Task 08은 완료됐으며 현재 다음 활성 구현은 [`09-pose-v2-publisher-and-unity-receiver.md`](./09-pose-v2-publisher-and-unity-receiver.md)다.

## 작업 목적

Python 손 추적 코드를 실행·테스트할 수 있는 재현 가능한 Python 3.11 환경을 구성하고, Unity LTS 프로젝트를 올바른 Editor로 열 수 있는 상태로 만든다. 의존성 버전과 실제 환경 정보를 기록해 다른 AI 또는 개발자가 같은 명령으로 환경을 재구성할 수 있게 한다.

## 선행 조건

- Task 01과 Task 02가 완료되어 프로젝트 구조와 계약이 확정되어 있을 것
- Python 3.11 설치 여부를 확인할 수 있을 것
- Unity Hub 또는 Unity LTS Editor의 설치 여부와 경로를 확인할 수 있을 것
- 외부 패키지 설치와 모델 자산 다운로드에는 네트워크 권한이 필요할 수 있음을 인지할 것
- 기존 `pyproject.toml`, Unity `manifest.json`, 잠금 파일이 있다면 덮어쓰기 전에 내용을 확인할 것

## 작업 단계

1. 현재 도구 버전을 조사하고 결과를 기록한다.

   PowerShell 기준:

   ```powershell
   py -0p
   py -3.11 --version
   git --version
   ```

   - Python 3.11이 없으면 설치를 임의로 진행하지 말고 필요한 권한과 설치 방법을 사용자에게 알린다.
   - Unity Hub/Editor는 `Get-Command`와 일반적인 설치 위치를 읽기 전용으로 확인한다.
   - 프로젝트에 `ProjectVersion.txt`가 있으면 그 버전을 우선한다.

2. Python 가상 환경을 `Mediapipe/.venv`에 만든다.

   ```powershell
   Set-Location Mediapipe
   py -3.11 -m venv .venv
   .\.venv\Scripts\python.exe -m pip install --upgrade pip
   ```

   활성화 여부에 의존하지 않도록 검증 명령은 가능한 한 `.venv\Scripts\python.exe`의 명시적 경로를 사용한다.

3. `Mediapipe/pyproject.toml`을 구성한다.

   최소 항목:

   - Python 요구 버전 `>=3.11,<3.12` 또는 실제 호환 범위
   - 런타임 의존성: OpenCV, MediaPipe, `websockets`
   - 개발 의존성: `pytest`
   - `src` 레이아웃 패키지 검색 설정
   - pytest 테스트 경로
   - 앱 진입점 또는 `python -m mediapipe_rps.app` 실행 방식

   아직 검증하지 않은 버전을 임의로 고정하지 않는다. 설치가 성공한 조합을 확인한 후 재현 가능한 버전 범위를 기록한다.

4. Python 패키지를 설치한다.

   프로젝트가 editable 설치를 지원하도록 구성한 뒤:

   ```powershell
   .\.venv\Scripts\python.exe -m pip install -e ".[dev]"
   ```

   샌드박스 또는 네트워크 제한으로 실패하면 같은 명령에 필요한 승인을 요청한다. 전역 Python 환경에 대신 설치하지 않는다.

5. 설치된 환경을 검증한다.

   ```powershell
   .\.venv\Scripts\python.exe -c "import cv2; import mediapipe; import websockets; print('imports-ok')"
   .\.venv\Scripts\python.exe -m pytest
   .\.venv\Scripts\python.exe -m pip check
   .\.venv\Scripts\python.exe -m pip freeze
   ```

   - `pytest`에 아직 테스트가 없다면 테스트 없음과 실패를 구분한다.
   - 임시 smoke test를 만든 경우 지속적으로 가치가 있는 테스트만 저장소에 남긴다.
   - `pip freeze` 결과에서 실제 핵심 버전을 README에 기록한다.

6. MediaPipe API와 모델 자산 방식을 확정한다.

   - 설치된 MediaPipe에서 Hand Landmarker API 사용 가능 여부를 작은 import smoke test로 확인한다.
   - 기본 구현은 연속 영상용 Hand Landmarker와 `num_hands=1`을 사용하도록 계획한다.
   - 모델 파일이 필요하면 `Mediapipe/models/hand_landmarker.task`를 표준 경로로 정한다.
   - 모델의 공식 출처, 라이선스, 버전 또는 체크섬을 `Mediapipe/README.md`에 기록한다.
   - 모델 다운로드가 필요하면 사용자 승인과 네트워크 정책을 따르며 출처를 추측하지 않는다.
   - 설치 버전에서 계획한 API가 제공되지 않으면 임의 호환 코드를 추가하기 전에 의존성 버전 또는 Plan 변경을 결정하고 문서화한다.

7. Unity 프로젝트 상태를 확인한다.

   유효한 Unity 프로젝트 기준:

   - `MediapipeUnity/Assets/`
   - `MediapipeUnity/Packages/manifest.json`
   - `MediapipeUnity/ProjectSettings/ProjectVersion.txt`

   세 요소가 없으면 Unity Hub 또는 확인된 LTS Editor로 `MediapipeUnity/`에 프로젝트를 생성한다. Unity가 설치되어 있지 않거나 GUI 실행 승인이 필요하면 사용자에게 정확한 필요 작업을 요청한다. 텍스트 파일만으로 Unity 프로젝트를 위조하지 않는다.

8. Unity 버전을 고정하고 기록한다.

   - `ProjectVersion.txt`의 Editor 버전을 `MediapipeUnity/README.md`에 기록한다.
   - 프로젝트를 해당 버전으로 열고 컴파일 오류가 없는지 확인한다.
   - TextMeshPro 필수 리소스는 UI 구현 시점에 가져올 수 있도록 상태를 기록한다.
   - WebSocket 패키지는 후속 수신기 구현 전 작은 데스크톱 호환성 검증을 거쳐 선택하며, 이 Task에서 검증 없이 추가하지 않는다.

9. 저장소 상태를 정리한다.

   - `.venv/`, Unity `Library/`, `Temp/`, `Logs/`, `obj/`가 ignore되는지 확인한다.
   - Unity `.meta`, `Packages/manifest.json`, `packages-lock.json`, `ProjectSettings`는 필요한 프로젝트 파일로 보존한다.
   - 로컬 절대 경로나 사용자 이름이 설정 파일에 들어가지 않았는지 확인한다.

10. 재현 절차를 README에 작성한다.

    - Python 가상 환경 생성
    - editable 설치
    - import 및 pytest 검증
    - 모델 자산 배치 방법
    - Unity Editor 버전
    - Unity 프로젝트 여는 방법
    - 알려진 설치 오류와 해결 방법

11. 최종 검증 결과를 기록한다.

    - OS
    - Python과 핵심 패키지 버전
    - Unity Editor 버전
    - MediaPipe API 및 모델 경로
    - 성공한 명령
    - 자동으로 해결하지 못한 외부 환경 문제

## 완료 기준

- `Mediapipe/.venv`가 Python 3.11로 생성되어 있다.
- 가상 환경에서 OpenCV, MediaPipe, `websockets`, `pytest` import가 성공한다.
- `pip check`가 의존성 충돌 없이 끝난다.
- `pyproject.toml`로 패키지와 개발 의존성을 재설치할 수 있다.
- 사용할 MediaPipe 손 API와 모델 자산 경로가 확정·문서화되어 있다.
- `MediapipeUnity/`가 유효한 Unity 프로젝트이거나, 외부 설치 문제로 생성할 수 없는 정확한 사유가 기록되어 있다.
- Unity Editor 버전이 `ProjectVersion.txt`와 README에서 일치한다.
- 가상 환경과 Unity 생성 산출물이 버전 관리 대상에서 제외되어 있다.
- 환경 재현 및 검증 명령이 README에 기록되어 있다.

## 예상 산출물

- `Mediapipe/.venv/` 로컬 가상 환경
- 완성된 `Mediapipe/pyproject.toml`
- 갱신된 `Mediapipe/README.md`
- `Mediapipe/models/hand_landmarker.task` 또는 문서화된 모델 배치 절차
- 유효한 `MediapipeUnity/Assets`, `Packages`, `ProjectSettings`
- 갱신된 `MediapipeUnity/README.md`
- 환경 버전 및 설치 검증 기록

## 다음 Task와의 연결

검증된 OpenCV와 Python 실행 환경을 사용해 [`04-webcam-loop-and-preview.md`](./04-webcam-loop-and-preview.md)에서 카메라 입력 루프와 미리보기 창을 구현한다. Task 04는 MediaPipe를 호출하지 않고 프레임 캡처, 설정, 오류 처리와 정상 종료만 먼저 검증한다.

## 수행 결과

- Windows 11 x64와 Git 2.54.0 환경을 확인했다.
- Python 인터프리터가 없어 `winget`으로 Python 3.11.9를 설치했다. 기존 Python Launcher가 새 인터프리터를 열거하지 못해 `%LOCALAPPDATA%\Programs\Python\Python311\python.exe`를 사용했다.
- `Mediapipe/.venv`를 만들고 pip 26.1.2로 갱신했다.
- `pyproject.toml`에 MediaPipe 0.10.35, OpenCV contrib 5.0.0.93, websockets 16.1.1, pytest 8.4.2를 고정하고 editable 설치했다.
- OpenCV, MediaPipe, websockets import와 `vision.HandLandmarker` API 존재를 확인했다.
- 공식 HandLandmarker full float16 모델을 `Mediapipe/models/hand_landmarker.task`에 설치했다. 크기는 7,819,105 bytes, SHA-256은 `FBC2A30080C3C557093B5DDFC334698132EB341044CCEE322CCF8BCF3607CDE1`이다.
- 모델을 이용한 `HandLandmarker` VIDEO 모드 생성과 정상 종료에 성공했다.
- `python -m pytest` 결과는 2개 테스트 통과, `pip check` 결과는 의존성 충돌 없음이다.
- 관리형 샌드박스의 `.pytest_cache` 파일 잠금으로 테스트 종료가 지연돼 pytest cache provider를 프로젝트 설정에서 비활성화했다. 이후 표준 `python -m pytest`가 종료 코드 0으로 정상 완료됐다.
- 제한된 네트워크에서 모델 종료 시 원격 로그 업로더 연결 실패 메시지가 출력되지만, 모델 초기화와 종료 검증은 성공했다.
- Unity Hub 3.16.3과 Editor 6000.3.10f1을 확인하고 `MediapipeUnity/`에 정식 Unity 프로젝트를 생성했다.
- Unity batch mode 초기 import와 컴파일이 종료 코드 0으로 끝났고 C# 컴파일 오류가 없음을 로그에서 확인했다.
- `ProjectSettings/ProjectVersion.txt`가 `6000.3.10f1 (e35f0c77bd8e)`로 기록됐으며 README와 일치한다.
- TextMeshPro 리소스와 WebSocket 패키지는 실제 UI·수신기 구현 Task에서 검증하기 위해 아직 추가하지 않았다.
- `.venv`, Unity `Library`, `Temp`, `Logs`, `UserSettings`, `obj`는 루트 `.gitignore` 규칙으로 제외된다.
