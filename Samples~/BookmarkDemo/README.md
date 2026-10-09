# Bookmark Demo

Package Manager에서 **Bookmark Demo**를 Import한 뒤 **Tools → Achieve → Samples → Create Bookmark Demo**를 실행합니다.

새 폴더에 씬과 `Boss Phase 2` 북마크를 생성합니다. 열린 Play Bookmarks 창에서 **Run bookmark**를 누르면 위치 `(3, 0, 0)`, 체력 `10`, 무기 `FireSword`, 보스 페이즈 `2`가 적용됩니다.

`BookmarkDemoState`는 게임별 `IBookmarkAction` 연동의 최소 예제입니다. 실제 게임에서는 `state` 대입 대신 체력·장비·보스 시스템의 공개 API를 호출하세요.
