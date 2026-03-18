# Project Handoff — Autonomous Game Factory v2

이 문서는 향후 개발 스레드가 즉시 작업을 시작할 수 있도록 현재 프로젝트의 **최종 상태**를 정리한 핸드오프 문서다.

최종 갱신: 2026-03-18

---

## 1. 프로젝트 목적

**Human-in-the-Loop Learning Pipeline**을 구현하는 Unity AI 개발 팩토리.

```
AI가 모듈을 생성한다
→ 사람이 Unity Editor에서 검증하고 수정한다
→ 수정 이유를 기록한다
→ AI가 재검증/재커밋한다
→ 축적된 학습이 다음 생성의 품질을 높인다
```

대상 게임: 2D 아이소메트릭 타워 디펜스 Playable Ad (Unity C#, 네임스페이스 `Game`)

---

## 2. 현재 아키텍처 요약

### 2.1 3-Phase 파이프라인

```
╔═══════════════════════════════════════════════════╗
║  PHASE 1: 전처리                                  ║
║  [1] Feature Intake → FEATURE_QUEUE.yaml          ║
║  [2] Queue Generator → TASK_QUEUE + REGISTRY      ║
║                        + generated_specs/          ║
╠═══════════════════════════════════════════════════╣
║  PHASE 2: 모듈 생산 루프 (per module)             ║
║  [3] Orchestrator → 의존성 그래프, Builder 배정    ║
║  [4] Planner → plans/<Module>_PLAN.md             ║
║  [5] Builder → Assets/Game/Modules/<Module>/ 코드 ║
║  [6] ★ Human Validator ★ → 파이프라인 정지        ║
║  [7] Reviewer → commit_state: ready / blocked     ║
║      (blocked → [5] 루프, 최대 3회)               ║
╠═══════════════════════════════════════════════════╣
║  PHASE 3: 후처리                                  ║
║  [8] Committer → 5 Gate → git commit              ║
║  [9] Learning Recorder → learning/ 기록           ║
║  [Reviewer] → status: done (최종 마감)            ║
╚═══════════════════════════════════════════════════╝
```

### 2.2 핵심 설계 결정

| 결정 | 근거 |
|------|------|
| 9역할 바톤 패스 | 역할 경계 명확화, 교차 침범 방지 |
| 4차원 상태 모델 | 단일 status의 상태 폭발 방지 |
| Human Validation Gate | AI 자체 완료 불가 — 사람 검증 필수 |
| Reviewer ≠ Committer 분리 | 검증과 커밋의 역할 혼재 방지 |
| Learning Gate | 사람 수정 시 학습 기록 없이 커밋 불가 |
| feature_group 단위 커밋 | 부분 커밋 방지, 원자적 기능 단위 |

---

## 3. 현재 역할 모델 (9 Roles)

| # | 역할 | 트리거 | 핵심 산출물 |
|---|------|--------|------------|
| 1 | Feature Intake | 사용자 요청 | FEATURE_QUEUE.yaml (status: intake) |
| 2 | Queue Generator | FEATURE_QUEUE intake | TASK_QUEUE + MODULE_REGISTRY + Specs |
| 3 | Orchestrator | TASK_QUEUE pending | Builder Pool 배정 (최대 6개) |
| 4 | Planner | status == pending | plans/<Module>_PLAN.md, status → planned |
| 5 | Builder | status == planned | 6파일 코드, human_state → pending |
| 6 | Human Validator ★ | human_state == pending | human_state → validated (사람) |
| 7 | Reviewer | human_state == validated | commit_state → ready / status → blocked |
| 8 | Committer | commit_state == ready (전체) | git commit, commit_state → committed |
| 9 | Learning Recorder | commit_state == committed | learning/ 기록, learning_state → recorded |

Reviewer가 최종 마감: `commit_state ∈ {committed, recommitted} + learning_state == recorded → status → done`

상세: `AGENT_ROLES.md`

---

## 4. 현재 큐 모델

### 4.1 4차원 상태 모델

| 차원 | 필드 | 값 (enum) | 전이 주체 |
|------|------|-----------|-----------|
| 빌드 | `status` | pending → planned → in_progress → review → done / blocked → escalated | Planner, Builder, Reviewer |
| 사람 | `human_state` | none → pending → in_review → fixing → validated | Builder(pending), Human(나머지) |
| 학습 | `learning_state` | none → pending → recorded | System(pending), Learning Rec(recorded) |
| 커밋 | `commit_state` | none → ready → committed → recommit_ready → ready → recommitted | Reviewer(ready), Committer(committed) |

### 4.2 교차 조건 (Cross-Dimension Guards)

| status 전이 | 요구 조건 |
|-------------|-----------|
| in_progress → review | human_state == validated |
| review → done | commit_state ∈ {committed, recommitted} AND learning_state == recorded |
| blocked → in_progress | 리셋: human_state→pending, learning_state→none, commit_state→none |

### 4.3 큐 파일 현황

- `TASK_QUEUE.yaml`: v2 헤더 (4차원 모델 주석 포함). 기존 17개 done 모듈 + 신규 태스크용
- `FEATURE_QUEUE.yaml`: v1, features: [] (비어있음)
- `MODULE_REGISTRY.yaml`: 17개 모듈 등록됨

상세 스키마: `TASK_SCHEMA.md`

---

## 5. 학습 메모리 모델

### 5.1 파일 구조

```
docs/ai/learning/
├── LEARNING_INDEX.md              # 진입점 (에이전트 Quick-Start)
├── RULE_MEMORY.yaml               # 핵심 규칙 (기계 판독, 15+ 규칙)
├── LEARNING_LOG.md                # 시간순 이벤트 로그
├── VALIDATOR_FAILURE_PATTERNS.md  # 12 Validator별 패턴 사전
├── CODING_PATTERNS.md             # BAD/GOOD 코드 패턴 (15+ 패턴)
├── HUMAN_FIX_EXAMPLES.md          # Before/After 사람 수정 사례
├── CROSS_PROJECT_RULES.md         # 프로젝트 간 재사용 (scope: global)
└── RECURRING_MISTAKES.md          # 3회 이상 반복 AI 실수 패턴
```

### 5.2 학습 소비 (입력)

| 역할 | 읽는 파일 | 활용 |
|------|----------|------|
| Queue Generator | RULE_MEMORY, RECURRING_MISTAKES, VALIDATOR_FAILURE_PATTERNS | 위험 플래그, acceptance_criteria |
| Planner | RULE_MEMORY, RECURRING_MISTAKES | PLAN "회피 규칙" 섹션 |
| Builder | CODING_PATTERNS, RULE_MEMORY, HUMAN_FIX_EXAMPLES | 코드 생성 패턴 적용 |
| Reviewer | VALIDATOR_FAILURE_PATTERNS, RULE_MEMORY | 알려진 패턴 집중 검증 |

### 5.3 학습 생산 (출력)

Learning Recorder가 커밋 후 학습 데이터를 수집하여 해당 파일에 append-only로 기록.

---

## 6. 커밋/재커밋 모델

### 6.1 커밋 타입

| 타입 | 용도 | commit_state 전이 |
|------|------|-------------------|
| `feat(<group>)` | 첫 커밋 | ready → committed |
| `fix(<group>)` | 재커밋 | ready → recommitted |
| `chore(ai-learning)` | 학습 독립 커밋 | 태스크 무관 |

### 6.2 5 Gate 체계

| Gate | 확인 | 실패 시 |
|------|------|---------|
| Reviewer Gate | commit_state == ready | Reviewer 미완료 |
| Human Gate | human_state == validated | 사람 미검증 |
| Learning Gate | human_fixes > 0 → learning_state == recorded | Learning 미기록 |
| Completeness Gate | feature_group 전체 ready | 부분 커밋 금지 |
| Scope Gate | 스테이징 범위 | 관련 없는 파일 차단 |

### 6.3 재커밋 경로

```
done → blocked → in_progress → review → ready → recommitted → recorded → done
(commit_state: committed → recommit_ready → ready → recommitted)
```

상세: `COMMIT_RULES.md`

---

## 7. 문서 맵 (Source of Truth)

| 문서 | 역할 | 최종 상태 |
|------|------|-----------|
| `ORCHESTRATION_RULES.md` | **통합 오케스트레이션 명세** — 9역할, I/O 계약, 4차원, 경로 | ✅ v2 통합 완료 |
| `AGENT_ROLES.md` | 9역할 상세, 허용/금지, 권한 매트릭스 | ✅ v2 통합 완료 |
| `STATE_MACHINE.md` | 4차원 전이, 교차 조건, 복합 상태 | ✅ v2 통합 완료 |
| `PROJECT_OVERVIEW.md` | 프로젝트 개요, 아키텍처, 폴더 구조 | ✅ v2 통합 완료 |
| `COMMIT_RULES.md` | 5 Gate, Staging, Recommit, 메시지 규격 | ✅ v2 통합 완료 |
| `QUEUE_GENERATOR.md` | Queue Generator 분해/의존/위험, 예시 | ✅ v2 통합 완료 |
| `FEATURE_INTAKE.md` | Feature 입력 형식, 자연어 변환 예시 | ✅ v2 통합 완료 |
| `TASK_SCHEMA.md` | TASK_QUEUE 필드 정의, enum, 검증 규칙 | ✅ v2 통합 완료 |
| `CODING_RULES.md` | C#/Unity 코딩 규칙 | ✅ (변경 없음) |
| `MODULE_TEMPLATES.md` | 모듈 6파일 구조 | ✅ (변경 없음) |
| `generated_specs/README.md` | Spec 출력 규격 | ✅ v2 신규 |
| `AI_DEVELOPMENT_LOOP.md` | v1 레거시 — SUPERSEDED | ⚠️ 참조용만 |
| `KNOWN_FAILURE_PATTERNS.md` | v1 레거시 — learning/으로 이전 | ⚠️ 참조용만 |

---

## 8. C# 구현 현황

### 8.1 완료된 구현

| 파일 | 역할 |
|------|------|
| `DependencyGraphBuilder.cs` | 의존성 그래프 빌드, 토폴로지 정렬 |
| `OrchestratorSimulator.cs` | 오케스트레이션 시뮬레이션 (독립/혼합/병렬) |
| `ParallelBuilderOrchestrator.cs` | Builder Pool 배정, 모듈 격리 검증 |
| `BuilderAgent.cs` | Builder 에이전트 단위 — 폴더 경계 강제 |
| `ValidationRunner.cs` | 12개 Validator 실행 |
| `FeatureIntake.cs` | Feature 엔트리 파싱 |
| `FeatureDecomposer.cs` | Feature → 모듈 분해 |
| `TaskQueueGenerator.cs` | TASK_QUEUE 엔트리 생성 |
| `SpecGenerator.cs` | generated_specs/ 생성 |
| `FeatureGroupTracker.cs` | feature_group 커밋 준비 판단 |
| `GitCommitStage.cs` | 커밋 실행 (v1 feat만 지원) |
| `DependencyGraphTestRunner.cs` | 의존성 테스트 4개 케이스 |

### 8.2 미구현 (Implementation Gaps)

| 우선순위 | 작업 | 파일 | 이유 |
|---------|------|------|------|
| **1** | `FeatureGroupTracker` — commit_state, human_state, learning_state 파싱 | `FeatureGroupTracker.cs` | 4차원 커밋 판정의 기반 |
| **2** | `DependencyGraphBuilder.TaskEntry` — 4차원 필드 추가 | `DependencyGraphBuilder.cs` | 그래프 빌드 시 새 차원 인식 |
| **3** | `GitCommitStage` — 5 Gate 체크, fix 타입, Learning Gate | `GitCommitStage.cs` | COMMIT_RULES 구현 |
| **4** | `GitCommitStage.BuildCommitMessage` — fix 타입 + 확장 메타데이터 | `GitCommitStage.cs` | v2 커밋 메시지 규격 |
| **5** | Cursor rule `.cursor/rules/autonomous-pipeline.mdc` — 4차원 초기값 자동 생성 | `.cursor/rules/` | Queue Generator 산출물 정합 |

---

## 9. 다음 추천 작업 (Implementation Thread)

### Phase 1: C# 4차원 통합 (우선순위 최상)

```
1. FeatureGroupTracker.cs
   → commit_state, human_state, learning_state 파싱 추가
   → AllCommitReady, HasHumanFixes, LearningComplete 프로퍼티

2. DependencyGraphBuilder.TaskEntry
   → HumanState, LearningState, CommitState 필드 추가
   → 기존 status 기반 로직은 유지 (하위 호환)

3. GitCommitStage.cs
   → CommitCandidate에 4차원 게이트 필드 추가
   → PrepareCommit에 5 Gate 체크 구현
   → BuildCommitMessage에 fix 타입 + 확장 메타 구현
   → Learning Gate 체크 추가
```

### Phase 2: 첫 v2 파이프라인 실행

```
4. 실제 Feature 하나를 FEATURE_QUEUE.yaml에 intake로 등록
5. Queue Generator로 TASK_QUEUE 생성 테스트
6. Builder로 모듈 생성 → Human Validation → Reviewer → Committer → Learning Recorder
7. 전체 사이클 RUN_LOG 기록
```

### Phase 3: 학습 루프 검증

```
8. Learning Recorder가 learning/ 파일에 실제 데이터 기록 테스트
9. 다음 생성에서 Builder/Planner가 학습 데이터를 참조하는지 확인
10. chore(ai-learning) 독립 커밋 테스트
```

---

## 10. 현재 알려진 갭

| 카테고리 | 갭 | 영향 |
|---------|-----|------|
| C# 코드 | FeatureGroupTracker가 commit_state를 파싱하지 않음 | 4차원 커밋 판정 불가 |
| C# 코드 | GitCommitStage가 feat만 지원 (fix/recommit 미지원) | 재커밋 워크플로 불가 |
| C# 코드 | DependencyGraphBuilder.TaskEntry에 4차원 필드 없음 | 그래프에서 새 차원 활용 불가 |
| 문서 | TASK_QUEUE.yaml의 기존 17개 done 엔트리에 4차원 필드 없음 | 의도적 — 하위 호환, 새 태스크부터 적용 |
| 문서 | FEATURE_QUEUE.yaml이 비어있음 | 아직 v2 파이프라인 실행 전 |
| 프로세스 | 전체 9단계 파이프라인 end-to-end 실행 실적 없음 | Phase 2에서 검증 필요 |
| 레거시 | AI_DEVELOPMENT_LOOP.md — v1 문서 (SUPERSEDED) | 참조용, 혼란 가능성 |
| 레거시 | KNOWN_FAILURE_PATTERNS.md — learning/으로 이전 완료 | 참조용, 혼란 가능성 |

---

## 11. 수정 금지 영역

| 영역 | 이유 |
|------|------|
| `Assets/Editor/AI/` | 검증 시스템 — 공유 인프라 |
| `Assets/Game/Core/` | editable: false (명시적 허용 필요) |
| `Assets/Game/Modules/Template/` | 참조용 원본 |
| `.cursor/rules/` | 사용자만 수정 |

---

## 12. 에이전트 Quick-Start

새 에이전트(또는 새 대화)가 작업을 시작할 때:

1. **이 문서** (`PROJECT_HANDOFF.md`)를 읽어 전체 맥락을 파악한다
2. `ORCHESTRATION_RULES.md`를 읽어 9역할 I/O 계약과 상태 전이를 확인한다
3. `learning/LEARNING_INDEX.md`를 읽어 학습 시스템 구조를 파악한다
4. `TASK_QUEUE.yaml`로 현재 큐 상태를 확인한다
5. `MODULE_REGISTRY.yaml`로 기존 모듈과 의존성을 확인한다
6. 작업에 해당하는 역할 문서를 읽는다:
   - Feature 요청 → `FEATURE_INTAKE.md`, `QUEUE_GENERATOR.md`
   - 모듈 구현 → `CODING_RULES.md`, `MODULE_TEMPLATES.md`
   - 커밋 → `COMMIT_RULES.md`
   - 학습 기록 → `AGENT_ROLES.md` §5

---

## 13. 추천 다음 프롬프트

```
You are continuing work on:
Autonomous Game Factory v2 — Human-in-the-Loop Learning Pipeline

Your task is to implement the 4-dimensional state model in C#.

## Requirements

1. Update FeatureGroupTracker.cs:
   - Parse commit_state, human_state, learning_state from TASK_QUEUE.yaml
   - Add AllCommitReady, HasHumanFixes, LearningComplete properties

2. Update DependencyGraphBuilder.TaskEntry:
   - Add HumanState, LearningState, CommitState string fields
   - Maintain backward compatibility (missing fields default to appropriate values)

3. Update GitCommitStage.cs:
   - Implement 5 Gate checks (COMMIT_RULES.md §2)
   - Support fix type commits (recommit)
   - Add Learning Gate check

## References
- docs/ai/PROJECT_HANDOFF.md §8-9
- docs/ai/COMMIT_RULES.md §2, §6, §12
- docs/ai/STATE_MACHINE.md §5
- docs/ai/ORCHESTRATION_RULES.md §4

Do not ask for confirmation. Inspect existing code and implement directly.
```
