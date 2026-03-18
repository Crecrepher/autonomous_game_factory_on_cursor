# Architecture Diff Analyzer — Autonomous Game Factory v2.3

AI가 제안한 변경이 기존 코드베이스의 아키텍처를 위반하는지 사전 분석하는 안전 게이트.

---

## 1. 목적

| 문제 | 해결 |
|------|------|
| AI가 기존 모듈의 책임을 침범하는 변경 생성 | 책임 드리프트 감지 |
| 인터페이스 하위 호환성 파괴 | 인터페이스 diff 분석 |
| 순환 의존 도입 | 의존성 그래프 diff |
| MonoBehaviour 로직이 Runtime에 침투 | 계층 분리 검증 |
| 프레임당 GC 할당 도입 | 성능 위험 감지 |

---

## 2. 파이프라인 위치

```
[1] Feature Intake
  ↓
[2] Queue Generator
  [2.5] Module Discovery
  [2.7] Reuse Decision
  ↓
[2.9] ★ Architecture Diff Analyzer ★  ← 여기
  ↓
[3] Orchestrator
[4] Planner (Impact Analysis + Migration Planning 포함)
  ↓
[5] Builder
...
```

Architecture Diff Analyzer는 **Queue Generator 직후, Planner 직전**에 실행된다.
Reuse Decision의 결과(integration_strategy)와 기존 코드베이스를 비교하여 아키텍처 위험을 사전 감지한다.

---

## 3. Diff 분석 범위

| 범위 | 비교 대상 | 분석 내용 |
|------|-----------|-----------|
| 모듈 책임 | SPEC description vs 기존 모듈 description | 책임 겹침/드리프트 |
| 인터페이스 | 제안된 API vs 기존 I<Module>.cs | 메서드 추가/제거/변경 |
| 의존성 | 제안된 depends_on vs 기존 그래프 | 새 간선/순환/깊이 |
| 레지스트리 | 제안된 MODULE_REGISTRY 변경 vs 현재 | 이름 충돌/경로 충돌 |
| 모듈 경계 | 제안된 코드 참조 vs 모듈 격리 규칙 | 크로스 모듈 직접 참조 |
| 계층 분리 | Runtime vs MonoBehaviour | Runtime에 MonoBehaviour 상속 |

---

## 4. 감지 Diff 타입

| Diff 타입 | 코드 | 설명 |
|-----------|------|------|
| 신규 모듈 생성 | `new_module` | 새 모듈 폴더/파일 생성 |
| 인터페이스 변경 | `interface_change` | 기존 인터페이스 메서드 추가/제거/시그니처 변경 |
| 의존성 추가 | `dependency_addition` | 새 의존 간선 추가 |
| 의존성 제거 | `dependency_removal` | 기존 의존 간선 제거 |
| 모듈 교체 | `module_replacement` | 기존 모듈을 새 모듈로 대체 |
| 책임 드리프트 | `responsibility_drift` | 모듈의 핵심 책임 범위가 변경 |
| 아키텍처 규칙 위반 | `architecture_violation` | CODING_RULES/MODULE_TEMPLATES 위반 |

---

## 5. 위험 분류

| 레벨 | 조건 | 파이프라인 영향 |
|------|------|----------------|
| `low` | 신규 모듈 생성, 단순 의존성 추가 | 진행 허용, 로그만 |
| `medium` | 인터페이스 메서드 추가, 의존성 구조 변경 | 진행 허용, Planner/Reviewer에 경고 전달 |
| `high` | 기존 인터페이스 메서드 시그니처 변경, 모듈 교체 | 진행 허용, Human 승인 권장 |
| `critical` | 아래 Critical 조건 해당 | **파이프라인 차단** |

### 5.1 Critical 조건 (즉시 차단)

| # | 조건 | 이유 |
|---|------|------|
| 1 | 기존 모듈의 핵심 책임이 변경됨 | 단일 책임 원칙 위반 |
| 2 | 모듈 경계 파괴 (크로스 모듈 Runtime 직접 참조) | 모듈 격리 위반 |
| 3 | 의존성 순환 도입 | 그래프 무결성 파괴 |
| 4 | MonoBehaviour 로직이 Runtime 계층에 진입 | 아키텍처 계층 위반 |
| 5 | 프레임당 GC 할당 패턴 도입 (foreach, LINQ, 코루틴, 람다) | 성능 규칙 위반 |
| 6 | 기존 인터페이스 메서드 삭제 | 하위 호환성 파괴 |

---

## 6. Diff 결과 스키마

```yaml
architecture_diff_report:
  module: "<모듈명>"
  timestamp: "<ISO 8601>"
  strategy: "<integration_strategy>"
  overall_risk: "low | medium | high | critical"
  blocked: <boolean>
  
  diffs:
    - type: "<diff 타입>"
      target: "<대상 모듈/파일>"
      change: "<변경 내용>"
      risk: "low | medium | high | critical"
      reason: "<위험 사유>"
      mitigation: "<권장 해결 방법>"
  
  dependency_graph_diff:
    added_edges: [<"A → B">, ...]
    removed_edges: [<"A → B">, ...]
    cycle_detected: <boolean>
    max_depth_change: <int>
  
  summary:
    total_diffs: <int>
    critical_count: <int>
    high_count: <int>
    medium_count: <int>
    low_count: <int>
    blocking_reasons: [<string>]
```

