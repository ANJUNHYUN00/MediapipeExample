# MediaPipe model assets

이 디렉터리는 MediaPipe Tasks가 로컬에서 읽는 모델 자산을 보관한다.

## Hand Landmarker

- 표준 경로: `models/hand_landmarker.task`
- 모델: MediaPipe HandLandmarker full, float16, latest
- 공식 다운로드: `https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/latest/hand_landmarker.task`
- 모델 카드: `https://storage.googleapis.com/mediapipe-assets/Model%20Card%20Hand%20Tracking%20%28Lite_Full%29%20with%20Fairness%20Oct%202021.pdf`
- 라이선스: Apache License 2.0
- 사용 예정 모드: 연속 웹캠 프레임용 `VIDEO`
- 초기 최대 손 수: `1`

`hand_tracker.py` 구현에서는 절대 경로를 하드코딩하지 않고 설정을 통해 이 파일을 참조한다.

## 설치된 자산 검증

- SHA-256: `FBC2A30080C3C557093B5DDFC334698132EB341044CCEE322CCF8BCF3607CDE1`
- 파일 크기: 7,819,105 bytes
- 검증 결과: MediaPipe 0.10.35에서 `HandLandmarker` VIDEO 모드 생성 및 종료 성공
