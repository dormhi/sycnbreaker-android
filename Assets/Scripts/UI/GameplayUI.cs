/* =========================================
   GameplayUI.cs — In-Game HUD
   
   Displays score, combo, timer, lives,
   hit feedback, and level info during gameplay.
   Uses Unity UI (Canvas) with TextMeshPro.
   
   Ported from: js/UIManager.js + LevelManager render
   ========================================= */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SyncBreaker.Core;

namespace SyncBreaker.UI
{
    /// <summary>
    /// Manages the in-game HUD during Level and Endless states.
    /// Reads data from TimingBar and LevelManager to update displays.
    /// </summary>
    public class GameplayUI : MonoBehaviour
    {
        // ════════════════════════════════════════
        //  UI REFERENCES (assigned in Inspector)
        // ════════════════════════════════════════

        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI comboText;

        [Header("Level Info")]
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private TextMeshProUGUI levelDescText;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Color timerNormalColor = new Color(0.39f, 0.45f, 0.55f, 1f); // #64748b
        [SerializeField] private Color timerDangerColor = new Color(0.94f, 0.27f, 0.27f, 1f); // #ef4444
        [SerializeField] private float timerDangerThreshold = 10f;

        [Header("Lives")]
        [SerializeField] private Transform livesContainer;
        [SerializeField] private GameObject heartPrefab;

        [Header("Hit Feedback")]
        [SerializeField] private TextMeshProUGUI hitResultText;
        [SerializeField] private float hitResultFadeDuration = 0.5f;

        [Header("Timing Bar Visual")]
        [SerializeField] private RectTransform barBackground;
        [SerializeField] private RectTransform targetZoneRect;
        [SerializeField] private RectTransform indicatorRect;
        [SerializeField] private Image targetZoneImage;
        [SerializeField] private Image indicatorImage;

        [Header("Endless Mode")]
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private Slider waveProgressSlider;
        [SerializeField] private GameObject waveFlashOverlay;

        [Header("Transition Overlay")]
        [SerializeField] private Image transitionOverlay;

        // ── Internal State ──
        private Gameplay.TimingBar _bar;
        private Gameplay.LevelManager _levelManager;
        private float _hitResultTimer;
        private GameObject[] _heartInstances;

        // ── Colors ──
        private static readonly Color PerfectColor = Utils.HexToColor("#22c55e");
        private static readonly Color GoodColor = Utils.HexToColor("#4ade80");
        private static readonly Color MissColor = Utils.HexToColor("#ef4444");
        private static readonly Color ComboColor = Utils.HexToColor("#f59e0b");

        // ════════════════════════════════════════
        //  INITIALIZATION
        // ════════════════════════════════════════

        /// <summary>
        /// Bind the UI to a TimingBar and LevelManager instance.
        /// Called when entering Level or Endless state.
        /// </summary>
        public void Bind(Gameplay.TimingBar bar, Gameplay.LevelManager levelManager)
        {
            _bar = bar;
            _levelManager = levelManager;

            if (_bar != null)
            {
                _bar.OnHit += OnHitFeedback;
            }

            // Initialize displays
            UpdateLevelInfo();
            CreateHearts();
            ClearHitResult();

            // Endless mode elements
            if (waveText != null)
                waveText.gameObject.SetActive(levelManager.EndlessMode);
            if (waveProgressSlider != null)
                waveProgressSlider.gameObject.SetActive(levelManager.EndlessMode);
        }

        /// <summary>
        /// Unbind from TimingBar (cleanup).
        /// </summary>
        public void Unbind()
        {
            if (_bar != null)
            {
                _bar.OnHit -= OnHitFeedback;
            }
            _bar = null;
            _levelManager = null;
        }

        // ════════════════════════════════════════
        //  UPDATE
        // ════════════════════════════════════════

        private void Update()
        {
            if (_bar == null || _levelManager == null) return;

            UpdateScore();
            UpdateTimer();
            UpdateLives();
            UpdateTimingBarVisual();
            UpdateHitResultFade();
            UpdateTransitionOverlay();

            if (_levelManager.EndlessMode)
            {
                UpdateEndlessUI();
            }
        }

        // ════════════════════════════════════════
        //  SCORE & COMBO
        // ════════════════════════════════════════

