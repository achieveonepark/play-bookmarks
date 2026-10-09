using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Linq;
using Achieve.PlayBookmarks.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Achieve.PlayBookmarks.Tests
{
    public sealed class BookmarkFileTests
    {
        private string folder;
        private BookmarkProfile profile;

        [SetUp] public void SetUp()
        {
            folder = "Assets/BookmarkFileTest_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            profile = ScriptableObject.CreateInstance<BookmarkProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
        }

        [TearDown] public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(profile);
            AssetDatabase.DeleteAsset(folder);
        }

        [Test] public void FileRoundTripPreservesPolymorphicStepsAndSettings()
        {
            profile.description = "보스 2페이즈";
            profile.useRandomSeed = true;
            profile.randomSeed = 42;
            profile.steps.Add(new MoveTargetStep { targetId = "player", position = new Vector3(1, 2, 3) });
            profile.steps.Add(new WaitStep { seconds = 0.5f, enabled = false });
            var restored = BookmarkFileCodec.FromJson(BookmarkFileCodec.ToJson(profile));
            try
            {
                Assert.That(restored.description, Is.EqualTo(profile.description));
                Assert.That(restored.randomSeed, Is.EqualTo(42));
                Assert.That(restored.steps[0], Is.TypeOf<MoveTargetStep>());
                Assert.That(((MoveTargetStep)restored.steps[0]).position, Is.EqualTo(new Vector3(1, 2, 3)));
                Assert.That(restored.steps[1].enabled, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(restored); }
        }

        [Test] public void UnknownStepDataIsPreservedWhenSavingAgain()
        {
            profile.steps.Add(new MissingBookmarkStep { missingType = "Game.RemovedStep, Game", originalJson = "{\"quest\":123}" });
            var json = BookmarkFileCodec.ToJson(profile);
            var restored = BookmarkFileCodec.FromJson(json);
            try
            {
                Assert.That(restored.steps[0], Is.TypeOf<MissingBookmarkStep>());
                Assert.That(restored.GetValidationErrors(), Has.Count.EqualTo(1));
                Assert.That(BookmarkFileCodec.ToJson(restored), Is.EqualTo(json));
            }
            finally { UnityEngine.Object.DestroyImmediate(restored); }
        }

        [Test] public void ImportedPbmHasEditableToolkitInspectorAndApplyWritesSource()
        {
            var path = folder + "/Test.pbm";
            BookmarkFileCodec.Write(path, profile);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            Assert.That(AssetDatabase.LoadAssetAtPath<BookmarkProfile>(path), Is.Not.Null);
            var editor = (AssetImporterEditor)UnityEditor.Editor.CreateEditor(AssetImporter.GetAtPath(path));
            try
            {
                Assert.That(editor, Is.TypeOf<BookmarkFileImporterEditor>());
                var root = editor.CreateInspectorGUI();
                Assert.That(root.Q<Button>("add-step"), Is.Not.Null);
                Assert.That(root.Query<IMGUIContainer>().ToList(), Is.Empty);
                var extra = (SerializedObject)typeof(AssetImporterEditor).GetProperty("extraDataSerializedObject",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(editor);
                extra.FindProperty("description").stringValue = "Saved from Inspector";
                extra.FindProperty("steps").arraySize = 1;
                extra.FindProperty("steps").GetArrayElementAtIndex(0).managedReferenceValue = new WaitStep { seconds = 2 };
                extra.ApplyModifiedProperties();
                Assert.That(editor.HasModified(), Is.True);
                editor.SaveChanges();
                var saved = BookmarkFileCodec.Read(path);
                try
                {
                    Assert.That(saved.description, Is.EqualTo("Saved from Inspector"));
                    Assert.That(((WaitStep)saved.steps[0]).seconds, Is.EqualTo(2));
                    Assert.That(AssetDatabase.LoadAssetAtPath<BookmarkProfile>(path).description, Is.EqualTo(saved.description));
                }
                finally { UnityEngine.Object.DestroyImmediate(saved); }
                extra.Update();
                extra.FindProperty("description").stringValue = "Discard me";
                extra.ApplyModifiedProperties();
                editor.DiscardChanges();
                Assert.That(File.ReadAllText(path), Does.Not.Contain("Discard me"));
                Assert.That(((BookmarkProfile)extra.targetObject).description, Is.EqualTo("Saved from Inspector"));
            }
            finally { UnityEngine.Object.DestroyImmediate(editor); }
        }

        [Test] public void SceneGuidResolvesAfterSceneIsMoved()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var original = folder + "/Before.unity";
                var moved = folder + "/After.unity";
                EditorSceneManager.SaveScene(scene, original);
                profile.startScenePath = original;
                var json = BookmarkFileCodec.ToJson(profile);
                Assert.That(AssetDatabase.MoveAsset(original, moved), Is.Empty);
                var restored = BookmarkFileCodec.FromJson(json);
                try { Assert.That(restored.startScenePath, Is.EqualTo(moved)); }
                finally { UnityEngine.Object.DestroyImmediate(restored); }
            }
            finally
            {
                if (previous.Any(item => item.isActive && item.isLoaded && !string.IsNullOrEmpty(item.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test] public void FileStepsRejectUnityObjectReferences()
        {
            profile.steps.Add(new ObjectReferenceStep { reference = profile });
            Assert.Throws<ArgumentException>(() => BookmarkFileCodec.ToJson(profile));
        }

        [Serializable] public sealed class ObjectReferenceStep : BookmarkStep
        {
            public UnityEngine.Object reference;
            public override string DisplayName => "Object reference test";
            public override IEnumerator Execute(BookmarkContext context) { yield break; }
        }
    }
}
