# Architecture Decisions

주요 아키텍처 의사결정 기록. ADR(Architecture Decision Record) 형식.

---

## ADR-001: 모듈 6파일 구조

- **상태**: 채택
- **날짜**: 2026-03-18
- **맥락**: AI가 생성하는 모듈 코드의 일관성과 검증 가능성을 보장해야 함
- **결정**: 모든 모듈은 I<M>, <M>Config, <M>Runtime, <M>Factory, <M>Bootstrap, Tests/<M>Tests 6파일로 구성
- **근거**: Validator가 구조를 자동 검증 가능. 템플릿 기반 생성으로 일관성 보장.
- **결과**: ModuleStructureValidator가 6파일 준수를 강제

## ADR-002: Runtime 순수 C# 강제

- **상태**: 채택
- **날짜**: 2026-03-18
- **맥락**: 비즈니스 로직의 테스트 용이성과 Unity 의존 최소화
- **결정**: Runtime 클래스는 MonoBehaviour 상속 금지. 순수 C#만 허용.
- **근거**: 단위 테스트 가능, 재사용성 향상, GC 위험 감소
- **결과**: ArchitectureRuleValidator가 강제. 위반 시 critical 차단.

## ADR-003: 인터페이스 우선 통신

- **상태**: 채택
- **날짜**: 2026-03-18
- **맥락**: 모듈 간 결합도를 최소화하고 교체 가능성을 확보해야 함
- **결정**: 모듈 간 참조는 I<Module> 인터페이스를 통해서만 허용
- **근거**: SOLID 원칙 중 DIP/ISP 준수. 모듈 교체 시 영향 최소화.
- **결과**: ModuleBoundaryValidator가 크로스 모듈 Runtime 직접 참조를 차단

## ADR-004: GC 제로 정책

- **상태**: 채택
- **날짜**: 2026-03-18
- **맥락**: 모바일 게임 타겟. 프레임당 GC 할당이 히칭을 유발
- **결정**: foreach, LINQ, 코루틴, 람다, Invoke 전면 금지. for문만 사용.
- **근거**: Unity 모바일 최적화 가이드라인 준수
- **결과**: CodingStyleValidator + PerformanceValidator가 강제

## ADR-005: Architecture Diff Analyzer 도입

- **상태**: 채택
- **날짜**: 2026-03-18
- **맥락**: AI가 제안한 변경이 기존 아키텍처를 파괴할 위험 존재
- **결정**: Queue Generator 직후, Planner 직전에 아키텍처 diff 분석 단계 삽입
- **근거**: 순환 의존, 인터페이스 호환성 파괴, MonoBehaviour 침투를 사전 감지
- **결과**: critical 위험 시 파이프라인 즉시 차단. Committer에 Gate 7 추가.

## ADR-006: Pipeline Self-Healing

- **상태**: 채택
- **날짜**: 2026-03-18
- **맥락**: 메타데이터 불일치 등 단순 오류로 파이프라인이 불필요하게 중단됨
- **결정**: 런타임 코드를 건드리지 않는 범위에서 메타데이터 자동 복구 허용
- **근거**: TASK_QUEUE/MODULE_REGISTRY 불일치는 자동 수정 가능한 단순 오류
- **결과**: PipelineSelfHealer가 메타데이터만 수정. 코드 수정은 절대 금지.

---

*새 ADR 추가 시 번호를 순차적으로 부여한다.*
