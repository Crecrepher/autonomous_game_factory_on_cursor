> **EXAMPLE FILE** — 이 파일은 AI Game Dev Framework의 예시 문서입니다.
> 실제 프로젝트에 적용할 때 이 내용을 프로젝트에 맞게 수정하여 `Docs/ai/CODING_RULES.md`로 사용하세요.

# AI용 코딩 규칙

Cursor가 코드를 생성·수정할 때 반드시 준수해야 할 규칙이다. 기존 프로젝트 규칙과 충돌 시 이 문서를 우선한다.

---

## 1. C# / Unity 기본

### 1.1 네이밍

| 대상 | 규칙 | 예시 |
|------|------|------|
| private 필드 | `_` + camelCase (private 키워드 생략 가능) | `_moveSpeed`, `_animator` |
| public 노출 | 프로퍼티 또는 `=>` 표현식, 가능하면 get-only | `public int MaxHealth => _maxHealth;` |
| 상수 | UPPER_SNAKE_CASE, 매직넘버 금지 | `const float MOVE_SPEED_MULTIPLIER = 2f;` |
| Enum 타입 | `E` 접두사 | `enum ECombatState { Idle, Attack, Hurt }` |
| 코루틴 메서드 | `Co` 접두사 | `IEnumerator CoDelayedSpawn()` |

### 1.2 필드·프로퍼티

- public 필드 직접 노출 금지. 이벤트는 예외.
- Inspector 노출이 필요한 private 필드는 `[SerializeField]` 사용.
- const → public static → private static → public 이벤트 → public 프로퍼티 → public 필드 → 직렬화 필드 → private 필드 순으로 선언하고, 비-static 필드부터는 기능 단위로 주석 구분 후 동일 자료형끼리 묶는다.

---

## 2. MonoBehaviour

- **얇게 유지**: 로직은 서비스·런타임(비-MonoBehaviour) 클래스로 분리. MonoBehaviour는 바인딩·라이프사이클만 담당.
- **Awake**: 독립된 기능이 아니면 사용하지 않는다.
- **싱글턴 금지**: MonoBehaviour 상속 클래스에 싱글턴 패턴 사용 금지.
- **GetComponent 런타임 사용 금지**: 에디터 시점에 필드로 참조하거나, 어딘가에서 캐싱해 두고 참조.

---

## 3. 성능·GC

- **Update**: `magnitude`/`distance` 대신 `sqrMagnitude` 등 가벼운 연산 사용. Mathf의 무거운 연산 회피.
- **나눗셈**: Update 등 매 프레임 연산에서 나눗셈이 필요하면 역수를 미리 캐싱한 뒤 곱셈으로 대체.
- **GC 방지**: 코루틴·무명 메서드·람다·LINQ(Select, Where, ToList 등)·foreach 사용 지양. for 문과 메서드 캐싱 사용.
- **Invoke 사용 금지**.

---

## 4. 구조·참조

- **자식만 참조**: 동일 레벨·상위 오브젝트 직접 참조 금지. **자식 오브젝트만** 참조.
- **리플렉션 사용 금지**.
- **nullable `?` 사용 금지** (프로젝트 규칙에 따름).
- **Animator 파라미터**: `private readonly static int _hash = Animator.StringToHash("ParamName");` 형태로 해시 캐싱.
- **문자열 조합**: 변경이 잦으면 `StringBuilder _sb` 사용. 가짓수가 적으면 `private readonly static string[]` 등으로 미리 정의.

---

## 5. 데이터·컬렉션

- 길이 변경이 없으면 `List` 대신 배열 `[]` 사용.
- 리스트 길이에 변화가 없다면 배열을 우선한다.

---

## 6. 통신·결합

- **이벤트 버스 사용 금지**. 필요한 객체끼리만 통신.
- 범용적인 인터페이스가 적절하면 인터페이스를 사용한다.
- `System.Event`, `SerializeField`를 적극 활용한다.

---

## 7. 씬·프리팹

- 대규모 씬/프리팹 수정은 피한다. 가능하면 프리팹 단위·작은 변경으로 제한.
- 기존 시스템이 있으면 재사용을 우선한다.

---

## 8. 기타

- **MonoBehaviour 상속이 불필요한 경우**: 상속하지 않고, 직렬화 가능한 클래스로 두고 상위 객체가 필드로 보유. 초기화는 상위에서 제어.
- **IInitiable** 등 초기화 인터페이스를 활용할 경우, 상위에서 초기화 시점을 제어한다.
- **Rigidbody/물리**: 가능하면 더 가벼운 충돌·충돌 검사 방식 우선.
- **쉐이더**로 해결 가능한 연출·성능 이슈는 쉐이더로 처리하는 방안을 검토한다.

---

## 9. 아이소메트릭 3D 아케이드 Idle 특화

- **설정 데이터**: 아이템 타입·생산 규칙·보스 패턴·업그레이드 정의 등은 ScriptableObject로 분리. 매직넘버·하드코딩 금지.
- **풀링**: 적·투사체·이펙트 등 단기 생성/삭제 객체는 풀링 사용. 런타임 Instantiate/Destroy 최소화.
- **아이소메트릭**: 깊이 정렬·그리드 스냅은 가능하면 한 곳(예: Core 또는 전용 모듈)에서 관리. 정렬 기준(Y·Z) 상수화.
- **Idle/주기 로직**: 생산 주기·스폰 주기 등은 델타타임 누적 또는 타이머 서비스 사용. Update에서 매 프레임 검사 시 가벼운 연산만.
- **재사용**: 기계·손님·적·아이템 등 동일 패턴은 공통 인터페이스·공통 컴포넌트로 재사용. 모듈 간 계약은 Shared 인터페이스로.

---

## 10. 문서 참조

- 모듈 경로·의존성·편집 가능 여부: `MODULE_REGISTRY.yaml`
- 프로젝트 목적·아키텍처·폴더 구조: `PROJECT_OVERVIEW.md`
