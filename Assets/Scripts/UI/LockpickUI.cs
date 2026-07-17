/* =========================================
   LockpickUI.cs — Lockpick Visual Display
   
   Renders the lockpick mini-game:
   - Circular node layout with direction arrows
   - Rotating cursor with trail
   - Connection lines between nodes
   - Lock icon at center
   - Progress bar
   - Result display
   
   Uses Unity UI (Canvas) elements positioned
   programmatically in a circle layout.
   
   Ported from: js/LockpickSystem.js (render methods)
   ========================================= */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SyncBreaker.Core;

namespace SyncBreaker.UI
{
    /// <summary>
    /// Renders the lockpick mini-game visuals.
    /// Reads from LockpickSystem and updates UI elements each frame.
    /// </summary>
    public class LockpickUI : MonoBehaviour
    {
        // ════════════════════════════════════════
        //  INSPECTOR REFERENCES
        // ════════════════════════════════════════

        [Header("Layout")]
        [SerializeField] private RectTransform circleCenter;
        [SerializeField] private float circleRadius = 140f;
        [SerializeField] private float nodeSize = 32f;

        [Header("Cursor")]
        [SerializeField] private RectTransform cursorDot;
        [SerializeField] private Image cursorDotImage;
        [SerializeField] private RectTransform cursorLine;

        [Header("Decorative Rings")]
        [SerializeField] private RectTransform outerRing;
        [SerializeField] private RectTransform innerRing;

        [Header("Lock Icon")]
        [SerializeField] private Image lockBodyImage;
        [SerializeField] private Image lockShackleImage;
        [SerializeField] private Color lockLockedColor = new Color(0.12f, 0.14f, 0.17f, 1f);
        [SerializeField] private Color lockOpenColor = new Color(0.13f, 0.77f, 0.37f, 1f);

        [Header("Progress Bar")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("Prefabs")]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private GameObject connectionLinePrefab;

        [Header("Colors")]
        [SerializeField] private Color nodeDefaultBg = new Color(0.06f, 0.09f, 0.16f, 0.8f);
        [SerializeField] private Color nodeDefaultBorder = new Color(0.2f, 0.25f, 0.33f, 1f);
        [SerializeField] private Color nodeActiveBg = new Color(0.23f, 0.51f, 0.96f, 0.15f);
        [SerializeField] private Color nodeActiveBorder = new Color(0.23f, 0.51f, 0.96f, 1f);
        [SerializeField] private Color nodeSolvedBg = new Color(0.13f, 0.77f, 0.37f, 0.15f);
        [SerializeField] private Color nodeSolvedBorder = new Color(0.13f, 0.77f, 0.37f, 1f);
        [SerializeField] private Color nodeFailedBg = new Color(0.94f, 0.27f, 0.27f, 0.15f);
        [SerializeField] private Color nodeFailedBorder = new Color(0.94f, 0.27f, 0.27f, 1f);

        // ── Runtime ──
        private Gameplay.LockpickSystem _lockpick;
        private List<RectTransform> _nodeTransforms = new();
        private List<Image> _nodeBgImages = new();
        private List<Image> _nodeBorderImages = new();
        private List<TextMeshProUGUI> _nodeTexts = new();

        // ════════════════════════════════════════
        //  INITIALIZATION
        // ════════════════════════════════════════

