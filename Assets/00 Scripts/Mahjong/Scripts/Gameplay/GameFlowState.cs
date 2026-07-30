namespace MahjongOut3D.Gameplay
{
    /// <summary>
    /// Defines the high-level flow states used by the game session.
    /// </summary>
    public enum GameFlowState
    {
        None = 0,
        Bootstrapping = 1,
        MainMenu = 2,
        LevelSelect = 3,
        Gameplay = 4,
        Paused = 5,
        Win = 6,
        Lose = 7,
    }
}
