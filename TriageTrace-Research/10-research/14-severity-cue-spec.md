# 14. 응급도 단서 레이어 사양

> 구현 대응: [`22-instrumentation-spec.md`](../20-engineering/22-instrumentation-spec.md)
> 용어 규칙: [`01-project-definition.md`](../00-overview/01-project-definition.md) §5

## 1. 설계 원칙

1. **직교성.** 기존 `PatientInteractionState`(확인 흐름)와 새 응급도 차원을 절대 합치지 않는다. `PatientView.cs`의 기존 주석이 이미 이 분리를 요구하고 있다.
2. **관찰 가능성.** 단서는 참가자가 **화면에서 볼 수 있어야** 한다. 볼 수 없는 속성(혈압 등)은 넣지 않는다.
3. **standoff 현실성.** 실제 원거리 센싱이 판별 가능하다고 보고된 신호만 쓴다. 문헌상 그것은 **총체적 움직임, 보행 가능 여부, 자세**가 사실상 전부다.
4. **정답의 결정론성.** 단서 조합 → 라벨은 규칙표로 완전히 결정된다. 판단이 개입하지 않는다.
5. **비잔혹성.** 출혈·외상은 추상 표식으로만 표현한다. IRB 대응이자 참가자 보호다.

## 2. 단서 차원 — 4개

### C1. 운동성 (Motion)

| 값 | 화면 표현 | standoff 판별 가능성 |
|---|---|---|
| `Ambulatory` | 서 있거나 천천히 이동하는 애니메이션 | 높음 |
| `Purposeful` | 앉거나 누운 채 팔을 움직임 (손짓) | 높음 |
| `Minimal` | 미세한 몸통 움직임만 | 중간 |
| `None` | 완전 정지 | 높음 (단, 오탐 위험 — CMU 사례의 오탐 3건이 전부 이 방향) |

### C2. 자세 (Posture)

| 값 | 화면 표현 |
|---|---|
| `Standing` | 직립 |
| `Seated` | 좌석/바닥 착석 |
| `Kneeling` | 무릎 |
| `Prone` | 엎드림 |
| `Supine` | 누움 |

자세는 선택 난이도에도 영향을 준다(엎드린 대상은 바운딩 박스가 작다). 4순위 프레이밍의 씨앗이기도 하다.

### C3. 호흡 표시 (Respiration Indicator)

| 값 | 화면 표현 |
|---|---|
| `Visible-Normal` | 가슴 상하 애니메이션, 느린 주기 |
| `Visible-Rapid` | 빠른 주기 |
| `Not-Visible` | 움직임 없음 |

⚠️ **수치(분당 호흡수)를 표시하지 않는다.** 수치를 넣는 순간 생체신호 측정 주장이 된다. "보이는가/빠른가"까지만 간다.

### C4. 반응성 (Responsiveness)

| 값 | 화면 표현 |
|---|---|
| `Responds` | 참가자가 지정하면 고개를 돌리거나 손을 듦 |
| `Delayed` | 지연된 미약한 반응 |
| `None` | 무반응 |

**C4는 dwell 상호작용과 자연스럽게 결합된다.** 지정 → 반응 관찰 → 확인이라는 흐름이 생기고, 이는 실제 트리아지의 "지시 반응 확인"과 구조적으로 대응한다. 연구적으로 가장 값어치 있는 단서다.

## 3. 정답 라벨 규칙표

라벨은 4단계. 색은 국제 관행을 따르되, **코드와 논문 모두에서 우선순위 라벨로만 부른다.**

| 라벨 | 코드 enum | 색 | 규칙 (위에서부터 먼저 맞는 것) |
|---|---|---|---|
| P1 즉시 | `Immediate` | 적 | `C1=None` **또는** `C3=Not-Visible` **또는** (`C3=Visible-Rapid` **그리고** `C4≠Responds`) |
| P2 지연 | `Delayed` | 황 | 비보행(`C1≠Ambulatory`) **그리고** `C4∈{Responds, Delayed}` **그리고** P1 아님 |
| P3 경증 | `Minor` | 녹 | `C1=Ambulatory` **그리고** `C4=Responds` |
| P4 예상 | `Expectant` | 흑 | `C1=None` **그리고** `C3=Not-Visible` **그리고** `C4=None` |

