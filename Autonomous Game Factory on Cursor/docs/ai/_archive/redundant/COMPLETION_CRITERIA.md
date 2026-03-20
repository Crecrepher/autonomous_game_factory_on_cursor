# Completion Criteria — AGF v2.3

이 문서는 AI 작업의 완료 조건과 실패 조건을 엄격하게 정의한다.
모든 AI 에이전트는 이 기준을 따라야 하며, 이 기준을 충족하지 않는 응답은 유효하지 않다.

---

## 1. 완료 조건 (COMPLETE)

작업이 **완료**되었다고 판정하려면 아래 조건을 **모두** 충족해야 한다.

### 1.1 YAML 상태 업데이트

| 파일 | 조건 |
|------|------|
| `docs/ai/TASK_QUEUE.yaml` | 관련 모듈 엔트리가 존재하고, `status`가 올바르게 전이됨 |
| `docs/ai/MODULE_REGISTRY.yaml` | 신규 모듈이 있으면 등록 완료. dependencies가 TASK_QUEUE와 일치 |
| `docs/ai/FEATURE_QUEUE.yaml` | 관련 feature 엔트리가 존재하고 status가 업데이트됨 |

### 1.2 코드 파일 변경

| 조건 | 설명 |
|------|------|
| 모듈 파일 존재 | 6파일 구조(I, Config, Runtime, Factory, Bootstrap, Tests) 완비 |
| 코드 규칙 준수 | CODING_RULES.md의 모든 규칙 통과 |
| GC-Free | 코루틴, 람다, LINQ, foreach, Invoke 없음 |
| 매직넘버 없음 | 모든 리터럴이 const UPPER_SNAKE_CASE |
| GetComponent 런타임 없음 | 런타임 코드에서 GetComponent/Find 계열 호출 없음 |
| null conditional 없음 | `?.` `??` 사용 없음 |
| 린터 에러 0건 | 수정된 파일에 린터 에러가 없음 |

### 1.3 파이프라인 상태

| 조건 | 설명 |
|------|------|
| human_state: pending | 모든 관련 모듈이 Human Validation 대기 상태 |
| status: in_progress | Builder가 코드 생성을 완료했음 |
| arch_diff_blocked: false | Architecture Diff에서 critical 차단이 없음 |

---

## 2. 실패 조건 (FAILURE)

아래 중 **하나라도** 해당하면 작업은 **실패**이다.

### 2.1 파일 미수정

```
IF 수정된 파일 수 == 0:
    STATUS = FAILURE
    REASON = "파일이 수정되지 않았다. 작업이 실행되지 않은 것이다."
```

**"파일이 수정되지 않으면 작업은 아직 진행 중이다."**

### 2.2 설명만 출력

```
IF 응답이 텍스트 설명/분석/보고서만 포함하고 파일 변경이 없다면:
    STATUS = FAILURE
    REASON = "분석은 실행이 아니다. 파일을 수정해야 한다."
```

### 2.3 계획만 출력

```
IF 응답이 계획/TODO/단계 목록만 포함하고 파일 변경이 없다면:
    STATUS = FAILURE
    REASON = "계획 수립은 실행이 아니다. 계획을 실행해야 한다."
```

### 2.4 파이프라인 미준수

```
IF GDD가 입력되었는데 7단계 파이프라인이 모두 실행되지 않았다면:
    STATUS = FAILURE
    REASON = "파이프라인 단계가 누락되었다."
```

### 2.5 YAML 불일치

```
IF TASK_QUEUE.yaml과 MODULE_REGISTRY.yaml 간 의존성이 불일치하면:
    STATUS = FAILURE
    REASON = "YAML 일관성 위반."
```

### 2.6 코딩 규칙 위반

```
IF 생성/수정된 코드에 CODING_RULES.md 위반이 있으면:
    STATUS = FAILURE
    REASON = "코딩 규칙 위반. 수정 후 재검증 필요."
```

### 2.7 Human Validator 우회

```
IF human_state != validated인 상태에서 commit_state를 ready로 전이하면:
    STATUS = FAILURE
    REASON = "Human Validation Gate 우회."
```

---

## 3. 작업 유형별 완료 기준

### 3.1 GDD 기반 신규 기능

| 필수 산출물 | 조건 |
|-----------|------|
| FEATURE_QUEUE.yaml | 엔트리 추가, status: in_progress |
| TASK_QUEUE.yaml | 모든 모듈 엔트리 추가, status: in_progress |
| MODULE_REGISTRY.yaml | 신규 모듈 등록 |
| 모듈 코드 | 6파일 구조 × N개 모듈 |
| 테스트 | 모듈당 최소 2개 |
| 린터 | 에러 0건 |
| 파이프라인 상태 | human_state: pending |

### 3.2 기존 코드 수정/버그 픽스

| 필수 산출물 | 조건 |
|-----------|------|
| 코드 파일 수정 | 실제 파일 변경 |
| 린터 | 에러 0건 |
| CODING_RULES.md 준수 | 위반 없음 |

### 3.3 코딩 규칙 위반 수정

| 필수 산출물 | 조건 |
|-----------|------|
| 코드 파일 수정 | Before → After 코드 변경 |
| 위반 0건 | 수정 후 재스캔하여 위반 없음 확인 |
| 린터 | 에러 0건 |

### 3.4 프로젝트 스캔/감사

| 필수 산출물 | 조건 |
|-----------|------|
| 위반 목록 | 구체적인 파일 경로 + 줄 번호 |
| 즉시 수정 | 발견된 위반을 즉시 수정 |
| 린터 | 수정 후 에러 0건 |

**스캔 결과만 보고하고 수정하지 않으면 FAILURE이다.**

---

## 4. 완료 판정 체크리스트

작업 종료 시 AI는 이 체크리스트를 확인해야 한다:

```
[ ] 파일이 실제로 수정되었는가?
[ ] TASK_QUEUE.yaml이 올바르게 업데이트되었는가?
[ ] MODULE_REGISTRY.yaml이 올바르게 업데이트되었는가? (해당 시)
[ ] FEATURE_QUEUE.yaml이 올바르게 업데이트되었는가? (해당 시)
[ ] 모든 코드가 CODING_RULES.md를 준수하는가?
[ ] 린터 에러가 0건인가?
[ ] human_state: pending에 도달했는가? (모듈 생성 시)
[ ] YAML 파일 간 일관성이 유지되는가?
[ ] 중복 엔트리가 없는가?
```

하나라도 `[ ]`(미체크)이면 작업은 완료되지 않은 것이다.

---

## 5. 글로벌 규칙

- AGF v2.3 기존 파이프라인만 사용한다.
- 신규 아키텍처를 발명하지 않는다.
- 시스템을 재설계하지 않는다.
- Human Validator를 우회하지 않는다.
- 기존 모듈의 핵심 책임을 변경하지 않는다.
- `editable: false` 모듈을 수정하지 않는다.

---

## ENFORCEMENT

**All future AI tasks must follow this document.**
**Any response violating this document is invalid.**

파일이 수정되지 않은 응답은 완료된 것이 아니다.
분석만 출력한 응답은 실행이 아니다.
이 기준은 모든 AI 에이전트에 동일하게 적용된다.
