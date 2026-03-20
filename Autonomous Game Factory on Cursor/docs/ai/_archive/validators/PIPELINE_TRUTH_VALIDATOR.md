# PipelineTruthValidator

> Version: 1.0  
> Category: **Blocking**  
> Registered in: `ValidationRunner.cs`  
> Source: `Assets/Editor/AI/Validators/PipelineTruthValidator.cs`

## 목적

파이프라인이 **거짓 성공 상태**를 보고하는 것을 방지한다.  
모든 상태 소스 (AIValidationReport.json, TASK_QUEUE.yaml, FEATURE_QUEUE.yaml)가 **일관되게 진실**을 반영하는지 검증한다.

## 검증 대상

### 1. Validation Report vs Commit State

| 조건 | 판정 |
|---|---|
| AIValidationReport.json의 ErrorCount > 0이고 Passed=true | **Error** (거짓 PASS) |
| ErrorCount > 0인데 TASK_QUEUE의 commit_state가 ready/committed | **Error** (검증 실패 상태에서 커밋 진행) |

### 2. TASK_QUEUE 상태 진실성

| 조건 | 판정 |
|---|---|
| status=review/done이지만 human_state≠validated (none 제외) | **Error** (Human Gate 우회) |
| status=done이지만 commit_state≠committed/recommitted (none 제외) | **Error** (커밋 없이 완료 주장) |
| arch_diff_blocked=true이지만 status가 planned/in_progress/review/done | **Error** (아키텍처 블로킹 무시) |
| commit_state=ready이지만 status가 review/done이 아님 | **Error** (비정상 커밋 준비) |

### 3. FEATURE_QUEUE vs TASK_QUEUE 일관성

| 조건 | 판정 |
|---|---|
| Feature status=done이지만 하위 모듈 중 status≠done인 것 존재 | **Error** (불완전한 Feature를 완료로 표시) |

## Severity

- 모든 위반은 **Error** (blocking)
- 이 Validator가 발견한 문제가 있으면 파이프라인은 PASS를 보고할 수 없다

## 진실 소스 매핑

```
AIValidationReport.json → ErrorCount, Passed
TASK_QUEUE.yaml → status, human_state, commit_state, learning_state, arch_diff_blocked
FEATURE_QUEUE.yaml → status, modules[]
```

## 실패 케이스 예시

### Case 1: 거짓 PASS 보고
```
AIValidationReport.json: { ErrorCount: 3, Passed: true }
→ Error: "Report must not claim PASS when blocking errors exist"
```

### Case 2: Human Gate 우회
```
TASK_QUEUE:
  - name: NewModule
    status: review
    human_state: pending
→ Error: "human_state must be 'validated' before status can be review/done"
```

### Case 3: Feature 조기 완료
```
FEATURE_QUEUE:
  - name: feature-x
    status: done
    modules: [ModA, ModB]

TASK_QUEUE:
  - name: ModA, status: done
  - name: ModB, status: in_progress
→ Error: "all feature modules must be done before feature is done"
```

## 통합 위치

- `ValidationRunner.cs`에서 `RegressionGuardianValidator` 이후 실행
- 결과는 `AIValidationReport.json`에 기록
- Console에 Error 레벨로 출력

## 재테스트 시나리오

1. TASK_QUEUE에서 status=done, human_state=pending인 엔트리를 만들고 실행 → Error 발생 확인
2. AIValidationReport.json을 수동으로 ErrorCount=1, Passed=true로 편집 → Error 발생 확인
3. FEATURE_QUEUE에서 done인 feature의 하위 모듈 하나를 in_progress로 변경 → Error 발생 확인
