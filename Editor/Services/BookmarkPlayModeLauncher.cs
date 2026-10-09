using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Achieve.PlayBookmarks.Editor
{
    [InitializeOnLoad]
    public static class BookmarkPlayModeLauncher
    {
        private const string RequestKey = "Achieve.PlayBookmarks.Pending";
        private const string RestoreKey = "Achieve.PlayBookmarks.RestoreStartScene";
        private const string PreviousSceneKey = "Achieve.PlayBookmarks.PreviousStartScene";

        static BookmarkPlayModeLauncher()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += RestoreAfterAbortedLaunch;
        }

        public static bool CanLaunch => !EditorApplication.isPlayingOrWillChangePlaymode &&
                                        !EditorApplication.isCompiling && !EditorApplication.isUpdating;

        public static void Launch(BookmarkProfile profile)
        {
            if (!CanLaunch) throw new InvalidOperationException("Wait for the Editor to finish its current operation.");
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var errors = profile.GetValidationErrors();
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
            var profilePath = AssetDatabase.GetAssetPath(profile);
            if (string.IsNullOrEmpty(profilePath)) throw new ArgumentException("Save the bookmark as an asset first.");
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(profile.startScenePath);
            if (scene == null) throw new ArgumentException("Choose a valid start scene.");

            // Preserve Edit mode scenes: only override which scene is used when entering Play mode.
            var previous = EditorSceneManager.playModeStartScene;
            SessionState.SetString(PreviousSceneKey, previous == null ? string.Empty : AssetDatabase.GetAssetPath(previous));
            SessionState.SetBool(RestoreKey, true);
            SessionState.SetString(RequestKey, AssetDatabase.AssetPathToGUID(profilePath));
            AssetDatabase.SaveAssetIfDirty(profile);
            EditorSceneManager.playModeStartScene = scene;
            EditorApplication.isPlaying = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void PrepareRequestedBookmark()
        {
            var guid = SessionState.GetString(RequestKey, string.Empty);
            SessionState.EraseString(RequestKey); // Consume once, including failed requests.
            if (string.IsNullOrEmpty(guid)) return;
            var profile = AssetDatabase.LoadAssetAtPath<BookmarkProfile>(AssetDatabase.GUIDToAssetPath(guid));
            try { BookmarkRunner.Prepare(profile, guid); }
            catch (Exception exception) { Debug.LogError($"Bookmark launch failed: {exception.Message}"); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartRequestedBookmark() => BookmarkRunner.StartPrepared();

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) BookmarkRunner.Cancel();
            if (state == PlayModeStateChange.EnteredEditMode) RestoreStartScene();
        }

        private static void RestoreAfterAbortedLaunch()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) RestoreStartScene();
        }

        private static void RestoreStartScene()
        {
            if (!SessionState.GetBool(RestoreKey, false)) return;
            var path = SessionState.GetString(PreviousSceneKey, string.Empty);
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path)
                ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            SessionState.EraseBool(RestoreKey);
            SessionState.EraseString(PreviousSceneKey);
            SessionState.EraseString(RequestKey);
        }
    }
}