        private void UpdateScore()
        {
            if (scoreText != null)
            {
                scoreText.text = Utils.FormatScore(_bar.Score);
            }

            if (comboText != null)
            {
                if (_bar.Combo > 1)
                {
                    comboText.gameObject.SetActive(true);
                    comboText.text = $"x{_bar.Combo}";
                    comboText.color = ComboColor;
                }
                else
                {
                    comboText.gameObject.SetActive(false);
                }
            }
        }

        // ════════════════════════════════════════
        //  LEVEL INFO
        // ════════════════════════════════════════

        private void UpdateLevelInfo()
        {
            if (_levelManager.CurrentLevelData == null) return;

            if (levelNameText != null)
                levelNameText.text = _levelManager.CurrentLevelData.levelName;

            if (levelDescText != null)
                levelDescText.text = _levelManager.CurrentLevelData.description;

            UpdateProgress();
        }

        private void UpdateProgress()
        {
            if (progressText != null && _bar != null && _levelManager.CurrentLevelData != null)
            {
                int required = _levelManager.CurrentLevelData.requiredHits;
                if (required < int.MaxValue)
                {
                    progressText.text = $"Hit: {_bar.HitCount}/{required}";
                }
                else
                {
                    progressText.text = $"Hit: {_bar.HitCount}";
                }
            }
        }

        // ════════════════════════════════════════
        //  TIMER
        // ════════════════════════════════════════

        private void UpdateTimer()
        {
            if (timerText == null) return;

            float remaining = _levelManager.RemainingTime;

            if (_levelManager.CurrentLevelData.maxTime <= 0 || remaining >= float.MaxValue / 2f)
            {
                // No timer (endless mode)
                timerText.gameObject.SetActive(false);
                return;
            }

            timerText.gameObject.SetActive(true);
            timerText.text = $"Time: {Utils.FormatTime(remaining)}";
            timerText.color = remaining < timerDangerThreshold ? timerDangerColor : timerNormalColor;
        }

        // ════════════════════════════════════════
        //  LIVES (Hearts)
        // ════════════════════════════════════════

        private void CreateHearts()
        {
            if (livesContainer == null || heartPrefab == null) return;

            // Clear existing
            foreach (Transform child in livesContainer)
            {
                Destroy(child.gameObject);
            }

            int maxLives = _bar?.MaxLives ?? 3;
            _heartInstances = new GameObject[maxLives];

            for (int i = 0; i < maxLives; i++)
            {
                var heart = Instantiate(heartPrefab, livesContainer);
                _heartInstances[i] = heart;
            }
        }

        private void UpdateLives()
        {
            if (_heartInstances == null || _bar == null) return;

            for (int i = 0; i < _heartInstances.Length; i++)
            {
                if (_heartInstances[i] == null) continue;

                var img = _heartInstances[i].GetComponent<Image>();
                if (img != null)
                {
                    bool alive = i < _bar.Lives;
                    img.color = alive
                        ? new Color(0.94f, 0.27f, 0.27f, 1f)   // #ef4444 (red)
                        : new Color(0.94f, 0.27f, 0.27f, 0.2f); // faded
                }
            }

            UpdateProgress();
        }

        // ════════════════════════════════════════
        //  TIMING BAR VISUAL
        // ════════════════════════════════════════

        private void UpdateTimingBarVisual()
        {
            if (barBackground == null || _bar == null) return;

            float barWidth = barBackground.rect.width;

            // Update target zone position and size
            if (targetZoneRect != null)
            {
                float zoneX = _bar.TargetZoneStart * barWidth;
                float zoneW = (_bar.TargetZoneEnd - _bar.TargetZoneStart) * barWidth;
                targetZoneRect.anchoredPosition = new Vector2(zoneX, 0);
                targetZoneRect.sizeDelta = new Vector2(zoneW, targetZoneRect.sizeDelta.y);
            }

            // Update indicator position
            if (indicatorRect != null)
            {
                float indicatorX = _bar.BarPosition * barWidth;
                indicatorRect.anchoredPosition = new Vector2(indicatorX, indicatorRect.anchoredPosition.y);

                // Bob animation
                float bob = Mathf.Sin(Time.time * 6f) * 3f;
                indicatorRect.anchoredPosition = new Vector2(indicatorX, bob);
            }

            // Breathing opacity on target zone
            if (targetZoneImage != null)
            {
                float breathe = 0.6f + Mathf.Sin(Time.time * 3f) * 0.2f;
                var c = targetZoneImage.color;
                targetZoneImage.color = new Color(c.r, c.g, c.b, breathe);
            }
        }

