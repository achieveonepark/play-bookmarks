using System;
using System.Collections;
using UnityEngine;

namespace Achieve.PlayBookmarks
{
    [Serializable]
    public abstract class BookmarkStep
    {
        public bool enabled = true;
        [Min(0.01f)] public float timeoutSeconds = 10f;
        public virtual string DisplayName => GetType().Name;
        public virtual string Validate() => null;
        public abstract IEnumerator Execute(BookmarkContext context);
    }

    public interface IBookmarkAction
    {
        string ActionId { get; }
        IEnumerator ExecuteBookmarkAction(BookmarkContext context, string payloadJson);
    }
}
