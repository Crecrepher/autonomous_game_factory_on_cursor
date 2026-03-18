# Global Coding Patterns — Cross-Project Intelligence

프로젝트 간 공유되는 검증된 코딩 패턴.

---

## 1. 필드 선언 순서

```csharp
// 1. const (UPPER_SNAKE_CASE)
const int MAX_SLOTS = 20;

// 2. public static
// 3. private static
static readonly int ANIM_HASH_IDLE = Animator.StringToHash("Idle");

// 4. public event
public event System.Action<int> OnValueChanged;

// 5. public property (get-only)
public int CurrentValue => _currentValue;

// 6. [SerializeField] private
[SerializeField] int _maxValue;

// 7. private
int _currentValue;
```

## 2. for문 패턴 (GC-free)

```csharp
// 올바름
for (int i = 0; i < _items.Length; i++)
{
    if (_items[i].Id == targetId)
        return _items[i];
}

// 위반 — foreach, LINQ
foreach (var item in _items) { ... }
_items.Where(x => x.Id == targetId).First();
```

## 3. Animator 해시 캐싱

```csharp
static readonly int ANIM_ATTACK = Animator.StringToHash("Attack");
static readonly int ANIM_IDLE = Animator.StringToHash("Idle");
```

## 4. Update 최적화

```csharp
// 역수 캐싱으로 나눗셈 제거
float _inverseDuration;

void Init(float duration)
{
    _inverseDuration = 1f / duration;
}

void Tick(float deltaTime)
{
    _progress += deltaTime * _inverseDuration;
}
```

## 5. 모듈 팩토리 패턴

```csharp
public static class EconomyFactory
{
    public static EconomyRuntime Create(EconomyConfig config)
    {
        EconomyRuntime runtime = new EconomyRuntime();
        runtime.Init(config);
        return runtime;
    }
}
```

---

*Builder가 코드 생성 시 이 패턴을 참조한다.*
