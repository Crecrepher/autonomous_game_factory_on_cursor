# Codebase Inventory — MatchClash Playable002

**스캔 대상**: `Assets/Supercent/MatchClash/Playable002/Scripts/`
**스캔 일시**: 2026-03-19
**총 .cs 파일**: 49개
**총 라인 수**: 13,122줄

---

## 파일 분류표

### Core (3 files, 1,195 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Core/GameFlowController.cs` | 1,032 | MonoBehaviour | **GodClass** (1,032줄), **LogicInMonoBehaviour** |
| `Core/GameSceneInitializer.cs` | 87 | MonoBehaviour (Bootstrapper) | 없음 |
| `Core/StageController.cs` | 76 | Pure C# | 없음 |

### Config (1 file, 160 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Config/GameBalanceConfig.cs` | 160 | ScriptableObject | 없음 |

### Events (1 file, 219 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Events/GameEvents.cs` | 219 | Static Utility (Event Bus) | **GlobalStaticState** |

### Character/Controller (2 files, 223 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Character/Controller/CharacterController.cs` | 143 | Pure C# | 없음 |
| `Character/Controller/SkillGauge.cs` | 80 | Pure C# (Serializable) | 없음 |

### Character/Model (2 files, 90 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Character/Model/CharacterModel.cs` | 75 | Pure C# (Serializable) | 없음 |
| `Character/Model/EWeaponType.cs` | 15 | Enum | 없음 |

### Character/View (2 files, 379 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Character/View/CharacterView.cs` | 314 | MonoBehaviour (View) | 없음 |
| `Character/View/SkillAnimationPlayer.cs` | 65 | MonoBehaviour (View) | 없음 |

### Combat/Controller (2 files, 189 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Combat/Controller/CombatController.cs` | 150 | Pure C# | 없음 |
| `Combat/Controller/DamageCalculator.cs` | 39 | Pure C# | 없음 |

### Combat/Model (1 file, 68 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Combat/Model/MonsterModel.cs` | 68 | Pure C# (Serializable) | 없음 |

### Combat/View (3 files, 2,423 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Combat/View/CombatVFX.cs` | 1,428 | MonoBehaviour | **GodClass** (1,428줄), **LogicInMonoBehaviour** |
| `Combat/View/MonsterView.cs` | 803 | MonoBehaviour (View) | **GodClass** (803줄) |
| `Combat/View/DamageText.cs` | 192 | MonoBehaviour (View) | 없음 |

### Puzzle/Controller (5 files, 1,425 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Puzzle/Controller/PuzzleController.cs` | 273 | Pure C# | 없음 |
| `Puzzle/Controller/PuzzleInputHandler.cs` | 299 | Pure C# | 없음 |
| `Puzzle/Controller/PuzzleMatcher.cs` | 823 | Pure C# | **GodClass** (823줄) |
| `Puzzle/Controller/PuzzleRefill.cs` | 229 | Pure C# | 없음 |
| `Puzzle/Controller/SpecialBlockActivator.cs` | 310 | Pure C# | 없음 |

### Puzzle/Model (4 files, 321 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Puzzle/Model/PuzzleBoard.cs` | 169 | Pure C# (Serializable) | 없음 |
| `Puzzle/Model/BlockData.cs` | 57 | Struct | 없음 |
| `Puzzle/Model/MatchResult.cs` | 72 | Struct | 없음 |
| `Puzzle/Model/EBlockType.cs` | 23 | Enum | 없음 |

### Puzzle/View (3 files, 2,675 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Puzzle/View/PuzzleBoardView.cs` | 1,714 | MonoBehaviour (View) | **GodClass** (1,714줄), **LogicInMonoBehaviour** |
| `Puzzle/View/BlockView.cs` | 874 | MonoBehaviour (View) | **GodClass** (874줄) |
| `Puzzle/View/PuzzleCameraAspectFitter.cs` | 87 | MonoBehaviour (View) | 없음 |

### Skill (2 files, 332 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Skill/SkillController.cs` | 244 | Pure C# | 없음 |
| `Skill/SkillExecutor.cs` | 88 | Pure C# | 없음 |

### Sound (1 file, 193 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Sound/GameSoundController.cs` | 193 | MonoBehaviour | 없음 |

### EndCard (1 file, 95 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `EndCard/EndCardController.cs` | 95 | MonoBehaviour (View) | 없음 |

### Input (1 file, 557 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Input/InputGuideController.cs` | 557 | MonoBehaviour | **GodClass** (557줄), **LogicInMonoBehaviour** |

### VFX (13 files, 1,737 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `VFX/ProjectileEffect.cs` | 127 | MonoBehaviour | 없음 |
| `VFX/ManaProjectile.cs` | 170 | MonoBehaviour | 없음 |
| `VFX/SwordProjectile.cs` | 149 | MonoBehaviour | 없음 |
| `VFX/BombProjectile.cs` | 146 | MonoBehaviour | 없음 |
| `VFX/LightningProjectile.cs` | 461 | MonoBehaviour | 없음 |
| `VFX/HitEffect.cs` | 82 | MonoBehaviour | 없음 |
| `VFX/HitEffectPool.cs` | 105 | MonoBehaviour (Pool) | 없음 |
| `VFX/BlockMatchParticle.cs` | 83 | MonoBehaviour | 없음 |
| `VFX/BlockMatchParticlePool.cs` | 109 | MonoBehaviour (Pool) | 없음 |
| `VFX/CameraShake.cs` | 136 | MonoBehaviour | 없음 |
| `VFX/VFXEventListener.cs` | 149 | MonoBehaviour (Listener) | 없음 |
| `VFX/EProjectileType.cs` | 12 | Enum | 없음 |
| `VFX/LightningDebugHelper.cs` | 69 | MonoBehaviour (Debug) | 없음 |

### Utils (2 files, 271 lines)

| 파일 | 라인 | 분류 | 드리프트 |
|------|------|------|---------|
| `Utils/AutoBindHelper.cs` | 123 | Static Utility (Editor) | 없음 |
| `Utils/CoordinateConverter.cs` | 148 | Pure C# | 없음 |

---

## 아키텍처 드리프트 요약

| 드리프트 유형 | 해당 파일 수 | 파일 |
|-------------|------------|------|
| **GodClass** (>500줄) | 7 | GameFlowController, CombatVFX, PuzzleBoardView, BlockView, PuzzleMatcher, MonsterView, InputGuideController |
| **LogicInMonoBehaviour** | 4 | GameFlowController, CombatVFX, PuzzleBoardView, InputGuideController |
| **GlobalStaticState** | 1 | GameEvents (static 이벤트 버스) |
| **GCHeavy** | 0 | foreach/LINQ/코루틴/람다 0건 |
| **MutableStateInSO** | 0 | GameBalanceConfig는 설정만 |
| **EditorRuntimeMix** | 0 | `#if UNITY_EDITOR`로 정상 분리 |

---

## 타입 분포

| 타입 | 파일 수 |
|------|---------|
| MonoBehaviour | 22 |
| Pure C# | 14 |
| Serializable Pure C# | 5 |
| Struct | 2 |
| Enum | 3 |
| Static Utility | 2 |
| ScriptableObject | 1 |
