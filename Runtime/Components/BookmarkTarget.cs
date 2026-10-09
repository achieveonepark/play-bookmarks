using UnityEngine;

namespace Achieve.PlayBookmarks
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Play Bookmarks/Bookmark Target")]
    public sealed class BookmarkTarget : MonoBehaviour
    {
        [SerializeField] private string id = "player";
        public string Id { get => id; set => id = value; }
    }
}
