Triage Trace 프로토타입에서 CharacterController가 지하철 차량의 벽, 좌석, 기둥을 통과하는 문제를 수정해줘.

먼저 다음 파일을 읽어라.

- AGENTS.md
- docs/unity-space-and-first-person-methodology.md
- Assets/Scripts/Presentation/FirstPersonCameraController.cs
- Assets/Editor/StationEnvironmentGenerator.cs
- Assets/Scenes/TriageTraceEnvironmentPrototype.unity

현재 확인된 상태:

- Main Camera에 CharacterController가 정상 추가되어 있다.
- CharacterController.Move() 기반 이동도 적용되어 있다.
- Allow Fly Mode는 꺼져 있다.
- GeneratedStationEnvironment의 플랫폼과 벽에는 Collider가 있다.
- 하지만 차량 내부의 벽, 좌석, 기둥과 대부분의 구조물을 통과한다.
- 현재 씬 전체 Collider 개수로는 차량 내부 구조를 충분히 덮지 못한다.
- Presentation_plane MeshCollider만으로는 차량 내부 충돌이 완성되지 않는다.

목표:

TriageTraceEnvironmentPrototype 씬의 연결된 지하철 차량 3개에 안전하고 관리 가능한 정적 충돌 구조를 생성한다.

필수 요구사항:

1. Editor 전용 충돌 생성 도구를 구현한다.
2. 메뉴 이름은 다음으로 한다.
   Triage Trace > Generate Train Interior Colliders
3. TriageTraceEnvironmentPrototype 씬에서만 실행한다.
4. Play Mode에서는 실행하지 않는다.
5. Undo를 지원한다.
6. 기존 차량 모델과 재질은 수정하지 않는다.
7. 기존 Collider를 중복 생성하지 않는다.
8. 모든 Mesh에 무조건 MeshCollider를 추가하지 않는다.
9. 각 SubwayTrainEnvironment 아래의 의미 있는 그룹을 우선 분석한다.
   - FLOOR
   - WALLS
   - CHAIR
   - POLE
   - TRAIN
10. 다음 기준으로 충돌을 생성한다.
   - 바닥과 벽: 정적 비볼록 MeshCollider 또는 단순 BoxCollider
   - 좌석: 가능한 경우 Renderer bounds 기반 BoxCollider
   - 수직 기둥: bounds 기반 CapsuleCollider
   - 문틀과 통로 경계: 필요한 경우 BoxCollider
11. 광고, 조명, 카메라, 데칼, 작은 장식물에는 Collider를 만들지 않는다.
12. 생성된 충돌 오브젝트는 차량별 GeneratedTrainColliders 아래에 정리한다.
13. 충돌 오브젝트는 Renderer 없이 Collider만 갖도록 한다.
14. Rigidbody를 추가하지 않는다.
15. CharacterController와 충돌 가능한 일반 Collider로 생성한다.
16. Trigger로 만들지 않는다.
17. 이동 통로를 Collider가 막지 않도록 한다.
18. 차량 간 연결 구간을 통과할 수 있도록 한다.
19. Main Camera, PointerOrigin, Patient, Pose, WebSocket, Raycast, Dwell, 카드 UI는 변경하지 않는다.
20. 기존 FirstPersonCameraController 동작을 변경하지 않는다.
21. 충돌 생성 개수와 대상 그룹을 Console에 요약한다.
22. 동일 메뉴를 다시 실행하면 기존 GeneratedTrainColliders만 Undo 지원으로 교체한다.

추가 진단:

- CharacterController의 detectCollisions가 활성 상태인지 확인한다.
- CharacterController가 충돌 대상 Layer와 상호작용하는지 확인한다.
- Main Camera와 생성 Collider의 Layer Collision 설정을 확인한다.
- Collider 생성 전후 개수를 보고한다.
- 시작 위치에서 CharacterController가 기존 Collider 안에 겹쳐 생성되지 않는지 확인한다.

검증 기준:

- 차량 바닥 아래로 떨어지지 않는다.
- 차량 외벽을 통과하지 않는다.
- 좌석을 정면으로 통과하지 않는다.
- 기둥을 통과하지 않는다.
- 차량 내부 중앙 통로는 정상 이동할 수 있다.
- 차량 3개 연결 구간을 이동할 수 있다.
- 플랫폼으로 이동할 수 있다.
- Console에 새 빨간 오류가 없다.

완료 후 보고:

- 변경 파일
- 생성한 메뉴
- 차량별 생성된 Collider 개수
- BoxCollider, CapsuleCollider, MeshCollider 각각의 개수
- 제외한 오브젝트 종류
- Unity에서 사용자가 실행할 메뉴와 테스트 순서
- 자동 검증하지 못한 항목

아직 FPS 손, 구급상자, AR HUD, 환자 방향 화살표는 구현하지 마라.
이번 작업은 차량과 역 구조물의 충돌 안정화만 수행한다.