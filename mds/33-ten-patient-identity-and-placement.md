# Task 33 - Ten Patient Identity and Placement

## Goal
Patient_01~Patient_10을 독립적인 환자로 정리하고,
ID·상태 카드·HUD가 10명을 정확히 구분하도록 검증한다.

## Rules
- PatientView, Collider, Patient Layer, Status Card Anchor를 유지한다.
- Pose, WebSocket, Pointer, Raycast, Dwell, World Space Card,
  AR Guidance HUD의 계약을 변경하지 않는다.
- 외형 모델과 기존 PatientVisual_Model을 교체하거나 삭제하지 않는다.
- 씬 저장과 Play Mode 실행은 하지 않는다.
- Edit Mode 전용 Undo 지원 메뉴로만 적용한다.

## Required Behavior
- Patient_01~Patient_10을 찾아 Display Name을 TR-001~TR-010으로 설정한다.
- PatientMarker의 고정 ID 텍스트가 있으면 동일한 ID로 갱신한다.
- 각 PatientView에 Target Renderers와 Status Card Anchor가 유효한지 검사한다.
- 누락된 Collider, Patient Layer, PatientView를 경고로 보고한다.
- 생성된 10명은 처음에 모두 미확인 상태여야 한다.
- 환자별 상태는 독립적으로 유지되어야 한다.
- Console에 환자별 ID, 모델 Renderer 수, Collider, Layer,
  Card Anchor, 현재 Checked 상태를 표로 출력한다.

## Placement Plan
- 차량 1: TR-001, TR-002, TR-003
- 차량 2: TR-004, TR-005, TR-006
- 차량 3 또는 플랫폼: TR-007, TR-008, TR-009, TR-010
- 이동 통로, 출입문, Player 시작 지점, 환자 포인터 경로는 막지 않는다.
- 자동 위치 이동은 하지 않고, 위치 충돌 위험만 보고한다.

## Acceptance Criteria
- HUD에서 UNCONFIRMED 10 / CHECKED 0으로 인식 가능
- 각 환자를 확인하면 해당 ID만 Checked로 전환
- 화면 밖 미확인 환자만 방향 화살표와 거리 표시
- 씬 자동 저장 없음