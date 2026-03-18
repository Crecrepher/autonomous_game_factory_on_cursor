# Queue Generator — Autonomous Game Factory v2

사용자의 Feature 요청을 받아 구조화된 TASK_QUEUE 엔트리와 MODULE_REGISTRY 엔트리를 생성하는 에이전트의 행동 명세다.

---

## 1. 역할 정의

Queue Generator는 Feature Intake → Decomposition → Queue/Registry 등록까지의 **전처리 파이프라인 전체**를 실행한다.

```
사용자 요청 (자연어 / 디자인 문서)
 ↓ [A] Feature Intake — 구조화된 Feature 엔트리 생성
 ↓ [B] Learning Scan — 과거 실수/규칙 확인
 ↓ [B.5] Module Discovery — 기존 모듈 탐색 + 유사도 분석 (★ v2.1)
 ↓ [B.7] Reuse Decision — 재사용/확장/교체/신규 결정 (★ v2.1)
 ↓ [C] Decomposition — 모듈 목록, 의존성, 책임 분해 (Discovery 결과 반영)
 ↓ [D] Dependency Inference — 모듈 간 의존 순서 결정
 ↓ [E] Risk Annotation — 위험 플래그, 경고 주석
 ↓ [F] Queue Entry Generation — TASK_QUEUE.yaml 엔트리 생성 (integration_strategy 포함)
 ↓ [G] Registry Entry Generation — MODULE_REGISTRY.yaml 엔트리 생성
 ↓ [H] Spec Generation — generated_specs/<Module>_SPEC.md 생성
 ↓ [H.5] ★ Architecture Diff Analysis — 아키텍처 위험 분석 (★ v2.3)
 ↓ [I] Output Report — 산출물 요약
```

---

## 2. 입력 계약 (Input Contract)

### 2.1 지원 입력 유형

| 유형 | 설명 | 예시 |
|------|------|------|
| 자연어 한 줄 | 기능 요약 | "플레이어 재화 시스템: 골드/젬, 추가/소비/조회" |
| 기능 목록 | 시스템 나열 | "Combat core: health, damage, detection, cooldown, buff" |
| 디자인 브리프 | 게임플레이 설명 | "적 탐지 → 공격 → 피해 → 체력 감소 → 사망/회복" |
| 기획 문서 | 상세 명세 | PROJECT_OVERVIEW.md 또는 별도 문서 |
| FEATURE_QUEUE 엔트리 | 기존 intake 항목 | FEATURE_QUEUE.yaml의 status: intake 엔트리 |

### 2.2 최소 필수 정보

Queue Generator가 작동하려면 입력에서 다음을 추출 또는 추론할 수 있어야 한다:

| 필드 | 필수 | 추론 가능 | 설명 |
|------|------|-----------|------|
| feature_name | O | X | 기능 이름 |
| description | O | X | 기능 설명 (1줄 이상) |
| feature_group | △ | O | 커밋 그룹 slug. 없으면 feature_name에서 추론 |
| priority | △ | O | high/medium/low. 기본값: medium |
| modules | X | O | 모듈 목록. Queue Generator가 분해 |
| constraints | X | X | 명시적 제약 (선택) |
| references | X | X | 참조 문서 (선택) |

### 2.3 입력 예시

**자연어 한 줄:**
```
"I want a player currency system with gold/gems, add/spend/query"
```

**기능 목록:**
```
"Combat core: health, damage, enemy detection, cooldown, buff/debuff"
```

**구조화된 YAML:**
```yaml
- name: "Currency System"
  description: |
    플레이어 재화 관리. 골드와 젬 두 종류.
    추가(Add), 소비(Spend), 잔고 조회(Query) API.
    최대 잔고 제한. 소비 시 잔고 부족 처리.
  priority: high
  feature_group: "currency-economy"
  constraints:
    - "기존 Economy 모듈이 있으면 의존성으로 연결"
    - "UI 모듈은 이 feature에 포함하지 않음"
```

---

## 3. 분해 규칙 (Decomposition Rules)

