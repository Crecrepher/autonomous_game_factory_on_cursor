# 프로젝트 개요 — Autonomous Game Factory v2

## 1. 프로젝트 정의

이 프로젝트는 **Human-in-the-Loop Learning Pipeline**을 구현하는 Unity AI 개발 팩토리다.

핵심 사이클:

```
AI 코드 생성 → 사람이 Unity에서 검증 → 사람이 수정 → 수정 이유 기록 → AI 재검증 → 재커밋 → 학습 축적
```

이것은 단순한 코드 자동생성 도구가 아니다.
AI가 생성하고, 사람이 검증하고 고치며, 그 과정에서 축적된 학습이 다음 생성의 품질을 올리는 **피드백 루프 시스템**이다.

---

## 2. 시스템의 세 가지 축

### 2.1 AI Generation (생성)

- 사용자가 기능 설명 또는 기획서를 제공한다.
- AI가 이를 모듈 단위로 분해한다.
- 각 모듈은 표준 템플릿(Interface, Config, Runtime, Factory, Bootstrap, Tests)에 따라 생성된다.
- 생성된 코드는 CODING_RULES.md와 MODULE_REGISTRY.yaml의 규칙을 준수한다.

### 2.2 Human Validation (사람 검증)

- AI는 자체적으로 검증을 "완료"할 수 없다.
- **반드시 사람이 Unity Editor에서 Validator를 실행**해야 한다.
- 사람이 생성 코드를 직접 확인하고, 필요하면 수정한다.
- AI는 사람의 수정 사항을 받아 재검증(Reviewer)과 재커밋(Committer)을 수행한다.

### 2.3 Learning Accumulation (학습 축적)

- 사람이 수정한 모든 건에 대해 **수정 이유(rationale)**를 기록한다.
- Validator 실패 패턴을 `learning/VALIDATOR_FAILURE_PATTERNS.md`에 축적한다.
- 핵심 규칙을 `learning/RULE_MEMORY.yaml`에 기계 판독 가능 형식으로 저장한다.
- 시간순 학습 이벤트를 `learning/LEARNING_LOG.md`에 기록한다.
- 사람 수정 사례를 `learning/HUMAN_FIX_EXAMPLES.md`에 Before/After로 기록한다.
- 축적된 학습은 다음 생성 시 AI가 참조하는 컨텍스트가 된다.
- 학습 데이터는 프로젝트를 넘어 **다른 프로젝트에서도 재사용** 가능하다 (`scope: global`).

---

## 3. 대상 게임

