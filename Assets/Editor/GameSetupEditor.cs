/* =========================================
   GameSetupEditor.cs — Auto-Generate Assets
   
   Editor script to create all necessary:
   - LevelData ScriptableObject assets
   - GameplayScene.unity with full UI setup
   - GameManager prefab
   - UI prefabs (heartPrefab, nodePrefab)
   
   Run via: Tools → SyncBreaker → Setup Project
   ========================================= */

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using SyncBreaker.Core;
using SyncBreaker.UI;
using SyncBreaker.Gameplay;

namespace SyncBreaker.Editor
{
    public class GameSetupEditor : EditorWindow
    {
        [MenuItem("Tools/SyncBreaker/Setup Project (Full)", false, 0)]
        public static void SetupFull()
        {
            GenerateLevelDataAssets();
            GenerateGameplayScene();
            AssetDatabase.Refresh();
            Debug.Log("[GameSetup] Full project setup complete!");
        }

        [MenuItem("Tools/SyncBreaker/Generate Level Data Assets", false, 1)]
        public static void GenerateLevelDataOnly()
        {
            GenerateLevelDataAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameSetup] Level data assets generated.");
        }

        [MenuItem("Tools/SyncBreaker/Generate Gameplay Scene", false, 2)]
        public static void GenerateSceneOnly()
        {
            GenerateGameplayScene();
            AssetDatabase.Refresh();
            Debug.Log("[GameSetup] Gameplay scene generated.");
        }

        // ════════════════════════════════════════
        //  LEVEL DATA ASSETS
        // ════════════════════════════════════════

        private static void GenerateLevelDataAssets()
        {
            string dir = "Assets/ScriptableObjects/LevelData";
            EnsureDirectory(dir);

            var configs = new[]
            {
                new { id = 1, name = "CLEAR_LOGS", desc = "Delete attacker log files from the system",
                      diff = 1, speed = 0.7f, target = 0.22f, hits = 8, time = 25f, lives = 3, lockpick = 1 },
                new { id = 2, name = "CLOSE_PORTS", desc = "Shut down open backdoor connections",
                      diff = 2, speed = 0.9f, target = 0.18f, hits = 10, time = 28f, lives = 3, lockpick = 2 },
                new { id = 3, name = "REMOVE_MALWARE", desc = "Detect and remove malicious software",
                      diff = 3, speed = 1.1f, target = 0.15f, hits = 12, time = 30f, lives = 3, lockpick = 3 },
                new { id = 4, name = "RESET_CREDS", desc = "Reset compromised access credentials",
                      diff = 4, speed = 1.4f, target = 0.12f, hits = 14, time = 35f, lives = 4, lockpick = 4 },
                new { id = 5, name = "FIREWALL", desc = "Rebuild the system firewall barriers",
                      diff = 5, speed = 1.7f, target = 0.10f, hits = 16, time = 40f, lives = 4, lockpick = 5 },
                new { id = 6, name = "CUT_ACCESS", desc = "Completely sever attacker connection",
                      diff = 6, speed = 2.0f, target = 0.08f, hits = 18, time = 60f, lives = 5, lockpick = 6 },
            };

            foreach (var cfg in configs)
            {
                string path = $"{dir}/Level_{cfg.id}_{cfg.name}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Gameplay.LevelData>(path);

                Gameplay.LevelData asset;
                if (existing != null)
                {
                    asset = existing;
                }
                else
                {
                    asset = ScriptableObject.CreateInstance<Gameplay.LevelData>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                asset.id = cfg.id;
                asset.levelName = cfg.name;
                asset.description = cfg.desc;
                asset.difficulty = cfg.diff;
                asset.barSpeed = cfg.speed;
                asset.targetSize = cfg.target;
                asset.requiredHits = cfg.hits;
                asset.maxTime = cfg.time;
                asset.startingLives = cfg.lives;
                asset.lockpickDifficulty = cfg.lockpick;
                asset.speedIncrement = 0.015f;
                asset.maxBarSpeed = 3.5f;
                asset.perfectScore = 100;
                asset.goodScore = 50;
                asset.perfectThreshold = 0.35f;
                asset.springForce = 5f;
                asset.barDamping = 0.5f;
                asset.shakeIntensity = 0.3f;

                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[GameSetup] Generated 6 LevelData assets.");
        }

        // ════════════════════════════════════════
        //  GAMEPLAY SCENE
        // ════════════════════════════════════════

        private static void GenerateGameplayScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Main Camera ──
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.09f); // #0a0c17
            cam.orthographic = false;
            camGo.transform.position = new Vector3(0, 0, -10f);
            camGo.tag = "MainCamera";

            // ── EventSystem ──
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // ── GameManager ──
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<Core.GameManager>();

            // ── Canvas ──
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Create UI Panels ──
            var mainMenuPanel = CreateUIPanel(canvasGo.transform, "MainMenuPanel");
            var hubPanel = CreateUIPanel(canvasGo.transform, "HubPanel");
            var gameplayPanel = CreateUIPanel(canvasGo.transform, "GameplayPanel");
            var lockpickPanel = CreateUIPanel(canvasGo.transform, "LockpickPanel");
            var gameOverPanel = CreateUIPanel(canvasGo.transform, "GameOverPanel");

            // ── Configure MainMenuPanel ──
            SetupMainMenuUI(mainMenuPanel);

            // ── Configure HubPanel ──
            SetupHubUI(hubPanel);

            // ── Configure GameplayPanel ──
            SetupGameplayUI(gameplayPanel);

            // ── Configure LockpickPanel ──
            SetupLockpickUI(lockpickPanel);

            // ── Configure GameOverPanel ──
            SetupGameOverUI(gameOverPanel);

            // ── Save Scene ──
            string sceneDir = "Assets/Scenes";
            EnsureDirectory(sceneDir);
            string scenePath = $"{sceneDir}/GameplayScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log("[GameSetup] Gameplay scene created and saved.");
        }

