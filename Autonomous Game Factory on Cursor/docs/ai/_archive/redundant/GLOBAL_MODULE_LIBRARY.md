# Global Module Library — CPIL

프로젝트 간 재사용 가능한 검증된 모듈 카탈로그.

---

## 1. 디렉토리 구조

```
GlobalModules/
├── GLOBAL_MODULE_CATALOG.yaml    # 카탈로그 메타데이터
├── Economy/                       # 재화/상점 모듈 코드
├── Combat/                        # 전투 모듈 코드
├── Inventory/                     # 인벤토리 모듈 코드
├── Farming/                       # 농사 모듈 코드
├── Progression/                   # 진행 모듈 코드
├── Quest/                         # 퀘스트 모듈 코드
├── Audio/                         # 오디오 모듈 코드
└── Save/                          # 저장 모듈 코드
```

---

## 2. 모듈 엔트리 스키마

```yaml
- name: <PascalCase 모듈명>
  category: <Economy | Combat | Inventory | Farming | ...>
  source_project: <원본 프로젝트 이름>
  stability: <high | medium | low>
  description: "<한 줄 설명>"
  dependencies: [<의존 모듈>]
  interface_methods:
    - "<메서드 시그니처>"
  architecture_pattern: SixFileModuleStructure
  reused_in_projects: [<프로젝트 목록>]
  usage_notes: "<사용 시 참고사항>"
```

---

## 3. 안정성 등급

| 등급 | 기준 | 재사용 권장 |
|------|------|-------------|
| `high` | 2+ 프로젝트에서 사용, Validator 통과 | 즉시 재사용 가능 |
| `medium` | 1 프로젝트에서 사용, Validator 통과 | 어댑터 필요 가능 |
| `low` | 템플릿 또는 미검증 | 참고용, 수정 필요 |

---

## 4. 내보내기 (Export)

```
프로젝트에서 안정적인 모듈 → GlobalModules/<ModuleName>/
```

조건:
- 6파일 구조 완전 (Runtime, Interface, Config, Factory)
- Validator 통과
- 순환 의존 없음

```csharp
GlobalModuleLibrary.ExportModule("InventorySystem", "luna_lumberchopper");
```

---

## 5. 가져오기 (Import)

```
GlobalModules/<ModuleName>/ → Assets/Game/Modules/<ModuleName>/
```

조건:
- 로컬에 동일 모듈이 없음
- MODULE_REGISTRY에 등록 필요

```csharp
GlobalModuleLibrary.ImportModule("CurrencyWallet");
```

---

## 6. 검색

```csharp
LibrarySearchResult[] results = GlobalModuleLibrary.Search("inventory item slot", null);
```

카테고리 필터:
```csharp
LibrarySearchResult[] results = GlobalModuleLibrary.Search("wallet", "Economy");
```

---

## 7. 초기 카탈로그

| 모듈 | 카테고리 | 안정성 | 원본 |
|------|----------|--------|------|
| CurrencyWallet | Economy | high | luna_lumberchopper |
| InventorySystem | Inventory | high | luna_lumberchopper |
| ItemStacking | Inventory | high | luna_lumberchopper |
| HealthSystem | Combat | medium | template |
| DamageCalculator | Combat | medium | template |
| CropGrowth | Farming | low | template |
