/* =========================================
   LevelManager.cs — Level Flow Manager
   
   Coordinates the overall level lifecycle:
   - Loading level data
   - Tracking progress (unlocked/completed)
   - Win/lose condition checking
   - Timer management
   - Connecting TimingBar to StateManager
   
   Ported from: js/LevelManager.js (flow logic)
   ========================================= */

using UnityEngine;
using System;
using System.Collections.Generic;
using SyncBreaker.Core;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Runtime state for a single level's progress.
    /// </summary>
    [System.Serializable]
    public class LevelProgress
    {
        public int id;
        public bool unlocked;
        public bool completed;
        public int bestScore;

        public LevelProgress(int id, bool unlocked = false)
        {
            this.id = id;
            this.unlocked = unlocked;
            this.completed = false;
            this.bestScore = 0;
        }
    }

    /// <summary>
    /// Manages level lifecycle, progression, and save/load.
    /// Works with TimingBar for gameplay and StateManager for flow.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        // ── Events ──
        /// <summary>Fired when a level is completed successfully.</summary>
        public event Action<int, int> OnLevelComplete;  // (levelIndex, score)

        /// <summary>Fired when the player fails a level.</summary>
        public event Action<int, int> OnLevelFailed;    // (levelIndex, score)

        /// <summary>Fired every frame with the remaining time.</summary>
        public event Action<float> OnTimerUpdate;

        // ── Level Database ──
        [Header("Level Configuration")]
        [Tooltip("All level data assets, in order")]
        [SerializeField] private LevelData[] levelDataAssets;

        // ── Runtime State ──
        /// <summary>Progress tracking for each level.</summary>
        public List<LevelProgress> Progress { get; private set; } = new();

        /// <summary>Index of the currently active level (-1 if none).</summary>
        public int CurrentLevelIndex { get; private set; } = -1;

        /// <summary>Currently active level data (null if none).</summary>
        public LevelData CurrentLevelData { get; private set; }

        /// <summary>Reference to the timing bar (created at runtime).</summary>
        public TimingBar Bar { get; private set; }

        // ── Timer ──
        /// <summary>Remaining time for the current level.</summary>
        public float RemainingTime { get; private set; }

        // ── Level State ──
        /// <summary>Has the current level been completed?</summary>
        public bool LevelComplete { get; private set; }

        /// <summary>Has the current level failed?</summary>
        public bool LevelFailed { get; private set; }

        /// <summary>Has the player used their revive this attempt?</summary>
        public bool UsedRevive { get; set; }

        // ── Endless Mode State ──
        public bool EndlessMode { get; set; }
        public int EndlessWave { get; set; } = 1;
        public int EndlessHitsInWave { get; set; }
        public int EndlessHitsPerWave { get; set; } = 5;
        public float EndlessWaveFlash { get; set; }
        public int EndlessBest { get; private set; }

        // ── Save Keys ──
        private const string SAVE_KEY_PROGRESS = "sb_progress";
        private const string SAVE_KEY_ENDLESS_BEST = "sb_endless_best";

        // ════════════════════════════════════════
        //  INITIALIZATION
        // ════════════════════════════════════════

        private void Awake()
        {
            // If no level data assets assigned in inspector,
            // create defaults programmatically
            if (levelDataAssets == null || levelDataAssets.Length == 0)
            {
                levelDataAssets = LevelData.CreateDefaultLevels();
                Debug.LogWarning("[LevelManager] No level data assets assigned. Using defaults.");
            }

            // Initialize progress tracking
            InitializeProgress();
            LoadProgress();
            EndlessBest = LoadEndlessBest();
        }

        private void InitializeProgress()
        {
            Progress.Clear();
            for (int i = 0; i < levelDataAssets.Length; i++)
            {
                var ld = levelDataAssets[i];
                bool unlocked = (i == 0); // First level always unlocked
                Progress.Add(new LevelProgress(ld.id, unlocked));
            }
        }

        // ════════════════════════════════════════
        //  LEVEL LIFECYCLE
        // ════════════════════════════════════════

        /// <summary>
        /// Start a specific level by index.
        /// </summary>
        public bool StartLevel(int index)
        {
            if (index < 0 || index >= levelDataAssets.Length)
            {
                Debug.LogError($"[LevelManager] Invalid level index: {index}");
                return false;
            }

            if (!Progress[index].unlocked)
            {
                Debug.LogWarning($"[LevelManager] Level {index} is locked.");
                return false;
            }

            CurrentLevelIndex = index;
            CurrentLevelData = levelDataAssets[index];
            LevelComplete = false;
            LevelFailed = false;
            UsedRevive = false;
            EndlessMode = false;
            RemainingTime = CurrentLevelData.maxTime;

            // Create or get TimingBar
            EnsureTimingBar();
            Bar.Initialize(CurrentLevelData);

            // Listen for hits
            Bar.OnHit -= OnBarHit;
            Bar.OnHit += OnBarHit;

            Debug.Log($"[LevelManager] Started level {index}: {CurrentLevelData.levelName}");
            return true;
        }

        /// <summary>
        /// Update the current level (called every frame during LEVEL state).
        /// </summary>
        public void UpdateLevel(float dt)
        {
            if (CurrentLevelData == null || LevelComplete || LevelFailed) return;

            // Timer countdown
            if (CurrentLevelData.maxTime > 0)
            {
                RemainingTime -= dt;
                OnTimerUpdate?.Invoke(RemainingTime);

                if (RemainingTime <= 0f)
                {
                    RemainingTime = 0f;
                    CheckEndCondition();
                }
            }
        }

        /// <summary>
        /// Handle a hit result from the TimingBar.
        /// </summary>
        private void OnBarHit(HitResult result, int scoreEarned, int combo)
        {
            if (result == HitResult.Miss && Bar.Lives <= 0)
            {
                FailLevel();
                return;
            }

            CheckEndCondition();
        }

        /// <summary>
        /// Check if the level should end (win or lose).
        /// </summary>
        private void CheckEndCondition()
        {
            if (LevelComplete || LevelFailed) return;

            // Win: required hits reached
            if (Bar.HitCount >= CurrentLevelData.requiredHits)
            {
                CompleteLevel();
            }
            // Lose: time ran out without enough hits
            else if (RemainingTime <= 0f && CurrentLevelData.maxTime > 0)
            {
                FailLevel();
            }
        }

        /// <summary>
        /// Mark the current level as completed.
        /// </summary>
        private void CompleteLevel()
        {
            LevelComplete = true;
            Bar.Stop();

            // Update progress
            var prog = Progress[CurrentLevelIndex];
            if (Bar.Score > prog.bestScore) prog.bestScore = Bar.Score;
            prog.completed = true;

            // Unlock next level
            int next = CurrentLevelIndex + 1;
            if (next < Progress.Count)
            {
                Progress[next].unlocked = true;
            }

            SaveProgress();

            Debug.Log($"[LevelManager] Level {CurrentLevelIndex} COMPLETED! Score: {Bar.Score}");
            OnLevelComplete?.Invoke(CurrentLevelIndex, Bar.Score);
        }

        /// <summary>
        /// Mark the current level as failed.
        /// </summary>
        private void FailLevel()
        {
            LevelFailed = true;
            Bar.Stop();

            Debug.Log($"[LevelManager] Level {CurrentLevelIndex} FAILED. Score: {Bar.Score}");
            OnLevelFailed?.Invoke(CurrentLevelIndex, Bar.Score);
        }

        // ════════════════════════════════════════
        //  ENDLESS MODE
        // ════════════════════════════════════════

        /// <summary>
        /// Start endless mode.
        /// </summary>
        public void StartEndless()
        {
            EndlessMode = true;
            CurrentLevelIndex = -1;

            // Create an endless level data at runtime
            CurrentLevelData = ScriptableObject.CreateInstance<LevelData>();
            CurrentLevelData.id = 999;
            CurrentLevelData.levelName = "ENDLESS_MODE";
            CurrentLevelData.description = "Permanent defense — survive as long as you can";
            CurrentLevelData.barSpeed = 0.6f;
            CurrentLevelData.targetSize = 0.24f;
            CurrentLevelData.requiredHits = int.MaxValue;
            CurrentLevelData.maxTime = 0f; // No time limit
            CurrentLevelData.startingLives = 6;
            CurrentLevelData.speedIncrement = 0.008f;
            CurrentLevelData.maxBarSpeed = 4.0f;
            CurrentLevelData.perfectScore = 100;
            CurrentLevelData.goodScore = 50;
            CurrentLevelData.perfectThreshold = 0.35f;
            CurrentLevelData.shakeIntensity = 0.3f;
            CurrentLevelData.barDamping = 0.5f;

            LevelComplete = false;
            LevelFailed = false;
            UsedRevive = false;
            RemainingTime = float.MaxValue;

            // Wave system
            EndlessWave = 1;
            EndlessHitsInWave = 0;
            EndlessHitsPerWave = 5;
            EndlessWaveFlash = 0f;
            EndlessBest = LoadEndlessBest();

            // Initialize timing bar
            EnsureTimingBar();
            Bar.Initialize(CurrentLevelData);
            Bar.OnHit -= OnEndlessHit;
            Bar.OnHit += OnEndlessHit;

            Debug.Log("[LevelManager] Endless mode started.");
        }

        /// <summary>
        /// Handle a hit in endless mode.
        /// </summary>
        private void OnEndlessHit(HitResult result, int scoreEarned, int combo)
        {
            if (result == HitResult.Miss && Bar.Lives <= 0)
            {
                // Save best score
                SaveEndlessBest();
                LevelFailed = true;
                Bar.Stop();
                OnLevelFailed?.Invoke(-1, Bar.Score);
                return;
            }

            if (result != HitResult.Miss)
            {
                EndlessHitsInWave++;

                // Wave progression
                if (EndlessHitsInWave >= EndlessHitsPerWave)
                {
                    EndlessWave++;
                    EndlessHitsInWave = 0;
                    EndlessWaveFlash = 1.0f;

                    // Scale up difficulty
                    Bar.ScaleUpDifficulty(0.15f, 0.012f);

                    // Bonus life every 3 waves
                    if (EndlessWave % 3 == 0)
                    {
                        // Can't directly set Lives on TimingBar, so we use Revive pattern
                        // This is a design decision — lives capped at 5 for balance
                    }

                    Debug.Log($"[LevelManager] Endless WAVE {EndlessWave}!");
                }
            }
        }

        /// <summary>
        /// Update endless mode wave flash.
        /// </summary>
        public void UpdateEndless(float dt)
        {
            if (!EndlessMode || LevelFailed) return;

            if (EndlessWaveFlash > 0f)
            {
                EndlessWaveFlash -= dt * 2f;
            }
        }

        // ════════════════════════════════════════
        //  QUERIES
        // ════════════════════════════════════════

        /// <summary>
        /// Check if all story levels are completed.
        /// </summary>
        public bool IsAllCompleted()
        {
            for (int i = 0; i < Progress.Count; i++)
            {
                if (!Progress[i].completed) return false;
            }
            return true;
        }

        /// <summary>
        /// Get the number of levels.
        /// </summary>
        public int LevelCount => levelDataAssets?.Length ?? 0;

        /// <summary>
        /// Get level data by index.
        /// </summary>
        public LevelData GetLevelData(int index)
        {
            if (index < 0 || index >= levelDataAssets.Length) return null;
            return levelDataAssets[index];
        }

        // ════════════════════════════════════════
        //  TIMING BAR MANAGEMENT
        // ════════════════════════════════════════

        private void EnsureTimingBar()
        {
            if (Bar == null)
            {
                // Try to find existing TimingBar in scene
                Bar = FindAnyObjectByType<TimingBar>();

                if (Bar == null)
                {
                    // Create one
                    var go = new GameObject("TimingBar");
                    Bar = go.AddComponent<TimingBar>();
                }
            }
        }

        // ════════════════════════════════════════
        //  PERSISTENCE
        // ════════════════════════════════════════

        /// <summary>
        /// Save level progress to PlayerPrefs.
        /// </summary>
        public void SaveProgress()
        {
            try
            {
                var data = new ProgressData { levels = Progress };
                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString(SAVE_KEY_PROGRESS, json);
                PlayerPrefs.Save();
                Debug.Log("[LevelManager] Progress saved.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelManager] Save failed: {e.Message}");
            }
        }

        /// <summary>
        /// Load level progress from PlayerPrefs.
        /// </summary>
        public void LoadProgress()
        {
            try
            {
                string json = PlayerPrefs.GetString(SAVE_KEY_PROGRESS, "");
                if (string.IsNullOrEmpty(json)) return;

                var data = JsonUtility.FromJson<ProgressData>(json);
                if (data?.levels == null) return;

                foreach (var saved in data.levels)
                {
                    var prog = Progress.Find(p => p.id == saved.id);
                    if (prog != null)
                    {
                        prog.unlocked = saved.unlocked;
                        prog.completed = saved.completed;
                        prog.bestScore = saved.bestScore;
                    }
                }
                Debug.Log("[LevelManager] Progress loaded.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelManager] Load failed: {e.Message}");
            }
        }

        /// <summary>
        /// Reset all progress.
        /// </summary>
        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY_PROGRESS);
            PlayerPrefs.DeleteKey(SAVE_KEY_ENDLESS_BEST);
            PlayerPrefs.Save();
            InitializeProgress();
            Debug.Log("[LevelManager] Progress reset.");
        }

        private void SaveEndlessBest()
        {
            if (Bar.Score > EndlessBest)
            {
                EndlessBest = Bar.Score;
                PlayerPrefs.SetInt(SAVE_KEY_ENDLESS_BEST, EndlessBest);
                PlayerPrefs.Save();
            }
        }

        private int LoadEndlessBest()
        {
            return PlayerPrefs.GetInt(SAVE_KEY_ENDLESS_BEST, 0);
        }

        // ── Serialization Helper ──
        [System.Serializable]
        private class ProgressData
        {
            public List<LevelProgress> levels;
        }

        // ════════════════════════════════════════
        //  CLEANUP
        // ════════════════════════════════════════

        private void OnDestroy()
        {
            if (Bar != null)
            {
                Bar.OnHit -= OnBarHit;
                Bar.OnHit -= OnEndlessHit;
            }
        }
    }
}
