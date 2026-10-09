using UnityEngine;

namespace Achieve.PlayBookmarks
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Play Bookmarks/Ready Signal")]
    public sealed class BookmarkReadySignal : MonoBehaviour
    {
        [SerializeField] private string id = "game";
        [Tooltip("For simple scenes. Asynchronous games should call MarkReady after initialization.")]
        public bool readyOnStart;
        public string Id { get => id; set => id = value; }
        public bool IsReady { get; private set; }

        private void Awake() => IsReady = false;
        private void Start()
        {
            if (readyOnStart) MarkReady();
        }

        public void MarkReady() => IsReady = true;
        public void ResetReady() => IsReady = false;
    }
}
