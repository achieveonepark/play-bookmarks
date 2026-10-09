using System;
using System.Collections;
using UnityEngine;

namespace Achieve.PlayBookmarks
{
    [Serializable]
    public sealed class WaitStep : BookmarkStep
    {
        [Min(0)] public float seconds = 0.25f;
        public override string DisplayName => $"Wait {seconds:0.##}s (unscaled)";
        public override string Validate() => seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)
            ? "Wait duration must be a non-negative finite number." : null;

        public override IEnumerator Execute(BookmarkContext context)
        {
            var until = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < until) yield return null;
        }
    }
}
