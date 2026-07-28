/* =========================================
   GameManager.cs — Main Game Coordinator
   Singleton pattern, scene management,
   subsystem orchestration
   ========================================= */

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SyncBreaker.Core
{
    /// <summary>
    /// Central game coordinator. Persists across scenes.
    /// Manages state transitions, subsystem references, and global game data.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ──
        public static GameManager Instance { get; private set; }

        // ── Subsystem References ──
        [Header("Subsystems")]
        public StateManager State { get; private set; }
        public Gameplay.LevelManager Levels { get; private set; }
        public Systems.EnergySystem Energy { get; private set; }
        public Systems.SaveSystem Save { get; private set; }
        public Gameplay.TouchInputHandler Input { get; private set; }

        // ── Global Game Data ──
        [Header("Game Configuration")]
        [SerializeField] private int targetFrameRate = 60;

        /// <summary>
        /// The level index currently selected in the Hub.
        /// -1 means no level selected.
        /// </summary>
        public int SelectedLevelIndex { get; set; } = -1;

        /// <summary>
        /// Context data passed between states (e.g., score, reason for lockpick).
        /// </summary>
        public StateContext CurrentContext { get; set; }

        // ════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════

        private void Awake()
        {
            // Singleton enforcement
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize subsystems
            InitializeSubsystems();

            // Frame rate
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            // Force landscape orientation
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            // Start in MENU state
            State.ChangeState(GameState.Menu);
        }

        private void Update()
        {
            // Global update - subsystems that need per-frame updates
            State.UpdateState(Time.deltaTime);
        }

        // ════════════════════════════════════════
        //  INITIALIZATION
        // ════════════════════════════════════════

        private void InitializeSubsystems()
        {
            // State Manager
            State = gameObject.AddComponent<StateManager>();

            // Level Manager
            Levels = gameObject.AddComponent<Gameplay.LevelManager>();

            // Energy System
            Energy = gameObject.AddComponent<Systems.EnergySystem>();

            // Input Handler
            Input = gameObject.AddComponent<Gameplay.TouchInputHandler>();

            // Save System (not a MonoBehaviour)
            Save = new Systems.SaveSystem();

            // Register all state handlers
            RegisterStateHandlers();

            Debug.Log("[GameManager] All subsystems initialized.");
        }

        // ════════════════════════════════════════
        //  SCENE MANAGEMENT
        // ════════════════════════════════════════

        /// <summary>
        /// Load a scene by name. Used for transitioning between Menu, Hub, Gameplay, Lockpick.
        /// </summary>
        public void LoadScene(string sceneName, System.Action onLoaded = null)
        {
            StartCoroutine(LoadSceneAsync(sceneName, onLoaded));
        }

        private System.Collections.IEnumerator LoadSceneAsync(string sceneName, System.Action onLoaded)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
            {
                yield return null;
            }
            onLoaded?.Invoke();
        }

        // ════════════════════════════════════════
        //  STATE HANDLER REGISTRATION
        // ════════════════════════════════════════

        private void RegisterStateHandlers()
        {
            // Level State Handler
            var levelHandler = GetComponentInChildren<Gameplay.LevelStateHandler>();
            if (levelHandler == null)
            {
                levelHandler = gameObject.AddComponent<Gameplay.LevelStateHandler>();
            }
            var gameplayUI = FindAnyObjectByType<UI.GameplayUI>();
            levelHandler.Initialize(Levels, Input, gameplayUI);
            State.RegisterState(GameState.Level, levelHandler);

            // Lockpick State Handler
            var lockpickHandler = GetComponentInChildren<Gameplay.LockpickStateHandler>();
            if (lockpickHandler == null)
            {
                lockpickHandler = gameObject.AddComponent<Gameplay.LockpickStateHandler>();
            }
            var lockpickUI = FindAnyObjectByType<UI.LockpickUI>();
            lockpickHandler.Initialize(Levels, Input);
            State.RegisterState(GameState.Lockpick, lockpickHandler);

            // Menu State Handler
            var menuHandler = GetComponentInChildren<Gameplay.MenuStateHandler>();
            if (menuHandler == null)
            {
                menuHandler = gameObject.AddComponent<Gameplay.MenuStateHandler>();
            }
            var menuUI = FindAnyObjectByType<UI.MainMenuUI>();
            menuHandler.Initialize(menuUI);
            State.RegisterState(GameState.Menu, menuHandler);

            // Hub State Handler
            var hubHandler = GetComponentInChildren<Gameplay.HubStateHandler>();
            if (hubHandler == null)
            {
                hubHandler = gameObject.AddComponent<Gameplay.HubStateHandler>();
            }
            var hubUI = FindAnyObjectByType<UI.HubUI>();
            hubHandler.Initialize(hubUI, Levels);
            State.RegisterState(GameState.Hub, hubHandler);

            // Game Over State Handler (handles both GameOver and EndlessOver)
            var gameOverHandler = GetComponentInChildren<Gameplay.GameOverStateHandler>();
            if (gameOverHandler == null)
            {
                gameOverHandler = gameObject.AddComponent<Gameplay.GameOverStateHandler>();
            }
            var gameOverUI = FindAnyObjectByType<UI.GameOverUI>();
            gameOverHandler.Initialize(gameOverUI);
            State.RegisterState(GameState.GameOver, gameOverHandler);
            State.RegisterState(GameState.EndlessOver, gameOverHandler);

            // Endless State Handler
            var endlessHandler = GetComponentInChildren<Gameplay.EndlessStateHandler>();
            if (endlessHandler == null)
            {
                endlessHandler = gameObject.AddComponent<Gameplay.EndlessStateHandler>();
            }
            endlessHandler.Initialize(Levels, Input, gameplayUI);
            State.RegisterState(GameState.Endless, endlessHandler);

            Debug.Log("[GameManager] All state handlers registered.");
        }

        // ════════════════════════════════════════
        //  UTILITY
        // ════════════════════════════════════════

        /// <summary>
        /// Reset all progress (called from Hub reset button).
        /// </summary>
        public void ResetAllData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[GameManager] All data reset.");

            // Reload the current scene to refresh everything
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Save?.SaveSettings();
                Levels?.SaveProgress();
                Debug.Log("[GameManager] App paused — data saved.");
            }
        }

        private void OnApplicationQuit()
        {
            Save?.SaveSettings();
            Levels?.SaveProgress();
            Debug.Log("[GameManager] App quit — data saved.");
        }
    }

    /// <summary>
    /// Data container passed between state transitions.
    /// Replaces the JavaScript "context" object pattern.
    /// </summary>
    [System.Serializable]
    public class StateContext
    {
        public string Reason;          // "shortcut", "revive", "endless_revive"
        public int LevelIndex = -1;
        public int Score;
        public int Difficulty = 1;
        public bool NoRevive;
        public bool UsedRevive;

        // Endless mode context
        public int EndlessScore;
        public int EndlessWave = 1;
        public int EndlessMaxCombo;
        public int EndlessHitCount;

        public StateContext() { }

        public StateContext(string reason, int levelIndex = -1)
        {
            Reason = reason;
            LevelIndex = levelIndex;
        }
    }
}
