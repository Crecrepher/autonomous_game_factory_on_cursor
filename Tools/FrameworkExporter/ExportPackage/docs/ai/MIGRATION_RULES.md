# Migration Rules — Autonomous Game Factory v2

모듈 교체(replace) 또는 확장(extend) 시 안전한 마이그레이션을 보장하는 명세.

---

## 1. 목적

| 문제 | 해결 |
|------|------|
| 기존 모듈 교체 시 의존자 파손 | 영향 분석 + 단계적 마이그레이션 |
| 확장 시 하위 호환 깨짐 | 호환성 검증 규칙 |
| 교체 후 잔여 코드 방치 | 명시적 은퇴(retire) 절차 |

---

## 2. 실행 시점

```
[Queue Generator]
  Module Discovery → Reuse Decision: replace/extend 결정
  ↓
[Planner]
  ★ Impact Analysis ★  ← 여기
  ★ Migration Plan ★   ← 여기
  ↓
[Builder]
  Migration Plan에 따라 구현
  ↓
[Reviewer]
  Impact Analysis + Migration Plan 검증
```

---

## 3. Impact Analysis

### 3.1 분석 항목

| 항목 | 설명 | 자동 분석 |
|------|------|-----------|
| 의존 모듈 | target_module을 depends_on하는 모듈 목록 | MODULE_REGISTRY.yaml + TASK_QUEUE.yaml 스캔 |
| 레지스트리 변경 | MODULE_REGISTRY.yaml 수정 필요 여부 | dependencies 필드 비교 |
| 큐 변경 | TASK_QUEUE.yaml 수정 필요 여부 | depends_on 필드 비교 |
| Validator 위험 | 변경으로 인한 Validator 실패 가능성 | 위험도 추정 |
| feature_group 영향 | 다른 feature_group에 영향 여부 | feature_group 교차 확인 |

### 3.2 Impact Analysis 스키마

```yaml
impact_analysis:
  target_module: "<교체/확장 대상 모듈>"
  strategy: "replace | extend"
  timestamp: "<ISO 8601>"

  affected_modules:
    - module: "<의존자 모듈명>"
      dependency_type: "direct | transitive"
      required_change: "update_import | update_api_call | no_change"
      risk: "low | medium | high"

  registry_changes_required: <boolean>
  queue_changes_required: <boolean>
  validator_risk: "low | medium | high"
  feature_group_impact:
    - group: "<feature_group>"
      effect: "none | update_required | blocked"

  summary:
    total_affected: <int>
    high_risk_count: <int>
    blocking_issues: [<string>]
```

### 3.3 Impact Analysis 예시

```yaml
impact_analysis:
  target_module: Economy
  strategy: replace
  timestamp: "2026-03-18T22:00:00"

  affected_modules:
    - module: Warriors
      dependency_type: direct
      required_change: update_api_call
      risk: medium
    - module: DefenseTowers
      dependency_type: direct
      required_change: update_api_call
      risk: medium
    - module: Fortress
      dependency_type: direct
      required_change: update_api_call
      risk: medium
    - module: Pickups
      dependency_type: direct
      required_change: update_import
      risk: low
    - module: HireNodes
      dependency_type: direct
      required_change: update_api_call
      risk: medium
    - module: Blacksmith
      dependency_type: direct
      required_change: update_api_call
      risk: medium
    - module: UI
      dependency_type: direct
      required_change: update_import
      risk: low
    - module: GameManager
      dependency_type: direct
      required_change: update_import
      risk: low

  registry_changes_required: true
  queue_changes_required: true
  validator_risk: high
  feature_group_impact:
    - group: item-stacking
      effect: none
    - group: inventory-system
      effect: none

  summary:
    total_affected: 8
    high_risk_count: 0
    blocking_issues: []
```

---

## 4. Migration Plan

### 4.1 구성 요소

| 항목 | 설명 |
|------|------|
| 기존 모듈 처리 | 즉시 제거 vs 임시 유지 vs deprecated |
| 어댑터 필요 여부 | 기존 API → 새 API 변환 레이어 |
| 의존성 업데이트 | 영향받는 모듈의 depends_on/dependencies 변경 |
| 큐 변경 | TASK_QUEUE.yaml 엔트리 수정 |
| 검증 계획 | 마이그레이션 후 검증 시나리오 |

### 4.2 Migration Plan 스키마

```yaml
migration_plan:
  target_module: "<교체/확장 대상>"
  new_module: "<새 모듈명 | null>"
  strategy: "replace | extend"
  timestamp: "<ISO 8601>"

  old_module_handling: "keep_temporarily | deprecate | remove_after_validation"
  introduce_adapter: "<어댑터 모듈명 | null>"
  adapter_purpose: "<어댑터 역할 설명 | null>"

  dependency_updates:
    - module: "<의존자 모듈명>"
      old_dependency: "<기존 모듈명>"
      new_dependency: "<새 모듈명>"
      change_type: "swap | add | remove"

  queue_changes:
    - module: "<TASK_QUEUE 모듈명>"
      field: "depends_on"
      old_value: "<기존 값>"
      new_value: "<새 값>"

  registry_changes:
    - module: "<MODULE_REGISTRY 모듈명>"
      field: "dependencies"
      old_value: "<기존 값>"
      new_value: "<새 값>"

  validation_plan:
    - step: "<검증 단계>"
      description: "<검증 내용>"
      expected_result: "<기대 결과>"

  retire_old_module_after_validation: <boolean>
  estimated_risk: "low | medium | high"
  requires_human_approval: <boolean>
```