- **장르**: 2D 아이소메트릭 탑뷰 타워 디펜스 Playable Ad
- **엔진**: Unity (C#)
- **네임스페이스**: `Game`
- **레퍼런스**: KingShot 등 캐주얼 아이소메트릭 디펜스 게임

게임 기획 상세(게임플레이 루프, 적 패턴, 경제, 수비탑, 업그레이드 흐름 등)는 별도 기획 문서에서 관리한다. 이 문서는 **개발 파이프라인 아키텍처**에 집중한다.

---

## 4. 파이프라인 전체 흐름

9개 역할이 정해진 순서로 바톤을 넘긴다:

```
사용자 요청 (기능 설명 / 기획서)
 ↓
[1] Feature Intake       → FEATURE_QUEUE.yaml
 ↓
[2] Queue Generator      → TASK_QUEUE.yaml + MODULE_REGISTRY.yaml + generated_specs/
 ↓
[3] Orchestrator         → 의존성 그래프 빌드, Builder Pool 배정
 ↓
[4] Planner              → plans/<Module>_PLAN.md
 ↓
[5] Builder              → Assets/Game/Modules/<Module>/ (6파일)
 ↓
[6] ★ Human Validator ★  → 사람이 Unity Validator 실행 + 코드 수정
 ↓
[7] Reviewer             → 코드 검증 (통과: commit ready / 실패: blocked → [5] 루프)
 ↓
[8] Committer            → feature_group 단위 git commit
 ↓
[9] Learning Recorder    → learning/ 폴더에 학습 데이터 축적
```

**[6] Human Validator**가 이 시스템의 핵심 관문이다.
human_state == validated 없이 done 처리되는 모듈은 존재하지 않는다.

---

## 5. 에이전트 역할 요약

| # | 역할 | 핵심 책임 | 코드 수정 권한 |
|---|------|-----------|---------------|
| 1 | **Feature Intake** | 사용자 요청 → FEATURE_QUEUE 엔트리 | 없음 |
| 2 | **Queue Generator** | 모듈 분해, TASK_QUEUE/REGISTRY 등록, Spec 생성 | 없음 |
| 3 | **Orchestrator** | 의존성 그래프, Builder Pool 배정 | 없음 |
| 4 | **Planner** | Spec 분석, PLAN 작성 | 없음 |
| 5 | **Builder** | PLAN에 따라 모듈 코드 생성 | 자기 모듈 폴더만 |
| 6 | **Human Validator** | Unity Validator 실행, 코드 수정 | 전체 (사람) |
| 7 | **Reviewer** | 품질 검증, 아키텍처 적합성 판정 | 없음 |
| 8 | **Committer** | 스테이징, 커밋, 재커밋 | 없음 (git만) |
| 9 | **Learning Recorder** | 수정 이유 기록, 패턴 축적 | 학습 문서만 |

상세: `AGENT_ROLES.md` 참조.

---

## 6. 아키텍처 원칙

### 6.1 설계 원칙

- **SOLID** — 단일 책임, 개방-폐쇄, 리스코프 치환, 인터페이스 분리, 의존성 역전
- **모듈 격리** — 모듈 간 참조는 인터페이스만. 직접 Runtime 참조 금지
- **의존성 주입** — 생성자/세터/ScriptableObject 등으로 주입. 하드코딩 지양
- **이벤트 기반 통신** — 필요한 객체끼리만 통신. 전역 이벤트 버스 금지

### 6.2 구조 원칙

- **MonoBehaviour는 얇게** — Bootstrap/View만. 로직은 Runtime(순수 C#)으로
- **GC 최소화** — 코루틴, 람다, LINQ, foreach, Invoke 전부 금지
- **런타임 GetComponent 금지** — SerializeField 또는 에디터 타임 캐싱
- **자식만 참조** — 동일/상위 오브젝트 직접 참조 금지
- **매직넘버 금지** — const UPPER_SNAKE_CASE 사용

### 6.3 모듈 구조

모든 모듈은 6개 필수 파일로 구성된다:

```
Assets/Game/Modules/<Module>/
├── I<Module>.cs          # 인터페이스 (공개 계약)
├── <Module>Config.cs     # ScriptableObject (설정 데이터)
├── <Module>Runtime.cs    # 순수 C# (비즈니스 로직)
├── <Module>Factory.cs    # static class (생성/DI)
├── <Module>Bootstrap.cs  # MonoBehaviour (씬 진입점, 얇게)
└── Tests/Editor/
    └── <Module>Tests.cs  # NUnit 테스트 (최소 2개)
```

상세: `MODULE_TEMPLATES.md`, `CODING_RULES.md` 참조.

---

## 7. 폴더 구조

```
Assets/
  Game/
    Modules/           # AI가 생성하는 모듈 코드
      Template/        # 참조용 템플릿 (수정 금지)
      Economy/         # 생성된 모듈 예시
      Player/
      ...
  Editor/
    AI/                # 검증 시스템, 파이프라인 도구 (수정 금지)
      Validators/      # 12+ 검증기
      ...

docs/
  ai/
    PROJECT_OVERVIEW.md      # 이 문서
    AGENT_ROLES.md           # 에이전트 역할 정의
    ORCHESTRATION_RULES.md   # 오케스트레이션 규칙
    CODING_RULES.md          # 코딩 규칙
    MODULE_REGISTRY.yaml     # 모듈 레지스트리
    MODULE_TEMPLATES.md      # 모듈 템플릿 가이드
    TASK_QUEUE.yaml           # 태스크 큐
    FEATURE_QUEUE.yaml        # 기능 큐
    FEATURE_INTAKE.md         # 기능 입력 형식
    COMMIT_RULES.md           # 커밋 규칙
    KNOWN_FAILURE_PATTERNS.md # (레거시 → learning/ 으로 이전)
    generated_specs/          # 모듈 명세서
    plans/                    # 구현 계획서
    reviews/                  # 검증 보고서
    commit_logs/              # 커밋 로그
    learning/                 # 학습 메모리 시스템
      LEARNING_INDEX.md       #   진입점 (에이전트 Quick-Start)
      RULE_MEMORY.yaml        #   규칙 저장소 (기계 판독용)
      LEARNING_LOG.md         #   시간순 이벤트 로그
      VALIDATOR_FAILURE_PATTERNS.md  # Validator별 실패 패턴 사전
      CODING_PATTERNS.md      #   BAD/GOOD 코드 패턴
      HUMAN_FIX_EXAMPLES.md   #   Before/After 사람 수정 사례
      CROSS_PROJECT_RULES.md  #   프로젝트 간 재사용 규칙
      RECURRING_MISTAKES.md   #   반복 AI 실수 패턴
```

---

## 8. Human-in-the-Loop: 왜 필수인가

### AI만으로 안 되는 이유

1. **Unity 컴파일은 Unity Editor에서만 가능하다** — AI는 실제 컴파일 결과를 알 수 없다.
2. **씬/프리팹 연결은 사람이 확인해야 한다** — SerializeField 바인딩, 프리팹 참조 등.
3. **게임 "느낌"은 사람만 판단할 수 있다** — 연출, 타이밍, 밸런스.
4. **AI는 같은 실수를 반복한다** — 학습 없이는 동일 패턴의 에러를 계속 만든다.

### 사람이 개입하는 지점

| 단계 | 사람의 역할 |
|------|------------|
| 기능 요청 | 기획 의도 전달 |
| Validator 실행 | Unity Editor에서 `Tools/AI/Validate Generated Modules` 실행 |
| 코드 수정 | 생성 코드의 컴파일 에러, 로직 오류, 스타일 수정 |
| 수정 이유 기록 | 왜 고쳤는지를 LEARNING_LOG에 기록 |
| 최종 승인 | Reviewer 결과 확인 후 커밋 승인 |

### 사람이 하지 않는 것

- TASK_QUEUE 상태 전이 (자동)
- 재검증 실행 (Reviewer가 수행)
- 재커밋 (Committer가 수행)
- 학습 데이터 구조화 (Learning Recorder가 수행)

---

## 9. 학습 시스템 개요

### 9.1 학습 데이터 소스

| 소스 | 데이터 |
|------|--------|
| Validator 실패 | 에러 유형, 파일, 검증기 이름 |
| 사람 수정 | 변경 전/후, 수정 이유(rationale) |
| Reviewer 보고서 | blocked 사유, 반복 위반 패턴 |
| 재커밋 이력 | 몇 번 만에 통과했는지, 어떤 수정이 필요했는지 |

### 9.2 학습 데이터 저장소

진입점: `learning/LEARNING_INDEX.md`

| 파일 | 용도 | 형식 |
|------|------|------|
| `learning/RULE_MEMORY.yaml` | 핵심 규칙 저장소 | YAML (기계 판독용) |
| `learning/LEARNING_LOG.md` | 시간순 학습 이벤트 로그 | Markdown + YAML |
| `learning/VALIDATOR_FAILURE_PATTERNS.md` | 12 Validator별 실패 패턴 사전 | 테이블 |
| `learning/CODING_PATTERNS.md` | BAD/GOOD 코드 패턴 | 코드 예시 |
| `learning/HUMAN_FIX_EXAMPLES.md` | Before/After 사람 수정 사례 | 코드 예시 |
| `learning/CROSS_PROJECT_RULES.md` | 프로젝트 간 재사용 규칙 (scope: global) | 자연어 |
| `learning/RECURRING_MISTAKES.md` | 3회 이상 반복 AI 실수 패턴 | 구조화 설명 |

### 9.3 학습 활용

- **Planner**: `RULE_MEMORY.yaml` + `RECURRING_MISTAKES.md`를 읽고 PLAN에 "회피할 규칙" 반영
- **Builder**: `CODING_PATTERNS.md` + `HUMAN_FIX_EXAMPLES.md` + `RULE_MEMORY.yaml`을 읽고 코드 생성
- **Reviewer**: `VALIDATOR_FAILURE_PATTERNS.md`를 참조하여 알려진 패턴 집중 검증
- **Learning Recorder**: 커밋 사이클 완료 시 `learning/` 전체 파일에 새 데이터 기록
- 새 프로젝트 시작 시 `learning/` 폴더를 복사하여 사전 학습 컨텍스트로 사용한다.

---

## 10. 수정 금지 영역

| 영역 | 이유 |
|------|------|
| `Assets/Editor/AI/` | 검증 시스템 — 공유 인프라 |
| `Assets/Game/Modules/Template/` | 참조용 템플릿 — 원본 보존 |
| `.cursor/rules/` | Cursor 규칙 — 사용자만 수정 |

---

## 11. 관련 문서

| 문서 | 내용 |
|------|------|
| `ORCHESTRATION_RULES.md` | **통합 오케스트레이션 명세** — 9역할 바톤 패스, 4차원 상태, I/O 계약 |
| `AGENT_ROLES.md` | 9개 에이전트 역할 상세, 허용/금지 액션, 권한 매트릭스 |
| `STATE_MACHINE.md` | 4차원 상태 전이 명세, 교차 조건, 복합 상태 매핑 |
| `QUEUE_GENERATOR.md` | Queue Generator 분해/의존/위험 규칙, 전체 예시 |
| `FEATURE_INTAKE.md` | Feature Intake 입력 형식, 자연어 변환 예시 |
| `COMMIT_RULES.md` | 5 Gate, Staging Policy, Recommit, 커밋 메시지 규격 |
| `CODING_RULES.md` | C#/Unity 코딩 규칙 |
| `MODULE_TEMPLATES.md` | 모듈 템플릿 6파일 구조 |
| `TASK_SCHEMA.md` | TASK_QUEUE.yaml 필드 정의, enum 값 |
| `generated_specs/README.md` | Spec 출력 규격 |
| `PROJECT_HANDOFF.md` | 핸드오프 문서 — 아키텍처 요약, 갭, 다음 작업 |
