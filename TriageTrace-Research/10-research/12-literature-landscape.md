# 12. 문헌 지형과 공백

> 조사 시점 2026-09. 각 항목에 (a) 포화 (b) 활발 (c) 공백 평가를 붙인다.
> ⚠️ 표시는 원문 확인이 완료되지 않아 인용 전 직접 확인이 필요한 항목이다.

## 1. 자동 트리아지 / CV 기반 응급도 추정

### 1a. 영상·자세 기반 대량사상자 트리아지 — (b) 활발, 그러나 하드웨어 게이트

| 문헌 | 출처 | 핵심 |
|---|---|---|
| Automated UAS for Camera-Based Semi-Automatic Triage in MCIs | *Drones* 2024, [10.3390/drones8100589](https://www.mdpi.com/2504-446X/8/10/589) | 틸트윙 UAS + Jetson Nano, rPPG HR + 광학 흉부 호흡 + 자세 분류 → mSTaRT/PRIOR. **89% 정확도, F1 0.94**, 피험자 15명·30회, 12m/45°. 명시적 *semi*-automatic — 의사가 결정 보유 |
| Multimodal Human Mesh Recovery for Stand-off Triage | CMU RI MSR thesis 2025, [PDF](https://publications.ri.cmu.edu/storage/publications/2025/08/Aniket_Agarwal_MSR_Thesis.pdf) | RGB+LiDAR+IR SMPL 메시, 관절 회전 임계로 운동 각성도 3분류. MPJPE 86.9→75.8mm. **실제 DARPA 필드 데이터에서 15명 중 10명 정답, 오탐 3건(모두 Absent→Normal)**. Jetson Orin에서 ~1 FPS |
| Deep Learning-based Gait Recognition of the Wounded | *Disaster Med Public Health Prep* 2025, [Cambridge](https://www.cambridge.org/core/journals/disaster-medicine-and-public-health-preparedness/article/deep-learningbased-gait-recognition-and-evaluation-of-the-wounded/A6739A97819B8195C8A90AC299D75D87) | YOLOv5, 4,500장. **정상 97%, 절뚝임 70%** — "walking wounded" 자동 판별에 가장 근접한 시도이나 성능이 약함 |
| DARPA Triage Challenge | [darpa.mil](https://www.darpa.mil/research/challenges/darpa-triage-challenge), [Event 2 결과](https://www.darpa.mil/news/2025/dart-msai-triumph-darpa-triage-challenge) | 2023–2026. UAV/UGV standoff 검출 + 부상 평가. Event 2는 2025-09~10 Guardian Centers. Finals 2026 |
| Embodied AI for Emergency Care in Unstructured Environments | CMU PhD 2025, [PDF](https://www.ri.cmu.edu/app/uploads/2025/10/cgmorale_phd_robotics_2025.pdf) | ⚠️ 원문 미확인(파일 크기). 로봇 트리아지의 가장 완결된 서술로 추정 — 직접 읽을 것 |

**시사점.** 이 라인은 DARPA 자금, 다중 랩, 다중 센서, 필드 데이터 수집의 경쟁이다. 웹캠 파이프라인으로 경쟁 불가. 또한 프로토콜 불일치에 주목: 아무도 영상에서 START를 이름으로 쓰지 않는다. mSTaRT/PRIOR 또는 자체 휴리스틱을 쓴다.

### 1b. 비접촉 생체신호 — (a) 알고리즘은 포화, (c) 단시간·고심박·야외 강건성은 공백

| 문헌 | 출처 | 핵심 |
|---|---|---|
| Demographic bias in public rPPG datasets | *npj Digital Medicine* 2025, [링크](https://www.nature.com/articles/s41746-025-01973-9) | 100편 조사. UBFC-rPPG 26%, PURE 17%, COHFACE 11%. Monk 4–10 **25% 미만**. **고전 MAE 5.2→14.1 bpm, DL 6.0→9.5 bpm** |
| Reliability of rPPG under low illumination and elevated heart rates | *npj Digital Medicine* 2025, [링크](https://www.nature.com/articles/s41746-025-02192-y) | **조명은 거의 무관(830 vs 140 lux), 심박 상승이 치명적.** 8개 중 5개가 ~80bpm 초과에서 유의 저하. PhysNet Δmedian −13.55. POS 안정시 ~2.1 bpm MAE |
| Camera Measurement of Blood Oxygen Saturation | arXiv 2503.01699, [ar5iv](https://ar5iv.labs.arxiv.org/html/2503.01699) | 데이터셋 내 RMSE 1.69–3.24%. **캘리브레이션 의존**: 5프레임 5.85%, 270프레임 3.24%. 교차 데이터셋 실패 |
| Contactless vital sign algorithms for drone-based MCI triage | *Scientific Reports* 2026, [링크](https://www.nature.com/articles/s41598-026-40691-4) | 최상 사례: HR 97.7% 실내(RMSE 2.48bpm), RR 85.2/82.8%, SpO₂ 98.65/99.60%, BT 98.59%. 단 **HR 6.4%가 5bpm 이상 오차**, RR 창을 15초로 강제(권장 32초), 건강한 지원자만, 피부톤 다양성 없음 |
| Seconds Matter: Rapid Non-Contact HR/RR from Face Videos | *Sensors* 2026, [링크](https://www.mdpi.com/1424-8220/26/5/1506) | **단시간 측정 창**을 명시적으로 겨냥 — 트리아지에 유일하게 관련된 방향 |

**결정.** 이 영역에 진입하지 않는다. 대신 §3 관련연구에서 **"왜 진입하지 않았는가"의 근거로 인용**한다. 트리아지 대상은 정의상 빈맥·이동 중·다양한 피부톤이고, 문헌의 오차 막대는 평온한 좌위 지원자에게서 나온다.

### 1c. 열화상 / 외상 / 자세 / 의식 수준

| 문헌 | 출처 | 평가 |
|---|---|---|
| 드론 적외선 출혈 검출 | [PMC10693069](https://pmc.ncbi.nlm.nih.gov/articles/PMC10693069/) | 개념 증명 수준 |
| 열화상 vs rPPG 호흡수 비교 | [ScienceDirect](https://www.sciencedirect.com/science/article/pii/S2665917424006238) | 호흡수는 열화상이 대체로 우세 |
| 법의학 외상 분할·분류 | *Forensic Sci Med Pathol* 2023, [Springer](https://link.springer.com/article/10.1007/s12024-023-00668-5) | 근접 임상 영상에서만 동작 |
| 다중모달 상처 분류·심각도 추정 | 2026, [Springer](https://link.springer.com/article/10.1007/s42452-026-08719-6) | 근접 촬영, 장면 규모 아님 |
| **AVPU 자동 평가 (영상+음성)** | *Am J Emerg Med* 2023 | **가장 프로토콜 관련성 높음. 논문 2~3편뿐 — 얇음** |
| FHIR 연동 자동 AVPU | 2026, [PubMed 42332118](https://pubmed.ncbi.nlm.nih.gov/42332118/) | 진행 방향을 보여줌 |
| 무반응 환자의 은밀한 자발 안면 움직임 검출 | *Commun Med* 2025, [링크](https://www.nature.com/articles/s43856-025-01042-y) | ICU 환경, 현장 아님 |
| 낙상·자세 검출 | CVIU 2021, EAAI 2024 | **(a) 포화·상용화 수준. 쓰되 연구하지 말 것** |

## 2. XR 기반 MCI 트리아지 — **여기에 공백이 있다**

| 문헌 | 출처 | 핵심 |
|---|---|---|
| **Bridging Simulation and Reality: Augmented Virtuality for MCI Triage Training** | **CHI 2025**, [10.1145/3706598.3713794](https://dl.acm.org/doi/10.1145/3706598.3713794) | **가장 먼저 읽을 논문.** 126편 지형 분석 + 60명 실험(AV vs 전통 롤플레이 vs VR). AV와 VR 모두 전통 대비 우수. **AV의 촉각 통합이 신체적 몰입·만족도·트리아지 정확도를 유의하게 향상.** ⚠️ ACM DL 403 — 저자 순서와 수치는 PDF에서 재확인할 것 |
| **AR User Interfaces for First Responders: A Scoping Review** | arXiv 2506.09236 (2025-06), [링크](https://arxiv.org/abs/2506.09236) | **90편**, 6면 분류(EMS 55 / 소방 53 / 경찰 45; OST HMD 52; 공간형 76 vs HUD 40; 시각 90 / 청각 10 / 촉각 2). **트리아지 전용 인터페이스 라인 부재, off-FOV 안내 커버리지 부족, SA 체계적 평가 부재**를 명시 |
| ARTTS: Exploring AR Triage Tools to Support MCIs | **HFES 2022**, [10.1177/1071181322661337](https://doi.org/10.1177/1071181322661337) | 머리 착용 초기 분류 + 가상 SALT 워크스루 + 동적 가상 태그. **UX 설계 방법론 논문 — 성능 평가 없음** |
| Design and Evaluation of AR-based Adaptive Triage Training | **HFES 2024**, [PDF](https://midl.tamu.edu/data/papers/hfes_triage_2024.pdf) | HoloLens 2 적응형 SALT 훈련, 공공안전 인력 15명(12명 분석). **사후 퀴즈 +33%**, 외재부하 1.8 / 내재부하 4.41. **학습 지표이지 트리아지 판단 지표가 아님** |
| Simulations for AR Evaluation for MCI Triage | arXiv 2601.08186 (2026), [링크](https://arxiv.org/abs/2601.08186) | 컴퓨터 시뮬레이션과 현장 대응을 잇는 **단계적 시뮬레이션 전략** 제안. ⚠️ 초록만 확인. **데스크톱 전용 연구의 방법론적 정당화로 반드시 인용** |
| XR for EMS training: systematic review | *Front Disaster Emerg Med* 2025, [링크](https://www.frontiersin.org/journals/disaster-and-emergency-medicine/articles/10.3389/femer.2025.1630167/full) | 19편(1,077 스크리닝). VR 13 / AR 4 / MR 2, **10편(53%)이 MCI 트리아지**. 18/19가 2018년 이후. **8/19가 윤리 승인 문서 결여** |
| AR for Prehospital Emergency Care: Systematic Review of RCTs | *JMIR XR* [2025;1:e66222](https://xr.jmir.org/2025/1/e66222) | RCT 14편. 의사결정 정확도·훈련 성과 개선, 모의 대응시간 단축. ⚠️ 연도 충돌(저널 2025 vs Semantic Scholar 2024) |
| VR MCI 훈련 클러스터 | [BMC Digital Health 2024](https://link.springer.com/article/10.1186/s44247-024-00117-5), [PMC11811659](https://pmc.ncbi.nlm.nih.gov/articles/PMC11811659/), [PubMed 40082745](https://pubmed.ncbi.nlm.nih.gov/40082745/), [PubMed 40916379](https://pubmed.ncbi.nlm.nih.gov/40916379/) | 의학교육 저널 중심. **트리아지 정확도 지표가 여기 산다** |

### 이 문헌이 쓰는 지표

1. **트리아지 정확도** — 전문가 기준 대비 정분류율. 어디서나 주 종점
2. **과분류 / 저분류 비율** — **분리 보고가 표준.** 저분류가 안전 임계. 리뷰어가 분리를 기대한다
3. **대상당 시간 / 전체 현장 시간 / 처리율**
4. **학습 성과** — 사전/사후 퀴즈 델타, 자신감
5. **인지부하** — NASA-TLX, 또는 내재/외재/본유 3분할 (HFES 2024가 후자)
6. **상황인식** — **SAGAT**([Endsley](https://www.taylorfrancis.com/chapters/edit/10.4324/9781315087924-9/direct-measurement-situation-awareness-validity-use-sagat-mica-endsley))이 정본. 트리아지 간호사 대상 적용례([PLOS ONE 2025](https://journals.plos.org/plosone/article?id=10.1371%2Fjournal.pone.0318555)), 2026 교차도메인 메타리뷰([Human Factors](https://journals.sagepub.com/doi/10.1177/00187208251412110)). 스코핑 리뷰가 AR 대응 연구에서 SA 평가 부재를 지적 — **실제 구멍**
7. 사용성/수용성 — SUS, 프레즌스(IPQ/PQ), 시뮬레이터 멀미(SSQ)

### 평가

- VR 트리아지 **훈련**: **(a) 포화.** 의학교육 저널이 잘 덮고 있다. 학생 프로토타입이 보탤 것 없음
- AR 트리아지 **의사결정 지원 + 판단 결과 측정**: **(c) 공백.** 90편 스코핑 리뷰에 전용 인터페이스 연구 없음. 존재하는 2편(ARTTS 2022, Mohanty 2024)은 **인터페이스 사용의 결과로서 트리아지 정확도나 과/저분류를 보고하지 않는다.** 인용 가능한 실제 공백

## 3. 상호작용 기법 기준선 — (a) 포화

| 문헌 | 출처 | 핵심 |
|---|---|---|
| Gaze + pinch interaction in VR | **SUI 2017**, [10.1145/3131277.3132180](https://dl.acm.org/doi/10.1145/3131277.3132180) | 시선 조준 + 손 확정의 정본 분업. Vision Pro에 탑재됨 |
| Design Principles for Gaze and Pinch | 2024, [arXiv 2401.10948](https://arxiv.org/pdf/2401.10948) | 설계 원칙을 쓸 만큼 성숙했다는 신호 |
| Pinpointing: Head/Eye Target Selection for AR | **CHI 2018**, [PDF](https://3dvar.com/Kyt%C3%B62018Pinpointing.pdf) | 정밀 AR 선택의 기준점 |
| Eye&Head: Synergetic Eye and Head Movement | **UIST 2019**, [10.1145/3332165.3347921](https://dl.acm.org/doi/10.1145/3332165.3347921) | 결합 포인팅 표준 인용 |
| Bare-Hand Mid-Air Pointing in Dense VR | **CHI 2023 EA**, [10.1145/3544549.3585615](https://dl.acm.org/doi/10.1145/3544549.3585615) | **밀집 환경** — 다수 환자 중 선택의 최근접 유사 사례 |
| Dwell 임계 계보 | [CHI 2017](https://dl.acm.org/doi/abs/10.1145/3025453.3025517), [NordiCHI 2004](https://dl.acm.org/doi/10.1145/1028014.1028045), [GazeIntent 2024](https://arxiv.org/html/2404.13829v1), [CONTEXT-GAD VRST 2025](https://dl.acm.org/doi/10.1145/3756884.3766048), [Multi-Threshold Dwell CHI 2025](https://dl.acm.org/doi/10.1145/3706598.3713781), [동적 dwell 메타분석 HCI 2025](https://www.tandfonline.com/doi/full/10.1080/07370024.2025.2497236) | **고정 dwell은 20년 전에 지나갔다.** 현재는 적응형/동적/의도모델/다중임계 |
| Head, Gaze, or Finger? (저시력 AR 선택) | [arXiv 2607.06778](https://arxiv.org/html/2607.06778) | Quest 3 + Pupil Labs Neon. **"dwell 임계는 선행 문헌에 따라 0.8초로 설정"** ⚠️ 게재 venue 미확인 |

### 여기서 나오는 결론 3가지

1. **0.7초 dwell은 이미 문헌 기본값이다.** 정당화는 불필요하고 주장도 불가능하다
2. **dwell 시간 비교는 2026년에 기여가 아니다.** CHI-tier에서는 알려진 속도–정확도 교환의 재현으로 데스크리젝트 대상. 큰 논문 안의 설계 근거 파일럿으로만 허용
3. **얇은 빈틈 하나:** *사람*을 대상으로 한 선택 문헌을 찾지 못했다. 군중 속 신체 중 하나를 고르는 것, 신체에 레이를 쏘는 행위의 사회적·주의적 결과에 대한 연구가 없다. 논문 전체를 걸기엔 얇지만 차별점은 된다

## 4. 불확실성 시각화 / 적정 의존 — 채택 기여의 이론적 배경

| 문헌 | 출처 |
|---|---|
| Trusting AI: does uncertainty visualization affect decision-making? | [Frontiers Comp Sci 2025](https://www.frontiersin.org/journals/computer-science/articles/10.3389/fcomp.2025.1464348/full) |
| Uncertainty Aware Task Delegation and Human-AI Collaborative Decision-Making | [FAccT 2025](https://dl.acm.org/doi/10.1145/3715275.3732155) |
| Confirmation bias in AI-assisted decision-making: AI triage recommendations | [ScienceDirect](https://www.sciencedirect.com/science/article/pii/S2949882124000264) |
| Risks of automation bias in healthcare AI: Bowtie analysis | [ScienceDirect](https://www.sciencedirect.com/science/article/pii/S2666449624000410) |

**(b) 활발하되 공간 AR 트리아지에 미적용.** 성숙한 이론 + 미개척 도메인 = 이 연구의 자리.

## 5. 종합 — 우리가 서 있는 좌표

```
자동 트리아지 추정 ────── (b) DARPA급, 진입 불가
비접촉 생체신호 ───────── (a) 포화 / (c) 단시간·고심박은 공백이나 센싱 문제
자세·낙상 검출 ────────── (a) 상용 수준, 도구로 사용
영상 AVPU ────────────── (b) 얇음, 흥미롭지만 실제 사람 필요
VR 트리아지 훈련 ───────── (a) 포화
▶ AR 트리아지 의사결정 지원 + 판단 결과 측정 ── (c) 공백 ◀  ← 여기
사람 대상 선택 기법 ────── (c) 공백이나 얇음
dwell 시간 ────────────── (a) 완전 포화
불확실성 시각화 ────────── (b) 성숙, AR 트리아지 미적용 ← 여기의 이론 배경
```

## 6. 반드시 원문 확인할 것 (인용 전)

- [ ] CHI 2025 *Bridging Simulation and Reality* — 저자 순서, 트리아지 정확도 수치
- [ ] arXiv 2506.09236 스코핑 리뷰 — 제1저자명(자동 추출이 "Argo"로 읽었으나 미확인)
- [ ] arXiv 2601.08186 Nelson et al. — 본문 전체
- [ ] arXiv 2607.06778 — 게재 venue와 연도
- [ ] JMIR XR AR-prehospital RCT 리뷰 — 연도 충돌 해소
- [ ] CMU Morales 2025 PhD thesis — 본문
