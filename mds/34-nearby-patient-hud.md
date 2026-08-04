# Task 34 - Nearby Patient AR HUD

## Goal
ARGuidanceHUD의 하단 환자 목록을 화면 주변의 가장 가까운 환자
4명만 보여주는 간결한 AR 패널로 개선한다.

## Scope
- Main Camera 기준 거리순으로 PatientView를 정렬한다.
- 가장 가까운 4명만 표시한다.
- 확인 완료 여부와 거리를 함께 표시한다.
- 기존 좌우 화면 끝 방향 화살표와 거리 표시는 유지한다.
- 기존 LINK, POSE, 상단 미확인/확인 완료 카운트는 유지한다.

## UI Design
- 위치: 화면 좌측 하단
- 제목: `주변 환자`
- 고정 폭의 반투명 패널
- 한 줄 형식:
  `TR-004   미확인   2.8m`
  `TR-003   확인 완료   4.1m`
- 미확인: 주황색 계열
- 확인 완료: 청록색 계열
- 최대 4줄만 표시
- 한 환자는 한 번만 표시
- 텍스트는 모두 좌측 정렬
- 화면 밖으로 넘치거나 다른 HUD와 겹치지 않게 한다.

## Rules
- PatientView의 상태를 읽기만 하고 변경하지 않는다.
- Pose, WebSocket, Pointer, Raycast, Dwell, World Space Card,
  First Person Presentation 코드는 수정하지 않는다.
- 기존 환자 위치와 모델은 수정하지 않는다.
- 씬 저장과 Play Mode 실행은 하지 않는다.
- HUD Installer의 Undo 지원을 유지한다.

## Acceptance Criteria
- 환자가 10명이어도 목록은 최대 4명만 보인다.
- 카메라 이동에 따라 가까운 4명이 갱신된다.
- Checked 상태와 거리가 올바르게 보인다.
- 목록의 좌측 정렬, 줄 간격, 패널 폭이 일정하다.
- 좌우 방향 화살표와 기존 환자 선택 기능이 유지된다.