        // ════════════════════════════════════════
        //  PANEL CREATION HELPERS
        // ════════════════════════════════════════

        private static GameObject CreateUIPanel(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go;
        }

        private static GameObject CreateUIElement(string name, Transform parent,
            float anchorX = 0.5f, float anchorY = 0.5f, float width = 200f, float height = 100f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;

            return go;
        }

        private static TextMeshProUGUI CreateText(GameObject parent, string text, int fontSize = 24,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(parent.transform, false);

            var rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return tmp;
        }

        private static Button CreateButton(GameObject parent, string text, int fontSize = 20)
        {
            var btn = parent.AddComponent<Button>();

            // Image
            var img = parent.AddComponent<Image>();
            img.color = new Color(0.23f, 0.51f, 0.96f, 1f); // #3b82f6

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(parent.transform, false);

            var rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            // ColorBlock
            var colors = btn.colors;
            colors.normalColor = new Color(0.23f, 0.51f, 0.96f, 1f);
            colors.highlightedColor = new Color(0.37f, 0.61f, 0.96f, 1f);
            colors.pressedColor = new Color(0.15f, 0.38f, 0.80f, 1f);
            colors.disabledColor = new Color(0.2f, 0.25f, 0.33f, 1f);
            btn.colors = colors;

            return btn;
        }

        // ════════════════════════════════════════
        //  MAIN MENU UI
        // ════════════════════════════════════════

        private static void SetupMainMenuUI(GameObject panel)
        {
            var menuUI = panel.AddComponent<UI.MainMenuUI>();

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(panel.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.09f, 0.95f);

            // Title
            var titleGo = CreateUIElement("Title", panel.transform, 0.5f, 0.7f, 800f, 120f);
            CreateText(titleGo, "SYNCBREAKER", 72);

            // Subtitle
            var subGo = CreateUIElement("Subtitle", panel.transform, 0.5f, 0.62f, 800f, 50f);
            CreateText(subGo, "Defend the system. Break the breach.", 22)
                .color = new Color(0.39f, 0.45f, 0.55f, 1f);

            // Play button
            var playGo = CreateUIElement("PlayButton", panel.transform, 0.5f, 0.45f, 360f, 80f);
            var playBtn = CreateButton(playGo, "SYNC BREAK", 28);
            // Set via serialized field lookup
            SetPrivateField(menuUI, "playButton", playBtn);
            SetPrivateField(menuUI, "playButtonText", playGo.GetComponentInChildren<TextMeshProUGUI>());
            SetPrivateField(menuUI, "titleText", titleGo.GetComponentInChildren<TextMeshProUGUI>());
            SetPrivateField(menuUI, "subtitleText", subGo.GetComponentInChildren<TextMeshProUGUI>());
            SetPrivateField(menuUI, "backgroundPanel", bgImg);

            // Settings button
            var settingsGo = CreateUIElement("SettingsButton", panel.transform, 0.92f, 0.08f, 120f, 50f);
            var settingsBtn = CreateButton(settingsGo, "SETTNGS", 14);
            SetPrivateField(menuUI, "settingsButton", settingsBtn);

            // Version text
            var verGo = CreateUIElement("VersionText", panel.transform, 0.5f, 0.08f, 300f, 40f);
            CreateText(verGo, "v0.1.0", 16).color = new Color(0.27f, 0.32f, 0.39f, 1f);
            SetPrivateField(menuUI, "versionText", verGo.GetComponentInChildren<TextMeshProUGUI>());

            panel.SetActive(false);
        }

