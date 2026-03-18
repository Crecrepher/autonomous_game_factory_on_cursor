# Coding Patterns — Autonomous Game Factory v2

이 프로젝트에서 검증된 코딩 패턴을 정리한다.
AI 에이전트는 코드 생성 시 이 문서의 패턴을 기본 템플릿으로 사용한다.

**삭제 금지. 패턴 추가만 허용.**

---

## 사용법

| 역할 | 접근 |
|------|------|
| Builder | 코드 생성 시 해당 카테고리의 패턴 적용 |
| Planner | PLAN에 적용할 패턴 명시 |
| Reviewer | 패턴 준수 여부 확인 |
| Learning Recorder | 새로운 검증된 패턴 발견 시 append |

---

## 1. Module 기본 구조

### CP-001: 표준 모듈 6파일 구조

```
Assets/Game/Modules/<ModuleName>/
├── I<ModuleName>.cs           # 공개 계약 (인터페이스)
├── <ModuleName>Config.cs      # 설정 데이터 (ScriptableObject)
├── <ModuleName>Runtime.cs     # 비즈니스 로직 (순수 C#)
├── <ModuleName>Factory.cs     # 생성 로직 (static class)
├── <ModuleName>Bootstrap.cs   # Unity 진입점 (MonoBehaviour, 얇게)
└── Tests/Editor/
    └── <ModuleName>Tests.cs   # NUnit 테스트 (최소 2개)
```

**적용 범위**: global (모든 Unity 모듈 프로젝트)

---

## 2. 필드 선언 순서

### CP-002: 표준 필드 순서

```csharp
namespace Game
{
    public class ExampleRuntime
    {
        // ── const ──
        const float MOVE_SPEED = 5f;
        const int MAX_ITEMS = 100;

        // ── public static ──
        public static int TotalCount;

        // ── private static ──
        static readonly int ANIM_SPEED = Animator.StringToHash("Speed");

        // ── public event ──
        public event System.Action<int> OnDamaged;

        // ── public property ──
        public int CurrentHp => _currentHp;
        public bool IsAlive => _currentHp > 0;

        // ── Combat ──
        [SerializeField] int _maxHp;
        int _currentHp;
        float _damageMultiplier;

        // ── Movement ──
        [SerializeField] float _moveSpeedBase;
        float _currentSpeed;
        Vector3 _targetPosition;
    }
}
```

**규칙**:
- const → public static → private static → public event → public property → (기능별 분류 시작) SerializeField → private 필드
- static이 아닌 필드부터는 기능 단위 주석으로 분류 후, 같은 자료형끼리 묶음
- private 키워드 생략, `_camelCase`

**적용 범위**: global

---

## 3. GC-Free 패턴

### CP-003: for문 기반 컬렉션 순회

```csharp
// BAD: GC 유발
foreach (var enemy in enemies) { enemy.TakeDamage(10); }

// GOOD: GC-free
for (int i = 0; i < enemies.Length; i++)
{
    enemies[i].TakeDamage(10);
}
```

**적용 범위**: global

### CP-004: 이벤트 구독 메서드 캐싱

```csharp
// BAD: 람다 → 클로저 객체 할당
_button.onClick.AddListener(() => OnClick());

// GOOD: named method
_button.onClick.AddListener(OnButtonClick);

void OnButtonClick()
{
    // 로직
}
```

**적용 범위**: global

### CP-005: 타이머 패턴 (코루틴 대체)

```csharp
// BAD: 코루틴
IEnumerator CoSpawn()
{
    yield return new WaitForSeconds(2f);
    Spawn();
}

// GOOD: 델타타임 누적 타이머
const float SPAWN_INTERVAL = 2f;
float _spawnTimer;

void Tick(float deltaTime)
{
    _spawnTimer += deltaTime;
    if (_spawnTimer < SPAWN_INTERVAL) return;
    _spawnTimer -= SPAWN_INTERVAL;
    Spawn();
}
```

**적용 범위**: global

---

## 4. 성능 패턴

### CP-006: Update 나눗셈 역수 캐싱

```csharp
// BAD: 매 프레임 나눗셈
void Update()
{
    float normalized = currentValue / maxValue;
}

// GOOD: 역수 미리 캐싱
float _inverseMaxValue;

void Init(float maxValue)
{
    _inverseMaxValue = 1f / maxValue;
}

void Tick(float deltaTime)
{
    float normalized = _currentValue * _inverseMaxValue;
}
```

**적용 범위**: global

### CP-007: sqrMagnitude 기반 거리 비교