        /// <summary>
        /// Bind to a LockpickSystem and create visual elements.
        /// </summary>
        public void Bind(Gameplay.LockpickSystem lockpick, bool isRevive = false)
        {
            _lockpick = lockpick;

            // Set title based on context
            if (titleText != null)
            {
                titleText.text = isRevive ? "RECOVERY PROTOCOL" : "CODE BREAKER";
                titleText.color = Utils.HexToColor("#f59e0b");
            }

            if (subtitleText != null)
            {
                subtitleText.text = isRevive
                    ? "Break the code to restore the connection"
                    : "Break the security code to access the locked node";
            }

            // Create node visuals
            CreateNodeVisuals();

            // Listen for events
            _lockpick.OnNodeSolved += OnNodeSolvedVisual;
            _lockpick.OnNodeFailed += OnNodeFailedVisual;

            // Initial state
            UpdateHint();
            UpdateLockIcon();
            UpdateProgressBar();

            if (resultText != null)
                resultText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Unbind and cleanup.
        /// </summary>
        public void Unbind()
        {
            if (_lockpick != null)
            {
                _lockpick.OnNodeSolved -= OnNodeSolvedVisual;
                _lockpick.OnNodeFailed -= OnNodeFailedVisual;
            }

            ClearNodeVisuals();
            _lockpick = null;
        }

        // ════════════════════════════════════════
        //  UPDATE
        // ════════════════════════════════════════

        private void Update()
        {
            if (_lockpick == null || !_lockpick.Active) return;

            UpdateCursor();
            UpdateNodes();
            UpdateDecorativeRings();
            UpdateProgressBar();
            UpdateLockIcon();
            UpdateHint();
            UpdateResult();
        }

        // ════════════════════════════════════════
        //  CURSOR
        // ════════════════════════════════════════

        private void UpdateCursor()
        {
            if (cursorDot == null || _lockpick.Result != null) return;

            // Position cursor on circle
            Vector2 pos = _lockpick.GetCursorPosition(circleRadius);
            cursorDot.anchoredPosition = pos;

            // Cursor line from center to cursor position
            if (cursorLine != null)
            {
                float angle = Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg;
                cursorLine.localRotation = Quaternion.Euler(0, 0, angle);
                cursorLine.sizeDelta = new Vector2(circleRadius, cursorLine.sizeDelta.y);
            }

            // Pulse effect on cursor dot
            float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.15f;
            cursorDot.localScale = Vector3.one * pulse;
        }

        // ════════════════════════════════════════
        //  NODES
        // ════════════════════════════════════════

        private void CreateNodeVisuals()
        {
            ClearNodeVisuals();

            if (_lockpick == null || nodePrefab == null || circleCenter == null) return;

            for (int i = 0; i < _lockpick.NodeCount; i++)
            {
                var node = _lockpick.Nodes[i];

                // Instantiate node prefab
                var go = Instantiate(nodePrefab, circleCenter);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(nodeSize, nodeSize);

                // Position on circle
                Vector2 pos = _lockpick.GetNodePosition(i, circleRadius);
                rt.anchoredPosition = pos;

                // Get image components (expected structure: bg image + border outline + text)
                var images = go.GetComponentsInChildren<Image>();
                Image bgImg = images.Length > 0 ? images[0] : null;
                Image borderImg = images.Length > 1 ? images[1] : null;

                // Get text component for direction symbol
                var text = go.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = node.DirectionSymbol;
                    text.fontSize = 18;
                }

                _nodeTransforms.Add(rt);
                _nodeBgImages.Add(bgImg);
                _nodeBorderImages.Add(borderImg);
                _nodeTexts.Add(text);
            }
        }

        private void UpdateNodes()
        {
            for (int i = 0; i < _lockpick.NodeCount && i < _nodeTransforms.Count; i++)
            {
                var node = _lockpick.Nodes[i];
                var rt = _nodeTransforms[i];

                // Apply vibration offset (spring physics)
                Vector2 basePos = _lockpick.GetNodePosition(i, circleRadius);
                float vibX = node.VibrationOffset * Mathf.Cos(node.Angle * Mathf.Deg2Rad);
                float vibY = node.VibrationOffset * Mathf.Sin(node.Angle * Mathf.Deg2Rad);
                rt.anchoredPosition = basePos + new Vector2(vibX, vibY);

                // Apply scale animation
                rt.localScale = Vector3.one * node.ScaleAnim;

                // Update colors
                Color bg, border, textColor;

                if (node.Solved)
                {
                    bg = nodeSolvedBg;
                    border = nodeSolvedBorder;
                    textColor = nodeSolvedBorder;
                }
                else if (node.Failed)
                {
                    bg = nodeFailedBg;
                    border = nodeFailedBorder;
                    textColor = nodeFailedBorder;
                }
                else if (i == _lockpick.CurrentNodeIndex && _lockpick.Result == null)
                {
                    bg = nodeActiveBg;
                    border = nodeActiveBorder;
                    textColor = Color.white;
                }
                else
                {
                    bg = nodeDefaultBg;
                    border = nodeDefaultBorder;
                    textColor = new Color(0.39f, 0.45f, 0.55f, 1f); // #64748b
                }

                if (_nodeBgImages[i] != null) _nodeBgImages[i].color = bg;
                if (_nodeBorderImages[i] != null) _nodeBorderImages[i].color = border;
                if (_nodeTexts[i] != null) _nodeTexts[i].color = textColor;
            }
        }

