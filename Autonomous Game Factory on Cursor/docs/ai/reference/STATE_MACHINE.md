# Task State Machine — Human-in-the-Loop Learning Pipeline

이 문서는 TASK_QUEUE.yaml의 태스크 상태를 정의하는 공식 명세서다.

---

## 1. 설계 원칙

단일 `status` 필드로 빌드 진행, 사람 검증, 학습 기록, 커밋 상태를 동시에 표현하면 상태 폭발이 발생하고 전이 규칙이 비직관적이 된다.

따라서 태스크 상태를 **4개 독립 차원(dimension)**으로 분리한다:

| 차원 | 필드명 | 추적 대상 | 소유자 |
|------|--------|-----------|--------|
| **빌드 진행** | `status` | AI 파이프라인 단계 | Planner, Builder, Reviewer |
| **사람 검증** | `human_state` | Human Validation Gate 상태 | Human, Builder |
| **학습 기록** | `learning_state` | 학습 데이터 기록 여부 | Learning Recorder |
| **커밋 상태** | `commit_state` | Git 커밋/재커밋 상태 | Committer |

각 차원은 독립적으로 전이하며, 교차 조건(cross-dimension guards)으로 안전을 보장한다.

---

## 2. 차원 1: status (빌드 진행)

AI 에이전트의 작업 단계를 추적한다. 기존 파이프라인과의 하위 호환성을 유지한다.

### 2.1 상태 목록

| 상태 | 설명 |
|------|------|
| `pending` | 큐에 등록됨, 아무도 건드리지 않음 |
| `planned` | Planner가 PLAN을 작성 완료 |
| `in_progress` | Builder가 코드 작성 중 |
| `review` | AI Reviewer가 검증 중 |
| `done` | 전체 파이프라인 완료 (커밋 + 학습 완료) |
| `blocked` | 검증 실패 또는 의존 모듈 미완성 |
| `escalated` | 3회 재시도 실패, 사람에게 에스컬레이션 |

### 2.2 전이 다이어그램

```
pending ──► planned ──► in_progress ──► review ──► done
                              │            │
                              │            ▼
                              │         blocked
                              │            │
                              ◄────────────┘
                         (Builder 수정)
                                           │
                                           ▼ (3회 실패)
                                       escalated
```

### 2.3 전이 규칙

| 전이 | 주체 | 선행 조건 |
|------|------|-----------|
| `pending → planned` | Planner | PLAN 문서 작성 완료 |
| `planned → in_progress` | Builder | depends_on 모듈 모두 done, owner 할당 |
| `in_progress → review` | Builder | 코드 생성 완료, 자체 점검 통과, `human_state == validated` |
| `review → done` | Reviewer | `commit_state ∈ {committed, recommitted}` + `learning_state == recorded` |
| `review → blocked` | Reviewer | 검증 실패, 실패 사유 기록 |
| `blocked → in_progress` | Builder | 수정 완료. 리셋: human_state→pending, learning_state→none, commit_state→none |
| `blocked → escalated` | System | `retry_count >= 3` |

### 2.4 교차 조건 (Cross-Dimension Guards)

`status`를 전이하려면 다른 차원의 상태가 특정 조건을 충족해야 한다:

| status 전이 | human_state 조건 | learning_state 조건 | commit_state 조건 |
|-------------|-----------------|--------------------|--------------------|
| `in_progress → review` | `== validated` | (무관) | (무관) |
| `review → done` | `== validated` | `== recorded` | `∈ {committed, recommitted}` |
| `review → blocked` | (무관) | (무관) | (무관) |
| `blocked → in_progress` | 자동 `pending`으로 리셋 | 자동 `none`으로 리셋 | 자동 `none`으로 리셋 |

**핵심: `in_progress → review` 전이는 `human_state == validated`가 필수다.**
이것이 Human Validation Gate를 강제하는 메커니즘이다.

---

## 3. 차원 2: human_state (사람 검증)

사람이 Unity Editor에서 수행하는 검증과 수정을 추적한다.

### 3.1 상태 목록

| 상태 | 설명 |
|------|------|
| `none` | 아직 사람 검증 단계 아님 (pending, planned) |
| `pending` | Builder가 코드 생성 완료, 사람 검증 대기 |
| `in_review` | 사람이 검증 중 (Unity Validator 실행, 코드 확인) |
| `fixing` | 사람이 코드를 직접 수정 중 |
| `validated` | 사람이 검증 완료 (수정 포함/미포함) |