### 3.1 기본 원칙

| # | 원칙 | 설명 |
|---|------|------|
| D-1 | 단일 책임 | 한 모듈 = 한 가지 핵심 책임 |
| D-2 | God-module 금지 | GameplaySystem, UniversalManager, CombatManagerEverything 등 거대 모듈 금지 |
| D-3 | 기존 모듈 우선 | MODULE_REGISTRY.yaml에 이미 존재하는 모듈은 새로 만들지 않음 |
| D-4 | 이름 기반이 아닌 책임 기반 | "Combat"이라고 하나의 모듈이 되는 게 아님. 책임별로 분리 |
| D-5 | 최대 10개 | feature당 모듈 10개 초과 시 sub-feature로 분할 |
| D-6 | 6파일 템플릿 필수 | 각 모듈은 I*.cs, *Config.cs, *Runtime.cs, *Factory.cs, *Bootstrap.cs, Tests/Editor/*Tests.cs |

### 3.2 분해 절차 (v2.1 — Module Discovery 통합)

```
1. 입력에서 핵심 기능(capability)을 추출한다
2. ★ [B.5] Module Discovery 실행:
   a. MODULE_REGISTRY.yaml 전체 로드
   b. Assets/Game/Modules/ 디렉토리 스캔
   c. 각 기능에 대해 기존 모듈 유사도 분석
      - 이름 유사도, 인터페이스 유사도, 책임 유사도, 의존성 패턴
   d. similarity_score >= 0.4 인 후보 목록 생성
   상세: MODULE_DISCOVERY.md
3. ★ [B.7] Reuse Decision 실행:
   a. 후보별 integration_strategy 결정
      - similarity >= 0.9: reuse (중복 생성 차단)
      - similarity >= 0.7: extend 또는 adapt 우선
      - similarity >= 0.4: create_new (참조로만 기록)
      - 후보 없음: create_new
   b. replace 전략은 Impact Analysis + Migration Plan 필수
   상세: INTEGRATION_STRATEGY.md
4. Decomposition (Discovery 결과 반영):
   a. reuse 결정된 기능 → 신규 모듈 생성하지 않음, 의존성으로 연결
   b. extend 결정된 기능 → 기존 모듈 수정 태스크 생성
   c. adapt 결정된 기능 → 어댑터 모듈 태스크 생성
   d. replace 결정된 기능 → 새 모듈 + Migration Plan 포함 태스크 생성
   e. create_new → 표준 신규 모듈 태스크 생성
5. 모듈 이름을 PascalCase로 결정한다
6. 각 모듈의 책임(Responsibility)을 한 줄로 정의한다
7. 모듈 경로를 Assets/Game/Modules/<ModuleName>/ 으로 설정한다
8. 6파일 deliverables를 자동 산출한다
```

### 3.3 분해 예시

**입력**: "Combat core: health, damage, enemy detection, cooldown, buff/debuff"

```
추출된 기능(capability):
  1. 체력 관리 (HP 추적, 피해 적용, 사망 판정)
  2. 데미지 계산 (데미지 값, 감쇠, 크리티컬)
  3. 적 탐지 (범위 기반 탐색, 타겟 선택)
  4. 쿨다운 관리 (스킬/공격 쿨다운 타이머)
  5. 버프/디버프 (상태 효과 적용/제거/틱)

모듈 매핑:
  1. HealthSystem — 체력 관리
  2. DamageSystem — 데미지 계산 → 의존: HealthSystem
  3. EnemyDetection — 적 탐지
  4. CooldownSystem — 쿨다운 관리
  5. BuffSystem — 버프/디버프 → 의존: StatusEffect (기존)
```

**입력**: "I want a player currency system with gold/gems, add/spend/query"

```
추출된 기능:
  1. 재화 지갑 (잔고 추적, 추가, 소비, 조회)

모듈 매핑:
  1. CurrencyWallet — 재화 지갑
     → 의존: Economy (기존 모듈이 있으면)
     
참고: 골드/젬은 Config에서 재화 종류로 정의, 별도 모듈이 아님.
단일 CurrencyWallet 모듈이 다중 재화를 Config 기반으로 관리.
```

### 3.4 분해 안티패턴

| 안티패턴 | 문제 | 올바른 분해 |
|---------|------|------------|
| `CombatSystem` (모든 전투 로직) | God-module | Health, Damage, Detection, Cooldown, Buff 분리 |
| `GoldSystem` + `GemSystem` | 데이터 차이를 모듈로 분리 | CurrencyWallet 하나 + Config에서 타입 구분 |
| `GameManager` | 모든 걸 관리 | 책임별 모듈 분리 |
| `Utils` | 범용 유틸 모듈 | Shared/ 폴더에 인터페이스로 |
| `CombatUI` (비즈니스 모듈에 UI 혼합) | 관심사 분리 위반 | UI는 별도 UI 모듈 |

---

## 4. 의존성 추론 규칙 (Dependency Inference)

### 4.1 추론 절차

```
1. 각 모듈의 책임을 분석한다
2. "이 모듈이 동작하려면 어떤 데이터/서비스가 필요한가?"를 판단한다
3. 필요한 데이터/서비스를 제공하는 모듈을 의존성으로 추가한다
4. MODULE_REGISTRY.yaml에서 기존 모듈 확인 — 존재하면 의존으로 연결
5. 순환 의존 검사 수행
```

### 4.2 의존성 판단 기준

| 관계 | 의존 방향 | 예시 |
|------|-----------|------|
| A가 B의 데이터를 소비 | A → B | DamageSystem → HealthSystem (HP 값 변경) |
| A가 B의 이벤트에 반응 | A → B | BuffSystem → StatusEffect (상태 이벤트) |
| A가 B의 결과를 사용 | A → B | SellFlow → Economy (재화 추가) |
| A와 B가 독립적 | 의존 없음 | CooldownSystem ↔ EnemyDetection |

### 4.3 의존성 안전 규칙

| # | 규칙 |
|---|------|
| 1 | 순환 의존 절대 금지 (A→B→A, A→B→C→A) |
| 2 | 공통 계약 필요 시 Shared 인터페이스로 의존 역전 |
| 3 | 의존은 MODULE_REGISTRY.yaml의 dependencies에 선언된 것만 허용 |
| 4 | UnityEngine, System은 의존 목록에서 필터링 (TASK_QUEUE에 포함하지 않음) |
| 5 | 기존 done 모듈에 대한 의존은 허용 (이미 완료된 모듈) |
| 6 | blocked/escalated 모듈에 대한 의존은 경고 플래그 추가 |

### 4.4 의존성 추론 예시

**Combat Core:**
```
HealthSystem     → []                    (독립)
DamageSystem     → [HealthSystem]        (HP 변경)
EnemyDetection   → []                    (독립)
CooldownSystem   → []                    (독립)
BuffSystem       → [StatusEffect]        (기존 모듈 의존)
```

**Farming Loop:**
```
InventorySystem  → []                    (독립)
CropGrowth       → []                    (독립)
HarvestSystem    → [CropGrowth, InventorySystem]  (수확 → 인벤토리)
SellFlow         → [InventorySystem, Economy]      (판매 → 재화)
```

---

## 5. feature_group 추론 규칙

### 5.1 추론 절차

```
1. 사용자가 명시했으면 그대로 사용
2. 명시하지 않았으면 feature_name에서 추론:
   a. 공백/밑줄을 하이픈으로 변환
   b. 소문자로 변환
   c. 핵심 키워드 2~3개로 축약
```

### 5.2 추론 예시

| feature_name | 추론된 feature_group |
|-------------|---------------------|
| "Combat Core" | `combat-core` |
| "Currency System" | `currency-economy` |
| "Farming Progression Loop" | `farming-loop` |
| "Player Upgrade System" | `player-upgrade` |
| "Shop and IAP" | `shop-iap` |

### 5.3 규칙

- feature_group은 **소문자 + 하이픈**만 사용
- 3단어 이하로 유지
- 동일 feature_group이 이미 존재하면 접미사 추가 (`combat-core-v2`)
- feature_group은 커밋 단위이므로 의미적으로 하나의 기능을 대표해야 함

---

## 6. 위험 주석 규칙 (Risk Annotation)

### 6.1 Queue Generator는 다음을 확인하여 risk 플래그를 설정한다

| 조건 | risk | 이유 |
|------|------|------|
| Core 폴더에 접근 필요 | high | editable: false |
| 기존 done 모듈 수정 필요 | high | 사후 재커밋 필요 |
| 의존 모듈이 blocked/escalated | medium | 진행 차단 가능 |
| 새 Shared 인터페이스 추가 필요 | medium | 다른 모듈에 영향 |
| 모듈 수 5개 초과 | medium | 복잡도 증가 |
| RECURRING_MISTAKES.md에 관련 패턴 존재 | medium | 과거 반복 실수 영역 |
| 독립 모듈, 의존 없음, 단순 구조 | low | 안전 영역 |

### 6.2 Learning Memory 기반 위험 플래그

Queue Generator는 TASK_QUEUE 생성 전에 **반드시** 다음을 읽는다:

| 파일 | 참조 목적 |
|------|-----------|
| `learning/RULE_MEMORY.yaml` | 관련 규칙 위반 가능성 확인 |
| `learning/RECURRING_MISTAKES.md` | 이 모듈 유형에 반복 실수가 있는지 확인 |
| `learning/VALIDATOR_FAILURE_PATTERNS.md` | 유사 모듈에서 발생한 검증 실패 패턴 확인 |

결과를 각 TASK_QUEUE 엔트리의 `notes` 또는 별도 `risk_flags` 필드에 기록한다.

### 6.3 위험 주석 예시

```yaml
- name: BuffSystem
  risk: medium
  notes: >
    Auto-generated from feature: Combat Core.
    [RISK] RECURRING_MISTAKES REC-001: Runtime에 MonoBehaviour 상속 주의.
    [RISK] 의존 모듈 StatusEffect가 기존 done 상태 — 인터페이스만 참조할 것.
    [LEARNING] RM-0003: using 참조 시 MODULE_REGISTRY dependencies 먼저 추가.
```

---

## 7. 수락 기준 생성 규칙 (Acceptance Criteria)

### 7.1 자동 생성 기준

모든 모듈에 공통으로 적용되는 수락 기준:

```yaml
acceptance_criteria:
  structure:
    - "I<Module>.cs 인터페이스 존재"
    - "<Module>Config.cs (ScriptableObject 상속)"
    - "<Module>Runtime.cs (MonoBehaviour 상속 금지)"
    - "<Module>Factory.cs (static class)"
    - "<Module>Bootstrap.cs (MonoBehaviour, 얇게)"
    - "Tests/Editor/<Module>Tests.cs (최소 2개 테스트)"
  coding:
    - "namespace Game"
    - "foreach 없음"
    - "LINQ 없음"
    - "코루틴 없음"
    - "매직넘버 없음 (const UPPER_SNAKE)"
    - "GetComponent 런타임 사용 없음"
  architecture:
    - "MODULE_REGISTRY.yaml에 등록됨"
    - "dependencies에 선언된 모듈만 using"
    - "순환 의존 없음"
  validation:
    - "12개 Validator 전체 PASS"
    - "Human Validation Gate 통과"
```

### 7.2 도메인별 추가 기준

Queue Generator는 모듈의 책임에 따라 도메인별 기준을 추가한다:

```yaml
# 예: HealthSystem
domain_criteria:
  - "TakeDamage(int) 호출 시 HP 감소"
  - "HP가 0 이하일 때 사망 이벤트 발생"
  - "MaxHp는 Config에서 설정"
  - "회복 시 MaxHp 초과 방지"

# 예: CurrencyWallet
domain_criteria:
  - "Add(int) 호출 시 잔고 증가"
  - "TrySpend(int) 호출 시 잔고 충분하면 true 반환 및 차감"
  - "잔고 부족 시 TrySpend false 반환, 잔고 유지"
  - "MaxBalance 초과 시 MaxBalance로 클램프"
  - "OnBalanceChanged 이벤트 발생"
```

### 7.3 Learning Memory 기반 추가 기준

과거 실패 이력에서 추출한 추가 기준:

```yaml
# RECURRING_MISTAKES.md에서 REC-001 발견 시
learning_criteria:
  - "[RM-0001] Runtime이 MonoBehaviour를 상속하지 않음을 명시적으로 확인"
  - "[RM-0004] foreach가 코드에 없음을 확인"
  - "[RM-0003] using Game.X가 MODULE_REGISTRY dependencies에 선언됨"
```

---

## 8. 출력 계약 (Output Contract)

### 8.1 TASK_QUEUE.yaml 엔트리

```yaml
- name: <ModuleName>                    # PascalCase
  status: pending                       # 초기 상태
  priority: <high | medium | low>
  owner: null
  role: null
  depends_on: [<Module1>, <Module2>]    # UnityEngine, System 제외
  module_path: Assets/Game/Modules/<ModuleName>
  feature_group: "<feature-group-slug>"
  description: "<모듈 책임 한 줄>"

  # v2 상태 차원 (초기값)
  human_state: none
  learning_state: none
  commit_state: none
  retry_count: 0

  # v2.1 — Integration Strategy
  integration_strategy: "<reuse | extend | adapt | replace | create_new>"
  existing_module_candidates: [<유사 모듈 목록>]
  compatibility_review: "<not_required | pending>"
  impact_analysis: "<not_required | pending>"
  migration_required: <boolean>

  # v2.2 — Fix Classification (초기값)
  ai_post_validation_fixes: []
  post_validation_fix_count: 0
  post_validation_fix_actor: none
  learning_note_required: false

  # v2.3 — Architecture Diff Analysis (초기값)
  arch_diff_risk: not_analyzed
  arch_diff_blocked: false
  arch_diff_report_path: null

  # 메타데이터
  feature_goal: "<상위 기능 목표>"
  based_on: Template
  notes: "<위험 플래그 + 학습 경고>"

  # 수락 기준
  acceptance_criteria:
    structure: [...]
    coding: [...]
    architecture: [...]
    domain: [...]
    learning: [...]

  # 산출물
  deliverables:
    - I<ModuleName>.cs
    - <ModuleName>Config.cs
    - <ModuleName>Runtime.cs
    - <ModuleName>Factory.cs
    - <ModuleName>Bootstrap.cs
    - Tests/Editor/<ModuleName>Tests.cs

  constraints:
    - "<적용 제약>"
  references:
    - "docs/ai/MODULE_TEMPLATES.md"
```

### 8.2 MODULE_REGISTRY.yaml 엔트리

```yaml
- name: <ModuleName>
  path: Assets/Game/Modules/<ModuleName>
  editable: true
  risk: <low | medium | high>
  description: "<모듈 책임 한 줄>"
  dependencies: [<Module1>, <Module2>]
```

### 8.3 FEATURE_QUEUE.yaml 업데이트

```yaml
# status를 intake → decomposed → queued로 전이
# modules 필드에 분해된 모듈 목록 추가
```

### 8.4 Spec 파일

```
docs/ai/generated_specs/<ModuleName>_SPEC.md
```

상세 형식은 `generated_specs/README.md` 참조.

---

## 9. 전체 실행 예시 1: Currency System

### 입력

```
"I want a player currency system with gold/gems, add/spend/query"
```

### Step A: Feature Intake

```yaml
- name: "Currency System"
  description: "플레이어 재화 관리. 골드/젬 두 종류. Add/Spend/Query API."
  priority: medium
  status: intake
  feature_group: "currency-economy"
```

### Step B: Learning Scan

```
학습 메모리 스캔 결과:
  - RM-0001: Runtime MonoBehaviour 상속 금지 → 적용
  - RM-0003: using 참조 시 registry 선언 필수 → 적용
  - REC-002: foreach/LINQ 습관적 사용 주의 → 경고 플래그
  - 기존 Economy 모듈 존재 확인 → 의존성으로 연결
```

### Step C: Decomposition

```
기능 분석:
  1. 재화 지갑 — 잔고 관리, Add, Spend, Query

판단:
  골드/젬은 데이터 차이이므로 모듈을 분리하지 않음.
  CurrencyWallet 하나로 Config에서 재화 종류를 정의.

모듈:
  1. CurrencyWallet
     책임: 플레이어 재화 잔고 관리 (Add, Spend, Query)
     의존: [Economy]  (기존 모듈)
     경로: Assets/Game/Modules/CurrencyWallet
```

### Step D~E: 의존성 + Risk

```
의존성: CurrencyWallet → [Economy]
Risk: low (독립적, 기존 패턴과 동일)
```

### Step F: TASK_QUEUE 엔트리

```yaml
- name: CurrencyWallet
  status: pending
  priority: medium
  owner: null
  role: null
  depends_on: [Economy]
  module_path: Assets/Game/Modules/CurrencyWallet
  feature_group: "currency-economy"
  description: "플레이어 재화 잔고 관리 — Add, Spend, Query API"
  human_state: none
  learning_state: none
  commit_state: none
  retry_count: 0
  feature_goal: "Currency System"
  based_on: Template
  notes: >
    Auto-generated from feature: Currency System.
    [LEARNING] RM-0001: Runtime MonoBehaviour 금지.
    [LEARNING] REC-002: foreach/LINQ 주의.
  acceptance_criteria:
    structure:
      - "ICurrencyWallet.cs 인터페이스"
      - "CurrencyWalletConfig.cs (ScriptableObject)"
      - "CurrencyWalletRuntime.cs (순수 C#)"
      - "CurrencyWalletFactory.cs (static class)"
      - "CurrencyWalletBootstrap.cs (MonoBehaviour)"
      - "Tests/Editor/CurrencyWalletTests.cs (최소 2개)"
    domain:
      - "Add(int) 호출 시 잔고 증가"
      - "TrySpend(int) 잔고 충분 → true + 차감"
      - "잔고 부족 → false, 잔고 유지"
      - "MaxBalance 초과 시 클램프"
      - "OnBalanceChanged 이벤트"
    learning:
      - "[RM-0001] Runtime MonoBehaviour 상속 없음 확인"
      - "[RM-0004] foreach 없음 확인"
  deliverables:
    - ICurrencyWallet.cs
    - CurrencyWalletConfig.cs
    - CurrencyWalletRuntime.cs
    - CurrencyWalletFactory.cs
    - CurrencyWalletBootstrap.cs
    - Tests/Editor/CurrencyWalletTests.cs
  constraints:
    - "Economy 모듈 인터페이스만 참조"
```

---

## 10. 전체 실행 예시 2: Combat Core

### 입력

```
"Combat core: health, damage, enemy detection, cooldown, buff/debuff"
```

### Step A: Feature Intake

```yaml
- name: "Combat Core"
  description: |
    전투 루프 전체 구현.
    플레이어/적 체력, 데미지 적용, 적 탐지, 공격 쿨다운, 버프/디버프 지원.
  priority: high
  status: intake
  feature_group: "combat-core"
```

### Step B: Learning Scan

```
학습 메모리 스캔 결과:
  - RM-0001: Runtime MonoBehaviour 금지 → 모든 모듈에 적용
  - RM-0013: Update에서 magnitude 금지 → EnemyDetection에 특히 주의
  - REC-001: Runtime MonoBehaviour 고위험 → 5개 모듈 전부 경고
  - 기존 StatusEffect 모듈 존재 확인 → BuffSystem 의존으로 연결
```

### Step C: Decomposition

```
기능 분석:
  1. 체력 관리 — HP 추적, 피해 적용, 사망 판정
  2. 데미지 계산 — 데미지 값 결정, 감쇠
  3. 적 탐지 — 범위 기반 탐색, 타겟 선택
  4. 쿨다운 관리 — 스킬/공격 타이머
  5. 버프/디버프 — 상태 효과 적용/제거

모듈:
  1. HealthSystem     — 체력 관리
  2. DamageSystem     — 데미지 계산 → [HealthSystem]
  3. EnemyDetection   — 적 탐지
  4. CooldownSystem   — 쿨다운 관리
  5. BuffSystem       — 버프/디버프 → [StatusEffect]
```

### Step D~E: 의존성 + Risk

```
HealthSystem     → []               risk: low
DamageSystem     → [HealthSystem]    risk: low
EnemyDetection   → []               risk: medium (magnitude 주의 — RM-0013)
CooldownSystem   → []               risk: low
BuffSystem       → [StatusEffect]    risk: medium (기존 모듈 의존)
```

### Step F: TASK_QUEUE 엔트리 (5개)

```yaml
- name: HealthSystem
  status: pending
  priority: high
  owner: null
  role: null
  depends_on: []
  module_path: Assets/Game/Modules/HealthSystem
  feature_group: "combat-core"
  description: "체력 관리 — HP 추적, 피해 적용, 사망 판정, 회복"
  human_state: none
  learning_state: none
  commit_state: none
  retry_count: 0
  notes: "[LEARNING] RM-0001: Runtime MonoBehaviour 금지"
  acceptance_criteria:
    domain:
      - "TakeDamage(int) → HP 감소"
      - "HP <= 0 → 사망 이벤트"
      - "Heal(int) → HP 증가, MaxHp 클램프"
      - "Config에서 MaxHp 설정"
  deliverables:
    - IHealthSystem.cs
    - HealthSystemConfig.cs
    - HealthSystemRuntime.cs
    - HealthSystemFactory.cs
    - HealthSystemBootstrap.cs
    - Tests/Editor/HealthSystemTests.cs

- name: DamageSystem
  status: pending
  priority: high
  owner: null
  role: null
  depends_on: [HealthSystem]
  module_path: Assets/Game/Modules/DamageSystem
  feature_group: "combat-core"
  description: "데미지 계산 — 데미지 값 결정, 감쇠, HealthSystem에 적용"
  human_state: none
  learning_state: none
  commit_state: none
  retry_count: 0
  notes: "[LEARNING] RM-0003: HealthSystem 의존 시 registry 선언 필수"
  acceptance_criteria:
    domain:
      - "ApplyDamage(target, amount) → HealthSystem.TakeDamage 호출"
      - "데미지 감쇠 계수 Config 기반"
      - "IHealthSystem 인터페이스만 참조"

- name: EnemyDetection
  status: pending
  priority: high
  owner: null
  role: null
  depends_on: []
  module_path: Assets/Game/Modules/EnemyDetection
  feature_group: "combat-core"
  description: "적 탐지 — 범위 기반 타겟 탐색, 가장 가까운 적 선택"
  human_state: none
  learning_state: none
  commit_state: none
  retry_count: 0
  notes: >
    [RISK] RM-0013: magnitude 대신 sqrMagnitude 사용 필수.
    [LEARNING] 거리 계산 성능 주의.
  acceptance_criteria:
    domain:
      - "범위 내 적 탐색 (sqrMagnitude 기반)"
      - "가장 가까운 적 반환"
      - "Config에서 탐지 범위 설정"
      - "Tick()에서 매 프레임 magnitude 사용 금지"

- name: CooldownSystem
  status: pending
  priority: medium
  owner: null
  role: null
  depends_on: []
  module_path: Assets/Game/Modules/CooldownSystem
  feature_group: "combat-core"
  description: "쿨다운 관리 — 스킬/공격 타이머, 사용 가능 여부 판정"
  human_state: none
  learning_state: none
  commit_state: none
  retry_count: 0
  notes: "[LEARNING] CP-005: 코루틴 대신 델타타임 누적 타이머 사용"
  acceptance_criteria:
    domain:
      - "StartCooldown(id, duration) → 타이머 시작"
      - "IsReady(id) → 쿨다운 완료 여부"
      - "Tick(deltaTime) → 타이머 감소"
      - "Config에서 기본 쿨다운 설정"

- name: BuffSystem
  status: pending
  priority: medium
  owner: null
  role: null
  depends_on: [StatusEffect]
  module_path: Assets/Game/Modules/BuffSystem
  feature_group: "combat-core"
  description: "버프/디버프 — 상태 효과 적용, 지속시간, 제거"
  human_state: none
  learning_state: none
  commit_state: none
  retry_count: 0
  notes: >
    [RISK] StatusEffect 기존 모듈 의존 — 인터페이스만 참조.
    [LEARNING] RM-0003: dependencies에 StatusEffect 선언 필수.
  acceptance_criteria:
    domain:
      - "ApplyBuff(type, duration) → 효과 적용"
      - "RemoveBuff(type) → 효과 제거"
      - "Tick(deltaTime) → 지속시간 감소, 만료 시 자동 제거"
      - "IStatusEffect 인터페이스만 참조"
```

---

## 11. 산출물 요약 (Output Report)

Queue Generator 실행 완료 후 반드시 출력한다:

```
QUEUE GENERATION REPORT:
  feature: <feature_name>
  feature_group: <group>
  discovery_result:
    candidates_found: <count>
    reuse_decisions: <count> (reuse + extend + adapt)
    create_new_decisions: <count>
  modules_created: <count>
  modules_reused: <count>
  modules_extended: <count>
  modules_skipped: <count> (이미 존재)
  dependencies_inferred:
    - <Module1> → [<deps>]
    - <Module2> → [<deps>]
  risk_summary:
    high: <count>
    medium: <count>
    low: <count>
  learning_flags: <count>
  files_generated:
    - TASK_QUEUE.yaml (<count> entries added)
    - MODULE_REGISTRY.yaml (<count> entries added)
    - FEATURE_QUEUE.yaml (status → queued)
    - generated_specs/<Module>_SPEC.md × <count>
  next_step: "Planner가 각 모듈의 PLAN을 작성한다"
```

---

## 12. Queue Generator가 하지 않는 것

| 항목 | 담당 역할 |
|------|-----------|
| PLAN 작성 | Planner |
| 코드 생성 | Builder |
| 검증 실행 | Reviewer |
| 커밋 실행 | Committer |
| 학습 기록 | Learning Recorder |
| TASK_QUEUE status 변경 (pending 이후) | Planner, Builder, Reviewer |
| Core 폴더 수정 | 사용자 명시 허가 필요 |

---

## 13. 참조 문서

| 문서 | 용도 |
|------|------|
| `FEATURE_INTAKE.md` | 입력 형식 상세 |
| `MODULE_TEMPLATES.md` | 모듈 6파일 구조 |
| `MODULE_REGISTRY.yaml` | 기존 모듈 확인 |
| `TASK_QUEUE.yaml` | 기존 태스크 확인 |
| `TASK_SCHEMA.md` | TASK_QUEUE 필드 정의 |
| `CODING_RULES.md` | 코딩 규칙 (수락 기준 생성) |
| `learning/LEARNING_INDEX.md` | 학습 메모리 진입점 |
| `learning/RULE_MEMORY.yaml` | 규칙 저장소 |
| `learning/RECURRING_MISTAKES.md` | 반복 실수 패턴 |
| `learning/VALIDATOR_FAILURE_PATTERNS.md` | Validator 실패 패턴 |
| `MODULE_DISCOVERY.md` | Module Discovery 절차 (v2.1) |
| `INTEGRATION_STRATEGY.md` | Reuse Decision Engine (v2.1) |
| `MIGRATION_RULES.md` | 마이그레이션 규칙 (v2.1) |
| `PIPELINE_HARDENING.md` | 파이프라인 강화 (v2.2) |
| `LEARNING_SYSTEM.md` | Learning Recorder 강화 (v2.2) |
| `CONFIG_RULES.md` | Config Source-of-Truth 규칙 (v2.2) |
| `ARCHITECTURE_DIFF_ANALYZER.md` | Architecture Diff Analyzer 명세 (v2.3) |
