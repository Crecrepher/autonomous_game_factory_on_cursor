# Parallel Orchestration Architecture — AGF v3.0

> 역할 기반 직렬 → 실행 단위 기반 병렬 전환

---

## 1. 아키텍처 전환

### 1.1 Before (v2.3 — 직렬)

```
사용자 요청
  ↓
[단일 에이전트 세션]
  ↓ Planner 역할
  ↓ Builder 역할 (for 루프로 모듈 순차 처리)
  ↓ Reviewer 역할
  ↓ Committer 역할
  ↓
완료
```

**문제**: 독립 모듈 A, B, C가 있어도 A 완료 후 B 시작, B 완료 후 C 시작.

### 1.2 After (v3.0 — 병렬)

```
사용자 요청
  ↓
[Orchestrator]
  ↓ 의존성 분석 → DependencyReadyQueue
  ↓
  ├── [Slot 1] TaskExecUnit A → Branch/Worktree A → Build → Validate
  ├── [Slot 2] TaskExecUnit B → Branch/Worktree B → Build → Validate
  └── [Slot 3] TaskExecUnit C → Branch/Worktree C → Build → Validate
  ↓
[JoinAndMerge] → 충돌 감지 → 리뷰 → 머지
  ↓
[Committer] → feature_group 단위 커밋
```

---

## 2. 실행 모델

### 2.1 Tier 1 — Subagent 기반 (현재 환경에서 가능)

Cursor의 Task 도구를 사용하여 서브에이전트를 병렬 실행한다.

```
Primary Agent (Orchestrator)
  ├── Task(subagent_type=generalPurpose) → Module A Build
  ├── Task(subagent_type=generalPurpose) → Module B Build
  └── Task(subagent_type=generalPurpose) → Module C Build
```

**제한사항**:
- 최대 4개 동시 서브에이전트 (Cursor 제한)
- 동일 워크스페이스 공유 → 파일 충돌 가능
- 서브에이전트는 도구 접근이 제한될 수 있음

**적합한 작업**:
- 독립 모듈의 코드 생성 (서로 다른 폴더)
- 문서 생성
- 분석/리포트 생성

### 2.2 Tier 2 — Branch 기반 격리

각 태스크를 별도 브랜치에서 실행하고 머지한다.

```
ai_test (base)
  ├── agf/build/CurrencyWallet (Agent Slot 1)
  ├── agf/build/ShopSystem (Agent Slot 2)
  └── agf/build/HealthSystem (Agent Slot 3)
       ↓ (완료 후)
  JoinAndMerge → ai_test
```

**요구사항**:
- 에이전트가 브랜치를 생성/전환할 수 있어야 함
- 머지 시 충돌 감지 + 해결 필요
- 현재 Cursor 환경에서는 단일 워크스페이스이므로 동시 브랜치 작업 불가

### 2.3 Tier 3 — Worktree 기반 완전 격리

각 태스크를 별도 Git 워크트리에서 실행한다.

```
/Users/supercent/luna_lumberchopper (main worktree)
/Users/supercent/luna_lumberchopper-wt-CurrencyWallet (worktree 1)
/Users/supercent/luna_lumberchopper-wt-ShopSystem (worktree 2)
```

**요구사항**:
- MCP GitKraken의 `git_worktree` 도구 사용
- 각 워크트리에서 별도 에이전트 세션 필요
- Background Agent 또는 Cloud Agent 필요
- 현재 환경에서는 **사람이 수동으로** 확인 필요

---

## 3. 오케스트레이션 흐름

### 3.1 Phase 1 — Preparation (직렬)

```
[1] Feature Intake → FEATURE_QUEUE
[2] Intelligent Decomposer → 시스템/모듈 분해
[3] Queue Generator → TASK_QUEUE + MODULE_REGISTRY
[4] Architecture Diff Analyzer → 위험 분석
[5] Dependency Graph Build → 토폴로지 정렬
```

이 단계는 반드시 직렬. 전체 계획이 확정되어야 병렬 실행 가능.

### 3.2 Phase 2 — Parallel Execution

```
[6] DependencyReadyQueue 구성
    → 의존성 만족 모듈을 ready_queue에 추가

[7] AgentLease 배정
    → 사용 가능한 에이전트 슬롯에 태스크 임대
    → max_concurrent 제한 적용

[8] Parallel Build
    → 각 슬롯이 독립적으로 모듈 빌드
    → 빌드 완료 시 자동 검증 (Validator)
    → 검증 완료 시 임대 해제

[9] 반복
    → 해제된 슬롯에 다음 ready 태스크 배정
    → 모든 태스크가 done 또는 blocked일 때까지
```

### 3.3 Phase 3 — Join and Merge (직렬)

```
[10] 완료된 모듈 수집
[11] 충돌 감지 (같은 파일 수정 여부)
[12] Merge Readiness Check
[13] Reviewer Validation
[14] ★ Human Gate ★
[15] Committer → feature_group 단위 커밋
[16] Learning Recorder
```

---

## 4. 동시 실행 제약

| 제약 | 이유 | 대응 |
|---|---|---|
| 최대 4 서브에이전트 | Cursor 제한 | 라운드 기반 스케줄링 |
| 동일 파일 충돌 | 공유 워크스페이스 | AGF 모듈은 폴더별 격리 (위험 낮음) |
| Shared 인터페이스 수정 | Assets/Game/Shared/ 충돌 | 직렬 처리 또는 선점 잠금 |
| Human Gate | 병렬 불가 | 모든 병렬 작업 완료 후 일괄 검증 |
| 커밋 | feature_group 단위 | Join 후 일괄 커밋 |

---

## 5. AGF 모듈의 자연 격리

AGF 모듈 구조는 병렬 실행에 유리하다:

```
Assets/Game/Modules/
├── CurrencyWallet/      ← Agent 1이 여기만 수정
│   ├── ICurrencyWallet.cs
│   ├── CurrencyWalletConfig.cs
│   ├── CurrencyWalletRuntime.cs
│   ├── CurrencyWalletFactory.cs
│   ├── CurrencyWalletBootstrap.cs
│   └── Tests/Editor/CurrencyWalletTests.cs
├── ShopSystem/          ← Agent 2가 여기만 수정
│   └── ...
└── HealthSystem/        ← Agent 3이 여기만 수정
    └── ...
```

각 모듈은 독립 폴더에 6파일 구조. 서로 다른 모듈의 파일은 겹치지 않음.
→ **공유 워크스페이스에서도 동시 빌드가 안전**함 (Shared/ 수정 제외).

---

## 6. 현재 가능한 최대 병렬 수준

| 방식 | 동시 실행 | 격리 수준 | 실현 가능성 |
|---|---|---|---|
| Cursor Subagent (Task) | 최대 4 | 파일 수준 (모듈 폴더 분리) | ✅ 즉시 가능 |
| Branch per task | 1 (브랜치 전환 필요) | 브랜치 수준 | ⚠ 수동 전환 |
| Worktree per task | N (워크트리 수만큼) | 완전 격리 | ⚠ 환경 확인 필요 |
| Background Agent | N | 완전 격리 | ❓ Cursor 계정 확인 |

---

## 7. 관련 문서

| 문서 | 역할 |
|---|---|
| `TASK_EXECUTION_SCHEMA.md` | TaskExecutionUnit / AgentLease / DependencyReadyQueue |
| `WORKTREE_STRATEGY.md` | 작업 격리 전략 |
| `JOIN_AND_MERGE_REVIEW.md` | 병렬 작업 합류/머지 |
| `PARALLEL_EXECUTION_AUDIT.md` | 현재 상태 감사 결과 |
