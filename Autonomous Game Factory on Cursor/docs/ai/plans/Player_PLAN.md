# Player 모듈 PLAN

> **범용 모듈** — dimension: both, genre_tags: [universal, action, arcade, idle, runner, rpg]
> 아래 PLAN은 최초 구현 시의 컨텍스트를 포함하지만, Player 모듈 자체는 모든 장르에서 재사용 가능하다.

> Planner 에이전트 산출물 — TASK_QUEUE.yaml `Player` 태스크

---

## PLAN

```yaml
PLAN:
  module: Player
  target_files:
    - Assets/Game/Modules/Player/IPlayer.cs
    - Assets/Game/Modules/Player/PlayerConfig.cs
    - Assets/Game/Modules/Player/PlayerRuntime.cs
    - Assets/Game/Modules/Player/PlayerFactory.cs
    - Assets/Game/Modules/Player/PlayerBootstrap.cs
    - Assets/Game/Modules/Player/Tests/Editor/PlayerTests.cs
  dependencies: [UnityEngine, System]
  core_access: false
  risk: medium
  notes: >
    플레이어 이동, 자동 공격, 입력 처리.
    GameManager에서 참조. 적(Enemies) 모듈과는 인터페이스를 통해 간접 통신.
```

---

## 1. 모듈 목적

Player 모듈은 **플레이어 캐릭터의 이동·자동 공격·상태 관리**를 담당한다.

- 이동: 기지 주변 이동, 이동 속도 관리
- 자동 공격: 범위 내 적 자동 공격, 공격 속도·데미지 관리
- 입력 처리: 터치/클릭 입력 → 이동 방향 결정
- 상태 이벤트: 공격/이동 상태 변경 이벤트 발행

---

## 2. 인터페이스 설계 (`IPlayer.cs`)

```csharp
namespace Game
{
    public interface IPlayer
    {
        void Init();
        void Tick(float deltaTime);

        float MoveSpeed { get; }
        float AttackSpeed { get; }
        int AttackDamage { get; }
        float AttackRange { get; }
        bool IsAttacking { get; }

        void SetMoveDirection(float x, float y);
        void SetMoveSpeed(float speed);
        void SetAttackSpeed(float speed);

        event System.Action OnAttack;
        event System.Action OnAttackTargetChanged;
    }
}
```

### 설계 근거

- `SetMoveSpeed`/`SetAttackSpeed`: DynamicConfig 모듈에서 런타임에 속도를 조절하기 위한 세터
- `OnAttack`: 공격 시 이펙트/사운드 연출용 이벤트
- `SetMoveDirection`: 입력 시스템으로부터 방향 전달
- `AttackRange`: sqrMagnitude 기반 범위 판정에 사용 (Update에서 magnitude 대신)

---

## 3. Config 설계 (`PlayerConfig.cs`)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `_moveSpeed` | `float` | 5f | 기본 이동 속도 |
| `_attackSpeed` | `float` | 1f | 초당 공격 횟수 |
| `_attackDamage` | `int` | 1 | 공격당 데미지 |
| `_attackRange` | `float` | 3f | 공격 범위 |

---

## 4. Runtime 설계 (`PlayerRuntime.cs`)

### 상태

- `_moveDirection` (Vector2): 현재 이동 방향
- `_attackTimer` (float): 공격 쿨다운 타이머
- `_inversedAttackSpeed` (float): 공격 간격 역수 캐싱
- `_sqrAttackRange` (float): 공격 범위 제곱 캐싱

### 핵심 로직

1. **Init()**: Config 값으로 초기화, 역수/제곱 캐싱
2. **Tick(float)**: 이동 처리, 공격 쿨다운 갱신
3. **SetMoveDirection**: 입력 방향 설정
4. **SetMoveSpeed/SetAttackSpeed**: DynamicConfig 연동

---

## 5. 테스트 설계

| # | 테스트명 | 검증 내용 |
|---|---------|-----------|
| 1 | `CreateRuntime_WithConfig_ReturnsNonNull` | Factory 생성 |
| 2 | `Init_ThenTick_DoesNotThrow` | 기본 동작 |
| 3 | `Init_SetsConfigValues` | Config 값 반영 |
| 4 | `SetMoveSpeed_UpdatesSpeed` | 속도 변경 |
| 5 | `SetAttackSpeed_UpdatesSpeed` | 공격 속도 변경 |

---

## 6. 위험 요소

| 위험 | 수준 | 대응 |
|------|------|------|
| GameManager 의존 | low | 인터페이스만 노출 |
| DynamicConfig 연동 | low | setter 메서드 제공 |
| 적 타겟팅 로직 | medium | 별도 인터페이스로 분리, 현재는 빈 구현 |
