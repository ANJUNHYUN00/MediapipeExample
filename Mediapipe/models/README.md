# MediaPipe model assets

이 디렉터리는 MediaPipe Tasks가 로컬에서 읽는 모델 자산을 보관한다. 모델 파일은 Python 프로세스에서만 사용하며 Unity로 전송하지 않는다.

## Legacy Hand Landmarker

기존 gesture v1 실습 자산으로 보존한다.

- 경로: `models/hand_landmarker.task`
- 모델: MediaPipe HandLandmarker full, float16, latest
- 공식 다운로드: `https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/latest/hand_landmarker.task`
- 모델 카드: `https://storage.googleapis.com/mediapipe-assets/Model%20Card%20Hand%20Tracking%20%28Lite_Full%29%20with%20Fairness%20Oct%202021.pdf`
- 라이선스: Apache License 2.0
- SHA-256: `FBC2A30080C3C557093B5DDFC334698132EB341044CCEE322CCF8BCF3607CDE1`
- 크기: 7,819,105 bytes
- 검증: MediaPipe 0.10.35에서 HandLandmarker VIDEO 모드 생성·종료 성공

Triage Trace의 활성 Pose 흐름에서 이 모델을 사용하지 않는다.

## Pose Landmarker

Triage Trace의 Python 단독 실시간 MVP 모델이다.

- 경로: `models/pose_landmarker_lite.task`
- 모델: MediaPipe Pose Landmarker Lite, float16, latest
- 공식 다운로드: `https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_lite/float16/latest/pose_landmarker_lite.task`
- 모델 카드: `https://storage.googleapis.com/mediapipe-assets/Model%20Card%20BlazePose%20GHUM%203D.pdf`
- 라이선스: Apache License 2.0
- SHA-256: `59929E1D1EE95287735DDD833B19CF4AC46D29BC7AFDDBBF6753C459690D574A`
- 크기: 5,777,746 bytes
- 검증: MediaPipe 0.10.35에서 `PoseLandmarker` VIDEO 모드 생성, 검정 합성 프레임 추론, 종료 성공

Lite 변형은 한 사람의 오른팔 관절을 웹캠에서 연속 추적하는 MVP에서 CPU
실시간성을 우선해 선택했다. Full 또는 Heavy로 바꿀 때는 정확도뿐 아니라 목표
장치의 프레임 시간도 함께 다시 측정해야 한다.

Hand Landmarker 파일의 이름만 바꾸거나 Pose API에 전달하지 않는다.
