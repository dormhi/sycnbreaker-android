/* =========================================
   GameOverUI.cs — Game Over Screen
   
   Displays end-of-game results:
   - Story mode: score, revive option, retry, menu
   - Endless mode: score, wave, max combo, hits
   
   Cyberpunk-themed.
   ========================================= */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using SyncBreaker.Core;

namespace SyncBreaker.UI
{
    /// <summary>
    /// Game over / endless over screen.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private TextMeshProUGUI gameOverTitle;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI scoreLabel;
        [SerializeField] private GameObject scoreHighlight;

        [Header("Story Mode")]
        [SerializeField] private GameObject storyPanel;
        [SerializeField] private Button reviveButton;
        [SerializeField] private TextMeshProUGUI reviveButtonText;
        [SerializeField] private GameObject reviveLockedIndicator;

        [Header("Endless Mode")]
        [SerializeField] private GameObject endlessPanel;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI maxComboText;
        [SerializeField] private TextMeshProUGUI hitCountText;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        [Header("Background")]
        [SerializeField] private Image backgroundOverlay;
        [SerializeField] private float overlayAlpha = 0.85f;

        public event Action OnReviveClicked;
        public event Action OnRetryClicked;
        public event Action OnMenuClicked;

        private CanvasGroup _canvasGroup;
        private bool _isEndless;

        // ════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            if (reviveButton != null)
                reviveButton.onClick.AddListener(() => OnReviveClicked?.Invoke());

            if (retryButton != null)
                retryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());

            if (menuButton != null)
                menuButton.onClick.AddListener(() => OnMenuClicked?.Invoke());

            HideImmediate();
        }

        private void Update()
        {
            if (backgroundOverlay != null && gameObject.activeSelf)
            {
                float pulse = overlayAlpha + Mathf.Sin(Time.time * 0.5f) * 0.02f;
                backgroundOverlay.color = new Color(0.04f, 0.05f, 0.09f, pulse);
            }
        }

        // ════════════════════════════════════════
        //  SHOW GAME OVER (STORY)
        // ════════════════════════════════════════

        public void ShowGameOver(int score, bool noRevive, bool usedRevive)
        {
            _isEndless = false;
            gameObject.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            if (gameOverTitle != null)
            {
                gameOverTitle.text = "CONNECTION LOST";
                gameOverTitle.color = Utils.HexToColor("#ef4444");
            }

            if (scoreText != null)
            {
                scoreText.text = Utils.FormatScore(score);
            }

            if (scoreLabel != null)
            {
                scoreLabel.text = "SYNAPSE SCORE";
            }

            if (scoreHighlight != null)
                scoreHighlight.SetActive(true);

            // Panels
            if (storyPanel != null)
                storyPanel.SetActive(true);
            if (endlessPanel != null)
                endlessPanel.SetActive(false);

            // Revive button
            if (reviveButton != null)
            {
                bool canRevive = !noRevive && !usedRevive;
                reviveButton.interactable = canRevive;

                if (reviveButtonText != null)
                {
                    reviveButtonText.text = canRevive
                        ? "ATTEMPT RECOVERY"
                        : "NO RECOVERY AVAILABLE";
                    reviveButtonText.color = canRevive
                        ? Utils.HexToColor("#f59e0b")
                        : Utils.HexToColor("#64748b");
                }
            }

            if (reviveLockedIndicator != null)
            {
                reviveLockedIndicator.SetActive(noRevive || usedRevive);
            }

            // Retry button
            if (retryButton != null)
            {
                var retryText = retryButton.GetComponentInChildren<TextMeshProUGUI>();
                if (retryText != null) retryText.text = "RETRY";
            }

            Debug.Log($"[GameOverUI] Story game over shown. Score={score}, noRevive={noRevive}");
        }

        // ════════════════════════════════════════
        //  SHOW ENDLESS OVER
        // ════════════════════════════════════════

        public void ShowEndlessOver(int score, int wave, int maxCombo, int hitCount)
        {
            _isEndless = true;
            gameObject.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            if (gameOverTitle != null)
            {
                gameOverTitle.text = "DEFENSE BREACHED";
                gameOverTitle.color = Utils.HexToColor("#ef4444");
            }

            if (scoreText != null)
            {
                scoreText.text = Utils.FormatScore(score);
            }

            if (scoreLabel != null)
            {
                scoreLabel.text = "ENDLESS SCORE";
            }

            if (scoreHighlight != null)
                scoreHighlight.SetActive(true);

            // Panels
            if (storyPanel != null)
                storyPanel.SetActive(false);
            if (endlessPanel != null)
                endlessPanel.SetActive(true);

            if (waveText != null)
                waveText.text = $"WAVE {wave}";
            if (maxComboText != null)
                maxComboText.text = $"MAX COMBO: x{maxCombo}";
            if (hitCountText != null)
                hitCountText.text = $"TOTAL HITS: {hitCount}";

            // Revive for endless
            if (reviveButton != null)
            {
                reviveButton.interactable = true;
                if (reviveButtonText != null)
                {
                    reviveButtonText.text = "EMERGENCY PROTOCOL";
                    reviveButtonText.color = Utils.HexToColor("#f59e0b");
                }
            }

            if (reviveLockedIndicator != null)
                reviveLockedIndicator.SetActive(false);

            // Retry button
            if (retryButton != null)
            {
                var retryText = retryButton.GetComponentInChildren<TextMeshProUGUI>();
                if (retryText != null) retryText.text = "DEFEND AGAIN";
            }

            Debug.Log($"[GameOverUI] Endless over shown. Score={score}, Wave={wave}");
        }

        // ════════════════════════════════════════
        //  SHOW / HIDE
        // ════════════════════════════════════════

        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        private void HideImmediate()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        // ════════════════════════════════════════
        //  CLEANUP
        // ════════════════════════════════════════

        private void OnDestroy()
        {
            if (reviveButton != null) reviveButton.onClick.RemoveAllListeners();
            if (retryButton != null) retryButton.onClick.RemoveAllListeners();
            if (menuButton != null) menuButton.onClick.RemoveAllListeners();
        }
    }
}