### 3.2 전이 다이어그램

```
none ──► pending ──► in_review ──► validated
                        │
                        ▼
                      fixing ──► validated
                        ▲           │
                        │           │ (재수정 필요 시)
                        └───────────┘
```

### 3.3 전이 규칙

| 전이 | 주체 | 조건 |
|------|------|------|
| `none → pending` | Builder | `status`가 `in_progress`이고 코드 생성 완료 |
| `pending → in_review` | Human | Unity Editor에서 검증 시작 |
| `in_review → validated` | Human | Validator PASS, 수정 불필요 |
| `in_review → fixing` | Human | 에러 발견, 직접 수정 시작 |
| `fixing → validated` | Human | 수정 완료, `human_fixes` 기록 |
| `validated → pending` | System | `status`가 `blocked → in_progress`로 돌아갈 때 자동 리셋 |

### 3.4 human_fixes 필드

`human_state`가 `fixing` 또는 `validated`일 때, 사람이 수정한 내역을 `human_fixes` 배열에 기록한다:

```yaml
human_fixes:
  - file: "EconomyRuntime.cs"
    change: "foreach → for 변환"
    rationale: "CODING_RULES에서 foreach 금지"
    timestamp: "2026-03-18T14:30:00"
```

---

## 4. 차원 3: learning_state (학습 기록)

커밋 사이클에서 발생한 학습 데이터의 기록 상태를 추적한다.

### 4.1 상태 목록

| 상태 | 설명 |
|------|------|
| `none` | 아직 학습 기록 단계 아님 |
| `pending` | 커밋 완료, 학습 기록 대기 |
| `recorded` | Learning Recorder가 학습 데이터 기록 완료 |

### 4.2 전이 다이어그램

```
none ──► pending ──► recorded
```

### 4.3 전이 규칙

| 전이 | 주체 | 조건 |
|------|------|------|
| `none → pending` | System | `commit_state`가 `committed`가 될 때 자동 |
| `pending → recorded` | Learning Recorder | 학습 데이터 기록 완료 |
| `recorded → pending` | System | 재커밋 발생 시 리셋 |

### 4.4 기록 대상

Learning Recorder는 다음을 수집하여 기록한다:

| 항목 | 저장소 |
|------|--------|
| 핵심 규칙 | `learning/RULE_MEMORY.yaml` |
| 시간순 이벤트 로그 | `learning/LEARNING_LOG.md` |
| Validator 실패 패턴 | `learning/VALIDATOR_FAILURE_PATTERNS.md` |
| 사람 수정 Before/After | `learning/HUMAN_FIX_EXAMPLES.md` |
| 반복 패턴 (3회 이상) | `learning/RECURRING_MISTAKES.md` |
| 범용 교훈 (scope: global) | `learning/CROSS_PROJECT_RULES.md` |

---

## 5. 차원 4: commit_state (커밋 상태)

Git 커밋/재커밋의 진행 상태를 추적한다.

### 5.1 상태 목록

| 상태 | 설명 |
|------|------|
| `none` | 아직 커밋 단계 아님 |
| `ready` | feature_group 내 모든 모듈이 Reviewer 검증 통과 (validated), 커밋 가능 |
| `committed` | Committer가 git commit 완료 |
| `recommit_ready` | 사후 수정 발생, 재커밋 필요 |
| `recommitted` | 재커밋 완료 |

### 5.2 전이 다이어그램

```
none ──► ready ──► committed
           ▲          │
           │          ▼ (사후 이슈)
           └── recommit_ready
                      │
                      ▼
                  recommitted
```

### 5.3 전이 규칙

| 전이 | 주체 | 조건 |
|------|------|------|
| `none → ready` | Reviewer | `status == review` 진입 시 Reviewer가 통과 판정 |
| `ready → committed` | Committer | feature_group 전체 `ready`, git commit 성공 |
| `committed → recommit_ready` | Human/System | 사후 이슈 발견, 수정 필요 |
| `recommit_ready → ready` | Reviewer | 재검증 통과 |
| `ready → recommitted` | Committer | 재커밋 완료 (이전 커밋 이력 존재) |

### 5.4 커밋 조건 (Commit Gate)

Committer가 `ready → committed`를 실행하려면:

```
모든 모듈 in feature_group:
  status == review (Reviewer 검증 중이거나 통과)
  human_state == validated
  commit_state == ready
AND
  feature_group 내 blocked 모듈 == 0
```

