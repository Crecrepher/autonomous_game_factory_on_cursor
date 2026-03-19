# Parallel Execution Proof — AGF v2.3

이 문서는 AI가 병렬 실행을 주장할 때 반드시 제공해야 하는 증거 기준을 정의한다.
병렬 실행을 설명하는 것은 병렬 실행이 아니다.

---

## 1. 핵심 원칙

**"Describing parallelism is NOT parallel execution."**

병렬 실행은 실제로 여러 서브에이전트가 동시에 작업할 때만 유효하다.
"이 작업들을 병렬로 실행할 수 있다"고 서술하는 것은 순차 실행이다.

---

## 2. 유효한 병렬 실행의 조건

병렬 실행이 유효하려면 아래 조건을 **모두** 충족해야 한다.

### 2.1 서브에이전트 사용

| 조건 | 설명 |
|------|------|
| 서브에이전트 실제 사용 | Task 도구를 통해 서브에이전트가 실제로 생성되었다 |
| 동시 실행 | 2개 이상의 서브에이전트가 동일 메시지 턴에서 동시에 시작되었다 |
| 독립적 작업 | 각 서브에이전트의 작업이 서로 의존하지 않는다 |

### 2.2 모듈 소유권 분리

| 조건 | 설명 |
|------|------|
| 명확한 모듈 배정 | 각 서브에이전트에 담당 모듈이 명시적으로 배정되었다 |
| 파일 책임 분리 | 서브에이전트 간 수정 파일이 겹치지 않는다 |
| 충돌 없음 | 동일 파일을 2개 이상의 서브에이전트가 수정하지 않는다 |

### 2.3 실제 파일 변경

| 조건 | 설명 |
|------|------|
| 파일 수정 | 각 서브에이전트가 실제로 파일을 생성/수정했다 |
| 결과 검증 | 수정된 파일에 린터 에러가 없다 |
| CODING_RULES 준수 | 모든 생성/수정 코드가 규칙을 준수한다 |

---

## 3. 필수 증거 (Proof)

병렬 실행을 주장하는 응답은 아래 증거를 반드시 포함해야 한다.

### 3.1 서브에이전트 목록

```
Parallel Execution Proof:
- Subagent Count: <N>
- Subagents:
  - Subagent 1:
    - Type: <generalPurpose | explore | shell | etc.>
    - Module: <담당 모듈 이름>
    - Files Modified:
      - <file_path_1>
      - <file_path_2>
  - Subagent 2:
    - Type: <type>
    - Module: <담당 모듈 이름>
    - Files Modified:
      - <file_path_3>
      - <file_path_4>
```

### 3.2 소유권 매트릭스

```
Module Ownership Matrix:
| Subagent | Modules | Files | Status |
|----------|---------|-------|--------|
| 1 | PuzzleBlock | 6 files | Complete |
| 2 | PuzzleBoard | 6 files | Complete |
| 3 | BattleEffect | 6 files | Complete |
```

### 3.3 파일 충돌 검증

```
File Conflict Check:
- Overlapping files: 0
- Conflict detected: false
```

파일 충돌이 감지되면 병렬 실행 결과는 **무효**이다.

---

## 4. 유효하지 않은 병렬 실행

아래 경우는 병렬 실행으로 인정하지 않는다.

### 4.1 서술만 있는 경우

```
INVALID:
"PuzzleBlock과 PuzzleBoard를 병렬로 처리한다."
→ 서브에이전트 사용 증거 없음 → 순차 실행으로 판정
```

### 4.2 순차 실행을 병렬이라고 주장

```
INVALID:
"먼저 PuzzleBlock을 수정하고, 다음에 PuzzleBoard를 수정한다."
→ 순차적 파일 수정 → 순차 실행
```

### 4.3 단일 에이전트의 다중 도구 호출

```
VALID (Partial):
단일 에이전트가 같은 메시지 턴에서 여러 도구를 동시 호출하는 것은
"도구 수준 병렬 호출"이며, 서브에이전트 병렬 실행과는 구분된다.
이 경우 "Tool-level parallelism"으로 표기해야 한다.
```

### 4.4 판정 규칙

```
IF 서브에이전트 수 == 0:
    EXECUTION_TYPE = "sequential"

IF 서브에이전트 수 >= 2 AND 동시 시작 AND 모듈 소유권 분리:
    EXECUTION_TYPE = "parallel"

IF 서브에이전트 수 >= 2 AND 순차 시작:
    EXECUTION_TYPE = "sequential (with subagents)"

IF 도구 호출만 병렬 AND 서브에이전트 없음:
    EXECUTION_TYPE = "tool-level parallelism"
```

---

## 5. 병렬 실행이 권장되는 경우

| 상황 | 이유 |
|------|------|
| 신규 모듈 3개 이상 동시 생성 | 모듈 간 의존성이 없으면 병렬 가능 |
| 독립된 코딩 규칙 위반 수정 | 파일이 겹치지 않으면 병렬 가능 |
| 여러 모듈의 테스트 생성 | 각 모듈 테스트는 독립적 |

---

## 6. 병렬 실행이 금지되는 경우

| 상황 | 이유 |
|------|------|
| 의존성이 있는 모듈 | depends_on 관계가 있으면 순차 실행 필수 |
| 동일 파일 수정 | 충돌 위험 |
| YAML 파일 동시 수정 | TASK_QUEUE/MODULE_REGISTRY는 단일 에이전트가 관리 |
| Architecture Diff → Builder | 단계 간 의존성으로 순차 필수 |

---

## 7. 보고 형식

병렬 실행을 사용한 작업의 최종 보고에는 반드시 아래를 포함한다:

```
## Execution Mode
- Type: parallel | sequential | tool-level parallelism
- Subagent Count: <N>
- Modules per Subagent:
  - Subagent 1: [<module_list>]
  - Subagent 2: [<module_list>]
- Files Modified per Subagent:
  - Subagent 1: [<file_list>]
  - Subagent 2: [<file_list>]
- File Conflicts: 0
- Result: VALID | INVALID
```

---

## 8. 글로벌 규칙

- AGF v2.3 기존 파이프라인만 사용한다.
- 신규 아키텍처를 발명하지 않는다.
- 시스템을 재설계하지 않는다.
- Human Validator를 우회하지 않는다.
- 병렬이든 순차든, 모든 실행은 동일한 코딩 규칙과 파이프라인 규칙을 따른다.
- 병렬 실행이 완료 기준을 낮추지 않는다.

---

## ENFORCEMENT

**All future AI tasks must follow this document.**
**Any response violating this document is invalid.**

서브에이전트 수준의 소유권이 보이지 않으면 해당 실행은 순차로 간주된다.
병렬 실행을 서술하는 것은 병렬 실행이 아니다.
이 기준은 모든 AI 에이전트에 동일하게 적용된다.
