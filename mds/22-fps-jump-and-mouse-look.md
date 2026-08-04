Triage Trace의 CharacterController 기반 1인칭 이동에 점프와 일반 FPS 방식의 마우스 시점을 구현해줘.

먼저 다음 파일을 읽어라.

- AGENTS.md
- docs/unity-space-and-first-person-methodology.md
- Assets/Scripts/Presentation/FirstPersonCameraController.cs
- Assets/Editor/StationEnvironmentGenerator.cs
- Assets/Scenes/TriageTraceEnvironmentPrototype.unity

현재 확인된 상태:

- CharacterController 이동이 정상 작동한다.
- 차량 바닥, 외벽, 좌석, 기둥과 플랫폼 충돌이 정상이다.
- 차량 밖으로 이동한 뒤 높이 차이 때문에 다시 들어갈 수 없다.
- 현재는 마우스 오른쪽 버튼을 누르고 있어야 시점을 움직일 수 있다.
- 사용자는 일반 FPS처럼 버튼을 누르지 않고 마우스 이동만으로 시점을 돌리고 싶다.

목표:

점프가 가능한 일반 FPS 조작 방식으로 개선한다.

필수 요구사항:

1. FirstPersonCameraController에 grounded 점프를 추가한다.
2. 점프 키는 Space로 한다.
3. CharacterController.isGrounded 상태에서만 점프할 수 있다.
4. 공중에서 연속 점프할 수 없게 한다.
5. 기본 Jump Height는 0.8m로 한다.
6. Inspector에서 Jump Height를 조정할 수 있게 한다.
7. 기존 gravity 값을 사용해 자연스러운 초기 점프 속도를 계산한다.
8. 점프 중에도 WASD 수평 이동은 유지한다.
9. Allow Fly Mode가 켜진 경우 점프 로직과 충돌하지 않게 한다.
10. 기본 requireRightMouseButton을 false로 변경한다.
11. Game View에 포커스를 준 뒤에는 버튼을 누르지 않아도 마우스 이동으로 yaw/pitch가 변경되어야 한다.
12. Play Mode 시작 시 마우스 커서를 자동으로 중앙에 고정하고 숨긴다.
13. Escape를 누르면 커서 잠금을 해제하고 표시한다.
14. 커서가 해제된 상태에서 Game View를 다시 클릭하면 커서를 잠근다.
15. 커서가 해제된 동안에는 마우스 이동으로 시점이 회전하지 않게 한다.
16. 기존 pitch 제한을 유지한다.
17. 기존 CharacterController.Move(), 수평 이동, 중력, 충돌 기능을 유지한다.
18. Main Camera, PointerOrigin, Pose, WebSocket, Raycast, Dwell, 환자 카드 코드를 변경하지 않는다.
19. TriageTraceEnvironmentPrototype 씬의 FirstPersonCameraController 설정을 Undo 가능하게 반영한다.
20. 기존 Triage Trace > Configure Grounded First-Person Controller 메뉴를 확장하거나 별도 안전한 설정 메뉴를 제공한다.
21. Play Mode에서는 씬 설정을 수정하지 않는다.

권장 기본값:

- Jump Height: 0.8
- Gravity: 20
- Mouse Sensitivity: 기존 값 유지
- Require Right Mouse Button: Off
- Lock Cursor While Looking: On
- Allow Fly Mode: Off

검증 기준:

- Space를 누르면 한 번 점프한다.
- 플랫폼에서 차량 출입구 높이까지 올라갈 수 있다.
- 착지 후 다시 점프할 수 있다.
- 공중에서 Space를 연속 입력해도 무한 점프하지 않는다.
- 오른쪽 마우스를 누르지 않아도 시점이 움직인다.s
- Escape로 마우스가 풀린다.
- Game View를 다시 클릭하면 마우스가 고정된다.
- 아래를 보며 전진해도 바닥으로 내려가지 않는다.
- 벽과 좌석 충돌이 유지된다.
- 기존 포인터와 카드 기능이 유지된다.
- Console에 새 빨간 오류가 없다.

완료 후 보고:

- 변경 파일
- 추가한 Inspector 옵션
- 최종 입력 방식
- Unity 메뉴 실행 방법
- 씬에 반영된 값
- 사용자 테스트 순서
- 자동 검증하지 못한 항목

아직 FPS 손, 구급상자, AR HUD 방향 화살표는 구현하지 마라.
이번 작업은 점프와 마우스 시점 개선만 수행한다.