# Play Bookmarks 구현 가이드

## 소스 위치와 파일 형식

패키지 루트 아래에서 다음 파일을 읽는다.

| 작업 | 관련 소스 |
| --- | --- |
| 실행·준비·결과 | `Runtime/Core/BookmarkRunner.cs`, `BookmarkExecutionHost.cs`, `BookmarkContext.cs`, `BookmarkRunReport.cs` |
| 대상과 게임 초기화 | `Runtime/Components/BookmarkTarget.cs`, `BookmarkReadySignal.cs` |
| 사용자 단계 | `Runtime/Core/BookmarkStep.cs`, `Runtime/Steps/` |
| 파일 저장·임포트 | `Editor/Importers/BookmarkFileCodec.cs`, `BookmarkFileImporter.cs` |
| 편집·실행 UI | `Editor/Inspectors/`, `Editor/Windows/BookmarkWindow.cs`, `Editor/Services/BookmarkPlayModeLauncher.cs` |
| UI 언어·스타일 | `Editor/Services/BookmarkLocalization.cs`, `Editor/UI/Styles/PlayBookmarks.uss` |

`.pbm` formatVersion 1은 씬 GUID와 경로, 시드, 준비 신호, 단계 목록을 저장한다. 각 단계의 `type`은 `Full.Type.Name, AssemblyName`이며 `json`에 일반 직렬화 데이터를 넣는다. Unity 오브젝트 직접 참조 대신 대상 ID 또는 에셋 GUID 문자열을 쓴다. 현재 제한은 256개 단계와 1M문자다.

타입이 없으면 `MissingBookmarkStep`이 원래 타입 키와 JSON을 보존한다. 복구 전 실행을 막고 데이터를 삭제하지 않는다. 타입/어셈블리 이름 변경 시 파일의 키를 마이그레이션한다. `MovedFrom`만으로 이 명시적 키가 변경되지는 않는다.

## 게임별 액션

```csharp
using System;
using System.Collections;
using Achieve.PlayBookmarks;
using UnityEngine;

public sealed class PlayerSetupAction : MonoBehaviour, IBookmarkAction
{
    [Serializable] private sealed class Args { public int health; }
    public string ActionId => "SetHealth";
    public int Health { get; private set; }

    public IEnumerator ExecuteBookmarkAction(BookmarkContext context, string payloadJson)
    {
        var args = JsonUtility.FromJson<Args>(payloadJson);
        if (args == null || args.health < 0) throw new ArgumentException("Invalid health.");
        Health = args.health; // 실제 게임의 상태 변경 API로 교체한다.
        yield break;
    }
}
```

이 컴포넌트와 `BookmarkTarget`을 같은 오브젝트에 붙인다. `InvokeActionStep`에 대상 ID, `SetHealth`, `{"health":10}`을 지정한다. 대상 하나에서 같은 `ActionId`를 여러 컴포넌트가 사용하지 않도록 한다.

사용자 단계는 `[Serializable]`, 공개 타입, 인자 없는 생성자와 `Execute(BookmarkContext)`를 제공한다. `Validate()`는 잘못된 설정에 메시지를 반환한다. 기본 단계는 이동, 활성 상태 변경, 시간 대기, 대상 생성 대기, 게임 액션 호출이다.

## 실행 생명주기

에디터 실행기는 시작 씬 설정과 `SessionState` 요청을 관리하고 Play 진입 시 씬 Awake 전에 `Prepare`, 씬 로드 후 `StartPrepared`를 호출한다. `Prepare`는 프로필을 복제하여 실행 중 원본을 변경하지 않는다. 플레이 종료 시 이전 시작 씬 설정을 복원한다.

`UnityEngine.Random` 시드는 준비 단계에서 적용된다. 별도 RNG는 게임 초기화 코드가 직접 설정한다. 다른 `BeforeSceneLoad` 콜백과의 호출 순서를 보장한다고 가정하지 않는다. 복제한 프로필은 준비 시작 이벤트에서 `PendingProfile`로 읽을 수 있다.

하위 코루틴을 `yield return NestedRoutine()`으로 반환하면 실행기가 예외, 취소와 `Dispose/finally`를 관리한다. 물리·NavMesh 이동은 Transform 이동 대신 게임 액션에서 해당 이동 시스템의 API를 호출한다.

## 에디터와 테스트

`BookmarkProfileEditor.CreateEditorUI(SerializedObject)`로 프로필과 Importer 편집 데이터를 같은 UI에 바인딩한다. 파일 인스펙터는 native Apply/Revert와 extra-data Undo/저장 상태를 유지한다. 실행 직전에 파일을 저장하고 재임포트된 프로필을 사용한다.

UI 변경은 작은 사이드바에서도 검색창이 넘치지 않는지, 선택·편집 데이터가 언어 변경 후 유지되는지 확인한다. Unity 버전에 따라 실제 설치된 API와 기존 버전 조건부 분기를 확인한다.

기존 검증은 `Tests/Editor`, `Tests/Runtime`에 있다. 세 패키지 연동의 실제 게임 코드는 알려진 원본 루트의 `Samples/AchieveIntegration/Runtime/IntegrationDemoPlayer.cs`, `IntegrationTraceBridge.cs`를 필요할 때 읽는다. 소비 게임의 asmdef에서 필요한 어셈블리만 참조한다.
