# Integration Strategy — Autonomous Game Factory v2

Reuse Decision Engine 명세.
Module Discovery 결과를 받아 기존 모듈의 재사용 전략을 결정한다.

---

## 1. 목적

| 문제 | 해결 |
|------|------|
| 기존 모듈과 중복되는 신규 모듈 생성 | 전략적 재사용 결정 |
| 기존 코드를 무시하고 처음부터 구현 | 재사용 > 확장 > 교체 > 신규 우선순위 |
| 교체 시 영향 분석 없이 진행 | Impact Analysis 필수 게이트 |

---

## 2. 실행 시점

```
Queue Generator 내부:
  [A] Feature Intake
  [B] Learning Scan
  [B.5] Module Discovery
  [B.7] ★ Reuse Decision Engine ★  ← 여기
  [C] Decomposition (결정 결과 반영)
  [D] Dependency Inference
  ...
```

---

## 3. 허용 전략 (Integration Strategies)

| 전략 | 코드 | 설명 | 위험도 |
|------|------|------|--------|
| 재사용 | `reuse` | 기존 모듈을 그대로 사용. 의존성으로 연결 | low |
| 확장 | `extend` | 기존 모듈에 새 기능/API 추가 | medium |
| 적응 | `adapt` | 기존 모듈을 래핑하여 새 인터페이스 제공 | medium |
| 교체 | `replace` | 기존 모듈을 새 모듈로 대체 | high |
| 신규 생성 | `create_new` | 유사 모듈 없음, 처음부터 생성 | low |

---

## 4. 결정 규칙 (Decision Rules)

### 4.1 우선순위

```
reuse > extend > adapt > replace > create_new
```

**항상 재사용을 우선한다.** 교체는 최후의 수단이다.

### 4.2 전략 선택 기준

```
IF discovery.similarity_score >= 0.9 AND 기능이 완전히 커버됨:
  → strategy: reuse
  → 신규 모듈 생성하지 않음, 의존성으로 연결

IF discovery.similarity_score >= 0.7 AND 기능 일부만 커버:
  → strategy: extend (기존 모듈에 추가)
  → 또는 adapt (래핑 모듈 생성)
  → 결정 기준: 기존 모듈의 editable 여부

IF discovery.similarity_score >= 0.4 AND 패턴만 유사:
  → strategy: create_new (참조만)
  → existing_module_candidates에 기록

IF discovery.candidate_count == 0:
  → strategy: create_new

IF 기존 모듈이 editable: false:
  → extend/replace 불가 → adapt 또는 create_new

IF 기존 모듈이 risk: high:
  → extend/replace 시 Impact Analysis 필수
```

### 4.3 교체(replace) 허용 조건

replace는 다음 **모든** 조건을 만족할 때만 허용:

1. Impact Analysis 완료 (`impact_analysis: completed`)
2. Migration Plan 작성 (`migration_required: true`, plan 존재)
3. 영향받는 모듈이 모두 식별됨
4. Human 승인 (교체는 위험이 높으므로 사람이 확인)
5. 기존 모듈이 `editable: true`

---

## 5. Reuse Decision 결과 스키마

```yaml
reuse_decision:
  integration_strategy: "reuse | extend | adapt | replace | create_new"
  target_module: "<기존 모듈명 | null>"
  reason: "<결정 이유>"
  compatibility_review: "pending | passed | failed"
  impact_analysis: "not_required | pending | completed"
  migration_required: <boolean>
  risk_level: "low | medium | high"
  constraints:
    - "<적용 제약>"
```

---

## 6. 전략별 행동

### 6.1 reuse

```
행동:
  - 신규 모듈 생성하지 않음
  - TASK_QUEUE에 신규 태스크 추가하지 않음
  - 상위 모듈의 depends_on에 기존 모듈 추가
  - MODULE_REGISTRY의 dependencies에 기존 모듈 추가

TASK_QUEUE 필드:
  integration_strategy: reuse
  existing_module_candidates: [<기존 모듈>]
  compatibility_review: passed
  impact_analysis: not_required
  migration_required: false
```

### 6.2 extend

```
행동:
  - 기존 모듈의 인터페이스에 새 메서드 추가
  - 기존 Runtime에 새 로직 추가
  - 테스트 추가/확장
  - MODULE_REGISTRY의 description 업데이트

필수 조건:
  - 기존 모듈이 editable: true
  - Impact Analysis 완료
  - 하위 호환성 유지 (기존 API 변경 금지)

TASK_QUEUE 필드:
  integration_strategy: extend
  existing_module_candidates: [<확장 대상 모듈>]
  compatibility_review: pending → passed
  impact_analysis: pending → completed
  migration_required: false (하위 호환이면)
```

