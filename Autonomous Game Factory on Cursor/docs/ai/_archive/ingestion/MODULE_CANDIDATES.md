# Module Candidates — MatchClash Playable002

**분석 일시**: 2026-03-19
**분석 기준**: 폴더 그룹별 기능 단위 + AGF 6파일 구조 매핑

---

## 모듈 후보 목록

### MC-01: PuzzleCore

**소스 파일**:
- `Puzzle/Controller/PuzzleController.cs` (273줄)
- `Puzzle/Controller/PuzzleMatcher.cs` (823줄)
- `Puzzle/Controller/PuzzleRefill.cs` (229줄)
- `Puzzle/Controller/PuzzleInputHandler.cs` (299줄)
- `Puzzle/Controller/SpecialBlockActivator.cs` (310줄)
- `Puzzle/Model/PuzzleBoard.cs` (169줄)
- `Puzzle/Model/BlockData.cs` (57줄)
- `Puzzle/Model/MatchResult.cs` (72줄)
- `Puzzle/Model/EBlockType.cs` (23줄)

**책임**: 매치-3 퍼즐 로직 전체 — 보드 상태, 스왑, 매칭 감지, 리필, 특수 블록 활성화, 입력 처리
**전략**: `AdaptExistingModule` — MODULE_REGISTRY의 PuzzleBlock + PuzzleBoard + SpecialBlock에 대응
**위험도**: Medium (PuzzleMatcher 823줄 GodClass)
**매핑**: 기존 Registry의 PuzzleBlock/PuzzleBoard/SpecialBlock 3개 모듈을 합산 커버
**비고**: 실제 코드는 이 9개 파일이 하나의 퍼즐 엔진으로 밀접하게 결합되어 있음. Controller가 Runtime 역할, Model이 데이터 역할을 이미 수행 중.

---

### MC-02: PuzzleView

**소스 파일**:
- `Puzzle/View/PuzzleBoardView.cs` (1,714줄)
- `Puzzle/View/BlockView.cs` (874줄)
- `Puzzle/View/PuzzleCameraAspectFitter.cs` (87줄)

**책임**: 퍼즐 보드의 시각적 표현 — 블록 스프라이트, 스왑/소멸/낙하/스폰 애니메이션, 카메라 피팅
**전략**: `WrapWithModuleInterface` — View 전용 모듈로 분리, PuzzleCore 인터페이스 참조
**위험도**: High (PuzzleBoardView 1,714줄 GodClass)
**비고**: PuzzleBoardView가 로직+연출 혼재. Bootstrap 역할도 일부 수행 중.

---

### MC-03: BattleCharacter

**소스 파일**:
- `Character/Controller/CharacterController.cs` (143줄)
- `Character/Controller/SkillGauge.cs` (80줄)
- `Character/Model/CharacterModel.cs` (75줄)
- `Character/Model/EWeaponType.cs` (15줄)

**책임**: 3명 캐릭터 관리 — 스킬 게이지, 무기 타입, 퍼즐 매치 → 게이지 충전
**전략**: `AdaptExistingModule` — MODULE_REGISTRY의 BattleCharacter에 대응
**위험도**: Low
**매핑**: 기존 Registry의 BattleCharacter 1:1 대응
**비고**: MVC 분리 양호. Controller가 Runtime 역할, Model이 데이터 역할.

---

### MC-04: BattleCharacterView

**소스 파일**:
- `Character/View/CharacterView.cs` (314줄)
- `Character/View/SkillAnimationPlayer.cs` (65줄)

**책임**: 캐릭터 시각 표현 — 게이지 바 UI, 스킬 버튼, 공격/스킬 애니메이션, 스케일 연출
**전략**: `WrapWithModuleInterface` — View 전용, BattleCharacter 인터페이스 참조
**위험도**: Low
**비고**: AGF에서는 MatchClashUI 모듈의 일부로 통합 가능.

---

### MC-05: BattleMonster

**소스 파일**:
- `Combat/Controller/CombatController.cs` (150줄)
- `Combat/Controller/DamageCalculator.cs` (39줄)
- `Combat/Model/MonsterModel.cs` (68줄)

**책임**: 몬스터 HP 관리, 데미지 계산, 사망 판정, 스테이지 전환 트리거
**전략**: `AdaptExistingModule` — MODULE_REGISTRY의 BattleMonster에 대응
**위험도**: Low
**매핑**: CombatController + DamageCalculator가 Runtime 역할, MonsterModel이 데이터 역할
**비고**: MVC 분리 양호.

---

### MC-06: BattleMonsterView

**소스 파일**:
- `Combat/View/MonsterView.cs` (803줄)
- `Combat/View/DamageText.cs` (192줄)

**책임**: 몬스터 시각 표현 — HP 바, 트레일링 바, 피격 플래시, 사망 연출, 데미지 텍스트
**전략**: `WrapWithModuleInterface` — View 전용
**위험도**: Medium (MonsterView 803줄 GodClass)

---

### MC-07: BattleEffect

**소스 파일**:
- `Combat/View/CombatVFX.cs` (1,428줄)
- `VFX/ProjectileEffect.cs` (127줄)
- `VFX/ManaProjectile.cs` (170줄)
- `VFX/SwordProjectile.cs` (149줄)
- `VFX/BombProjectile.cs` (146줄)
- `VFX/LightningProjectile.cs` (461줄)
- `VFX/HitEffect.cs` (82줄)
- `VFX/HitEffectPool.cs` (105줄)
- `VFX/BlockMatchParticle.cs` (83줄)
- `VFX/BlockMatchParticlePool.cs` (109줄)
- `VFX/CameraShake.cs` (136줄)
- `VFX/VFXEventListener.cs` (149줄)
- `VFX/EProjectileType.cs` (12줄)
- `VFX/LightningDebugHelper.cs` (69줄)

