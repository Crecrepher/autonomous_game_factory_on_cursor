# 모듈 템플릿 가이드

Builder 에이전트가 새 모듈을 생성할 때 **반드시** 따라야 하는 구조와 규칙을 정의한다.
표준 참조 구현은 `Assets/Game/Modules/Template/`에 있다.

---

## 1. 폴더 구조

모든 모듈은 아래 구조를 따른다. `<Module>`은 모듈 이름(PascalCase)으로 치환한다.

```
Assets/Game/Modules/<Module>/
├── I<Module>.cs                 # 인터페이스 — 공개 계약
├── <Module>Config.cs            # ScriptableObject — 설정 데이터
├── <Module>Runtime.cs           # 순수 C# — 비즈니스 로직
├── <Module>Factory.cs           # static class — 생성/의존성 주입
├── <Module>Bootstrap.cs         # MonoBehaviour — 씬 진입점 (얇게)
└── Tests/
    └── Editor/
        └── <Module>Tests.cs     # NUnit 테스트 — 최소 1개
```

---

## 2. 각 파일의 역할과 규칙

### 2.1 `I<Module>.cs` — 인터페이스

**역할**: 모듈의 공개 계약. 다른 모듈/시스템은 이 인터페이스만 참조한다.

**규칙**:
- 네임스페이스: `Game`
- 최소 메서드: `Init()`, `Tick(float deltaTime)`
- 도메인에 맞는 추가 메서드/프로퍼티 정의 가능
- 구현 세부사항 노출 금지

**예시**:

```csharp
namespace Game
{
    public interface ICurrencyWallet
    {
        void Init();
        void Tick(float deltaTime);
        int Balance { get; }
        bool TrySpend(int amount);
        void Add(int amount);
        event System.Action<int> OnBalanceChanged;
    }
}
```

### 2.2 `<Module>Config.cs` — 설정 데이터

**역할**: 에디터에서 에셋으로 생성하는 설정 데이터. 로직 없음.

**규칙**:
- `ScriptableObject` 상속
- `[CreateAssetMenu]` 어트리뷰트 필수
- 모든 필드는 `[SerializeField]` private → 프로퍼티(`=>`)로 노출
- 매직넘버 금지, const 사용
- 로직/계산 코드 금지 (순수 데이터만)

**예시**:

```csharp
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "CurrencyWalletConfig", menuName = "Game/Modules/CurrencyWalletConfig")]
    public class CurrencyWalletConfig : ScriptableObject
    {
        [SerializeField] int _startingBalance = 6;
        [SerializeField] int _maxBalance = 9999;

        public int StartingBalance => _startingBalance;
        public int MaxBalance => _maxBalance;
    }
}
```

### 2.3 `<Module>Runtime.cs` — 비즈니스 로직

**역할**: 모듈의 핵심 로직. 상태 관리, 비즈니스 규칙 처리.

**규칙**:
- **MonoBehaviour 상속 금지** — 순수 C# 클래스
- 인터페이스(`I<Module>`) 구현
- 생성자에서 Config를 받아 의존성 주입
- GC 유발 코드 금지:
  - 코루틴 사용 금지
  - 무명 메서드/람다 사용 금지
  - LINQ 사용 금지
  - foreach 사용 금지 (for문만 사용)
  - Invoke 사용 금지
- Update 성능 규칙:
  - `magnitude`, `distance` 대신 `sqrMagnitude` 사용
  - 나눗셈은 역수를 캐싱 후 곱셈으로 대체
  - `Mathf`의 무거운 연산 회피
- `?` (null conditional) 사용 금지
- 매직넘버 금지, const 사용 (`UPPER_SNAKE_CASE`)
- 필드 순서: const → public static → private static → 이벤트 → 프로퍼티 → public → 직렬화 → private
- 비-static 필드는 기능 단위 주석으로 분류 후, 그 안에서 순서 준수

**예시**:

```csharp
using System;

namespace Game
{
    public class CurrencyWalletRuntime : ICurrencyWallet
    {
        const int MIN_BALANCE = 0;

        // 잔고
        public event Action<int> OnBalanceChanged;
        public int Balance => _balance;
        readonly CurrencyWalletConfig _config;
        int _balance;

        public CurrencyWalletRuntime(CurrencyWalletConfig config)
        {
            _config = config;
        }

        public void Init()
        {
            _balance = _config.StartingBalance;
        }

        public void Tick(float deltaTime)
        {
        }

        public bool TrySpend(int amount)
        {
            if (_balance < amount)
                return false;

            _balance -= amount;
            if (_balance < MIN_BALANCE)
                _balance = MIN_BALANCE;

            OnBalanceChanged?.Invoke(_balance);
            return true;
        }

        public void Add(int amount)
        {
            _balance += amount;
            if (_balance > _config.MaxBalance)
                _balance = _config.MaxBalance;

            OnBalanceChanged?.Invoke(_balance);
        }
    }
}
```

### 2.4 `<Module>Factory.cs` — 생성

**역할**: Config로 Runtime 인스턴스를 생성. 의존성 주입 진입점.

**규칙**:
- `static class`
- Config를 받아 인터페이스(`I<Module>`)를 반환
- 복잡한 의존성이 있으면 추가 파라미터로 주입

