/* =========================================
   SettingsUI.cs — Settings Panel
   
   In-game settings menu:
   - Master/Music/SFX volume sliders
   - Haptic toggle
   - Mute toggle
   - Credits link
   
   Accessible from MainMenu and pause.
   ========================================= */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SyncBreaker.Systems;

namespace SyncBreaker.UI
{
    /// <summary>
    /// Settings panel UI controller.
    /// Reads/writes SoundManager volume preferences.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("Volume Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Toggles")]
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private Toggle hapticToggle;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI masterLabel;
        [SerializeField] private TextMeshProUGUI musicLabel;
        [SerializeField] private TextMeshProUGUI sfxLabel;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;

        // ── Events ──
        public System.Action OnClose;

        // ════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════

        private void OnEnable()
        {
            LoadCurrentValues();
            BindListeners();
        }

        private void OnDisable()
        {
            UnbindListeners();
            SoundManager.Instance?.SaveVolumePreferences();
        }

        // ════════════════════════════════════════
        //  BINDING
        // ════════════════════════════════════════

        private void LoadCurrentValues()
        {
            var sound = SoundManager.Instance;
            if (sound == null) return;

            if (masterSlider != null)
            {
                masterSlider.value = sound.MasterVolume;
                UpdateLabel(masterLabel, sound.MasterVolume);
            }
            if (musicSlider != null)
            {
                musicSlider.value = sound.MusicVolume;
                UpdateLabel(musicLabel, sound.MusicVolume);
            }
            if (sfxSlider != null)
            {
                sfxSlider.value = sound.SFXVolume;
                UpdateLabel(sfxLabel, sound.SFXVolume);
            }
            if (muteToggle != null)
                muteToggle.isOn = sound.Muted;
            if (hapticToggle != null)
                hapticToggle.isOn = PlayerPrefs.GetInt("SB_Haptics", 1) == 1;
        }

        private void BindListeners()
        {
            if (masterSlider != null)
                masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (musicSlider != null)
                musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(OnSFXChanged);
            if (muteToggle != null)
                muteToggle.onValueChanged.AddListener(OnMuteChanged);
            if (hapticToggle != null)
                hapticToggle.onValueChanged.AddListener(OnHapticChanged);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);
        }

        private void UnbindListeners()
        {
            if (masterSlider != null)
                masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
            if (muteToggle != null)
                muteToggle.onValueChanged.RemoveListener(OnMuteChanged);
            if (hapticToggle != null)
                hapticToggle.onValueChanged.RemoveListener(OnHapticChanged);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
            if (resetButton != null)
                resetButton.onClick.RemoveListener(OnResetClicked);
        }

        // ════════════════════════════════════════
        //  CALLBACKS
        // ════════════════════════════════════════

        private void OnMasterChanged(float value)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.MasterVolume = value;
            UpdateLabel(masterLabel, value);
        }

        private void OnMusicChanged(float value)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.MusicVolume = value;
            UpdateLabel(musicLabel, value);
        }

        private void OnSFXChanged(float value)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.SFXVolume = value;
            UpdateLabel(sfxLabel, value);

            // Play a test SFX
            SoundManager.Instance?.PlaySFX(SFX.ButtonClick, value);
        }

        private void OnMuteChanged(bool muted)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.Muted = muted;
        }

        private void OnHapticChanged(bool enabled)
        {
            PlayerPrefs.SetInt("SB_Haptics", enabled ? 1 : 0);
            PlayerPrefs.Save();

            // Test haptic
            if (enabled)
                SoundManager.Instance?.HapticLight();
        }

        private void OnCloseClicked()
        {
            SoundManager.Instance?.PlaySFX(SFX.MenuClose);
            OnClose?.Invoke();
            gameObject.SetActive(false);
        }

        private void OnResetClicked()
        {
            if (masterSlider != null) masterSlider.value = 1f;
            if (musicSlider != null) musicSlider.value = 0.7f;
            if (sfxSlider != null) sfxSlider.value = 1f;
            if (muteToggle != null) muteToggle.isOn = false;
            if (hapticToggle != null) hapticToggle.isOn = true;
        }

        // ════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════

        private void UpdateLabel(TextMeshProUGUI label, float value)
        {
            if (label != null)
                label.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        /// <summary>
        /// Show the settings panel.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            SoundManager.Instance?.PlaySFX(SFX.MenuOpen);
        }
    }
}
