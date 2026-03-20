# Migration Plan — MatchClash Playable002

**분석 일시**: 2026-03-19
**전략**: 어댑터 우선, 단계적 마이그레이션

---

## 마이그레이션 원칙

1. **원본 코드 무수정** — `Assets/Supercent/` 아래 파일은 절대 변경하지 않음
2. **새 모듈은 `Assets/Game/Modules/`에 생성** — AGF 6파일 구조
3. **어댑터 패턴** — 기존 클래스를 AGF 인터페이스로 래핑
4. **점진적 전환** — 한 번에 하나의 모듈만 전환
5. **Human Validation 필수** — 각 모듈 전환 후 검증

---

## 마이그레이션 유형별 가이드

### Type A: Pure C# → AGF Runtime (래핑)

**대상**: MC-03, MC-05, MC-08
**방법**:
1. AGF 6파일 생성 (I<Module>, Config, Runtime, Factory, Bootstrap, Tests)
2. Runtime이 기존 Pure C# 클래스를 내부적으로 사용
3. Config에서 기존 GameBalanceConfig의 해당 설정을 미러링
4. 기존 코드는 건드리지 않음

**예시** (BattleCharacter):
```
기존: CharacterController + CharacterModel + SkillGauge
  ↓ 래핑
AGF: IBattleCharacter → BattleCharacterRuntime (내부에서 CharacterController 사용)
     BattleCharacterConfig (GameBalanceConfig에서 캐릭터 관련 설정 추출)
     BattleCharacterFactory.Create(config)
     BattleCharacterBootstrap (씬 진입점)
```

### Type B: MonoBehaviour → AGF View+Bootstrap 분리

**대상**: MC-02, MC-04, MC-06, MC-12
**방법**:
1. 기존 MonoBehaviour는 그대로 유지
2. AGF Bootstrap이 기존 MonoBehaviour를 SerializeField로 참조
3. AGF Runtime이 비즈니스 로직 담당, 기존 View에 위임
4. 씬 구조 변경 최소화

### Type C: ScriptableObject → AGF Config (재사용)

**대상**: MC-10
**방법**:
1. 기존 GameBalanceConfig를 AGF Config로 그대로 사용
2. 추가 메타데이터만 AGF Config에 추가
3. 기존 참조 유지

### Type D: Static Class → 유지 (전환 보류)

**대상**: MC-11 (GameEvents)
**방법**:
1. 현 단계에서는 유지
2. 향후 모듈 간 직접 이벤트로 점진 전환
3. 전환 시 이벤트별로 하나씩 제거

### Type E: GodClass → 분할 (대규모, 추후)

**대상**: MC-09 (GameFlowController), MC-07 (CombatVFX)
**방법**:
1. 현 단계에서는 유지
2. 기획 변경 시 해당 영역만 부분 분할
3. 분할 시 어댑터로 기존 인터페이스 유지

---

## 마이그레이션 완료 조건 (엄격)

- [ ] AGF 6파일 구조 완성
- [ ] 테스트 최소 2개 통과
- [ ] MODULE_REGISTRY 등록
- [ ] 기존 코드 무수정 확인
- [ ] Human Validation 완료
- [ ] TASK_QUEUE `human_state: validated`

---

## 위험 관리

| 위험 | 완화 방안 |
|------|---------|
| 기존 씬 참조 깨짐 | 어댑터가 기존 참조를 유지 |
| GameEvents 의존성 | 단계적 제거, 초기에는 유지 |
| GodClass 분할 시 버그 | 분할 전 테스트 작성, 분할 후 검증 |
| 이중 코드 유지 비용 | Playable Ad 특성상 수명이 짧아 허용 가능 |
