# Prototype Disaster Dressing

## Goal
TriageTraceEnvironmentPrototype의 지하철 내부·플랫폼 외부에
비의료 시뮬레이션용 재난 상황 연출을 추가한다.

## Scope
- 기존 에셋 없이 Unity Primitive와 Material로 만든 단순 잔해
- 차량 외부: 쓰러진 배리어, 파손 패널, 상자, 연기처럼 보이는 반투명 효과
- 차량 내부: 넘어간 안내판, 작은 파편, 꺼진 조명 또는 경고등
- 차량 외부의 큰 잔해는 물리적 크기를 느낄 수 있도록 collider를 둘 수 있다.
  단, Patient·포인터 raycast 경로와 주 이동 통로에는 배치하지 않는다.
- 차량 내부 잔해는 Patient 선택과 이동 흐름을 보호하기 위해 visual-only로 둔다.
- Patient_01~03, Pointer, Dwell, HUD, Camera 구조는 변경하지 않는다.
- 씬 저장은 사람이 결정하고, 적용은 Undo 가능한 Editor 메뉴로만 한다.

## Out of Scope
- 실제 의료 판단·응급도 계산
- 복잡한 메시, 외부 에셋 추가, 파티클 시스템, 폭력적·사실적인 표현
- 기존 차량·플랫폼·카메라·환자 Prefab 변경
- Play Mode 자동 실행

## Visual Direction
- 어두운 회색/갈색/주황 경고등 중심
- 통로와 환자 raycast 경로는 막지 않는다.
- 외부 연출은 멀리서도 보이되 Player 시작 위치를 막지 않는다.
- Simulation Only 고지를 유지한다.

## Implementation
- 메뉴: `Triage Trace > Add Prototype Disaster Dressing`
- 대상 씬: `TriageTraceEnvironmentPrototype`
- 생성 루트: `GeneratedDisasterDressing`
- 다시 실행하면 이전 생성 루트를 Undo 가능하게 교체한다.
- Console에 생성된 오브젝트 수, 통로/Patient collider 겹침 경고를 출력한다.

## Acceptance Criteria
- 씬을 직접 저장하지 않는다.
- Undo 한 번으로 연출 전체를 되돌릴 수 있다.
- Patient와 이동 통로를 가리지 않는다.
- 외부에서도 재난 시나리오 분위기가 읽힌다.
