# 지하철 플랫폼 공간 생성 작업

## 목표

TriageTraceEnvironmentPrototype 씬의 연결된 지하철 차량 3개 주변에 이동 가능한 지하철 플랫폼과 터널 기본 공간을 생성한다.

## 선행 분석

explorer 서브에이전트 1개를 사용하여 읽기 전용으로 다음을 확인한다.

- AGENTS.md
- docs/unity-space-and-first-person-methodology.md
- TriageTraceEnvironmentPrototype 씬
- 차량 3개의 위치, 길이 축, 전체 Bounds
- Main Camera와 FirstPersonCameraController 구조
- 기존 Collider와 조명 구조

분석이 끝날 때까지 기다린 후 메인 에이전트가 구현한다.

## 구현 범위

Unity Editor 전용 공간 생성 도구를 만든다.

생성할 구조:

- GeneratedStationEnvironment
- 차량과 평행한 플랫폼
- 플랫폼 끝 안전벽
- 터널 측면 벽
- 터널 천장
- 반복 기둥
- 비상 이동 통로
- 시작 구역
- 환자 탐색 구역 3개
- Built-in Standard 재질
- 이동 가능한 바닥 Collider

플랫폼은 차량의 X축 길이와 자동으로 맞추고, 차량 3개 전체를 포함해야 한다.

## 생성 규칙

- 메뉴: Triage Trace > Generate Station Environment
- 반드시 TriageTraceEnvironmentPrototype 씬에서만 실행한다.
- 기존 카메라, 차량, 환자, UI를 수정하지 않는다.
- Python, WebSocket, Raycast, Dwell 코드를 변경하지 않는다.
- 재생성할 때 GeneratedStationEnvironment만 교체한다.
- Undo를 지원한다.
- 제작용 TriageTraceDemoScene을 변경하지 않는다.
- 활성 Main Camera를 추가하지 않는다.
- Built-in Render Pipeline만 사용한다.
- URP 또는 HDRP 패키지를 설치하지 않는다.

## 시각 방향

최종 디자인은 현실적인 재난 대응 훈련 공간이다.

- 기본 구조는 어두운 회색 콘크리트
- 플랫폼 안전선은 노란색
- 조명은 중립색과 비상등 포인트
- 넓은 이동 통로 확보
- 장식보다 이동성과 환자 탐색 가독성 우선

## 제외 범위

- 환자 모델 교체
- Figma 카드 적용
- Python Pose 수정
- 기존 차량 재질 수정
- 최종 VFX와 사운드
- Git commit과 push

## 완료 기준

- 차량 3개를 포함하는 플랫폼이 생성된다.
- 1인칭으로 플랫폼을 이동할 수 있다.
- 바닥을 뚫고 떨어지지 않는다.
- 차량 및 기둥 Collider가 정상이다.
- 기존 환자 선택과 AR 시스템 파일은 변경되지 않는다.
- Console에 새로운 빨간 오류가 없다.

작업 완료 후 변경 파일, 사용 방법, Inspector 설정, 수동 검증 절차를 요약한다.