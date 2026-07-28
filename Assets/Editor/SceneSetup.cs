/* =========================================
   SceneSetup.cs — One-Click Scene Builder
   
   Unity Editor utility that builds the entire
   SyncBreaker game scene from scratch:
   - Camera setup (2D, dark background)
   - Canvas + EventSystem (UI)
   - GameManager (singleton)
   - All gameplay systems
   - All UI elements
   
   Usage: Unity Menu → SyncBreaker → Setup Scene
   ========================================= */

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
using SyncBreaker.Core;
using SyncBreaker.Gameplay;
using SyncBreaker.UI;
using SyncBreaker.Systems;

public class SceneSetup : EditorWindow
{
    [MenuItem("SyncBreaker/Setup Game Scene %#g")]
    public static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog(
            "SyncBreaker — Scene Setup",
            "Bu işlem mevcut sahneye tüm oyun objelerini ekleyecek.\n\nDevam etmek istiyor musunuz?",
            "Evet, Kur!", "İptal"))
        {
            return;
        }

        Debug.Log("[SceneSetup] ═══════════════════════════════════");
        Debug.Log("[SceneSetup] SyncBreaker sahne kurulumu başlıyor...");

        // ── 1. CAMERA ──
        SetupCamera();

        // ── 2. GAME MANAGER (Root) ──
        var gameRoot = CreateGameManager();

        // ── 3. CANVAS + EVENT SYSTEM ──
        var canvas = CreateCanvas();

        // ── 4. TIMING BAR VISUAL ──
        CreateTimingBarUI(canvas.transform);

        // ── 5. GAMEPLAY HUD ──
        CreateGameplayHUD(canvas.transform);

        // ── 6. LOCKPICK UI ──
        CreateLockpickUI(canvas.transform);

        // ── 7. MENU PANELS ──
        CreateMenuPanels(canvas.transform);

        // ── 8. BACKGROUND ──
        CreateBackground();

        // ── 9. PARTICLE SYSTEM ──
        CreateParticleSystem();

        Debug.Log("[SceneSetup] ✅ Sahne kurulumu tamamlandı!");
        Debug.Log("[SceneSetup] ═══════════════════════════════════");
        Debug.Log("[SceneSetup] ▶ Play butonuna basarak test edebilirsiniz.");

        // Mark scene as dirty so it can be saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    // ════════════════════════════════════════
    //  1. CAMERA
    // ════════════════════════════════════════

    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";
        }

        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.09f, 1f); // Dark cyber bg
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        Debug.Log("[SceneSetup] ✓ Camera configured (2D Orthographic)");
    }

    // ════════════════════════════════════════
    //  2. GAME MANAGER
    // ════════════════════════════════════════

    static GameObject CreateGameManager()
    {
        // Root object
        var root = new GameObject("═══ GAME MANAGER ═══");

        // GameManager
        root.AddComponent<GameManager>();

        // StateManager
        root.AddComponent<StateManager>();

        // TouchInputHandler
        root.AddComponent<TouchInputHandler>();

        // LevelManager
        root.AddComponent<LevelManager>();

        // ThemeManager
        root.AddComponent<ThemeManager>();

        // TimingBar
        var barGo = new GameObject("TimingBar");
        barGo.transform.SetParent(root.transform);
        barGo.AddComponent<TimingBar>();

        // LockpickSystem
        var lockGo = new GameObject("LockpickSystem");
        lockGo.transform.SetParent(root.transform);
        lockGo.AddComponent<LockpickSystem>();

        // State Handlers
        var handlersGo = new GameObject("StateHandlers");
        handlersGo.transform.SetParent(root.transform);
        handlersGo.AddComponent<LevelStateHandler>();
        handlersGo.AddComponent<LockpickStateHandler>();

        Debug.Log("[SceneSetup] ✓ GameManager + all systems created");
        return root;
    }

    // ════════════════════════════════════════
    //  3. CANVAS + EVENT SYSTEM
    // ════════════════════════════════════════

    static GameObject CreateCanvas()
    {
        // Event System
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        // Main Canvas
        var canvasGo = new GameObject("═══ UI CANVAS ═══");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        Debug.Log("[SceneSetup] ✓ Canvas + EventSystem created");
        return canvasGo;
    }

    // ════════════════════════════════════════
    //  4. TIMING BAR VISUAL
    // ════════════════════════════════════════

    static void CreateTimingBarUI(Transform parent)
    {
        var barPanel = CreatePanel(parent, "TimingBarPanel",
            new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f),
            new Vector2(0f, 0f), new Vector2(700f, 60f));

        // Bar Background
        var barBg = CreateUIElement<Image>(barPanel.transform, "BarBackground",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        barBg.color = new Color(0.1f, 0.12f, 0.18f, 0.8f);
        AddRoundedCorners(barBg.gameObject, 8f);

        // Target Zone
        var targetZone = CreateUIElement<Image>(barBg.transform, "TargetZone",
            new Vector2(0.35f, 0f), new Vector2(0.55f, 1f),
            Vector2.zero, Vector2.zero);
        targetZone.color = new Color(0.13f, 0.77f, 0.37f, 0.4f);

        // Indicator (moving dot)
        var indicator = CreateUIElement<Image>(barBg.transform, "Indicator",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(12f, 50f));
        indicator.color = Color.white;

        Debug.Log("[SceneSetup] ✓ Timing Bar UI created");
    }

    // ════════════════════════════════════════
    //  5. GAMEPLAY HUD
    // ════════════════════════════════════════

    static void CreateGameplayHUD(Transform parent)
    {
        var hudPanel = CreatePanel(parent, "GameplayHUD",
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        hudPanel.GetComponent<Image>().color = Color.clear; // Transparent

        var hudComp = hudPanel.AddComponent<GameplayUI>();

        // ── Score (top-right) ──
        var scoreText = CreateText(hudPanel.transform, "ScoreText",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-120f, -40f), new Vector2(200f, 50f),
            "0", 36, TextAlignmentOptions.Right);
        scoreText.color = new Color(0.95f, 0.96f, 0.98f, 1f);

        // ── Combo (below score) ──
        var comboText = CreateText(hudPanel.transform, "ComboText",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-120f, -80f), new Vector2(200f, 40f),
            "x1", 28, TextAlignmentOptions.Right);
        comboText.color = new Color(0.96f, 0.62f, 0.04f, 1f);
        comboText.gameObject.SetActive(false);

        // ── Level Name (top-left) ──
        var levelName = CreateText(hudPanel.transform, "LevelNameText",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(120f, -40f), new Vector2(300f, 40f),
            "LEVEL 1", 24, TextAlignmentOptions.Left);
        levelName.color = new Color(0.96f, 0.62f, 0.04f, 1f);

        // ── Level Description ──
        var levelDesc = CreateText(hudPanel.transform, "LevelDescText",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(120f, -75f), new Vector2(400f, 30f),
            "Clear the logs", 16, TextAlignmentOptions.Left);
        levelDesc.color = new Color(0.58f, 0.63f, 0.73f, 1f);

        // ── Timer (top-center) ──
        var timerText = CreateText(hudPanel.transform, "TimerText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -40f), new Vector2(200f, 40f),
            "Time: 25.0", 22, TextAlignmentOptions.Center);

        // ── Progress (below timer) ──
        var progressText = CreateText(hudPanel.transform, "ProgressText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -75f), new Vector2(200f, 30f),
            "Hit: 0/8", 18, TextAlignmentOptions.Center);
        progressText.color = new Color(0.58f, 0.63f, 0.73f, 1f);

        // ── Hit Result (center screen) ──
        var hitResult = CreateText(hudPanel.transform, "HitResultText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 60f), new Vector2(400f, 80f),
            "PERFECT", 42, TextAlignmentOptions.Center);
        hitResult.color = new Color(0.13f, 0.77f, 0.37f, 1f);
        hitResult.gameObject.SetActive(false);

        // ── Lives Container (bottom-left) ──
        var livesContainer = new GameObject("LivesContainer");
        livesContainer.transform.SetParent(hudPanel.transform, false);
        var livesRT = livesContainer.AddComponent<RectTransform>();
        livesRT.anchorMin = new Vector2(0f, 0f);
        livesRT.anchorMax = new Vector2(0f, 0f);
        livesRT.anchoredPosition = new Vector2(80f, 50f);
        livesRT.sizeDelta = new Vector2(150f, 40f);
        var livesLayout = livesContainer.AddComponent<HorizontalLayoutGroup>();
        livesLayout.spacing = 8f;

        // Heart prefab (simple red circle)
        var heartPrefab = CreateHeartPrefab();

        Debug.Log("[SceneSetup] ✓ Gameplay HUD created (score, combo, timer, lives, hit result)");
    }

    // ════════════════════════════════════════
    //  6. LOCKPICK UI
    // ════════════════════════════════════════

    static void CreateLockpickUI(Transform parent)
    {
        var lockPanel = CreatePanel(parent, "LockpickPanel",
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        lockPanel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f, 0.95f);
        lockPanel.SetActive(false); // Hidden until lockpick starts

        var lockUI = lockPanel.AddComponent<LockpickUI>();

        // ── Title ──
        var title = CreateText(lockPanel.transform, "LockpickTitle",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -50f), new Vector2(400f, 50f),
            "CODE BREAKER", 32, TextAlignmentOptions.Center);
        title.color = new Color(0.96f, 0.62f, 0.04f, 1f);

        // ── Subtitle ──
        var subtitle = CreateText(lockPanel.transform, "LockpickSubtitle",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -90f), new Vector2(500f, 30f),
            "Break the security code", 16, TextAlignmentOptions.Center);
        subtitle.color = new Color(0.58f, 0.63f, 0.73f, 1f);

        // ── Circle Center (node container) ──
        var circleCenter = new GameObject("CircleCenter");
        circleCenter.transform.SetParent(lockPanel.transform, false);
        var circRT = circleCenter.AddComponent<RectTransform>();
        circRT.anchorMin = circRT.anchorMax = new Vector2(0.5f, 0.5f);
        circRT.anchoredPosition = Vector2.zero;
        circRT.sizeDelta = new Vector2(300f, 300f);

        // ── Outer Ring (decorative) ──
        var outerRing = CreateUIElement<Image>(circleCenter.transform, "OuterRing",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(310f, 310f));
        outerRing.color = new Color(0.2f, 0.25f, 0.33f, 0.2f);

        // ── Inner Ring ──
        var innerRing = CreateUIElement<Image>(circleCenter.transform, "InnerRing",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(260f, 260f));
        innerRing.color = new Color(0.2f, 0.25f, 0.33f, 0.1f);

        // ── Cursor Dot ──
        var cursorDot = CreateUIElement<Image>(circleCenter.transform, "CursorDot",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 140f), new Vector2(16f, 16f));
        cursorDot.color = Color.white;

        // ── Progress Bar ──
        var progressGo = new GameObject("LockpickProgress");
        progressGo.transform.SetParent(lockPanel.transform, false);
        var progressRT = progressGo.AddComponent<RectTransform>();
        progressRT.anchorMin = new Vector2(0.3f, 0.1f);
        progressRT.anchorMax = new Vector2(0.7f, 0.1f);
        progressRT.anchoredPosition = new Vector2(0f, 30f);
        progressRT.sizeDelta = new Vector2(0f, 20f);
        var slider = progressGo.AddComponent<Slider>();
        slider.interactable = false;

        // Slider background
        var sliderBg = CreateUIElement<Image>(progressGo.transform, "SliderBg",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        sliderBg.color = new Color(0.1f, 0.12f, 0.18f, 0.8f);

        // Slider fill
        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(progressGo.transform, false);
        var fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = Vector2.zero;
        fillAreaRT.offsetMax = Vector2.zero;

        var fill = CreateUIElement<Image>(fillArea.transform, "Fill",
            Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        fill.color = new Color(0.23f, 0.51f, 0.96f, 1f);
        slider.fillRect = fill.rectTransform;

        // ── Progress Text ──
        var progressText = CreateText(lockPanel.transform, "LockpickProgressText",
            new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f),
            new Vector2(0f, 55f), new Vector2(100f, 25f),
            "0 / 4", 14, TextAlignmentOptions.Center);

        // ── Hint Text ──
        var hint = CreateText(lockPanel.transform, "LockpickHint",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 60f), new Vector2(500f, 30f),
            "Swipe in any direction to start", 14, TextAlignmentOptions.Center);
        hint.color = new Color(0.58f, 0.63f, 0.73f, 0.7f);

        // ── Result Text ──
        var result = CreateText(lockPanel.transform, "LockpickResult",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(400f, 60f),
            "ACCESS GRANTED", 36, TextAlignmentOptions.Center);
        result.color = new Color(0.13f, 0.77f, 0.37f, 1f);
        result.gameObject.SetActive(false);

        Debug.Log("[SceneSetup] ✓ Lockpick UI panel created");
    }

    // ════════════════════════════════════════
    //  7. MENU PANELS
    // ════════════════════════════════════════

    static void CreateMenuPanels(Transform parent)
    {
        // ── MAIN MENU ──
        var menuPanel = CreatePanel(parent, "MainMenuPanel",
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        menuPanel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f, 1f);

        // Title
        var titleText = CreateText(menuPanel.transform, "GameTitle",
            new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f),
            Vector2.zero, new Vector2(500f, 80f),
            "SYNCBREAKER", 56, TextAlignmentOptions.Center);
        titleText.color = new Color(0.23f, 0.51f, 0.96f, 1f);
        titleText.fontStyle = FontStyles.Bold;

        // Subtitle
        CreateText(menuPanel.transform, "GameSubtitle",
            new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f),
            Vector2.zero, new Vector2(400f, 30f),
            "HACK THE SYSTEM", 18, TextAlignmentOptions.Center).color =
            new Color(0.58f, 0.63f, 0.73f, 1f);

        // Play Button
        CreateButton(menuPanel.transform, "PlayButton",
            new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f),
            Vector2.zero, new Vector2(280f, 60f),
            "▶  START", new Color(0.23f, 0.51f, 0.96f, 1f));

        // Endless Button
        CreateButton(menuPanel.transform, "EndlessButton",
            new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f),
            Vector2.zero, new Vector2(280f, 50f),
            "∞  ENDLESS MODE", new Color(0.49f, 0.27f, 0.96f, 0.8f));

        // Version Text
        CreateText(menuPanel.transform, "VersionText",
            new Vector2(0.5f, 0.05f), new Vector2(0.5f, 0.05f),
            Vector2.zero, new Vector2(200f, 25f),
            "v1.0.0", 12, TextAlignmentOptions.Center).color =
            new Color(0.39f, 0.45f, 0.55f, 0.5f);

        // ── HUB PANEL ──
        var hubPanel = CreatePanel(parent, "HubPanel",
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        hubPanel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f, 1f);
        hubPanel.SetActive(false);

        CreateText(hubPanel.transform, "HubTitle",
            new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f),
            Vector2.zero, new Vector2(400f, 50f),
            "NODE MAP", 32, TextAlignmentOptions.Center).color =
            new Color(0.96f, 0.62f, 0.04f, 1f);

        // ── GAME OVER PANEL ──
        var gameOverPanel = CreatePanel(parent, "GameOverPanel",
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        gameOverPanel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f, 0.95f);
        gameOverPanel.SetActive(false);

        CreateText(gameOverPanel.transform, "GameOverTitle",
            new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f),
            Vector2.zero, new Vector2(400f, 60f),
            "CONNECTION LOST", 40, TextAlignmentOptions.Center).color =
            new Color(0.94f, 0.27f, 0.27f, 1f);

        CreateButton(gameOverPanel.transform, "RetryButton",
            new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f),
            Vector2.zero, new Vector2(250f, 55f),
            "↻  RETRY", new Color(0.23f, 0.51f, 0.96f, 1f));

        CreateButton(gameOverPanel.transform, "ReviveButton",
            new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f),
            Vector2.zero, new Vector2(250f, 50f),
            "🔓  REVIVE", new Color(0.96f, 0.62f, 0.04f, 0.9f));

        // ── TRANSITION OVERLAY ──
        var overlay = CreateUIElement<Image>(parent, "TransitionOverlay",
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        overlay.color = new Color(0.04f, 0.05f, 0.09f, 0f); // Start transparent
        overlay.raycastTarget = false;

        Debug.Log("[SceneSetup] ✓ Menu panels created (MainMenu, Hub, GameOver, Transition)");
    }

    // ════════════════════════════════════════
    //  8. BACKGROUND
    // ════════════════════════════════════════

    static void CreateBackground()
    {
        var bgRoot = new GameObject("═══ BACKGROUND ═══");
        bgRoot.AddComponent<BackgroundGrid>();

        Debug.Log("[SceneSetup] ✓ Background system created");
    }

    // ════════════════════════════════════════
    //  9. PARTICLE SYSTEM
    // ════════════════════════════════════════

    static void CreateParticleSystem()
    {
        var fxRoot = new GameObject("═══ PARTICLE FX ═══");
        fxRoot.AddComponent<ParticleEffects>();
        fxRoot.AddComponent<ParticleRenderer>();

        Debug.Log("[SceneSetup] ✓ Particle effects system created");
    }

    // ════════════════════════════════════════
    //  UTILITY HELPERS
    // ════════════════════════════════════════

    static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = Color.clear;
        return go;
    }

    static T CreateUIElement<T>(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return go.AddComponent<T>();
    }

    static TextMeshProUGUI CreateText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        string text, int fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = new Color(0.95f, 0.96f, 0.98f, 1f);
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    static Button CreateButton(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        string label, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        var img = go.AddComponent<Image>();
        img.color = bgColor;
        AddRoundedCorners(go, 12f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;

        // Label
        var labelTmp = CreateText(go.transform, "Label",
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero,
            label, 20, TextAlignmentOptions.Center);
        labelTmp.color = Color.white;
        labelTmp.fontStyle = FontStyles.Bold;

        return btn;
    }

    static GameObject CreateHeartPrefab()
    {
        // Create a heart prefab in Resources
        var heart = new GameObject("HeartIcon");
        var img = heart.AddComponent<Image>();
        img.color = new Color(0.94f, 0.27f, 0.27f, 1f);
        var rt = heart.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(28f, 28f);

        // Save as prefab
        string prefabPath = "Assets/Prefabs/HeartIcon.prefab";
        EnsureDirectoryExists(prefabPath);
        PrefabUtility.SaveAsPrefabAsset(heart, prefabPath);
        Object.DestroyImmediate(heart);

        Debug.Log("[SceneSetup] ✓ Heart prefab created at " + prefabPath);
        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    static void AddRoundedCorners(GameObject go, float radius)
    {
        // Unity doesn't have built-in rounded corners for Image.
        // This is a placeholder — real implementation would use
        // a custom shader or a 9-sliced sprite with rounded edges.
        // For now, we just tag it so we know it needs rounding.
        // A custom RoundedImage component could be added later.
    }

    static void EnsureDirectoryExists(string assetPath)
    {
        string dir = System.IO.Path.GetDirectoryName(assetPath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }
    }
}
#endif
