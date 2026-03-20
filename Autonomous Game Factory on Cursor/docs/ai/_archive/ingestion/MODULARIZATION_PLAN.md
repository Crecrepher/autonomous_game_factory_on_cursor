# Modularization Plan — MatchClash Playable002

**분석 일시**: 2026-03-19

---

## 핵심 판단: Playable Ad 특수성

이 코드베이스는 **Playable Ad** (모바일 광고용 미니 게임)이다.
- 용량 제한: 2~5MB
- 단일 씬 구조
- 짧은 플레이 타임 (30초~1분)
- 재사용성보다 **빠른 변경 대응**이 핵심

따라서 모듈화의 목표는:
1. **기획 변경에 대응 가능한 구조** — 기능별 격리
2. **기존 코드 파괴 없이 점진적 전환** — 어댑터 우선
3. **과도한 추상화 회피** — Playable Ad는 가벼워야 함

---

## Phase 1: 즉시 모듈화 가능 (Low Risk)

이미 AGF 패턴에 가까운 Pure C# 코드. 인터페이스 래핑만으로 모듈화 가능.

| 후보 | 대상 Registry 모듈 | 전략 | 예상 작업량 |
|------|--------------------|------|-----------|
| MC-03: BattleCharacter | BattleCharacter | AdaptExistingModule | 소 |
| MC-05: BattleMonster | BattleMonster | AdaptExistingModule | 소 |
| MC-08: SkillSystem | (신규 또는 BattleCharacter 통합) | ExtractRuntimeModule | 소 |
| MC-10: GameConfig | (Shared Config) | ReuseAsIs | 최소 |
| MC-14: EndCard | EndCard | AdaptExistingModule | 최소 |
| MC-13: GameSound | (신규) | WrapWithModuleInterface | 소 |

---

## Phase 2: 중간 작업 필요 (Medium Risk)

로직은 분리되어 있으나 View 코드가 크거나 이벤트 의존이 깊음.

| 후보 | 대상 Registry 모듈 | 전략 | 예상 작업량 |
|------|--------------------|------|-----------|
| MC-01: PuzzleCore | PuzzleBlock + PuzzleBoard + SpecialBlock | AdaptExistingModule | 중 |
| MC-12: InputGuide | Guide | AdaptExistingModule | 중 |
| MC-11: GameEvents | (Shared 또는 유지) | LeaveUnclassified | 유지 |

---

## Phase 3: 대규모 리팩토링 필요 (High Risk)

GodClass 분할, View-Logic 분리가 필요. **기획 변경과 동시에 진행하면 위험**.

| 후보 | 전략 | 위험 요인 | 권장 접근 |
|------|------|----------|---------|
| MC-09: GameFlow (1,032줄) | SplitIntoMultipleModules | 모든 컨트롤러의 허브, 변경 시 전체 흐름 영향 | 어댑터로 래핑 후 점진 분할 |
| MC-02: PuzzleView (1,714줄) | WrapWithModuleInterface | 로직+연출 혼재, 블록 풀링/애니메이션 밀결합 | View 내부 리팩토링 우선, 인터페이스 분리는 2차 |
| MC-07: BattleEffect (1,428줄) | AdaptExistingModule | CombatVFX GodClass, 투사체/풀/이벤트 전부 담당 | 투사체/풀/이벤트를 서브 컴포넌트로 분리 |
| MC-04/06: Views | WrapWithModuleInterface | View 분리 시 씬 참조 재배선 필요 | 어댑터 패턴으로 기존 참조 유지 |

---

## 권장 실행 순서

```
1. MC-10: GameConfig     — 그대로 유지 (ReuseAsIs)
2. MC-11: GameEvents     — 그대로 유지 (LeaveUnclassified)
3. MC-03: BattleCharacter — Pure C# 래핑
4. MC-05: BattleMonster   — Pure C# 래핑
5. MC-08: SkillSystem     — Pure C# 래핑
6. MC-14: EndCard         — 간단 래핑
7. MC-13: GameSound       — 간단 래핑
8. MC-01: PuzzleCore      — Controller/Model 래핑 (View 분리 안 함)
9. MC-12: InputGuide      — 어댑터 래핑
10. MC-02: PuzzleView     — 추후 리팩토링
11. MC-04: BattleCharacterView — 추후 리팩토링
12. MC-06: BattleMonsterView   — 추후 리팩토링
13. MC-07: BattleEffect   — 추후 리팩토링
14. MC-09: GameFlow       — 추후 리팩토링
```

---

## 절대 금지 사항

1. **원본 코드 수정 금지** — 모든 모듈화는 새 파일 생성 + 어댑터
2. **원본 코드 삭제 금지** — 기존 동작 보존
3. **마이그레이션 완료 주장 금지** — 모든 엔트리 `status: pending`
4. **대량 리팩토링 금지** — 어댑터 우선, 점진적 전환
