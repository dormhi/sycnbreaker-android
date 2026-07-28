/* =========================================
   BackgroundGrid.cs — Animated Background
   
   Renders the cyberpunk-style animated grid
   and floating ambient particles behind the
   gameplay area. Responds to the current theme.
   
   Visual elements:
   - Scrolling grid lines
   - Floating ambient particles (dots)
   - Gradient overlay
   - Subtle parallax on device tilt
   
   Ported from: js/UIManager.js (drawGrid, drawParticles)
   ========================================= */

using UnityEngine;
using System.Collections.Generic;
using SyncBreaker.Gameplay;

namespace SyncBreaker.Systems
{
    /// <summary>
    /// Data for a single ambient background particle.
    /// </summary>
    public class AmbientParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Size;
        public float Alpha;
        public float PulseOffset; // For breathing effect
        public float PulseSpeed;
    }

    /// <summary>
    /// Renders animated background elements.
    /// Adapts colors from ThemeManager.
    /// </summary>
    public class BackgroundGrid : MonoBehaviour
    {
        // ── Configuration ──
        [Header("Grid")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private float gridScrollSpeed = 0.3f;
        [SerializeField] private float gridLineWidth = 0.02f;
        [SerializeField] private int gridLinesX = 12;
        [SerializeField] private int gridLinesY = 8;

        [Header("Ambient Particles")]
        [SerializeField] private int ambientCount = 30;
        [SerializeField] private float ambientMinSize = 0.01f;
        [SerializeField] private float ambientMaxSize = 0.04f;
        [SerializeField] private float ambientMinSpeed = 0.1f;
        [SerializeField] private float ambientMaxSpeed = 0.4f;

        [Header("Parallax")]
        [SerializeField] private bool useParallax = true;
        [SerializeField] private float parallaxStrength = 0.02f;

        [Header("Rendering")]
        [SerializeField] private SpriteRenderer gridRenderer;
        [SerializeField] private Sprite dotSprite;

        // ── Internal ──
        private readonly List<AmbientParticle> _ambientParticles = new();
        private readonly List<SpriteRenderer> _dotRenderers = new();
        private float _gridOffset;
        private Vector2 _parallaxOffset;
        private Camera _camera;
        private float _screenWidth;
        private float _screenHeight;

        // ════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════

        private void Start()
        {
            _camera = Camera.main;
            UpdateScreenBounds();
            InitializeAmbientParticles();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            UpdateScreenBounds();
            UpdateGridScroll(dt);
            UpdateAmbientParticles(dt);
            UpdateParallax(dt);
            RenderAmbientParticles();
        }

        // ════════════════════════════════════════
        //  SCREEN BOUNDS
        // ════════════════════════════════════════

        private void UpdateScreenBounds()
        {
            if (_camera == null) return;
            _screenHeight = _camera.orthographicSize * 2f;
            _screenWidth = _screenHeight * _camera.aspect;
        }

        // ════════════════════════════════════════
        //  GRID
        // ════════════════════════════════════════

        private void UpdateGridScroll(float dt)
        {
            if (!showGrid) return;
            _gridOffset = (_gridOffset + gridScrollSpeed * dt) % 1f;

            // Grid rendering is handled by a shader or
            // tiled sprite on gridRenderer
            if (gridRenderer != null)
            {
                var theme = ThemeManager.Instance?.CurrentTheme;
                if (theme != null)
                {
                    gridRenderer.color = theme.gridColor;
                }

                // Scroll by adjusting material offset
                var mat = gridRenderer.material;
                if (mat != null)
                {
                    mat.mainTextureOffset = new Vector2(0f, _gridOffset);
                }
            }
        }

        // ════════════════════════════════════════
        //  AMBIENT PARTICLES
        // ════════════════════════════════════════

        private void InitializeAmbientParticles()
        {
            _ambientParticles.Clear();
            foreach (var sr in _dotRenderers)
            {
                if (sr != null) Destroy(sr.gameObject);
            }
            _dotRenderers.Clear();

            for (int i = 0; i < ambientCount; i++)
            {
                var p = CreateRandomAmbient();
                _ambientParticles.Add(p);

                // Create renderer
                var go = new GameObject($"AmbientDot_{i}");
                go.transform.SetParent(transform);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = dotSprite;
                sr.sortingOrder = -10;
                sr.sortingLayerName = "Background";
                _dotRenderers.Add(sr);
            }
        }

        private AmbientParticle CreateRandomAmbient()
        {
            float halfW = _screenWidth / 2f;
            float halfH = _screenHeight / 2f;

            return new AmbientParticle
            {
                Position = new Vector2(
                    Random.Range(-halfW, halfW),
                    Random.Range(-halfH, halfH)
                ),
                Velocity = new Vector2(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ).normalized * Random.Range(ambientMinSpeed, ambientMaxSpeed),
                Size = Random.Range(ambientMinSize, ambientMaxSize),
                Alpha = Random.Range(0.1f, 0.4f),
                PulseOffset = Random.Range(0f, Mathf.PI * 2f),
                PulseSpeed = Random.Range(1f, 3f)
            };
        }

        private void UpdateAmbientParticles(float dt)
        {
            float halfW = _screenWidth / 2f + 0.5f;
            float halfH = _screenHeight / 2f + 0.5f;

            for (int i = 0; i < _ambientParticles.Count; i++)
            {
                var p = _ambientParticles[i];

                // Move
                p.Position += p.Velocity * dt;

                // Wrap around screen
                if (p.Position.x < -halfW) p.Position.x = halfW;
                if (p.Position.x > halfW) p.Position.x = -halfW;
                if (p.Position.y < -halfH) p.Position.y = halfH;
                if (p.Position.y > halfH) p.Position.y = -halfH;

                // Breathing alpha
                float breathe = 0.5f + Mathf.Sin(Time.time * p.PulseSpeed + p.PulseOffset) * 0.5f;
                p.Alpha = Mathf.Lerp(0.05f, 0.3f, breathe);
            }
        }

        private void RenderAmbientParticles()
        {
            var theme = ThemeManager.Instance?.CurrentTheme;
            Color baseColor = theme?.primaryColor ?? new Color(0.23f, 0.51f, 0.96f, 1f);

            for (int i = 0; i < _ambientParticles.Count && i < _dotRenderers.Count; i++)
            {
                var p = _ambientParticles[i];
                var sr = _dotRenderers[i];

                if (sr == null) continue;

                Vector2 pos = p.Position + _parallaxOffset;
                sr.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
                sr.transform.localScale = Vector3.one * p.Size;
                sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, p.Alpha);
            }
        }

        // ════════════════════════════════════════
        //  PARALLAX (DEVICE TILT)
        // ════════════════════════════════════════

        private void UpdateParallax(float dt)
        {
            if (!useParallax) return;

            // Use accelerometer for subtle parallax
            Vector3 accel = Input.acceleration;
            Vector2 target = new Vector2(accel.x, accel.y) * parallaxStrength;

            _parallaxOffset = Vector2.Lerp(_parallaxOffset, target, 5f * dt);
        }

        // ════════════════════════════════════════
        //  CLEANUP
        // ════════════════════════════════════════

        private void OnDestroy()
        {
            foreach (var sr in _dotRenderers)
            {
                if (sr != null) Destroy(sr.gameObject);
            }
            _dotRenderers.Clear();
            _ambientParticles.Clear();
        }
    }
}
