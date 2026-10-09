# Play Bookmarks 연동 가이드

## 구조

`BookmarkProfile` → `BookmarkRunner` → `BookmarkStep.Execute(context)` 순서로 실행합니다. 시작 시 프로필을 복제하여 실행 단계가 원본 에셋을 변경하지 않도록 합니다.

에디터 실행 요청은 `SessionState`에 보관합니다. Play 모드의 씬 `Awake` 전에 프로필과 Unity 랜덤 시드를 준비하고, 씬 로드 후 선택한 준비 신호를 기다립니다. 등록된 단계는 순차적으로 실행하며, 준비 신호와 각 단계의 제한 시간은 unscaled real time을 사용합니다.

| 기본 단계 | 역할 |
| --- | --- |
| `MoveTargetStep` | ID로 찾은 대상의 위치·회전 변경 |
| `SetActiveStep` | 비활성 대상을 포함한 GameObject 활성화/비활성화 |
| `WaitStep` | unscaled 시간 대기 |
| `WaitForTargetStep` | 동적으로 생성될 대상 대기 |
| `InvokeActionStep` | 대상의 `IBookmarkAction`을 찾아 JSON 인자 전달 |

대상 ID와 준비 신호 ID는 로드된 모든 씬에서 각각 고유해야 합니다. 중복은 오류로 보고합니다.

## 게임 액션

```csharp
using System;
using System.Collections;
using Achieve.PlayBookmarks;
using UnityEngine;

public sealed class PlayerBookmarkActions : MonoBehaviour, IBookmarkAction
{
    [Serializable] private sealed class Args { public int health; }
    public string ActionId => "SetHealth";
    public int Health { get; private set; }

    public IEnumerator ExecuteBookmarkAction(BookmarkContext context, string payloadJson)
    {
        var args = JsonUtility.FromJson<Args>(payloadJson);
        Health = args.health; // 실제 게임에서는 체력 시스템 API 호출
        yield break;
    }
}
```

같은 GameObject에 `BookmarkTarget`과 이 컴포넌트를 붙입니다. `InvokeActionStep`의 target ID, action ID `SetHealth`, payload `{"health":10}`을 지정합니다.

## 사용자 단계

```csharp
using System;
using System.Collections;
using Achieve.PlayBookmarks;

[Serializable]
public sealed class WaitForPlayerStep : BookmarkStep
{
    public string targetId = "player";
    public override string DisplayName => "Wait for player";
    public override string Validate() => string.IsNullOrWhiteSpace(targetId) ? "Target ID required." : null;
    public override IEnumerator Execute(BookmarkContext context)
    {
        while (!context.TryGetTarget(targetId, out _)) yield return null;
    }
}
```

`[Serializable]`이며 인자 없는 생성자를 가진 공개 단계는 **Add step** 메뉴에 자동으로 표시됩니다. `.pbm`은 단계의 전체 타입 이름과 어셈블리 이름, 단계별 JSON을 저장합니다. 타입 이름이나 어셈블리를 바꿀 때는 파일의 `type` 키를 마이그레이션하세요. 찾을 수 없는 단계는 `MissingBookmarkStep`으로 원본 JSON을 보존하며 실행을 막습니다. `MovedFrom`만으로 이 파일 형식의 명시적 타입 키를 변경하지는 않습니다.

인스펙터는 Importer의 편집용 데이터를 사용합니다. **Apply**가 `.pbm` 원본에 기록하고 재임포트하며, **Revert**는 마지막 저장 상태를 복원합니다. 실행에 필요한 값이 아직 비어 있어도 작업 중인 파일을 저장할 수 있습니다. 파일에는 최대 256개 단계와 1M문자를 허용합니다. 사용자 단계에 Unity 오브젝트 필드를 저장해야 한다면 대상 ID나 에셋 GUID 문자열로 변환하세요.

중첩 코루틴은 `yield return NestedRoutine()`으로 반환합니다. 실행기는 중첩 예외를 실패 결과에 기록하고, 취소/제한 시간 초과 시 `IDisposable.Dispose`를 호출하여 iterator의 `finally`를 실행합니다. `yield return StartCoroutine(...)`처럼 별도로 실행한 작업은 실행기의 정리 범위 밖입니다.

## Event Tracer와 선택적으로 연결

두 패키지를 설치한 프로젝트의 게임 어셈블리에 아래 코드를 넣습니다. 해당 asmdef에서 `Achieve.PlayBookmarks`, `Achieve.EventTracer`를 참조합니다. 패키지 자체에는 상호 참조가 없습니다.

```csharp
using Achieve.PlayBookmarks;
using Achieve.EventTracer;
using UnityEngine;

public static class BookmarkTraceBridge
{
    private static TraceId setup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        BookmarkRunner.RunStarted -= OnStarted;
        BookmarkRunner.RunCompleted -= OnCompleted;
        BookmarkRunner.RunStarted += OnStarted;
        BookmarkRunner.RunCompleted += OnCompleted;
    }

    private static void OnStarted(BookmarkRunReport report)
    {
        var profile = BookmarkRunner.PendingProfile;
        EventTrace.StartSession(report.bookmarkName, new
        {
            report.bookmarkId,
            report.runId,
            settings = profile != null ? JsonUtility.ToJson(profile) : ""
        });
        setup = EventTrace.Record("Bookmark.SetupStarted");
    }

    private static void OnCompleted(BookmarkRunReport report)
    {
        EventTrace.Record("Bookmark.SetupCompleted",
            new { report.status, report.message, report.elapsedSeconds }, parentId: setup);
    }
}
```

실행 요청마다 새 추적 세션이 시작되고 북마크 ID와 실행 시 설정이 세션 메타데이터에 보관됩니다. 메타데이터를 이용한 자동 재실행 UI는 현재 버전에 포함하지 않습니다. 북마크 창에서 동일 프리셋을 다시 실행할 수 있습니다.

## 테스트

소비 프로젝트의 `Packages/manifest.json`에 `"testables": ["com.achive.play-bookmarks"]`를 추가하면 패키지 테스트가 나타납니다. 테스트 프레임워크는 소비 프로젝트에서 설치합니다. 이 패키지는 테스트 프레임워크를 런타임 의존성으로 요구하지 않습니다.
