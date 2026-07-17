/* =========================================
   LevelStateHandler.cs — Level State Behavior
   
   IStateHandler implementation for the Level
   game state. Coordinates between TimingBar,
   LevelManager, TouchInput, and UI.
   
   Ported from: js/GameManager.js (_updateLevelState)
   ========================================= */

using UnityEngine;
using SyncBreaker.Core;
using SyncBreaker.UI;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Controls game behavior during the LEVEL state.
    /// Connects touch input to timing bar, checks win/lose,
    /// and triggers state transitions.
    /// </summary>
    public class LevelStateHandler : MonoBehaviour, IStateHandler
    {
        // ── References ──
        private LevelManager _levels;
        private TouchInputHandler _input;
        private GameplayUI _ui;

        // ── State ──
        private float _completionDelay;
        private bool _transitionPending;

        // ════════════════════════════════════════
        //  INITIALIZATION
        // ════════════════════════════════════════

        public void Initialize(LevelManager levels, TouchInputHandler input, GameplayUI ui)
        {
            _levels = levels;
            _input = input;
            _ui = ui;
        }

        // ════════════════════════════════════════
        //  IStateHandler
        // ════════════════════════════════════════

        public void OnEnter(StateContext context)
        {
            _transitionPending = false;
            _completionDelay = 0f;

            // Determine level index
            int levelIndex = context?.LevelIndex ?? GameManager.Instance.SelectedLevelIndex;
            if (levelIndex < 0)
            {
                Debug.LogError("[LevelState] No level selected!");
                GameManager.Instance.State.ChangeState(GameState.Hub);
                return;
            }

            // Start the level
            bool started = _levels.StartLevel(levelIndex);
            if (!started)
            {
                Debug.LogError($"[LevelState] Failed to start level {levelIndex}");
                GameManager.Instance.State.ChangeState(GameState.Hub);
                return;
            }

            // Bind UI
            if (_ui != null)
            {
                _ui.Bind(_levels.Bar, _levels);
            }

            // Connect touch input
            if (_input != null)
            {
                _input.OnTap += HandleTap;
            }

            // Listen for level completion/failure
            _levels.OnLevelComplete += HandleLevelComplete;
            _levels.OnLevelFailed += HandleLevelFailed;

            Debug.Log($"[LevelState] Entered level {levelIndex}: {_levels.CurrentLevelData.levelName}");
        }

        public void OnUpdate(float dt)
        {
            if (_levels == null) return;

            // Update level timer
            _levels.UpdateLevel(dt);

            // Handle delayed transitions (show result before switching)
            if (_transitionPending)
            {
                _completionDelay -= dt;
                if (_completionDelay <= 0f)
                {
                    _transitionPending = false;
                    ExecutePendingTransition();
                }
            }
        }

        public void OnExit()
        {
            // Disconnect input
            if (_input != null)
            {
                _input.OnTap -= HandleTap;
            }

            // Disconnect events
            if (_levels != null)
            {
                _levels.OnLevelComplete -= HandleLevelComplete;
                _levels.OnLevelFailed -= HandleLevelFailed;
            }

            // Unbind UI
            if (_ui != null)
            {
                _ui.Unbind();
            }
        }

        // ════════════════════════════════════════
        //  INPUT
        // ════════════════════════════════════════

        private void HandleTap()
        {
            if (_levels?.Bar == null || _transitionPending) return;

            _levels.Bar.ProcessHit();
        }

        // ════════════════════════════════════════
        //  LEVEL RESULTS
        // ════════════════════════════════════════

        private void HandleLevelComplete(int levelIndex, int score)
        {
            Debug.Log($"[LevelState] Level {levelIndex} completed with score {score}!");

            if (_ui != null)
            {
                _ui.ShowLevelComplete(score, _levels.Bar?.MaxCombo ?? 0);
            }

            // Delay before transition to Hub
            _transitionPending = true;
            _completionDelay = 2.0f;
        }

        private void HandleLevelFailed(int levelIndex, int score)
        {
            Debug.Log($"[LevelState] Level {levelIndex} failed with score {score}");

            // Delay before transition to GameOver
            _transitionPending = true;
            _completionDelay = 1.0f;
        }

        private void ExecutePendingTransition()
        {
            if (_levels.LevelComplete)
            {
                // Go back to Hub
                GameManager.Instance.State.ChangeState(GameState.Hub);
            }
            else if (_levels.LevelFailed)
            {
                if (_levels.UsedRevive)
                {
                    // Already used revive → Game Over with no revive option
                    var ctx = new StateContext("gameover", _levels.CurrentLevelIndex)
                    {
                        Score = _levels.Bar?.Score ?? 0,
                        NoRevive = true
                    };
                    GameManager.Instance.State.ChangeState(GameState.GameOver, ctx);
                }
                else
                {
                    // First death → Game Over with revive option
                    var ctx = new StateContext("gameover", _levels.CurrentLevelIndex)
                    {
                        Score = _levels.Bar?.Score ?? 0,
                        NoRevive = false
                    };
                    GameManager.Instance.State.ChangeState(GameState.GameOver, ctx);
                }
            }
        }
    }
}
