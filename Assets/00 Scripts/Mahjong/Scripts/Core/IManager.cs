namespace MahjongOut3D.Core
{
    /// <summary>
    /// Defines the lifecycle contract for every runtime manager in Mahjong Out 3D.
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// Gets a value indicating whether the manager has completed initialization.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the initialization order used by the bootstrapper.
        /// </summary>
        int InitializationOrder { get; }

        /// <summary>
        /// Initializes the manager with the shared game context.
        /// </summary>
        /// <param name="context">Shared game context for the current runtime session.</param>
        void Initialize(GameContext context);

        /// <summary>
        /// Shuts the manager down and releases runtime state.
        /// </summary>
        void Shutdown();
    }
}
