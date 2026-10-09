using System;
using System.Collections;

namespace Achieve.PlayBookmarks
{
    [Serializable]
    public sealed class MissingBookmarkStep : BookmarkStep
    {
        public string missingType;
        public string originalJson;
        public override string DisplayName => "Missing: " + missingType;
        public override string Validate() => $"Step type '{missingType}' is unavailable. Restore its script before saving.";
        public override IEnumerator Execute(BookmarkContext context) => throw new InvalidOperationException(Validate());
    }
}
