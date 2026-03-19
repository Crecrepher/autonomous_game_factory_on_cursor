# Dependency Graph — MatchClash Playable002

**분석 일시**: 2026-03-19

---

## 네임스페이스 의존성

```
Supercent.MatchClash.Playable002
├── .Core
│   ├── → .Config
│   ├── → .Events
│   ├── → .Character.Controller
│   ├── → .Character.Model
│   ├── → .Character.View
│   ├── → .Combat.Controller
│   ├── → .Combat.View
│   ├── → .Puzzle.Controller
│   ├── → .Puzzle.Model
│   ├── → .Puzzle.View
│   ├── → .Skill
│   ├── → .Sound
│   ├── → .Input
│   ├── → .VFX
│   ├── → .EndCard
│   └── → .Utils
├── .Character.Controller
│   ├── → .Character.Model
│   ├── → .Character.View
│   ├── → .Config
│   └── → .Events
├── .Character.View
│   └── (UnityEngine, UnityEngine.UI만)
├── .Character.Model
│   └── (System, UnityEngine만)
├── .Combat.Controller
│   ├── → .Combat.Model
│   ├── → .Config
│   └── → .Events
├── .Combat.View
│   ├── → .Character.View
│   ├── → .Config
│   ├── → .Events
│   ├── → .Puzzle.Model
│   ├── → .Utils
│   └── → .VFX
├── .Combat.Model
│   └── (System만)
├── .Puzzle.Controller
│   ├── → .Config
│   ├── → .Events
│   └── → .Puzzle.Model
├── .Puzzle.View
│   └── (UnityEngine만, Model은 BlockData/EBlockType 참조)
├── .Puzzle.Model
│   └── (System, UnityEngine만)
├── .Skill
│   ├── → .Character.Controller
│   ├── → .Character.Model
│   ├── → .Combat.Controller
│   ├── → .Config
│   └── → .Events
├── .Sound
│   ├── → .Events
│   └── → .Puzzle.Model
├── .Input
│   ├── → .Config
│   ├── → .Events
│   ├── → .Puzzle.Controller
│   ├── → .Character.View
│   └── → .Utils
├── .VFX
│   ├── → .Character.View
│   ├── → .Combat.View
│   └── → .Events
├── .EndCard
│   └── → .Events
├── .Events
│   └── (System만 — 순수 이벤트 정의)
├── .Config
│   └── (UnityEngine만)
└── .Utils
    └── (UnityEngine, UnityEngine.UI만)
```

---

## 모듈 후보 간 의존성 그래프

```
MC-09: GameFlow ──────────→ MC-01: PuzzleCore
       │                    MC-02: PuzzleView
       │                    MC-03: BattleCharacter
       │                    MC-04: BattleCharacterView
       │                    MC-05: BattleMonster
       │                    MC-06: BattleMonsterView
       │                    MC-07: BattleEffect
       │                    MC-08: SkillSystem
       │                    MC-10: GameConfig
       │                    MC-11: GameEvents
       │                    MC-12: InputGuide
       │                    MC-13: GameSound
       │                    MC-14: EndCard
       │                    MC-15: Utils

MC-08: SkillSystem ───────→ MC-03: BattleCharacter
                             MC-05: BattleMonster
                             MC-10: GameConfig
                             MC-11: GameEvents

MC-07: BattleEffect ──────→ MC-04: BattleCharacterView
                             MC-06: BattleMonsterView
                             MC-11: GameEvents
                             MC-15: Utils

MC-12: InputGuide ────────→ MC-01: PuzzleCore
                             MC-02: PuzzleView
                             MC-04: BattleCharacterView
                             MC-10: GameConfig
                             MC-11: GameEvents
                             MC-15: Utils

MC-03: BattleCharacter ───→ MC-10: GameConfig
                             MC-11: GameEvents

MC-05: BattleMonster ─────→ MC-10: GameConfig
                             MC-11: GameEvents

MC-01: PuzzleCore ────────→ MC-10: GameConfig
                             MC-11: GameEvents

MC-13: GameSound ─────────→ MC-11: GameEvents

MC-14: EndCard ───────────→ MC-11: GameEvents

MC-02: PuzzleView ────────→ (UnityEngine 전용)

MC-04: BattleCharacterView → (UnityEngine, UI 전용)

MC-06: BattleMonsterView ─→ (UnityEngine 전용)

MC-10: GameConfig ────────→ (UnityEngine 전용)

MC-11: GameEvents ────────→ (System 전용)

MC-15: Utils ─────────────→ (UnityEngine 전용)
```

---

## 순환 의존 분석

**순환 의존 감지: 0건**

모든 의존성이 단방향 계층 구조를 유지하고 있음.
GameEvents가 중앙 허브 역할을 하면서 간접적으로 모듈 간 통신을 중개하고 있어 직접 순환은 발생하지 않음.

---

## 핵심 허브 노드

| 노드 | 참조하는 모듈 수 | 참조받는 모듈 수 | 역할 |
|------|----------------|----------------|------|
| MC-11: GameEvents | 0 | 10 | 이벤트 허브 (최다 피참조) |
| MC-10: GameConfig | 0 | 5 | 설정 허브 |
| MC-09: GameFlow | 14 | 0 | 최상위 오케스트레이터 (최다 참조) |
| MC-15: Utils | 0 | 3 | 유틸리티 |
