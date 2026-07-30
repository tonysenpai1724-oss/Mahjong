namespace MahjongOut3D.Gameplay
{
    /// <summary>
    /// Published whenever the high-level game flow state changes.
    /// </summary>
    public readonly struct GameFlowStateChangedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameFlowStateChangedEvent"/> struct.
        /// </summary>
        /// <param name="previousState">Previous game state.</param>
        /// <param name="currentState">Current game state.</param>
        public GameFlowStateChangedEvent(GameFlowState previousState, GameFlowState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }

        /// <summary>
        /// Gets the previous state.
        /// </summary>
        public GameFlowState PreviousState { get; }

        /// <summary>
        /// Gets the current state.
        /// </summary>
        public GameFlowState CurrentState { get; }
    }
}
