using System;
using System.Collections.Generic;

namespace Achieve.PlayBookmarks
{
    public enum BookmarkRunStatus { Preparing, WaitingForReady, Running, Succeeded, Failed, Cancelled }
    public enum BookmarkStepStatus { Pending, Running, Succeeded, Failed, Skipped, Cancelled }

    [Serializable]
    public sealed class BookmarkStepReport
    {
        public string name;
        public BookmarkStepStatus status;
        public double elapsedSeconds;
        public string error;
    }

    [Serializable]
    public sealed class BookmarkRunReport
    {
        public string runId;
        public string bookmarkId;
        public string bookmarkName;
        public string startedUtc;
        public BookmarkRunStatus status;
        public string message;
        public double elapsedSeconds;
        public List<BookmarkStepReport> steps = new List<BookmarkStepReport>();

        public bool IsFinished => status == BookmarkRunStatus.Succeeded ||
                                  status == BookmarkRunStatus.Failed || status == BookmarkRunStatus.Cancelled;
    }
}
