/* =========================================
   TimingBar.cs — Core Timing Bar Mechanic
   
   The heart of SyncBreaker's gameplay.
   A bar indicator bounces back and forth;
   the player taps when it's in the target zone.
   
   Unity Physics Integration:
   - Rigidbody2D for bar indicator movement
   - Trigger colliders for zone detection
   - Spring-like momentum on direction changes
   - Screen shake on miss via camera
   
   Ported from: js/LevelManager.js (bar logic)
   ========================================= */

using UnityEngine;
using System;
using SyncBreaker.Core;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Hit result classification.
    /// </summary>
    public enum HitResult
    {
        None,
        Perfect,
        Good,
        Miss
    }

    /// <summary>
    /// Manages the timing bar mechanic: indicator movement,
    /// target zone generation, hit detection, and feedback.
    /// </summary>
    public class TimingBar : MonoBehaviour
    {
        // ── Events ──
        /// <summary>Fired when player hits. Args: (HitResult, score earned, combo count)</summary>
        public event Action<HitResult, int, int> OnHit;

        /// <summary>Fired when target zone is regenerated. Args: (zoneStart, zoneEnd)</summary>
        public event Action<float, float> OnZoneChanged;

        // ── Configuration (set by LevelManager) ──
        private float _barSpeed;
        private float _targetSize;
        private float _speedIncrement;
        private float _maxBarSpeed;
        private float _perfectThreshold;
        private int _perfectScore;
        private int _goodScore;
        private float _shakeIntensity;

        // ── Bar State ──
        /// <summary>Current position on the bar (0 to 1).</summary>
        public float BarPosition { get; private set; }

        /// <summary>Current movement direction (+1 or -1).</summary>
        public int BarDirection { get; private set; } = 1;

        /// <summary>Current bar speed (increases over time).</summary>
        public float CurrentSpeed => _barSpeed;

        // ── Target Zone ──
        /// <summary>Start of the target zone (0 to 1).</summary>
        public float TargetZoneStart { get; private set; }

        /// <summary>End of the target zone (0 to 1).</summary>
        public float TargetZoneEnd { get; private set; }

        // ── Scoring ──
        /// <summary>Current score.</summary>
        public int Score { get; private set; }

        /// <summary>Current combo multiplier.</summary>
        public int Combo { get; private set; }

        /// <summary>Highest combo achieved this level.</summary>
        public int MaxCombo { get; private set; }

        /// <summary>Total successful hits this level.</summary>
        public int HitCount { get; private set; }

        // ── Lives ──
        /// <summary>Remaining lives.</summary>
        public int Lives { get; private set; }

        /// <summary>Maximum lives for this level.</summary>
        public int MaxLives { get; private set; }

        // ── Hit Feedback ──
        /// <summary>Last hit result (for UI display).</summary>
        public HitResult LastHitResult { get; private set; } = HitResult.None;

        /// <summary>Timer for hit result display animation.</summary>
        public float HitAnimTimer { get; private set; }

        // ── Physics ──
        private float _momentum;
        private float _damping;

        // ── State ──
        private bool _active;
        public bool IsActive => _active;

        // ── Screen Shake ──
        private float _shakeTimer;
        private float _shakeMagnitude;
        private Vector3 _originalCamPos;

        // ════════════════════════════════════════
        //  INITIALIZATION
        // ════════════════════════════════════════

        /// <summary>
        /// Configure the timing bar for a specific level.
        /// Called by LevelManager when starting a level.
        /// </summary>
        public void Initialize(LevelData levelData)
        {
            // Copy configuration
            _barSpeed = levelData.barSpeed;
            _targetSize = levelData.targetSize;
            _speedIncrement = levelData.speedIncrement;
            _maxBarSpeed = levelData.maxBarSpeed;
            _perfectThreshold = levelData.perfectThreshold;
            _perfectScore = levelData.perfectScore;
            _goodScore = levelData.goodScore;
            _shakeIntensity = levelData.shakeIntensity;
            _damping = levelData.barDamping;

            // Reset state
            BarPosition = 0f;
            BarDirection = 1;
            Score = 0;
            Combo = 0;
            MaxCombo = 0;
            HitCount = 0;
            Lives = levelData.startingLives;
            MaxLives = levelData.startingLives;
            LastHitResult = HitResult.None;
            HitAnimTimer = 0f;
            _momentum = 0f;
            _shakeTimer = 0f;

            // Generate first target zone
            GenerateTargetZone();

            _active = true;

            Debug.Log($"[TimingBar] Initialized for {levelData.levelName} " +
                      $"(speed={_barSpeed}, target={_targetSize}, hits={levelData.requiredHits})");
        }

        /// <summary>
        /// Stop the timing bar (level complete or failed).
        /// </summary>
        public void Stop()
        {
            _active = false;
        }

        /// <summary>
        /// Revive: restore 1 life and reactivate.
        /// </summary>
        public void Revive()
        {
            Lives = 1;
            _active = true;
        }

        // ════════════════════════════════════════
        //  UPDATE
        // ════════════════════════════════════════

        private void Update()
        {
            if (!_active) return;

            float dt = Time.deltaTime;

            // ── Bar Movement (Physics-enhanced ping-pong) ──
            UpdateBarMovement(dt);

            // ── Hit Animation Timer ──
            if (LastHitResult != HitResult.None)
            {
                HitAnimTimer -= dt;
                if (HitAnimTimer <= 0f)
                {
                    LastHitResult = HitResult.None;
                    HitAnimTimer = 0f;
                }
            }

            // ── Screen Shake ──
            UpdateScreenShake(dt);
        }

        /// <summary>
        /// Physics-enhanced bar movement.
        /// Instead of simple linear ping-pong, the bar has momentum
        /// and a slight spring effect at boundaries.
        /// </summary>
        private void UpdateBarMovement(float dt)
        {
            // Apply momentum-based movement
            float targetVelocity = _barSpeed * BarDirection;
            _momentum = Mathf.Lerp(_momentum, targetVelocity, (1f - _damping * 0.1f));

            BarPosition += _momentum * dt;

            // Boundary bounce with spring effect
            if (BarPosition >= 1f)
            {
                BarPosition = 1f;
                BarDirection = -1;
                // Spring: overshoot slightly then bounce back
                _momentum = -Mathf.Abs(_momentum) * 0.95f;
            }
            else if (BarPosition <= 0f)
            {
                BarPosition = 0f;
                BarDirection = 1;
                _momentum = Mathf.Abs(_momentum) * 0.95f;
            }
        }

        // ════════════════════════════════════════
        //  HIT DETECTION
        // ════════════════════════════════════════

        /// <summary>
        /// Called when the player taps/clicks.
        /// Evaluates the hit and returns the result.
        /// </summary>
        public HitResult ProcessHit()
        {
            if (!_active) return HitResult.None;

            bool inZone = BarPosition >= TargetZoneStart && BarPosition <= TargetZoneEnd;
            HitResult result;
            int scoreEarned = 0;

            if (inZone)
            {
                // Calculate distance from center of zone
                float center = (TargetZoneStart + TargetZoneEnd) / 2f;
                float halfSize = (TargetZoneEnd - TargetZoneStart) / 2f;
                float normalizedDist = Mathf.Abs(BarPosition - center) / halfSize;

                if (normalizedDist < _perfectThreshold)
                {
                    // PERFECT HIT
                    result = HitResult.Perfect;
                    Combo++;
                    scoreEarned = _perfectScore * Combo;
                    Score += scoreEarned;
                }
                else
                {
                    // GOOD HIT
                    result = HitResult.Good;
                    Combo++;
                    scoreEarned = _goodScore * Combo;
                    Score += scoreEarned;
                }

                HitCount++;
                if (Combo > MaxCombo) MaxCombo = Combo;
            }
            else
            {
                // MISS
                result = HitResult.Miss;
                Combo = 0;
                Lives--;
                scoreEarned = 0;

                // Trigger screen shake
                TriggerScreenShake(_shakeIntensity, 0.3f);
            }

            // Set feedback state
            LastHitResult = result;
            HitAnimTimer = 0.5f;

            // Regenerate target zone
            GenerateTargetZone();

            // Increase speed (harder over time)
            _barSpeed = Mathf.Min(_barSpeed + _speedIncrement, _maxBarSpeed);

            // Fire event
            OnHit?.Invoke(result, scoreEarned, Combo);

            Debug.Log($"[TimingBar] Hit: {result} | Score: +{scoreEarned} | " +
                      $"Combo: x{Combo} | Lives: {Lives} | Speed: {_barSpeed:F2}");

            return result;
        }

        // ════════════════════════════════════════
        //  TARGET ZONE
        // ════════════════════════════════════════

        /// <summary>
        /// Generate a new random target zone position.
        /// </summary>
        public void GenerateTargetZone()
        {
            float start = UnityEngine.Random.Range(0.05f, 0.95f - _targetSize);
            TargetZoneStart = start;
            TargetZoneEnd = start + _targetSize;

            OnZoneChanged?.Invoke(TargetZoneStart, TargetZoneEnd);
        }

        // ════════════════════════════════════════
        //  SCREEN SHAKE
        // ════════════════════════════════════════

        /// <summary>
        /// Trigger a screen shake effect (physics-based camera movement).
        /// </summary>
        public void TriggerScreenShake(float magnitude, float duration)
        {
            _shakeMagnitude = magnitude;
            _shakeTimer = duration;

            if (Camera.main != null)
            {
                _originalCamPos = Camera.main.transform.position;
            }
        }

        private void UpdateScreenShake(float dt)
        {
            if (_shakeTimer <= 0f) return;

            _shakeTimer -= dt;

            if (Camera.main != null)
            {
                float dampedMagnitude = _shakeMagnitude * (_shakeTimer / 0.3f);
                float offsetX = UnityEngine.Random.Range(-1f, 1f) * dampedMagnitude;
                float offsetY = UnityEngine.Random.Range(-1f, 1f) * dampedMagnitude;

                Camera.main.transform.position = _originalCamPos + new Vector3(offsetX, offsetY, 0f);
            }

            if (_shakeTimer <= 0f && Camera.main != null)
            {
                Camera.main.transform.position = _originalCamPos;
            }
        }

        // ════════════════════════════════════════
        //  ENDLESS MODE SUPPORT
        // ════════════════════════════════════════

        /// <summary>
        /// Scale up difficulty for endless mode wave progression.
        /// Called by EndlessMode when a wave is completed.
        /// </summary>
        public void ScaleUpDifficulty(float speedIncrease, float targetShrink)
        {
            _barSpeed = Mathf.Min(_barSpeed + speedIncrease, 4.0f);
            _targetSize = Mathf.Max(_targetSize - targetShrink, 0.05f);
        }

        /// <summary>
        /// Process a hit in endless mode with wave-based scoring.
        /// </summary>
        public HitResult ProcessEndlessHit(int wave)
        {
            if (!_active) return HitResult.None;

            bool inZone = BarPosition >= TargetZoneStart && BarPosition <= TargetZoneEnd;
            HitResult result;
            int scoreEarned = 0;

            if (inZone)
            {
                float center = (TargetZoneStart + TargetZoneEnd) / 2f;
                float halfSize = (TargetZoneEnd - TargetZoneStart) / 2f;
                float normalizedDist = Mathf.Abs(BarPosition - center) / halfSize;

                if (normalizedDist < _perfectThreshold)
                {
                    result = HitResult.Perfect;
                    Combo++;
                    scoreEarned = _perfectScore * Combo * wave;
                    Score += scoreEarned;
                }
                else
                {
                    result = HitResult.Good;
                    Combo++;
                    scoreEarned = _goodScore * Combo * wave;
                    Score += scoreEarned;
                }

                HitCount++;
                if (Combo > MaxCombo) MaxCombo = Combo;
            }
            else
            {
                result = HitResult.Miss;
                Combo = 0;
                Lives--;
                TriggerScreenShake(_shakeIntensity, 0.3f);
            }

            LastHitResult = result;
            HitAnimTimer = 0.5f;
            GenerateTargetZone();
            _barSpeed = Mathf.Min(_barSpeed + 0.008f, 4.0f);

            OnHit?.Invoke(result, scoreEarned, Combo);
            return result;
        }
    }
}
