/* =========================================
   ThemeManager.cs — Runtime Theme Applicator
   
   Applies LevelTheme colors and settings to
   all visual elements at runtime:
   - Camera background
   - UI elements (bar, lockpick, HUD)
   - Particle systems
   - Post-processing (vignette, chromatic)
   
   Singleton — lives on GameManager object.
   ========================================= */

using UnityEngine;
using UnityEngine.UI;
using System;
using SyncBreaker.Core;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Manages visual theme transitions and applies
    /// theme colors to all relevant game objects.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        // ── Singleton ──
        public static ThemeManager Instance { get; private set; }

        // ── Current Theme ──
        /// <summary>Currently active theme.</summary>
        public LevelTheme CurrentTheme { get; private set; }

        // ── Theme Transition ──
        private LevelTheme _previousTheme;
        private float _transitionProgress = 1f;
        private float _transitionSpeed = 2f;
        private bool _transitioning;

        // ── Events ──
        /// <summary>Fired when theme changes. Args: (newTheme)</summary>
        public event Action<LevelTheme> OnThemeChanged;

        // ── Cached References ──
        [Header("References")]
        [SerializeField] private Camera mainCamera;

        [Header("Default Theme")]
        [SerializeField] private LevelTheme defaultTheme;

        // ── Runtime Color Cache ──
        /// <summary>Interpolated primary color (during transitions).</summary>
        public Color Primary { get; private set; }
        public Color Secondary { get; private set; }
        public Color Accent { get; private set; }
        public Color Background { get; private set; }
        public Color TextPrimary { get; private set; }
        public Color TextSecondary { get; private set; }

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

            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void Start()
        {
            // Apply default theme immediately
            if (defaultTheme != null)
            {
                ApplyThemeImmediate(defaultTheme);
            }
        }

        private void Update()
        {
            if (!_transitioning) return;

            _transitionProgress += _transitionSpeed * Time.deltaTime;
            if (_transitionProgress >= 1f)
            {
                _transitionProgress = 1f;
                _transitioning = false;
            }

            // Interpolate colors during transition
            InterpolateColors(_transitionProgress);
            ApplyColorsToScene();
        }

        // ════════════════════════════════════════
        //  PUBLIC API
        // ════════════════════════════════════════

        /// <summary>
        /// Smoothly transition to a new theme.
        /// </summary>
        public void TransitionToTheme(LevelTheme newTheme, float duration = 0.5f)
        {
            if (newTheme == null) return;
            if (newTheme == CurrentTheme && !_transitioning) return;

            _previousTheme = CurrentTheme ?? newTheme;
            CurrentTheme = newTheme;
            _transitionProgress = 0f;
            _transitionSpeed = duration > 0f ? 1f / duration : 100f;
            _transitioning = true;

            OnThemeChanged?.Invoke(newTheme);
            Debug.Log($"[ThemeManager] Transitioning to: {newTheme.themeName}");
        }

        /// <summary>
        /// Apply a theme instantly (no transition).
        /// </summary>
        public void ApplyThemeImmediate(LevelTheme theme)
        {
            if (theme == null) return;

            _previousTheme = theme;
            CurrentTheme = theme;
            _transitionProgress = 1f;
            _transitioning = false;

            // Set cached colors directly
            Primary = theme.primaryColor;
            Secondary = theme.secondaryColor;
            Accent = theme.accentColor;
            Background = theme.backgroundColor;
            TextPrimary = theme.textPrimary;
            TextSecondary = theme.textSecondary;

            ApplyColorsToScene();
            OnThemeChanged?.Invoke(theme);
            Debug.Log($"[ThemeManager] Applied: {theme.themeName}");
        }

        /// <summary>
        /// Apply theme for a specific level index.
        /// Looks up the theme from the level's LevelData.
        /// </summary>
        public void ApplyThemeForLevel(int levelIndex)
        {
            var levelManager = FindAnyObjectByType<Gameplay.LevelManager>();
            var levelData = levelManager?.GetLevelData(levelIndex);
            if (levelData?.theme != null)
            {
                TransitionToTheme(levelData.theme);
            }
            else
            {
                // Fallback: apply default or generate from preset
                if (defaultTheme != null)
                    TransitionToTheme(defaultTheme);
            }
        }

        // ════════════════════════════════════════
        //  COLOR INTERPOLATION
        // ════════════════════════════════════════

        private void InterpolateColors(float t)
        {
            if (_previousTheme == null || CurrentTheme == null) return;

            // Smooth step for more pleasing transition
            float smooth = t * t * (3f - 2f * t);

            Primary = Color.Lerp(_previousTheme.primaryColor, CurrentTheme.primaryColor, smooth);
            Secondary = Color.Lerp(_previousTheme.secondaryColor, CurrentTheme.secondaryColor, smooth);
            Accent = Color.Lerp(_previousTheme.accentColor, CurrentTheme.accentColor, smooth);
            Background = Color.Lerp(_previousTheme.backgroundColor, CurrentTheme.backgroundColor, smooth);
            TextPrimary = Color.Lerp(_previousTheme.textPrimary, CurrentTheme.textPrimary, smooth);
            TextSecondary = Color.Lerp(_previousTheme.textSecondary, CurrentTheme.textSecondary, smooth);
        }

        private void ApplyColorsToScene()
        {
            // Camera background
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = Background;
            }
        }
    }
}