        // ════════════════════════════════════════
        //  HUB UI
        // ════════════════════════════════════════

        private static void SetupHubUI(GameObject panel)
        {
            var hubUI = panel.AddComponent<UI.HubUI>();

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(panel.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f, 0.95f);

            // Hub Title
            var titleGo = CreateUIElement("HubTitle", panel.transform, 0.5f, 0.92f, 600f, 60f);
            CreateText(titleGo, "NETWORK NODES", 36);

            // Energy text
            var energyGo = CreateUIElement("EnergyText", panel.transform, 0.85f, 0.95f, 300f, 40f);
            CreateText(energyGo, "ENERGY: 10/10", 18);

            // Level list container (Scroll View)
            var scrollGo = new GameObject("LevelListContainer");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.05f, 0.15f);
            scrollRt.anchorMax = new Vector2(0.7f, 0.85f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var scrollMask = scrollGo.AddComponent<Image>();
            scrollMask.color = new Color(0.06f, 0.08f, 0.12f, 1f);
            var scrollMask2 = scrollGo.AddComponent<Mask>();

            // Content inside scroll
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 600f);
            var vertLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vertLayout.spacing = 10f;
            vertLayout.padding = new RectOffset(20, 20, 20, 20);
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = true;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childForceExpandHeight = false;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Create level button prefab
            var levelBtnPrefabGo = new GameObject("LevelButton_0");
            levelBtnPrefabGo.transform.SetParent(contentGo.transform, false);
            var levelBtnPrefabRt = levelBtnPrefabGo.AddComponent<RectTransform>();
            levelBtnPrefabRt.sizeDelta = new Vector2(0, 80f);
            var levelBtnPrefabImg = levelBtnPrefabGo.AddComponent<Image>();
            levelBtnPrefabImg.color = new Color(0.08f, 0.11f, 0.19f, 1f);
            levelBtnPrefabGo.AddComponent<Button>();

            var levelNameTextGo = new GameObject("NameText");
            levelNameTextGo.transform.SetParent(levelBtnPrefabGo.transform, false);
            var levelNameRt = levelNameTextGo.AddComponent<RectTransform>();
            levelNameRt.anchorMin = new Vector2(0.05f, 0.3f);
            levelNameRt.anchorMax = new Vector2(0.5f, 0.8f);
            levelNameRt.offsetMin = Vector2.zero; levelNameRt.offsetMax = Vector2.zero;
            var levelNameTmp = levelNameTextGo.AddComponent<TextMeshProUGUI>();
            levelNameTmp.text = "LEVEL_NAME";
            levelNameTmp.fontSize = 22;
            levelNameTmp.alignment = TextAlignmentOptions.Left;
            levelNameTmp.color = Color.white;

            var levelDescTextGo = new GameObject("DescriptionText");
            levelDescTextGo.transform.SetParent(levelBtnPrefabGo.transform, false);
            var levelDescRt = levelDescTextGo.AddComponent<RectTransform>();
            levelDescRt.anchorMin = new Vector2(0.05f, 0f);
            levelDescRt.anchorMax = new Vector2(0.5f, 0.35f);
            levelDescRt.offsetMin = Vector2.zero; levelDescRt.offsetMax = Vector2.zero;
            var levelDescTmp = levelDescTextGo.AddComponent<TextMeshProUGUI>();
            levelDescTmp.text = "desc";
            levelDescTmp.fontSize = 14;
            levelDescTmp.alignment = TextAlignmentOptions.Left;
            levelDescTmp.color = new Color(0.39f, 0.45f, 0.55f, 1f);

            var levelStatusGo = new GameObject("StatusText");
            levelStatusGo.transform.SetParent(levelBtnPrefabGo.transform, false);
            var levelStatusRt = levelStatusGo.AddComponent<RectTransform>();
            levelStatusRt.anchorMin = new Vector2(0.55f, 0.3f);
            levelStatusRt.anchorMax = new Vector2(0.9f, 0.8f);
            levelStatusRt.offsetMin = Vector2.zero; levelStatusRt.offsetMax = Vector2.zero;
            var levelStatusTmp = levelStatusGo.AddComponent<TextMeshProUGUI>();
            levelStatusTmp.text = "LOCKED";
            levelStatusTmp.fontSize = 20;
            levelStatusTmp.alignment = TextAlignmentOptions.Right;
            levelStatusTmp.color = new Color(0.39f, 0.45f, 0.55f, 1f);

            // Save level button prefab
            string prefabDir = "Assets/Prefabs";
            EnsureDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/LevelButton.prefab";
            PrefabUtility.SaveAsPrefabAsset(levelBtnPrefabGo, prefabPath);
            Object.DestroyImmediate(levelBtnPrefabGo);

