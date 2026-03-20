# CHECKLIST — 완료 판정 기준

작업 종료 전 반드시 이 체크리스트를 확인한다. 하나라도 미충족이면 완료가 아니다.

---

## 모듈 생성 시

```
[ ] 파일이 실제로 수정/생성되었는가
[ ] 6파일 구조 완비 (I, Config, Runtime, Factory, Bootstrap, Tests)
[ ] 네임스페이스 Game
[ ] Runtime: MonoBehaviour 미상속
[ ] Config: ScriptableObject 상속
[ ] Factory: static class
[ ] GC 유발 코드 없음 (foreach, 코루틴, 람다, LINQ, Invoke)
[ ] 매직넘버 없음 (모두 const UPPER_SNAKE_CASE)
[ ] GetComponent 런타임 호출 없음
[ ] null conditional 없음 (?. ??)
[ ] 테스트 최소 2개
[ ] TASK_QUEUE.yaml 엔트리 업데이트
[ ] MODULE_REGISTRY.yaml 등록 (신규 시)
[ ] FEATURE_QUEUE.yaml 업데이트 (해당 시)
[ ] YAML 간 의존성 일치
[ ] status: in_progress, human_state: pending 도달
[ ] 린터 에러 0건
```

## 기존 코드 수정 시

```
[ ] 파일이 실제로 수정되었는가
[ ] CODING_RULES 준수
[ ] 린터 에러 0건
[ ] 기존 인터페이스 하위 호환성 유지
```

## 커밋 시

```
[ ] 7 Gate 전부 통과
[ ] Validation Report Gate: AIValidationReport.json Passed == true
[ ] Human Gate: human_state == validated
[ ] Learning Gate: human_fixes > 0이면 learning_state == recorded
[ ] feature_group 전체 ready
[ ] 관련 파일만 스테이징 (YAML, learning 제외)
[ ] arch_diff_blocked != true
[ ] 커밋 메시지 규격 준수
```

## 실패 판정 (하나라도 해당 시 FAILURE)

- 수정된 파일 수 == 0
- 설명/분석/계획만 출력하고 파일 변경 없음
- 파이프라인 단계 누락 (GDD 입력 시)
- YAML 불일치
- 코딩 규칙 위반
- Human Gate 우회
