/* =========================================
   ParticleEffects.cs — Visual Effects System
   
   Manages all particle and visual effects:
   - Hit sparks (perfect/good/miss)
   - Combo burst
   - Level complete celebration
   - Lockpick node solve flash
   - Ambient floating particles
   - Screen shake controller
   
   Uses Unity's ParticleSystem + manual
   sprite-based particles for mobile perf.
   
   Ported from: js/UIManager.js (particle methods)
   ========================================= */

using UnityEngine;
using System.Collections.Generic;
using SyncBreaker.Core;

namespace SyncBreaker.Systems
{
    /// <summary>
    /// Data for a single sprite-based particle.
    /// Lighter than full ParticleSystem for simple effects.
    /// </summary>
    public class SpriteParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public float Size;
        public float Rotation;
        public float RotationSpeed;
        public Color Color;
        public float Gravity;
        public float Drag;

        /// <summary>Normalized lifetime (0 = just spawned, 1 = dead).</summary>
        public float NormalizedLife => MaxLife > 0f ? 1f - (Life / MaxLife) : 1f;

        /// <summary>Is this particle still alive?</summary>
        public bool IsAlive => Life > 0f;
    }

    /// <summary>
    /// Centralized visual effects manager.
    /// Handles particle spawning, screen shake, and post-processing.
    /// </summary>
    public class ParticleEffects : MonoBehaviour
    {
        // ── Singleton ──
        public static ParticleEffects Instance { get; private set; }

        // ── Particle Pool ──
        private readonly List<SpriteParticle> _particles = new();
        private const int MaxParticles = 200;

        // ── Screen Shake ──
        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeTimer;
        private float _shakeFrequency = 25f;
        private Vector3 _originalCameraPos;
        private Camera _camera;

        /// <summary>Current screen shake offset (apply to camera).</summary>
        public Vector3 ShakeOffset { get; private set; }

        // ── Configuration ──
        [Header("Particle Settings")]
        [SerializeField] private int hitParticleCount = 12;
        [SerializeField] private int comboParticleCount = 20;
        [SerializeField] private int celebrationParticleCount = 50;

        [Header("Screen Shake")]
        [SerializeField] private float perfectShakeIntensity = 0.08f;
        [SerializeField] private float missShakeIntensity = 0.15f;
        [SerializeField] private float shakeDecay = 5f;

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

            _camera = Camera.main;
            if (_camera != null)
                _originalCameraPos = _camera.transform.localPosition;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            UpdateParticles(dt);
            UpdateScreenShake(dt);
        }

        private void LateUpdate()
        {
            // Apply screen shake to camera (in LateUpdate to override other movement)
            if (_camera != null && _shakeTimer > 0f)
            {
                _camera.transform.localPosition = _originalCameraPos + ShakeOffset;
            }
        }

        // ════════════════════════════════════════
        //  HIT EFFECTS
        // ════════════════════════════════════════

        /// <summary>
        /// Spawn particles for a perfect hit.
        /// Green sparks radiating outward with golden trails.
        /// </summary>
        public void SpawnPerfectHit(Vector2 worldPos)
        {
            var theme = Gameplay.ThemeManager.Instance?.CurrentTheme;
            Color primary = theme?.perfectColor ?? new Color(0.13f, 0.77f, 0.37f, 1f);
            Color accent = theme?.accentColor ?? new Color(1f, 0.84f, 0f, 1f);

            // Main burst
            SpawnBurst(worldPos, hitParticleCount, primary, 
                       minSpeed: 3f, maxSpeed: 8f, 
                       minLife: 0.3f, maxLife: 0.7f,
                       minSize: 0.03f, maxSize: 0.08f,
                       gravity: -2f);

            // Accent sparkles
            SpawnBurst(worldPos, hitParticleCount / 3, accent,
                       minSpeed: 2f, maxSpeed: 5f,
                       minLife: 0.2f, maxLife: 0.5f,
                       minSize: 0.02f, maxSize: 0.04f,
                       gravity: 0f);

            // Screen shake
            TriggerShake(perfectShakeIntensity, 0.15f);
        }

        /// <summary>
        /// Spawn particles for a good hit.
        /// Lighter green sparks, less intense.
        /// </summary>
        public void SpawnGoodHit(Vector2 worldPos)
        {
            var theme = Gameplay.ThemeManager.Instance?.CurrentTheme;
            Color color = theme?.goodColor ?? new Color(0.29f, 0.87f, 0.50f, 1f);

            SpawnBurst(worldPos, hitParticleCount / 2, color,
                       minSpeed: 2f, maxSpeed: 5f,
                       minLife: 0.2f, maxLife: 0.5f,
                       minSize: 0.02f, maxSize: 0.06f,
                       gravity: -1f);

            TriggerShake(perfectShakeIntensity * 0.5f, 0.1f);
        }

        /// <summary>
        /// Spawn particles for a miss.
        /// Red sparks falling downward.
        /// </summary>
        public void SpawnMissHit(Vector2 worldPos)
        {
            var theme = Gameplay.ThemeManager.Instance?.CurrentTheme;
            Color color = theme?.missColor ?? new Color(0.94f, 0.27f, 0.27f, 1f);

            SpawnBurst(worldPos, hitParticleCount / 2, color,
                       minSpeed: 1f, maxSpeed: 4f,
                       minLife: 0.3f, maxLife: 0.6f,
                       minSize: 0.02f, maxSize: 0.05f,
                       gravity: 5f); // Fall down

            TriggerShake(missShakeIntensity, 0.2f);
        }

        // ════════════════════════════════════════
        //  COMBO EFFECTS
        // ════════════════════════════════════════

        /// <summary>
        /// Spawn a combo milestone burst (every 5x combo).
        /// </summary>
        public void SpawnComboBurst(Vector2 worldPos, int comboCount)
        {
            var theme = Gameplay.ThemeManager.Instance?.CurrentTheme;
            Color primary = theme?.primaryColor ?? new Color(0.96f, 0.62f, 0.04f, 1f);
            Color accent = theme?.accentColor ?? new Color(1f, 0.84f, 0f, 1f);

            // Scale particle count with combo
            int count = Mathf.Min(comboParticleCount + comboCount * 2, MaxParticles / 2);

            // Ring burst
            SpawnRing(worldPos, count, primary, 
                      radius: 0.5f + comboCount * 0.1f,
                      speed: 4f + comboCount * 0.5f,
                      life: 0.5f);

            // Inner sparkles
            SpawnBurst(worldPos, count / 3, accent,
                       minSpeed: 1f, maxSpeed: 3f,
                       minLife: 0.3f, maxLife: 0.8f,
                       minSize: 0.02f, maxSize: 0.05f,
                       gravity: 0f);

            TriggerShake(0.1f + comboCount * 0.02f, 0.2f);
        }

        // ════════════════════════════════════════
        //  CELEBRATION EFFECTS
        // ════════════════════════════════════════

        /// <summary>
        /// Spawn level complete celebration.
        /// Multi-color firework burst.
        /// </summary>
        public void SpawnCelebration(Vector2 worldPos)
        {
            var theme = Gameplay.ThemeManager.Instance?.CurrentTheme;

            Color[] colors = {
                theme?.primaryColor ?? Color.blue,
                theme?.secondaryColor ?? Color.magenta,
                theme?.accentColor ?? Color.cyan,
                theme?.perfectColor ?? Color.green,
                new Color(1f, 0.84f, 0f, 1f) // Gold
            };

            for (int wave = 0; wave < 3; wave++)
            {
                float delay = wave * 0.15f; // Staggered waves
                Color color = colors[wave % colors.Length];

                // Each wave is a ring
                SpawnRing(worldPos, celebrationParticleCount / 3, color,
                          radius: 0.3f + wave * 0.4f,
                          speed: 5f + wave * 2f,
                          life: 0.8f + wave * 0.3f);
            }

            // Big shake
            TriggerShake(0.2f, 0.4f);
        }

        // ════════════════════════════════════════
        //  LOCKPICK EFFECTS
        // ════════════════════════════════════════

        /// <summary>
        /// Flash effect when a lockpick node is solved.
        /// </summary>
        public void SpawnNodeSolve(Vector2 worldPos)
        {
            var theme = Gameplay.ThemeManager.Instance?.CurrentTheme;
            Color color = theme?.lockpickSolvedColor ?? new Color(0.13f, 0.77f, 0.37f, 1f);

            SpawnRing(worldPos, 8, color,
                      radius: 0.2f,
                      speed: 4f,
                      life: 0.4f);

            TriggerShake(0.05f, 0.1f);
        }

        /// <summary>
        /// Shatter effect when lockpick fails.
        /// </summary>
        public void SpawnNodeFail(Vector2 worldPos)
        {
            Color color = new Color(0.94f, 0.27f, 0.27f, 1f);

            SpawnBurst(worldPos, 15, color,
                       minSpeed: 3f, maxSpeed: 7f,
                       minLife: 0.4f, maxLife: 0.8f,
                       minSize: 0.03f, maxSize: 0.07f,
                       gravity: 8f);

            TriggerShake(0.15f, 0.25f);
        }

        // ════════════════════════════════════════
        //  PARTICLE SPAWNERS
        // ════════════════════════════════════════

        /// <summary>
        /// Spawn particles in a radial burst from a point.
        /// </summary>
        private void SpawnBurst(Vector2 origin, int count, Color color,
                                float minSpeed, float maxSpeed,
                                float minLife, float maxLife,
                                float minSize, float maxSize,
                                float gravity)
        {
            for (int i = 0; i < count; i++)
            {
                if (_particles.Count >= MaxParticles) break;

                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(minSpeed, maxSpeed);
                float life = Random.Range(minLife, maxLife);
                float size = Random.Range(minSize, maxSize);

                _particles.Add(new SpriteParticle
                {
                    Position = origin,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    Life = life,
                    MaxLife = life,
                    Size = size,
                    Rotation = Random.Range(0f, 360f),
                    RotationSpeed = Random.Range(-180f, 180f),
                    Color = color,
                    Gravity = gravity,
                    Drag = 0.95f
                });
            }
        }

        /// <summary>
        /// Spawn particles in a ring (evenly distributed angles).
        /// </summary>
        private void SpawnRing(Vector2 origin, int count, Color color,
                               float radius, float speed, float life)
        {
            float angleStep = (Mathf.PI * 2f) / count;

            for (int i = 0; i < count; i++)
            {
                if (_particles.Count >= MaxParticles) break;

                float angle = i * angleStep;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                _particles.Add(new SpriteParticle
                {
                    Position = origin + dir * radius * 0.3f,
                    Velocity = dir * speed,
                    Life = life,
                    MaxLife = life,
                    Size = Random.Range(0.02f, 0.05f),
                    Rotation = angle * Mathf.Rad2Deg,
                    RotationSpeed = Random.Range(-90f, 90f),
                    Color = color,
                    Gravity = 0f,
                    Drag = 0.92f
                });
            }
        }

        // ════════════════════════════════════════
        //  PARTICLE UPDATE
        // ════════════════════════════════════════

        private void UpdateParticles(float dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];

                // Update lifetime
                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    _particles.RemoveAt(i);
                    continue;
                }

                // Physics
                p.Velocity.y -= p.Gravity * dt;       // Gravity
                p.Velocity *= Mathf.Pow(p.Drag, dt);  // Drag
                p.Position += p.Velocity * dt;         // Movement
                p.Rotation += p.RotationSpeed * dt;    // Spin

                // Fade color alpha based on lifetime
                float alpha = Mathf.Clamp01(p.Life / p.MaxLife);
                // Ease out for smoother fade
                alpha = alpha * alpha;
                p.Color = new Color(p.Color.r, p.Color.g, p.Color.b, alpha);
            }
        }

        // ════════════════════════════════════════
        //  SCREEN SHAKE
        // ════════════════════════════════════════

        /// <summary>
        /// Trigger screen shake with given intensity and duration.
        /// </summary>
        public void TriggerShake(float intensity, float duration)
        {
            // Apply theme multiplier
            float mult = Gameplay.ThemeManager.Instance?.CurrentTheme?.screenShakeMultiplier ?? 1f;
            intensity *= mult;

            // Only override if stronger than current
            if (intensity > _shakeIntensity)
            {
                _shakeIntensity = intensity;
                _shakeDuration = duration;
                _shakeTimer = duration;
            }
        }

        private void UpdateScreenShake(float dt)
        {
            if (_shakeTimer <= 0f)
            {
                ShakeOffset = Vector3.zero;
                if (_camera != null)
                    _camera.transform.localPosition = _originalCameraPos;
                return;
            }

            _shakeTimer -= dt;
            float t = _shakeTimer / _shakeDuration;
            float currentIntensity = _shakeIntensity * t; // Linear decay

            // Perlin noise based shake for organic feel
            float x = (Mathf.PerlinNoise(Time.time * _shakeFrequency, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, Time.time * _shakeFrequency) - 0.5f) * 2f;

            ShakeOffset = new Vector3(x, y, 0f) * currentIntensity;
        }

        // ════════════════════════════════════════
        //  QUERIES
        // ════════════════════════════════════════

        /// <summary>
        /// Get all active particles for rendering.
        /// </summary>
        public IReadOnlyList<SpriteParticle> ActiveParticles => _particles;

        /// <summary>
        /// Number of active particles.
        /// </summary>
        public int ActiveParticleCount => _particles.Count;

        /// <summary>
        /// Clear all particles.
        /// </summary>
        public void ClearAll()
        {
            _particles.Clear();
            _shakeTimer = 0f;
            ShakeOffset = Vector3.zero;
        }
    }
}
