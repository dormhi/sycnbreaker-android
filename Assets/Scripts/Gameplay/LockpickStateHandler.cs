/* =========================================
   LockpickStateHandler.cs — Lockpick State
   
   IStateHandler for the LOCKPICK game state.
   Manages the lockpick mini-game lifecycle:
   - Shortcut (skip to next level)
   - Revive (extra life)
   - Endless revive
   
   Ported from: js/GameManager.js (_updateLockpickState)
   ========================================= */

using UnityEngine;
using SyncBreaker.Core;
using SyncBreaker.UI;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Controls game behavior during the LOCKPICK state.
    /// Creates and manages a LockpickSystem instance,
    /// connects swipe input, and handles results.
    /// </summary>
    public class LockpickStateHandler : MonoBehaviour, IStateHandler
    {
        // ── References ──
        private LockpickSystem _lockpick;
        private LockpickUI _lockpickUI;
        private TouchInputHandler _input;
        private LevelManager _levels;

        // ── State ──
        private string _reason; // "shortcut", "revive", "endless_revive"
        private int _levelIndex;

        // ════════════════════════════════════════
        //  INITIALIZATION
        // ════════════════════════════════════════

        public void Initialize(LevelManager levels, TouchInputHandler input)
        {
            _levels = levels;
            _input = input;
        }

        // ════════════════════════════════════════
        //  IStateHandler
        // ════════════════════════════════════════

        public void OnEnter(StateContext context)
        {
            _reason = context?.Reason ?? "shortcut";
            _levelIndex = context?.LevelIndex ?? GameManager.Instance.SelectedLevelIndex;

            // Determine difficulty based on reason
            int difficulty;
            switch (_reason)
            {
                case "shortcut":
                    // Difficulty based on target level
                    var levelData = _levels.GetLevelData(_levelIndex);
                    difficulty = levelData?.lockpickDifficulty ?? 1;
                    break;
                case "revive":
                    // Medium difficulty for revive
                    difficulty = Mathf.Clamp(_levelIndex + 1, 1, 4);
                    break;
                case "endless_revive":
                    // Based on endless wave
                    difficulty = Mathf.Clamp(context?.EndlessWave / 2 ?? 1, 1, 6);
                    break;
                default:
                    difficulty = 1;
                    break;
            }

            // Create or find LockpickSystem
            EnsureLockpick();

            // Start the mini-game
            _lockpick.StartLockpick(difficulty, OnLockpickComplete);

            // Create or find LockpickUI
            EnsureLockpickUI();
            bool isRevive = _reason == "revive" || _reason == "endless_revive";
            _lockpickUI.Bind(_lockpick, isRevive);

            // Connect swipe input
            if (_input != null)
            {
                _input.OnSwipe += HandleSwipe;
                _input.OnTap += HandleTap;
            }

            Debug.Log($"[LockpickState] Entered: reason={_reason}, " +
                      $"level={_levelIndex}, difficulty={difficulty}");
        }

        public void OnUpdate(float dt)
        {
            // LockpickSystem and LockpickUI update themselves via MonoBehaviour.Update()
        }

        public void OnExit()
        {
            // Disconnect input
            if (_input != null)
            {
                _input.OnSwipe -= HandleSwipe;
                _input.OnTap -= HandleTap;
            }

            // Unbind UI
            if (_lockpickUI != null)
            {
                _lockpickUI.Unbind();
            }

            // Cancel lockpick if still active
            if (_lockpick != null && _lockpick.Active)
            {
                _lockpick.Cancel();
            }
        }

        // ════════════════════════════════════════
        //  INPUT
        // ════════════════════════════════════════

        private void HandleSwipe(SwipeDirection dir)
        {
            _lockpick?.HandleInput(dir);
        }

        private void HandleTap()
        {
            // Tap during lockpick can act as "start" if not yet started
            if (_lockpick != null && !_lockpick.Started)
            {
                // First tap starts the cursor — send an Up swipe as trigger
                _lockpick.HandleInput(SwipeDirection.Up);
            }
        }

        // ════════════════════════════════════════
        //  RESULT
        // ════════════════════════════════════════

        private void OnLockpickComplete(bool success)
        {
            Debug.Log($"[LockpickState] Complete: success={success}, reason={_reason}");

            if (success)
            {
                HandleSuccess();
            }
            else
            {
                HandleFailure();
            }
        }

        private void HandleSuccess()
        {
            switch (_reason)
            {
                case "shortcut":
                    // Unlock the target level and go to it
                    if (_levelIndex >= 0 && _levelIndex < _levels.Progress.Count)
                    {
                        _levels.Progress[_levelIndex].unlocked = true;
                        _levels.SaveProgress();
                    }
                    GameManager.Instance.SelectedLevelIndex = _levelIndex;
                    var ctx = new StateContext("level", _levelIndex);
                    GameManager.Instance.State.ChangeState(GameState.Level, ctx);
                    break;

                case "revive":
                    // Revive: restore 1 life and go back to level
                    if (_levels.Bar != null)
                    {
                        _levels.Bar.Revive();
                        _levels.UsedRevive = true;
                    }
                    // Return to the same level state
                    // (We need to re-enter Level without resetting)
                    var reviveCtx = new StateContext("level", _levelIndex)
                    {
                        UsedRevive = true
                    };
                    GameManager.Instance.State.ChangeState(GameState.Level, reviveCtx);
                    break;

                case "endless_revive":
                    // Endless revive: restore 1 life, go back to endless
                    if (_levels.Bar != null)
                    {
                        _levels.Bar.Revive();
                        _levels.UsedRevive = true;
                    }
                    var endlessCtx = new StateContext("endless")
                    {
                        UsedRevive = true
                    };
                    GameManager.Instance.State.ChangeState(GameState.Endless, endlessCtx);
                    break;
            }
        }

        private void HandleFailure()
        {
            switch (_reason)
            {
                case "shortcut":
                    // Failed shortcut → back to Hub
                    GameManager.Instance.State.ChangeState(GameState.Hub);
                    break;

                case "revive":
                    // Failed revive → Game Over (no more chances)
                    var ctx = new StateContext("gameover", _levelIndex)
                    {
                        Score = _levels.Bar?.Score ?? 0,
                        NoRevive = true
                    };
                    GameManager.Instance.State.ChangeState(GameState.GameOver, ctx);
                    break;

                case "endless_revive":
                    // Failed endless revive → Endless Over
                    var endlessCtx = new StateContext("endless_over")
                    {
                        EndlessScore = _levels.Bar?.Score ?? 0,
                        EndlessWave = _levels.EndlessWave,
                        EndlessMaxCombo = _levels.Bar?.MaxCombo ?? 0,
                        EndlessHitCount = _levels.Bar?.HitCount ?? 0
                    };
                    GameManager.Instance.State.ChangeState(GameState.EndlessOver, endlessCtx);
                    break;
            }
        }

        // ════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════

        private void EnsureLockpick()
        {
            if (_lockpick == null)
            {
                _lockpick = FindAnyObjectByType<LockpickSystem>();
                if (_lockpick == null)
                {
                    var go = new GameObject("LockpickSystem");
                    _lockpick = go.AddComponent<LockpickSystem>();
                }
            }
        }

        private void EnsureLockpickUI()
        {
            if (_lockpickUI == null)
            {
                _lockpickUI = FindAnyObjectByType<LockpickUI>();
                // Note: LockpickUI should be pre-placed in the scene
                // on a Canvas. If not found, log a warning.
                if (_lockpickUI == null)
                {
                    Debug.LogWarning("[LockpickState] No LockpickUI found in scene. " +
                                   "Create a Canvas with LockpickUI component.");
                }
            }
        }
    }
}
