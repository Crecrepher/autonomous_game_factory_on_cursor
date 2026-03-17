# 설치 가이드

## 이 저장소는 무엇인가

**AI Game Dev Framework** 소스 저장소.
Unity 프로젝트와 동일한 폴더 구조를 사용하므로, 그대로 복사하면 바로 동작한다.

> 이 저장소를 직접 Unity에서 열어 게임을 만들지 마세요.
> 실제 Unity 프로젝트에 복사해서 사용합니다.

---

## 필수 환경

| 항목 | 최소 버전 |
|------|-----------|
| **Unity** | 2021.3 LTS 이상 |
| **Cursor IDE** | 최신 안정 버전 |

---

## 빠른 시작

```bash
# 1. 클론
git clone <이 저장소 URL>

# 2. 프로젝트에 복사
cp -r ai-dev-framework/* MyGameProject/

# 3. .example 파일 이름 변경
cd MyGameProject/Docs/ai/
mv PROJECT_OVERVIEW.example.md PROJECT_OVERVIEW.md
mv CODING_RULES.example.md CODING_RULES.md
mv MODULE_REGISTRY.example.yaml MODULE_REGISTRY.yaml
mv AI_DEVELOPMENT_LOOP.example.md AI_DEVELOPMENT_LOOP.md

# 4. 내용을 프로젝트에 맞게 수정
# 5. Unity에서 Tools > AI > Validate Generated Modules 실행
```

---

## 다음 단계

- [HOW_TO_APPLY_TO_PROJECT.md](HOW_TO_APPLY_TO_PROJECT.md) — 상세 적용 방법
- [HOW_TO_EVOLVE_RULES.md](HOW_TO_EVOLVE_RULES.md) — 규칙 진화 방법
- [FRAMEWORK_OVERVIEW.md](FRAMEWORK_OVERVIEW.md) — 프레임워크 구조
