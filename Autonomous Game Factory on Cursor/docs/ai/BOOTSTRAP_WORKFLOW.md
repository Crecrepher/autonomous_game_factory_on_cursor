# Game Factory Bootstrap Workflow

> Version: 1.0
> Location: `Assets/Editor/GameFactory/`
> Entry: `Tools/AI/Game Factory Bootstrap`

## 목적

디자인 문서 하나로 AI 게임 개발 파이프라인의 **안전한 출발점**을 생성한다.
풀 게임을 자동 생성하는 것이 아니라, Human-in-the-Loop 파이프라인이 시작할 수 있는 **구조적 토대**를 만든다.

## 워크플로우

```
디자인 문서 입력
  ↓
IntelligentDecomposer — 시스템/모듈 추론
  ↓
Dry Run — 프리뷰 (파일 변경 없음)
  ↓
사용자 확인
  ↓
Apply — 실제 생성
  ↓
  ├── 폴더 구조 생성
  ├── 씬 생성 (Bootstrap/Gameplay)
  ├── 프리팹 플레이스홀더 생성
  ├── 모듈 폴더 + Tests/Editor 생성
  ├── ScriptableObject 설정 폴더 생성
  ├── FEATURE_QUEUE 초기화
  ├── TASK_QUEUE 초기화
  ├── MODULE_REGISTRY 초기화
  ├── SPEC/PLAN 문서 생성
  └── BOOTSTRAP_REPORT 생성
  ↓
Human-in-the-Loop 파이프라인 시작
```

## 생성되는 Unity 구조

```
Assets/
  Game/
    Modules/            # 모듈 코드 (기존 + 신규)
    Scenes/             # Bootstrap/Gameplay 씬
    Prefabs/            # 구조적 플레이스홀더
      Environment/
      UI/
      Gameplay/
      Characters/
    ScriptableObjects/  # Config 에셋 폴더
      Configs/<Module>/
    Bootstrap/          # 부트스트랩 진입점
    Art/                # 아트 에셋
    Audio/              # 오디오 에셋
    Data/               # 데이터 에셋
    Shared/             # 공유 인터페이스/타입
```

## 씬 구조

### BootstrapScene
```
GameFactoryBootstrapRoot
SystemsRoot
UIRoot
GameplayRoot
EnvironmentRoot
```

### GameplayScene
```
GameFactoryBootstrapRoot
SystemsRoot
UIRoot
GameplayRoot
EnvironmentRoot
```

## 프리팹 플레이스홀더

| 프리팹 | 위치 | 용도 |
|---|---|---|
| UIRoot | Prefabs/UI/ | UI 계층 루트 |
| PlayerRoot | Prefabs/Characters/ | 플레이어 구조 루트 |
| WorldRoot | Prefabs/Environment/ | 월드/환경 루트 |
| SystemsRoot | Prefabs/Gameplay/ | 시스템 오브젝트 루트 |

## 재사용/덮어쓰기 보호

| 상황 | 동작 |
|---|---|
| 폴더가 이미 존재 | Reuse (건너뜀) |
| 씬이 이미 존재 | Reuse (건너뜀) |
| 프리팹이 이미 존재 | Reuse (건너뜀) |
| 유사 프리팹이 다른 위치에 존재 | Skip + Warning |
| 모듈이 이미 존재 (Registry 기반) | Reuse + 안내 |
| TASK_QUEUE 엔트리가 이미 존재 | Skip + Warning |
| FEATURE_QUEUE 엔트리가 이미 존재 | Skip + Warning |
| MODULE_REGISTRY 엔트리가 이미 존재 | Skip + Reuse count |

## 파이프라인 큐 초기화

### FEATURE_QUEUE
- status: `queued` (done이 아님)
- 각 추론된 feature group마다 1개 엔트리

### TASK_QUEUE
- status: `pending` (done이 아님)
- human_state: `none`
- commit_state: `none`
- learning_state: `none`
- integration_strategy: `create_new` 또는 `reuse_or_extend`
- decomposition_source: `bootstrap_engine`

### MODULE_REGISTRY
- editable: `true`
- risk: `low`
- 기존 엔트리는 절대 수정하지 않고 append만 함

## Dry Run vs Apply

| 모드 | 동작 |
|---|---|
| Dry Run | 모든 검사 수행, 결과 프리뷰 표시, 파일 변경 없음 |
| Apply | 실제 폴더/씬/프리팹/큐 생성, 확인 다이얼로그 필수 |

## EditorWindow UI

1. **Design Input** — 프로젝트명, 디자인 문서, 피처 노트, 장르 힌트
2. **Bootstrap Options** — 씬/프리팹/모듈/큐 생성 토글
3. **Decomposition Preview** — 추론된 시스템, 모듈, 기존 모듈 재사용 여부
4. **Execute** — Dry Run / Apply 버튼
5. **Result** — 액션 로그, 경고, 다음 단계 안내

## Control Panel 통합

`GameFactoryControlPanel` (Tools/AI/Game Factory Control Panel)의 CPIL 섹션에
"Open Bootstrap Window (Full Setup)" 버튼이 추가되어 Bootstrap Window를 바로 열 수 있다.

## 안전 규칙

- 어떤 항목도 `done`으로 표시하지 않음
- 어떤 항목도 자동 커밋하지 않음
- 어떤 기존 파일도 덮어쓰지 않음
- Validator를 우회하지 않음
- Human validation gate를 건너뛰지 않음
- 거짓 완료 보고 금지

## 첫 번째 테스트 시나리오

1. `Tools/AI/Game Factory Bootstrap` 메뉴 클릭
2. Project Name: "TestGame"
3. Design Document: "RPG 게임. 골드와 젬 재화. 상점에서 아이템 구매. 인벤토리 관리. 전투와 레벨업."
4. "Analyze Design Document" 클릭 → 추론 결과 확인
5. "Dry Run" 클릭 → 생성될 내용 확인 (파일 변경 없음)
6. 결과 확인 후 "Apply" 클릭 → 실제 생성
7. `docs/ai/bootstrap/BOOTSTRAP_REPORT.md` 확인
8. `docs/ai/generated_specs/` 확인
9. Game Factory Control Panel → Run Validators