**예시**:

```csharp
namespace Game
{
    public static class CurrencyWalletFactory
    {
        public static ICurrencyWallet CreateRuntime(CurrencyWalletConfig config)
        {
            return new CurrencyWalletRuntime(config);
        }
    }
}
```

### 2.5 `<Module>Bootstrap.cs` — 씬 진입점

**역할**: 씬에서 한 번 초기화. Config 참조 → Factory 호출 → Runtime 등록.

**규칙**:
- `MonoBehaviour` 상속 (유일하게 허용되는 곳)
- **얇게 유지**: 로직 없음, 바인딩/라이프사이클만
- `[SerializeField]`로 Config 참조
- `Start()`에서 Factory 호출 → Init
- Awake는 독립된 기능이 아니면 사용 금지
- 싱글턴 패턴 사용 금지

**예시**:

```csharp
using UnityEngine;

namespace Game
{
    public class CurrencyWalletBootstrap : MonoBehaviour
    {
        [SerializeField] CurrencyWalletConfig _config;

        void Start()
        {
            if (_config == null)
                return;

            ICurrencyWallet runtime = CurrencyWalletFactory.CreateRuntime(_config);
            runtime.Init();
        }
    }
}
```

### 2.6 `<Module>Tests.cs` — 테스트

**역할**: 모듈의 핵심 경로를 검증하는 NUnit 테스트.

**규칙**:
- `Tests/Editor/` 폴더에 위치
- 최소 2개 테스트:
  1. Factory로 Runtime 생성 후 null이 아닌지 확인
  2. Init → Tick 호출 시 예외 없는지 확인
- 도메인 로직에 대한 추가 테스트 권장
- ScriptableObject 생성 후 반드시 `Object.DestroyImmediate()`로 정리

**예시**:

```csharp
using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class CurrencyWalletTests
    {
        [Test]
        public void CreateRuntime_WithConfig_ReturnsNonNull()
        {
            var config = ScriptableObject.CreateInstance<CurrencyWalletConfig>();
            ICurrencyWallet runtime = CurrencyWalletFactory.CreateRuntime(config);
            Assert.IsNotNull(runtime);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Init_ThenTick_DoesNotThrow()
        {
            var config = ScriptableObject.CreateInstance<CurrencyWalletConfig>();
            ICurrencyWallet runtime = CurrencyWalletFactory.CreateRuntime(config);
            runtime.Init();
            Assert.DoesNotThrow(() => runtime.Tick(0.016f));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Add_IncreasesBalance()
        {
            var config = ScriptableObject.CreateInstance<CurrencyWalletConfig>();
            ICurrencyWallet runtime = CurrencyWalletFactory.CreateRuntime(config);
            runtime.Init();
            int initialBalance = runtime.Balance;
            runtime.Add(10);
            Assert.AreEqual(initialBalance + 10, runtime.Balance);
            Object.DestroyImmediate(config);
        }
    }
}
```

---

## 3. 런타임 제약 조건 요약

| 규칙 | 허용 | 금지 |
|------|------|------|
| MonoBehaviour | Bootstrap/View만 | Runtime, Config, Factory |
| 로직 위치 | Runtime (순수 C#) | Bootstrap, Config |
| GC 할당 | 없어야 함 | 코루틴, 람다, LINQ, foreach |
| 반복문 | `for` | `foreach` |
| 널 체크 | `== null` | `?.` `??` |
| 매직넘버 | 금지 | `const UPPER_SNAKE` 사용 |
| 필드 노출 | 프로퍼티/`=>` | public 필드 직접 노출 |
| 런타임 검색 | 금지 | `GetComponent`, `Find` 등 |
| 통신 | 이벤트, 인터페이스 | 이벤트 버스, 리플렉션 |
| 문자열 | StringBuilder 또는 static 배열 | 런타임 string 결합 |
| Animator | `Animator.StringToHash` 캐싱 | 문자열 직접 사용 |
| 컬렉션 | 길이 고정이면 `[]` 배열 | 불필요한 `List<>` |

---

## 4. 의존성 규칙

- 모듈 간 의존은 `MODULE_REGISTRY.yaml`의 `dependencies`에 선언된 것만 허용
- 순환 의존 절대 금지
- 공용 타입은 `Assets/Game/Shared/`에 배치
- 다른 모듈의 Runtime을 직접 참조하지 않고, 인터페이스(`I<Module>`)만 참조
- `UnityEngine`, `System` 네임스페이스는 dependencies에 명시 후 사용 가능

---

## 5. 새 모듈 생성 절차

1. `Assets/Game/Modules/Template/` 전체를 복사
2. 모든 파일명에서 `Template` → `<Module>` 로 치환
3. 모든 클래스/인터페이스명에서 `Template` → `<Module>` 로 치환
4. 도메인에 맞게 인터페이스 메서드/프로퍼티 추가
5. Config에 도메인 설정 필드 추가
6. Runtime에 비즈니스 로직 구현
7. Factory에 의존성 주입 파라미터 추가 (필요 시)
8. Bootstrap에서 추가 바인딩 (필요 시)
9. Tests에 도메인 로직 테스트 추가
10. `MODULE_REGISTRY.yaml`에 모듈 항목 등록
