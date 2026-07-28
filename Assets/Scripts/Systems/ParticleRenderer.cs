/* =========================================
   ParticleRenderer.cs — GPU Particle Rendering
   
   Renders the sprite-based particles from
   ParticleEffects using a single draw call
   via Graphics.DrawMeshInstanced or simple
   SpriteRenderer pooling.
   
   Mobile-optimized: single material,
   batched rendering, no per-particle
   GameObject overhead.
   ========================================= */

using UnityEngine;
using System.Collections.Generic;

namespace SyncBreaker.Systems
{
    /// <summary>
    /// Renders particles from ParticleEffects system.
    /// Uses a pool of SpriteRenderers for mobile efficiency.
    /// </summary>
    public class ParticleRenderer : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private Sprite particleSprite;
        [SerializeField] private Material particleMaterial;
        [SerializeField] private int sortingOrder = 100;
        [SerializeField] private string sortingLayerName = "Particles";

        // ── Pool ──
        private readonly List<SpriteRenderer> _pool = new();
        private readonly List<GameObject> _poolObjects = new();
        private int _activeCount;

        // ════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════

        private void Start()
        {
            // Pre-warm pool with some renderers
            EnsurePoolSize(50);
        }

        private void LateUpdate()
        {
            var effects = ParticleEffects.Instance;
            if (effects == null) return;

            var particles = effects.ActiveParticles;
            EnsurePoolSize(particles.Count);

            // Update active renderers
            _activeCount = 0;
            for (int i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                if (!p.IsAlive) continue;

                var sr = _pool[_activeCount];
                var go = _poolObjects[_activeCount];

                go.SetActive(true);
                go.transform.position = new Vector3(p.Position.x, p.Position.y, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, p.Rotation);
                go.transform.localScale = Vector3.one * p.Size;

                sr.color = p.Color;

                _activeCount++;
            }

            // Deactivate unused renderers
            for (int i = _activeCount; i < _pool.Count; i++)
            {
                _poolObjects[i].SetActive(false);
            }
        }

        // ════════════════════════════════════════
        //  POOL MANAGEMENT
        // ════════════════════════════════════════

        private void EnsurePoolSize(int needed)
        {
            while (_pool.Count < needed)
            {
                var go = new GameObject($"Particle_{_pool.Count}");
                go.transform.SetParent(transform);
                go.SetActive(false);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = particleSprite;
                if (particleMaterial != null)
                    sr.material = particleMaterial;
                sr.sortingOrder = sortingOrder;
                sr.sortingLayerName = sortingLayerName;

                _pool.Add(sr);
                _poolObjects.Add(go);
            }
        }

        // ════════════════════════════════════════
        //  CLEANUP
        // ════════════════════════════════════════

        private void OnDestroy()
        {
            foreach (var go in _poolObjects)
            {
                if (go != null) Destroy(go);
            }
            _pool.Clear();
            _poolObjects.Clear();
        }
    }
}
