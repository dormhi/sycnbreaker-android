/* =========================================
   MainMenuUI.cs — Main Menu Screen
   
   Displays the main menu with game title,
   Play button, and settings access.
   Cyberpunk-themed UI.
   ========================================= */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using SyncBreaker.Core;

namespace SyncBreaker.UI
{
    /// <summary>
    /// Main menu screen UI controller.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Title")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI playButtonText;
        [SerializeField] private Button settingsButton;

        [Header("Background")]
        [SerializeField] private Image backgroundPanel;
        [SerializeField] private float bgPulseSpeed = 1.5f;

        [Header("Version")]
        [SerializeField] private TextMeshProUGUI versionText;

        public event Action OnPlayClicked;

        private CanvasGroup _canvasGroup;
        private bool _visible;

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
            if (playButton != null)
            {
                playButton.onClick.AddListener(() => OnPlayClicked?.Invoke());
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(HandleSettings);
            }

            if (titleText != null)
            {
                titleText.text = "SYNCBREAKER";
            }

            if (subtitleText != null)
            {
                subtitleText.text = "Defend the system. Break the breach.";
            }

            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }

            // Start hidden
            HideImmediate();
        }

        private void Update()
        {
            if (!_visible) return;

            // Background breathing effect
            if (backgroundPanel != null)
            {
                float breathe = 0.96f + Mathf.Sin(Time.time * bgPulseSpeed) * 0.02f;
                backgroundPanel.color = new Color(0.04f, 0.05f, 0.09f, breathe);
            }

            // Title subtle glow pulse
            if (titleText != null)
            {
                float glow = 0.8f + Mathf.Sin(Time.time * 0.7f) * 0.2f;
                var c = titleText.color;
                titleText.color = new Color(c.r, c.g, c.b, glow);
            }
        }

        // ════════════════════════════════════════
        //  SHOW / HIDE
        // ════════════════════════════════════════

        public void Show()
        {
            _visible = true;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            gameObject.SetActive(true);

            Debug.Log("[MainMenuUI] Shown.");
        }

        public void Hide()
        {
            _visible = false;
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
            _visible = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        // ════════════════════════════════════════
        //  ACTIONS
        // ════════════════════════════════════════

        private void HandleSettings()
        {
            Debug.Log("[MainMenuUI] Settings clicked — not yet implemented.");
        }

        // ════════════════════════════════════════
        //  CLEANUP
        // ════════════════════════════════════════

        private void OnDestroy()
        {
            if (playButton != null)
                playButton.onClick.RemoveAllListeners();
            if (settingsButton != null)
                settingsButton.onClick.RemoveAllListeners();
        }
    }
}
