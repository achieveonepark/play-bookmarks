using System;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Achieve.PlayBookmarks.Editor
{
    [CustomEditor(typeof(BookmarkFileImporter))]
    public sealed class BookmarkFileImporterEditor : ScriptedImporterEditor
    {
        private BookmarkProfileEditor profileEditor;
        protected override Type extraDataType => typeof(BookmarkProfile);
        protected override bool needsApplyRevert => true;
        public override bool showImportedObject => false;

        protected override void InitializeExtraDataInstance(UnityEngine.Object extraData, int targetIndex)
        {
            var path = ((BookmarkFileImporter)targets[targetIndex]).assetPath;
            try
            {
                var profile = BookmarkFileCodec.Read(path);
                EditorUtility.CopySerialized(profile, extraData);
                DestroyImmediate(profile);
            }
            catch (Exception) { /* Invalid source is reported by the importer and by the inspector below. */ }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            try
            {
                var check = BookmarkFileCodec.Read(((BookmarkFileImporter)target).assetPath);
                DestroyImmediate(check);
            }
            catch (Exception exception)
            {
                root.Add(new HelpBox(exception.Message, HelpBoxMessageType.Error));
                return root;
            }
            if (profileEditor != null) DestroyImmediate(profileEditor);
            profileEditor = (BookmarkProfileEditor)UnityEditor.Editor.CreateEditor(extraDataTarget);
            root.Add(profileEditor.CreateEditorUI(extraDataSerializedObject));
#if UNITY_6000_0_OR_NEWER
            root.Add(new ApplyRevertButtonContainer(this));
#else
            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.Add(new Button(() => { DiscardChanges(); Rebuild(root); }) { text = "Revert" });
            actions.Add(new Button(SaveChanges) { text = "Apply" });
            root.Add(actions);
#endif
            root.TrackSerializedObjectValue(extraDataSerializedObject, _ => hasUnsavedChanges = HasModified());
            BookmarkLocalization.Install(root);
            root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (profileEditor != null) DestroyImmediate(profileEditor);
                profileEditor = null;
            });
            return root;
        }

        private void Rebuild(VisualElement root)
        {
            root.Clear();
            root.Add(CreateInspectorGUI());
        }

        protected override bool CanApply()
        {
            try { BookmarkFileCodec.ToJson((BookmarkProfile)extraDataTarget); return true; }
            catch (ArgumentException) { return false; }
        }

        protected override void Apply()
        {
            extraDataSerializedObject.ApplyModifiedProperties();
            for (var i = 0; i < targets.Length; i++)
                BookmarkFileCodec.Write(((BookmarkFileImporter)targets[i]).assetPath, (BookmarkProfile)extraDataTargets[i]);
            base.Apply();
        }
    }
}
