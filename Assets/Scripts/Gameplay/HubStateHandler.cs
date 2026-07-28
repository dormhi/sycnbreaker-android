/* =========================================
   HubStateHandler.cs — Hub/Level Select State
   
   IStateHandler for the HUB game state.
   Level selection screen with progress display.
   ========================================= */

using UnityEngine;
using SyncBreaker.Core;
using SyncBreaker.UI;
using SyncBreaker.Systems;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Controls the hub/level-select state.
    /// </summary>
    public class HubStateHandler : MonoBehaviour, IStateHandler
    {
        private HubUI _hubUI;
        private LevelManager _levels;
        private bool _pendingAction;
        private string _actionType; // "level", "endless", "back"

        public void Initialize(HubUI hubUI, LevelManager levels)
        {
            _hubUI = hubUI;
            _levels = levels;
        }

        public void OnEnter(StateContext context)
        {
            _pendingAction = false;
            _actionType = null;

            if (_hubUI != null && _levels != null)
            {
                _hubUI.Bind(_levels);
                _hubUI.OnLevelSelected += HandleLevelSelected;
                _hubUI.OnEndlessSelected += HandleEndlessSelected;
                _hubUI.OnBackClicked += HandleBackClicked;
                _hubUI.OnResetClicked += HandleResetClicked;
                _hubUI.OnLockpickShortcut += HandleLockpickShortcut;
                _hubUI.Show();
            }

            Debug.Log("[HubState] Entered hub.");
        }

        public void OnUpdate(float dt)
        {
            if (!_pendingAction) return;

            _pendingAction = false;

            switch (_actionType)
            {
                case "level":
                    int levelIndex = GameManager.Instance.SelectedLevelIndex;

                    // Check energy
                    var energy = GameManager.Instance.GetComponent<EnergySystem>();
                    if (energy != null && !energy.ConsumeForLevel())
                    {
                        Debug.Log("[HubState] Not enough energy to play!");
                        // Show energy popup
                        GameManager.Instance.State.ChangeState(GameState.Hub);
                        break;
                    }

                    var ctx = new StateContext("level", levelIndex);
                    GameManager.Instance.State.ChangeState(GameState.Level, ctx);
                    break;

                case "endless":
                    var endlessEnergy = GameManager.Instance.GetComponent<EnergySystem>();
                    if (endlessEnergy != null && !endlessEnergy.ConsumeForEndless())
                    {
                        Debug.Log("[HubState] Not enough energy for endless!");
                        GameManager.Instance.State.ChangeState(GameState.Hub);
                        break;
                    }

                    var endlessCtx = new StateContext("endless");
                    GameManager.Instance.State.ChangeState(GameState.Endless, endlessCtx);
                    break;

                case "back":
                    GameManager.Instance.State.ChangeState(GameState.Menu);
                    break;

                case "reset":
                    if (_levels != null)
                    {
                        _levels.ResetProgress();
                        _hubUI?.Bind(_levels);
                    }
                    break;
            }
        }

        public void OnExit()
        {
            if (_hubUI != null)
            {
                _hubUI.OnLevelSelected -= HandleLevelSelected;
                _hubUI.OnEndlessSelected -= HandleEndlessSelected;
                _hubUI.OnBackClicked -= HandleBackClicked;
                _hubUI.OnResetClicked -= HandleResetClicked;
                _hubUI.OnLockpickShortcut -= HandleLockpickShortcut;
                _hubUI.Hide();
            }
        }

        private void HandleLevelSelected(int index)
        {
            GameManager.Instance.SelectedLevelIndex = index;

            // Check if level is locked — offer lockpick shortcut
            var progress = _levels?.Progress;
            if (progress != null && index < progress.Count)
            {
                if (!progress[index].unlocked)
                {
                    // Level is locked — try lockpick to unlock
                    var shortcutCtx = new StateContext("shortcut", index);
                    GameManager.Instance.State.ChangeState(GameState.Lockpick, shortcutCtx);
                    return;
                }
            }

            _actionType = "level";
            _pendingAction = true;
        }

        private void HandleEndlessSelected()
        {
            _actionType = "endless";
            _pendingAction = true;
        }

        private void HandleBackClicked()
        {
            _actionType = "back";
            _pendingAction = true;
        }

        private void HandleResetClicked()
        {
            _actionType = "reset";
            _pendingAction = true;
        }

        private void HandleLockpickShortcut(int index)
        {
            GameManager.Instance.SelectedLevelIndex = index;
            var shortcutCtx = new StateContext("shortcut", index);
            GameManager.Instance.State.ChangeState(GameState.Lockpick, shortcutCtx);
        }
    }
}
