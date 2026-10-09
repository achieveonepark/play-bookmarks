---
name: achieve-play-bookmarks
description: Implement, extend, integrate, or debug Achieve Play Bookmarks (com.achive.play-bookmarks) in Unity. Use for 플레이 북마크, .pbm files, reusable test starting conditions, BookmarkRunner, setup steps, scene launching, and ready-signal integration, including related requests that do not explicitly name this skill. Excludes browser bookmarks and general save-game systems.
---

# Achieve Play Bookmarks

Unity 게임의 테스트 시작 조건을 구현하거나 이 패키지 자체를 수정할 때 사용한다.

## 패키지 찾기

- 패키지 ID는 `com.achive.play-bookmarks`, 네임스페이스와 런타임 어셈블리는 `Achieve.PlayBookmarks`다. `achive` 철자를 유지한다.
- 현재 프로젝트의 `Packages/manifest.json`, 설치된 패키지, asmdef를 먼저 확인한다. 알려진 원본은 `C:/Users/parka/orca/projects/ts1/unity-bookmarks`다. 설치된 버전의 실제 API를 읽고 작업한다.
- 파일 형식·에디터 또는 사용자 단계를 변경할 때 [구현 가이드](references/package-guide.md)를 읽는다. 기본 사용법은 패키지의 `README.md`, 자세한 연동은 `Documentation~/play-bookmarks.md`에 있다.

## 구현 기준

- 게임별 시작 조건은 `IBookmarkAction` 또는 `[Serializable] BookmarkStep`으로 연결한다. 대상 컴포넌트와 `BookmarkTarget`은 같은 GameObject에 두고, 로드된 씬 전체에서 대상 ID와 준비 신호 ID가 각각 고유하도록 한다.
- 비동기 게임 초기화는 `BookmarkReadySignal.MarkReady()` 뒤에 단계를 실행한다. 플레이 중 `BookmarkRunner.Run(profile)`은 현재 씬에만 적용한다. 시작 씬 로드는 에디터 실행기의 역할이다.
- 기본 북마크 파일은 `.pbm`이다. `BookmarkFileCodec`과 Importer를 사용하고 별도의 `.asset` 생성 흐름으로 되돌리지 않는다. 인스펙터 편집은 Importer의 extra data에 바인딩하고 Apply/Revert가 원본 파일에 반영되도록 한다.
- 단계는 순차 실행과 unscaled 제한 시간·취소를 유지한다. 관리할 하위 작업은 중첩 `IEnumerator`로 반환한다. 별도의 `StartCoroutine`이나 오래 걸리는 동기 호출을 실행기가 중단할 수 있다고 가정하지 않는다.
- 에디터 창과 파일 인스펙터는 UI Toolkit으로 작성하고 Unity의 회색 톤과 기본 컨트롤을 사용한다. `BookmarkLocalization`의 한국어/English 선택과 `Achieve.Editor.Language` 설정을 유지한다. 동적 상태 문구는 `SetText`로 갱신하고 메모·ID·JSON 등 데이터는 번역하지 않는다.
- 패키지는 독립성을 유지한다. Event Tracer나 Masked Values와의 연동은 소비 프로젝트의 게임 코드 또는 선택적 샘플에 둔다. 함께 구현할 때는 해당 패키지 스킬도 적용한다.

## 확인

파일 변경은 `.pbm` 왕복 저장, 누락된 단계 보존, 씬 GUID, Apply/Revert를 확인한다. 실행 변경은 준비 신호, 순서, 시간 초과와 취소 중 영향을 받는 동작을 확인한다. 에디터 수정은 실제 창에서 레이아웃과 두 언어를 확인하고 기존 `.meta` GUID를 보존한다.

로컬 검증 프로젝트는 `C:/Users/parka/orca/projects/ts1/validation/UnityProject`다. 테스트나 샘플 생성 전 실제 대상 프로젝트를 확인한다. 외부 Unity 프로젝트의 패키지 경로를 자동으로 이 고정 경로로 바꾸지는 않는다.
