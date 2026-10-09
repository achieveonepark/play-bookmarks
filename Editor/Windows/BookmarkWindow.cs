using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.UIElements;

namespace Achieve.PlayBookmarks.Editor
{
    public sealed class BookmarkWindow : EditorWindow
    {
        [SerializeField] private BookmarkProfile selected;
        private readonly List<BookmarkProfile> profiles = new List<BookmarkProfile>();
        private ListView list;
        private ToolbarSearchField search;
        private Label countLabel;
        private Label emptyList;
        private Label bookmarkTitle;
        private Label subtitle;
        private Label statusBadge;
        private Label statusText;
        private Label elapsed;
        private ProgressBar progress;
        private Button runButton;
        private Button cancelButton;
        private ScrollView inspectorHost;
        private VisualElement reportSteps;
        private UnityEditor.Editor profileEditor;
        private string reportSignature;

        [MenuItem("Tools/Achieve/Play Bookmarks")]
        public static void Open() => GetWindow<BookmarkWindow>("Play Bookmarks");

        public static void ShowBookmark(BookmarkProfile profile)
        {
            var window = GetWindow<BookmarkWindow>("Play Bookmarks");
            if (!window.SavePendingEdits()) return;
            window.RefreshProfiles();
            window.SelectProfile(profile);
        }

        public void CreateGUI()
        {
            minSize = new Vector2(840, 560);
            var root = rootVisualElement;
            root.Clear();
            root.AddToClassList("pb-root");
            if (!EditorGUIUtility.isProSkin) root.AddToClassList("light-theme");
            AddStyles(root);

            var header = new Toolbar();
            header.AddToClassList("pb-app-header");
            var wordmark = new Label("Play Bookmarks");
            wordmark.AddToClassList("pb-wordmark");
            header.Add(wordmark);
            var hint = new Label("Test starting conditions");
            hint.AddToClassList("pb-header-hint");
            header.Add(hint);
            header.Add(BookmarkLocalization.CreateSelector());
            root.Add(header);

            var split = new TwoPaneSplitView(0, 240, TwoPaneSplitViewOrientation.Horizontal);
            split.AddToClassList("pb-workspace");
            root.Add(split);
            var sidebar = new VisualElement();
            sidebar.AddToClassList("pb-sidebar");
            var libraryHeader = new VisualElement();
            libraryHeader.AddToClassList("pb-section-row");
            var libraryLabel = new Label("Bookmarks");
            libraryLabel.AddToClassList("pb-eyebrow");
            libraryHeader.Add(libraryLabel);
            countLabel = new Label("0");
            countLabel.AddToClassList("pb-count");
            libraryHeader.Add(countLabel);
            sidebar.Add(libraryHeader);
            search = new ToolbarSearchField { name = "bookmark-search" };
            search.AddToClassList("pb-search");
            search.style.width = StyleKeyword.Auto;
            search.style.minWidth = 0;
            search.style.maxWidth = new Length(100, LengthUnit.Percent);
            search.RegisterValueChangedCallback(_ => RefreshProfiles());
            sidebar.Add(search);
            list = new ListView(profiles, 53, MakeProfileRow, BindProfileRow)
            {
                name = "bookmark-list", selectionType = SelectionType.Single
            };
            list.AddToClassList("pb-profile-list");
            list.selectionChanged += objects =>
            {
                var profile = objects.OfType<BookmarkProfile>().FirstOrDefault();
                if (profile != null) SelectProfile(profile);
            };
            sidebar.Add(list);
            emptyList = new Label("No bookmarks yet.\nCreate one to get started.");
            emptyList.AddToClassList("pb-library-empty");
            sidebar.Add(emptyList);
            var create = new Button(CreateBookmark) { text = "+ New bookmark", name = "new-bookmark" };
            create.AddToClassList("pb-button");
            create.AddToClassList("pb-create-button");
            sidebar.Add(create);
            split.Add(sidebar);

            var content = new VisualElement();
            content.AddToClassList("pb-content");
            var contentHeader = new VisualElement();
            contentHeader.AddToClassList("pb-content-header");
            var heading = new VisualElement();
            heading.style.flexGrow = 1;
            bookmarkTitle = new Label("Choose a bookmark");
            bookmarkTitle.AddToClassList("pb-title");
            subtitle = new Label("Select a .pbm file to edit its starting conditions.");
            subtitle.AddToClassList("pb-subtitle");
            heading.Add(bookmarkTitle);
            heading.Add(subtitle);
            contentHeader.Add(heading);
            runButton = new Button(LaunchSelected) { text = "Run bookmark", name = "run-bookmark" };
            runButton.AddToClassList("pb-button");
            runButton.AddToClassList("pb-primary");
            contentHeader.Add(runButton);
            content.Add(contentHeader);
            inspectorHost = new ScrollView { name = "bookmark-settings" };
            inspectorHost.AddToClassList("pb-settings");
            content.Add(inspectorHost);

            var statusCard = new VisualElement { name = "bookmark-run-status" };
            statusCard.AddToClassList("pb-status-card");
            var statusHeader = new VisualElement();
            statusHeader.AddToClassList("pb-section-row");
            statusBadge = new Label("IDLE");
            statusBadge.AddToClassList("pb-status-badge");
            statusHeader.Add(statusBadge);
            elapsed = new Label("—");
            elapsed.AddToClassList("pb-elapsed");
            statusHeader.Add(elapsed);
            cancelButton = new Button(BookmarkRunner.Cancel) { text = "Cancel setup", name = "cancel-bookmark" };
            cancelButton.AddToClassList("pb-button");
            statusHeader.Add(cancelButton);
            statusCard.Add(statusHeader);
            statusText = new Label("No bookmark has run in this session.");
            statusText.AddToClassList("pb-status-text");
            statusCard.Add(statusText);
            progress = new ProgressBar { lowValue = 0, highValue = 100, title = "", name = "bookmark-progress" };
            progress.AddToClassList("pb-progress");
            statusCard.Add(progress);
            var reportScroll = new ScrollView();
            reportScroll.AddToClassList("pb-report-scroll");
            reportSteps = new VisualElement();
            reportScroll.Add(reportSteps);
            statusCard.Add(reportScroll);
            content.Add(statusCard);
            split.Add(content);
            RefreshProfiles();
            SelectProfile(selected);
            BookmarkLocalization.Install(root);
            root.schedule.Execute(UpdateStatus).Every(150);
            UpdateStatus();
        }

