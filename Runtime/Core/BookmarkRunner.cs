using System;
using UnityEngine;

namespace Achieve.PlayBookmarks
{
    public static class BookmarkRunner
    {
        private static BookmarkExecutionHost host;
        private static BookmarkContext prepared;
        public static BookmarkRunReport CurrentReport { get; private set; }
        public static BookmarkProfile PendingProfile => prepared?.Profile;
        public static bool IsRunning => CurrentReport != null && !CurrentReport.IsFinished;
        public static event Action<BookmarkRunReport> RunStarted;
        public static event Action<BookmarkRunReport> RunCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            host = null;
            prepared = null;
            CurrentReport = null;
            // Keep subscriptions for objects retained when scene/domain reload is disabled.
            // Subscribers must unsubscribe on disable (see the integration example).
        }

        /// <summary>Prepare before scene Awake when launched from the Editor.</summary>
        public static BookmarkRunReport Prepare(BookmarkProfile profile, string bookmarkId = null)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Bookmarks require Play mode.");
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (IsRunning) throw new InvalidOperationException("A bookmark is already running.");
            var errors = profile.GetValidationErrors();
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors), nameof(profile));

            // Managed reference steps are copied as part of the profile. Applying a step cannot mutate the asset.
            var snapshot = UnityEngine.Object.Instantiate(profile);
            snapshot.name = profile.name;
            snapshot.hideFlags = HideFlags.HideAndDontSave;
            CurrentReport = new BookmarkRunReport
            {
                runId = Guid.NewGuid().ToString("N"),
                bookmarkId = bookmarkId ?? string.Empty,
                bookmarkName = profile.name,
                startedUtc = DateTime.UtcNow.ToString("O"),
                status = BookmarkRunStatus.Preparing,
                message = "Preparing scene"
            };
            foreach (var step in snapshot.steps)
                CurrentReport.steps.Add(new BookmarkStepReport { name = step.DisplayName });
            prepared = new BookmarkContext(snapshot, CurrentReport);
            if (snapshot.useRandomSeed) UnityEngine.Random.InitState(snapshot.randomSeed);
            Notify(RunStarted, CurrentReport);
            return CurrentReport;
        }

        /// <summary>Run a bookmark in the current scene. This method does not load startScenePath.</summary>
        public static BookmarkRunReport Run(BookmarkProfile profile, string bookmarkId = null)
        {
            Prepare(profile, bookmarkId);
            StartPrepared();
            return CurrentReport;
        }

        public static void StartPrepared()
        {
            if (prepared == null) return;
            if (host != null) throw new InvalidOperationException("A bookmark execution host already exists.");
            var gameObject = new GameObject("[Play Bookmarks]") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            host = gameObject.AddComponent<BookmarkExecutionHost>();
            var context = prepared;
            prepared = null;
            host.Begin(context);
        }

        public static void Cancel()
        {
            if (host != null) host.Cancel();
            else if (prepared != null)
            {
                var context = prepared;
                prepared = null;
                context.IsCancellationRequested = true;
                context.Report.status = BookmarkRunStatus.Cancelled;
                context.Report.message = "Cancelled before scene setup";
                Complete(context, null);
            }
        }

        internal static void Complete(BookmarkContext context, BookmarkExecutionHost completedHost)
        {
            if (host == completedHost) host = null;
            Notify(RunCompleted, context.Report);
            if (context.Profile != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(context.Profile);
                else UnityEngine.Object.DestroyImmediate(context.Profile);
            }
        }

        private static void Notify(Action<BookmarkRunReport> callbacks, BookmarkRunReport report)
        {
            if (callbacks == null) return;
            foreach (Action<BookmarkRunReport> callback in callbacks.GetInvocationList())
            {
                try { callback(report); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }
    }
}
