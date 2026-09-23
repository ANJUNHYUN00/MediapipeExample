# 15. 윤리 · IRB · 규제 프레이밍

> 관련: [`01-project-definition.md`](../00-overview/01-project-definition.md) §5 용어 통제

## 1. 가장 먼저 알아야 할 규제 사실

미국 FDA의 임상 의사결정 지원(CDS) FAQ는 다음을 명시한다:

> *"Software that is meant to support time-critical decision making does not meet the definition of Non-Device CDS."*
> — [FDA CDS Software FAQs](https://www.fda.gov/medical-devices/software-medical-device-samd/clinical-decision-support-software-frequently-asked-questions-faqs)

**트리아지는 정의상 시간 임계다.** 즉 실제 진료 목적으로 사람의 응급도 우선순위를 출력하는 소프트웨어는 의료기기 영역에 들어간다.

⚠️ 2026년 개정 CDS 가이던스에서 time-critical 배제 문구가 삭제되고 Criterion 4로 이전되었다는 분석이 있으나([Hardian Health](https://www.hardianhealth.com/insights/fda-2026-clinical-decision-support-c-guidance-update), [Covington](https://www.cov.com/news-and-insights/insights/2026/01/5-key-takeaways-from-fdas-revised-clinical-decision-support-cds-software-guidance), [McDermott](https://www.mcdermottlaw.com/insights/fda-issues-long-awaited-final-clinical-decision-support-software-guidance/)), **이는 2차 법률 분석이며 원문 미확인**이다. 논문에 쓰려면 FDA 가이던스 원문을 직접 확인할 것. FAQ의 time-critical 인용문은 1차 출처이므로 안전하다.

한국 규제(의료기기법, 식약처 SaMD 가이드라인)는 별도 확인이 필요하나, **본 연구는 실제 사람을 대상으로 하지 않으므로 규제 대상이 아니다.** 아래 §2의 근거를 유지하는 한 문제되지 않는다.

## 2. 의료기기 주장을 피하는 3중 방어

"우리는 비의료입니다"라고 선언하는 것만으로는 부족하다. **설계 자체가 방어여야 한다.**

### 방어 1 — 대상이 존재하지 않는다

환자는 전부 Unity 안의 가상 모델이다. 실제 환자가 없으므로 임상 결정도, 임상 피험자도 없다. 이것이 가장 강한 방어다.

방법론적 근거로 **Nelson et al., "Simulations for Augmented Reality Evaluation for Mass Casualty Incident Triage"** ([arXiv 2601.08186](https://arxiv.org/abs/2601.08186))를 인용한다. 이 논문은 AR MCI 트리아지 평가를 위한 단계적 시뮬레이션 전략을 제안하며, 데스크톱 시뮬레이션 단계의 정당성을 제공한다.

### 방어 2 — 종속변수가 임상 지표가 아니다

- ❌ "시스템이 응급도를 정확히 평가하는가" → 의료 주장
- ✅ "인터페이스가 선택 시간, 판단 정확도, 상황 인식을 어떻게 바꾸는가" → HCI 측정

시뮬레이션의 정답은 **우리가 저작한 것**이다. 따라서 그 정답 대비 정확도는 진단 검증이 아니라 인터페이스 측정이다. **이 문장을 논문에 명시적으로 쓴다.**

### 방어 3 — 인간 권한 보존 언어

실제 센서를 쓰는 시스템조차 이 언어를 쓴다. *Drones* 2024의 UAS 논문은 89% 정확도를 내면서도 *"preliminary triage category"* 를 산출한다고 표현하고, *"the emergency physician in charge can decide whether to act according to the proposed triage or take different measures"* 라고 명시한다.

우리 시스템의 모든 출력에는 다음이 동반되어야 한다:
- HUD 상시 표기: `SIMULATION ONLY` (이미 구현되어 있음 — 유지할 것)
- 추정 결과 표기: `ESTIMATE` 또는 `SIMULATED ESTIMATE`, 절대 `DIAGNOSIS`나 `ASSESSMENT` 단독 사용 금지
- 논문 §1과 §3에 배포 의도 고지 1문단

## 3. 허용 표현 / 금지 표현

### 금지

- "실제 응급의료 정확도를 개선했다"
- "실제 구조 현장에서 안전하다"
- "AR 글래스 사용성을 검증했다" (데스크톱 단계)
- "의료진 협업 효과를 입증했다"
- "환자를 분류한다" (주어가 시스템일 때)
- "진단", "평가", "판정" (수식어 없이)

### 허용

- "재난 대응을 가정한 제스처 기반 대상 확인 상호작용을 탐색하는 프로토타입"
- "AR 글래스 인터페이스를 모사한 데스크톱 평가 환경" (1단계) / "광학투과 HMD 조건" (2단계)
- "다수 대상 탐색·상태 추적을 위한 정보 표현 가능성을 탐색"
- "모의 우선순위 추정 결과의 제시 방식이 참가자 판단에 미치는 영향"
- "저작된 정답 라벨 대비 참가자 응답의 일치도"

## 4. IRB

### 예상 심의 구분

**최소위험 / 신속심의(expedited)** 로 분류될 가능성이 높다. 단, 재난 시나리오라는 주제와 웹캠 사용 때문에 심의위원이 추가 질문을 할 수 있다.

### 필수 준비 항목

| 항목 | 내용 |
|---|---|
| 기만 없음 | 실제 사상자에 대한 기만적 사실성을 만들지 않는다. "시뮬레이션"임을 반복 고지 |
| 시각 표현 | **비잔혹한 양식화된 렌더링.** 스크린샷을 IRB에 첨부한다 |
| 철회권 | 주제의 정서적 무게를 감안해 불이익 없는 중도 철회를 명시 |
| 디스트레스 프로토콜 | 불편 호소 시 즉시 중단, 디브리핑, 필요 시 상담 안내 |
| 웹캠 데이터 | **관절 좌표만 처리, 원본 영상 미저장** — §5 |
| 보상 | 중도 철회 시에도 비례 지급 |

### 인용할 수 있는 근거

- **Watson et al., "Triage ethics in mass casualty incident simulation: A phenomenological exploration"**, *Nursing Ethics* 2025, [10.1177/09697330241299526](https://journals.sagepub.com/doi/10.1177/09697330241299526) — MCI 트리아지 시뮬레이션 *내부에서* 참가자가 겪는 도덕적 고통을 실증한 연구. 이 자극 범주에 심리적 위험이 있다는 가장 강한 근거
- **NIMH, Ethical Issues in Post-Disaster Research** — [링크](https://www.nimh.nih.gov/funding/grant-writing-and-application-process/ethical-issues-to-consider-in-developing-evaluating-and-conducting-research-post-disaster) — 디스트레스 프로토콜과 디브리핑의 표준 참조
- **XR EMS 체계적 리뷰 2025** — 19편 중 **8편이 윤리 승인 문서 결여**. 이 분야의 알려진 약점이므로, 승인 절차를 꼼꼼히 기록하면 리뷰어에게 과분한 점수를 받는다

### P4(예상) 라벨의 취급

P4는 "소생 시도를 보류하는" 범주에 대응하므로 참가자에게 정서적으로 가장 무거운 자극이다.

- 파일럿에서 반응을 관찰한다
- 디스트레스가 관찰되면 **시나리오에서 제외**하고 P1을 늘린다
- 포함하기로 결정하면 사전 고지와 디브리핑에서 명시적으로 다룬다
- **오라벨 대상으로 절대 쓰지 않는다**

## 5. 개인정보 — PIPA 대응

### 현 아키텍처가 이미 방어적이다

`AGENTS.md` §3이 이미 선언하고 있다: *"영상 프레임은 Unity 또는 외부 네트워크로 전송하지 않는다."*

그리고 `config.py`의 `WebSocketConfig.__post_init__`이 호스트를 `127.0.0.1`/`localhost` 외의 값으로 설정하면 **ValueError로 거부한다.** 즉 외부 송출 차단이 선언이 아니라 코드로 강제되어 있다.

**이것을 설계상의 장점으로 논문과 IRB 문서 양쪽에 명시한다.** 사후 해명이 아니라 처음부터 그렇게 만들었다는 사실이 중요하다.

### 추가 조치

| 항목 | 조치 |
|---|---|
| 원본 영상 | **저장하지 않는다.** 프리뷰 창은 화면 표시만, 파일로 기록 금지 |
| 저장 데이터 | 관절 좌표(3개), 포인터 좌표, 상호작용 이벤트, 설문 응답 |
| 식별자 | 참가자 ID는 `P01`~`P24` 임의 배정. 이름·학번·이메일을 로그에 남기지 않는다 |
| 동의서 보관 | 로그와 물리적/논리적으로 분리 |
| 보존 기간 | 논문 게재 후 N년 (기관 규정), 이후 파기. 기간을 동의서에 명시 |
| 얼굴 | 관절 좌표에 얼굴 랜드마크를 포함하지 않는다 (현재 Pose 12/14/16만 사용 — 이미 충족) |

### 데모 영상 촬영 시

연구자 본인 또는 별도 동의를 받은 인물만 등장시킨다. 참가자 세션은 촬영하지 않는다. 촬영이 필요하면 별도 동의 항목을 만든다.

## 6. HoloLens 2 단계 추가 고려

| 항목 | 조치 |
|---|---|
| 위생 | 착용 전후 소독. 안면 패드 교체 또는 일회용 커버 |
| 시선 데이터 | 시선은 생체정보로 취급. 캘리브레이션 데이터 저장 금지, 시선 좌표만 익명 기록 |
| 멀미 | SSQ 사전/사후. 증상 보고 시 즉시 중단 |
| 장시간 착용 | 블록당 15분 제한, 사이 휴식 필수 |
| 사진 촬영 기능 | HL2의 카메라 기능을 비활성화하거나, 활성화 시 참가자에게 고지 |

## 7. 제출 전 확인

- [ ] 기관 IRB 신청서 초안
- [ ] 참가자 동의서 (철회권, 디스트레스, 데이터 처리 포함)
- [ ] 개인정보 처리방침 1페이지
- [ ] 시나리오 스크린샷 (비잔혹성 입증용)
- [ ] 디스트레스 대응 프로토콜 1페이지
- [ ] 논문 §1 배포 의도 고지 문단
- [ ] 논문 §3 "저작된 정답" 설명 문단
- [ ] 논문 §한계 — 의료 주장 부재 명시
- [ ] ⚠️ FDA 2026 CDS 가이던스 원문 확인 (인용할 경우에만)