        // ════════════════════════════════════════
        //  HIT FEEDBACK
        // ════════════════════════════════════════

        private void OnHitFeedback(Gameplay.HitResult result, int scoreEarned, int combo)
        {
            if (hitResultText == null) return;

            switch (result)
            {
                case Gameplay.HitResult.Perfect:
                    hitResultText.text = "PERFECT";
                    hitResultText.color = PerfectColor;
                    break;
                case Gameplay.HitResult.Good:
                    hitResultText.text = "GOOD";
                    hitResultText.color = GoodColor;
                    break;
                case Gameplay.HitResult.Miss:
                    hitResultText.text = "MISS";
                    hitResultText.color = MissColor;
                    break;
            }

            hitResultText.gameObject.SetActive(true);
            _hitResultTimer = hitResultFadeDuration;

            // Scale punch animation
            hitResultText.transform.localScale = Vector3.one * 1.3f;
        }

        private void UpdateHitResultFade()
        {
            if (hitResultText == null || _hitResultTimer <= 0f) return;

            _hitResultTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(_hitResultTimer / hitResultFadeDuration);

            var c = hitResultText.color;
            hitResultText.color = new Color(c.r, c.g, c.b, alpha);

            // Scale animation (shrink back to 1.0)
            float scale = 1f + (1f - alpha) * 0.15f;
            hitResultText.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, alpha);

            if (_hitResultTimer <= 0f)
            {
                hitResultText.gameObject.SetActive(false);
            }
        }

        private void ClearHitResult()
        {
            if (hitResultText != null)
            {
                hitResultText.gameObject.SetActive(false);
                _hitResultTimer = 0f;
            }
        }

        // ════════════════════════════════════════
        //  ENDLESS MODE UI
        // ════════════════════════════════════════

        private void UpdateEndlessUI()
        {
            if (waveText != null)
            {
                waveText.text = $"WAVE {_levelManager.EndlessWave}";
            }

            if (waveProgressSlider != null)
            {
                waveProgressSlider.value = (float)_levelManager.EndlessHitsInWave / _levelManager.EndlessHitsPerWave;
            }

            // Wave flash overlay
            if (waveFlashOverlay != null)
            {
                if (_levelManager.EndlessWaveFlash > 0f)
                {
                    waveFlashOverlay.SetActive(true);
                    var img = waveFlashOverlay.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = new Color(0.96f, 0.62f, 0.04f, _levelManager.EndlessWaveFlash * 0.15f);
                    }
                }
                else
                {
                    waveFlashOverlay.SetActive(false);
                }
            }
        }

        // ════════════════════════════════════════
        //  TRANSITION OVERLAY
        // ════════════════════════════════════════

        private void UpdateTransitionOverlay()
        {
            if (transitionOverlay == null) return;

            var state = GameManager.Instance?.State;
            if (state != null && state.IsTransitioning)
            {
                transitionOverlay.gameObject.SetActive(true);
                transitionOverlay.color = new Color(0.04f, 0.05f, 0.09f, state.TransitionAlpha);
            }
            else
            {
                transitionOverlay.gameObject.SetActive(false);
            }
        }

        // ════════════════════════════════════════
        //  LEVEL COMPLETE / FAILED OVERLAY
        // ════════════════════════════════════════

        /// <summary>
        /// Show "NODE CLEANED!" overlay text.
        /// Called by the Level state handler when level completes.
        /// </summary>
        public void ShowLevelComplete(int score, int maxCombo)
        {
            if (hitResultText != null)
            {
                hitResultText.text = "NODE CLEANED!";
                hitResultText.color = PerfectColor;
                hitResultText.fontSize = 48;
                hitResultText.gameObject.SetActive(true);
                _hitResultTimer = 2f;
            }
        }

        // ════════════════════════════════════════
        //  CLEANUP
        // ════════════════════════════════════════

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
