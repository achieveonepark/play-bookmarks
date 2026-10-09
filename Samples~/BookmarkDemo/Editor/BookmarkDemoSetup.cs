using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Achieve.PlayBookmarks.Samples.Editor
{
    public static class BookmarkDemoSetup
    {
        [MenuItem("Tools/Achieve/Samples/Create Bookmark Demo")]
        public static void CreateDemo()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var folder = AssetDatabase.GenerateUniqueAssetPath("Assets/PlayBookmarksDemo");
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(folder));
            var profile = CreateDemoAt(folder);
            Selection.activeObject = profile;
            Achieve.PlayBookmarks.Editor.BookmarkWindow.ShowBookmark(profile);
        }

        public static BookmarkProfile CreateDemoAt(string folder)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.13f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            var player = new GameObject("Demo player");
            player.AddComponent<BookmarkTarget>().Id = "player";
            player.AddComponent<BookmarkDemoState>();
            var ready = player.AddComponent<BookmarkReadySignal>();
            ready.Id = "game";
            ready.readyOnStart = true;
            var scenePath = folder + "/BookmarkDemo.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            var profile = ScriptableObject.CreateInstance<BookmarkProfile>();
            profile.name = "Boss Phase 2";
            profile.description = "Start with low health, a FireSword, and the boss in phase 2.";
            profile.startScenePath = scenePath;
            profile.readySignalId = "game";
            profile.useRandomSeed = true;
            profile.steps.Add(new MoveTargetStep { targetId = "player", position = new Vector3(3, 0, 0) });
            profile.steps.Add(new InvokeActionStep
            {
                targetId = "player", actionId = "ApplyState",
                payloadJson = "{\"health\":10,\"weapon\":\"FireSword\",\"bossPhase\":2}"
            });
            var bookmarkPath = folder + "/Boss Phase 2.pbm";
            Achieve.PlayBookmarks.Editor.BookmarkFileCodec.Write(bookmarkPath, profile);
            UnityEngine.Object.DestroyImmediate(profile);
            AssetDatabase.ImportAsset(bookmarkPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<BookmarkProfile>(bookmarkPath);
        }
    }
}
