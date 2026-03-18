# Human Fix Examples — Autonomous Game Factory v2

사람이 AI 생성 코드를 수정한 실제 사례를 Before/After 형태로 기록한다.
이 문서는 AI가 같은 실수를 반복하지 않도록 하는 가장 직접적인 교재다.

**삭제 금지. 사례 추가만 허용.**

---

## 사용법

| 역할 | 접근 |
|------|------|
| Builder | 코드 생성 전 유사 모듈의 과거 수정 사례 참조 |
| Reviewer | 검증 시 과거 수정 패턴과 대조 |
| Learning Recorder | 사람 수정 발생 시 새 사례 추가 |
| Human | 수정 이유(rationale) 직접 입력 |

---

## 엔트리 스키마

```
### HF-XXXX: <제목>

- **모듈**: <ModuleName>
- **파일**: <파일 경로>
- **관련 Validator**: <validator 이름 또는 "없음">
- **관련 규칙**: <RULE_MEMORY id 또는 "없음">
- **범위**: project | global
- **날짜**: YYYY-MM-DD

#### Before (AI 생성)
```code
// AI가 생성한 코드
```

#### After (사람 수정)
```code
// 사람이 수정한 코드
```

#### Rationale (수정 이유)
> 왜 이렇게 수정했는가

#### Prevention (예방 규칙)
> 다음부터 이 실수를 피하려면 무엇을 해야 하는가
```

---

## 사례 목록

### HF-0001: Runtime이 MonoBehaviour를 상속한 경우

- **모듈**: InventorySystem
- **파일**: Assets/Game/Modules/InventorySystem/InventoryRuntime.cs
- **관련 Validator**: ArchitectureRuleValidator
- **관련 규칙**: RM-0001
- **범위**: global
- **날짜**: 2026-03-18

#### Before (AI 생성)

```csharp
using UnityEngine;

namespace Game
{
    public class InventoryRuntime : MonoBehaviour
    {
        const int MAX_SLOTS = 20;

        int[] _itemIds;
        int[] _itemCounts;

        void Awake()
        {
            _itemIds = new int[MAX_SLOTS];
            _itemCounts = new int[MAX_SLOTS];
        }

        void Update()
        {
            // 아이템 만료 체크 로직
        }
    }
}
```

#### After (사람 수정)

```csharp
namespace Game
{
    public class InventoryRuntime
    {
        const int MAX_SLOTS = 20;

        int[] _itemIds;
        int[] _itemCounts;

        public void Init()
        {
            _itemIds = new int[MAX_SLOTS];
            _itemCounts = new int[MAX_SLOTS];
        }

        public void Tick(float deltaTime)
        {
            // 아이템 만료 체크 로직
        }
    }
}
```

#### Rationale (수정 이유)

> Runtime은 순수 C# 클래스여야 한다. MonoBehaviour를 상속하면:
> 1. 테스트가 어려워진다 (PlayMode 필요)
> 2. new로 생성할 수 없다 (AddComponent 필수)
> 3. GC 압박이 증가한다
> Awake → Init(), Update → Tick(deltaTime)으로 변환하여 Bootstrap에서 호출한다.

#### Prevention (예방 규칙)

> Builder는 *Runtime.cs 생성 시 절대 `: MonoBehaviour`를 붙이지 않는다.
> Unity 라이프사이클이 필요하면 Bootstrap에서 Tick()을 호출한다.

---

### HF-0002: 모듈 간 미선언 의존성

- **모듈**: CropGrowth
- **파일**: Assets/Game/Modules/CropGrowth/CropGrowthRuntime.cs
- **관련 Validator**: DependencyValidator
- **관련 규칙**: RM-0003
- **범위**: global
- **날짜**: 2026-03-18

#### Before (AI 생성)

```csharp
using UnityEngine;
using Game.Economy;      // MODULE_REGISTRY에 미선언
using Game.Inventory;    // MODULE_REGISTRY에 미선언

namespace Game
{
    public class CropGrowthRuntime
    {
        EconomyRuntime _economy;
        InventoryRuntime _inventory;

        public void Harvest(int cropId)
        {
            int value = _economy.GetCropValue(cropId);
            _inventory.AddItem(cropId, 1);
        }
    }
}
```

#### After (사람 수정)

```csharp
namespace Game
{
    public class CropGrowthRuntime
    {
        ICurrencyProvider _currency;
        IItemReceiver _itemReceiver;

        public void Init(ICurrencyProvider currency, IItemReceiver itemReceiver)
        {
            _currency = currency;
            _itemReceiver = itemReceiver;
        }

        public void Harvest(int cropId)
        {
            int value = _currency.GetValue(cropId);
            _itemReceiver.ReceiveItem(cropId, 1);
        }
    }
}
```

#### Rationale (수정 이유)

> 1. MODULE_REGISTRY.yaml에 Economy, Inventory 의존성이 선언되지 않았다.
> 2. 직접 Runtime 참조 대신 Shared 인터페이스(ICurrencyProvider, IItemReceiver)를 사용해야 한다.
> 3. 인터페이스를 사용하면 순환 의존을 원천 차단하고 테스트 시 Mock 주입이 가능하다.

