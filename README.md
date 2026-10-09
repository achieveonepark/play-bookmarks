# Play Bookmarks

씬·위치·체력 같은 테스트 시작 조건을 `.pbm` 파일로 저장하고 실행하는 Unity 패키지입니다.

## 설치

Unity **2022.3 이상**에서 Package Manager → **+ → Add package from git URL**:

```text
https://github.com/achieveonepark/play-bookmarks.git
```

## 사용

1. **Tools → Achieve → Play Bookmarks**에서 새 북마크를 만듭니다.
2. 시작 씬을 지정하고 대상 GameObject에 `BookmarkTarget`과 고유 ID를 설정합니다.
3. 이동·활성화·대기·게임 액션 단계를 추가하고 **Apply → Run bookmark**를 누릅니다.

창과 파일 인스펙터에서 **한국어 / English**를 선택할 수 있습니다.

샘플: Package Manager의 **Bookmark Demo**를 Import → **Tools → Achieve → Samples → Create Bookmark Demo**.

자세한 연동과 단계 확장은 [사용 가이드](Documentation~/play-bookmarks.md)를 참고하세요.

## AI 스킬

저장소에 Codex용 [`achieve-play-bookmarks`](.agents/skills/achieve-play-bookmarks/SKILL.md)와 Claude Code용 [동일 스킬](.claude/skills/achieve-play-bookmarks/SKILL.md)이 포함되어 있습니다. 직접 호출: Codex `$achieve-play-bookmarks`, Claude Code `/achieve-play-bookmarks`.
