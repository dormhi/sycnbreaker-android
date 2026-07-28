/* =========================================
   SoundManager.cs — Audio System
   
   Centralized sound management:
   - SFX playback (pooled AudioSources)
   - Music playback with crossfade
   - Volume control (master/music/sfx)
   - Haptic feedback (vibration)
   - Pitch variation for organic feel
   - Save/load volume preferences
   
   Ported from: js/AudioManager.js concept
   ========================================= */

using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;

namespace SyncBreaker.Systems
{
    /// <summary>
    /// Sound effect identifiers.
    /// Each maps to an AudioClip in the SFX library.
    /// </summary>
    public enum SFX
    {
        // Timing Bar
        HitPerfect,
        HitGood,
        HitMiss,
        ComboUp,
        ComboBreak,

        // Lockpick
        LockpickStart,
        LockpickNodeSolve,
        LockpickNodeFail,
        LockpickSuccess,
        LockpickFail,

        // UI
        ButtonClick,
        ButtonHover,
        MenuOpen,
        MenuClose,

        // Game Flow
        LevelStart,
        LevelComplete,
        LevelFail,
        GameOver,
        Revive,

        // Misc
        CountdownTick,
        TimerWarning,
        EnergyRefill,
        WaveTransition
    }

    /// <summary>
    /// Centralized audio management singleton.
    /// Handles SFX, music, volume, and haptics.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        // ── Singleton ──
        public static SoundManager Instance { get; private set; }

        // ── Audio Mixer ──
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";

        // ── Music ──
        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameplayMusic;
        [SerializeField] private AudioClip endlessMusic;
        [SerializeField] private AudioClip bossMusic;
        [SerializeField] private float musicCrossfadeDuration = 1.5f;

        // ── SFX Library ──
        [Header("SFX Library")]
        [SerializeField] private SFXEntry[] sfxLibrary;

        // ── Configuration ──
        [Header("Settings")]
        [SerializeField] private int sfxPoolSize = 8;
        [SerializeField] private float pitchVariation = 0.08f;
        [SerializeField] private bool enableHaptics = true;

        // ── Internal ──
        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private AudioSource _activeMusicSource;
        private readonly List<AudioSource> _sfxPool = new();
        private int _sfxPoolIndex;

        // Crossfade
        private bool _crossfading;
        private float _crossfadeTimer;
        private AudioSource _fadeInSource;
        private AudioSource _fadeOutSource;

        // Volume (0-1 range, converted to dB for mixer)
        private float _masterVolume = 1f;
        private float _musicVolume = 0.7f;
        private float _sfxVolume = 1f;
        private bool _muted;

        // ── Events ──
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;

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

