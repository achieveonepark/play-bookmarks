using System;
using System.Collections;
using UnityEngine;

namespace Achieve.PlayBookmarks.Tests
{
    public sealed class TestBookmarkAction : MonoBehaviour, IBookmarkAction
    {
        [Serializable] private sealed class State { public int health; }
        public string ActionId => "Health";
        public int Health { get; private set; }
        public IEnumerator ExecuteBookmarkAction(BookmarkContext context, string payloadJson)
        {
            Health = JsonUtility.FromJson<State>(payloadJson).health;
            yield break;
        }
    }
}
