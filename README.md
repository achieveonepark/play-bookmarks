# Play Bookmarks

게임을 특정 테스트 시작 조건으로 실행하는 독립 Unity 패키지입니다.

- 패키지: `com.achive.play-bookmarks`
- 런타임 네임스페이스/어셈블리: `Achieve.PlayBookmarks`
- 최소 Unity: 2022.3. 현재 검증 에디터: 6000.5.10f1.

## 빠른 시작

1. Package Manager에서 이 폴더의 `package.json`을 설치합니다.
2. **Tools → Achieve → Play Bookmarks → New bookmark** 또는 **Assets → Create → Achieve → Play Bookmark**로 `.pbm` 파일을 생성합니다.
3. **Start scene**을 지정합니다.
4. 대상 GameObject에 `BookmarkTarget`을 붙이고 고유 ID를 지정합니다.
5. **Add step**에서 이동·활성화·대기·게임 액션 단계를 추가합니다.
6. 인스펙터의 **Apply**로 파일을 저장하거나 **Revert**로 편집을 취소합니다. **Run bookmark**는 창의 변경 사항을 먼저 저장한 뒤 실행합니다.

실행 중에는 진행 상황과 단계별 결과가 표시됩니다. **Cancel setup**은 준비 작업을 취소합니다. 플레이 종료 시 기존 시작 씬 설정을 복구합니다.

`.pbm`은 버전이 있는 JSON 파일입니다. Project 창에서 선택하면 UI Toolkit 인스펙터에 시작 씬과 단계가 표시됩니다. Importer가 실행용 `BookmarkProfile`을 생성하므로 별도의 `.asset`을 만들 필요가 없습니다. 단계에는 일반 직렬화 데이터와 대상 ID를 사용합니다. Unity 오브젝트 직접 참조는 저장하지 않으며, 씬은 GUID와 경로를 함께 저장합니다.

창과 인스펙터의 **한국어 / English** 메뉴로 표시 언어를 선택합니다. 선택은 Event Tracer와 공유되며, 북마크 이름·메모·JSON 같은 저장 데이터는 변경하지 않습니다.

## 게임 초기화와 연동

비동기로 초기화하는 게임은 `BookmarkReadySignal`을 추가하고 북마크의 **Ready signal**에 동일한 ID를 지정합니다. 실제 초기화가 완료되면 `signal.MarkReady()`를 호출합니다. 간단한 씬에서는 `readyOnStart`를 사용할 수 있습니다.

체력·장비·퀘스트는 `IBookmarkAction` 구현 또는 `[Serializable] BookmarkStep` 서브클래스로 연결합니다. Action은 대상 GameObject에 붙은 컴포넌트에서 찾습니다.

```csharp
using Achieve.PlayBookmarks;

// 이미 실행 중인 씬에 북마크의 설정을 적용합니다.
BookmarkRunReport report = BookmarkRunner.Run(profile);
// BookmarkRunner.Run은 startScenePath를 로드하지 않습니다.
BookmarkRunner.Cancel();
```

`useRandomSeed`는 `UnityEngine.Random`의 시드를 설정합니다. 별도의 RNG는 게임에서 `BookmarkRunner.PendingProfile`을 읽어 초기화하세요. Unity의 다른 `BeforeSceneLoad` 콜백 간 순서는 보장되지 않으므로, 게임의 초기화 단계에서 명시적으로 적용하는 것이 안전합니다.

## 샘플

Package Manager에서 **Bookmark Demo**를 Import하고 **Tools → Achieve → Samples → Create Bookmark Demo**를 실행합니다. 생성된 **Boss Phase 2** 북마크를 실행하면 체력 10, FireSword, 보스 2페이즈, 위치 `(3, 0, 0)`이 적용됩니다.

## 동작 범위

북마크는 명시적으로 설정한 시작 조건을 적용합니다. 현재 게임 전체의 자동 저장/복원과 입력 재생은 포함하지 않습니다. 위치 이동은 Transform을 변경하므로 CharacterController·NavMeshAgent·물리 기반 이동은 게임 액션에서 해당 시스템의 이동 API를 호출하세요.

게임 코루틴이 별도로 시작한 백그라운드 작업은 게임 코드가 취소를 처리해야 합니다. 단계는 중첩 `IEnumerator`를 반환하여 실행기가 생명주기를 관리하도록 작성하세요. 동일 스레드의 무한 루프나 오래 걸리는 동기 메서드는 코루틴 감시기로 중단할 수 없습니다.

[단계 확장과 추적기 연결 예제](Documentation~/play-bookmarks.md)

## Codex 스킬

이 저장소의 `.agents/skills/achieve-play-bookmarks`에 구현·연동 스킬이 포함되어 있습니다. 관련 작업에서 자동으로 선택하도록 설정되어 있으며, `$achieve-play-bookmarks`로 직접 호출할 수도 있습니다. `AGENTS.md`도 패키지 작업을 이 스킬로 연결합니다.
