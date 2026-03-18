# Pipeline Automation — Autonomous Game Factory v2.3

이 문서는 파이프라인 자동화 캡빌리티의 상세 명세를 정의한다.

---

## 1. 자동화 캡빌리티 총괄

| # | 캡빌리티 | 자동화 수준 | 사람 개입 |
|---|----------|-------------|-----------|
| CAP1 | Architecture Knowledge Memory | 완전 자동 | 패턴 확인 선택적 |
| CAP2 | Automated Regression Guardian | 완전 자동 | critical 시 필수 |
| CAP3 | Pipeline Self-Healing Engine | 반자동 (Dry Run 지원) | Apply 시 확인 권장 |
| CAP4 | Intelligent Feature Decomposer | 완전 자동 | 분해 결과 확인 권장 |
| CAP5 | Game Factory Control Panel | UI 도구 | 사람이 버튼 클릭 |

---

## 2. CAP1 — Architecture Knowledge Memory

### 2.1 목적
파이프라인 실행 중 발견된 아키텍처 교훈을 영구 저장하여, 동일한 실수를 반복하지 않도록 한다.

### 2.2 저장소 구조

```
docs/ai/architecture_memory/
├── ARCHITECTURE_PATTERNS.yaml   # 검증된 안전 패턴 (AP-001~)
├── ANTI_PATTERNS.yaml           # 위험 패턴 (ANTI-001~)
├── MODULE_EVOLUTION_LOG.md      # 모듈 변경 이력
└── ARCHITECTURE_DECISIONS.md    # ADR (Architecture Decision Records)
```

### 2.3 자동화 흐름

```
Builder 코드 생성
  ↓
Validator 실행 → 위반 감지
  ↓
anti_pattern 매칭 → occurrence_count++
  ↓
Learning Recorder → ARCHITECTURE_PATTERNS.yaml 업데이트
  ↓
다음 파이프라인 실행 시 → Builder가 패턴/안티패턴 참조
```

### 2.4 API

```csharp
// 패턴 로드
KnownPattern[] patterns = ArchitectureKnowledgeMemory.LoadPatterns();

// 안티패턴 로드
AntiPattern[] anti = ArchitectureKnowledgeMemory.LoadAntiPatterns();

// 코드 스캔
List<string> violations = new List<string>();
ArchitectureKnowledgeMemory.ScanCodeForAntiPatterns(filePath, anti, violations);

// 진화 로그 추가
ArchitectureKnowledgeMemory.AppendEvolutionEntry(
    "Economy", "extend", "AddCurrency 메서드 추가",
    "다중 재화 지원", "ShopSystem", "low", "AI");
```

---

## 3. CAP2 — Automated Regression Guardian

### 3.1 감지 항목

| 감지 | 심각도 | 차단 |
|------|--------|------|
| 의존성 깨짐 (없는 모듈 참조) | critical | 커밋 차단 |
| 인터페이스 파일 누락 | high | 경고 |
| 빈 인터페이스 (의존자 존재) | high | 경고 |
| Runtime 파일 누락 | high | 경고 |
| Factory 파일 누락 | medium | 경고 |

### 3.2 실행 시점

1. **Validator 파이프라인 내** — `RegressionGuardianValidator`가 자동 실행
2. **수동 실행** — `Tools/AI/Run Regression Guardian` 메뉴
3. **Control Panel** — Regression Guardian 버튼

### 3.3 커밋 차단 규칙

`RegressionReport.ShouldBlock == true`이면:
- Validator가 blocking error를 보고
- Committer Gate 1 (Validation Report)에서 차단

---

## 4. CAP3 — Pipeline Self-Healing Engine

### 4.1 수정 가능 대상 (메타데이터만)

| 대상 | 수정 내용 |
|------|-----------|
| MODULE_REGISTRY.yaml | 누락된 모듈 엔트리 자동 추가 |
| TASK_QUEUE.yaml | 잘못된 status 값 → pending으로 리셋 |
| 의존성 순서 | 순환 감지 + 리포트 (자동 수정 불가) |

### 4.2 절대 금지

- **모듈 코드 수정 금지** — Runtime, Config, Factory 등 `.cs` 파일 절대 건드리지 않음
- **Git 작업 금지** — commit, push, reset 등 금지
- **상태 전이 우회 금지** — status를 done으로 직접 변경하는 것 금지

### 4.3 실행 모드

| 모드 | 설명 |
|------|------|
| Dry Run | 문제 감지만, 수정 없음 |
| Apply Fixes | 감지된 문제를 실제 수정 |

---

## 5. CAP4 — Intelligent Feature Decomposer

### 5.1 알고리즘

```
1. 게임 디자인 설명 입력 (자연어)
2. 키워드 추출 → 소문자 변환 → 단어 분리
3. 8개 시스템 템플릿과 키워드 매칭
4. KEYWORD_MATCH_THRESHOLD (0.3) 이상인 시스템 선별
5. 각 시스템의 서브모듈 목록 생성
6. MODULE_REGISTRY 스캔 → 기존 모듈 존재 여부 확인
7. 존재하면 "reuse_or_extend", 없으면 "create_new"
8. 의존성 그래프 자동 추론
9. 분해 결과 출력
```