        private void ClearNodeVisuals()
        {
            foreach (var rt in _nodeTransforms)
            {
                if (rt != null) Destroy(rt.gameObject);
            }
            _nodeTransforms.Clear();
            _nodeBgImages.Clear();
            _nodeBorderImages.Clear();
            _nodeTexts.Clear();
        }

        // ════════════════════════════════════════
        //  DECORATIVE ELEMENTS
        // ════════════════════════════════════════

        private void UpdateDecorativeRings()
        {
            // Outer ring: slow rotation following cursor
            if (outerRing != null)
            {
                float rot = _lockpick.CursorAngle * 0.3f;
                outerRing.localRotation = Quaternion.Euler(0, 0, rot);
            }

            // Inner ring: counter-rotation
            if (innerRing != null)
            {
                float rot = -_lockpick.CursorAngle * 0.5f;
                innerRing.localRotation = Quaternion.Euler(0, 0, rot);
            }
        }

        private void UpdateLockIcon()
        {
            if (lockBodyImage == null) return;

            bool open = _lockpick.Result == "success";
            Color c = open ? lockOpenColor : lockLockedColor;

            lockBodyImage.color = c;
            if (lockShackleImage != null)
            {
                lockShackleImage.color = open
                    ? lockOpenColor
                    : new Color(0.27f, 0.32f, 0.39f, 1f);
            }
        }

        // ════════════════════════════════════════
        //  PROGRESS & TEXT
        // ════════════════════════════════════════

        private void UpdateProgressBar()
        {
            if (progressSlider != null)
            {
                progressSlider.value = _lockpick.Progress;
            }

            if (progressText != null)
            {
                progressText.text = $"{_lockpick.SolvedCount} / {_lockpick.NodeCount}";
            }
        }

        private void UpdateHint()
        {
            if (hintText == null) return;

            if (_lockpick.Result != null)
            {
                hintText.gameObject.SetActive(false);
            }
            else if (!_lockpick.Started)
            {
                hintText.gameObject.SetActive(true);
                hintText.text = "Swipe in any direction to start";
            }
            else
            {
                hintText.gameObject.SetActive(true);
                hintText.text = "Swipe the correct direction when cursor reaches a node!";
            }
        }

        private void UpdateResult()
        {
            if (resultText == null || _lockpick.Result == null) return;

            resultText.gameObject.SetActive(true);

            bool success = _lockpick.Result == "success";
            resultText.text = success ? "ACCESS GRANTED" : "FAILED";
            resultText.color = success
                ? Utils.HexToColor("#22c55e")
                : Utils.HexToColor("#ef4444");

            // Scale pulse
            float t = Mathf.Clamp01(Time.time % 2f);
            resultText.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.05f);
        }

        // ════════════════════════════════════════
        //  EVENT CALLBACKS
        // ════════════════════════════════════════

        private void OnNodeSolvedVisual(int index)
        {
            // Could trigger particle effects here
            Debug.Log($"[LockpickUI] Node {index} solved — visual feedback");
        }

        private void OnNodeFailedVisual(int index)
        {
            // Could trigger shake/particle effects here
            Debug.Log($"[LockpickUI] Node {index} failed — visual feedback");
        }

        // ════════════════════════════════════════
        //  CLEANUP
        // ════════════════════════════════════════

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
