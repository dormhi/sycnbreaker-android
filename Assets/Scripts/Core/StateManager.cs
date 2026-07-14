/* =========================================
   StateManager.cs — Finite State Machine
   Manages game states with enter/exit/update
   lifecycle, transition animations, and
   state history tracking.
   
   Ported from: js/StateManager.js
   ========================================= */

using UnityEngine;
using System;
using System.Collections.Generic;

namespace SyncBreaker.Core
{
    /// <summary>
    /// All possible game states.
    /// </summary>
    public enum GameState
    {
        None,
        Menu,
        Hub,
        Level,
        Lockpick,
        GameOver,
        Endless,
        EndlessOver
    }

    /// <summary>
    /// Interface for state handlers.
    /// Each game state implements this to define its behavior.
    /// </summary>
    public interface IStateHandler
    {
        void OnEnter(StateContext context);
        void OnUpdate(float dt);
        void OnExit();
    }

    /// <summary>
    /// Manages game state transitions with fade animation support.
    /// Replaces the JavaScript StateManager with a more robust C# implementation.
    /// </summary>
    public class StateManager : MonoBehaviour
    {
        // ── Current State ──
        public GameState CurrentState { get; private set; } = GameState.None;
        public GameState PreviousState { get; private set; } = GameState.None;

        // ── State Handlers ──
        private readonly Dictionary<GameState, IStateHandler> _handlers = new();

        // ── Transition ──
        private bool _transitioning;
        private float _transitionTimer;
        private float _transitionDuration = 0.3f;
        private float _transitionProgress;
        private GameState _pendingState = GameState.None;
        private StateContext _pendingContext;
        private bool _pendingExecuted;

        // ── Events ──
        /// <summary>
        /// Fired when a state transition begins. Args: (fromState, toState)
        /// </summary>
        public event Action<GameState, GameState> OnStateTransitionStart;

        /// <summary>
        /// Fired when a state transition completes. Args: (newState)
        /// </summary>
        public event Action<GameState> OnStateTransitionComplete;

        // ── Transition Overlay ──
        /// <summary>
        /// Current transition alpha (0-1). Used by UI to render fade overlay.
        /// </summary>
        public float TransitionAlpha { get; private set; }
        public bool IsTransitioning => _transitioning;

        // ════════════════════════════════════════
        //  REGISTRATION
        // ════════════════════════════════════════

        /// <summary>
        /// Register a state handler for a given game state.
        /// </summary>
        public void RegisterState(GameState state, IStateHandler handler)
        {
            if (_handlers.ContainsKey(state))
            {
                Debug.LogWarning($"[StateManager] Overwriting handler for state: {state}");
            }
            _handlers[state] = handler;
            Debug.Log($"[StateManager] Registered handler for: {state}");
        }

        /// <summary>
        /// Unregister a state handler.
        /// </summary>
        public void UnregisterState(GameState state)
        {
            _handlers.Remove(state);
        }

        // ════════════════════════════════════════
        //  STATE TRANSITIONS
        // ════════════════════════════════════════

        /// <summary>
        /// Request a state change. Triggers a fade transition.
        /// </summary>
        public void ChangeState(GameState newState, StateContext context = null)
        {
            if (_transitioning)
            {
                Debug.LogWarning($"[StateManager] Transition already in progress. Ignoring change to {newState}.");
                return;
            }

            if (newState == GameState.None)
            {
                Debug.LogError("[StateManager] Cannot transition to None state.");
                return;
            }

            Debug.Log($"[StateManager] Transition requested: {CurrentState} → {newState}");

            _pendingState = newState;
            _pendingContext = context;
            _transitioning = true;
            _transitionTimer = 0f;
            _transitionProgress = 0f;
            _pendingExecuted = false;

            OnStateTransitionStart?.Invoke(CurrentState, newState);
        }

        /// <summary>
        /// Immediately change state without transition animation.
        /// Use for initial state setup only.
        /// </summary>
        public void ForceState(GameState newState, StateContext context = null)
        {
            ExecuteTransition(newState, context);
        }

        // ════════════════════════════════════════
        //  UPDATE
        // ════════════════════════════════════════

        /// <summary>
        /// Called by GameManager every frame.
        /// </summary>
        public void UpdateState(float dt)
        {
            // Handle transition animation
            if (_transitioning)
            {
                _transitionTimer += dt;
                _transitionProgress = Mathf.Clamp01(_transitionTimer / _transitionDuration);

                // Calculate fade alpha (fade in → execute → fade out)
                if (_transitionProgress < 0.5f)
                {
                    TransitionAlpha = _transitionProgress * 2f; // 0 → 1
                }
                else
                {
                    TransitionAlpha = (1f - _transitionProgress) * 2f; // 1 → 0
                }

                // Execute the actual state change at the midpoint (screen fully black)
                if (_transitionProgress >= 0.5f && !_pendingExecuted)
                {
                    _pendingExecuted = true;
                    ExecuteTransition(_pendingState, _pendingContext);
                }

                // Transition complete
                if (_transitionProgress >= 1f)
                {
                    _transitioning = false;
                    TransitionAlpha = 0f;
                    _pendingState = GameState.None;
                    _pendingContext = null;

                    OnStateTransitionComplete?.Invoke(CurrentState);
                }

                return; // Don't update current state during transition
            }

            // Normal state update
            if (_handlers.TryGetValue(CurrentState, out IStateHandler handler))
            {
                handler.OnUpdate(dt);
            }
        }

        // ════════════════════════════════════════
        //  INTERNAL
        // ════════════════════════════════════════

        private void ExecuteTransition(GameState newState, StateContext context)
        {
            // Exit current state
            if (_handlers.TryGetValue(CurrentState, out IStateHandler oldHandler))
            {
                oldHandler.OnExit();
            }

            // Update state tracking
            PreviousState = CurrentState;
            CurrentState = newState;

            // Also update the GameManager context
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CurrentContext = context;
            }

            // Enter new state
            if (_handlers.TryGetValue(CurrentState, out IStateHandler newHandler))
            {
                newHandler.OnEnter(context);
            }
            else
            {
                Debug.LogWarning($"[StateManager] No handler registered for state: {CurrentState}");
            }

            Debug.Log($"[StateManager] State changed: {PreviousState} → {CurrentState}");
        }

        // ════════════════════════════════════════
        //  QUERIES
        // ════════════════════════════════════════

        /// <summary>
        /// Check if a handler is registered for a given state.
        /// </summary>
        public bool HasHandler(GameState state) => _handlers.ContainsKey(state);

        /// <summary>
        /// Get the handler for a given state (null if not registered).
        /// </summary>
        public IStateHandler GetHandler(GameState state)
        {
            _handlers.TryGetValue(state, out IStateHandler handler);
            return handler;
        }
    }
}
