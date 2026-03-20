# AI Game Dev Framework (AGF) — 사용자 가이드

> **버전:** v2.3 + CPIL  
> **대상:** Unity Playable Ad 프로젝트 (장르 무관 — 2D / 3D / 러너 / 퍼즐 / 머지 / RPG 등)  
> **핵심 철학:** AI가 코드를 생성하고, 사람이 검증하고, 시스템이 학습한다.

---

## 목차

1. [프레임워크 개요](#1-프레임워크-개요)
2. [핵심 개념](#2-핵심-개념)
3. [프로젝트 구조](#3-프로젝트-구조)
4. [Prompt OS — 자연어 명령 시스템](#4-prompt-os--자연어-명령-시스템)
5. [9단계 파이프라인 상세](#5-9단계-파이프라인-상세)
6. [주요 기능별 사용법](#6-주요-기능별-사용법)
7. [모듈 구조 (6파일 표준)](#7-모듈-구조-6파일-표준)
8. [4차원 상태 관리](#8-4차원-상태-관리)
9. [검증 시스템 (19개 Validator)](#9-검증-시스템-19개-validator)
10. [커밋 안전 시스템 (7 Gate)](#10-커밋-안전-시스템-7-gate)
11. [학습 시스템](#11-학습-시스템)
12. [Cross-Project Intelligence (CPIL)](#12-cross-project-intelligence-cpil)
13. [코딩 규칙 요약](#13-코딩-규칙-요약)
14. [자주 묻는 질문](#14-자주-묻는-질문)

---

## 1. 프레임워크 개요

AGF는 **Human-in-the-Loop AI 코드 생성 파이프라인**이다.

```
사용자의 자연어 요청
  → AI가 모듈 코드를 자동 생성
    → Unity Validator가 자동 검증
      → 사람이 최종 확인 + 수정
        → AI가 수정 내용을 학습
          → Git 커밋
```

### 세 개의 기둥

| 기둥 | 설명 |
|------|------|
| **AI 생성** | 기획서나 자연어 요청에서 모듈 코드를 자동 생성 |
| **Human 검증** | 사람이 Unity Editor에서 반드시 검증해야 다음 단계로 진행 |
| **학습 축적** | 사람의 수정 사항을 기록해서 같은 실수를 반복하지 않음 |

### 지원 환경

- **Cursor IDE** — `.cursor/rules/` 안의 규칙 파일이 AI의 행동을 제어
- **Unity Editor** — `Tools/AI/` 메뉴의 Validator, Control Panel, Bootstrap 도구

---

## 2. 핵심 개념

### 모듈 (Module)

기능 단위의 코드 묶음. 하나의 모듈은 6개 파일로 구성되며, `Assets/Game/Modules/<ModuleName>/` 아래에 배치된다.

### 파이프라인 (Pipeline)

요청 → 분해 → 생성 → 검증 → 커밋의 9단계 자동화 흐름.

### Gate

다음 단계로 넘어가기 위해 반드시 통과해야 하는 안전 관문.  
Human Gate, Validation Report Gate, Architecture Diff Gate 등이 있다.

### TASK_QUEUE

모든 모듈 작업의 상태를 추적하는 YAML 파일. 4차원 상태로 관리된다.

### MODULE_REGISTRY

프로젝트의 모든 모듈을 등록하는 카탈로그. 의존성, 편집 가능 여부, 위험도 등을 기록한다.

### FEATURE_QUEUE

사용자 요청(피처)의 생명주기를 추적하는 YAML 파일.

---

## 3. 프로젝트 구조

```
프로젝트 루트/
├── Assets/
│   ├── Game/
│   │   ├── Modules/           ← 모든 모듈 코드가 여기에 생성됨
│   │   │   ├── Template/      ← 6파일 표준 템플릿
│   │   │   ├── Combat/        ← 예: 전투 모듈
│   │   │   ├── Economy/       ← 예: 경제 모듈
│   │   │   └── ...
│   │   ├── Shared/            ← 크로스-모듈 인터페이스/계약
│   │   ├── Core/              ← 핵심 시스템 (editable: false — 수정 금지)
│   │   ├── Scenes/            ← BootstrapScene, GameplayScene
│   │   └── Prefabs/           ← 프리팹 에셋
│   └── Editor/
│       └── AI/                ← Unity Validator, Control Panel 등
│
└── docs/ai/                   ← AI 프레임워크 문서 + 메타데이터
    ├── MODULE_REGISTRY.yaml   ← 모듈 카탈로그
    ├── TASK_QUEUE.yaml        ← 작업 상태 추적
    ├── FEATURE_QUEUE.yaml     ← 피처 생명주기
    ├── CODING_RULES.md        ← 코딩 규칙
    ├── MODULE_TEMPLATES.md    ← 6파일 표준 명세
    ├── generated_specs/       ← 자동 생성된 모듈 스펙
    ├── plans/                 ← 자동 생성된 구현 계획
    ├── learning/              ← 학습 데이터 축적
    │   ├── RULE_MEMORY.yaml   ← 학습된 규칙
    │   ├── LEARNING_LOG.md    ← 학습 로그
    │   └── RECURRING_MISTAKES.md ← 반복 실수 패턴
    ├── ingestion/             ← 코드 흡수 리포트
    └── global_learning/       ← 크로스 프로젝트 학습
```

---

## 4. Prompt OS — 자연어 명령 시스템

Cursor에서 자연어로 명령하면 **Prompt OS**가 인텐트를 자동으로 분류하고 적절한 파이프라인을 실행한다.

### 지원하는 명령어

| 이렇게 말하면... | 인텐트 | 실행되는 것 |
|---|---|---|
| **"기획서 첨부했어, 셋팅해줘"** | `bootstrap_from_design` | 기획서 분석 → 프로젝트 구조 + 모듈 큐 자동 생성 |
| **"이 폴더 읽어줘" / "코드 분석해줘"** | `ingest_codebase` | 기존 코드 스캔 → 모듈화 후보 리포트 생성 |
| **"이 기능 만들어줘"** | `integrate_feature` | 9단계 풀 파이프라인 실행 |
| **"기존 모듈 활용해서"** | `reuse_first_integration` | 기존 모듈 우선 탐색 후 통합 |
| **"모듈 상태 확인해줘"** | `analyze_existing_modules` | 분석 전용 (코드 변경 없음) |
| **"큐만 생성해줘"** | `generate_queue_only` | TASK_QUEUE 생성까지만 |
| **"검증해줘"** | `run_validation_only` | 19개 Validator 실행 (코드 변경 없음) |
| **"커밋해줘"** | `commit_changes` | 7 Gate 체크 후 안전 커밋 |
| **"무엇을 배웠어?"** | `review_learning` | 학습 데이터 리뷰 |

### 사용 예시

```
사용자: "체력 시스템 만들어줘"
→ Prompt OS가 integrate_feature 인텐트로 분류
→ 9단계 파이프라인 자동 시작
→ Health 모듈 (6파일) 생성
→ Human Validation 대기
```

```
사용자: "기획서 첨부했어, 원클릭 셋팅해줘"
→ Prompt OS가 bootstrap_from_design 인텐트로 분류
→ 기획서 분석 → 모듈 분해 → Dry Run(미리보기) → Apply(생성)
→ 모든 태스크 pending 상태로 시작
```

---

## 5. 9단계 파이프라인 상세

"이 기능 만들어줘"를 말하면 아래 9단계가 순차적으로 실행된다.

```
[1] Feature Intake ──→ 요청을 FEATURE_QUEUE에 등록
        ↓
[2] Queue Generator ──→ 모듈 분해, TASK_QUEUE + REGISTRY + SPEC 생성
   [2.5] Module Discovery ──→ 기존 모듈에서 재사용 가능한 것 탐색
   [2.7] Reuse Decision ──→ reuse / extend / adapt / replace / create_new 결정
        ↓
[2.9] Architecture Diff ──→ 아키텍처 위험 분석 (critical이면 차단!)
        ↓
[3] Orchestrator ──→ 의존성 그래프 빌드, 실행 순서 결정
        ↓
[4] Planner ──→ 각 모듈의 구현 계획(PLAN) 작성
        ↓
[5] Builder ──→ 모듈 코드 생성 (6파일)
   [5.5] Self-Validation ──→ 빌더 자체 검증 (10개 체크)
        ↓
[6] ★ Human Validator ★ ──→ 파이프라인 정지! 사람이 검증해야 함
        ↓
[7] Reviewer ──→ AI 재검증 + Integration Strategy 검증
        ↓
[8] Committer ──→ 7 Gate 통과 시 Git 커밋
   [8.5] Meta Commit ──→ YAML 메타데이터 별도 커밋
        ↓
[9] Learning Recorder ──→ 수정 사항 학습 기록
```

### 각 단계 요약

| # | 단계 | 핵심 행동 | 자동/수동 |
|---|------|----------|----------|
| 1 | Feature Intake | 요청 분석, FEATURE_QUEUE 등록 | 자동 |
| 2 | Queue Generator | 모듈 분해, 의존성 추론, 큐 생성 | 자동 |
| 2.9 | Arch Diff | 아키텍처 위반 사전 분석 | 자동 |
| 3 | Orchestrator | 실행 순서 결정 | 자동 |
| 4 | Planner | 구현 계획 작성 | 자동 |
| 5 | Builder | 코드 생성 (6파일) | 자동 |
| **6** | **Human Validator** | **Unity에서 검증 + 수정** | **수동 (필수!)** |
| 7 | Reviewer | AI 재검증 | 자동 |
| 8 | Committer | Git 커밋 | 자동 (요청 시) |
| 9 | Learning | 학습 기록 | 자동 |

---

## 6. 주요 기능별 사용법

### 6-1. 새 프로젝트 부트스트랩 (Bootstrap)

**언제:** 기획서가 있고, 프로젝트를 처음 세팅할 때

**방법:**
1. Cursor에서 기획서를 첨부하거나 붙여넣기
2. `"셋팅해줘"` 또는 `"원클릭 셋팅"` 이라고 말하기

**결과:**
- 프로젝트 폴더 구조 자동 생성
- 씬 구조 (BootstrapScene, GameplayScene) 생성
- 각 모듈의 SPEC, PLAN 문서 생성
- TASK_QUEUE, MODULE_REGISTRY, FEATURE_QUEUE 초기화
- 모든 태스크는 `pending` 상태로 시작 (done이 아님!)

**주의:** 기존 파일은 절대 덮어쓰지 않음. 충돌 시 skip + 보고.

---

### 6-2. 기존 코드 흡수 (Ingest)

**언제:** 이미 작성된 레거시 코드가 있고, AGF 모듈 구조로 전환하고 싶을 때

**방법:**
1. `"이 폴더 읽어줘"` 또는 `"코드 분석해줘"` 라고 말하기
2. 특정 폴더를 지정하려면 `@Assets/Scripts` 처럼 `@`로 참조

**결과:**
- 모든 `.cs` 파일을 분류 (MonoBehaviour / ScriptableObject / 순수 C# 등)
- 아키텍처 드리프트 감지 (God Class, GC 패턴, 로직-in-MonoBehaviour 등)
- 모듈화 후보 + 전략 제안 (ReuseAsIs / WrapWithInterface / ExtractRuntime / Split 등)
- `docs/ai/ingestion/` 아래에 7개 리포트 생성

**주의:** 원본 코드를 절대 수정/삭제하지 않음. 분석 리포트만 생성.

---

### 6-3. 기능 추가 (Full Pipeline)

**언제:** 새로운 게임 기능을 모듈로 만들고 싶을 때

**방법:**
1. `"체력 시스템 만들어줘"`, `"경제 모듈 추가해줘"` 등으로 말하기
2. AI가 9단계 파이프라인을 자동 실행
3. **[6] Human Validator** 단계에서 파이프라인이 멈춤
4. Unity Editor → `Tools/AI/Validate Generated Modules` 실행
5. 에러 있으면 코드 수정
6. 검증 통과 후 `"커밋해줘"` 로 커밋

**흐름:**
```
"체력 시스템 만들어줘"
  → [1] FEATURE_QUEUE에 등록
  → [2] Health 모듈로 분해, TASK_QUEUE 생성
  → [2.9] 아키텍처 위험 분석 (통과)
  → [3-4] 실행 순서 + PLAN 작성
  → [5] 6개 파일 생성:
       IHealth.cs, HealthConfig.cs, HealthRuntime.cs,
       HealthFactory.cs, HealthBootstrap.cs, HealthTests.cs
  → [6] ★ 정지 — 사람이 Unity에서 검증해야 함 ★
  → (사람 검증 완료)
  → [7] Reviewer 재검증
  → [8] 커밋 (요청 시)
  → [9] 학습 기록
```

---

### 6-4. 기존 모듈 재사용 (Reuse-First)

**언제:** 새 기능이 필요하지만, 기존 모듈을 최대한 활용하고 싶을 때

**방법:**
1. `"기존 모듈 활용해서 만들어줘"` 라고 말하기
2. AI가 MODULE_REGISTRY를 탐색하고 재사용 가능한 모듈을 먼저 찾음

**전략 우선순위:**
```
reuse (그대로 사용) > extend (확장) > adapt (적응) > replace (교체) > create_new (신규)
```

---

### 6-5. 검증만 실행

**방법:** `"검증해줘"` 또는 `"체크해줘"`

**결과:** 19개 Validator가 실행되고 결과를 `AIValidationReport.json`에 기록. 코드 변경 없음.

---

### 6-6. 커밋

**방법:** `"커밋해줘"`

**결과:** 7개 Gate를 사전 체크한 후, 모두 통과해야만 커밋 실행.
게이트 실패 시 어떤 게이트가 왜 실패했는지 상세 보고.

---

## 7. 모듈 구조 (6파일 표준)

모든 모듈은 아래 6개 파일로 구성된다.

```
Assets/Game/Modules/<ModuleName>/
├── I<Module>.cs           ← 인터페이스 (외부 계약)
├── <Module>Config.cs      ← ScriptableObject 설정 (로직 없음, 데이터만)
├── <Module>Runtime.cs     ← 순수 C# 비즈니스 로직 (MonoBehaviour 금지!)
├── <Module>Factory.cs     ← static class 팩토리 (생성 로직)
├── <Module>Bootstrap.cs   ← MonoBehaviour 진입점 (얇게!)
└── Tests/Editor/
    └── <Module>Tests.cs   ← NUnit 테스트 (최소 2개)
```

### 각 파일의 역할

| 파일 | 상속 | 역할 | 규칙 |
|------|------|------|------|
| **Interface** | — | 외부 모듈이 참조하는 계약 | 다른 모듈은 이것만 참조 |
| **Config** | ScriptableObject | 밸런스 데이터, 설정값 | 로직 절대 금지 |
| **Runtime** | 순수 C# | 핵심 비즈니스 로직 | MonoBehaviour 상속 금지 |
| **Factory** | static class | Runtime 인스턴스 생성 | Config → Runtime 변환 |
| **Bootstrap** | MonoBehaviour | Unity 생명주기 연결 | 최대한 얇게, 위임만 |
| **Tests** | NUnit | 자동 테스트 | 최소 2개 이상 |

### 왜 이렇게 나누나?

- **테스트 가능성**: Runtime이 순수 C#이라 Unity 없이도 테스트 가능
- **교체 가능성**: 인터페이스 기반이라 구현체를 바꿔 끼울 수 있음
- **설정 분리**: Config가 ScriptableObject라 에디터에서 밸런스 조정 가능
- **GC 최소화**: MonoBehaviour 사용을 Bootstrap 하나로 제한

---

## 8. 4차원 상태 관리

TASK_QUEUE의 각 모듈은 4개의 독립적인 상태 차원으로 관리된다.

### 상태 차원

| 차원 | 값 | 설명 |
|------|-----|------|
| **status** | `pending` → `planned` → `in_progress` → `review` → `done` | 작업 진행도 |
| **human_state** | `none` → `pending` → `in_review` → `validated` | 사람 검증 상태 |
| **learning_state** | `none` → `recorded` / `recorded_existing_rule_reference` | 학습 기록 상태 |
| **commit_state** | `none` → `ready` → `committed` | 커밋 준비 상태 |

### 핵심 제약 (Cross-Dimension Guards)

```
in_progress → review : human_state == validated 필수
review → done       : commit_state ∈ {committed} + learning 완료 필수
commit_state: ready : Reviewer가 설정 (Human Validator 아님)
human_state: validated : 오직 사람만 설정 가능 (AI 금지!)
```

---

## 9. 검증 시스템 (19개 Validator)

Unity Editor의 `Tools/AI/Validate Generated Modules`로 실행한다.

주요 Validator 목록:

| # | Validator | 검사 내용 |
|---|-----------|----------|
| 1 | ModuleStructure | 6파일 존재 여부 |
| 2 | Namespace | 네임스페이스가 `Game`인지 |
| 3 | RuntimeInheritance | Runtime이 MonoBehaviour 상속 안 하는지 |
| 4 | ConfigInheritance | Config가 ScriptableObject 상속하는지 |
| 5 | FactoryStatic | Factory가 static class인지 |
| 6 | MagicNumber | 매직넘버 사용 여부 |
| 7 | GCPattern | foreach, 코루틴, 람다, LINQ 사용 여부 |
| 8 | TestExistence | 테스트 최소 1개 존재 |
| 9 | DependencyRegistry | 의존성이 REGISTRY에 선언되었는지 |
| 10 | CircularDependency | 순환 의존 여부 |
| 11 | ConfigConflict | Config Source-of-Truth 충돌 |
| 12 | ModuleReuseIntegrity | 재사용 전략 정합성 |
| 13 | PipelineTruth | 파이프라인 상태 진실성 |
| ... | ... | ... |

결과는 `AIValidationReport.json`에 저장되며, **blocking error가 0이어야** 커밋이 가능하다.

---

## 10. 커밋 안전 시스템 (7 Gate)

커밋 전에 7개의 게이트를 순서대로 체크한다. **하나라도 실패하면 커밋 불가.**

| # | Gate | 조건 |
|---|------|------|
| 1 | **Validation Report** | `AIValidationReport.json`의 `Passed == true` |
| 2 | **Reviewer** | `commit_state == ready` |
| 3 | **Human** | `human_state == validated` |
| 4 | **Learning** | `human_fixes > 0`이면 `learning_state == recorded` |
| 5 | **Completeness** | feature_group 내 모든 모듈 ready |
| 6 | **Scope** | 관련 파일만 스테이징됨 |
| 7 | **Architecture Diff** | `arch_diff_blocked != true` |

### 커밋 순서

```
1. feat(<group>): add <modules>     ← 기능 코드
2. chore(ai-meta): finalize <group> ← YAML 메타데이터 (1개만!)
3. chore(ai-learning): record       ← 학습 데이터 (있으면)
```

---

## 11. 학습 시스템

사람이 코드를 수정하면, 그 수정 내용을 자동으로 분류하고 기록한다.

### 학습 흐름

```
사람이 코드 수정 (human_fixes[])
  → Learning Recorder가 각 fix를 분류
    → 기존 규칙 강화 (existing_rule_reinforced)
    → 새 규칙 생성 (new_rule)
    → 반복 실패 패턴 등록 (failure_pattern)
  → learning/ 폴더에 기록
    → 다음 파이프라인 실행 시 참조
```

### 학습 데이터 파일

| 파일 | 역할 |
|------|------|
| `RULE_MEMORY.yaml` | 학습된 규칙 (ID + reinforcement_count) |
| `LEARNING_LOG.md` | 시간순 학습 이벤트 로그 |
| `RECURRING_MISTAKES.md` | 3회 이상 반복된 실수 패턴 |
| `CODING_PATTERNS.md` | 발견된 코딩 패턴 |
| `HUMAN_FIX_EXAMPLES.md` | 사람 수정의 구체적 예시 |

### 학습의 효과

Queue Generator는 매 파이프라인 시작 시 `RULE_MEMORY.yaml`과 `RECURRING_MISTAKES.md`를 **반드시** 읽는다.
이전에 학습된 실수를 반복하지 않도록 코드를 생성한다.

---

## 12. Cross-Project Intelligence (CPIL)

프로젝트 간에 지식을 공유하는 메타 레이어.

### 기능

| 기능 | 설명 |
|------|------|
| **Global Module Library** | 검증된 모듈을 프로젝트 간 공유 |
| **Cross-Project Learning** | 프로젝트 A의 학습이 프로젝트 B에 전파 |
| **Pattern Recognition** | 여러 프로젝트에서 발견된 공통 패턴 |
| **Bootstrap Generator** | 새 프로젝트 부트스트랩 시 기존 학습 활용 |

### 데이터 위치

```
docs/ai/global_learning/
├── GLOBAL_LEARNING_LOG.md
├── GLOBAL_FAILURE_PATTERNS.md
├── GLOBAL_CODING_PATTERNS.md
└── GLOBAL_RULE_MEMORY.yaml
```

### 안전 규칙

- 기존 프로젝트를 자동 수정하지 않음
- 제안/내보내기만 가능
- Human Validation 필수

---

## 13. 코딩 규칙 요약

| 규칙 | 상세 |
|------|------|
| **네임스페이스** | `Game` |
| **GC 금지** | 코루틴, 람다, LINQ, foreach, Invoke 전부 금지 |
| **반복문** | `for`문만 사용 |
| **매직넘버 금지** | `const UPPER_SNAKE_CASE` 필수 |
| **private 필드** | `_camelCase` (private 키워드 생략) |
| **public 노출** | 프로퍼티 또는 `=>` |
| **null 조건부** | `?` 사용 금지 |
| **GetComponent** | 런타임 사용 금지, 에디터 타임에 캐싱 |
| **Runtime** | MonoBehaviour 상속 금지, 순수 C# |
| **Config** | ScriptableObject, 로직 없음 |
| **Factory** | static class |
| **Bootstrap** | MonoBehaviour, 최대한 얇게 |
| **필드 순서** | const → static → 이벤트 → 프로퍼티 → public → 직렬화 → private |
| **의존성** | 인터페이스(`I<Module>`)로만 참조 |

---

## 14. 자주 묻는 질문

### Q: 파이프라인을 꼭 전부 실행해야 하나요?

아니요. 예외 처리가 있습니다:
- `"플랜만 보여줘"` → [4]까지만 실행
- `"커밋하지 마"` → [8]~[9] 건너뜀
- `"기존 모듈 수정"` → [1]~[2] 건너뛰고 [5]부터
- 단일 파일 수정, 버그 수정 → 파이프라인 적용 안 함

### Q: Human Validator를 건너뛸 수 있나요?

**절대 불가.** Human Gate는 우회할 수 없는 필수 관문입니다.
`human_state == validated` 없이는 review 단계에 진입할 수 없습니다.

### Q: 기존 코드를 자동으로 삭제하거나 대규모 리팩토링하나요?

아니요. 기존 코드는 절대 자동 삭제하지 않으며, 대량 재작성도 금지입니다.
코드 흡수(Ingest) 시에도 원본은 건드리지 않고 분석 리포트만 생성합니다.

### Q: Core 모듈을 수정할 수 있나요?

기본적으로 `editable: false`라서 수정 불가입니다.
수정하려면 사용자가 명시적으로 허가해야 합니다 (예: "Core 수정해도 돼").

### Q: 커밋이 자꾸 차단되는데요?

7 Gate 중 어떤 것이 실패하는지 확인하세요:
1. Validator 실행했나요? → `AIValidationReport.json`의 `Passed == true` 확인
2. Human Validation 했나요? → `human_state == validated` 확인
3. Learning 기록했나요? → 수정 사항이 있으면 `learning_state == recorded` 필요
4. feature_group 전체가 ready인가요?

### Q: Architecture Diff에서 critical로 차단되면?

critical 차단 사유를 확인하고 아키텍처를 수정하거나, 명시적으로 승인한 뒤 재분석해야 합니다.
주요 차단 사유: 인터페이스 메서드 삭제, 순환 의존, Runtime의 MonoBehaviour 상속, GC 패턴 등.

---

## 빠른 시작 체크리스트

```
□ 기획서 준비
□ Cursor에서 "기획서 첨부했어, 셋팅해줘" 입력
□ AI가 프로젝트 구조 + 큐 생성 (자동)
□ "이 기능 만들어줘" 로 모듈 생성 시작
□ AI가 코드 생성 후 Human Validator에서 멈춤
□ Unity Editor → Tools/AI/Validate Generated Modules 실행
□ 에러 있으면 수정
□ "커밋해줘" 로 안전 커밋
□ 학습 자동 기록 → 다음 생성에 반영
```

---

> **이 프레임워크의 핵심:** AI는 생성하고, 사람은 검증하고, 시스템은 학습한다.  
> 모든 안전 장치는 "AI가 실수해도 프로덕션에 영향을 주지 않도록" 설계되었다.
