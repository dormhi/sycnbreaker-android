/* =========================================
   EndlessStateHandler.cs — Endless Mode State
   
   IStateHandler for the ENDLESS game state.
   Manages endless mode gameplay with wave system.
   ========================================= */

using UnityEngine;
using SyncBreaker.Core;
using SyncBreaker.UI;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Controls endless mode state behavior.
    /// Similar to LevelState but with wave progression.
    /// </summary>
    public class EndlessStateHandler : MonoBehaviour, IStateHandler
    {
        private LevelManager _levels;
        private TouchInputHandler _input;
        private GameplayUI _ui;

        private float _completionDelay;
        private bool _transitionPending;

        public void Initialize(LevelManager levels, TouchInputHandler input, GameplayUI ui)
        {
            _levels = levels;
            _input = input;
            _ui = ui;
        }

        public void OnEnter(StateContext context)
        {
            _transitionPending = false;
            _completionDelay = 0f;

            _levels.StartEndless();

            if (_ui != null)
            {
                _ui.Bind(_levels.Bar, _levels);
            }

            if (_input != null)
            {
                _input.OnTap += HandleTap;
            }

            _levels.OnLevelFailed += HandleEndlessFailed;

            Debug.Log("[EndlessState] Entered endless mode.");
        }

        public void OnUpdate(float dt)
        {
            if (_levels == null) return;

            _levels.UpdateEndless(dt);

            if (_transitionPending)
            {
                _completionDelay -= dt;
                if (_completionDelay <= 0f)
                {
                    _transitionPending = false;

                    var ctx = new StateContext("endless_over")
                    {
                        EndlessScore = _levels.Bar?.Score ?? 0,
                        EndlessWave = _levels.EndlessWave,
                        EndlessMaxCombo = _levels.Bar?.MaxCombo ?? 0,
                        EndlessHitCount = _levels.Bar?.HitCount ?? 0
                    };

                    if (_levels.UsedRevive)
                    {
                        ctx.NoRevive = true;
                    }

                    GameManager.Instance.State.ChangeState(GameState.EndlessOver, ctx);
                }
            }
        }

        public void OnExit()
        {
            if (_input != null)
            {
                _input.OnTap -= HandleTap;
            }

            if (_levels != null)
            {
                _levels.OnLevelFailed -= HandleEndlessFailed;
            }

            if (_ui != null)
            {
                _ui.Unbind();
            }
        }

        private void HandleTap()
        {
            if (_levels?.Bar == null || _transitionPending) return;

            _levels.Bar.ProcessEndlessHit(_levels.EndlessWave);
        }

        private void HandleEndlessFailed(int levelIndex, int score)
        {
            Debug.Log($"[EndlessState] Endless run ended. Score: {score}, Wave: {_levels.EndlessWave}");

            _transitionPending = true;
            _completionDelay = 1.5f;
        }
    }
}
