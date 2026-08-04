Triage Trace 데스크톱 AR 시뮬레이션의 1인칭 이동을 물리 충돌 기반으로 개선해줘.

먼저 다음 문서를 읽어라.

- AGENTS.md
- docs/unity-space-and-first-person-methodology.md
- Assets/Scripts/Presentation/FirstPersonCameraController.cs
- Assets/Editor/StationEnvironmentGenerator.cs

현재 문제:

- 카메라가 transform.position을 직접 변경해서 Collider를 통과한다.
- 카메라가 아래를 보고 전진하면 높이가 계속 낮아진다.
- 중력과 바닥 고정이 없어 움직임이 부자연스럽다.
- 기존 Python Pose, WebSocket, PointerOrigin, Raycast, Dwell, 환자 카드 기능은 정상 동작 중이다.

목표:

Unity CharacterController를 사용해 일반적인 FPS 방식으로 이동하도록 수정한다.

필수 요구사항:

1. FirstPersonCameraController를 CharacterController 기반으로 변경한다.
2. 이동에는 CharacterController.Move()를 사용한다.
3. 전진·후진 이동은 카메라 pitch와 무관한 수평면 이동이어야 한다.
4. 마우스 좌우 회전은 yaw, 상하 회전은 pitch로 분리한다.
5. 중력을 적용한다.
6. 바닥에 있을 때 아래로 계속 가라앉지 않도록 grounded 처리를 한다.
7. 기본 상태에서는 Q/E 자유 비행을 비활성화한다.
8. 필요하면 Inspector에서만 켤 수 있는 Allow Fly Mode 옵션을 추가한다.
9. Main Camera, Camera 태그, PointerOrigin 자식 구조를 유지한다.
10. 기존 FirstPersonCameraController가 붙은 위치와 직렬화 값을 최대한 유지한다.
11. CharacterController 기본값은 다음을 기준으로 구성한다.
   - Height: 1.8
   - Radius: 0.3
   - Center: 카메라가 눈높이에 있을 때 발밑까지 캡슐이 내려가도록 설정
   - Step Offset: 0.3
   - Slope Limit: 45
12. 제작 씬 TriageTraceEnvironmentPrototype에 필요한 컴포넌트를 반영한다.
13. GeneratedStationEnvironment의 기존 바닥·벽 Collider는 유지한다.
14. 지하철 내부 이동 경로의 Collider 존재 여부를 점검하고 결과를 보고한다.
15. Collider가 없더라도 모든 Mesh에 무작정 MeshCollider를 추가하지 않는다.
16. 충돌이 필요한 바닥·벽·좌석·기둥을 구분해 누락 항목만 보고한다.
17. Python Pose, WebSocket, PointerRaycaster, PatientDwellSelector, PatientView, 카드 UI 코드는 변경하지 않는다.
18. Main Camera를 추가하거나 복제하지 않는다.
19. Play Mode가 실행 중이라면 씬을 수정하지 말고 중단 후 알려준다.
20. Undo 가능한 Editor 변경 방식을 사용한다.

검증 항목:

- 아래를 보면서 W를 눌러도 높이가 내려가지 않는다.
- 이동하지 않을 때 바닥 아래로 가라앉지 않는다.
- 플랫폼 바닥 위에 서 있을 수 있다.
- Collider가 있는 벽과 구조물을 통과하지 않는다.
- 마우스 시점 회전이 정상 동작한다.
- PointerOrigin과 기존 AR 포인터 기능이 유지된다.
- Console 컴파일 오류가 없다.

완료 후 다음을 보고한다.

- 변경 파일
- CharacterController 최종 설정값
- 씬에서 변경한 오브젝트
- Collider가 확인된 구조물
- Collider가 누락된 구조물
- Unity에서 사용자가 직접 테스트할 정확한 순서
- 테스트하지 못한 항목과 이유

아직 FPS 손 모델이나 AR HUD 방향 화살표는 구현하지 마라.
이번 작업은 이동·중력·충돌 안정화만 수행한다.