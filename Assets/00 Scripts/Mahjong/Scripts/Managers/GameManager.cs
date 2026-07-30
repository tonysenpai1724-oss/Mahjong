using MahjongOut3D.Core;
using MahjongOut3D.Gameplay;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Owns the high-level state machine for the game session.
    /// </summary>
    public sealed class GameManager : ManagerBehaviour
    {
        /// <summary>
        /// Gets the current runtime game flow state.
        /// </summary>
        public GameFlowState CurrentState { get; private set; } = GameFlowState.None;

        /// <summary>
        /// Gets the bootstrap order for the game manager.
        /// </summary>
        public override int InitializationOrder => 0;

        /// <summary>
        /// Applies the initial game flow state from project settings.
        /// </summary>
        protected override void OnInitialize()
        {
            SetState(GameFlowState.Bootstrapping);

            if (Context.ProjectSettings != null)
            {
                SetState(Context.ProjectSettings.InitialGameState);
            }
        }

        /// <summary>
        /// Changes the high-level game flow state and publishes the update.
        /// </summary>
        /// <param name="newState">New runtime state.</param>
        public void SetState(GameFlowState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }

            GameFlowState previousState = CurrentState;
            CurrentState = newState;
            Context?.EventBus.Publish(new GameFlowStateChangedEvent(previousState, CurrentState));
        }

        /// <summary>
        /// Transitions the runtime into gameplay.
        /// </summary>
        public void StartGameplay()
        {
            SetState(GameFlowState.Gameplay);
        }

        /// <summary>
        /// Pauses gameplay if the game is currently active.
        /// </summary>
        public void PauseGameplay()
        {
            if (CurrentState == GameFlowState.Gameplay)
            {
                SetState(GameFlowState.Paused);
            }
        }

        /// <summary>
        /// Resumes gameplay if the game is currently paused.
        /// </summary>
        public void ResumeGameplay()
        {
            if (CurrentState == GameFlowState.Paused)
            {
                SetState(GameFlowState.Gameplay);
            }
        }

        /// <summary>
        /// Transitions the runtime into the win state.
        /// </summary>
        public void WinGameplay()
        {
            SetState(GameFlowState.Win);
        }

        /// <summary>
        /// Transitions the runtime into the lose state.
        /// </summary>
        public void LoseGameplay()
        {
            SetState(GameFlowState.Lose);
        }
    }
}
