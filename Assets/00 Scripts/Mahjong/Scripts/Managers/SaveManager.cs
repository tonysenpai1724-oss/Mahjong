using MahjongOut3D.Core;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Tracks save system readiness and later will own JSON persistence.
    /// </summary>
    public sealed class SaveManager : ManagerBehaviour
    {
        /// <summary>
        /// Gets a value indicating whether the player profile has been loaded.
        /// </summary>
        public bool HasLoadedProfile { get; private set; }

        /// <summary>
        /// Gets the bootstrap order for the save manager.
        /// </summary>
        public override int InitializationOrder => 70;

        /// <summary>
        /// Marks the runtime profile as loaded.
        /// </summary>
        public void MarkProfileLoaded()
        {
            HasLoadedProfile = true;
        }

        /// <summary>
        /// Clears runtime save state during shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            HasLoadedProfile = false;
        }
    }
}
