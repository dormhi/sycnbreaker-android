/* =========================================
   GameOverStateHandler.cs — Game Over State
   
   IStateHandler for GAMEOVER and ENDLESSOVER.
   Shows game over screen with score, revive option,
   and navigation buttons.
   ========================================= */

using UnityEngine;
using SyncBreaker.Core;
using SyncBreaker.UI;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Controls the game over screen state.
    /// </summary>
    public class GameOverStateHandler : MonoBehaviour, IStateHandler
    {
        private GameOverUI _gameOverUI;
        private bool _pendingAction;
        private string _actionType;

        public void Initialize(GameOverUI gameOverUI)
        {
            _gameOverUI = gameOverUI;
        }

        public void OnEnter(StateContext context)
        {
            _pendingAction = false;
            _actionType = null;

            if (_gameOverUI != null)
            {
                _gameOverUI.OnReviveClicked += HandleRevive;
                _gameOverUI.OnRetryClicked += HandleRetry;
                _gameOverUI.OnMenuClicked += HandleMenu;
            }

            // Determine if this is endless or story game over
            bool isEndless = context?.Reason == "endless_over";

            if (isEndless && context != null)
            {
                _gameOverUI?.ShowEndlessOver(
                    context.EndlessScore,
                    context.EndlessWave,
                    context.EndlessMaxCombo,
                    context.EndlessHitCount
                );
            }
            else if (context != null)
            {
                _gameOverUI?.ShowGameOver(
                    context.Score,
                    context.NoRevive,
                    context.UsedRevive
                );
            }

            Debug.Log($"[GameOverState] Entered. isEndless={isEndless}");
        }

        public void OnUpdate(float dt)
        {
            if (!_pendingAction) return;
            _pendingAction = false;

            switch (_actionType)
            {
                case "revive":
                    var reviveCtx = new StateContext("revive",
                        GameManager.Instance.SelectedLevelIndex);
                    GameManager.Instance.State.ChangeState(GameState.Lockpick, reviveCtx);
                    break;

                case "endless_revive":
                    var ctx = GameManager.Instance.CurrentContext;
                    var endlessReviveCtx = new StateContext("endless_revive")
                    {
                        EndlessWave = ctx?.EndlessWave ?? 1
                    };
                    GameManager.Instance.State.ChangeState(GameState.Lockpick, endlessReviveCtx);
                    break;

                case "retry":
                    var retryCtx = new StateContext("level",
                        GameManager.Instance.SelectedLevelIndex);
                    GameManager.Instance.State.ChangeState(GameState.Level, retryCtx);
                    break;

                case "endless_retry":
                    var endlessRetryCtx = new StateContext("endless");
                    GameManager.Instance.State.ChangeState(GameState.Endless, endlessRetryCtx);
                    break;

                case "menu":
                    GameManager.Instance.State.ChangeState(GameState.Hub);
                    break;
            }
        }

        public void OnExit()
        {
            if (_gameOverUI != null)
            {
                _gameOverUI.OnReviveClicked -= HandleRevive;
                _gameOverUI.OnRetryClicked -= HandleRetry;
                _gameOverUI.OnMenuClicked -= HandleMenu;
                _gameOverUI.Hide();
            }
        }

        private void HandleRevive()
        {
            _actionType = "revive";
            _pendingAction = true;
        }

        private void HandleRetry()
        {
            _actionType = "retry";
            _pendingAction = true;
        }

        private void HandleMenu()
        {
            _actionType = "menu";
            _pendingAction = true;
        }
    }
}