            InitializeMusicSources();
            InitializeSFXPool();
            LoadVolumePreferences();
        }

        private void Update()
        {
            if (_crossfading)
            {
                UpdateCrossfade();
            }
        }

        // ════════════════════════════════════════
        //  INITIALIZATION
        // ════════════════════════════════════════

        private void InitializeMusicSources()
        {
            // Two AudioSources for crossfading
            _musicSourceA = gameObject.AddComponent<AudioSource>();
            _musicSourceA.loop = true;
            _musicSourceA.playOnAwake = false;
            _musicSourceA.volume = 0f;
            if (audioMixer != null)
            {
                var groups = audioMixer.FindMatchingGroups("Music");
                if (groups.Length > 0) _musicSourceA.outputAudioMixerGroup = groups[0];
            }

            _musicSourceB = gameObject.AddComponent<AudioSource>();
            _musicSourceB.loop = true;
            _musicSourceB.playOnAwake = false;
            _musicSourceB.volume = 0f;
            if (audioMixer != null)
            {
                var groups = audioMixer.FindMatchingGroups("Music");
                if (groups.Length > 0) _musicSourceB.outputAudioMixerGroup = groups[0];
            }

            _activeMusicSource = _musicSourceA;
        }

        private void InitializeSFXPool()
        {
            AudioMixerGroup sfxGroup = null;
            if (audioMixer != null)
            {
                var groups = audioMixer.FindMatchingGroups("SFX");
                if (groups.Length > 0) sfxGroup = groups[0];
            }

            for (int i = 0; i < sfxPoolSize; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                if (sfxGroup != null) source.outputAudioMixerGroup = sfxGroup;
                _sfxPool.Add(source);
            }
        }

        // ════════════════════════════════════════
        //  SFX PLAYBACK
        // ════════════════════════════════════════

        /// <summary>
        /// Play a sound effect by enum identifier.
        /// </summary>
        public void PlaySFX(SFX sfx, float volumeScale = 1f)
        {
            if (_muted) return;

            var clip = GetClip(sfx);
            if (clip == null)
            {
                Debug.LogWarning($"[SoundManager] No clip for SFX: {sfx}");
                return;
            }

            PlayClip(clip, volumeScale);
        }

        /// <summary>
        /// Play a raw AudioClip as SFX.
        /// </summary>
        public void PlayClip(AudioClip clip, float volumeScale = 1f)
        {
            if (_muted || clip == null) return;

            var source = GetNextSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume * volumeScale;
            source.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            source.Play();
        }

        /// <summary>
        /// Play a hit feedback sound with pitch based on combo.
        /// Higher combo = higher pitch = feels more intense.
        /// </summary>
        public void PlayHitFeedback(SFX sfx, int combo)
        {
            if (_muted) return;

            var clip = GetClip(sfx);
            if (clip == null) return;

            var source = GetNextSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume;

            // Pitch increases with combo (capped)
            float comboPitch = 1f + Mathf.Min(combo * 0.03f, 0.5f);
            source.pitch = comboPitch + UnityEngine.Random.Range(-0.02f, 0.02f);
            source.Play();
        }

        private AudioSource GetNextSFXSource()
        {
            var source = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Count;
            return source;
        }

        private AudioClip GetClip(SFX sfx)
        {
            if (sfxLibrary == null) return null;

            foreach (var entry in sfxLibrary)
            {
                if (entry.id == sfx) return entry.clip;
            }
            return null;
        }

        // ════════════════════════════════════════
        //  MUSIC PLAYBACK
        // ════════════════════════════════════════

        /// <summary>
        /// Play menu music with crossfade.
        /// </summary>
        public void PlayMenuMusic()
        {
            if (menuMusic != null)
                CrossfadeToMusic(menuMusic);
        }

        /// <summary>
        /// Play gameplay music with crossfade.
        /// </summary>
        public void PlayGameplayMusic()
        {
            if (gameplayMusic != null)
                CrossfadeToMusic(gameplayMusic);
        }

        /// <summary>
        /// Play endless mode music.
        /// </summary>
        public void PlayEndlessMusic()
        {
            if (endlessMusic != null)
                CrossfadeToMusic(endlessMusic);
            else if (gameplayMusic != null)
                CrossfadeToMusic(gameplayMusic);
        }

        /// <summary>
        /// Play boss music.
        /// </summary>
        public void PlayBossMusic()
        {
            if (bossMusic != null)
                CrossfadeToMusic(bossMusic);
        }

        /// <summary>
        /// Stop music with fade out.
        /// </summary>
        public void StopMusic(float fadeTime = 1f)
        {
            if (_activeMusicSource != null && _activeMusicSource.isPlaying)
            {
                _fadeOutSource = _activeMusicSource;
                _fadeInSource = null;
                _crossfading = true;
                _crossfadeTimer = 0f;
                musicCrossfadeDuration = fadeTime;
            }
        }

        private void CrossfadeToMusic(AudioClip clip)
        {
            if (clip == null) return;

            // Don't restart if already playing this clip
            if (_activeMusicSource.clip == clip && _activeMusicSource.isPlaying)
                return;

            // Determine which source to fade in
            _fadeOutSource = _activeMusicSource;
            _fadeInSource = (_activeMusicSource == _musicSourceA) ? _musicSourceB : _musicSourceA;
            _activeMusicSource = _fadeInSource;

            _fadeInSource.clip = clip;
            _fadeInSource.volume = 0f;
            _fadeInSource.Play();

            _crossfading = true;
            _crossfadeTimer = 0f;
        }

        private void UpdateCrossfade()
        {
            _crossfadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_crossfadeTimer / musicCrossfadeDuration);

            // Smooth step for nicer transition
            float smooth = t * t * (3f - 2f * t);

            if (_fadeInSource != null)
                _fadeInSource.volume = smooth * _musicVolume;

            if (_fadeOutSource != null)
                _fadeOutSource.volume = (1f - smooth) * _musicVolume;

            if (t >= 1f)
            {
                _crossfading = false;
                if (_fadeOutSource != null)
                    _fadeOutSource.Stop();
            }
        }

        // ════════════════════════════════════════
        //  VOLUME CONTROL
        // ════════════════════════════════════════

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                ApplyVolumes();
                OnMasterVolumeChanged?.Invoke(_masterVolume);
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                if (_activeMusicSource != null && !_crossfading)
                    _activeMusicSource.volume = _musicVolume;
                ApplyVolumes();
                OnMusicVolumeChanged?.Invoke(_musicVolume);
            }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                ApplyVolumes();
                OnSFXVolumeChanged?.Invoke(_sfxVolume);
            }
        }

        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                ApplyVolumes();
            }
        }

        private void ApplyVolumes()
        {
            if (audioMixer == null) return;

            float master = _muted ? -80f : LinearToDecibel(_masterVolume);
            float music = LinearToDecibel(_musicVolume);
            float sfx = LinearToDecibel(_sfxVolume);

            audioMixer.SetFloat(masterVolumeParam, master);
            audioMixer.SetFloat(musicVolumeParam, music);
            audioMixer.SetFloat(sfxVolumeParam, sfx);
        }

        private float LinearToDecibel(float linear)
        {
            return linear > 0.001f ? 20f * Mathf.Log10(linear) : -80f;
        }

        // ════════════════════════════════════════
        //  HAPTIC FEEDBACK
        // ════════════════════════════════════════

        /// <summary>
        /// Trigger light haptic feedback (for hits).
        /// </summary>
        public void HapticLight()
        {
            if (!enableHaptics) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// Trigger medium haptic feedback (for combo milestones).
        /// </summary>
        public void HapticMedium()
        {
            if (!enableHaptics) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// Trigger heavy haptic feedback (for failures, game over).
        /// </summary>
        public void HapticHeavy()
        {
            if (!enableHaptics) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        // ════════════════════════════════════════
        //  PERSISTENCE
        // ════════════════════════════════════════

        private void LoadVolumePreferences()
        {
            _masterVolume = PlayerPrefs.GetFloat("SB_MasterVol", 1f);
            _musicVolume = PlayerPrefs.GetFloat("SB_MusicVol", 0.7f);
            _sfxVolume = PlayerPrefs.GetFloat("SB_SFXVol", 1f);
            _muted = PlayerPrefs.GetInt("SB_Muted", 0) == 1;
            enableHaptics = PlayerPrefs.GetInt("SB_Haptics", 1) == 1;
            ApplyVolumes();
        }

        public void SaveVolumePreferences()
        {
            PlayerPrefs.SetFloat("SB_MasterVol", _masterVolume);
            PlayerPrefs.SetFloat("SB_MusicVol", _musicVolume);
            PlayerPrefs.SetFloat("SB_SFXVol", _sfxVolume);
            PlayerPrefs.SetInt("SB_Muted", _muted ? 1 : 0);
            PlayerPrefs.SetInt("SB_Haptics", enableHaptics ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveVolumePreferences();
        }

        private void OnApplicationQuit()
        {
            SaveVolumePreferences();
        }
    }

    /// <summary>
    /// Maps a SFX enum to an AudioClip.
    /// Used in the Inspector to build the SFX library.
    /// </summary>
    [System.Serializable]
    public class SFXEntry
    {
        public SFX id;
        public AudioClip clip;
    }
}