        internal static void AddStyles(VisualElement root)
        {
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.achive.play-bookmarks/Editor/UI/Styles/PlayBookmarks.uss");
            if (style != null) root.styleSheets.Add(style);
        }

        private void OnEnable() => EditorApplication.projectChanged += RefreshProfiles;
        private void OnDisable()
        {
            EditorApplication.projectChanged -= RefreshProfiles;
            if (profileEditor != null) DestroyImmediate(profileEditor);
        }

        private VisualElement MakeProfileRow()
        {
            var row = new VisualElement();
            row.AddToClassList("pb-profile-row");
            var nameLabel = new Label { name = "profile-name" };
            nameLabel.AddToClassList("pb-profile-name");
            nameLabel.AddToClassList("achieve-data");
            row.Add(nameLabel);
            var sceneLabel = new Label { name = "profile-scene" };
            sceneLabel.AddToClassList("pb-profile-scene");
            sceneLabel.AddToClassList("achieve-data");
            row.Add(sceneLabel);
            return row;
        }

        private void BindProfileRow(VisualElement row, int index)
        {
            var profile = profiles[index];
            row.Q<Label>("profile-name").text = profile.name;
            row.Q<Label>("profile-scene").text = string.IsNullOrEmpty(profile.startScenePath)
                ? BookmarkLocalization.Text("Choose a start scene") : System.IO.Path.GetFileNameWithoutExtension(profile.startScenePath);
        }

