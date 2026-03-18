# Task Execution Schema — 실행 단위 기반 병렬 아키텍처

> Version: 1.0  
> 목적: 역할 기반 직렬 오케스트레이션을 실행 단위 기반 병렬 오케스트레이션으로 전환

---

## 1. 핵심 개념

### 기존 (역할 기반 직렬)
```
Planner → Builder → Reviewer → Committer
  (단일 에이전트가 순서대로 역할을 교체하며 실행)
```

### 목표 (실행 단위 기반 병렬)
```
DependencyReadyQueue → [TaskExecUnit A] → Agent Slot 1 (Branch A)
                     → [TaskExecUnit B] → Agent Slot 2 (Branch B)
                     → [TaskExecUnit C] → Agent Slot 3 (Branch C)
                                           ↓
                                      JoinAndMerge
```

---

## 2. TaskExecutionUnit

하나의 독립 실행 가능한 작업 단위. 기존 TASK_QUEUE의 모듈 엔트리를 실행 메타데이터로 확장한다.

### 2.1 스키마

```yaml
execution_unit:
  # 기존 TASK_QUEUE 필드 (모두 유지)
  name: ModuleName
  status: pending | planned | ready | executing | validating | merging | done | blocked
  priority: high | medium | low
  depends_on: [Dep1, Dep2]
  feature_group: feature_group_name

  # 실행 단위 확장 필드
  execution:
    unit_id: "exec_<module>_<timestamp>"
    dependency_status: waiting | satisfied | blocked
    execution_owner: null | "agent_slot_1" | "subagent_<id>"
    lease_id: null | "lease_<uuid>"
    isolated_workspace:
      type: none | worktree | branch | cloud
      ref: null | "worktree/ModuleName" | "branch/agf/ModuleName"
      path: null | "/path/to/worktree"
    merge_target: "ai_test"
    started_at: null | "2026-03-18T10:00:00Z"
    timeout_minutes: 30
    validation_requirements:
      - module_structure
      - coding_style
      - architecture_rules
      - regression_guard
    result:
      status: null | success | failed | timeout
      files_changed: []
      conflicts: []
      validation_report_path: null
```

### 2.2 상태 전이

```
pending
  ↓ (의존성 만족)
ready
  ↓ (AgentLease 획득)
executing
  ↓ (빌드 완료)
validating
  ↓ (검증 통과)
merging
  ↓ (머지 완료)
done

blocked ← (의존성 실패 | 검증 실패 | 충돌 감지)
```

### 2.3 dependency_status 결정 로직

```
IF all depends_on modules have status == done:
    dependency_status = satisfied
ELSE IF any depends_on module has status == blocked:
    dependency_status = blocked
ELSE:
    dependency_status = waiting
```

---

## 3. AgentLease

에이전트 슬롯이 특정 태스크를 임시 소유하는 계약.

### 3.1 스키마

```yaml
lease:
  lease_id: "lease_<uuid>"
  task_name: "ModuleName"
  unit_id: "exec_ModuleName_1710741600"
  agent_slot: "agent_slot_1" | "subagent_<id>" | "background_agent_<id>"
  agent_type: primary | subagent | background | cloud
  started_at: "2026-03-18T10:00:00Z"
  timeout_minutes: 30
  status: active | completed | released | timeout | failed
  workspace:
    type: worktree | branch | shared
    ref: "worktree/ModuleName"
  handoff:
    on_complete: release_to_queue
    on_timeout: release_and_requeue
    on_failure: release_and_block
```

### 3.2 임대 규칙

| 규칙 | 설명 |
|---|---|
| 하나의 태스크에 최대 1개 임대 | 동시 편집 방지 |
| 타임아웃 시 자동 해제 | stale 임대 방지 |
| 완료 시 즉시 해제 | 다음 태스크 진행 가능 |
| 실패 시 태스크 blocked 전이 | 수동 개입 필요 |
| 임대 없이 실행 불가 | 소유권 없는 실행 방지 |

### 3.3 에이전트 슬롯 유형

| 유형 | 설명 | 격리 수준 |
|---|---|---|
| `primary` | 현재 Cursor 대화의 메인 에이전트 | 공유 워크스페이스 |
| `subagent` | Cursor Task 도구로 생성된 서브에이전트 | 동일 워크스페이스 (읽기 중심) |
| `background` | Cursor Background Agent (확인 필요) | 별도 실행 컨텍스트 |
| `cloud` | 외부 CI/CD 또는 Cloud Agent | 완전 격리 |

---

## 4. DependencyReadyQueue

의존성이 만족된 태스크를 즉시 실행 가능한 대기열로 관리한다.

### 4.1 스키마

```yaml
ready_queue:
  timestamp: "2026-03-18T10:00:00Z"
  entries:
    - unit_id: "exec_CurrencyWallet_1710741600"
      task_name: "CurrencyWallet"
      priority: high
      feature_group: "economy"
      estimated_minutes: 15
      workspace_requirement: branch
    - unit_id: "exec_ShopSystem_1710741601"
      task_name: "ShopSystem"
      priority: medium
      feature_group: "economy"
      estimated_minutes: 20
      workspace_requirement: branch
  max_concurrent: 3
  active_leases: 1
  available_slots: 2
```

### 4.2 큐 진입 조건

1. `dependency_status == satisfied`
2. `status == ready` (pending에서 ready로 전이 완료)
3. 현재 임대 없음 (`lease_id == null`)
4. `blocked` 상태가 아님

### 4.3 디스패치 우선순위

1. `priority: high` > `medium` > `low`
2. 동일 우선순위: `feature_group` 내 토폴로지 순서
3. 동일 그룹: 추정 실행 시간이 짧은 것 우선

---

## 5. 기존 TASK_QUEUE.yaml과의 호환성

새 실행 필드는 기존 TASK_QUEUE 엔트리에 `execution:` 블록으로 추가된다.
기존 필드는 모두 유지. 하위 호환 100%.

```yaml
# 기존 필드 (유지)
  - name: CurrencyWallet
    status: pending
    priority: high
    owner: null
    role: null
    depends_on: [Economy]
    human_state: none
    commit_state: none
    feature_group: economy_system

    # 신규 실행 필드 (추가)
    execution:
      unit_id: "exec_CurrencyWallet_1710741600"
      dependency_status: waiting
      execution_owner: null
      lease_id: null
      isolated_workspace:
        type: none
        ref: null
      merge_target: "ai_test"
```

---

## 6. 관련 문서

| 문서 | 역할 |
|---|---|
| `PARALLEL_EXECUTION_AUDIT.md` | 현재 직렬/병렬 상태 감사 |
| `PARALLEL_ORCHESTRATION.md` | 병렬 오케스트레이션 아키텍처 |
| `WORKTREE_STRATEGY.md` | 작업 격리 전략 |
| `JOIN_AND_MERGE_REVIEW.md` | 병렬 작업 합류/머지 |
