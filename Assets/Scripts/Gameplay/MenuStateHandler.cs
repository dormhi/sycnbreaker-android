/* =========================================
   MenuStateHandler.cs — Main Menu State
   
   IStateHandler for the MENU game state.
   Displays main menu with Play button.
   ========================================= */

using UnityEngine;
using SyncBreaker.Core;
using SyncBreaker.UI;

namespace SyncBreaker.Gameplay
{
    /// <summary>
    /// Controls the main menu state behavior.
    /// </summary>
    public class MenuStateHandler : MonoBehaviour, IStateHandler
    {
        private MainMenuUI _menuUI;
        private bool _readyToTransition;

        public void Initialize(MainMenuUI menuUI)
        {
            _menuUI = menuUI;
        }

        public void OnEnter(StateContext context)
        {
            _readyToTransition = false;

            if (_menuUI != null)
            {
                _menuUI.Show();
                _menuUI.OnPlayClicked += HandlePlayClicked;
            }

            Debug.Log("[MenuState] Entered main menu.");
        }

        public void OnUpdate(float dt)
        {
            if (_readyToTransition)
            {
                _readyToTransition = false;
                GameManager.Instance.State.ChangeState(GameState.Hub);
            }
        }

        public void OnExit()
        {
            if (_menuUI != null)
            {
                _menuUI.OnPlayClicked -= HandlePlayClicked;
                _menuUI.Hide();
            }
        }

        private void HandlePlayClicked()
        {
            _readyToTransition = true;
        }
    }
}
