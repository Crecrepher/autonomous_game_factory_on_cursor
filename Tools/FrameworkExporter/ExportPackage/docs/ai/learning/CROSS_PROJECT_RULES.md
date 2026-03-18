# Cross-Project Rules — Autonomous Game Factory v2

프로젝트에 종속되지 않는 범용 규칙을 정리한다.
새 프로젝트 시작 시 이 파일을 그대로 복사하여 사전 학습 컨텍스트로 사용한다.

**삭제 금지. 축적 전용.**

---

## 추출 기준

RULE_MEMORY.yaml에서 `scope: global`인 규칙을 여기에도 자연어로 정리한다.
RULE_MEMORY는 기계 판독용(YAML), 이 문서는 사람과 에이전트 모두를 위한 가독성 높은 버전이다.

---

## Architecture Rules

### CPR-001: Runtime은 순수 C#

Runtime 클래스는 MonoBehaviour를 상속하지 않는다.
Unity 라이프사이클이 필요하면 Bootstrap에서 Tick()을 호출한다.
이유: 테스트 용이성, new 생성 가능, GC 최소화.

**출처**: RM-0001

### CPR-002: Config는 ScriptableObject

Config 클래스는 반드시 ScriptableObject를 상속하고, 로직을 포함하지 않는다.
데이터 전용 컨테이너로 사용한다.

**출처**: RM-0002

### CPR-003: Factory는 static class

Factory는 인스턴스가 필요 없다. static class로 선언하고 Create() 메서드를 통해 Runtime을 생성한다.

**출처**: CP-011

### CPR-004: 모듈당 6파일 필수

I<Module>.cs, <Module>Config.cs, <Module>Runtime.cs, <Module>Factory.cs, <Module>Bootstrap.cs, Tests/Editor/<Module>Tests.cs.
하나라도 빠지면 ModuleStructureValidator가 실패한다.

**출처**: CP-001, RM-0015

### CPR-005: MonoBehaviour 싱글턴 금지

static Instance 패턴은 숨겨진 의존성, 테스트 불가능성, 초기화 순서 문제를 야기한다.
DI 또는 명시적 SerializeField 참조를 사용한다.

**출처**: RM-0010

---

## Dependency Rules

### CPR-006: using은 선언된 의존성만

코드에서 `using Game.X`를 사용하려면 MODULE_REGISTRY.yaml의 dependencies에 X가 있어야 한다.
미선언 using은 DependencyValidator 에러.

**출처**: RM-0003

### CPR-007: 순환 의존 절대 금지

A→B→A 또는 A→B→C→A 같은 순환 참조는 허용하지 않는다.
공통 계약이 필요하면 Shared 인터페이스로 의존 역전한다.

**출처**: RM-0012

### CPR-008: 인터페이스 기반 모듈 간 통신

다른 모듈의 Runtime을 직접 참조하지 않는다.
Shared 인터페이스를 통해 계약 기반으로 통신한다.

**출처**: CP-014

---

## Performance Rules

### CPR-009: GC 유발 코드 금지

foreach, LINQ, 람다, 코루틴, Invoke 모두 GC를 유발한다.
for문, named method, 델타타임 타이머를 사용한다.

**출처**: RM-0004, RM-0008, RM-0014

### CPR-010: 런타임 GetComponent 금지

GetComponent, Find, FindObjectOfType는 런타임에서 사용하지 않는다.
SerializeField 또는 초기화 시 캐싱한다.

**출처**: RM-0005

### CPR-011: Update에서 무거운 연산 회피

magnitude → sqrMagnitude, 나눗셈 → 역수 곱셈.
Mathf의 삼각함수나 sqrt도 매 프레임 호출을 피한다.

**출처**: RM-0013, CP-006, CP-007

### CPR-012: Animator 해시 캐싱

문자열 기반 Animator 호출 대신 static readonly int + StringToHash.

**출처**: RM-0006, CP-008

---

## Coding Style Rules

### CPR-013: namespace 필수

모든 .cs 파일에 namespace 선언 필수. 이 프레임워크에서는 `namespace Game` 사용.

**출처**: RM-0011

### CPR-014: 필드 선언 순서

const → public static → private static → event → property → (기능별 분류) SerializeField → private.
private 키워드 생략, `_camelCase`.

**출처**: CP-002

### CPR-015: 매직넘버 금지

모든 숫자 리터럴은 const UPPER_SNAKE_CASE로 선언한다.

**출처**: CODING_RULES.md §1.1

---

## 규칙 추가 원칙

1. RULE_MEMORY.yaml에서 `scope: global`로 등록된 규칙만 여기에 추가한다.
2. ID 형식: `CPR-XXX` (순차 증가)
3. 반드시 출처(RULE_MEMORY id 또는 CODING_PATTERNS id)를 명시한다.
4. 프로젝트 특화 규칙은 이 문서에 포함하지 않는다.
