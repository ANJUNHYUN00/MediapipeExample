# Task 27 - AR Guidance HUD

## 목표
1인칭 Triage Trace 화면에 과하지 않은 AR 글래스형 안내 UI를 추가한다.

## 화면 구성
- 좌측 상단: 연결 상태와 Pose 상태를 작은 텍스트로 표시
- 상단 중앙 또는 우측: 미확인 환자 수 / 확인 완료 수 표시
- 화면 좌우: 카메라 화면 밖에 있는 미확인 환자가 있을 때만
  방향 화살표와 거리 정보를 표시
- 기존 환자 위 World Space 카드와 PatientMarker는 유지한다.

## 디자인 원칙
- 게임 점수, 체력바, 미니맵, 총기 UI는 추가하지 않는다.
- 실제 의료 판단처럼 보이는 위험도·진단·중증도 표현은 하지 않는다.
- 흰색 기본, cyan 활성, amber 미확인 정도만 사용한다.
- 반투명 패널, 얇은 선, 작은 글자 중심의 세련된 AR HUD로 만든다.
- 환자 카드가 표시될 때 HUD와 겹치거나 화면을 가리지 않게 한다.

## 구현 조건
1. Python Pose, WebSocket, PointerOrigin, Raycast, Dwell, 환자 상태 전이 로직은 변경하지 않는다.
2. 기존 StatusCardCanvas와 WorldSpacePatientStatusCard는 삭제하거나 대체하지 않는다.
3. Screen Space Overlay Canvas를 별도 ARGuidanceHUD로 구성한다.
4. PatientView의 Checked 여부를 읽어 미확인/완료 수를 갱신한다.
5. Main Camera 기준으로 화면 밖의 미확인 환자만 좌우 화살표로 안내한다.
6. 화살표는 좌측 또는 우측 가장자리에만 표시하고,
   환자가 화면 안에 있으면 숨긴다.
7. HUD는 Raycast 또는 버튼 입력을 가로채지 않는다.
8. `Triage Trace > Install AR Guidance HUD` Editor 메뉴를 만든다.
9. 기존 씬을 자동 저장하거나 Play Mode에서 수정하지 않는다.
10. 설치는 Undo 가능해야 한다.
11. 코드 컴파일 확인과 Unity에서 실행할 메뉴/확인 항목을 요약한다.

## 완료 기준
- Game View에서 작은 AR 상태 표시가 보인다.
- 화면 밖 미확인 환자가 있으면 해당 방향 화살표가 나타난다.
- 기존 포인터, dwell, World Space 카드가 그대로 동작한다.