**규칙 적용 순서: P4 → P1 → P2 → P3.** P4가 P1보다 먼저 평가되어야 한다(P4는 P1의 부분집합).

### 근거 표기 방법 (논문용)

> 우선순위 라벨은 공개된 mass-casualty triage 프로토콜의 관찰 분기(보행 가능 여부, 자율 호흡의 가시성, 지시 반응)를 시나리오 저작 시점에 기계적으로 적용하여 결정하였다. 이는 임상 판단이 아니라 시나리오 설계 규칙이며, 시스템은 어떤 시점에도 환자를 평가하지 않는다.

이 문장이 §3 첫 문단에 들어가야 한다.

## 4. 10명 환자 시나리오 구성

`Patient_01`~`Patient_10` (표시 ID `TR-001`~`TR-010`)에 대한 기준 배분:

| 라벨 | 인원 | 비고 |
|---|---|---|
| P1 즉시 | 3 | 최소 1명은 화면 밖 초기 배치 |
| P2 지연 | 4 | |
| P3 경증 | 2 | 최소 1명은 이동 중 |
| P4 예상 | 1 | 윤리적 민감성 — [`15-ethics-and-irb.md`](./15-ethics-and-irb.md) 참조 |

### 시나리오 변형

조건 간 학습 효과를 막기 위해 **최소 3개 시나리오 세트**가 필요하다. 각 세트는:
- 라벨 분포는 동일하게 유지
- 위치·ID·단서 조합은 재배치
- `scenarioSeed`로 재현 가능하게 저장

**P4(예상) 포함 여부는 파일럿 후 결정한다.** 참가자 디스트레스 반응이 관찰되면 제외하고 P1을 4명으로 늘린다.

## 5. 구현 매핑

### 새 컴포넌트: `PatientUrgencyProfile`

```
enum UrgencyLabel { Immediate, Delayed, Minor, Expectant }
enum MotionCue { Ambulatory, Purposeful, Minimal, None }
enum PostureCue { Standing, Seated, Kneeling, Prone, Supine }
enum RespirationCue { VisibleNormal, VisibleRapid, NotVisible }
enum ResponsivenessCue { Responds, Delayed, None }
```

- `PatientView`와 **같은 GameObject에 별도 컴포넌트로** 붙인다. `PatientView`를 수정하지 않는다.
- `UrgencyLabel`은 4개 cue로부터 계산되는 읽기 전용 프로퍼티로 두고, Inspector에서 직접 설정하지 못하게 한다. 규칙표와 어긋날 수 없게 만드는 것이 목적이다.
- Animator 상태는 `MotionCue` + `RespirationCue` 조합으로 결정한다.

### Animator 요구사항

기존 환자 FBX는 `PatientModelIntegrationMenu.cs`가 **Animator를 비활성화**하고 기본 pose를 유지하도록 설치한다. 단서 레이어를 넣으려면 이 부분을 바꿔야 한다:

- Animator를 살리되 **Root Motion은 끈다** (위치가 바뀌면 배치 검증이 무효)
- 최소 상태 4개: `Idle_Still`, `Idle_Breathing`, `Gesture_Wave`, `Walk_Slow`
- `Responds` 반응은 dwell 시작 시 트리거되는 일회성 애니메이션

⚠️ 이 변경은 `TenPatientIdentityAndPlacementMenu.cs`의 배치 검증과 충돌할 수 있다. 변경 후 반드시 재실행하여 collider overlap과 position drift를 확인한다.

## 6. 검증 체크리스트

- [ ] 10명 각각의 cue 조합 → 라벨이 규칙표와 일치하는지 자동 테스트 (EditMode)
- [ ] 라벨이 Inspector에서 수동 변경 불가능한지 확인
- [ ] 4개 cue 애니메이션이 서로 다른 pose(seated/prone)에서 모두 보이는지
- [ ] Animator 활성화 후에도 환자 위치·collider가 변하지 않는지
- [ ] 시나리오 3세트가 동일 라벨 분포를 가지는지 자동 검증
- [ ] P4 환자의 시각 표현이 잔혹하지 않은지 (스크린샷으로 IRB 제출)
