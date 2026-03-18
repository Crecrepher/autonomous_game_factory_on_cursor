# Parallel Execution Audit — AGF v2.3

> Audit Date: 2026-03-18  
> Auditor: Autonomous Game Factory Pipeline  
> Status: **SERIAL — 병렬은 개념적으로만 존재**

---

## 1. 감사 결론 (Executive Summary)

**현재 시스템은 100% 직렬(serial) 실행이다.**

"병렬(Parallel)"이라는 단어가 코드와 문서 전반에 사용되지만,
이는 **"한 라운드에서 여러 모듈을 처리할 수 있다"는 스케줄링 개념**이지,
실제로 **여러 에이전트/프로세스/스레드가 동시에 코드를 생성하는 것이 아니다.**

---

## 2. 감사 범위

### 2.1 검사한 코드 파일

| 파일 | 판정 |
|---|---|
| `OrchestratorSimulator.cs` | `while(round)` + `for` 루프. 동시성 프리미티브 없음. |
| `ParallelBuilderOrchestrator.cs` | 이름만 "Parallel". `for` 루프로 순차 실행. |
| `AutonomousPipeline.cs` | 순차 레이어 A→B→C→D→E→F→G. |
| `BuilderAgent.cs` | 단일 모듈 빌드 로직. 스레드 없음. |
| `TaskStateTransition.cs` | 상태 전이 규칙. 실행 모델 아님. |

### 2.2 검사한 문서

| 문서 | 판정 |
|---|---|
| `ORCHESTRATION_RULES.md` | "순차 바톤 전달" 명시. 병렬 실행 미언급. |
| `AGENT_ROLES.md` | 역할은 개념적 레이블. 별도 에이전트 매핑 없음. |
| `STATE_MACHINE.md` | 태스크별 상태 허용하나 실행 모델 미정의. |
| `AI_DEVELOPMENT_LOOP.md` | "한 에이전트가 모든 역할을 순서대로 수행" 명시. |

### 2.3 검사한 환경

