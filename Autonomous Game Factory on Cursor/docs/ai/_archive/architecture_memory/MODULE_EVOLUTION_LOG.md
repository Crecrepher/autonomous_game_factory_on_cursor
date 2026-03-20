# Module Evolution Log

모듈의 아키텍처 변경 이력을 시간순으로 기록한다.

---

## 기록 형식

```
### [날짜] <모듈명> — <변경 유형>

- **변경**: <what changed>
- **사유**: <why>
- **영향**: <affected modules>
- **위험**: low | medium | high
- **결정자**: AI | Human | Both
```

---

## 이력

### [2026-03-18] InventorySystem — 신규 생성

- **변경**: InventorySystem 모듈 6파일 생성
- **사유**: 슬롯 기반 인벤토리 기능 요청
- **영향**: ItemStacking (의존)
- **위험**: low
- **결정자**: AI

### [2026-03-18] ItemStacking — 신규 생성

- **변경**: ItemStacking 모듈 6파일 생성
- **사유**: 아이템 스택 관리 기능 요청 (InventorySystem 하위)
- **영향**: 없음 (독립)
- **위험**: low
- **결정자**: AI

---

*이 로그는 Committer가 feat 커밋 시 자동으로 추가한다.*