```csharp
// BAD: sqrt 포함
if (Vector3.Distance(a, b) < threshold) { }

// GOOD: sqrMagnitude
float sqrThreshold = threshold * threshold;
if ((a - b).sqrMagnitude < sqrThreshold) { }
```

**적용 범위**: global

### CP-008: Animator 해시 캐싱

```csharp
// BAD: 매번 문자열 해시 변환
_animator.SetFloat("MoveSpeed", speed);

// GOOD: static readonly 해시
static readonly int MOVE_SPEED_HASH = Animator.StringToHash("MoveSpeed");
_animator.SetFloat(MOVE_SPEED_HASH, speed);
```

**적용 범위**: global

---

## 5. 아키텍처 패턴

### CP-009: Bootstrap 얇게 유지

```csharp
// BAD: Bootstrap에 로직 가득
public class CombatBootstrap : MonoBehaviour
{
    void Update()
    {
        CalculateDamage();
        ApplyBuffs();
        CheckDeath();
    }
}

// GOOD: Bootstrap은 바인딩과 호출만
public class CombatBootstrap : MonoBehaviour
{
    [SerializeField] CombatConfig _config;
    CombatRuntime _runtime;

    void Start()
    {
        _runtime = CombatFactory.Create(_config);
    }

    void Update()
    {
        _runtime.Tick(Time.deltaTime);
    }
}
```

**적용 범위**: global

### CP-010: Runtime 순수 C# (MonoBehaviour 금지)

```csharp
// BAD
public class InventoryRuntime : MonoBehaviour { }

// GOOD
public class InventoryRuntime
{
    InventoryConfig _config;

    public void Init(InventoryConfig config)
    {
        _config = config;
    }

    public void Tick(float deltaTime) { }
}
```

**적용 범위**: global

### CP-011: Factory static class

```csharp
// BAD: Factory가 MonoBehaviour
public class CombatFactory : MonoBehaviour { }

// GOOD: static class
namespace Game
{
    public static class CombatFactory
    {
        public static CombatRuntime Create(CombatConfig config)
        {
            var runtime = new CombatRuntime();
            runtime.Init(config);
            return runtime;
        }
    }
}
```

**적용 범위**: global

### CP-012: Config는 ScriptableObject + 로직 없음

```csharp
namespace Game
{
    [CreateAssetMenu(menuName = "Game/CombatConfig")]
    public class CombatConfig : ScriptableObject
    {
        [SerializeField] int _maxHp = 100;
        [SerializeField] float _attackInterval = 1.5f;

        public int MaxHp => _maxHp;
        public float AttackInterval => _attackInterval;
    }
}
```

**적용 범위**: global

---

## 6. 참조/통신 패턴

### CP-013: 자식만 참조 규칙

```csharp
// BAD: 같은 레벨이나 상위 참조
[SerializeField] Transform _parentController; // 상위 오브젝트

// GOOD: 자식 오브젝트만 참조
[SerializeField] Transform _weaponSlot;     // 자식 오브젝트
[SerializeField] Renderer _bodyRenderer;    // 자식 오브젝트
```

**적용 범위**: project

### CP-014: 이벤트 기반 모듈 간 통신

```csharp
// BAD: 직접 참조
_inventoryRuntime.AddItem(item); // 다른 모듈 직접 호출

// GOOD: 인터페이스 또는 이벤트
public interface IItemReceiver
{
    void ReceiveItem(int itemId, int amount);
}

// 호출 측은 인터페이스만 알면 된다
_itemReceiver.ReceiveItem(itemId, amount);
```

**적용 범위**: global

---

## 7. 문자열 패턴

### CP-015: StringBuilder 또는 캐싱된 string[]

```csharp
// BAD: 매 프레임 문자열 결합
void Update()
{
    _label.text = "HP: " + _hp + "/" + _maxHp;
}

// GOOD (가짓수 적을 때): 미리 캐싱
static readonly string[] HP_LABELS = { "HP: 0", "HP: 1", "HP: 2" /* ... */ };

// GOOD (가짓수 많을 때): StringBuilder
readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(32);

void UpdateLabel()
{
    _sb.Clear();
    _sb.Append("HP: ");
    _sb.Append(_hp);
    _sb.Append('/');
    _sb.Append(_maxHp);
    _label.text = _sb.ToString();
}
```

**적용 범위**: global

---

## 패턴 추가 규칙

1. 새 패턴은 적절한 카테고리 섹션에 추가한다.
2. 패턴 ID 형식: `CP-XXX` (순차 증가)
3. 반드시 BAD/GOOD 코드 예시를 포함한다.
4. 적용 범위를 `global` 또는 `project`로 명시한다.
5. 동일 패턴이 이미 존재하면 기존 항목에 사례를 추가한다.
