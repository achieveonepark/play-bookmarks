using System;
using System.Linq;
using Achieve.PlayBookmarks.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Achieve.PlayBookmarks.Tests
{
    public sealed class BookmarkEditorTests
    {
        private BookmarkProfile profile;
        [SetUp] public void SetUp() => profile = ScriptableObject.CreateInstance<BookmarkProfile>();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(profile);

        [Test] public void ProfileRejectsMissingStepAndInvalidTimeout()
        {
            profile.steps.Add(null);
            profile.readyTimeoutSeconds = float.NaN;
            Assert.That(profile.GetValidationErrors(), Has.Count.EqualTo(2));
        }

        [Test] public void DisabledStepDoesNotNeedValidRuntimeSettings()
        {
            profile.steps.Add(new MoveTargetStep { enabled = false, targetId = "", timeoutSeconds = -1 });
            Assert.That(profile.GetValidationErrors(), Is.Empty);
        }

        [Test] public void ProfileCloneCopiesManagedReferenceSteps()
        {
            profile.steps.Add(new MoveTargetStep { position = new Vector3(2, 3, 4) });
            var clone = UnityEngine.Object.Instantiate(profile);
            try
            {
                ((MoveTargetStep)clone.steps[0]).position = Vector3.zero;
                Assert.That(((MoveTargetStep)profile.steps[0]).position, Is.EqualTo(new Vector3(2, 3, 4)));
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }

        [Test] public void WindowBuildsToolkitLibrarySettingsAndStatus()
        {
            var window = ScriptableObject.CreateInstance<BookmarkWindow>();
            try
            {
                window.CreateGUI();
                Assert.That(window.rootVisualElement.Q<ListView>("bookmark-list"), Is.Not.Null);
                Assert.That(window.rootVisualElement.Q<Button>("run-bookmark"), Is.Not.Null);
                Assert.That(window.rootVisualElement.Q<ProgressBar>("bookmark-progress"), Is.Not.Null);
                Assert.That(window.rootVisualElement.Query<IMGUIContainer>().ToList(), Is.Empty);
                Assert.That(window.rootVisualElement.styleSheets.count, Is.GreaterThan(0));
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        [Test] public void InspectorUsesToolkitAndExposesStepEditor()
        {
            profile.steps.Add(new InvokeActionStep());
            var editor = UnityEditor.Editor.CreateEditor(profile);
            try
            {
                var root = editor.CreateInspectorGUI();
                Assert.That(root.Q<ObjectField>(), Is.Not.Null);
                Assert.That(root.Q<Button>("add-step"), Is.Not.Null);
                Assert.That(root.Query<PropertyField>().ToList().Count, Is.GreaterThan(5));
                Assert.That(root.Query<IMGUIContainer>().ToList(), Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(editor); }
        }

        [Test] public void ValidationReportsBadTargetAndAction()
        {
            profile.steps.Add(new InvokeActionStep { actionId = "" });
            profile.steps.Add(new WaitStep { seconds = float.PositiveInfinity });
            Assert.That(profile.GetValidationErrors(), Has.Count.EqualTo(2));
        }

        [Test] public void LanguageSwitchTranslatesControlsAndPreservesEditableData()
        {
            var existed = EditorPrefs.HasKey("Achieve.Editor.Language");
            var previous = EditorPrefs.GetString("Achieve.Editor.Language");
            var root = new VisualElement();
            var button = new Button { text = "Run bookmark" };
            var notes = new TextField("Notes") { value = "Run bookmark" };
            root.Add(button); root.Add(notes); root.Add(BookmarkLocalization.CreateSelector());
            try
            {
                BookmarkLocalization.SetLanguage(true);
                BookmarkLocalization.Apply(root);
                Assert.That(button.text, Is.EqualTo("북마크 실행"));
                Assert.That(notes.label, Is.EqualTo("메모"));
                Assert.That(notes.value, Is.EqualTo("Run bookmark"));
                BookmarkLocalization.SetLanguage(false);
                BookmarkLocalization.Apply(root);
                Assert.That(button.text, Is.EqualTo("Run bookmark"));
                Assert.That(notes.label, Is.EqualTo("Notes"));
                Assert.That(notes.value, Is.EqualTo("Run bookmark"));
            }
            finally
            {
                if (existed) EditorPrefs.SetString("Achieve.Editor.Language", previous);
                else EditorPrefs.DeleteKey("Achieve.Editor.Language");
            }
        }
    }
}
