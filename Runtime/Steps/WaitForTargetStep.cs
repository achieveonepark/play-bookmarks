using System;
using System.Collections;

namespace Achieve.PlayBookmarks
{
    [Serializable]
    public sealed class WaitForTargetStep : BookmarkStep
    {
        public string targetId = "player";
        public override string DisplayName => $"Wait for {targetId}";
        public override string Validate() => string.IsNullOrWhiteSpace(targetId) ? "Target ID is empty." : null;

        public override IEnumerator Execute(BookmarkContext context)
        {
            while (!context.TryGetTarget(targetId, out _)) yield return null;
        }
    }
}