| 항목 | 결과 |
|---|---|
| Git 워크트리 | 1개만 존재 (메인) |
| Git 브랜치 | `ai_test` (현재), creative/* 다수 |
| `.cursor/rules/` | 3개 룰 (모두 단일 에이전트 가정) |
| MCP 서버 | GitKraken (worktree 도구 있음), cursor-ide-browser |
| Subagent 설정 | 없음 |
| Cloud Agent 설정 | 없음 |

---

## 3. 상세 진단

### 3.1 "병렬"의 실체

```
현재 "병렬" 실행:
  Round 1: [ModuleA, ModuleB, ModuleC] — 의존성 만족
  for (i = 0; i < 3; i++) {
      ExecuteBuilder(modules[i]);  // 순차 실행
      ExecuteValidation(modules[i]); // 순차 실행
  }
  // → ModuleA 완료 후 ModuleB 시작, ModuleB 완료 후 ModuleC 시작

진짜 병렬 실행이라면:
  Round 1: [ModuleA, ModuleB, ModuleC]
  Agent1.Execute(ModuleA) ← 동시
  Agent2.Execute(ModuleB) ← 동시
  Agent3.Execute(ModuleC) ← 동시
  JoinAll() → MergeReview
```

### 3.2 동시성 프리미티브 부재

| 프리미티브 | 존재 여부 |
|---|---|
| `Task.Run` / `Task.WhenAll` | 없음 |
| `async` / `await` | 없음 |
| `System.Threading` | 없음 |
| `Parallel.ForEach` | 없음 |
| Git Worktree per task | 없음 |
| Branch per task | 없음 |
| Separate process spawn | 없음 |
| Cursor Subagent spawn | 없음 |

### 3.3 역할 vs 에이전트 혼동

현재 시스템에서 "Builder", "Reviewer", "Committer" 등은:
- ❌ 별도의 AI 에이전트 인스턴스가 아님
- ❌ 별도의 프로세스/스레드가 아님
- ❌ 별도의 작업 공간(워크트리)에서 실행되지 않음
- ✅ **단일 에이전트가 순서대로 수행하는 역할 레이블**

---

## 4. Cursor 환경 역량 분석

### 4.1 레포에서 확인된 사항

| 항목 | 상태 |
|---|---|
| `.cursor/rules/` 3개 룰 | 모두 단일 에이전트 가정 |
| MCP GitKraken `git_worktree` 도구 | 존재 (list/add) |
| MCP cursor-ide-browser | 존재 |
| 문서에 "한 에이전트" 명시 | 확인 (`AI_DEVELOPMENT_LOOP.md`) |

### 4.2 아키텍처에서 추론된 사항

| 항목 | 추론 |
|---|---|
| Cursor Subagent (Task 도구) | 현재 세션에서 사용 가능 (도구 목록에 존재) |
| Subagent 동시 실행 | 단일 메시지에서 여러 Task 도구 호출로 가능 |
| 워크트리 기반 격리 | MCP로 워크트리 생성 가능하나, 파이프라인이 사용하지 않음 |
| Cloud/Background Agent | 레포에서 확인 불가 |

### 4.3 사람이 직접 확인해야 할 사항

| 확인 항목 | 방법 |
|---|---|
| Cursor 버전이 Subagent/Parallel Agent 지원하는지 | Cursor 설정 > About |
| Background Agent가 활성화되어 있는지 | Cursor 설정 > Features/Beta |
| Parallel Agent (워크트리 기반) 활성화 여부 | Cursor 설정 > Experimental |
| Subagent가 별도 실행 컨텍스트에서 동작하는지 | 두 Subagent 동시 실행 후 파일 충돌 테스트 |
| MCP git_worktree 도구 정상 동작 여부 | Cursor에서 직접 호출 테스트 |

---

## 5. 현재 상태 분류

```
┌─────────────────────────────────────────────────────────────┐
│ 분류                    │ AGF v2.3 현재 상태                │
│─────────────────────────│─────────────────────────────────│
│ currently serial        │ ✅ 모든 실행이 순차적             │
│ parallel-capable design │ ✅ 의존성 그래프, 라운드 스케줄링  │
│ parallel-ready arch     │ ❌ 실행 격리, 에이전트 임대 없음   │
│ truly execution-parallel│ ❌ 동시성 프리미티브 없음           │
└─────────────────────────────────────────────────────────────┘
```

---

## 6. 병렬 실행으로의 경로

### 6.1 Tier 1 — 즉시 가능 (레포 코드만으로)

1. `TaskExecutionUnit` / `AgentLease` / `DependencyReadyQueue` 데이터 모델 도입
2. 실행 격리 전략 문서화 (워크트리/브랜치)
3. Join & Merge Review 프로세스 정의
4. 오케스트레이터를 "실행 단위" 기반으로 재설계

### 6.2 Tier 2 — Cursor 환경 의존

1. Cursor Subagent(Task 도구)를 파이프라인에 통합
2. 독립 태스크를 별도 Subagent로 실제 병렬 실행
3. 결과를 Join 스테이지에서 수집 + 검증

### 6.3 Tier 3 — 인프라 의존

1. Git 워크트리 per task 자동 생성/정리
2. Background Agent / Cloud Agent 통합
3. 브랜치 기반 격리 + 자동 머지

---

## 7. 참조

| 문서 | 내용 |
|---|---|
| `PARALLEL_ORCHESTRATION.md` | 병렬 실행 아키텍처 설계 |
| `TASK_EXECUTION_SCHEMA.md` | TaskExecutionUnit / AgentLease 스키마 |
| `WORKTREE_STRATEGY.md` | 작업 격리 전략 |
| `JOIN_AND_MERGE_REVIEW.md` | 병렬 작업 합류/머지 프로세스 |
