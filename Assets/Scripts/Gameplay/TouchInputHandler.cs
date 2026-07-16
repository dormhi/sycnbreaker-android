/* =========================================
   TouchInputHandler.cs — Mobile Touch Input
   
   Handles tap and swipe gestures for mobile.
   - Tap anywhere → triggers hit (timing bar)
   - Swipe → directional input (lockpick)
   - Button taps handled separately by UI
   
   Ported from: js/GameManager.js (_setupInput)
   ========================================= */

using UnityEngine;
using System;
using SyncBreaker.Core;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Swipe direction for lockpick input.
    /// </summary>
    public enum SwipeDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// Centralized touch input handler.
    /// Translates raw touch/mouse input into game actions.
    /// </summary>
    public class TouchInputHandler : MonoBehaviour
    {
        // ── Events ──
        /// <summary>Fired when the player taps the screen (not on UI).</summary>
        public event Action OnTap;

        /// <summary>Fired when the player completes a swipe. Args: (direction)</summary>
        public event Action<SwipeDirection> OnSwipe;

        /// <summary>Fired every frame with the current touch/mouse position in screen space.</summary>
        public event Action<Vector2> OnPointerMove;

        // ── Configuration ──
        [Header("Swipe Settings")]
        [Tooltip("Minimum distance (in pixels) for a swipe to register")]
        [SerializeField] private float swipeThreshold = 50f;

        [Tooltip("Maximum time (in seconds) for a swipe gesture")]
        [SerializeField] private float swipeMaxTime = 0.5f;

        // ── Internal State ──
        private Vector2 _touchStartPos;
        private float _touchStartTime;
        private bool _touchActive;
        private int _activeTouchId = -1;

        // ── Singleton ──
        public static TouchInputHandler Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ════════════════════════════════════════
        //  UPDATE
        // ════════════════════════════════════════

        private void Update()
        {
            // Handle touch input (mobile)
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
            }
            // Handle mouse input (editor/desktop fallback)
            else
            {
                HandleMouseInput();
            }

            // Keyboard input (editor testing)
            HandleKeyboardInput();
        }

        // ════════════════════════════════════════
        //  TOUCH (MOBILE)
        // ════════════════════════════════════════

        private void HandleTouchInput()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        if (!_touchActive)
                        {
                            _touchActive = true;
                            _activeTouchId = touch.fingerId;
                            _touchStartPos = touch.position;
                            _touchStartTime = Time.time;
                        }
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (touch.fingerId == _activeTouchId)
                        {
                            OnPointerMove?.Invoke(touch.position);
                        }
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (touch.fingerId == _activeTouchId)
                        {
                            ProcessTouchEnd(touch.position);
                            _touchActive = false;
                            _activeTouchId = -1;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Evaluate the completed touch gesture.
        /// Short touch = tap, long drag = swipe.
        /// </summary>
        private void ProcessTouchEnd(Vector2 endPos)
        {
            float elapsed = Time.time - _touchStartTime;
            Vector2 delta = endPos - _touchStartPos;
            float distance = delta.magnitude;

            // Check if it's a swipe
            if (distance >= swipeThreshold && elapsed <= swipeMaxTime)
            {
                SwipeDirection dir = GetSwipeDirection(delta);
                if (dir != SwipeDirection.None)
                {
                    OnSwipe?.Invoke(dir);
                    Debug.Log($"[TouchInput] Swipe: {dir}");
                    return;
                }
            }

            // Otherwise it's a tap
            // Check if tap is on UI (Unity's EventSystem handles this)
            if (!IsPointerOverUI())
            {
                OnTap?.Invoke();
            }
        }

        // ════════════════════════════════════════
        //  MOUSE (EDITOR FALLBACK)
        // ════════════════════════════════════════

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _touchStartPos = Input.mousePosition;
                _touchStartTime = Time.time;
                _touchActive = true;
            }

            if (_touchActive)
            {
                OnPointerMove?.Invoke(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0) && _touchActive)
            {
                ProcessTouchEnd(Input.mousePosition);
                _touchActive = false;
            }
        }

        // ════════════════════════════════════════
        //  KEYBOARD (EDITOR TESTING)
        // ════════════════════════════════════════

        private void HandleKeyboardInput()
        {
            // Space = Tap
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnTap?.Invoke();
            }

            // Arrow keys / WASD = Swipe directions
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                OnSwipe?.Invoke(SwipeDirection.Up);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                OnSwipe?.Invoke(SwipeDirection.Down);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                OnSwipe?.Invoke(SwipeDirection.Left);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                OnSwipe?.Invoke(SwipeDirection.Right);
            }

            // Escape = Back
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Handled by individual state handlers
            }
        }

        // ════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════

        /// <summary>
        /// Determine swipe direction from a delta vector.
        /// </summary>
        private SwipeDirection GetSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                // Horizontal swipe
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            }
            else
            {
                // Vertical swipe
                return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
            }
        }

        /// <summary>
        /// Check if the pointer is currently over a UI element.
        /// Prevents taps on buttons from also triggering game actions.
        /// </summary>
        private bool IsPointerOverUI()
        {
            // Unity's EventSystem handles this
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;

            // Check for touch
            if (Input.touchCount > 0)
            {
                return UnityEngine.EventSystems.EventSystem.current
                    .IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }

            // Check for mouse
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
    }
}
