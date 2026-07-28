/* =========================================
   SaveSystem.cs — Central Save/Load System

   Manages all persistent game data:
   - Game settings (audio, graphics)
   - Player preferences
   - Full game state serialization

   Uses PlayerPrefs for simple data,
   JSON files for complex state.
   ========================================= */

using UnityEngine;
using System;
using System.IO;

namespace SyncBreaker.Systems
{
    /// <summary>
    /// Centralized save system.
    /// Works alongside LevelManager's own save for backward compat.
    /// </summary>
    public class SaveSystem
    {
        private const string SAVE_DIR = "SyncBreaker";
        private const string SETTINGS_FILE = "settings.json";
        private const string GAME_STATE_FILE = "gamestate.json";

        private GameSettings _settings;
        private string _savePath;

        public GameSettings Settings => _settings;

        // ════════════════════════════════════════
        //  INITIALIZATION
        // ════════════════════════════════════════

        public SaveSystem()
        {
            _savePath = Path.Combine(Application.persistentDataPath, SAVE_DIR);

            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }

            LoadSettings();
        }

        // ════════════════════════════════════════
        //  SETTINGS
        // ════════════════════════════════════════

        public void LoadSettings()
        {
            string path = Path.Combine(_savePath, SETTINGS_FILE);

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    _settings = JsonUtility.FromJson<GameSettings>(json);
                    Debug.Log("[SaveSystem] Settings loaded.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveSystem] Failed to load settings: {e.Message}");
                    _settings = new GameSettings();
                }
            }
            else
            {
                _settings = new GameSettings();
                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                string path = Path.Combine(_savePath, SETTINGS_FILE);
                string json = JsonUtility.ToJson(_settings, true);
                File.WriteAllText(path, json);
                Debug.Log("[SaveSystem] Settings saved.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save settings: {e.Message}");
            }
        }

        public void SetMasterVolume(float volume)
        {
            _settings.masterVolume = Mathf.Clamp01(volume);
            SaveSettings();
        }

        public void SetMusicVolume(float volume)
        {
            _settings.musicVolume = Mathf.Clamp01(volume);
            SaveSettings();
        }

        public void SetSfxVolume(float volume)
        {
            _settings.sfxVolume = Mathf.Clamp01(volume);
            SaveSettings();
        }

        public void SetVibration(bool enabled)
        {
            _settings.vibrationEnabled = enabled;
            SaveSettings();
        }

        public void SetLanguage(string langCode)
        {
            _settings.language = langCode;
            SaveSettings();
        }

        // ════════════════════════════════════════
        //  GAME STATE
        // ════════════════════════════════════════

        public void SaveGameState(GameStateData data)
        {
            try
            {
                string path = Path.Combine(_savePath, GAME_STATE_FILE);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
                Debug.Log("[SaveSystem] Game state saved.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save game state: {e.Message}");
            }
        }

        public GameStateData LoadGameState()
        {
            string path = Path.Combine(_savePath, GAME_STATE_FILE);

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var data = JsonUtility.FromJson<GameStateData>(json);
                    Debug.Log("[SaveSystem] Game state loaded.");
                    return data;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveSystem] Failed to load game state: {e.Message}");
                }
            }

            return null;
        }

        public void DeleteGameState()
        {
            string path = Path.Combine(_savePath, GAME_STATE_FILE);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        // ════════════════════════════════════════
        //  FULL RESET
        // ════════════════════════════════════════

        public void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // Delete save files
            string settingsPath = Path.Combine(_savePath, SETTINGS_FILE);
            string statePath = Path.Combine(_savePath, GAME_STATE_FILE);

            if (File.Exists(settingsPath)) File.Delete(settingsPath);
            if (File.Exists(statePath)) File.Delete(statePath);

            _settings = new GameSettings();
            Debug.Log("[SaveSystem] All data reset.");
        }

        // ════════════════════════════════════════
        //  UTILITY
        // ════════════════════════════════════════

        public string GetSavePath()
        {
            return _savePath;
        }

        public long GetTotalSaveSize()
        {
            long size = 0;
            if (Directory.Exists(_savePath))
            {
                foreach (string file in Directory.GetFiles(_savePath))
                {
                    size += new FileInfo(file).Length;
                }
            }
            return size;
        }
    }

    /// <summary>
    /// Serializable game settings.
    /// </summary>
    [System.Serializable]
    public class GameSettings
    {
        public float masterVolume = 0.8f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 1.0f;
        public bool vibrationEnabled = true;
        public string language = "en";
        public int graphicsQuality = 2; // 0=low, 1=medium, 2=high
        public bool showFps = false;
    }

    /// <summary>
    /// Serializable game state for save/load.
    /// </summary>
    [System.Serializable]
    public class GameStateData
    {
        public int currentLevelIndex;
        public int totalScore;
        public int highestLevelUnlocked;
        public int endlessBestScore;
        public int endlessBestWave;
        public int totalPlayTimeSeconds;
        public int totalGamesPlayed;
        public string lastSaveDate;
    }
}
