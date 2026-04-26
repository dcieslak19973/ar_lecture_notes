#pragma warning disable 0618  // suppress TMP obsolete enableWordWrapping
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using TMPro;

/// <summary>
/// One-shot project setup: Android player settings, prefabs, scenes, XR loader.
/// Runs automatically once on domain reload.
/// Re-run any time via menu: AR Lecture Notes > Run Setup.
/// </summary>
[InitializeOnLoad]
public static class ProjectSetup
{
    const string DoneKey = "ARLectureNotes_SetupDone_v2";

    static ProjectSetup()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        EditorApplication.delayCall += Run;
    }

    [MenuItem("AR Lecture Notes/Run Setup")]
    public static void RunManual()
    {
        EditorPrefs.SetBool(DoneKey, false);
        Run();
    }

    static void Run()
    {
        try
        {
            EditorPrefs.SetBool(DoneKey, true);
            ConfigureAndroidPlayerSettings();
            TryAssignXREALLoader();
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Scenes");
            CreateCourseRowPrefab();
            CreateNoteRowPrefab();
            CreateAllScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProjectSetup] Done. Next: File > Build Settings > Android > Switch Platform.");
        }
        catch (Exception e)
        {
            Debug.LogError("[ProjectSetup] Setup failed — " + e);
            EditorPrefs.SetBool(DoneKey, false);
        }
    }

    // ── Android Player Settings ──────────────────────────────────────────────

    static void ConfigureAndroidPlayerSettings()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.yourcompany.arlecturenotes");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        Debug.Log("[ProjectSetup] Android player settings configured.");
    }

    // ── XR Management ────────────────────────────────────────────────────────

    static void TryAssignXREALLoader()
    {
        var xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if (xrSettings == null || xrSettings.Manager == null)
        {
            Debug.LogWarning("[ProjectSetup] XR Management not yet initialized for Android. " +
                "Open Project Settings > XR Plug-in Management once, then run AR Lecture Notes > Run Setup again.");
            return;
        }
        const string xrealLoaderType = "Unity.XR.XREAL.XREALXRLoader";
        bool ok = XRPackageMetadataStore.AssignLoader(
            xrSettings.Manager, xrealLoaderType, BuildTargetGroup.Android);
        Debug.Log(ok
            ? "[ProjectSetup] XREALXRLoader assigned to Android XR Management."
            : "[ProjectSetup] XREALXRLoader already assigned (or assignment failed — check Project Settings > XR Plug-in Management).");
    }

    // ── Prefabs ──────────────────────────────────────────────────────────────

    static void CreateCourseRowPrefab()
    {
        const string path = "Assets/Prefabs/CourseRow.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var root = new GameObject("CourseRow");
        root.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.28f, 1f);
        root.AddComponent<Button>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 80);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(root.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Course Name";
        tmp.fontSize = 22;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(textGO.GetComponent<RectTransform>(), 20, 0, -20, 0);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    static void CreateNoteRowPrefab()
    {
        const string path = "Assets/Prefabs/NoteRow.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var root = new GameObject("NoteRow");
        root.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.24f, 1f);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(root.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Note text";
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        Stretch(textGO.GetComponent<RectTransform>(), 16, 4, -16, -4);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    // ── Scenes ───────────────────────────────────────────────────────────────

    static void CreateAllScenes()
    {
        var courseRow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CourseRow.prefab");
        var noteRow   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NoteRow.prefab");

        var buildScenes = new List<EditorBuildSettingsScene>
        {
            MakeScene("CourseList",     canvas => BuildCourseListScene(canvas, courseRow),    isFirst: true),
            MakeScene("LectureCapture", canvas => BuildLectureCaptureScene(canvas, noteRow)),
            MakeScene("Review",         canvas => BuildReviewScene(canvas, noteRow)),
        };
        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log("[ProjectSetup] Build settings scenes registered.");
    }

    static EditorBuildSettingsScene MakeScene(string name, Action<GameObject> buildUI, bool isFirst = false)
    {
        string path = $"Assets/Scenes/{name}.unity";
        if (!File.Exists(path))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var c = cam.AddComponent<Camera>();
            c.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            c.clearFlags = CameraClearFlags.SolidColor;
            cam.AddComponent<AudioListener>();

            // EventSystem
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // First scene: persistent singletons
            if (isFirst)
            {
                var bootstrap = new GameObject("AppBootstrap");
                bootstrap.AddComponent<AppBootstrap>();
                BuildToast(canvasGO.transform);
                BuildConfirmDialog(canvasGO.transform);
            }

            buildUI(canvasGO);

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[ProjectSetup] Scene created: {path}");
        }
        return new EditorBuildSettingsScene(path, true);
    }

    // ── Scene: CourseList ─────────────────────────────────────────────────────

    static void BuildCourseListScene(GameObject canvas, GameObject courseRowPrefab)
    {
        var root = ScreenRoot(canvas.transform, "CourseListScreen");
        var comp = root.AddComponent<CourseListScreen>();

        // Title
        var titleLabel = Label(root.transform, "Title", "My Courses", 36, TextAlignmentOptions.TopLeft);
        Anchor(titleLabel.rectTransform, 0, 0.89f, 1, 1, 40, 0, -40, 0);

        // Scroll list
        var (scroll, content) = ScrollView(root.transform, "CourseScrollView");
        Anchor(scroll.GetComponent<RectTransform>(), 0, 0.18f, 1, 0.88f, 0, 0, 0, 0);

        // Add Course button
        var addBtn = Btn(root.transform, "AddCourseButton", "+ Add Course", 26);
        Anchor(addBtn.GetComponent<RectTransform>(), 0.25f, 0.04f, 0.75f, 0.15f, 0, 0, 0, 0);

        // Add Course panel (modal)
        var panel = Panel(root.transform, "AddCoursePanel", new Color(0.08f, 0.08f, 0.12f, 0.97f));
        Anchor(panel.GetComponent<RectTransform>(), 0.05f, 0.05f, 0.95f, 0.95f, 0, 0, 0, 0);

        var nameInput       = InputField(panel.transform, "NameInput",       "Course name...");
        var instructorInput = InputField(panel.transform, "InstructorInput", "Instructor...");
        var roomInput       = InputField(panel.transform, "RoomInput",       "Room...");
        var scheduleInput   = InputField(panel.transform, "ScheduleInput",   "Schedule (e.g. Mon/Wed 10am)...");
        Anchor(nameInput.GetComponent<RectTransform>(),       0.05f, 0.74f, 0.95f, 0.88f, 0, 0, 0, 0);
        Anchor(instructorInput.GetComponent<RectTransform>(), 0.05f, 0.57f, 0.95f, 0.71f, 0, 0, 0, 0);
        Anchor(roomInput.GetComponent<RectTransform>(),       0.05f, 0.40f, 0.95f, 0.54f, 0, 0, 0, 0);
        Anchor(scheduleInput.GetComponent<RectTransform>(),   0.05f, 0.23f, 0.95f, 0.37f, 0, 0, 0, 0);

        var confirmBtn = Btn(panel.transform, "ConfirmAddButton", "Add",    24);
        var cancelBtn  = Btn(panel.transform, "CancelAddButton",  "Cancel", 24);
        Anchor(confirmBtn.GetComponent<RectTransform>(), 0.55f, 0.05f, 0.93f, 0.19f, 0, 0, 0, 0);
        Anchor(cancelBtn.GetComponent<RectTransform>(),  0.07f, 0.05f, 0.45f, 0.19f, 0, 0, 0, 0);

        Wire(comp,
            ("_listContainer",    (UnityEngine.Object)content.transform),
            ("_courseRowPrefab",  courseRowPrefab),
            ("_addCourseButton",  addBtn.GetComponent<Button>()),
            ("_addCoursePanel",   panel),
            ("_nameInput",        nameInput.GetComponent<TMP_InputField>()),
            ("_instructorInput",  instructorInput.GetComponent<TMP_InputField>()),
            ("_roomInput",        roomInput.GetComponent<TMP_InputField>()),
            ("_scheduleInput",    scheduleInput.GetComponent<TMP_InputField>()),
            ("_confirmAddButton", confirmBtn.GetComponent<Button>()),
            ("_cancelAddButton",  cancelBtn.GetComponent<Button>()));
    }

    // ── Scene: LectureCapture ─────────────────────────────────────────────────

    static void BuildLectureCaptureScene(GameObject canvas, GameObject noteRowPrefab)
    {
        var root = ScreenRoot(canvas.transform, "LectureCaptureScreen");
        var comp = root.AddComponent<LectureCaptureScreen>();

        var titleText = Label(root.transform, "SessionTitle", "Session", 30, TextAlignmentOptions.TopLeft);
        Anchor(titleText.rectTransform, 0, 0.89f, 1, 1, 40, 0, -40, 0);

        var transcriptFeed = Label(root.transform, "TranscriptFeed", "", 17, TextAlignmentOptions.TopLeft);
        transcriptFeed.color = new Color(0.65f, 0.65f, 0.65f, 1f);
        transcriptFeed.enableWordWrapping = true;
        Anchor(transcriptFeed.rectTransform, 0, 0.79f, 1, 0.88f, 40, 0, -40, 0);

        var (scroll, content) = ScrollView(root.transform, "NoteScrollView");
        Anchor(scroll.GetComponent<RectTransform>(), 0, 0.38f, 1, 0.78f, 0, 0, 0, 0);

        var bulletInput  = InputField(root.transform, "BulletInput",  "Type a note...");
        var addBulletBtn = Btn(root.transform, "AddBulletButton", "+", 30);
        Anchor(bulletInput.GetComponent<RectTransform>(),  0,     0.28f, 0.78f, 0.37f, 8, 0, -4, 0);
        Anchor(addBulletBtn.GetComponent<RectTransform>(), 0.79f, 0.28f, 1,     0.37f, 0, 0, -8, 0);

        var markImportant  = Btn(root.transform, "MarkImportantButton",  "! Important", 20);
        var markExam       = Btn(root.transform, "MarkExamButton",        "★ Exam",      20);
        var markAssignment = Btn(root.transform, "MarkAssignmentButton",  "✎ HW",        20);
        Anchor(markImportant.GetComponent<RectTransform>(),  0,     0.19f, 0.31f, 0.27f, 4, 0, -2, 0);
        Anchor(markExam.GetComponent<RectTransform>(),       0.33f, 0.19f, 0.65f, 0.27f, 2, 0, -2, 0);
        Anchor(markAssignment.GetComponent<RectTransform>(), 0.67f, 0.19f, 1,     0.27f, 2, 0, -4, 0);

        var toggleMicBtn = Btn(root.transform, "ToggleTranscriptionButton", "Start Mic",    22);
        var endSessionBtn = Btn(root.transform, "EndSessionButton",          "End Session", 22);
        endSessionBtn.GetComponent<Image>().color = new Color(0.65f, 0.18f, 0.18f, 1f);
        Anchor(toggleMicBtn.GetComponent<RectTransform>(),   0,     0.04f, 0.48f, 0.16f, 8, 0, -4, 0);
        Anchor(endSessionBtn.GetComponent<RectTransform>(),  0.52f, 0.04f, 1,     0.16f, 4, 0, -8, 0);

        Wire(comp,
            ("_sessionTitleText",          (UnityEngine.Object)titleText),
            ("_transcriptFeedText",         transcriptFeed),
            ("_bulletInput",               bulletInput.GetComponent<TMP_InputField>()),
            ("_addBulletButton",           addBulletBtn.GetComponent<Button>()),
            ("_markImportantButton",       markImportant.GetComponent<Button>()),
            ("_markExamButton",            markExam.GetComponent<Button>()),
            ("_markAssignmentButton",      markAssignment.GetComponent<Button>()),
            ("_endSessionButton",          endSessionBtn.GetComponent<Button>()),
            ("_toggleTranscriptionButton", toggleMicBtn.GetComponent<Button>()),
            ("_noteListContainer",         content.transform),
            ("_noteRowPrefab",             noteRowPrefab));
    }

    // ── Scene: Review ─────────────────────────────────────────────────────────

    static void BuildReviewScene(GameObject canvas, GameObject noteRowPrefab)
    {
        var root = ScreenRoot(canvas.transform, "ReviewScreen");
        var comp = root.AddComponent<ReviewScreen>();

        var titleText = Label(root.transform, "Title", "Session Review", 32, TextAlignmentOptions.TopLeft);
        Anchor(titleText.rectTransform, 0, 0.90f, 1, 1, 40, 0, -40, 0);

        var summaryLabel = Label(root.transform, "SummaryLabel", "Summary", 20, TextAlignmentOptions.TopLeft);
        summaryLabel.color = new Color(0.6f, 0.7f, 1f, 1f);
        Anchor(summaryLabel.rectTransform, 0.02f, 0.80f, 0.5f, 0.89f, 0, 0, 0, 0);

        var summaryText = Label(root.transform, "SummaryText", "...", 17, TextAlignmentOptions.TopLeft);
        summaryText.enableWordWrapping = true;
        Anchor(summaryText.rectTransform, 0.02f, 0.66f, 0.98f, 0.79f, 0, 0, 0, 0);

        var keyTermsLabel = Label(root.transform, "KeyTermsLabel", "Key Terms", 20, TextAlignmentOptions.TopLeft);
        keyTermsLabel.color = new Color(0.5f, 1f, 0.6f, 1f);
        Anchor(keyTermsLabel.rectTransform, 0.02f, 0.57f, 0.5f, 0.65f, 0, 0, 0, 0);

        var keyTermsText = Label(root.transform, "KeyTermsText", "—", 16, TextAlignmentOptions.TopLeft);
        Anchor(keyTermsText.rectTransform, 0.02f, 0.48f, 0.98f, 0.56f, 0, 0, 0, 0);

        var actionLabel = Label(root.transform, "ActionItemsLabel", "Action Items", 20, TextAlignmentOptions.TopLeft);
        actionLabel.color = new Color(1f, 0.78f, 0.4f, 1f);
        Anchor(actionLabel.rectTransform, 0.02f, 0.39f, 0.5f, 0.47f, 0, 0, 0, 0);

        var actionText = Label(root.transform, "ActionItemsText", "—", 16, TextAlignmentOptions.TopLeft);
        actionText.enableWordWrapping = true;
        Anchor(actionText.rectTransform, 0.02f, 0.29f, 0.98f, 0.38f, 0, 0, 0, 0);

        var (scroll, content) = ScrollView(root.transform, "NoteScrollView");
        Anchor(scroll.GetComponent<RectTransform>(), 0, 0.17f, 1, 0.28f, 0, 0, 0, 0);

        var searchInput = InputField(root.transform, "SearchInput", "Search sessions...");
        var searchBtn   = Btn(root.transform, "SearchButton", "Search", 22);
        Anchor(searchInput.GetComponent<RectTransform>(), 0,     0.08f, 0.76f, 0.15f, 8, 0, -4, 0);
        Anchor(searchBtn.GetComponent<RectTransform>(),   0.77f, 0.08f, 1,     0.15f, 0, 0, -8, 0);

        var exportBtn = Btn(root.transform, "ExportMarkdownButton", "Export .md", 22);
        var backBtn   = Btn(root.transform, "BackButton",           "← Back",    22);
        Anchor(exportBtn.GetComponent<RectTransform>(), 0.52f, 0.01f, 1,     0.07f, 4, 0, -8, 0);
        Anchor(backBtn.GetComponent<RectTransform>(),   0,     0.01f, 0.48f, 0.07f, 8, 0, -4, 0);

        // Busy overlay
        var busy = new GameObject("BusyIndicator");
        busy.transform.SetParent(root.transform, false);
        busy.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
        var busyRect = busy.GetComponent<RectTransform>();
        busyRect.anchorMin = Vector2.zero; busyRect.anchorMax = Vector2.one;
        busyRect.sizeDelta = Vector2.zero;
        var busyLabel = Label(busy.transform, "BusyText", "Loading...", 34, TextAlignmentOptions.Center);
        Anchor(busyLabel.rectTransform, 0, 0, 1, 1, 0, 0, 0, 0);
        busy.SetActive(false);

        Wire(comp,
            ("_titleText",            (UnityEngine.Object)titleText),
            ("_summaryText",          summaryText),
            ("_keyTermsText",         keyTermsText),
            ("_actionItemsText",      actionText),
            ("_noteListContainer",    content.transform),
            ("_noteRowPrefab",        noteRowPrefab),
            ("_searchInput",          searchInput.GetComponent<TMP_InputField>()),
            ("_searchButton",         searchBtn.GetComponent<Button>()),
            ("_exportMarkdownButton", exportBtn.GetComponent<Button>()),
            ("_backButton",           backBtn.GetComponent<Button>()),
            ("_busyIndicator",        busy));
    }

    // ── Singletons ────────────────────────────────────────────────────────────

    static void BuildToast(Transform canvasParent)
    {
        var root = new GameObject("Toast");
        root.transform.SetParent(canvasParent, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        var comp = root.AddComponent<Toast>();

        var toastPanel = Panel(root.transform, "ToastPanel", new Color(0.1f, 0.1f, 0.1f, 0.92f));
        Anchor(toastPanel.GetComponent<RectTransform>(), 0.15f, 0.05f, 0.85f, 0.15f, 0, 0, 0, 0);

        var msg = Label(toastPanel.transform, "MessageText", "", 22, TextAlignmentOptions.Center);
        msg.enableWordWrapping = true;
        Anchor(msg.rectTransform, 0, 0, 1, 1, 12, 4, -12, -4);

        toastPanel.SetActive(false);

        Wire(comp,
            ("_panel",       (UnityEngine.Object)toastPanel),
            ("_messageText", msg));
    }

    static void BuildConfirmDialog(Transform canvasParent)
    {
        var root = new GameObject("ConfirmDialog");
        root.transform.SetParent(canvasParent, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        var comp = root.AddComponent<ConfirmDialog>();

        // _panel = full-screen dark overlay (also serves as interaction blocker)
        var dialogPanel = Panel(root.transform, "DialogPanel", new Color(0f, 0f, 0f, 0.72f));
        Anchor(dialogPanel.GetComponent<RectTransform>(), 0, 0, 1, 1, 0, 0, 0, 0);

        // Inner dialog box
        var box = Panel(dialogPanel.transform, "Box", new Color(0.14f, 0.14f, 0.18f, 1f));
        Anchor(box.GetComponent<RectTransform>(), 0.15f, 0.35f, 0.85f, 0.65f, 0, 0, 0, 0);

        var msg = Label(box.transform, "MessageText", "", 24, TextAlignmentOptions.Center);
        msg.enableWordWrapping = true;
        Anchor(msg.rectTransform, 0.05f, 0.45f, 0.95f, 0.95f, 0, 0, 0, 0);

        var confirmBtn = Btn(box.transform, "ConfirmButton", "OK",     24);
        var cancelBtn  = Btn(box.transform, "CancelButton",  "Cancel", 24);
        Anchor(confirmBtn.GetComponent<RectTransform>(), 0.53f, 0.05f, 0.95f, 0.42f, 0, 0, 0, 0);
        Anchor(cancelBtn.GetComponent<RectTransform>(),  0.05f, 0.05f, 0.47f, 0.42f, 0, 0, 0, 0);

        dialogPanel.SetActive(false);

        Wire(comp,
            ("_panel",         (UnityEngine.Object)dialogPanel),
            ("_messageText",   msg),
            ("_confirmButton", confirmBtn.GetComponent<Button>()),
            ("_cancelButton",  cancelBtn.GetComponent<Button>()));
    }

    // ── UI Primitives ─────────────────────────────────────────────────────────

    static GameObject ScreenRoot(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero;
        return go;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.alignment = align; tmp.color = Color.white;
        return tmp;
    }

    static GameObject Btn(Transform parent, string name, string label, float fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.24f, 0.34f, 0.55f, 1f);
        go.AddComponent<Button>();
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = fontSize; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        var tr = textGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
        return go;
    }

    static GameObject InputField(Transform parent, string name, string placeholder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f, 1f);
        var field = go.AddComponent<TMP_InputField>();

        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(go.transform, false);
        textArea.AddComponent<Image>().color = Color.clear; // ensures RectTransform is present
        textArea.AddComponent<RectMask2D>();
        var taRect = textArea.GetComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero; taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10, 4); taRect.offsetMax = new Vector2(-10, -4);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textArea.transform, false);
        var textComp = textGO.AddComponent<TextMeshProUGUI>();
        textComp.fontSize = 20; textComp.color = Color.white;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.sizeDelta = Vector2.zero;

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textArea.transform, false);
        var phComp = phGO.AddComponent<TextMeshProUGUI>();
        phComp.text = placeholder; phComp.fontSize = 20;
        phComp.color = new Color(0.55f, 0.55f, 0.55f, 1f);
        phComp.fontStyle = FontStyles.Italic;
        var phRect = phGO.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one; phRect.sizeDelta = Vector2.zero;

        field.textViewport = taRect;
        field.textComponent = textComp;
        field.placeholder = phComp;
        return go;
    }

    static GameObject Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static (GameObject scroll, GameObject content) ScrollView(Transform parent, string name)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);
        var scrollRect = root.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(root.transform, false);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one; vpRect.sizeDelta = Vector2.zero;
        scrollRect.viewport = vpRect;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        content.AddComponent<Image>().color = Color.clear; // ensures RectTransform is present
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f); contentRect.sizeDelta = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4; vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;

        return (root, content);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Batch-set serialized fields using a SerializedObject.</summary>
    static void Wire(UnityEngine.Object target, params (string field, UnityEngine.Object value)[] pairs)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in pairs)
        {
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[ProjectSetup] Field not found on {target.name}: {field}"); continue; }
            prop.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Set anchor min/max and offset min/max on a RectTransform.</summary>
    static void Anchor(RectTransform rt,
        float ancMinX, float ancMinY, float ancMaxX, float ancMaxY,
        float offMinX, float offMinY, float offMaxX, float offMaxY)
    {
        rt.anchorMin = new Vector2(ancMinX, ancMinY);
        rt.anchorMax = new Vector2(ancMaxX, ancMaxY);
        rt.offsetMin = new Vector2(offMinX, offMinY);
        rt.offsetMax = new Vector2(offMaxX, offMaxY);
    }

    /// <summary>Stretch a RectTransform to fill its parent with optional insets.</summary>
    static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(right, top);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        var parent = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var full = parent + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, parts[i]);
            parent = full;
        }
    }
}
