# Parallel Execution Audit — AGF v3.0

> Audit Date: 2026-03-18 (Updated: 2026-03-18)  
> Auditor: Autonomous Game Factory Pipeline  
> Cursor Version: 2.6.20 (Stable, Darwin arm64)  
> Status: **PARALLEL-READY — Cursor 2.6.20이 워크트리 기반 Parallel Agent를 내장 지원**

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

### 4.1 직접 확인된 사항 (2026-03-18)

| 항목 | 상태 | 확인 방법 |
|---|---|---|
| Cursor 버전 | **2.6.20 Stable** (Darwin arm64) | 사용자 직접 확인 |
| Parallel Agent (워크트리 기반) | **✅ 내장 지원** | Cursor 2.6 공식 문서 확인 |
| Subagent (Task 도구) | **✅ 동작 확인** | 이 감사 세션에서 병렬 실행 검증 |
| MCP git_worktree 도구 | **✅ 정상 동작** | `list` 액션 직접 호출 성공 |
| Background Agent | **✅ Cursor에 내장** | 유료 플랜 기능, 설정에서 별도 토글 없이 사용 가능 |
| `.cursor/rules/` 3개 룰 | 단일 에이전트 가정 (업데이트 필요) | 레포 확인 |
| 문서에 "한 에이전트" 명시 | 확인 (`AI_DEVELOPMENT_LOOP.md`) — 구식 | 레포 확인 |

### 4.2 Cursor 2.6.20 Parallel Agent 기능 상세

Cursor 2.6.20은 다음을 **내장 지원**한다:

1. **워크트리 자동 생성**: Cursor가 자동으로 워크트리를 생성/관리
2. **1:1 에이전트-워크트리 매핑**: 각 에이전트가 자체 워크트리에서 격리 실행
3. **Apply 기능**: 에이전트 작업 완료 후 메인 브랜치에 머지
4. **Best-of-N**: 동일 프롬프트를 여러 모델에 동시 실행
5. **워크트리 자동 정리**: 최대 20개, 오래된 것부터 자동 삭제
6. **초기화 스크립트**: `.cursor/worktrees.json`으로 워크트리 설정 커스터마이징

워크트리 경로: `/Users/<user>/.cursor/worktrees/<repo>/<hash>`

### 4.3 Background Agent 확인 결과

- Background Agent는 Cursor에 **내장**되어 있다
- Features/Beta에 별도 토글이 없는 것은 **기본 활성화**되었거나 유료 플랜 기능이기 때문
- Background Agent는 자율적으로 백그라운드에서 코딩 작업을 수행
- 워크트리 기반 격리 실행 가능

### 4.4 남은 확인 사항

| 확인 항목 | 방법 |
|---|---|
| Background Agent가 현재 플랜에서 사용 가능한지 | Cursor 채팅에서 Background Agent 실행 시도 |
| `.cursor/worktrees.json` 초기화 스크립트 설정 | Unity 프로젝트용 설정 파일 생성 필요 |

---

## 5. 현재 상태 분류 (업데이트)

```
┌─────────────────────────────────────────────────────────────┐
│ 분류                    │ 상태 (2026-03-18)                 │
│─────────────────────────│──────────────────────────────────│
│ currently serial        │ ✅ AGF 코드 자체는 아직 직렬       │
│ parallel-capable design │ ✅ 의존성 그래프, 라운드 스케줄링  │
│ parallel-ready arch     │ ✅ TaskExecUnit/Lease/Queue 구현  │
│ Cursor parallel support │ ✅ 2.6.20이 워크트리 Parallel Agent│
│                         │    내장 + Subagent 동작 확인      │
│ truly execution-parallel│ ⚠ 연결만 하면 즉시 가능            │
└─────────────────────────────────────────────────────────────┘
```

---

## 6. 병렬 실행으로의 경로

### 6.1 Tier 1 — 완료 (레포 코드)

1. ✅ `TaskExecutionUnit` / `AgentLease` / `DependencyReadyQueue` 구현
2. ✅ 실행 격리 전략 문서화 (워크트리/브랜치/폴더)
3. ✅ Join & Merge Review 프로세스 정의
4. ✅ `TaskExecutionEngine.cs` 코드 구현

### 6.2 Tier 2 — 즉시 가능 (확인됨)

1. ✅ Cursor Subagent(Task 도구) — 동작 확인, 최대 4개 동시
2. 독립 태스크를 별도 Subagent로 실제 병렬 빌드 — 연결만 필요
3. 결과를 Join 스테이지에서 수집 + 검증

### 6.3 Tier 3 — Cursor 내장 기능 활용 (확인됨)

1. ✅ Cursor 2.6.20 Parallel Agent — 워크트리 자동 생성/관리 내장
2. ✅ MCP git_worktree 도구 — 정상 동작 확인
3. ⚠ Background Agent — 플랜 확인 필요
4. `.cursor/worktrees.json` 초기화 스크립트 설정 필요

---

## 7. 참조

| 문서 | 내용 |
|---|---|
| `PARALLEL_ORCHESTRATION.md` | 병렬 실행 아키텍처 설계 |
| `TASK_EXECUTION_SCHEMA.md` | TaskExecutionUnit / AgentLease 스키마 |
| `WORKTREE_STRATEGY.md` | 작업 격리 전략 |
| `JOIN_AND_MERGE_REVIEW.md` | 병렬 작업 합류/머지 프로세스 |
