# Learning Log — Autonomous Game Factory v2

이 문서는 Human-in-the-Loop 파이프라인에서 발생한 모든 학습 이벤트를 시간순으로 기록한다.

**삭제 금지. 축적 전용.**

---

## 사용 규칙

| 역할 | 접근 | 설명 |
|------|------|------|
| Learning Recorder | Write (append only) | 커밋 사이클 완료 후 새 엔트리 추가 |
| Planner | Read | PLAN 작성 시 과거 실패 패턴 회피 |
| Builder | Read | 코드 생성 시 반복 실수 참조 |
| Reviewer | Read | 검증 시 과거 사례와 대조 |
| Human | Read/Write | 수정 이유(rationale) 직접 입력 |

---

## 엔트리 스키마

```yaml
- id: LL-XXXX
  date: "YYYY-MM-DD"
  module: <ModuleName>
  feature_group: <group>
  event_type: validator_failure | human_fix | recommit | recurring_mistake
  source: <검증기 이름 | "human" | "builder">
  description: <무엇이 문제였는가>
  root_cause: <왜 이 문제가 발생했는가>
  fix_summary: <어떻게 수정했는가>
  rationale: <왜 이 수정이 올바른가 — 사람이 입력>
  files_affected:
    - <파일 경로>
  recurring: true | false
  cross_project: true | false
  related_rules:
    - <RULE_MEMORY id, 예: RM-0001>
  tags:
    - <검색용 태그>
```

### 필드 설명

| 필드 | 필수 | 설명 |
|------|------|------|
| `id` | O | 순차 증가 (LL-0001, LL-0002, ...) |
| `date` | O | 기록 날짜 |
| `module` | O | 대상 모듈 이름 |
| `feature_group` | O | 소속 feature group |
| `event_type` | O | 이벤트 유형 |
| `source` | O | 이벤트 발생원 (validator 이름 또는 human) |
| `description` | O | 문제 설명 |
| `root_cause` | O | 근본 원인 분석 |
| `fix_summary` | O | 수정 내용 요약 |
| `rationale` | △ | 사람 수정 시 필수. validator_failure 시 선택. |
| `files_affected` | O | 영향 받은 파일 목록 |
| `recurring` | O | 3회 이상 동일 유형 발생 시 true |
| `cross_project` | O | 다른 프로젝트에도 적용 가능한 교훈이면 true |
| `related_rules` | △ | RULE_MEMORY에 대응하는 규칙이 있으면 기록 |
| `tags` | O | 검색 키워드 |

---

## 로그 엔트리

### LL-0001

```yaml
- id: LL-0001
  date: "2026-03-18"
  module: ModuleStructureValidator
  feature_group: infra-validation
  event_type: validator_failure
  source: ModuleStructureValidator
  description: >
    MODULE_REGISTRY.yaml의 경로 패턴과 실제 모듈 폴더 구조가 불일치하여
    ModuleStructureValidator가 정규식 매칭에 실패했다.
    YAML 파싱 시 경로 끝의 슬래시 유무로 regex가 달라졌다.
  root_cause: >
    YAML에서 path 값을 읽을 때 trailing slash를 정규화하지 않아
    "Assets/Game/Modules/Combat"과 "Assets/Game/Modules/Combat/"를
    다른 경로로 인식했다.
  fix_summary: >
    ModuleStructureValidator의 경로 비교 로직에 TrimEnd('/') 추가.
    MODULE_REGISTRY.yaml의 path 항목도 trailing slash 없이 통일.
  rationale: "경로 정규화는 기본 중의 기본. OS별 separator 차이도 고려해야 한다."
  files_affected:
    - Assets/Editor/AI/Validators/ModuleStructureValidator.cs
    - docs/ai/MODULE_REGISTRY.yaml
  recurring: false
  cross_project: true
  related_rules: []
  tags: [yaml, path, regex, validator, normalization]
```

### LL-0002

```yaml
- id: LL-0002
  date: "2026-03-18"
  module: DependencyValidator
  feature_group: infra-validation
  event_type: validator_failure
  source: DependencyValidator
  description: >
    Registry와 TASK_QUEUE 간 경로/이름 불일치로 인해
    DependencyValidator가 "Task has no matching entry in MODULE_REGISTRY" 에러를 발생시켰다.
  root_cause: >
    TASK_QUEUE.yaml에 등록된 모듈 이름과 MODULE_REGISTRY.yaml의 name 필드가
    대소문자나 공백에서 미세하게 달랐다 (예: "combatSystem" vs "CombatSystem").
  fix_summary: >
    TASK_QUEUE.yaml과 MODULE_REGISTRY.yaml의 이름을 PascalCase로 통일.
    TaskQueueGenerator에 PascalCase 강제 변환 로직 추가.
  rationale: "이름 불일치는 자동화 파이프라인의 가장 흔한 실패 원인이다."
  files_affected:
    - docs/ai/TASK_QUEUE.yaml
    - docs/ai/MODULE_REGISTRY.yaml
    - Assets/Editor/AI/TaskQueueGenerator.cs
  recurring: false
  cross_project: true
  related_rules: [RM-0003]
  tags: [naming, registry, task-queue, pascal-case]
```

### LL-0003

```yaml
- id: LL-0003
  date: "2026-03-18"
  module: TestDetection
  feature_group: infra-validation
  event_type: human_fix
  source: human
  description: >
    ModuleStructureValidator가 Tests 폴더 내 테스트 파일을 찾지 못하여
    "No *Tests.cs in Tests folder" 에러를 발생시켰다.
    실제로는 Tests/Editor/ 하위에 파일이 존재했다.
  root_cause: >
    테스트 파일 검색이 Tests/ 루트만 검사하고 하위 디렉토리(Tests/Editor/)를
    재귀적으로 검색하지 않았다.
  fix_summary: >
    ModuleStructureValidator의 테스트 파일 검색을
    Directory.GetFiles(testsPath, "*Tests.cs", SearchOption.AllDirectories)로 변경.
  rationale: >
    Unity의 EditMode 테스트는 관례적으로 Tests/Editor/ 하위에 둔다.
    재귀 검색이 표준이어야 하며, PlayMode 테스트가 추가될 경우에도 대응 가능하다.
  files_affected:
    - Assets/Editor/AI/Validators/ModuleStructureValidator.cs
  recurring: false
  cross_project: true
  related_rules: [RM-0015]
  tags: [testing, recursive-search, directory, validator]
```

---

## 다음 엔트리 ID: LL-0004