#### Prevention (예방 규칙)

> Builder는 다른 모듈 참조 시:
> 1. MODULE_REGISTRY.yaml의 dependencies에 먼저 추가
> 2. 가능하면 Shared 인터페이스로 의존 역전
> 3. 직접 Runtime 참조는 최후의 수단

---

### HF-0003: foreach와 LINQ가 혼재된 경우

- **모듈**: EnemySpawner
- **파일**: Assets/Game/Modules/EnemySpawner/EnemySpawnerRuntime.cs
- **관련 Validator**: CodingStyleValidator
- **관련 규칙**: RM-0004, RM-0008
- **범위**: global
- **날짜**: 2026-03-18

#### Before (AI 생성)

```csharp
using System.Linq;

namespace Game
{
    public class EnemySpawnerRuntime
    {
        Enemy[] _enemies;

        public Enemy FindClosest(Vector3 position)
        {
            return _enemies
                .Where(e => e.IsAlive)
                .OrderBy(e => (e.Position - position).magnitude)
                .FirstOrDefault();
        }

        public void TickAll(float deltaTime)
        {
            foreach (var enemy in _enemies)
            {
                enemy.Tick(deltaTime);
            }
        }
    }
}
```

#### After (사람 수정)

```csharp
namespace Game
{
    public class EnemySpawnerRuntime
    {
        Enemy[] _enemies;
        int _enemyCount;

        public Enemy FindClosest(Vector3 position)
        {
            float minSqrDist = float.MaxValue;
            Enemy closest = null;
            for (int i = 0; i < _enemyCount; i++)
            {
                if (!_enemies[i].IsAlive) continue;
                float sqrDist = (_enemies[i].Position - position).sqrMagnitude;
                if (sqrDist >= minSqrDist) continue;
                minSqrDist = sqrDist;
                closest = _enemies[i];
            }
            return closest;
        }

        public void TickAll(float deltaTime)
        {
            for (int i = 0; i < _enemyCount; i++)
            {
                _enemies[i].Tick(deltaTime);
            }
        }
    }
}
```

#### Rationale (수정 이유)

> 1. LINQ의 Where/OrderBy/FirstOrDefault는 IEnumerable + 클로저를 매번 할당한다.
> 2. foreach도 IEnumerator를 할당한다.
> 3. magnitude 대신 sqrMagnitude를 사용하여 sqrt 연산을 제거했다.
> 4. 모바일 게임에서 GC spike는 프레임 드랍의 주범이다.

#### Prevention (예방 규칙)

> Builder는 컬렉션 처리 시:
> 1. using System.Linq 절대 추가하지 않음
> 2. foreach 대신 for (int i = 0; ...)
> 3. LINQ 대신 수동 루프 + 조건문
> 4. magnitude 대신 sqrMagnitude

---

### HF-0004: 테스트 폴더 구조 누락

- **모듈**: CropGrowth
- **파일**: (폴더 구조)
- **관련 Validator**: ModuleStructureValidator
- **관련 규칙**: RM-0015
- **범위**: project
- **날짜**: 2026-03-18

#### Before (AI 생성)

```
Assets/Game/Modules/CropGrowth/
├── ICropGrowth.cs
├── CropGrowthConfig.cs
├── CropGrowthRuntime.cs
├── CropGrowthFactory.cs
└── CropGrowthBootstrap.cs
   (Tests 폴더 없음)
```

#### After (사람 수정)

```
Assets/Game/Modules/CropGrowth/
├── ICropGrowth.cs
├── CropGrowthConfig.cs
├── CropGrowthRuntime.cs
├── CropGrowthFactory.cs
├── CropGrowthBootstrap.cs
└── Tests/
    └── Editor/
        └── CropGrowthTests.cs
```

#### Rationale (수정 이유)

> 모듈 템플릿은 6개 파일을 필수로 요구한다. Tests/Editor/<Module>Tests.cs가 없으면
> ModuleStructureValidator가 실패한다. 테스트 없이 커밋된 모듈은 품질 보증이 불가능하다.

#### Prevention (예방 규칙)

> Builder는 모듈 생성 시 반드시 6파일 체크리스트를 확인한다:
> I<Module>.cs, <Module>Config.cs, <Module>Runtime.cs,
> <Module>Factory.cs, <Module>Bootstrap.cs, Tests/Editor/<Module>Tests.cs

---

## 다음 엔트리 ID: HF-0005

---

## 사례 추가 규칙

1. 모든 사람 수정은 반드시 Before/After와 Rationale을 기록한다.
2. **Rationale이 없는 수정은 존재해서는 안 된다.**
3. Prevention은 Builder가 즉시 적용 가능한 구체적 규칙으로 작성한다.
4. 관련 RULE_MEMORY 규칙이 없으면 새로 추가한다.
5. 같은 유형의 수정이 3회 이상 반복되면 CODING_PATTERNS.md에도 패턴으로 등록한다.