### 5.2 지원 시스템 템플릿

| 시스템 | 키워드 예시 | 서브모듈 |
|--------|-------------|----------|
| Economy | gold, gem, shop, wallet | CurrencyWallet, ShopSystem, PriceCalculator |
| Combat | attack, damage, health | HealthSystem, DamageCalculator, CombatCore |
| Inventory | item, slot, equip | InventorySystem, ItemStacking, EquipmentSlot |
| Progression | level, xp, upgrade | LevelSystem, ExperienceTracker, UpgradeManager |
| Quest | quest, mission, reward | QuestTracker, ObjectiveEvaluator, RewardDistributor |
| UI | hud, menu, popup | UIManager, ScreenNavigation, PopupSystem |
| Audio | sound, music, sfx | AudioCore, SFXPlayer, MusicPlayer |
| Save | save, load, persist | SaveSystem, DataSerializer |

### 5.3 사용 예시

입력:
```
RPG 게임의 경제 시스템. 골드와 젬 두 종류의 재화가 있고,
상점에서 아이템을 구매/판매할 수 있다.
인벤토리에 아이템이 저장되고, 장비를 장착할 수 있다.
```

결과:
```
Game
 → Economy (score: 0.45)
   → CurrencyWallet [NEW]
   → ShopSystem [NEW]
   → PriceCalculator [NEW]
 → Inventory (score: 0.38)
   → InventorySystem [EXISTS — reuse_or_extend]
   → ItemStacking [EXISTS — reuse_or_extend]
   → EquipmentSlot [NEW]
```

---

## 6. CAP5 — Game Factory Control Panel

### 6.1 접근

`Tools/AI/Game Factory Control Panel` 메뉴로 열기.

### 6.2 표시 정보

| 섹션 | 내용 |
|------|------|
| Pipeline Status | Idle / In Progress / Blocked / Complete |
| Task Queue State | Pending/Planned/InProgress/Done/Blocked 카운트 + 각 태스크 상세 |
| Actions | Run Validators, Diff Analyzer, Regression Guardian, Self-Heal, Module Reuse, Architecture Knowledge |
| Validation Results | 최근 검증 결과 (PASSED/FAILED + 에러/경고 수) |
| Intelligent Decomposer | 게임 디자인 설명 입력 → 분해 결과 |
| Commit Readiness | 커밋 가능 여부 (READY / NOT READY) |

### 6.3 색상 코딩

| 상태 | 색상 |
|------|------|
| done | 초록 |
| in_progress | 노랑 |
| blocked | 빨강 |
| pending/planned | 기본 |
| READY TO COMMIT | 초록 |
| NOT READY | 빨강 |

---

## 7. 파이프라인 자동화 예시

### 7.1 시나리오: "RPG 전투 + 레벨업 시스템 만들어줘"

```
1. [Feature Intake]
   → FEATURE_QUEUE.yaml에 "RPG 전투 + 레벨업" 추가

2. [Intelligent Decomposer]
   → Combat 시스템 매칭 (score: 0.52)
   → Progression 시스템 매칭 (score: 0.41)
   → 6개 모듈 제안: HealthSystem, DamageCalculator, CombatCore,
                     LevelSystem, ExperienceTracker, UpgradeManager

3. [Queue Generator]
   → TASK_QUEUE에 6개 태스크 추가 (status: pending)
   → MODULE_REGISTRY에 6개 모듈 등록

4. [Architecture Diff Analyzer]
   → 각 모듈 diff 분석
   → 모두 create_new → overall_risk: low
   → 통과

5. [Orchestrator + Planner]
   → 의존성 정렬: HealthSystem → DamageCalculator → CombatCore
                   LevelSystem → ExperienceTracker → UpgradeManager
   → 6개 PLAN 생성

6. [Builder]
   → 각 모듈 6파일 생성 (36파일 총)

7. [Validator Pipeline]
   → 16 Validators 실행
   → Regression Guardian: 기존 모듈 영향 없음
   → PASSED

8. [Human Gate]
   → 사람이 Unity에서 확인 + validated

9. [Reviewer]
   → commit_state: ready

10. [Committer]
    → 7 Gate 통과
    → feat(combat): add HealthSystem, DamageCalculator, CombatCore modules
    → feat(progression): add LevelSystem, ExperienceTracker, UpgradeManager modules

11. [Learning Recorder]
    → architecture_memory/ 업데이트
    → 6개 모듈 진화 로그 추가
```

---

## 8. 참조 문서

| 문서 | 내용 |
|------|------|
| `ORCHESTRATION_RULES.md` | 파이프라인 오케스트레이션 |
| `ARCHITECTURE_DIFF_ANALYZER.md` | Diff Analyzer 명세 |
| `PIPELINE_HARDENING.md` | 파이프라인 강화 |
| `LEARNING_SYSTEM.md` | Learning Recorder 강화 |
| `CONFIG_RULES.md` | Config Source-of-Truth |
| `TASK_SCHEMA.md` | TASK_QUEUE 스키마 |
| `MODULE_TEMPLATES.md` | 모듈 6파일 구조 |
| `CODING_RULES.md` | C# 코딩 규칙 |
