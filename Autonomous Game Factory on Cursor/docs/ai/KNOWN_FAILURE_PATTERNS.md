# Known Failure Patterns

> **이 파일은 레거시 참조용이다.**
> 구조화된 학습 시스템은 `learning/` 폴더로 이전되었다.
> 새 패턴 추가는 아래 파일에 기록한다:
>
> - `learning/VALIDATOR_FAILURE_PATTERNS.md` — 12 Validator별 실패 패턴 사전
> - `learning/RULE_MEMORY.yaml` — 기계 판독용 규칙 저장소
> - `learning/LEARNING_LOG.md` — 시간순 이벤트 로그
>
> 진입점: `learning/LEARNING_INDEX.md`

---

## 레거시 기록 (이전 시스템)

- ModuleStructureValidator initially failed due to YAML regex mismatch → `LL-0001`
- Test detection needed recursive search under Tests/ → `LL-0003`
- Registry/path mismatch caused false warnings before cleanup → `LL-0002`