            SetPrivateField(hubUI, "levelListContainer", contentGo.transform);
            SetPrivateField(hubUI, "levelButtonPrefab",
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            SetPrivateField(hubUI, "hubTitleText", titleGo.GetComponentInChildren<TextMeshProUGUI>());
            SetPrivateField(hubUI, "energyText", energyGo.GetComponentInChildren<TextMeshProUGUI>());

            // Endless button
            var endlessGo = CreateUIElement("EndlessButton", panel.transform, 0.85f, 0.3f, 250f, 80f);
            var endlessBtn = CreateButton(endlessGo, "ENDLESS MODE", 22);
            SetPrivateField(hubUI, "endlessButton", endlessBtn);

            // Endless best
            var endlessBestGo = CreateUIElement("EndlessBestText", panel.transform, 0.85f, 0.23f, 250f, 40f);
            CreateText(endlessBestGo, "", 16);
            SetPrivateField(hubUI, "endlessBestText", endlessBestGo.GetComponentInChildren<TextMeshProUGUI>());

            // Back button
            var backGo = CreateUIElement("BackButton", panel.transform, 0.08f, 0.06f, 160f, 50f);
            var backBtn = CreateButton(backGo, "DISCONNECT", 16);
            SetPrivateField(hubUI, "backButton", backBtn);

            // Reset button
            var resetGo = CreateUIElement("ResetButton", panel.transform, 0.92f, 0.06f, 140f, 50f);
            var resetBtn = CreateButton(resetGo, "RESET ALL", 14);
            SetPrivateField(hubUI, "resetButton", resetBtn);

            // Reset confirm
            var confirmGo = CreateUIElement("ResetConfirm", panel.transform, 0.5f, 0.5f, 400f, 150f);
            confirmGo.AddComponent<Image>().color = new Color(0.06f, 0.09f, 0.16f, 1f);
            CreateText(confirmGo, "Are you sure?\nThis will delete all progress.", 18).transform.SetParent(confirmGo.transform);
            SetPrivateField(hubUI, "resetConfirmPanel", confirmGo);

            panel.SetActive(false);
        }

        // ════════════════════════════════════════
        //  GAMEPLAY UI
        // ════════════════════════════════════════

        private static void SetupGameplayUI(GameObject panel)
        {
            var gameplayUI = panel.AddComponent<UI.GameplayUI>();

            // Score
            var scoreGo = CreateUIElement("ScoreText", panel.transform, 0.5f, 0.92f, 300f, 50f);
            CreateText(scoreGo, "000000", 36);
            SetPrivateField(gameplayUI, "scoreText", scoreGo.GetComponentInChildren<TextMeshProUGUI>());

            // Combo
            var comboGo = CreateUIElement("ComboText", panel.transform, 0.5f, 0.86f, 200f, 40f);
            CreateText(comboGo, "", 24).color = Utils.HexToColor("#f59e0b");
            SetPrivateField(gameplayUI, "comboText", comboGo.GetComponentInChildren<TextMeshProUGUI>());

            // Level Name
            var nameGo = CreateUIElement("LevelNameText", panel.transform, 0.2f, 0.12f, 350f, 40f);
            CreateText(nameGo, "LEVEL", 22).alignment = TextAlignmentOptions.Left;
            SetPrivateField(gameplayUI, "levelNameText", nameGo.GetComponentInChildren<TextMeshProUGUI>());

            // Level Description
            var descGo = CreateUIElement("LevelDescText", panel.transform, 0.2f, 0.08f, 350f, 30f);
            CreateText(descGo, "", 16).alignment = TextAlignmentOptions.Left;
            SetPrivateField(gameplayUI, "levelDescText", descGo.GetComponentInChildren<TextMeshProUGUI>());

            // Progress
            var progGo = CreateUIElement("ProgressText", panel.transform, 0.8f, 0.12f, 350f, 40f);
            CreateText(progGo, "Hit: 0/8", 22).alignment = TextAlignmentOptions.Right;
            SetPrivateField(gameplayUI, "progressText", progGo.GetComponentInChildren<TextMeshProUGUI>());

            // Timer
            var timerGo = CreateUIElement("TimerText", panel.transform, 0.8f, 0.08f, 350f, 30f);
            CreateText(timerGo, "Time: 0:00", 18).alignment = TextAlignmentOptions.Right;
            SetPrivateField(gameplayUI, "timerText", timerGo.GetComponentInChildren<TextMeshProUGUI>());

            // Lives container
            var livesGo = new GameObject("LivesContainer");
            livesGo.transform.SetParent(panel.transform, false);
            var livesRt = livesGo.AddComponent<RectTransform>();
            livesRt.anchorMin = new Vector2(0.05f, 0.92f);
            livesRt.anchorMax = new Vector2(0.3f, 0.97f);
            livesRt.offsetMin = Vector2.zero; livesRt.offsetMax = Vector2.zero;
            var livesLayout = livesGo.AddComponent<HorizontalLayoutGroup>();
            livesLayout.spacing = 8f;
            livesLayout.childAlignment = TextAnchor.MiddleLeft;
            SetPrivateField(gameplayUI, "livesContainer", livesGo.transform);

            // Heart prefab
            var heartGo = new GameObject("Heart");
            var heartRt = heartGo.AddComponent<RectTransform>();
            heartRt.sizeDelta = new Vector2(40f, 40f);
            var heartImg = heartGo.AddComponent<Image>();
            heartImg.color = new Color(0.94f, 0.27f, 0.27f, 1f); // Red heart
            heartGo.AddComponent<LayoutElement>();

            string prefabDir = "Assets/Prefabs";
            EnsureDirectory(prefabDir);
            string heartPrefabPath = $"{prefabDir}/Heart.prefab";
            PrefabUtility.SaveAsPrefabAsset(heartGo, heartPrefabPath);
            Object.DestroyImmediate(heartGo);
            SetPrivateField(gameplayUI, "heartPrefab",
                AssetDatabase.LoadAssetAtPath<GameObject>(heartPrefabPath));

            // Hit result text
            var resultGo = CreateUIElement("HitResultText", panel.transform, 0.5f, 0.5f, 500f, 100f);
            CreateText(resultGo, "", 48);
            SetPrivateField(gameplayUI, "hitResultText", resultGo.GetComponentInChildren<TextMeshProUGUI>());

            // Timing bar elements
            var barGo = new GameObject("BarBackground");
            barGo.transform.SetParent(panel.transform, false);
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.1f, 0.2f);
            barRt.anchorMax = new Vector2(0.9f, 0.28f);
            barRt.offsetMin = Vector2.zero; barRt.offsetMax = Vector2.zero;
            var barImg = barGo.AddComponent<Image>();
            barImg.color = new Color(0.06f, 0.09f, 0.16f, 0.8f);
            SetPrivateField(gameplayUI, "barBackground", barRt);

