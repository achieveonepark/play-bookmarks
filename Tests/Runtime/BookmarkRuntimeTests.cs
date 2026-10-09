using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Achieve.PlayBookmarks.Tests
{
    public sealed class BookmarkRuntimeTests
    {
        private BookmarkProfile profile;
        private GameObject targetObject;
        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            profile = ScriptableObject.CreateInstance<BookmarkProfile>();
            profile.name = "Runtime test";
            targetObject = new GameObject("Test target");
            targetObject.AddComponent<BookmarkTarget>().Id = "test-player";
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            BookmarkRunner.Cancel();
            yield return null;
            Time.timeScale = previousTimeScale;
            UnityEngine.Object.Destroy(targetObject);
            UnityEngine.Object.Destroy(profile);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StepsExecuteInOrderWithoutMutatingProfile()
        {
            profile.steps.Add(new MoveTargetStep { targetId = "test-player", position = new Vector3(1, 2, 3) });
            profile.steps.Add(new SetActiveStep { targetId = "test-player", active = false });
            profile.steps.Add(new SetActiveStep { targetId = "test-player", active = true });
            var report = BookmarkRunner.Run(profile);
            yield return WaitForReport(report);
            Assert.That(report.status, Is.EqualTo(BookmarkRunStatus.Succeeded));
            Assert.That(targetObject.transform.position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(targetObject.activeSelf, Is.True);
            Assert.That(profile.steps.Count, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator SetupWaitsForExplicitReadySignal()
        {
            var signal = targetObject.AddComponent<BookmarkReadySignal>();
            signal.Id = "test-ready";
            profile.readySignalId = "test-ready";
            profile.steps.Add(new MoveTargetStep { targetId = "test-player", position = Vector3.one });
            var report = BookmarkRunner.Run(profile);
            yield return null;
            Assert.That(report.status, Is.EqualTo(BookmarkRunStatus.WaitingForReady));
            Assert.That(targetObject.transform.position, Is.EqualTo(Vector3.zero));
            signal.MarkReady();
            yield return WaitForReport(report);
            Assert.That(report.status, Is.EqualTo(BookmarkRunStatus.Succeeded));
        }

        [UnityTest]
        public IEnumerator ReadyTimeoutUsesUnscaledTime()
        {
            Time.timeScale = 0;
            profile.readySignalId = "missing-ready";
            profile.readyTimeoutSeconds = 0.05f;
            var report = BookmarkRunner.Run(profile);
            yield return WaitForReport(report);
            Assert.That(report.status, Is.EqualTo(BookmarkRunStatus.Failed));
            Assert.That(report.message, Does.Contain("timed out"));
        }

        [UnityTest]
        public IEnumerator WatchdogCancelsOpaqueWaitAndDisposesNestedIterator()
        {
            LongWaitStep.Disposed = false;
            Time.timeScale = 0;
            profile.steps.Add(new LongWaitStep { timeoutSeconds = 0.05f });
            var report = BookmarkRunner.Run(profile);
            yield return WaitForReport(report);
            Assert.That(report.status, Is.EqualTo(BookmarkRunStatus.Failed));
            Assert.That(LongWaitStep.Disposed, Is.True);
        }

        [UnityTest]
        public IEnumerator NestedExceptionIsReportedAndLaterStepsDoNotRun()
        {
            profile.steps.Add(new NestedFailureStep());
            profile.steps.Add(new MoveTargetStep { targetId = "test-player", position = Vector3.one });
            var report = BookmarkRunner.Run(profile);
            yield return WaitForReport(report);
            Assert.That(report.status, Is.EqualTo(BookmarkRunStatus.Failed));
            Assert.That(report.message, Does.Contain("nested failure"));
            Assert.That(report.steps[1].status, Is.EqualTo(BookmarkStepStatus.Pending));
            Assert.That(targetObject.transform.position, Is.EqualTo(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator CancellingStopsCurrentStepAndPreventsLaterSteps()
        {
            profile.steps.Add(new WaitStep { seconds = 5 });
            profile.steps.Add(new MoveTargetStep { targetId = "test-player", position = Vector3.one });
            var report = BookmarkRunner.Run(profile);
            BookmarkRunner.Cancel();
            yield return WaitForReport(report);
            Assert.That(report.status, Is.EqualTo(BookmarkRunStatus.Cancelled));
            Assert.That(targetObject.transform.position, Is.EqualTo(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator DuplicateTargetIdsFailInsteadOfChoosingArbitrarily()
        {
            var duplicate = new GameObject("Duplicate target");
            duplicate.AddComponent<BookmarkTarget>().Id = "test-player";
            profile.steps.Add(new MoveTargetStep { targetId = "test-player" });
            var report = BookmarkRunner.Run(profile);
            yield return WaitForReport(report);
            UnityEngine.Object.Destroy(duplicate);
            Assert.That(report.status, Is.EqualTo(BookmarkRunStatus.Failed));
            Assert.That(report.message, Does.Contain("Multiple loaded targets"));
        }

        [UnityTest]
        public IEnumerator RegisteredGameActionReceivesPayload()
        {
            var action = targetObject.AddComponent<TestBookmarkAction>();
            profile.steps.Add(new InvokeActionStep { targetId = "test-player", actionId = "Health", payloadJson = "{\"health\":10}" });
            var report = BookmarkRunner.Run(profile);
            yield return WaitForReport(report);
            Assert.That(report.status, Is.EqualTo(BookmarkRunStatus.Succeeded));
            Assert.That(action.Health, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator ConcurrentRunsAreRejected()
        {
            profile.steps.Add(new WaitStep { seconds = 5 });
            var report = BookmarkRunner.Run(profile);
            Assert.Throws<InvalidOperationException>(() => BookmarkRunner.Run(profile));
            BookmarkRunner.Cancel();
            yield return WaitForReport(report);
        }

        private static IEnumerator WaitForReport(BookmarkRunReport report)
        {
            var deadline = Time.realtimeSinceStartupAsDouble + 3;
            while (!report.IsFinished && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.That(report.IsFinished, Is.True, "Bookmark did not finish before the test deadline.");
        }

        [Serializable]
        public sealed class LongWaitStep : BookmarkStep
        {
            public static bool Disposed;
            public override IEnumerator Execute(BookmarkContext context) { yield return Nested(); }
            private IEnumerator Nested()
            {
                try { yield return new WaitForSeconds(60); }
                finally { Disposed = true; }
            }
        }

        [Serializable]
        public sealed class NestedFailureStep : BookmarkStep
        {
            public override IEnumerator Execute(BookmarkContext context) { yield return Nested(); }
            private IEnumerator Nested()
            {
                yield return null;
                throw new InvalidOperationException("nested failure");
            }
        }
    }
}
