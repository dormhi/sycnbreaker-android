/* =========================================
   LevelTheme.cs — Visual Theme ScriptableObject
   
   Defines the look and feel of each level:
   - Color palette (primary, secondary, accent, bg)
   - Particle settings
   - Timing bar & lockpick colors
   - Background grid/pattern settings
   - Screen shake intensity
   
   7 pre-configured themes matching
   the original web game's visual identity.
   
   Ported from: js/themes in UIManager.js
   ========================================= */

using UnityEngine;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// ScriptableObject defining the visual theme for a level.
    /// Each level references a theme that controls colors,
    /// particles, and visual effects.
    /// </summary>
    [CreateAssetMenu(fileName = "New Theme", menuName = "SyncBreaker/Level Theme", order = 2)]
    public class LevelTheme : ScriptableObject
    {
        // ════════════════════════════════════════
        //  IDENTITY
        // ════════════════════════════════════════

        [Header("Theme Identity")]
        [Tooltip("Display name for this theme")]
        public string themeName = "Default";

        [Tooltip("Unique identifier (e.g. 'neural_pulse')")]
        public string themeId = "default";

        // ════════════════════════════════════════
        //  COLOR PALETTE
        // ════════════════════════════════════════

        [Header("Core Colors")]
        [Tooltip("Primary accent color — used for active elements, highlights")]
        public Color primaryColor = new Color(0.23f, 0.51f, 0.96f, 1f);   // #3b82f6

        [Tooltip("Secondary color — used for gradients, subtle accents")]
        public Color secondaryColor = new Color(0.49f, 0.27f, 0.96f, 1f); // #7c3aed

        [Tooltip("Tertiary/Accent color — sparks, special effects")]
        public Color accentColor = new Color(0.06f, 0.82f, 0.88f, 1f);    // #10d1e0

        [Header("Background")]
        [Tooltip("Main background color")]
        public Color backgroundColor = new Color(0.04f, 0.05f, 0.09f, 1f); // #0a0d17

        [Tooltip("Secondary background (for gradients)")]
        public Color backgroundSecondary = new Color(0.06f, 0.09f, 0.16f, 1f); // #101729

        [Header("UI Text Colors")]
        [Tooltip("Primary text color")]
        public Color textPrimary = new Color(0.95f, 0.96f, 0.98f, 1f);    // #f1f5f9

        [Tooltip("Secondary/muted text color")]
        public Color textSecondary = new Color(0.58f, 0.63f, 0.73f, 1f);  // #94a3b8

        // ════════════════════════════════════════
        //  TIMING BAR COLORS
        // ════════════════════════════════════════

        [Header("Timing Bar")]
        [Tooltip("Bar background track color")]
        public Color barTrackColor = new Color(0.1f, 0.12f, 0.18f, 0.8f);

        [Tooltip("Target zone color")]
        public Color targetZoneColor = new Color(0.13f, 0.77f, 0.37f, 0.6f); // #22c55e

        [Tooltip("Indicator/cursor color")]
        public Color indicatorColor = Color.white;

        [Tooltip("Perfect hit feedback color")]
        public Color perfectColor = new Color(0.13f, 0.77f, 0.37f, 1f);  // #22c55e

        [Tooltip("Good hit feedback color")]
        public Color goodColor = new Color(0.29f, 0.87f, 0.50f, 1f);     // #4ade80

        [Tooltip("Miss feedback color")]
        public Color missColor = new Color(0.94f, 0.27f, 0.27f, 1f);     // #ef4444

        // ════════════════════════════════════════
        //  LOCKPICK COLORS
        // ════════════════════════════════════════

        [Header("Lockpick")]
        [Tooltip("Lockpick ring/circle color")]
        public Color lockpickRingColor = new Color(0.2f, 0.25f, 0.33f, 0.5f);

        [Tooltip("Active node glow color")]
        public Color lockpickActiveColor = new Color(0.23f, 0.51f, 0.96f, 1f);

        [Tooltip("Solved node color")]
        public Color lockpickSolvedColor = new Color(0.13f, 0.77f, 0.37f, 1f);

        [Tooltip("Cursor dot color")]
        public Color lockpickCursorColor = Color.white;

        // ════════════════════════════════════════
        //  PARTICLE SETTINGS
        // ════════════════════════════════════════

        [Header("Particles")]
        [Tooltip("Primary particle color (hits, sparks)")]
        public Color particlePrimary = new Color(0.23f, 0.51f, 0.96f, 1f);

        [Tooltip("Secondary particle color (trails, ambient)")]
        public Color particleSecondary = new Color(0.49f, 0.27f, 0.96f, 1f);

        [Tooltip("Particle emission rate multiplier")]
        [Range(0.5f, 3f)]
        public float particleIntensity = 1f;

        [Tooltip("Particle size multiplier")]
        [Range(0.5f, 2f)]
        public float particleScale = 1f;

        // ════════════════════════════════════════
        //  SCREEN EFFECTS
        // ════════════════════════════════════════

        [Header("Screen Effects")]
        [Tooltip("Screen shake intensity multiplier")]
        [Range(0f, 2f)]
        public float screenShakeMultiplier = 1f;

        [Tooltip("Vignette intensity on miss")]
        [Range(0f, 1f)]
        public float vignetteIntensity = 0.4f;

        [Tooltip("Chromatic aberration on perfect hit")]
        [Range(0f, 1f)]
        public float chromaticAberration = 0.3f;

        // ════════════════════════════════════════
        //  BACKGROUND GRID
        // ════════════════════════════════════════

        [Header("Background Grid/Pattern")]
        [Tooltip("Show animated grid in background")]
        public bool showGrid = true;

        [Tooltip("Grid line color")]
        public Color gridColor = new Color(0.23f, 0.51f, 0.96f, 0.03f);

        [Tooltip("Grid cell size in units")]
        public float gridCellSize = 40f;

        [Tooltip("Number of floating ambient particles")]
        [Range(0, 100)]
        public int ambientParticleCount = 30;

        // ════════════════════════════════════════
        //  GRADIENTS
        // ════════════════════════════════════════

        /// <summary>
        /// Create a gradient from primary to secondary color.
        /// Used for bar fills, progress indicators, etc.
        /// </summary>
        public Gradient CreatePrimaryGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new(primaryColor, 0f),
                    new(secondaryColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new(1f, 0f),
                    new(1f, 1f)
                }
            );
            return gradient;
        }

        /// <summary>
        /// Create a gradient for particle effects (primary → accent).
        /// </summary>
        public Gradient CreateParticleGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new(particlePrimary, 0f),
                    new(accentColor, 0.5f),
                    new(particleSecondary, 1f)
                },
                new GradientAlphaKey[]
                {
                    new(1f, 0f),
                    new(0.8f, 0.5f),
                    new(0f, 1f)
                }
            );
            return gradient;
        }

        // ════════════════════════════════════════
        //  FACTORY — 7 PRESET THEMES
        // ════════════════════════════════════════

        /// <summary>
        /// Get preset theme values by index (0-6).
        /// Used to auto-populate ScriptableObject defaults in editor.
        /// </summary>
        public static ThemePreset GetPreset(int index)
        {
            return index switch
            {
                0 => new ThemePreset
                {
                    Name = "Neural Pulse",
                    Id = "neural_pulse",
                    Primary = HexColor("#3b82f6"),
                    Secondary = HexColor("#1d4ed8"),
                    Accent = HexColor("#60a5fa"),
                    Background = HexColor("#0a0d17"),
                    BackgroundSecondary = HexColor("#101729"),
                    ParticleIntensity = 1f
                },
                1 => new ThemePreset
                {
                    Name = "Quantum Lock",
                    Id = "quantum_lock",
                    Primary = HexColor("#8b5cf6"),
                    Secondary = HexColor("#7c3aed"),
                    Accent = HexColor("#a78bfa"),
                    Background = HexColor("#0d0a17"),
                    BackgroundSecondary = HexColor("#170f29"),
                    ParticleIntensity = 1.2f
                },
                2 => new ThemePreset
                {
                    Name = "Firewall Breach",
                    Id = "firewall_breach",
                    Primary = HexColor("#ef4444"),
                    Secondary = HexColor("#dc2626"),
                    Accent = HexColor("#f87171"),
                    Background = HexColor("#170a0a"),
                    BackgroundSecondary = HexColor("#291010"),
                    ParticleIntensity = 1.5f
                },
                3 => new ThemePreset
                {
                    Name = "Data Stream",
                    Id = "data_stream",
                    Primary = HexColor("#22c55e"),
                    Secondary = HexColor("#16a34a"),
                    Accent = HexColor("#4ade80"),
                    Background = HexColor("#0a170d"),
                    BackgroundSecondary = HexColor("#102917"),
                    ParticleIntensity = 1f
                },
                4 => new ThemePreset
                {
                    Name = "Crypto Vault",
                    Id = "crypto_vault",
                    Primary = HexColor("#f59e0b"),
                    Secondary = HexColor("#d97706"),
                    Accent = HexColor("#fbbf24"),
                    Background = HexColor("#17130a"),
                    BackgroundSecondary = HexColor("#292010"),
                    ParticleIntensity = 1.3f
                },
                5 => new ThemePreset
                {
                    Name = "Ice Protocol",
                    Id = "ice_protocol",
                    Primary = HexColor("#06b6d4"),
                    Secondary = HexColor("#0891b2"),
                    Accent = HexColor("#22d3ee"),
                    Background = HexColor("#0a1317"),
                    BackgroundSecondary = HexColor("#101f29"),
                    ParticleIntensity = 1.1f
                },
                6 => new ThemePreset
                {
                    Name = "Root Access",
                    Id = "root_access",
                    Primary = HexColor("#ec4899"),
                    Secondary = HexColor("#db2777"),
                    Accent = HexColor("#f472b6"),
                    Background = HexColor("#170a12"),
                    BackgroundSecondary = HexColor("#29101e"),
                    ParticleIntensity = 1.4f
                },
                _ => GetPreset(0)
            };
        }

        /// <summary>
        /// Apply a preset to this ScriptableObject.
        /// </summary>
        public void ApplyPreset(int index)
        {
            var preset = GetPreset(index);
            themeName = preset.Name;
            themeId = preset.Id;
            primaryColor = preset.Primary;
            secondaryColor = preset.Secondary;
            accentColor = preset.Accent;
            backgroundColor = preset.Background;
            backgroundSecondary = preset.BackgroundSecondary;
            particlePrimary = preset.Primary;
            particleSecondary = preset.Secondary;
            particleIntensity = preset.ParticleIntensity;

            // Derive other colors from primary
            targetZoneColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.6f);
            lockpickActiveColor = primaryColor;
            gridColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.03f);
        }

        // ════════════════════════════════════════
        //  HELPER
        // ════════════════════════════════════════

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }

    /// <summary>
    /// Data container for theme preset values.
    /// </summary>
    public struct ThemePreset
    {
        public string Name;
        public string Id;
        public Color Primary;
        public Color Secondary;
        public Color Accent;
        public Color Background;
        public Color BackgroundSecondary;
        public float ParticleIntensity;
    }
}
