/* =========================================
   HubUI.cs — Level Selection Hub
   
   Displays all available levels with
   lock/unlock/completed status, difficulty,
   best scores, and endless mode button.
   
   Cyberpunk-themed level select hub.
   ========================================= */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using SyncBreaker.Core;
using SyncBreaker.Gameplay;
using SyncBreaker.Systems;

namespace SyncBreaker.UI
{
    /// <summary>
    /// Level selection hub screen.
    /// </summary>
    public class HubUI : MonoBehaviour
    {
        [Header("Title")]
        [SerializeField] private TextMeshProUGUI hubTitleText;
        [SerializeField] private TextMeshProUGUI energyText;

        [Header("Level List")]
        [SerializeField] private Transform levelListContainer;
        [SerializeField] private GameObject levelButtonPrefab;

        [Header("Endless Mode")]
        [SerializeField] private Button endlessButton;
        [SerializeField] private TextMeshProUGUI endlessBestText;

        [Header("Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private GameObject resetConfirmPanel;

        [Header("Colors")]
        [SerializeField] private Color lockedColor = new Color(0.39f, 0.45f, 0.55f, 1f);
        [SerializeField] private Color unlockedColor = new Color(0.23f, 0.51f, 0.96f, 1f);
        [SerializeField] private Color completedColor = new Color(0.13f, 0.77f, 0.37f, 1f);

        public event Action<int> OnLevelSelected;
        public event Action OnEndlessSelected;
        public event Action OnBackClicked;
        public event Action OnResetClicked;
#pragma warning disable CS0067 // Event is used by external state handlers
        public event Action<int> OnLockpickShortcut;
#pragma warning restore CS0067

        private LevelManager _levels;
        private CanvasGroup _canvasGroup;
        private List<GameObject> _levelButtons = new();

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
            if (endlessButton != null)
                endlessButton.onClick.AddListener(() => OnEndlessSelected?.Invoke());

            if (backButton != null)
                backButton.onClick.AddListener(() => OnBackClicked?.Invoke());

            if (resetButton != null)
                resetButton.onClick.AddListener(HandleResetClick);

            if (resetConfirmPanel != null)
                resetConfirmPanel.SetActive(false);

            HideImmediate();
        }

        private void Update()
        {
            UpdateEnergyDisplay();
        }

        // ════════════════════════════════════════
        //  BINDING
        // ════════════════════════════════════════

        public void Bind(LevelManager levels)
        {
            _levels = levels;
            BuildLevelList();
            UpdateEndlessSection();
        }

        // ════════════════════════════════════════
        //  LEVEL LIST
        // ════════════════════════════════════════

        private void BuildLevelList()
        {
            ClearLevelButtons();

            if (_levels == null || levelButtonPrefab == null || levelListContainer == null) return;

            for (int i = 0; i < _levels.LevelCount; i++)
            {
                var data = _levels.GetLevelData(i);
                var progress = _levels.Progress[i];

                var go = Instantiate(levelButtonPrefab, levelListContainer);
                _levelButtons.Add(go);

                int index = i; // capture for closure

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnLevelSelected?.Invoke(index));
                }

                // Populate button UI
                SetLevelButtonVisuals(go, data, progress, index);
            }
        }

        private void SetLevelButtonVisuals(GameObject go, LevelData data, LevelProgress progress, int index)
        {
            // Find child TextMeshProUGUI elements
            var texts = go.GetComponentsInChildren<TextMeshProUGUI>();

            // Expected structure: LevelNameText, DescriptionText, StatusText, ScoreText, DifficultyStars...
            foreach (var text in texts)
            {
                string name = text.gameObject.name.ToLower();

                if (name.Contains("name") || name.Contains("title"))
                {
                    text.text = data.levelName;
                    text.color = progress.completed ? completedColor
                        : progress.unlocked ? unlockedColor : lockedColor;
                }
                else if (name.Contains("desc") || name.Contains("description"))
                {
                    text.text = data.description;
                }
                else if (name.Contains("status"))
                {
                    text.text = progress.completed ? "CLEANED"
                        : progress.unlocked ? "AVAILABLE" : "LOCKED";
                }
                else if (name.Contains("score") || name.Contains("best"))
                {
                    text.text = progress.bestScore > 0
                        ? $"BEST: {Utils.FormatScore(progress.bestScore, 4)}"
                        : "";
                }
                else if (name.Contains("diff") || name.Contains("difficulty"))
                {
                    text.text = $"DIFFICULTY: {data.difficulty}/6";
                }
            }

            // Update button interactability
            var button = go.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = progress.unlocked || progress.completed;
            }

