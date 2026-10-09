using System;
using System.Collections;
using UnityEngine;

namespace Achieve.PlayBookmarks
{
    [Serializable]
    public sealed class InvokeActionStep : BookmarkStep
    {
        public string targetId = "player";
        public string actionId = "ApplyState";
        [TextArea(3, 10)] public string payloadJson = "{}";
        public override string DisplayName => $"{targetId}: {actionId}";

        public override string Validate()
        {
            if (string.IsNullOrWhiteSpace(targetId)) return "Target ID is empty.";
            return string.IsNullOrWhiteSpace(actionId) ? "Action ID is empty." : null;
        }

        public override IEnumerator Execute(BookmarkContext context)
        {
            var target = context.GetTarget(targetId);
            IBookmarkAction found = null;
            foreach (var component in target.GetComponents<MonoBehaviour>())
            {
                if (!(component is IBookmarkAction action) || action.ActionId != actionId) continue;
                if (found != null)
                    throw new InvalidOperationException($"Target '{targetId}' has multiple actions named '{actionId}'.");
                found = action;
            }
            if (found == null)
                throw new InvalidOperationException($"Action '{actionId}' was not found on target '{targetId}'.");
            var routine = found.ExecuteBookmarkAction(context, payloadJson);
            if (routine != null) yield return routine;
        }
    }
}
