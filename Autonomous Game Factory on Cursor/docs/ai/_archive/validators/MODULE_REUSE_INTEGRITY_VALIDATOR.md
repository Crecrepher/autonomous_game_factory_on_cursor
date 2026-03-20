# ModuleReuseIntegrityValidator

> Version: 1.0  
> Category: **Blocking** (replace-without-analysis), **Warning/Error** (기타)  
> Registered in: `ValidationRunner.cs`  
> Source: `Assets/Editor/AI/Validators/ModuleReuseIntegrityValidator.cs`

## 목적

재사용/확장/적응/대체 결정이 **실제로 유효하고 안전한지** 검증한다.  
가짜 재사용 결정, 불필요한 신규 생성, 분석 없는 대체를 차단한다.

## 검증 대상

### 1. Replace 전략 검증 (Blocking)

| 조건 | 판정 |
|---|---|
| integration_strategy=replace, impact_analysis≠completed | **Error** |
| integration_strategy=replace, migration_required=false | **Error** |
| integration_strategy=replace, migration_plan이 없거나 none | **Error** |

### 2. Create_new 정당성 검증 (Warning)

| 조건 | 판정 |
|---|---|
| integration_strategy=create_new, existing_module_candidates가 비어있지 않음 | **Warning** (기존 후보가 있는데 신규 생성) |

### 3. Extend/Adapt 후보 존재 검증 (Error)

| 조건 | 판정 |
|---|---|
| integration_strategy=extend/adapt, 후보 모듈 디렉토리가 존재하지 않음 | **Error** (없는 모듈에 대한 확장 주장) |

### 4. 메타데이터 갭 검증 (Warning)

| 조건 | 판정 |
|---|---|
| strategy≠create_new인데 existing_module_candidates가 비어있음 | **Warning** (참조 대상 누락) |

### 5. 책임 중복 탐지 (Warning)

| 조건 | 판정 |
|---|---|
| 의존관계 없는 두 모듈의 인터페이스 메서드 50%+ 겹침 | **Warning** (책임 중복 가능성) |

## 검증 입력

```
TASK_QUEUE.yaml:
  - integration_strategy
  - existing_module_candidates
  - impact_analysis
  - migration_required
  - migration_plan
  - status

MODULE_REGISTRY.yaml:
  - dependencies (DependencyGraphBuilder로 파싱)

모듈 인터페이스 파일:
  - Assets/Game/Modules/<Module>/I<Module>.cs
```

## Severity 분류

| 상황 | Severity |
|---|---|
| replace + 분석/마이그레이션 누락 | **Error** (Blocking) |
| create_new + 기존 후보 존재 | **Warning** |
| extend/adapt + 후보 미존재 | **Error** (Blocking) |
| 메타데이터 갭 | **Warning** |
| 책임 중복 50%+ | **Warning** |

## 실패 케이스 예시

### Case 1: 분석 없는 Replace
```yaml
- name: NewInventory
  integration_strategy: replace
  impact_analysis: none
  migration_required: false
→ Error: "Replace strategy requires completed impact analysis"
→ Error: "Replace strategy always requires migration"
```

### Case 2: 후보 무시한 신규 생성
```yaml
- name: CurrencySystem
  integration_strategy: create_new
  existing_module_candidates: [Economy]
→ Warning: "existing candidates were found: [Economy]"
```

### Case 3: 존재하지 않는 모듈 확장 주장
```yaml
- name: SuperInventory
  integration_strategy: extend
  existing_module_candidates: [NonExistentModule]
→ Error: "module directory does not exist"
```

### Case 4: 인터페이스 책임 중복
```
IModuleA: Push(), Pop(), Clear(), GetCount()
IModuleB: Push(), Pop(), Reset(), GetSize()
→ Warning: "50% interface method overlap"
```

## 통합 위치

- `ValidationRunner.cs`에서 `PipelineTruthValidator` 이후 실행
- 결과는 `AIValidationReport.json`에 기록

## 재테스트 시나리오

1. TASK_QUEUE에서 integration_strategy=replace, impact_analysis=none인 엔트리 추가 → Error 확인
2. create_new + existing_module_candidates=[Economy] 설정 → Warning 확인
3. 두 모듈의 인터페이스에 동일 메서드명 50%+ 설정 → Warning 확인
