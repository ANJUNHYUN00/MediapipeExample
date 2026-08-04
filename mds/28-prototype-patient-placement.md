# Task 28 - Prototype Patient Placement

## 목표
TriageTraceEnvironmentPrototype의 기존 Patient_01~03을
지하철 3개 차량과 플랫폼 동선에 분산 배치해
AR Guidance HUD, 포인터, Dwell, World Space 카드 테스트가 가능하게 한다.

## 배치 원칙
- Patient_01: 첫 번째 차량 안, 시작 위치에서 비교적 가까운 대상
- Patient_02: 두 번째 차량 안, 이동 후 확인할 대상
- Patient_03: 세 번째 차량 또는 플랫폼 쪽, 처음에는 화면 밖에 있어
  AR Guidance HUD 방향 화살표를 검증할 수 있는 대상

## 반드시 지킬 조건
1. 기존 Patient_01~03을 재사용한다. 새 환자 에셋을 추가하지 않는다.
2. PatientView, Patient Layer, Collider, Display Name,
   World Space 카드 Anchor 설정을 유지한다.
3. Python Pose, WebSocket, PointerOrigin, Raycast, Dwell,
   AR Guidance HUD 코드는 변경하지 않는다.
4. 기존 차량, GeneratedStationEnvironment, Main Camera,
   FirstPersonPresentation 구조를 수정하지 않는다.
5. 환자는 좌석 내부, 벽 내부, 바닥 아래에 배치하지 않는다.
6. 환자끼리 겹치지 않게 한다.
7. 환자 배치는 Undo 가능한 Editor 메뉴로만 적용한다.
8. 열린 Unity 씬을 직접 저장하거나 Play Mode 실행하지 않는다.

## 구현 요구사항
- `Triage Trace > Arrange Prototype Patients` 메뉴를 추가한다.
- TriageTraceEnvironmentPrototype에서만 동작한다.
- 차량 3개의 실제 bounds와 기존 환자 collider bounds를 분석해
  안전한 후보 위치를 정한다.
- 아래를 Console에 요약한다.
  - 각 환자의 최종 위치
  - 가장 가까운 차량 이름
  - 환자끼리의 최소 거리
  - 바닥 또는 구조물 겹침 경고
- 메뉴 적용 후 Unity에서 사람이 확인할 항목을 정리한다.

## 완료 기준
- 코드 컴파일 오류 없음
- 씬 파일 직접 수정 없음
- 3명 모두 각기 다른 구역에 배치할 수 있음
- HUD의 화면 밖 방향 화살표 테스트가 가능함