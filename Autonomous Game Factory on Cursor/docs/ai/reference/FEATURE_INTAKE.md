# Feature Intake — Autonomous Game Factory v2

Queue Generator 파이프라인의 **진입점**이다.
고수준 기능 설명을 받아 구조화된 Feature 엔트리를 생성하고 FEATURE_QUEUE.yaml에 등록한다.

---

## 1. 지원 입력 유형

| 유형 | 설명 | 예시 |
|------|------|------|
| 자연어 한 줄 | 기능 요약 | "플레이어 재화 시스템: 골드/젬, 추가/소비/조회" |
| 기능 목록 | 시스템 나열 | "Combat core: health, damage, detection, cooldown, buff" |
| 디자인 브리프 | 게임플레이 설명 | "적 탐지 → 공격 → 피해 → 체력 감소 → 사망/회복" |
| 메카닉 작성서 | 게임 메카닉 상세 | "쿨다운 기반 공격 시스템, 0.5초 기본 쿨다운" |
| 기획 문서 | 별도 문서 참조 | PROJECT_OVERVIEW.md 섹션 참조 |

---

## 2. Feature Intake 형식 (YAML)

Feature Intake는 `docs/ai/FEATURE_QUEUE.yaml`에 기록된다.

```yaml
features:
  - name: "Combat Core"
    description: |
      전투 루프 전체 구현.
      플레이어/적 체력, 데미지 적용, 적 탐지, 공격 쿨다운, 버프/디버프 지원.
    priority: high
    status: intake
    feature_group: "combat-core"
    requested_by: "designer"
    created_at: "2026-03-18"
    modules: []                    # Queue Generator가 분해하여 채운다
    constraints:
      - "StatusEffect 모듈 재사용"
      - "Economy와 직접 의존 없음"
    references:
      - "docs/ai/PROJECT_OVERVIEW.md"
```

### 필드 설명

| 필드 | 필수 | 설명 |
|------|------|------|
| `name` | O | 기능 이름 |
| `description` | O | 기능 설명. 한 줄 또는 YAML multi-line(`\|`) |
| `priority` | O | `high`, `medium`, `low` |
| `status` | O | 초기값 `intake`. Queue Generator가 전이 |
| `feature_group` | △ | 커밋 그룹 slug. 없으면 name에서 자동 추론 |
| `requested_by` | △ | 요청자 (기본: "system") |
| `created_at` | △ | 생성 날짜 (ISO 8601) |
| `modules` | X | 빈 배열. Queue Generator가 채움 |
| `constraints` | X | 분해/구현 시 준수할 제약 |
| `references` | X | 참조 문서 |

---

## 3. 상태 라이프사이클

```
intake ──► decomposed ──► queued ──► in_progress ──► done
                                        │
                                        ▼
                                     blocked
```

| 상태 | 설명 | 전이 주체 |
|------|------|-----------|
| `intake` | 디자인 입력 접수됨 | 사용자/시스템 |
| `decomposed` | 모듈 분해 완료, modules 필드 채워짐 | Queue Generator |
| `queued` | TASK_QUEUE + MODULE_REGISTRY에 엔트리 생성 완료 | Queue Generator |
| `in_progress` | 모듈 구현/검증 진행 중 | Orchestrator |
| `done` | 모든 모듈 done + 검증 통과 + Git 커밋 완료 | Orchestrator |
| `blocked` | 일부 모듈 blocked | Orchestrator |

---

## 4. 자연어 입력 → Feature 변환 예시

### 예시 1: 전투 루프

**입력:**
```
"Combat core: health, damage, enemy detection, cooldown, buff/debuff"
```

**변환 결과:**
```yaml
- name: "Combat Core"
  description: |
    전투 루프 전체 구현.
    플레이어/적 체력, 데미지 적용, 적 탐지, 공격 쿨다운, 버프/디버프 지원.
  priority: high
  status: intake
  feature_group: "combat-core"
  constraints:
    - "기존 StatusEffect 모듈 재사용"
```

**Queue Generator 분해 후 modules:**
```yaml
modules:
  - HealthSystem
  - DamageSystem
  - EnemyDetection
  - CooldownSystem
  - BuffSystem
```

### 예시 2: 재화 시스템

**입력:**
```
"I want a player currency system with gold/gems, add/spend/query"
```

**변환 결과:**
```yaml
- name: "Currency System"
  description: "플레이어 재화 관리. 골드/젬 두 종류. Add/Spend/Query API."
  priority: medium
  status: intake
  feature_group: "currency-economy"
```

**Queue Generator 분해 후:**
```yaml
modules:
  - CurrencyWallet
```

참고: 골드/젬은 CurrencyWalletConfig에서 재화 종류로 정의. 모듈 분리 아님.

### 예시 3: 농장 루프

**입력:**
```
"Create a farming progression loop with inventory, crop growth, harvesting, and sell flow"
```

**변환 결과:**
```yaml
- name: "Farming Loop"
  description: |
    농장 진행 루프. 인벤토리, 작물 성장, 수확, 판매 흐름.
  priority: medium
  status: intake
  feature_group: "farming-loop"
```

**Queue Generator 분해 후:**
```yaml
modules:
  - InventorySystem
  - CropGrowth
  - HarvestSystem
  - SellFlow
```

---

## 5. 분해 규칙

| # | 규칙 | 설명 |
|---|------|------|
| 1 | 책임 기반 분해 | 이름이 아닌 실제 기능 의미로 분해 |
| 2 | 기존 구현 존중 | MODULE_REGISTRY.yaml에 이미 있으면 새로 만들지 않음 |
| 3 | 모듈은 작게 | God-module 금지 (GameplaySystem, UniversalManager 등) |
| 4 | 의존성 명시 | MODULE_REGISTRY.yaml과 TASK_QUEUE.yaml의 depends_on 일치 |
| 5 | 데이터 차이 ≠ 모듈 분리 | 같은 로직이면 Config로 구분 (예: 골드/젬 → CurrencyWallet 하나) |
| 6 | 최대 10개 | feature당 모듈 10개 초과 시 sub-feature로 분할 |

상세 분해 규칙과 의존성 추론은 `QUEUE_GENERATOR.md` 참조.

---

## 6. 처리 파이프라인

```
Feature Intake (FEATURE_QUEUE.yaml)
 ↓
Queue Generator:
  ├── [A] Learning Scan — 학습 메모리 참조
  ├── [B] Decomposition — 모듈 분해
  ├── [C] Dependency Inference — 의존 순서
  ├── [D] Risk Annotation — 위험 플래그
  ├── [E] Queue Entry — TASK_QUEUE.yaml 생성
  ├── [F] Registry Entry — MODULE_REGISTRY.yaml 등록
  └── [G] Spec Generation — generated_specs/ 생성
 ↓
Planner (각 모듈 PLAN 작성)
 ↓
Builder (구현)
 ↓
★ Human Validation Gate ★
 ↓
Reviewer → Committer → Learning Recorder
```

---

## 7. 참조 문서

| 문서 | 내용 |
|------|------|
| `QUEUE_GENERATOR.md` | Queue Generator 행동 명세, 분해/의존/위험 규칙, 전체 예시 |
| `MODULE_TEMPLATES.md` | 모듈 6파일 구조 |
| `TASK_SCHEMA.md` | TASK_QUEUE 엔트리 스키마 |
| `COMMIT_RULES.md` | 커밋 게이트와 feature_group 규칙 |
| `learning/LEARNING_INDEX.md` | 학습 메모리 진입점 |
