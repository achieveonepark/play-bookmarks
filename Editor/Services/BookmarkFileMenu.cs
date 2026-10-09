using UnityEditor;
using UnityEngine;

namespace Achieve.PlayBookmarks.Editor
{
    internal static class BookmarkFileMenu
    {
        [MenuItem("Assets/Create/Achieve/Play Bookmark", priority = 230)]
        private static void CreateBookmark()
        {
            var profile = ScriptableObject.CreateInstance<BookmarkProfile>();
            try
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                profile.startScenePath = scene.IsValid() ? scene.path : string.Empty;
#if UNITY_6000_4_OR_NEWER
                ProjectWindowUtil.CreateAssetWithTextContent("New Bookmark.pbm", BookmarkFileCodec.ToJson(profile));
#else
                ProjectWindowUtil.CreateAssetWithContent("New Bookmark.pbm", BookmarkFileCodec.ToJson(profile));
#endif
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }
    }
}