### 6.3 adapt

```
행동:
  - 새 래핑/어댑터 모듈 생성
  - 기존 모듈을 의존성으로 연결
  - 어댑터가 기존 API를 새 인터페이스로 변환

특징:
  - 기존 모듈 코드를 수정하지 않음
  - 기존 모듈이 editable: false여도 가능

TASK_QUEUE 필드:
  integration_strategy: adapt
  existing_module_candidates: [<래핑 대상 모듈>]
  compatibility_review: pending → passed
  impact_analysis: not_required
  migration_required: false
```

### 6.4 replace

```
행동:
  - 새 모듈을 생성
  - 기존 모듈의 의존자들을 새 모듈로 전환
  - Migration Plan에 따라 순차 전환
  - 기존 모듈은 migration 완료 후 제거 또는 deprecated 처리

필수 조건:
  - Impact Analysis 완료 (affected_modules 전부 식별)
  - Migration Plan 작성 완료
  - Human 승인
  - 기존 모듈이 editable: true

TASK_QUEUE 필드:
  integration_strategy: replace
  existing_module_candidates: [<교체 대상 모듈>]
  compatibility_review: pending → passed
  impact_analysis: pending → completed
  migration_required: true
```

### 6.5 create_new

```
행동:
  - 표준 파이프라인대로 신규 모듈 생성
  - Discovery 후보를 참조로만 기록

TASK_QUEUE 필드:
  integration_strategy: create_new
  existing_module_candidates: []
  compatibility_review: not_required
  impact_analysis: not_required
  migration_required: false
```

---

## 7. 예시

### 예시 1: 스택 기능 요청 시 ItemStacking 발견

```yaml
reuse_decision:
  integration_strategy: reuse
  target_module: ItemStacking
  reason: "ItemStacking이 Push/Pop/IsFull/IsEmpty를 이미 제공. 요청된 기능과 정확히 일치."
  compatibility_review: passed
  impact_analysis: not_required
  migration_required: false
  risk_level: low
```

### 예시 2: 인벤토리에 검색 기능 추가 요청

```yaml
reuse_decision:
  integration_strategy: extend
  target_module: InventorySystem
  reason: "InventorySystem이 슬롯/스택 기반 인벤토리를 제공하나 검색(Find) API가 없음. 확장으로 해결."
  compatibility_review: pending
  impact_analysis: pending
  migration_required: false
  risk_level: medium
  constraints:
    - "기존 IInventorySystem 인터페이스에 Find 메서드 추가"
    - "기존 테스트 전부 통과 유지"
```

### 예시 3: Economy를 완전히 새로운 구조로 교체

```yaml
reuse_decision:
  integration_strategy: replace
  target_module: Economy
  reason: "기존 Economy가 단일 재화만 지원. 다중 재화 요구사항에 구조적으로 맞지 않음."
  compatibility_review: pending
  impact_analysis: pending
  migration_required: true
  risk_level: high
  constraints:
    - "Warriors, DefenseTowers, Fortress, Pickups, HireNodes, Blacksmith, UI, GameManager 모두 영향받음"
    - "Impact Analysis + Migration Plan 필수"
    - "Human 승인 필수"
```

---

## 8. 검증 규칙

| # | 규칙 | 검증 시점 |
|---|------|-----------|
| 1 | replace 전략은 impact_analysis == completed 없이 Builder 진입 불가 | status: planned → in_progress |
| 2 | replace 전략은 migration_required == true 이고 Migration Plan 존재 필수 | status: planned → in_progress |
| 3 | extend 전략은 compatibility_review == passed 없이 review 진입 불가 | status: in_progress → review |
| 4 | reuse 전략은 신규 모듈 생성을 차단 | Queue Generator 단계 |
| 5 | editable: false 모듈에 대해 extend/replace 금지 | Queue Generator 단계 |

---

## 9. 참조 문서

| 문서 | 관계 |
|------|------|
| `MODULE_DISCOVERY.md` | Discovery 후속 — 전략 입력 |
| `MIGRATION_RULES.md` | replace/extend 시 마이그레이션 |
| `QUEUE_GENERATOR.md` | Reuse Decision이 통합되는 상위 프로세스 |
| `ORCHESTRATION_RULES.md` | 전체 파이프라인 |
