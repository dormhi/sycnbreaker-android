/* =========================================
   LevelData.cs — Level Configuration
   ScriptableObject-based level definitions.
   
   Each level's parameters are defined as an
   asset in the Unity Editor, making it trivial
   to add new levels without touching code.
   
   Ported from: js/LevelManager.js (_createLevels)
   ========================================= */

using UnityEngine;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// ScriptableObject that defines a single level's configuration.
    /// Create new levels via: Assets → Create → SyncBreaker → Level Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevel", menuName = "SyncBreaker/Level Data", order = 1)]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique level ID (1-6 for story, 999 for endless)")]
        public int id;

        [Tooltip("Internal level name (e.g., CLEAR_LOGS)")]
        public string levelName;

        [Tooltip("Short description shown in the Hub")]
        [TextArea(1, 2)]
        public string description;

        [Header("Difficulty")]
        [Tooltip("Overall difficulty rating (1-6)")]
        [Range(1, 6)]
        public int difficulty = 1;

        [Tooltip("Lockpick mini-game difficulty (1-6)")]
        [Range(1, 6)]
        public int lockpickDifficulty = 1;

        [Header("Timing Bar")]
        [Tooltip("Base speed of the timing bar indicator (higher = faster)")]
        [Range(0.3f, 4.0f)]
        public float barSpeed = 0.7f;

        [Tooltip("Width of the target zone as a fraction of bar length (0-1)")]
        [Range(0.05f, 0.35f)]
        public float targetSize = 0.22f;

        [Tooltip("How much the bar speed increases after each hit")]
        [Range(0f, 0.05f)]
        public float speedIncrement = 0.015f;

        [Tooltip("Maximum bar speed cap")]
        [Range(1f, 5f)]
        public float maxBarSpeed = 3.5f;

        [Header("Win/Lose Conditions")]
        [Tooltip("Number of successful hits required to complete the level")]
        [Range(1, 50)]
        public int requiredHits = 8;

        [Tooltip("Time limit in seconds (0 = no time limit)")]
        [Range(0f, 120f)]
        public float maxTime = 25f;

        [Header("Lives")]
        [Tooltip("Number of lives the player starts with")]
        [Range(1, 10)]
        public int startingLives = 3;

        [Header("Scoring")]
        [Tooltip("Points awarded for a perfect hit (multiplied by combo)")]
        public int perfectScore = 100;

        [Tooltip("Points awarded for a good hit (multiplied by combo)")]
        public int goodScore = 50;

        [Tooltip("Distance from center to qualify as 'perfect' (0-1, lower = stricter)")]
        [Range(0.1f, 0.5f)]
        public float perfectThreshold = 0.35f;

        [Header("Physics (Unity-specific)")]
        [Tooltip("Spring force applied to the bar indicator")]
        [Range(0f, 20f)]
        public float springForce = 5f;

        [Tooltip("Damping applied to bar movement for momentum feel")]
        [Range(0f, 5f)]
        public float barDamping = 0.5f;

        [Tooltip("Screen shake intensity on miss")]
        [Range(0f, 1f)]
        public float shakeIntensity = 0.3f;

        // ════════════════════════════════════════
        //  FACTORY — Predefined Level Configurations
        // ════════════════════════════════════════

        /// <summary>
        /// Returns default configurations for all 6 story levels.
        /// Used to initialize ScriptableObject assets programmatically.
        /// </summary>
        public static LevelData[] CreateDefaultLevels()
        {
            var configs = new[]
            {
                // Level 1: CLEAR_LOGS
                new LevelConfig(1, "CLEAR_LOGS", "Delete the attacker's log files",
                    difficulty: 1, barSpeed: 0.7f, targetSize: 0.22f,
                    requiredHits: 8, maxTime: 25f, lives: 3, lockpick: 1),

                // Level 2: CLOSE_PORTS
                new LevelConfig(2, "CLOSE_PORTS", "Shut down open backdoors",
                    difficulty: 2, barSpeed: 0.9f, targetSize: 0.18f,
                    requiredHits: 10, maxTime: 28f, lives: 3, lockpick: 2),

                // Level 3: REMOVE_MALWARE
                new LevelConfig(3, "REMOVE_MALWARE", "Detect and remove malicious software",
                    difficulty: 3, barSpeed: 1.1f, targetSize: 0.15f,
                    requiredHits: 12, maxTime: 30f, lives: 3, lockpick: 3),

                // Level 4: RESET_CREDS
                new LevelConfig(4, "RESET_CREDS", "Reset compromised credentials",
                    difficulty: 4, barSpeed: 1.4f, targetSize: 0.12f,
                    requiredHits: 14, maxTime: 35f, lives: 4, lockpick: 4),

                // Level 5: FIREWALL
                new LevelConfig(5, "FIREWALL", "Rebuild the firewall",
                    difficulty: 5, barSpeed: 1.7f, targetSize: 0.10f,
                    requiredHits: 16, maxTime: 40f, lives: 4, lockpick: 5),

                // Level 6: CUT_ACCESS
                new LevelConfig(6, "CUT_ACCESS", "Completely sever the attacker's connection",
                    difficulty: 6, barSpeed: 2.0f, targetSize: 0.08f,
                    requiredHits: 18, maxTime: 60f, lives: 5, lockpick: 6),
            };

            var levels = new LevelData[configs.Length];
            for (int i = 0; i < configs.Length; i++)
            {
                var ld = CreateInstance<LevelData>();
                var c = configs[i];
                ld.id = c.Id;
                ld.levelName = c.Name;
                ld.description = c.Desc;
                ld.difficulty = c.Difficulty;
                ld.barSpeed = c.BarSpeed;
                ld.targetSize = c.TargetSize;
                ld.requiredHits = c.RequiredHits;
                ld.maxTime = c.MaxTime;
                ld.startingLives = c.Lives;
                ld.lockpickDifficulty = c.Lockpick;
                ld.name = $"Level_{c.Id}_{c.Name}";
                levels[i] = ld;
            }

            return levels;
        }

        // Internal struct for factory pattern
        private struct LevelConfig
        {
            public int Id;
            public string Name, Desc;
            public int Difficulty;
            public float BarSpeed, TargetSize;
            public int RequiredHits;
            public float MaxTime;
            public int Lives, Lockpick;

            public LevelConfig(int id, string name, string desc,
                int difficulty, float barSpeed, float targetSize,
                int requiredHits, float maxTime, int lives, int lockpick)
            {
                Id = id; Name = name; Desc = desc;
                Difficulty = difficulty; BarSpeed = barSpeed; TargetSize = targetSize;
                RequiredHits = requiredHits; MaxTime = maxTime;
                Lives = lives; Lockpick = lockpick;
            }
        }
    }
}