**책임**: 전투 이펙트 전체 — 투사체(칼/폭탄/번개/마나), 히트 이펙트, 블록 매치 파티클, 카메라 셰이크, VFX 이벤트 리스너
**전략**: `AdaptExistingModule` — MODULE_REGISTRY의 BattleEffect에 대응
**위험도**: High (CombatVFX 1,428줄 GodClass)
**비고**: CombatVFX가 투사체 발사 + 이벤트 처리 + 풀 관리를 전부 담당. 분할 추천.

---

### MC-08: SkillSystem

**소스 파일**:
- `Skill/SkillController.cs` (244줄)
- `Skill/SkillExecutor.cs` (88줄)

**책임**: 스킬 발동 조건 관리, 수동/자동 모드, 데미지 버스트 분할 적용
**전략**: `ExtractRuntimeModule` — 기존 Registry에 독립 모듈 없음, BattleCharacter에 통합 가능하나 단일 책임 원칙상 분리 유지 권장
**위험도**: Low
**비고**: SkillController는 Pure C#, 이미 AGF Runtime 패턴에 가까움.

---

### MC-09: GameFlow

**소스 파일**:
- `Core/GameFlowController.cs` (1,032줄)
- `Core/GameSceneInitializer.cs` (87줄)
- `Core/StageController.cs` (76줄)

**책임**: 게임 전체 흐름 제어 — 초기화, 게임 시작, 스테이지 전환, 게임 종료, 엔드카드 트리거
**전략**: `SplitIntoMultipleModules` — GameFlowController 1,032줄 GodClass → GameManager + StageManager로 분할
**위험도**: High (GodClass + LogicInMonoBehaviour)
**매핑**: MODULE_REGISTRY의 GameManager에 대응
**비고**: GameFlowController가 퍼즐/전투/캐릭터/스킬/VFX/사운드/가이드 모든 컨트롤러를 직접 참조하고 조율. 분할 시 의존성 그래프 재설계 필요.

---

### MC-10: GameConfig

**소스 파일**:
- `Config/GameBalanceConfig.cs` (160줄)

**책임**: 게임 전체 밸런스 설정 — 보드 크기, 몬스터 HP, 스킬 게이지, 데미지, 가이드 타이밍 등
**전략**: `ReuseAsIs` — 이미 ScriptableObject 구조, AGF Config 패턴에 부합
**위험도**: Low
**비고**: 모듈별로 Config 분리가 이상적이나, Playable Ad 특성상 단일 Config가 실용적.

---

### MC-11: GameEvents

**소스 파일**:
- `Events/GameEvents.cs` (219줄)

**책임**: 전역 이벤트 정의 — Puzzle, Combat, Skill, Character, Stage, GameFlow 이벤트
**전략**: `LeaveUnclassified` — AGF에서는 이벤트 버스 사용 금지가 원칙이나, 기존 코드가 이 패턴에 강하게 의존. 즉시 제거 불가.
**위험도**: Medium (GlobalStaticState 드리프트)
**비고**: 단계적으로 모듈 간 직접 이벤트/인터페이스 참조로 전환 필요. 초기에는 유지.

---

### MC-12: InputGuide

**소스 파일**:
- `Input/InputGuideController.cs` (557줄)

**책임**: 플레이 유도 — 손가락 가이드, 스킬 가이드, 특수 블록 가이드, 유휴 감지
**전략**: `AdaptExistingModule` — MODULE_REGISTRY의 Guide에 대응
**위험도**: Medium (557줄, LogicInMonoBehaviour)
**비고**: AGF Guide 모듈을 확장하여 매치-3 특화 가이드 로직 추가 가능.

---

### MC-13: GameSound

**소스 파일**:
- `Sound/GameSoundController.cs` (193줄)

**책임**: 게임 사운드 재생 — 이벤트 구독 → AudioManager.PlayOneShot 호출
**전략**: `WrapWithModuleInterface` — 독립 모듈화 가능, 얇은 이벤트 리스너 패턴
**위험도**: Low

---

### MC-14: EndCard

**소스 파일**:
- `EndCard/EndCardController.cs` (95줄)

**책임**: 엔드카드 표시 — 승리 패널, CTA 버튼
**전략**: `AdaptExistingModule` — MODULE_REGISTRY의 EndCard에 대응
**위험도**: Low

---

### MC-15: Utils

**소스 파일**:
- `Utils/CoordinateConverter.cs` (148줄)
- `Utils/AutoBindHelper.cs` (123줄)

**책임**: 좌표 변환 유틸리티, 에디터 자동 바인딩
**전략**: `LeaveUnclassified` — Shared 유틸리티로 분류, 별도 모듈화 불필요
**위험도**: Low
**비고**: CoordinateConverter는 Shared로, AutoBindHelper는 Editor 전용.

---

## 전략 요약

| 전략 | 모듈 수 | 후보 |
|------|---------|------|
| `AdaptExistingModule` | 6 | MC-01, MC-03, MC-05, MC-07, MC-12, MC-14 |
| `WrapWithModuleInterface` | 4 | MC-02, MC-04, MC-06, MC-13 |
| `SplitIntoMultipleModules` | 1 | MC-09 |
| `ExtractRuntimeModule` | 1 | MC-08 |
| `ReuseAsIs` | 1 | MC-10 |
| `LeaveUnclassified` | 2 | MC-11, MC-15 |
