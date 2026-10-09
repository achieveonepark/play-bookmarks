using System;
using System.Collections;
using UnityEngine;

namespace Achieve.PlayBookmarks
{
    [Serializable]
    public sealed class MoveTargetStep : BookmarkStep
    {
        public string targetId = "player";
        public Vector3 position;
        public bool localSpace;
        public bool setRotation = true;
        public Vector3 eulerAngles;
        public override string DisplayName => $"Move {targetId}";
        public override string Validate() => string.IsNullOrWhiteSpace(targetId) ? "Target ID is empty." : null;

        public override IEnumerator Execute(BookmarkContext context)
        {
            var target = context.GetTarget(targetId).transform;
            if (localSpace)
            {
                target.localPosition = position;
                if (setRotation) target.localRotation = Quaternion.Euler(eulerAngles);
            }
            else
            {
                target.position = position;
                if (setRotation) target.rotation = Quaternion.Euler(eulerAngles);
            }
            yield break;
        }
    }
}
