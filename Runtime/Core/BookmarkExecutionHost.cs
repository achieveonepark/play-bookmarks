using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Achieve.PlayBookmarks
{
    [AddComponentMenu("")]
    public sealed class BookmarkExecutionHost : MonoBehaviour
    {
        private BookmarkContext context;
        private Coroutine stepCoroutine;
        private GuardedRoutine stepRoutine;
        private double startedAt;
        private bool completed;

        internal void Begin(BookmarkContext runContext)
        {
            context = runContext;
            startedAt = Time.realtimeSinceStartupAsDouble;
            StartCoroutine(Run());
        }

        internal void Cancel()
        {
            if (completed || context == null) return;
            context.IsCancellationRequested = true;
            StopStep();
        }

        private IEnumerator Run()
        {
            if (!string.IsNullOrWhiteSpace(context.Profile.readySignalId))
            {
                context.Report.status = BookmarkRunStatus.WaitingForReady;
                context.Report.message = $"Waiting for '{context.Profile.readySignalId}'";
                while (!context.IsCancellationRequested)
                {
                    bool ready;
                    try { ready = context.IsReady(context.Profile.readySignalId); }
                    catch (Exception exception) { Fail(exception.Message); yield break; }
                    if (ready) break;
                    if (Elapsed >= context.Profile.readyTimeoutSeconds)
                    {
                        Fail($"Ready signal '{context.Profile.readySignalId}' timed out.");
                        yield break;
                    }
                    context.Report.elapsedSeconds = Elapsed;
                    yield return null;
                }
            }

            context.Report.status = BookmarkRunStatus.Running;
            for (var i = 0; i < context.Profile.steps.Count; i++)
            {
                if (context.IsCancellationRequested) break;
                var step = context.Profile.steps[i];
                var report = context.Report.steps[i];
                if (!step.enabled) { report.status = BookmarkStepStatus.Skipped; continue; }
                report.status = BookmarkStepStatus.Running;
                context.Report.message = $"Step {i + 1}/{context.Profile.steps.Count}: {report.name}";
                var stepStarted = Time.realtimeSinceStartupAsDouble;
                try { stepRoutine = new GuardedRoutine(step.Execute(context)); }
                catch (Exception exception)
                {
                    report.status = BookmarkStepStatus.Failed;
                    report.error = exception.Message;
                    Fail(report.error);
                    yield break;
                }
                stepCoroutine = StartCoroutine(stepRoutine);

                // A separate watchdog keeps timeouts effective even while a step yields WaitForSeconds.
                while (!stepRoutine.Finished && !context.IsCancellationRequested)
                {
                    report.elapsedSeconds = Time.realtimeSinceStartupAsDouble - stepStarted;
                    context.Report.elapsedSeconds = Elapsed;
                    if (report.elapsedSeconds >= step.timeoutSeconds)
                    {
                        report.status = BookmarkStepStatus.Failed;
                        report.error = $"'{report.name}' timed out after {step.timeoutSeconds:0.##} seconds.";
                        StopStep();
                        Fail(report.error);
                        yield break;
                    }
                    yield return null;
                }

                report.elapsedSeconds = Time.realtimeSinceStartupAsDouble - stepStarted;
                if (context.IsCancellationRequested)
                {
                    report.status = BookmarkStepStatus.Cancelled;
                    break;
                }
                var error = stepRoutine.Error;
                StopStep();
                if (error != null)
                {
                    report.status = BookmarkStepStatus.Failed;
                    report.error = error.Message;
                    Fail($"{report.name}: {error.Message}");
                    yield break;
                }
                report.status = BookmarkStepStatus.Succeeded;
            }

            Finish(context.IsCancellationRequested ? BookmarkRunStatus.Cancelled : BookmarkRunStatus.Succeeded,
                context.IsCancellationRequested ? "Setup cancelled" : "Ready to play");
        }

        private double Elapsed => Time.realtimeSinceStartupAsDouble - startedAt;
        private void Fail(string message) => Finish(BookmarkRunStatus.Failed, message);

        private void StopStep()
        {
            if (stepCoroutine != null) StopCoroutine(stepCoroutine);
            stepCoroutine = null;
            stepRoutine?.Dispose();
            // Retain the routine until the watchdog has inspected its error/completion state.
        }

        private void Finish(BookmarkRunStatus status, string message)
        {
            if (completed) return;
            completed = true;
            StopStep();
            context.Report.status = status;
            context.Report.message = message;
            context.Report.elapsedSeconds = Elapsed;
            BookmarkRunner.Complete(context, this);
            if (Application.isPlaying) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (completed || context == null) return;
            context.IsCancellationRequested = true;
            foreach (var report in context.Report.steps)
                if (report.status == BookmarkStepStatus.Running) report.status = BookmarkStepStatus.Cancelled;
            Finish(BookmarkRunStatus.Cancelled, "Play mode ended or execution host was destroyed");
        }

        /// <summary>Flatten nested enumerators, catch their exceptions, and dispose them on cancellation.</summary>
        private sealed class GuardedRoutine : IEnumerator, IDisposable
        {
            private readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
            public bool Finished { get; private set; }
            public Exception Error { get; private set; }
            public object Current { get; private set; }

            public GuardedRoutine(IEnumerator routine)
            {
                if (routine != null) stack.Push(routine);
            }

            public bool MoveNext()
            {
                if (Finished) return false;
                try
                {
                    while (stack.Count > 0)
                    {
                        var routine = stack.Peek();
                        if (!routine.MoveNext())
                        {
                            stack.Pop();
                            (routine as IDisposable)?.Dispose();
                            continue;
                        }
                        var value = routine.Current;
                        if (value is IEnumerator nested) { stack.Push(nested); continue; }
                        Current = value;
                        return true;
                    }
                    Finished = true;
                    return false;
                }
                catch (Exception exception)
                {
                    Error = exception;
                    Dispose();
                    return false;
                }
            }

            public void Reset() => throw new NotSupportedException();
            public void Dispose()
            {
                Finished = true;
                while (stack.Count > 0)
                {
                    try { (stack.Pop() as IDisposable)?.Dispose(); }
                    catch (Exception exception) { if (Error == null) Error = exception; }
                }
            }
        }
    }
}