            // Update background images for visual state
            var images = go.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img.gameObject.name.ToLower().Contains("lock") && !progress.unlocked)
                {
                    img.gameObject.SetActive(true);
                }
                else if (img.gameObject.name.ToLower().Contains("check") || img.gameObject.name.ToLower().Contains("done"))
                {
                    img.gameObject.SetActive(progress.completed);
                }
            }
        }

        private void ClearLevelButtons()
        {
            foreach (var go in _levelButtons)
            {
                if (go != null) Destroy(go);
            }
            _levelButtons.Clear();
        }

        // ════════════════════════════════════════
        //  ENDLESS SECTION
        // ════════════════════════════════════════

        private void UpdateEndlessSection()
        {
            if (endlessButton != null)
            {
                // Unlock endless after completing first level
                bool hasAnyCompleted = _levels?.Progress?.Exists(p => p.completed) ?? false;
                endlessButton.interactable = hasAnyCompleted;
            }

            if (endlessBestText != null && _levels != null)
            {
                int best = _levels.EndlessBest;
                if (best > 0)
                {
                    endlessBestText.text = $"BEST: {Utils.FormatScore(best, 6)}";
                    endlessBestText.gameObject.SetActive(true);
                }
                else
                {
                    endlessBestText.gameObject.SetActive(false);
                }
            }
        }

        // ════════════════════════════════════════
        //  ENERGY DISPLAY
        // ════════════════════════════════════════

        private void UpdateEnergyDisplay()
        {
            if (energyText == null) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            var energy = gm.GetComponent<EnergySystem>();
            if (energy != null)
            {
                energyText.text = $"ENERGY: {energy.CurrentEnergy}/{energy.MaxEnergy}";

                var regenTime = energy.GetTimeUntilNextRegen();
                if (regenTime.TotalSeconds > 0 && energy.CurrentEnergy < energy.MaxEnergy)
                {
                    energyText.text += $" | +1 in {regenTime.Minutes}:{regenTime.Seconds:D2}";
                }
            }
        }

        // ════════════════════════════════════════
        //  RESET CONFIRMATION
        // ════════════════════════════════════════

        private void HandleResetClick()
        {
            if (resetConfirmPanel != null)
            {
                bool showing = resetConfirmPanel.activeSelf;
                resetConfirmPanel.SetActive(!showing);

                if (!showing)
                {
                    // Set up confirm/cancel buttons
                    var buttons = resetConfirmPanel.GetComponentsInChildren<Button>();
                    foreach (var btn in buttons)
                    {
                        btn.onClick.RemoveAllListeners();
                        if (btn.gameObject.name.ToLower().Contains("confirm"))
                        {
                            btn.onClick.AddListener(() =>
                            {
                                OnResetClicked?.Invoke();
                                resetConfirmPanel.SetActive(false);
                            });
                        }
                        else if (btn.gameObject.name.ToLower().Contains("cancel"))
                        {
                            btn.onClick.AddListener(() => resetConfirmPanel.SetActive(false));
                        }
                    }
                }
            }
            else
            {
                OnResetClicked?.Invoke();
            }
        }

        // ════════════════════════════════════════
        //  SHOW / HIDE
        // ════════════════════════════════════════

        public void Show()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            gameObject.SetActive(true);
            Debug.Log("[HubUI] Shown.");
        }

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
            if (endlessButton != null) endlessButton.onClick.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();
            if (resetButton != null) resetButton.onClick.RemoveAllListeners();
            ClearLevelButtons();
        }
    }
}
