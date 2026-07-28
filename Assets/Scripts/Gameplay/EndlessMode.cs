/* =========================================
   EndlessMode.cs — Endless Mode Controller
   
   Manages the infinite gameplay loop:
   - Wave system with progressive difficulty
   - Boss waves (every 5th wave)
   - Dynamic speed/tolerance scaling
   - High score tracking
   - Wave transition effects
   - Leaderboard data preparation
   
   Works alongside LevelManager.EndlessMode.
   ========================================= */

using UnityEngine;
using System;
using SyncBreaker.Core;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Configuration for a single endless wave.
    /// Generated dynamically based on wave number.
    /// </summary>
    [System.Serializable]
    public class WaveConfig
    {
        public int WaveNumber;
        public float BarSpeed;
        public float TargetSize;
        public float SpeedIncrement;
        public int RequiredHits;
        public int Lives;
        public float MaxTime;
        public bool IsBossWave;
        public string WaveName;

        // Lockpick settings for boss waves
        public int LockpickDifficulty;
        public int LockpickNodeCount;
    }

    /// <summary>
    /// Controls endless mode progression, difficulty
    /// scaling, and wave transitions.
    /// </summary>
    public class EndlessMode : MonoBehaviour
    {
        // ── Singleton ──
        public static EndlessMode Instance { get; private set; }

        // ── Events ──
        /// <summary>Fired when a new wave starts. Args: (waveConfig)</summary>
        public event Action<WaveConfig> OnWaveStart;

        /// <summary>Fired when a wave is completed. Args: (waveNumber, score)</summary>
        public event Action<int, int> OnWaveComplete;

        /// <summary>Fired when a boss wave starts.</summary>
        public event Action<WaveConfig> OnBossWaveStart;

        /// <summary>Fired when the endless run ends. Args: (totalScore, wave, maxCombo)</summary>
        public event Action<int, int, int> OnRunEnd;

        // ── State ──
        /// <summary>Current wave number (1-based).</summary>
        public int CurrentWave { get; private set; }

        /// <summary>Total accumulated score across all waves.</summary>
        public int TotalScore { get; private set; }

        /// <summary>Maximum combo achieved in this run.</summary>
        public int MaxCombo { get; private set; }

        /// <summary>Total hits in this run.</summary>
        public int TotalHits { get; private set; }

        /// <summary>Current wave configuration.</summary>
        public WaveConfig CurrentWaveConfig { get; private set; }

        /// <summary>Is the endless run active?</summary>
        public bool Active { get; private set; }

        /// <summary>Time survived in this run.</summary>
        public float TimeSurvived { get; private set; }

        // ── High Score ──
        /// <summary>All-time best score.</summary>
        public int HighScore { get; private set; }

        /// <summary>All-time best wave reached.</summary>
        public int HighWave { get; private set; }

        // ── Difficulty Curves ──
        [Header("Difficulty Scaling")]
        [Tooltip("Base bar speed at wave 1")]
        [SerializeField] private float baseBarSpeed = 0.7f;

        [Tooltip("Bar speed increase per wave")]
        [SerializeField] private float speedPerWave = 0.06f;

        [Tooltip("Maximum bar speed cap")]
        [SerializeField] private float maxBarSpeed = 3.0f;

        [Tooltip("Base target zone size at wave 1")]
        [SerializeField] private float baseTargetSize = 0.22f;

        [Tooltip("Target size decrease per wave")]
        [SerializeField] private float targetShrinkPerWave = 0.012f;

        [Tooltip("Minimum target zone size")]
        [SerializeField] private float minTargetSize = 0.06f;

        [Tooltip("Base required hits at wave 1")]
        [SerializeField] private int baseRequiredHits = 5;

        [Tooltip("Hits increase per wave")]
        [SerializeField] private int hitsPerWave = 2;

        [Tooltip("Maximum required hits per wave")]
        [SerializeField] private int maxRequiredHits = 25;

        [Header("Boss Waves")]
        [Tooltip("Boss wave occurs every N waves")]
        [SerializeField] private int bossWaveInterval = 5;

        [Tooltip("Boss wave has extra time")]
        [SerializeField] private float bossExtraTime = 10f;

        [Header("Lives")]
        [Tooltip("Starting lives")]
        [SerializeField] private int startingLives = 3;

        [Tooltip("Extra life every N waves")]
        [SerializeField] private int extraLifeInterval = 3;

        // ── Wave Names ──
        private static readonly string[] WaveNames =
        {
            "BREACH", "DECRYPT", "OVERRIDE", "EXPLOIT",
            "CORRUPT", "HIJACK", "INFILTRATE", "BYPASS",
            "ESCALATE", "DOMINATE"
        };

        private static readonly string[] BossNames =
        {
            "FIREWALL ALPHA", "PROXY SENTINEL",
            "ROOT GUARDIAN", "QUANTUM GATE",
            "CORE NEXUS", "NEURAL OMEGA"
        };

        // ════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadHighScores();
        }

        private void Update()
        {
            if (Active)
            {
                TimeSurvived += Time.deltaTime;
            }
        }

        // ════════════════════════════════════════
        //  PUBLIC API
        // ════════════════════════════════════════

        /// <summary>
        /// Start a new endless run.
        /// </summary>
        public void StartRun()
        {
            CurrentWave = 0;
            TotalScore = 0;
            MaxCombo = 0;
            TotalHits = 0;
            TimeSurvived = 0f;
            Active = true;

            Debug.Log("[EndlessMode] ═══ NEW RUN STARTED ═══");

            // Start first wave
            AdvanceWave();
        }

        /// <summary>
        /// Complete the current wave and advance.
        /// </summary>
        /// <param name="waveScore">Score earned this wave</param>
        /// <param name="waveCombo">Max combo this wave</param>
        /// <param name="waveHits">Hits this wave</param>
        public void CompleteWave(int waveScore, int waveCombo, int waveHits)
        {
            TotalScore += waveScore;
            TotalHits += waveHits;
            if (waveCombo > MaxCombo) MaxCombo = waveCombo;

            OnWaveComplete?.Invoke(CurrentWave, waveScore);
            Debug.Log($"[EndlessMode] Wave {CurrentWave} complete! " +
                      $"Score: +{waveScore} (Total: {TotalScore})");

            // Play SFX
            SoundManager.Instance?.PlaySFX(SFX.WaveTransition);

            // Advance to next wave
            AdvanceWave();
        }

        /// <summary>
        /// End the endless run (player died).
        /// </summary>
        public void EndRun(int finalScore, int finalCombo, int finalHits)
        {
            Active = false;
            TotalScore += finalScore;
            TotalHits += finalHits;
            if (finalCombo > MaxCombo) MaxCombo = finalCombo;

            // Check high scores
            bool newHighScore = TotalScore > HighScore;
            bool newHighWave = CurrentWave > HighWave;

            if (newHighScore) HighScore = TotalScore;
            if (newHighWave) HighWave = CurrentWave;

            SaveHighScores();

            OnRunEnd?.Invoke(TotalScore, CurrentWave, MaxCombo);

            Debug.Log($"[EndlessMode] ═══ RUN ENDED ═══");
            Debug.Log($"[EndlessMode] Score: {TotalScore} | Wave: {CurrentWave} | " +
                      $"Combo: {MaxCombo} | Time: {TimeSurvived:F1}s");
            if (newHighScore) Debug.Log("[EndlessMode] 🏆 NEW HIGH SCORE!");
        }

        // ════════════════════════════════════════
        //  WAVE GENERATION
        // ════════════════════════════════════════

        private void AdvanceWave()
        {
            CurrentWave++;
            CurrentWaveConfig = GenerateWaveConfig(CurrentWave);

            // Check if boss wave
            if (CurrentWaveConfig.IsBossWave)
            {
                OnBossWaveStart?.Invoke(CurrentWaveConfig);
                SoundManager.Instance?.PlaySFX(SFX.LockpickStart);
            }

            OnWaveStart?.Invoke(CurrentWaveConfig);

            Debug.Log($"[EndlessMode] ─── Wave {CurrentWave}: {CurrentWaveConfig.WaveName} ───");
            Debug.Log($"  Speed: {CurrentWaveConfig.BarSpeed:F2} | " +
                      $"Target: {CurrentWaveConfig.TargetSize:F2} | " +
                      $"Hits: {CurrentWaveConfig.RequiredHits} | " +
                      $"Boss: {CurrentWaveConfig.IsBossWave}");
        }

        /// <summary>
        /// Generate wave configuration based on wave number.
        /// Difficulty scales logarithmically to avoid
        /// becoming impossible too quickly.
        /// </summary>
        public WaveConfig GenerateWaveConfig(int wave)
        {
            bool isBoss = wave % bossWaveInterval == 0;

            // Logarithmic scaling for smoother difficulty curve
            float difficultyFactor = Mathf.Log(wave + 1, 2f); // log2 curve

            // Bar speed: increases but with diminishing returns
            float speed = Mathf.Min(
                baseBarSpeed + speedPerWave * difficultyFactor * 2f,
                maxBarSpeed);

            // Target size: shrinks but never below minimum
            float targetSize = Mathf.Max(
                baseTargetSize - targetShrinkPerWave * difficultyFactor * 1.5f,
                minTargetSize);

            // Required hits: increases linearly then caps
            int hits = Mathf.Min(
                baseRequiredHits + (int)(hitsPerWave * difficultyFactor),
                maxRequiredHits);

            // Lives: start with base, add 1 every N waves
            int lives = startingLives;
            if (wave > 1)
            {
                int bonusLives = (wave - 1) / extraLifeInterval;
                lives = Mathf.Min(startingLives + bonusLives, 5);
            }

            // Time limit: generous early, tighter later
            float maxTime = 30f - difficultyFactor * 2f;
            maxTime = Mathf.Max(maxTime, 12f);

            // Boss wave adjustments
            if (isBoss)
            {
                hits = (int)(hits * 1.5f);
                maxTime += bossExtraTime;
                speed *= 1.15f;
            }

            // Wave name
            string waveName;
            if (isBoss)
            {
                int bossIndex = (wave / bossWaveInterval - 1) % BossNames.Length;
                waveName = BossNames[bossIndex];
            }
            else
            {
                int nameIndex = (wave - 1) % WaveNames.Length;
                waveName = WaveNames[nameIndex];
            }

            return new WaveConfig
            {
                WaveNumber = wave,
                BarSpeed = speed,
                TargetSize = targetSize,
                SpeedIncrement = 0.015f + wave * 0.002f,
                RequiredHits = hits,
                Lives = lives,
                MaxTime = maxTime,
                IsBossWave = isBoss,
                WaveName = waveName,
                LockpickDifficulty = isBoss ? Mathf.Clamp(wave / bossWaveInterval, 1, 6) : 0,
                LockpickNodeCount = isBoss ? Mathf.Clamp(3 + wave / bossWaveInterval, 4, 7) : 0
            };
        }

        // ════════════════════════════════════════
        //  HIGH SCORE PERSISTENCE
        // ════════════════════════════════════════

        private void LoadHighScores()
        {
            HighScore = PlayerPrefs.GetInt("SB_EndlessHighScore", 0);
            HighWave = PlayerPrefs.GetInt("SB_EndlessHighWave", 0);
        }

        private void SaveHighScores()
        {
            PlayerPrefs.SetInt("SB_EndlessHighScore", HighScore);
            PlayerPrefs.SetInt("SB_EndlessHighWave", HighWave);
            PlayerPrefs.Save();
        }

        // ════════════════════════════════════════
        //  QUERIES
        // ════════════════════════════════════════

        /// <summary>
        /// Is the current wave a boss wave?
        /// </summary>
        public bool IsBossWave => CurrentWaveConfig?.IsBossWave ?? false;

        /// <summary>
        /// Get a formatted time survived string.
        /// </summary>
        public string FormattedTime => Utils.FormatTime(TimeSurvived);

        /// <summary>
        /// Get difficulty rating for current wave (1-10 stars).
        /// </summary>
        public int DifficultyRating =>
            Mathf.Clamp((int)(Mathf.Log(CurrentWave + 1, 2f) * 2f), 1, 10);
    }
}
