# AI용 코딩 규칙

> `.example`을 제거하고 프로젝트에 맞게 수정하세요.

Cursor가 코드를 생성·수정할 때 반드시 준수해야 할 규칙이다.

---

## 1. C# / Unity 기본

### 1.1 네이밍

| 대상 | 규칙 | 예시 |
|------|------|------|
| private 필드 | `_` + camelCase | `_moveSpeed` |
| public 노출 | 프로퍼티 또는 `=>` | `public int MaxHealth => _maxHealth;` |
| 상수 | UPPER_SNAKE_CASE | `const float MOVE_SPEED = 2f;` |
| Enum 타입 | `E` 접두사 | `enum ECombatState { Idle, Attack }` |

### 1.2 필드·프로퍼티

- public 필드 직접 노출 금지. 이벤트는 예외.
- Inspector 노출은 `[SerializeField]` 사용.
- const → public static → private static → 이벤트 → 프로퍼티 → 직렬화 필드 → private 필드 순.

---

## 2. MonoBehaviour

- 얇게 유지: 로직은 Runtime 클래스로 분리.
- 싱글턴 금지.
- 런타임 GetComponent 사용 금지.

---

## 3. 성능·GC

- `sqrMagnitude` 등 가벼운 연산 사용.
- 코루틴·무명 메서드·람다·LINQ·foreach 사용 지양.
- Invoke 사용 금지.

---

## 4. 구조·참조

- 자식 오브젝트만 참조.
- 리플렉션 사용 금지.
- Animator 파라미터는 해시 캐싱.

---

## 5. 통신·결합

- 이벤트 버스 사용 금지. 필요한 객체끼리만 통신.
- 인터페이스 적극 활용.

---

## 6. 문서 참조

- 모듈 경로·의존성: `MODULE_REGISTRY.yaml`
- 프로젝트 구조: `PROJECT_OVERVIEW.md`