        private void RefreshProfiles()
        {
            if (list == null) return;
            var query = search?.value ?? string.Empty;
            profiles.Clear();
            profiles.AddRange(AssetDatabase.FindAssets("t:BookmarkProfile")
                .Select(guid => AssetDatabase.LoadAssetAtPath<BookmarkProfile>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(profile => profile != null && (query.Length == 0 ||
                    profile.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(profile => profile.name, StringComparer.OrdinalIgnoreCase));
            countLabel.text = profiles.Count.ToString();
            list.Rebuild();
            var index = profiles.IndexOf(selected);
            list.SetSelectionWithoutNotify(index >= 0 ? new[] { index } : Array.Empty<int>());
            emptyList.style.display = profiles.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            list.style.display = profiles.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void SelectProfile(BookmarkProfile profile)
        {
            if (profile != selected && !SavePendingEdits()) return;
            selected = profile;
            if (inspectorHost == null) return;
            inspectorHost.Clear();
            if (profileEditor != null) DestroyImmediate(profileEditor);
            profileEditor = null;
            bookmarkTitle.text = profile != null ? profile.name : "Choose a bookmark";
            bookmarkTitle.EnableInClassList("achieve-data", profile != null);
            subtitle.text = profile != null ? AssetDatabase.GetAssetPath(profile) : "Create a .pbm file or select one from the list.";
            if (profile == null)
            {
                var empty = new VisualElement();
                empty.AddToClassList("pb-welcome");
                var label = new Label("No bookmark selected");
                label.AddToClassList("pb-welcome-title");
                empty.Add(label);
                var body = new Label("Create a bookmark to configure its scene and setup steps.");
                body.AddToClassList("pb-welcome-body");
                empty.Add(body);
                inspectorHost.Add(empty);
            }
            else
            {
                var path = AssetDatabase.GetAssetPath(profile);
                profileEditor = path.EndsWith(".pbm", StringComparison.OrdinalIgnoreCase)
                    ? UnityEditor.Editor.CreateEditor(AssetImporter.GetAtPath(path))
                    : UnityEditor.Editor.CreateEditor(profile);
                inspectorHost.Add(profileEditor.CreateInspectorGUI());
            }
            UpdateStatus();
        }

        private void CreateBookmark()
        {
            var path = EditorUtility.SaveFilePanelInProject(BookmarkLocalization.Text("New bookmark"), "New Bookmark", "pbm", BookmarkLocalization.Text("Choose a location for your bookmark file."));
            if (string.IsNullOrEmpty(path)) return;
            var profile = CreateInstance<BookmarkProfile>();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            profile.startScenePath = scene.IsValid() ? scene.path : string.Empty;
            BookmarkFileCodec.Write(path, profile);
            DestroyImmediate(profile);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            profile = AssetDatabase.LoadAssetAtPath<BookmarkProfile>(path);
            search.SetValueWithoutNotify(string.Empty);
            RefreshProfiles();
            SelectProfile(profile);
            EditorGUIUtility.PingObject(profile);
        }

        private void LaunchSelected()
        {
            try
            {
                var path = AssetDatabase.GetAssetPath(selected);
                if (!SavePendingEdits()) return;
                selected = AssetDatabase.LoadAssetAtPath<BookmarkProfile>(path);
                BookmarkPlayModeLauncher.Launch(selected);
            }
            catch (Exception exception) { statusText.text = exception.Message; }
        }

        private bool SavePendingEdits()
        {
            if (!(profileEditor is AssetImporterEditor importer) || !importer.HasModified()) return true;
            try { importer.SaveChanges(); return true; }
            catch (Exception exception) { if (statusText != null) statusText.text = exception.Message; return false; }
        }

        private void UpdateStatus()
        {
            if (runButton == null) return;
            runButton.SetEnabled(selected != null && BookmarkPlayModeLauncher.CanLaunch &&
                selected.GetValidationErrors().Count == 0 && !string.IsNullOrEmpty(selected.startScenePath));
            cancelButton.SetEnabled(BookmarkRunner.IsRunning);
            var report = BookmarkRunner.CurrentReport;
            if (report == null) return;
            BookmarkLocalization.SetText(statusBadge, report.status.ToString());
            statusBadge.EnableInClassList("is-success", report.status == BookmarkRunStatus.Succeeded);
            statusBadge.EnableInClassList("is-error", report.status == BookmarkRunStatus.Failed);
            statusBadge.EnableInClassList("is-running", !report.IsFinished);
            BookmarkLocalization.SetText(statusText, report.message);
            elapsed.text = $"{report.elapsedSeconds:0.0}s";
            var finished = report.steps.Count(step => step.status == BookmarkStepStatus.Succeeded || step.status == BookmarkStepStatus.Skipped);
            progress.value = report.steps.Count == 0 ? (report.IsFinished ? 100 : 0) : 100f * finished / report.steps.Count;
            var signature = report.runId + string.Join("|", report.steps.Select(step => step.status.ToString()));
            if (reportSignature == signature) return;
            reportSignature = signature;
            reportSteps.Clear();
            for (var i = 0; i < report.steps.Count; i++)
            {
                var step = report.steps[i];
                var row = new VisualElement();
                row.AddToClassList("pb-report-row");
                row.Add(new Label((i + 1).ToString("00")));
                var label = new Label(step.name) { tooltip = step.error };
                label.AddToClassList("pb-report-name");
                row.Add(label);
                var state = new Label(step.status.ToString());
                state.AddToClassList("pb-report-state");
                state.EnableInClassList("is-error", step.status == BookmarkStepStatus.Failed);
                row.Add(state);
                reportSteps.Add(row);
            }
            BookmarkLocalization.Apply(reportSteps);
        }
    }
}
