# Codebase Ingestion & Modularization Workflow

> Version: 1.0
> Location: `Assets/Editor/GameFactory/`
> Entry: `Tools/AI/Codebase Ingestion Tool`

## 목적

기존 Unity 프로젝트의 레거시 코드를 스캔하여 AGF(Autonomous Game Factory) 모듈 형식으로의 **안전한 점진적 모듈화**를 제안한다. 코드를 자동으로 재작성하지 않는다.

## 워크플로우

```
코드 루트 선택
  ↓
CodebaseAnalyzer — 전체 .cs 파일 스캔
  ↓
분석 결과: 클래스/인터페이스/네임스페이스/의존성/아키텍처 드리프트
  ↓
ModuleCandidateInferrer — 모듈 후보 추론
  ↓
후보 결과: 모듈명/결정/리스크/유사 모듈/마이그레이션 전략
  ↓
IngestionReportGenerator — 리포트 + 큐/레지스트리 드래프트 생성
  ↓
Human-in-the-Loop 파이프라인으로 전달
```

## 코드 분석 대상 (10가지)

| # | 분석 항목 | 탐지 방법 |
|---|---|---|
| 1 | Classes | Regex: public/internal class + 상속 |
| 2 | Interfaces | Regex: interface I* |
| 3 | ScriptableObjects | : ScriptableObject 상속 탐지 |
| 4 | MonoBehaviours | : MonoBehaviour 상속 탐지 |
| 5 | Static Utility | static class 키워드 |
| 6 | Namespace Usage | using/namespace 문 파싱 |
| 7 | File/Folder Grouping | 폴더 구조 기반 자동 그룹핑 |
| 8 | Dependency References | using 문에서 외부 의존성 추출 |
| 9 | Runtime/Editor 분리 | UnityEditor using + #if UNITY_EDITOR 체크 |
| 10 | Feature Responsibility | 폴더명 기반 책임 추론 |

## 아키텍처 드리프트 탐지

| 드리프트 유형 | 설명 | 위험도 |
|---|---|---|
| LogicInMonoBehaviour | MonoBehaviour에 비즈니스 로직이 과도하게 포함 | Medium-High |
| MutableStateInSO | ScriptableObject에 변경 가능한 런타임 상태 | Medium |
| GlobalStaticState | static 클래스에 숨겨진 전역 상태 | Medium |
| EditorRuntimeMix | Editor/Runtime 코드 혼합 (#if 없이) | Low |
| GodClass | 500+ LOC 또는 20+ public 메서드 | High |
| GCHeavy | foreach/LINQ/코루틴/람다 사용 | Medium |

## 모듈 결정 모델

각 코드 클러스터에 대해 하나의 결정을 내린다:

| 결정 | 설명 | 조건 |
|---|---|---|
| ReuseAsIs | 변경 없이 등록 | 유사 AGF 모듈 존재 + 드리프트 적음 |
| WrapWithModuleInterface | AGF 인터페이스로 래핑 | 인터페이스 있음 + 드리프트 적음 |
| ExtractRuntimeModule | MonoBehaviour에서 Runtime 분리 | MonoBehaviour > Pure C# × 2 |
| SplitIntoMultipleModules | 여러 모듈로 분할 | 3000+ LOC + 8+ 파일 |
| AdaptExistingModule | 기존 AGF 모듈 확장/적응 | 유사 모듈 매칭 |
| ReplaceLater | 나중에 교체 | 저우선순위 |
| LeaveUnclassified | 분류 보류 | 수동 검토 필요 |

## 마이그레이션 안전 규칙

1. 원본 코드를 검증된 대체 없이 삭제하지 않음
2. 한 번에 하나의 모듈씩 점진적 마이그레이션
3. 각 마이그레이션 단계는 Validator 통과 필수
4. 레거시 코드 은퇴 전 Human Validation 필수
5. 직접 재작성보다 어댑터 패턴 우선

## 생성되는 리포트

| 파일 | 형식 | 내용 |
|---|---|---|
| CODEBASE_INVENTORY.md | Markdown | 전체 코드 인벤토리 + 드리프트 목록 |
| MODULE_CANDIDATES.md | Markdown | 모듈 후보 상세 + 결정/전략 |
| DEPENDENCY_GRAPH.md | Markdown | 네임스페이스/폴더 간 의존성 |
| MODULARIZATION_PLAN.md | Markdown | 3단계 모듈화 계획 (Low→Medium→High risk) |
| MIGRATION_PLAN.md | Markdown | 순서화된 마이그레이션 단계 |
| CODEBASE_INDEX.yaml | YAML | 구조화된 코드 유닛 인덱스 |
| MODULE_CANDIDATES.yaml | YAML | 구조화된 모듈 후보 데이터 |

## TASK_QUEUE / MODULE_REGISTRY 연동

- TASK_QUEUE: 각 후보마다 `pending` 상태 태스크 엔트리 추가
  - `integration_strategy`: 결정에 따라 reuse/adapt/create_new
  - `decomposition_source`: codebase_ingestion
  - `feature_group`: ingestion-migration
- MODULE_REGISTRY: 각 후보마다 draft 엔트리 추가
  - `editable: true`, 리스크에 따른 risk 레벨
  - 기존 엔트리 절대 수정하지 않음 (append only)

## EditorWindow UI

1. **Scan Roots** — 코드 루트 폴더 자동 탐지 + 수동 설정
2. **Actions** — Analyze Codebase / Infer Module Candidates
3. **Analysis Results** — 카테고리 분포, 폴더 그룹, 드리프트 목록
4. **Module Candidates** — 색상 코딩된 후보 목록 (결정별)
5. **Generate Artifacts** — 리포트 Dry Run/Apply + 큐/레지스트리 드래프트

## Control Panel 통합

`GameFactoryControlPanel`의 Actions 섹션에 "Codebase Ingestion Tool" 버튼 추가.

## 첫 번째 테스트 시나리오

1. `Tools/AI/Codebase Ingestion Tool` 메뉴 클릭
2. `Assets/Supercent` 체크 (auto-detected)
3. "Analyze Codebase" 클릭 → 분석 결과 확인
4. "Infer Module Candidates" 클릭 → 후보 목록 확인
5. "Generate Reports (Apply)" 클릭 → `docs/ai/ingestion/` 확인
6. 리포트 검토 후 필요 시 "Draft TASK_QUEUE Entries" 클릭