---

## 6. 상태 조합 매핑 (Composite State)

4개 차원의 조합으로 태스크의 실제 "상황"을 결정한다.

### 6.1 주요 복합 상태

| 상황 | status | human_state | learning_state | commit_state |
|------|--------|-------------|----------------|--------------|
| 신규 등록 | `pending` | `none` | `none` | `none` |
| 기획 완료 | `planned` | `none` | `none` | `none` |
| AI 빌드 중 | `in_progress` | `none` | `none` | `none` |
| 사람 검증 대기 | `in_progress` | `pending` | `none` | `none` |
| 사람 검증 중 | `in_progress` | `in_review` | `none` | `none` |
| 사람 수정 중 | `in_progress` | `fixing` | `none` | `none` |
| 사람 검증 완료 | `in_progress` | `validated` | `none` | `none` |
| AI 재검증 중 | `review` | `validated` | `none` | `none` |
| 검증 실패 | `blocked` | `validated` | `none` | `none` |
| 재수정 후 대기 | `in_progress` | `pending` | `none` | `none` |
| 커밋 준비 | `review` | `validated` | `none` | `ready` |
| 커밋 완료 | `review` | `validated` | `pending` | `committed` |
| 학습 기록 완료 | `done` | `validated` | `recorded` | `committed` |
| 재커밋 필요 | `blocked` | `pending` | `recorded` | `recommit_ready` |
| 에스컬레이션 | `escalated` | * | * | * |

### 6.2 터미널 상태

| 상태 | 의미 |
|------|------|
| `done` + `validated` + `recorded` + `committed` | 정상 완료 |
| `done` + `validated` + `recorded` + `recommitted` | 재커밋 후 완료 |
| `escalated` | 사람이 직접 해결해야 함 |

---

## 7. 전체 라이프사이클 흐름

### 7.1 정상 경로 (Happy Path)

```
[1] 큐 등록
    status: pending → planned
    human_state: none
    learning_state: none
    commit_state: none

[2] AI 빌드
    status: planned → in_progress
    human_state: none → pending (코드 생성 완료 시)

[3] 사람 검증
    human_state: pending → in_review → validated
    (수정 필요 시: in_review → fixing → validated)

[4] AI 리뷰
    status: in_progress → review (human_state == validated 필수)
    commit_state: none → ready (Reviewer 통과 시)

[5] 커밋
    commit_state: ready → committed
    learning_state: none → pending (자동)

[6] 학습 기록
    learning_state: pending → recorded

[7] 완료
    status: review → done
    (모든 차원이 터미널 상태)
```

### 7.2 검증 실패 경로 (Blocked Path)

```
[4'] AI 리뷰 실패
    status: review → blocked
    Reviewer가 실패 사유 기록

[5'] Builder 재수정
    status: blocked → in_progress
    human_state: 자동 → pending (리셋)
    retry_count += 1

[3'] 사람 재검증
    human_state: pending → in_review → validated

[4''] AI 재검증
    status: in_progress → review
    → 통과 시 [5]로
    → 실패 시 retry_count < 3이면 [5']로 반복

[E] 에스컬레이션
    retry_count >= 3 → status: blocked → escalated
```

### 7.3 사후 재커밋 경로

```
[R1] 사후 이슈 발견
    status: done → blocked
    commit_state: committed → recommit_ready
    human_state: validated → pending

[R2] Builder 수정 → 사람 검증 → AI 리뷰
    (정상 경로 [2]~[4] 반복)

[R3] 재커밋
    commit_state: recommit_ready → recommitted
    learning_state: recorded → pending → recorded

[R4] 완료
    status: review → done
```

---

## 8. 의존성과 상태의 관계

### 8.1 의존 모듈 상태 요구

| 하위 모듈 전이 | 상위 모듈 요구 상태 |
|---------------|-------------------|
| `planned → in_progress` | 모든 depends_on의 `status == done` |
| (나머지 전이) | 의존 무관 |

### 8.2 의존 모듈이 blocked/escalated일 때

- 하위 모듈은 `planned`에 머문다 (진행 불가)
- 상위 모듈이 `done`이 되면 자동으로 진행 가능

### 8.3 의존 모듈이 done에서 blocked로 돌아갈 때

