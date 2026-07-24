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

Triage Trace 활성 모델은 아직 설치되지 않았다. Task 07에서 다음을 완료한 뒤 이 섹션을 갱신한다.

- 공식 MediaPipe Pose Landmarker 모델 변형 선택
- 공식 출처와 라이선스 확인
- `models/pose_landmarker.task` 표준 경로
- 파일 크기와 SHA-256
- MediaPipe 0.10.35 `PoseLandmarker` VIDEO 모드 초기화
- 모델 변형 선택 이유와 성능 기준

Hand Landmarker 파일의 이름만 바꾸거나 Pose API에 전달하지 않는다.
