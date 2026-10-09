using System;
using System.Collections;
using UnityEngine;

namespace Achieve.PlayBookmarks.Samples
{
    public sealed class BookmarkDemoState : MonoBehaviour, IBookmarkAction
    {
        [Serializable]
        public sealed class State
        {
            public int health = 100;
            public string weapon = "StarterSword";
            public int bossPhase = 1;
        }

        public State state = new State();
        public string ActionId => "ApplyState";
        public IEnumerator ExecuteBookmarkAction(BookmarkContext context, string payloadJson)
        {
            var requested = JsonUtility.FromJson<State>(payloadJson);
            if (requested == null) throw new ArgumentException("Expected player state JSON.");
            state = requested;
            yield break;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(28, 28, 400, 240), GUI.skin.box);
            GUILayout.Label("PLAY BOOKMARKS / DEMO");
            GUILayout.Space(12);
            GUILayout.Label($"Health: {state.health}");
            GUILayout.Label($"Weapon: {state.weapon}");
            GUILayout.Label($"Boss phase: {state.bossPhase}");
            GUILayout.Label($"Position: {transform.position}");
            GUILayout.Space(12);
            GUILayout.Label("Run the Boss Phase 2 bookmark from Tools / Achieve.");
            GUILayout.EndArea();
        }
    }
}
