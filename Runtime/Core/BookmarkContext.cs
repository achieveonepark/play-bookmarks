using System;
using System.Collections.Generic;
using UnityEngine;

namespace Achieve.PlayBookmarks
{
    public sealed class BookmarkContext
    {
        public BookmarkProfile Profile { get; }
        public BookmarkRunReport Report { get; }
        public bool IsCancellationRequested { get; internal set; }
        public IDictionary<string, object> Values { get; } = new Dictionary<string, object>();

        internal BookmarkContext(BookmarkProfile profile, BookmarkRunReport report)
        {
            Profile = profile;
            Report = report;
        }

        public BookmarkTarget GetTarget(string id)
        {
            if (!TryGetTarget(id, out var target))
                throw new InvalidOperationException($"Bookmark target '{id}' was not found in a loaded scene.");
            return target;
        }

        public bool TryGetTarget(string id, out BookmarkTarget target)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Target ID is empty.", nameof(id));
            target = null;
            foreach (var candidate in FindSceneObjects<BookmarkTarget>())
            {
                if (!string.Equals(candidate.Id, id, StringComparison.Ordinal)) continue;
                if (target != null)
                    throw new InvalidOperationException($"Multiple loaded targets use ID '{id}'. Assign unique IDs.");
                target = candidate;
            }
            return target != null;
        }

        internal bool IsReady(string id)
        {
            BookmarkReadySignal found = null;
            foreach (var candidate in FindSceneObjects<BookmarkReadySignal>())
            {
                if (!string.Equals(candidate.Id, id, StringComparison.Ordinal)) continue;
                if (found != null)
                    throw new InvalidOperationException($"Multiple loaded ready signals use ID '{id}'.");
                found = candidate;
            }
            return found != null && found.IsReady;
        }

        private static IEnumerable<T> FindSceneObjects<T>() where T : Component
        {
            // Include inactive targets so SetActiveStep can enable them. Do not search prefab assets.
#if UNITY_6000_4_OR_NEWER
            var components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
#else
            var components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#endif
            foreach (var component in components)
            {
                var scene = component.gameObject.scene;
                if (scene.IsValid() && scene.isLoaded) yield return component;
            }
        }
    }
}
