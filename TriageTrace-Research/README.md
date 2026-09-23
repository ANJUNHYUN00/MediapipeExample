# Triage Trace — 연구 확장 문서 세트

> 작성일: 2026-09-16 · 대상 저장소: `C:\Projects\MediapipeExample`
> 이 폴더는 **연구·집필용 문서 전용**이다. 코드는 기존 저장소에 그대로 두고, 여기서는 기획·설계·일정·검증만 관리한다.

## 이 폴더가 존재하는 이유

기존 `MediapipeExample` 저장소는 **작동하는 인터랙션 프로토타입**까지 도달해 있다. 그러나 프로토타입은 논문이 아니다. 이 문서 세트는 "만들었다"를 "무엇을 새로 알아냈다"로 바꾸기 위한 계획을 담는다.

핵심 전략 한 줄:

> **응급도 추정기를 만들지 않는다. 불완전한 응급도 추정을 어떻게 보여줄 것인가를 연구한다.**

이유는 [`00-overview/03-strategic-judgment.md`](./00-overview/03-strategic-judgment.md)에 있다. 이 판단에 동의하지 않는다면 다른 문서를 읽기 전에 그 문서부터 읽을 것.

## 읽는 순서

**처음 읽는다면** → `00-overview` 전체 → `10-research/11` → `30-planning/31`

**이번 주에 뭘 해야 하는지만 알고 싶다면** → [`40-checklists/41-toolchain-archive.md`](./40-checklists/41-toolchain-archive.md) → [`30-planning/32-task-backlog.md`](./30-planning/32-task-backlog.md)

**지도교수/심사자에게 설명해야 한다면** → `00-overview/01` + `10-research/11` + `10-research/13`

## 문서 목차

### 00-overview — 프로젝트 정의와 판단

| 문서 | 내용 |
|---|---|
| [`01-project-definition.md`](./00-overview/01-project-definition.md) | 프로젝트 재정의, 비의료 경계, 연구 범위 |
| [`02-current-state.md`](./00-overview/02-current-state.md) | **지금까지 구현된 것 전부** (실제 코드 확인 기준) |
| [`03-strategic-judgment.md`](./00-overview/03-strategic-judgment.md) | 방향 판단, 무엇이 기여이고 무엇이 아닌가 |

### 10-research — 연구 설계

| 문서 | 내용 |
|---|---|
| [`11-research-questions.md`](./10-research/11-research-questions.md) | RQ, 가설, 기여 3개 |
| [`12-literature-landscape.md`](./10-research/12-literature-landscape.md) | 문헌 지형과 공백 (출처 포함) |
| [`13-experiment-design.md`](./10-research/13-experiment-design.md) | 조건, 참가자, 과업, 지표, 분석 계획 |
| [`14-severity-cue-spec.md`](./10-research/14-severity-cue-spec.md) | 응급도 단서 레이어 사양 + 정답 라벨 규칙 |
| [`15-ethics-and-irb.md`](./10-research/15-ethics-and-irb.md) | IRB, PIPA, 의료기기 주장 회피 |

### 20-engineering — 기술 사양

| 문서 | 내용 |
|---|---|
| [`21-current-architecture.md`](./20-engineering/21-current-architecture.md) | 현 시스템 기술 스펙 (실제 코드 기준) |
| [`22-instrumentation-spec.md`](./20-engineering/22-instrumentation-spec.md) | Level 1 — 로깅 스키마, 실험 모드 |
| [`23-uncertainty-hud-spec.md`](./20-engineering/23-uncertainty-hud-spec.md) | Level 2 — 신뢰도 조작 + HUD 3조건 |
| [`24-hololens2-port-plan.md`](./20-engineering/24-hololens2-port-plan.md) | HL2 이식 계획 + 툴체인 동결 대응 |

### 30-planning — 일정과 관리

| 문서 | 내용 |
|---|---|
| [`31-timeline.md`](./30-planning/31-timeline.md) | 주차별 일정 2026-09 ~ 2027-03 |
| [`32-task-backlog.md`](./30-planning/32-task-backlog.md) | 태스크 목록 (ID·산출물·수용기준·의존성) |
| [`33-venues-and-deadlines.md`](./30-planning/33-venues-and-deadlines.md) | 학회 마감 트래커 |
| [`34-risk-register.md`](./30-planning/34-risk-register.md) | 리스크 등록부 |

### 40-checklists — 실행 체크리스트

| 문서 | 내용 |
|---|---|
| [`41-toolchain-archive.md`](./40-checklists/41-toolchain-archive.md) | **이번 주 필수** — HL2 툴체인 아카이브 |
| [`42-demo-verification.md`](./40-checklists/42-demo-verification.md) | 시스템/데모 검증 |
| [`43-submission-evidence.md`](./40-checklists/43-submission-evidence.md) | 투고 전 증거물 |

## 지금 당장 중요한 3가지

1. **Unity 버전 충돌이 이미 존재한다.** 현재 프로젝트는 `6000.3.10f1`인데 HoloLens 2 지원 상한은 `6000.0.49f1`이다. HL2 이식은 업그레이드가 아니라 **별도 프로젝트 분기 + 다운그레이드**다. → [`24-hololens2-port-plan.md`](./20-engineering/24-hololens2-port-plan.md)
2. **HL2 툴체인 다운로드 링크가 사라지기 전에 받아둬야 한다.** → [`41-toolchain-archive.md`](./40-checklists/41-toolchain-archive.md)
3. **11~12월 마감은 데스크톱 버전으로 간다.** HL2는 3월 ISMAR용이다. → [`31-timeline.md`](./30-planning/31-timeline.md)

## 문서 규칙

- 확인하지 않은 것을 완료로 쓰지 않는다. 미확인 항목은 `⚠️ 미확인`으로 표시한다.
- 외부 사실(학회 마감, SDK 상태)은 출처 URL과 확인 시점을 함께 적는다.
- 의료적 효과를 주장하는 문장은 쓰지 않는다. 허용 표현은 [`15-ethics-and-irb.md`](./10-research/15-ethics-and-irb.md) 참조.
