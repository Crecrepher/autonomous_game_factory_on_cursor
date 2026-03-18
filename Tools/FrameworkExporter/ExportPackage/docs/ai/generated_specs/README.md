# Generated Specs — Autonomous Game Factory v2

이 폴더에는 Queue Generator → SpecGenerator가 자동 생성한 모듈 명세서가 저장된다.

---

## 파일 규칙

| 항목 | 값 |
|------|-----|
| 파일명 | `<ModuleName>_SPEC.md` |
| 생성 주체 | Queue Generator (SpecGenerator.cs) |
| 소비 주체 | Planner → Builder |
| 수정 가능 | Planner가 PLAN 작성 시 보완 가능 |

---

## Spec 구조

각 Spec 파일은 다음 섹션을 포함한다:

```
# Module Spec: <ModuleName>

Feature Group: <group>

## Purpose
모듈의 존재 이유 한 줄.

## Public API
- I<ModuleName> interface
- Init() — initialization
- Tick(float deltaTime) — per-frame update
- (도메인별 메서드/프로퍼티)

## Runtime Responsibilities
- 핵심 비즈니스 로직
- 상태 관리
- 이벤트 발행

## Config Needs
- <ModuleName>Config : ScriptableObject
- 도메인별 설정 필드

## Factory Responsibility
static <ModuleName>Factory.CreateRuntime(<ModuleName>Config) → I<ModuleName>

## Test Scope
- Factory creates non-null runtime
- Init → Tick does not throw
- 도메인별 동작 테스트

## Dependency Constraints
- [의존 모듈 목록]

## Must NOT Implement
- MonoBehaviour logic (only in Bootstrap)
- Direct references to other modules' Runtime classes
- GC-allocating patterns (coroutines, lambdas, LINQ, foreach)
- GetComponent at runtime
- Magic numbers (use const)
```

---

## 사용법

### Planner가 읽을 때

1. Spec에서 모듈 목적, 의존성, 제약 확인
2. PLAN에 구체적 구현 단계 추가
3. Spec에 없는 도메인 세부사항을 PLAN에 보완

### Builder가 읽을 때

1. Public API에서 인터페이스 메서드 확인
2. Runtime Responsibilities에서 구현 범위 파악
3. Must NOT Implement에서 금지 사항 확인
4. Test Scope에서 테스트 작성 가이드 확인

---

## 파일 생명주기

```
Queue Generator → 자동 생성 (intake → decomposed → queued)
 ↓
Planner → 보완 (PLAN 작성 시 세부사항 추가)
 ↓
Builder → 참조 (코드 생성 시)
 ↓
Reviewer → 참조 (수락 기준과 대조)
 ↓
Committer → 스테이징 (feature 커밋에 포함)
```