            // Target zone
            var zoneGo = new GameObject("TargetZone");
            zoneGo.transform.SetParent(barGo.transform, false);
            var zoneRt = zoneGo.AddComponent<RectTransform>();
            zoneRt.anchorMin = Vector2.zero;
            zoneRt.anchorMax = new Vector2(0.22f, 1f);
            zoneRt.offsetMin = new Vector2(0, 4f);
            zoneRt.offsetMax = new Vector2(0, -4f);
            var zoneImg = zoneGo.AddComponent<Image>();
            zoneImg.color = new Color(0.13f, 0.77f, 0.37f, 0.6f);
            SetPrivateField(gameplayUI, "targetZoneRect", zoneRt);
            SetPrivateField(gameplayUI, "targetZoneImage", zoneImg);

            // Indicator
            var indGo = new GameObject("Indicator");
            indGo.transform.SetParent(barGo.transform, false);
            var indRt = indGo.AddComponent<RectTransform>();
            indRt.anchorMin = new Vector2(0, 0);
            indRt.anchorMax = new Vector2(0, 1);
            indRt.sizeDelta = new Vector2(6f, 0f);
            indRt.offsetMin = new Vector2(-3f, -6f);
            indRt.offsetMax = new Vector2(3f, 6f);
            var indImg = indGo.AddComponent<Image>();
            indImg.color = Color.white;
            SetPrivateField(gameplayUI, "indicatorRect", indRt);
            SetPrivateField(gameplayUI, "indicatorImage", indImg);

            // Wave text (endless)
            var waveGo = CreateUIElement("WaveText", panel.transform, 0.5f, 0.78f, 300f, 50f);
            CreateText(waveGo, "WAVE 1", 28).color = Utils.HexToColor("#f59e0b");
            waveGo.SetActive(false);
            SetPrivateField(gameplayUI, "waveText", waveGo.GetComponentInChildren<TextMeshProUGUI>());

            // Wave progress
            var waveSliderGo = CreateUIElement("WaveSlider", panel.transform, 0.5f, 0.74f, 400f, 20f);
            var waveSlider = waveSliderGo.AddComponent<Slider>();
            var sliderBg = waveSliderGo.AddComponent<Image>();
            sliderBg.color = new Color(0.06f, 0.09f, 0.16f, 1f);
            waveSliderGo.SetActive(false);
            SetPrivateField(gameplayUI, "waveProgressSlider", waveSlider);

