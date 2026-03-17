# 오케스트레이션 규칙

병렬 Cursor 에이전트가 안전하게 모듈을 생성·검증하기 위한 조율 규칙을 정의한다.

---

## 1. 태스크 라이프사이클

모든 모듈 태스크는 아래 상태를 순서대로 거친다. 역방향 전이는 `blocked → review`(재검증)만 허용한다.

```
pending ──► planned ──► in_progress ──► review ──► done
                                          │
                                          ▼
                                       blocked
                                          │
                                          ▼
                                       review (재시도)
```

| 상태 | 설명 | 전이 주체 |
|------|------|-----------|
| `pending` | 아직 아무도 건드리지 않음 | — |
| `planned` | Planner가 PLAN 작성 완료 | Planner |
| `in_progress` | Builder가 코드 작성 중 | Builder |
| `review` | 코드 작성 완료, 검증 대기 | Builder → Reviewer |
| `done` | 검증 통과, 완료 | Reviewer |
| `blocked` | 의존 미충족 또는 검증 실패 | Reviewer 또는 시스템 |

### 전이 규칙

- `pending → planned`: Planner만 가능. PLAN 문서 산출 필수.
- `planned → in_progress`: Builder만 가능. owner 필드에 에이전트 ID 기록.
- `in_progress → review`: Builder만 가능. 자체 점검 완료 후.
- `review → done`: Reviewer만 가능. 검증 통과 시.
- `review → blocked`: Reviewer만 가능. 검증 실패 시. 실패 사유 기록.
- `blocked → review`: Builder가 수정 완료 후 재검증 요청 시.
- **그 외 전이는 금지**한다 (예: `in_progress → pending`, `done → review` 등).

---

## 2. 태스크 할당(Claiming) 규칙

### 2.1 기본 원칙

- **한 태스크에 한 Builder**: 동시에 같은 모듈에 두 명 이상의 Builder가 할당될 수 없다.
- **선착순**: 먼저 `TASK_QUEUE.yaml`에 자기 ID를 owner로 기록한 에이전트가 해당 태스크를 소유한다.
- **owner 확인 필수**: 작업 시작 전 반드시 `TASK_QUEUE.yaml`을 읽어 owner가 null인지 확인한다.

### 2.2 할당 절차

```
1. TASK_QUEUE.yaml 읽기
2. 원하는 태스크의 owner == null 확인
3. 의존 모듈이 모두 done인지 확인
4. owner에 자기 에이전트 ID 기록
5. role에 현재 역할(planner/builder/reviewer) 기록
6. status 전이
7. 작업 시작
```

### 2.3 해제

- 태스크 완료(`done`) 시 owner와 role을 null로 초기화한다.
- `blocked` 상태에서도 owner는 유지한다 (동일 Builder가 수정 담당).
- Builder가 작업을 포기해야 하는 경우, owner를 null로 되돌리고 status를 이전 상태로 복원한다.

---

## 3. 의존성 규칙

### 3.1 모듈 의존성

- `TASK_QUEUE.yaml`의 `depends_on` 필드에 나열된 모듈이 **모두 `done`** 상태여야 해당 태스크를 `in_progress`로 전이할 수 있다.
- 의존 모듈이 하나라도 `done`이 아니면 자동으로 `blocked` 상태를 유지한다.

### 3.2 의존성 그래프 (현재 TASK_QUEUE 기준)

```
[독립 — 즉시 시작 가능]
  Economy ─────────────────────────────────┐
  Player                                   │
  Enemies                                  │
  StatusEffect (done)                      │
  Guide                                    │
  DynamicConfig                            │
                                           ▼
[Economy 완료 후]                      Warriors ──┐
  DefenseTowers ◄── Economy               │       │
  Fortress ◄── Economy                    │       │
  Pickups ◄── Economy                     │       │
  UI ◄── Economy                          │       │
                                           ▼       ▼
[Economy + Warriors 완료 후]          HireNodes
  Blacksmith ◄── Economy, Warriors    ◄── Economy, Warriors

[StatusEffect 완료 후]
  BuffIconUI ◄── StatusEffect (즉시 시작 가능)

[Fortress 완료 후]
  EndCard ◄── Fortress

[최종]
  GameManager ◄── Player, Enemies, Economy, Warriors, DefenseTowers, Fortress
```

### 3.3 최대 병렬도

의존성 그래프에서 독립 태스크를 기준으로 한 번에 최대 병렬 작업 가능 수:

| 단계 | 병렬 가능 태스크 | 최대 동시 Builder |
|------|------------------|-------------------|
| 1단계 | Economy, Player, Enemies, Guide, DynamicConfig, BuffIconUI | 6 |
| 2단계 | Warriors, DefenseTowers, Fortress, Pickups, UI | 5 |
| 3단계 | HireNodes, Blacksmith, EndCard | 3 |
| 4단계 | GameManager | 1 |

---

## 4. 병렬 실행 규칙

### 4.1 모듈 격리 원칙

- **각 Builder는 자기 모듈 폴더만 수정한다**: `Assets/Game/Modules/<자기 모듈>/`
- 서로 다른 모듈을 작업하는 Builder들은 파일 충돌 없이 동시에 작업할 수 있다.
- `Assets/Game/Shared/`에 공용 인터페이스를 추가할 때는 기존 파일을 수정하지 않고, 새 파일만 추가한다.

