using System;
using System.Collections;

namespace Achieve.PlayBookmarks
{
    [Serializable]
    public sealed class SetActiveStep : BookmarkStep
    {
        public string targetId = "player";
        public bool active = true;
        public override string DisplayName => $"Set {targetId} active = {active}";
        public override string Validate() => string.IsNullOrWhiteSpace(targetId) ? "Target ID is empty." : null;

        public override IEnumerator Execute(BookmarkContext context)
        {
            context.GetTarget(targetId).gameObject.SetActive(active);
            yield break;
        }
    }
}