### 4.3 Migration Plan 예시

```yaml
migration_plan:
  target_module: Economy
  new_module: MultiCurrencyEconomy
  strategy: replace
  timestamp: "2026-03-18T22:30:00"

  old_module_handling: keep_temporarily
  introduce_adapter: EconomyAdapter
  adapter_purpose: "기존 IEconomy API를 IMultiCurrencyEconomy로 변환하여 점진적 마이그레이션 지원"

  dependency_updates:
    - module: Warriors
      old_dependency: Economy
      new_dependency: MultiCurrencyEconomy
      change_type: swap
    - module: DefenseTowers
      old_dependency: Economy
      new_dependency: MultiCurrencyEconomy
      change_type: swap

  queue_changes:
    - module: Warriors
      field: depends_on
      old_value: "[Economy]"
      new_value: "[MultiCurrencyEconomy]"

  registry_changes:
    - module: Warriors
      field: dependencies
      old_value: "[UnityEngine, System, Economy]"
      new_value: "[UnityEngine, System, MultiCurrencyEconomy]"

  validation_plan:
    - step: "1. 새 모듈 생성 + 테스트"
      description: "MultiCurrencyEconomy 모듈을 생성하고 독립 테스트 통과"
      expected_result: "15+ 테스트 통과"
    - step: "2. 어댑터 설치"
      description: "EconomyAdapter를 통해 기존 의존자들이 컴파일 통과"
      expected_result: "전체 컴파일 성공"
    - step: "3. 점진적 마이그레이션"
      description: "의존자 모듈을 하나씩 새 API로 전환"
      expected_result: "각 모듈 전환 후 Validator PASS"
    - step: "4. 기존 모듈 은퇴"
      description: "모든 의존자 전환 후 Economy + EconomyAdapter 제거"
      expected_result: "전체 Validator PASS"

  retire_old_module_after_validation: true
  estimated_risk: high
  requires_human_approval: true
```

---

## 5. 전략별 Migration 규칙

### 5.1 extend

```
필수:
  - 기존 인터페이스의 메서드 삭제 금지
  - 기존 메서드의 시그니처 변경 금지
  - 새 메서드만 추가
  - 기존 테스트 전부 통과 유지
  - 새 기능에 대한 추가 테스트 작성

Migration Plan:
  - old_module_handling: 해당 없음 (동일 모듈 유지)
  - introduce_adapter: null
  - dependency_updates: 보통 없음
  - retire_old_module_after_validation: false
```

### 5.2 replace

```
필수:
  - Impact Analysis 완료
  - Migration Plan 작성
  - Human 승인
  - 어댑터 레이어 고려
  - 점진적 마이그레이션 (빅뱅 교체 금지)
  - 기존 모듈은 마이그레이션 완료 전까지 유지

Migration Plan:
  - old_module_handling: keep_temporarily
  - introduce_adapter: 필요 시
  - dependency_updates: 전체 의존자 목록
  - retire_old_module_after_validation: true
```

---

## 6. 검증 게이트

### 6.1 Planner 단계

| 게이트 | 조건 | 실패 시 |
|--------|------|---------|
| Impact Analysis Gate | replace/extend → impact_analysis == completed | PLAN 작성 차단 |
| Migration Plan Gate | replace → migration_plan 존재 | PLAN 작성 차단 |

### 6.2 Reviewer 단계

| 게이트 | 조건 | 실패 시 |
|--------|------|---------|
| Compatibility Gate | extend → 기존 테스트 전부 통과 | commit_state → blocked |
| Migration Gate | replace → migration_plan 단계 모두 완료 | commit_state → blocked |
| Impact Verification Gate | 영향받는 모듈에 실제 변경 적용됨 | commit_state → blocked |

### 6.3 Validator (IntegrationStrategyValidator)

| 검사 | blocking |
|------|----------|
| replace 전략인데 impact_analysis 누락 | error (blocking) |
| replace 전략인데 migration_plan 누락 | error (blocking) |
| extend 전략인데 compatibility_review != passed | error (blocking) |
| 기존 인터페이스 메서드 삭제 감지 (extend 시) | error (blocking) |

---

## 7. 참조 문서

| 문서 | 관계 |
|------|------|
| `MODULE_DISCOVERY.md` | Discovery → Reuse Decision → Migration |
| `INTEGRATION_STRATEGY.md` | 전략 결정 → Migration Plan |
| `QUEUE_GENERATOR.md` | Discovery/Decision이 통합되는 상위 프로세스 |
| `ORCHESTRATION_RULES.md` | 전체 파이프라인 |
| `COMMIT_RULES.md` | 마이그레이션 커밋 규칙 |