### 4.2 동시 수정 금지 파일

아래 파일/폴더는 동시에 여러 에이전트가 수정하면 충돌이 발생할 수 있다. **순차적으로만 수정**하거나, 사전 조율 후 수정한다:

| 파일/폴더 | 수정 가능 역할 | 조건 |
|-----------|----------------|------|
| `TASK_QUEUE.yaml` | 모든 역할 | 자기 태스크 항목만 수정 |
| `MODULE_REGISTRY.yaml` | Planner (추가만) | 새 모듈 등록 시에만 |

### 4.3 수정 금지 영역

아래 파일/폴더는 **어떤 에이전트도 수정할 수 없다** (명시적 사용자 허용이 없는 한):

| 파일/폴더 | 이유 |
|-----------|------|
| `Assets/Editor/AI/` | 검증 시스템 — 공유 인프라 |
| `Assets/Game/Core/` | 핵심 시스템 — editable: false |
| `Assets/Game/Modules/Template/` | 참조용 템플릿 — 원본 보존 |
| `.cursor/rules/` | Cursor 규칙 — 사용자만 수정 |

---

## 5. 충돌 방지 프로토콜

### 5.1 TASK_QUEUE.yaml 수정 시

```
1. 파일 읽기
2. 자기 태스크 항목만 수정
3. 다른 태스크 항목은 절대 건드리지 않음
4. 저장
```

### 5.2 MODULE_REGISTRY.yaml 수정 시

- Planner만 새 모듈 항목을 **추가**할 수 있다.
- 기존 항목을 수정하거나 삭제하지 않는다.
- 한 번에 하나의 Planner만 수정한다.

### 5.3 Git 충돌 방지

- 각 에이전트는 자기 모듈 폴더에서만 작업하므로 코드 파일의 Git 충돌은 발생하지 않는다.
- `TASK_QUEUE.yaml`은 각 에이전트가 자기 항목만 수정하므로 충돌 가능성이 낮다.
- 충돌이 발생하면 가장 최근 `done` 상태를 우선한다.

---

## 6. 검증 흐름

### 6.1 Builder 자체 점검

Builder는 review로 넘기기 전에 다음을 확인한다:

1. 모든 필수 파일이 존재하는가 (I, Runtime, Config, Factory, Bootstrap, Tests)
2. 네임스페이스가 `Game`인가
3. Runtime이 MonoBehaviour를 상속하지 않는가
4. Config가 ScriptableObject를 상속하는가
5. Factory가 static class인가
6. 매직넘버가 없는가
7. GC 유발 코드가 없는가
8. 테스트가 최소 1개 있는가

### 6.2 Reviewer 검증

Reviewer는 다음 순서로 검증한다:

```
1. 변경된 파일 목록 확인
2. Unity 검증 도구 실행 (Tools/AI/Validate Generated Modules)
3. AIValidationReport.json 읽기
4. 에러/경고 분석
5. 결과 판정:
   ├── 에러 0개 → done
   └── 에러 1개 이상 → blocked + 수정 사항 목록 작성
```

### 6.3 검증 항목 매핑

| 검증기 | 검사 내용 | 실패 시 조치 |
|--------|-----------|-------------|
| CompileErrorValidator | 컴파일 에러 | Builder가 문법 수정 |
| ValidatorRegistrationValidator | 검증기 등록 정합성 | (보통 Builder 영역 아님) |
| ForbiddenFolderValidator | Core 등 금지 영역 수정 | Builder가 해당 변경 제거 |
| ModuleStructureValidator | 필수 파일 누락 | Builder가 누락 파일 생성 |
| ModuleBoundaryValidator | 모듈 간 불법 참조 | Builder가 인터페이스로 변경 |
| ArchitectureRuleValidator | 아키텍처 패턴 위반 | Builder가 패턴 준수하도록 수정 |

---

## 7. 에러 복구 절차

### 7.1 검증 실패 시

```
Reviewer: review → blocked (실패 사유 기록)
    ↓
Builder: 실패 사유 확인 → 코드 수정
    ↓
Builder: blocked → review (재검증 요청)
    ↓
Reviewer: 재검증 실행
    ↓
├── 통과 → done
└── 실패 → blocked (반복, 최대 3회)
    ↓
3회 실패 → 사용자에게 에스컬레이션
```

### 7.2 의존 모듈 blocked 시

- 의존하는 하위 태스크는 자동으로 진행 불가 (의존 체크에서 걸림)
- 상위 모듈이 `done`이 되면 하위 태스크가 자연스럽게 진행 가능해짐

### 7.3 Builder 교체

- Builder가 작업을 계속할 수 없는 경우 (세션 종료 등):
  1. owner를 null로 초기화
  2. status를 `planned`로 되돌림
  3. 다른 Builder가 할당받아 이어서 진행

---

## 8. 요약: 안전한 병렬 개발의 3가지 보장

| 보장 | 메커니즘 |
|------|----------|
| **태스크 소유권** | TASK_QUEUE.yaml의 owner 필드 — 한 태스크에 한 에이전트 |
| **모듈 격리** | 각 Builder는 자기 모듈 폴더만 수정 — 파일 충돌 불가 |
| **아키텍처 검증** | 6개 Validator가 구조/규칙/경계/컴파일 에러 검증 — 품질 보장 |