- 이미 `in_progress` 이후인 하위 모듈에는 **영향 없음** (이미 코드 참조가 성립)
- 아직 `planned`인 하위 모듈은 진행 불가 (상위 done 해제)

---

## 9. 실패 처리 정책

### 9.1 재시도 제한

| 항목 | 제한 | 초과 시 |
|------|------|---------|
| AI 검증 실패 → 재수정 | 3회 | `escalated` |
| 사람 수정 라운드 | 제한 없음 | (사람 판단) |
| 재커밋 | 제한 없음 | (사람 판단) |

### 9.2 escalated 상태

- AI 에이전트는 `escalated` 태스크를 건드리지 않는다.
- 사람이 직접 해결하고 `status`를 `planned` 또는 `in_progress`로 수동 복원한다.
- 복원 시 `retry_count`를 0으로 리셋한다.

### 9.3 부분 실패

feature_group 내 일부 모듈만 `blocked`/`escalated`인 경우:
- 해당 feature_group 전체가 커밋 불가
- 정상 모듈은 `review` 상태에서 대기
- blocked 모듈이 해소되면 feature_group 전체 커밋 진행

---

## 10. 하위 호환성

### 10.1 기존 코드와의 관계

기존 `DependencyGraphBuilder.cs`, `OrchestratorSimulator.cs` 등은 `status` 필드만 읽는다.
새 차원(`human_state`, `learning_state`, `commit_state`)은 추가 필드이므로 기존 YAML 파서가 무시한다.

기존 코드가 검사하는 상태값:
- `"planned"` — 실행 가능 여부 판정
- `"done"` — 의존 충족 판정
- `"in_progress"`, `"review"` — 진행 중 판정
- `"blocked"` — 차단 판정

이 값들은 모두 유지된다. 새 차원은 **문서적 규칙으로 강제**하되, C# 코드 업데이트는 별도 작업이다.

### 10.2 기존 done 모듈

이미 `done`인 모듈(현재 TASK_QUEUE의 17개)은 암묵적으로:
```yaml
human_state: validated    # 이전 프로세스에서 사람이 승인한 것으로 간주
learning_state: none      # 학습 시스템 도입 전이므로 기록 없음
commit_state: committed   # 이미 커밋 완료
```

기존 엔트리에 새 필드를 추가하지 않아도 된다. 새로 생성되는 태스크부터 새 필드를 사용한다.

---

## 11. 에이전트별 읽기/쓰기 권한

9개 역할의 4차원 상태 전이 권한:

| 차원 | Feature Intake | Queue Gen | Orchestrator | Planner | Builder | Human | Reviewer | Committer | Learning Rec |
|------|---------------|-----------|-------------|---------|---------|-------|----------|-----------|-------------|
| `status` | - | W(pending) | - | W(planned) | W(in_progress) | R | W(done,blocked) | R | R |
| `human_state` | - | - | - | R | W(pending) | W(전체) | R | R | R |
| `learning_state` | - | - | - | R | R | R | R | R | W(recorded) |
| `commit_state` | - | - | - | R | R | R | W(ready) | W(committed) | R |

통합 권한 매트릭스 (소스 코드, git, 학습 파일 포함): `AGENT_ROLES.md` §8, `ORCHESTRATION_RULES.md` §16

---

## 12. 요약

| 설계 결정 | 근거 |
|-----------|------|
| 4차원 분리 | 단일 status의 상태 폭발 방지, 각 차원의 독립 전이 |
| `human_state`를 `in_progress → review`의 guard로 | Human Validation Gate 강제 |
| `commit_state`를 `review → done`의 guard로 | Committer 분리 강제 |
| `learning_state`를 `review → done`의 guard로 | 학습 기록 강제 |
| `escalated` 상태 추가 | 무한 루프 방지 + 사람 개입 명시 |
| 기존 status 값 유지 | DependencyGraphBuilder 등 기존 C# 코드 하위 호환 |
| 새 필드는 기존 엔트리에 선택적 | 기존 done 모듈 소급 불필요 |

---

## 13. 병렬 실행 참고

**Cursor는 단일 에이전트다.** v3.0의 execution 차원(execution_status, dependency_status, lease_status)은
멀티 에이전트 환경을 전제로 설계되었으나, 현재 실행 환경에서는 사용하지 않는다.

기존 4차원(status, human_state, learning_state, commit_state)만으로 상태를 추적한다.
병렬 조건은 `EXECUTION_ENTRYPOINT.md` §5를 따른다.
