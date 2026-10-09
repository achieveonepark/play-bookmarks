using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Achieve.PlayBookmarks.Editor
{
    [CustomEditor(typeof(BookmarkProfile))]
    public sealed class BookmarkProfileEditor : UnityEditor.Editor
    {
        private VisualElement stepContainer;
        private HelpBox validation;
        private SerializedObject editingData;

        public override VisualElement CreateInspectorGUI()
            => CreateEditorUI(serializedObject);

        public VisualElement CreateEditorUI(SerializedObject data)
        {
            editingData = data;
            var root = new VisualElement();
            root.AddToClassList("pb-inspector");
            if (!EditorGUIUtility.isProSkin) root.AddToClassList("light-theme");
            BookmarkWindow.AddStyles(root);
            root.Add(BookmarkLocalization.CreateSelector());
            AddSection(root, "Starting conditions");
            root.Add(new PropertyField(editingData.FindProperty("description"), "Notes"));
            var sceneProperty = editingData.FindProperty("startScenePath");
            var sceneField = new ObjectField("Start scene")
            {
                objectType = typeof(SceneAsset), allowSceneObjects = false,
                value = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneProperty.stringValue)
            };
            sceneField.RegisterValueChangedCallback(evt =>
            {
                editingData.Update();
                sceneProperty.stringValue = evt.newValue == null ? string.Empty : AssetDatabase.GetAssetPath(evt.newValue);
                editingData.ApplyModifiedProperties();
            });
            root.Add(sceneField);
            root.Add(new PropertyField(editingData.FindProperty("useRandomSeed"), "Set Unity random seed"));
            var seed = new PropertyField(editingData.FindProperty("randomSeed"), "Seed");
            root.Add(seed);
            root.TrackPropertyValue(editingData.FindProperty("useRandomSeed"), property =>
                seed.style.display = property.boolValue ? DisplayStyle.Flex : DisplayStyle.None);
            seed.style.display = sceneProperty.serializedObject.FindProperty("useRandomSeed").boolValue
                ? DisplayStyle.Flex : DisplayStyle.None;
            root.Add(new PropertyField(editingData.FindProperty("readySignalId"), "Ready signal (optional)"));
            root.Add(new PropertyField(editingData.FindProperty("readyTimeoutSeconds"), "Ready timeout · seconds"));

            var stepHeader = new VisualElement();
            stepHeader.AddToClassList("pb-section-row");
            var heading = new Label("Setup steps");
            heading.AddToClassList("pb-eyebrow");
            stepHeader.Add(heading);
            var add = new Button(ShowStepMenu) { text = "+ Add step", name = "add-step" };
            add.AddToClassList("pb-button");
            stepHeader.Add(add);
            root.Add(stepHeader);
            stepContainer = new VisualElement { name = "bookmark-steps" };
            root.Add(stepContainer);
            validation = new HelpBox(string.Empty, HelpBoxMessageType.Error) { name = "bookmark-validation" };
            root.Add(validation);
            RebuildSteps();
            root.TrackSerializedObjectValue(editingData, _ => UpdateValidation());
            root.Bind(editingData);
            BookmarkLocalization.Install(root);
            return root;
        }

        private static void AddSection(VisualElement root, string text)
        {
            var label = new Label(text);
            label.AddToClassList("pb-eyebrow");
            root.Add(label);
        }

        private void ShowStepMenu()
        {
            var menu = new GenericMenu();
            foreach (var type in TypeCache.GetTypesDerivedFrom<BookmarkStep>()
                         .Where(type => type != typeof(MissingBookmarkStep) && !type.IsAbstract && !type.ContainsGenericParameters && type.IsSerializable &&
                                        type.GetConstructor(Type.EmptyTypes) != null)
                         .OrderBy(type => type.Name, StringComparer.Ordinal))
            {
                var captured = type;
                menu.AddItem(new GUIContent(BookmarkLocalization.Text(ObjectNames.NicifyVariableName(type.Name))), false, () =>
                {
                    try
                    {
                        editingData.Update();
                        var steps = editingData.FindProperty("steps");
                        var index = steps.arraySize++;
                        steps.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(captured);
                        editingData.ApplyModifiedProperties();
                        RebuildSteps();
                    }
                    catch (Exception exception) { Debug.LogException(exception); }
                });
            }
            menu.ShowAsContext();
        }

        private void RebuildSteps()
        {
            editingData.Update();
            stepContainer.Clear();
            var steps = editingData.FindProperty("steps");
            if (steps.arraySize == 0)
            {
                var empty = new Label("Add a step to prepare the situation you want to test.");
                empty.AddToClassList("pb-empty-inline");
                stepContainer.Add(empty);
            }
            for (var i = 0; i < steps.arraySize; i++)
            {
                var index = i;
                var property = steps.GetArrayElementAtIndex(i);
                var value = property.managedReferenceValue as BookmarkStep;
                var card = new VisualElement();
                card.AddToClassList("pb-step-card");
                var header = new VisualElement();
                header.AddToClassList("pb-step-header");
                var number = new Label((i + 1).ToString("00"));
                number.AddToClassList("pb-step-number");
                header.Add(number);
                var title = new Label(value?.DisplayName ?? "Missing step type");
                title.AddToClassList("pb-step-title");
                header.Add(title);
                AddAction(header, "↑", "Move up", () => Move(index, index - 1), i > 0);
                AddAction(header, "↓", "Move down", () => Move(index, index + 1), i + 1 < steps.arraySize);
                AddAction(header, "×", "Remove step", () =>
                {
                    editingData.Update();
                    editingData.FindProperty("steps").DeleteArrayElementAtIndex(index);
                    editingData.ApplyModifiedProperties();
                    RebuildSteps();
                }, true);
                card.Add(header);
                if (value != null)
                {
                    var fields = new VisualElement();
                    fields.AddToClassList("pb-step-fields");
                    var child = property.Copy();
                    var end = property.GetEndProperty();
                    if (child.NextVisible(true))
                    {
                        do
                        {
                            if (SerializedProperty.EqualContents(child, end)) break;
                            var field = new PropertyField(child.Copy());
                            field.Bind(editingData);
                            fields.Add(field);
                        } while (child.NextVisible(false));
                    }
                    card.Add(fields);
                }
                stepContainer.Add(card);
            }
            UpdateValidation();
        }

        private void Move(int from, int to)
        {
            editingData.Update();
            editingData.FindProperty("steps").MoveArrayElement(from, to);
            editingData.ApplyModifiedProperties();
            RebuildSteps();
        }

        private static void AddAction(VisualElement header, string text, string tooltip, Action callback, bool enabled)
        {
            var button = new Button(callback) { text = text, tooltip = tooltip };
            button.AddToClassList("pb-icon-button");
            button.SetEnabled(enabled);
            header.Add(button);
        }

        private void UpdateValidation()
        {
            if (validation == null || target == null) return;
            var errors = ((BookmarkProfile)editingData.targetObject).GetValidationErrors();
            validation.text = string.Join("\n", errors);
            validation.style.display = errors.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