---

## 7. 예시: Diff 리포트

### 7.1 안전한 신규 모듈

```yaml
architecture_diff_report:
  module: EquipmentSystem
  timestamp: "2026-03-18T23:00:00"
  strategy: create_new
  overall_risk: low
  blocked: false
  
  diffs:
    - type: new_module
      target: EquipmentSystem
      change: "새 모듈 생성: 장비 장착/해제/스탯 반영"
      risk: low
      reason: "기존 모듈과 책임 겹침 없음"
      mitigation: null
    - type: dependency_addition
      target: "EquipmentSystem → InventorySystem"
      change: "InventorySystem 의존 추가"
      risk: low
      reason: "단방향 의존, 순환 없음"
      mitigation: null
  
  dependency_graph_diff:
    added_edges: ["EquipmentSystem → InventorySystem"]
    removed_edges: []
    cycle_detected: false
    max_depth_change: 1
  
  summary:
    total_diffs: 2
    critical_count: 0
    high_count: 0
    medium_count: 0
    low_count: 2
    blocking_reasons: []
```

### 7.2 위험한 모듈 교체

```yaml
architecture_diff_report:
  module: MultiCurrencyEconomy
  timestamp: "2026-03-18T23:30:00"
  strategy: replace
  overall_risk: critical
  blocked: true
  
  diffs:
    - type: module_replacement
      target: Economy
      change: "Economy를 MultiCurrencyEconomy로 교체"
      risk: high
      reason: "8개 모듈이 Economy에 의존 중"
      mitigation: "점진적 마이그레이션 + 어댑터 패턴 사용"
    - type: interface_change
      target: IEconomy
      change: "AddGold(int) 메서드 삭제"
      risk: critical
      reason: "기존 인터페이스 메서드 삭제 — 하위 호환성 파괴"
      mitigation: "메서드 삭제 대신 deprecated 표시 후 새 메서드 추가"
    - type: responsibility_drift
      target: Economy
      change: "단일 재화 → 다중 재화로 책임 범위 확대"
      risk: medium
      reason: "책임 범위 변경이지만 동일 도메인"
      mitigation: "Config 기반으로 재화 타입 확장"
  
  dependency_graph_diff:
    added_edges: ["MultiCurrencyEconomy → CurrencyConfig"]
    removed_edges: ["Warriors → Economy"]
    cycle_detected: false
    max_depth_change: 0
  
  summary:
    total_diffs: 3
    critical_count: 1
    high_count: 1
    medium_count: 1
    low_count: 0
    blocking_reasons:
      - "기존 IEconomy.AddGold(int) 메서드 삭제 — 하위 호환성 파괴"
```

---

## 8. 파이프라인 통합

### 8.1 Planner에 전달

Diff Report의 `diffs[]`에서 `medium` 이상 항목은 Planner가 PLAN에 반영:

```
PLAN:
  architecture_diff_warnings:
    - "[medium] 인터페이스에 RemoveAllStacks() 추가 — 기존 의존자 확인 필요"
    - "[high] Economy 교체 — 8개 모듈 영향"
```

### 8.2 Reviewer에 전달

Reviewer는 Diff Report를 읽고 `medium` 이상 항목에 대해 추가 검증:

```
REVIEW:
  architecture_diff_check:
    - "[medium] 인터페이스 변경 확인: 기존 테스트 호환성 유지됨"
    - "[high] 교체 영향: Migration Plan 존재 확인"
```

### 8.3 Committer 차단

`overall_risk == critical` AND `blocked == true`이면:
- Committer는 해당 모듈을 커밋할 수 없다
- TASK_QUEUE status → blocked
- blocked_reason에 Diff Report의 blocking_reasons 기록

---

## 9. TASK_QUEUE 필드

```yaml
# v2.3 — Architecture Diff Analysis
arch_diff_risk: "low | medium | high | critical | not_analyzed"
arch_diff_blocked: false
arch_diff_report_path: "docs/ai/diff_reports/<Module>_DIFF.md"
```

---

## 10. 구현 참조

| 구성 요소 | 파일 |
|-----------|------|
| Diff 분석 엔진 | `Assets/Editor/AI/ArchitectureDiffAnalyzer.cs` |
| Validator 통합 | `Assets/Editor/AI/Validators/ArchitectureDiffValidator.cs` |
| 인터페이스 스캔 | `Assets/Editor/AI/ModuleDiscovery.cs` (InterfaceScanner) |
| 의존성 그래프 | `Assets/Editor/AI/DependencyGraphBuilder.cs` |

---

## 11. 참조 문서

| 문서 | 관계 |
|------|------|
| `ORCHESTRATION_RULES.md` | Diff Analyzer 파이프라인 위치 |
| `MODULE_DISCOVERY.md` | 기존 모듈 탐색 (Diff 입력) |
| `INTEGRATION_STRATEGY.md` | 전략 결정 (Diff 입력) |
| `MIGRATION_RULES.md` | 교체 시 마이그레이션 |
| `CODING_RULES.md` | 아키텍처 규칙 기준 |
| `PIPELINE_HARDENING.md` | 파이프라인 강화 |