            // Wave flash overlay
            var flashGo = CreateUIElement("WaveFlashOverlay", panel.transform, 0.5f, 0.5f, 1920f, 1080f);
            flashGo.AddComponent<Image>().color = new Color(1, 1, 1, 0);
            flashGo.SetActive(false);
            SetPrivateField(gameplayUI, "waveFlashOverlay", flashGo);

            // Transition overlay
            var transGo = CreateUIElement("TransitionOverlay", panel.transform, 0.5f, 0.5f, 1920f, 1080f);
            var transImg = transGo.AddComponent<Image>();
            transImg.color = new Color(0.04f, 0.05f, 0.09f, 0);
            transGo.SetActive(false);
            SetPrivateField(gameplayUI, "transitionOverlay", transImg);

            panel.SetActive(false);
        }

        // ════════════════════════════════════════
        //  LOCKPICK UI
        // ════════════════════════════════════════

        private static void SetupLockpickUI(GameObject panel)
        {
            var lockpickUI = panel.AddComponent<UI.LockpickUI>();

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(panel.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f, 0.95f);

            // Circle center
            var centerGo = CreateUIElement("CircleCenter", panel.transform, 0.5f, 0.55f, 300f, 300f);
            SetPrivateField(lockpickUI, "circleCenter", centerGo.GetComponent<RectTransform>());

            // Cursor dot
            var cursorGo = CreateUIElement("CursorDot", centerGo.transform, 0.5f, 0.5f, 16f, 16f);
            var cursorImg = cursorGo.AddComponent<Image>();
            cursorImg.color = new Color(0.23f, 0.51f, 0.96f, 1f);
            SetPrivateField(lockpickUI, "cursorDot", cursorGo.GetComponent<RectTransform>());
            SetPrivateField(lockpickUI, "cursorDotImage", cursorImg);

            // Cursor line
            var lineGo = CreateUIElement("CursorLine", centerGo.transform, 0.5f, 0.5f, 140f, 2f);
            lineGo.AddComponent<Image>().color = new Color(0.23f, 0.51f, 0.96f, 0.3f);
            SetPrivateField(lockpickUI, "cursorLine", lineGo.GetComponent<RectTransform>());

            // Decorative rings
            var outerRingGo = CreateUIElement("OuterRing", centerGo.transform, 0.5f, 0.5f, 340f, 340f);
            outerRingGo.AddComponent<Image>().color = new Color(0.1f, 0.13f, 0.2f, 0.5f);
            SetPrivateField(lockpickUI, "outerRing", outerRingGo.GetComponent<RectTransform>());

            var innerRingGo = CreateUIElement("InnerRing", centerGo.transform, 0.5f, 0.5f, 250f, 250f);
            innerRingGo.AddComponent<Image>().color = new Color(0.15f, 0.18f, 0.25f, 0.3f);
            SetPrivateField(lockpickUI, "innerRing", innerRingGo.GetComponent<RectTransform>());

            // Lock icon
            var lockGo = CreateUIElement("LockIcon", centerGo.transform, 0.5f, 0.5f, 50f, 50f);
            var lockImg = lockGo.AddComponent<Image>();
            lockImg.color = new Color(0.12f, 0.14f, 0.17f, 1f);
            SetPrivateField(lockpickUI, "lockBodyImage", lockImg);
            SetPrivateField(lockpickUI, "lockShackleImage", lockImg);

            // Title
            var titleGo = CreateUIElement("TitleText", panel.transform, 0.5f, 0.92f, 600f, 50f);
            CreateText(titleGo, "CODE BREAKER", 32).color = Utils.HexToColor("#f59e0b");
            SetPrivateField(lockpickUI, "titleText", titleGo.GetComponentInChildren<TextMeshProUGUI>());

            // Subtitle
            var subGo = CreateUIElement("SubtitleText", panel.transform, 0.5f, 0.87f, 600f, 40f);
            CreateText(subGo, "Break the security code", 18);
            SetPrivateField(lockpickUI, "subtitleText", subGo.GetComponentInChildren<TextMeshProUGUI>());

            // Progress
            var progressGo = CreateUIElement("ProgressSlider", panel.transform, 0.5f, 0.2f, 500f, 30f);
            var progressSlider = progressGo.AddComponent<Slider>();
            progressGo.AddComponent<Image>().color = new Color(0.06f, 0.09f, 0.16f, 1f);
            SetPrivateField(lockpickUI, "progressSlider", progressSlider);

            // Progress text
            var progTextGo = CreateUIElement("ProgressText", panel.transform, 0.5f, 0.16f, 200f, 30f);
            CreateText(progTextGo, "0 / 5", 18);
            SetPrivateField(lockpickUI, "progressText", progTextGo.GetComponentInChildren<TextMeshProUGUI>());

            // Hint
            var hintGo = CreateUIElement("HintText", panel.transform, 0.5f, 0.08f, 600f, 30f);
            CreateText(hintGo, "Swipe in any direction to start", 16);
            SetPrivateField(lockpickUI, "hintText", hintGo.GetComponentInChildren<TextMeshProUGUI>());

            // Result
            var resultGo = CreateUIElement("ResultText", panel.transform, 0.5f, 0.5f, 500f, 100f);
            CreateText(resultGo, "", 48);
            resultGo.SetActive(false);
            SetPrivateField(lockpickUI, "resultText", resultGo.GetComponentInChildren<TextMeshProUGUI>());

            // Node prefab
            var nodeGo = new GameObject("Node");
            var nodeRt = nodeGo.AddComponent<RectTransform>();
            nodeRt.sizeDelta = new Vector2(32f, 32f);

            var nodeBgGo = new GameObject("Background");
            nodeBgGo.transform.SetParent(nodeGo.transform, false);
            var nodeBgRt = nodeBgGo.AddComponent<RectTransform>();
            nodeBgRt.anchorMin = Vector2.zero; nodeBgRt.anchorMax = Vector2.one;
            nodeBgRt.offsetMin = Vector2.zero; nodeBgRt.offsetMax = Vector2.zero;
            nodeBgGo.AddComponent<Image>().color = new Color(0.06f, 0.09f, 0.16f, 0.8f);

            var nodeBorderGo = new GameObject("Border");
            nodeBorderGo.transform.SetParent(nodeGo.transform, false);
            var nodeBorderRt = nodeBorderGo.AddComponent<RectTransform>();
            nodeBorderRt.anchorMin = Vector2.zero; nodeBorderRt.anchorMax = Vector2.one;
            nodeBorderRt.offsetMin = Vector2.zero; nodeBorderRt.offsetMax = Vector2.zero;
            nodeBorderGo.AddComponent<Image>().color = new Color(0.2f, 0.25f, 0.33f, 1f);

            var nodeTextGo = new GameObject("DirectionText");
            nodeTextGo.transform.SetParent(nodeGo.transform, false);
            var nodeTextRt = nodeTextGo.AddComponent<RectTransform>();
            nodeTextRt.anchorMin = Vector2.zero; nodeTextRt.anchorMax = Vector2.one;
            nodeTextRt.offsetMin = Vector2.zero; nodeTextRt.offsetMax = Vector2.zero;
            var nodeTmp = nodeTextGo.AddComponent<TextMeshProUGUI>();
            nodeTmp.text = "";
            nodeTmp.fontSize = 18;
            nodeTmp.alignment = TextAlignmentOptions.Center;

            string prefabDir = "Assets/Prefabs";
            EnsureDirectory(prefabDir);
            string nodePrefabPath = $"{prefabDir}/LockpickNode.prefab";
            PrefabUtility.SaveAsPrefabAsset(nodeGo, nodePrefabPath);
            Object.DestroyImmediate(nodeGo);
            SetPrivateField(lockpickUI, "nodePrefab",
                AssetDatabase.LoadAssetAtPath<GameObject>(nodePrefabPath));

            panel.SetActive(false);
        }

        // ════════════════════════════════════════
        //  GAME OVER UI
        // ════════════════════════════════════════

        private static void SetupGameOverUI(GameObject panel)
        {
            var gameOverUI = panel.AddComponent<UI.GameOverUI>();

            // Background overlay
            var bg = new GameObject("Background");
            bg.transform.SetParent(panel.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.09f, 0.85f);
            SetPrivateField(gameOverUI, "backgroundOverlay", bgImg);

            // Title
            var titleGo = CreateUIElement("GameOverTitle", panel.transform, 0.5f, 0.82f, 600f, 60f);
            CreateText(titleGo, "CONNECTION LOST", 42).color = Utils.HexToColor("#ef4444");
            SetPrivateField(gameOverUI, "gameOverTitle", titleGo.GetComponentInChildren<TextMeshProUGUI>());

            // Score label
            var scoreLabelGo = CreateUIElement("ScoreLabel", panel.transform, 0.5f, 0.75f, 300f, 40f);
            CreateText(scoreLabelGo, "SYNAPSE SCORE", 18);
            SetPrivateField(gameOverUI, "scoreLabel", scoreLabelGo.GetComponentInChildren<TextMeshProUGUI>());

            // Score
            var scoreGo = CreateUIElement("ScoreText", panel.transform, 0.5f, 0.68f, 400f, 70f);
            CreateText(scoreGo, "000000", 56);
            SetPrivateField(gameOverUI, "scoreText", scoreGo.GetComponentInChildren<TextMeshProUGUI>());

            // Score highlight
            var highlightGo = new GameObject("ScoreHighlight");
            highlightGo.transform.SetParent(panel.transform, false);
            var highlightRt = highlightGo.AddComponent<RectTransform>();
            highlightRt.anchorMin = new Vector2(0.3f, 0.66f);
            highlightRt.anchorMax = new Vector2(0.7f, 0.73f);
            highlightRt.offsetMin = Vector2.zero; highlightRt.offsetMax = Vector2.zero;
            highlightGo.AddComponent<Image>().color = new Color(0.23f, 0.51f, 0.96f, 0.1f);
            SetPrivateField(gameOverUI, "scoreHighlight", highlightGo);

            // Story panel
            var storyGo = new GameObject("StoryPanel");
            storyGo.transform.SetParent(panel.transform, false);
            var storyRt = storyGo.AddComponent<RectTransform>();
            storyRt.anchorMin = Vector2.zero; storyRt.anchorMax = Vector2.one;
            storyRt.offsetMin = Vector2.zero; storyRt.offsetMax = Vector2.zero;
            SetPrivateField(gameOverUI, "storyPanel", storyGo);

            // Endless panel
            var endlessGo = new GameObject("EndlessPanel");
            endlessGo.transform.SetParent(panel.transform, false);
            var endlessRt = endlessGo.AddComponent<RectTransform>();
            endlessRt.anchorMin = Vector2.zero; endlessRt.anchorMax = Vector2.one;
            endlessRt.offsetMin = Vector2.zero; endlessRt.offsetMax = Vector2.zero;
            SetPrivateField(gameOverUI, "endlessPanel", endlessGo);

            // Wave text (in endless panel)
            var waveGo = CreateUIElement("WaveText", endlessGo.transform, 0.5f, 0.6f, 300f, 40f);
            CreateText(waveGo, "WAVE 1", 28).color = Utils.HexToColor("#f59e0b");
            SetPrivateField(gameOverUI, "waveText", waveGo.GetComponentInChildren<TextMeshProUGUI>());

            // Max combo text
            var comboGo = CreateUIElement("MaxComboText", endlessGo.transform, 0.5f, 0.55f, 300f, 30f);
            CreateText(comboGo, "MAX COMBO: x1", 18);
            SetPrivateField(gameOverUI, "maxComboText", comboGo.GetComponentInChildren<TextMeshProUGUI>());

            // Hit count text
            var hitsGo = CreateUIElement("HitCountText", endlessGo.transform, 0.5f, 0.5f, 300f, 30f);
            CreateText(hitsGo, "TOTAL HITS: 0", 18);
            SetPrivateField(gameOverUI, "hitCountText", hitsGo.GetComponentInChildren<TextMeshProUGUI>());

            // Revive button
            var reviveGo = CreateUIElement("ReviveButton", panel.transform, 0.5f, 0.38f, 400f, 70f);
            var reviveBtn = CreateButton(reviveGo, "ATTEMPT RECOVERY", 22);
            SetPrivateField(gameOverUI, "reviveButton", reviveBtn);
            SetPrivateField(gameOverUI, "reviveButtonText", reviveGo.GetComponentInChildren<TextMeshProUGUI>());

            // Revive locked indicator
            var lockedGo = CreateUIElement("ReviveLockedIndicator", reviveGo.transform, 0.92f, 0.5f, 30f, 30f);
            lockedGo.AddComponent<Image>().color = Utils.HexToColor("#ef4444");
            lockedGo.SetActive(false);
            SetPrivateField(gameOverUI, "reviveLockedIndicator", lockedGo);

            // Retry button
            var retryGo = CreateUIElement("RetryButton", panel.transform, 0.35f, 0.25f, 250f, 60f);
            var retryBtn = CreateButton(retryGo, "RETRY", 20);
            SetPrivateField(gameOverUI, "retryButton", retryBtn);

            // Menu button
            var menuGo = CreateUIElement("MenuButton", panel.transform, 0.65f, 0.25f, 250f, 60f);
            var menuBtn = CreateButton(menuGo, "MAIN MENU", 20);
            SetPrivateField(gameOverUI, "menuButton", menuBtn);

            panel.SetActive(false);
        }

        // ════════════════════════════════════════
        //  UTILITY
        // ════════════════════════════════════════

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                // Try property
                var prop = type.GetProperty(fieldName,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(target, value);
                }
                else
                {
                    Debug.LogWarning($"[GameSetup] Could not set '{fieldName}' on {type.Name}");
                }
            }
        }
    }
}
