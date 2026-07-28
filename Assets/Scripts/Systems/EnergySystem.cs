/* =========================================
   EnergySystem.cs — Energy/Stamina System
   
   Manages player energy for level attempts.
   Time-based regeneration (like mobile games).
   Prevents infinite play with energy cap.
   ========================================= */

using UnityEngine;
using System;

namespace SyncBreaker.Systems
{
    /// <summary>
    /// Mobile energy/stamina system.
    /// Each level attempt costs energy; energy regenerates over time.
    /// </summary>
    public class EnergySystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxEnergy = 10;
        [SerializeField] private int energyPerLevel = 2;
        [SerializeField] private int energyPerEndless = 1;
        [SerializeField] private float regenIntervalSeconds = 300f; // 5 minutes per energy
        [SerializeField] private int maxRegenCap = 10;
        [SerializeField] private int startingEnergy = 5;

        [Header("Ad Support (Future)")]
        [SerializeField] private int adRewardEnergy = 3;

        public int CurrentEnergy { get; private set; }
        public int MaxEnergy => maxEnergy;

        private DateTime _lastRegenTime;
        private const string SAVE_KEY_ENERGY = "sb_energy";
        private const string SAVE_KEY_LAST_REGEN = "sb_last_regen";

        public event Action<int> OnEnergyChanged;

        // ════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════

        private void Start()
        {
            LoadEnergy();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveEnergy();
            }
            else
            {
                RecalculateRegen();
            }
        }

        private void OnApplicationQuit()
        {
            SaveEnergy();
        }

        // ════════════════════════════════════════
        //  ENERGY QUERIES
        // ════════════════════════════════════════

        public bool HasEnoughForLevel()
        {
            return CurrentEnergy >= energyPerLevel;
        }

        public bool HasEnoughForEndless()
        {
            return CurrentEnergy >= energyPerEndless;
        }

        public bool HasEnough(int amount)
        {
            return CurrentEnergy >= amount;
        }

        // ════════════════════════════════════════
        //  ENERGY CONSUMPTION
        // ════════════════════════════════════════

        public bool ConsumeForLevel()
        {
            return Consume(energyPerLevel);
        }

        public bool ConsumeForEndless()
        {
            return Consume(energyPerEndless);
        }

        public bool Consume(int amount)
        {
            RecalculateRegen();

            if (CurrentEnergy < amount)
            {
                Debug.LogWarning($"[EnergySystem] Not enough energy: {CurrentEnergy}/{amount}");
                return false;
            }

            CurrentEnergy -= amount;

            // Start regen timer if we're below cap
            if (CurrentEnergy < maxRegenCap)
            {
                _lastRegenTime = DateTime.UtcNow;
            }

            OnEnergyChanged?.Invoke(CurrentEnergy);
            SaveEnergy();

            Debug.Log($"[EnergySystem] Consumed {amount} energy. Remaining: {CurrentEnergy}");
            return true;
        }

        // ════════════════════════════════════════
        //  ENERGY ADDITION
        // ════════════════════════════════════════

        public void AddEnergy(int amount)
        {
            CurrentEnergy = Mathf.Min(CurrentEnergy + amount, maxEnergy);
            OnEnergyChanged?.Invoke(CurrentEnergy);
            SaveEnergy();

            Debug.Log($"[EnergySystem] Added {amount} energy. Current: {CurrentEnergy}");
        }

        public void RewardAdEnergy()
        {
            AddEnergy(adRewardEnergy);
        }

        // ════════════════════════════════════════
        //  REGENERATION
        // ════════════════════════════════════════

        public TimeSpan GetTimeUntilNextRegen()
        {
            RecalculateRegen();

            if (CurrentEnergy >= maxRegenCap)
                return TimeSpan.Zero;

            double elapsed = (DateTime.UtcNow - _lastRegenTime).TotalSeconds;
            double remaining = regenIntervalSeconds - elapsed;

            return remaining > 0 ? TimeSpan.FromSeconds(remaining) : TimeSpan.Zero;
        }

        public int GetRegenCount()
        {
            RecalculateRegen();
            return Mathf.Min(CurrentEnergy, maxRegenCap);
        }

        private void RecalculateRegen()
        {
            if (CurrentEnergy >= maxRegenCap) return;

            double elapsed = (DateTime.UtcNow - _lastRegenTime).TotalSeconds;
            int regenCount = Mathf.FloorToInt((float)(elapsed / regenIntervalSeconds));

            if (regenCount > 0)
            {
                int regenAmount = Mathf.Min(regenCount, maxRegenCap - CurrentEnergy);
                CurrentEnergy += regenAmount;
                _lastRegenTime = DateTime.UtcNow;
                OnEnergyChanged?.Invoke(CurrentEnergy);

                Debug.Log($"[EnergySystem] Regenerated {regenAmount} energy. Current: {CurrentEnergy}");
            }
        }

        // ════════════════════════════════════════
        //  PERSISTENCE
        // ════════════════════════════════════════

        private void SaveEnergy()
        {
            PlayerPrefs.SetInt(SAVE_KEY_ENERGY, CurrentEnergy);
            PlayerPrefs.SetString(SAVE_KEY_LAST_REGEN, _lastRegenTime.ToBinary().ToString());
            PlayerPrefs.Save();
        }

        private void LoadEnergy()
        {
            CurrentEnergy = PlayerPrefs.GetInt(SAVE_KEY_ENERGY, startingEnergy);

            string lastRegenStr = PlayerPrefs.GetString(SAVE_KEY_LAST_REGEN, "");
            if (long.TryParse(lastRegenStr, out long binary))
            {
                _lastRegenTime = DateTime.FromBinary(binary);
            }
            else
            {
                _lastRegenTime = DateTime.UtcNow;
            }

            // Recalculate regeneration on load
            RecalculateRegen();

            OnEnergyChanged?.Invoke(CurrentEnergy);
            Debug.Log($"[EnergySystem] Loaded: {CurrentEnergy}/{maxEnergy} energy");
        }
    }
}
