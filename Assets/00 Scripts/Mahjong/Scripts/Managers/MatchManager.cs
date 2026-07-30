using MahjongOut3D.Core;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Owns match resolution state and later will drive pair validation and feedback.
    /// </summary>
    public sealed class MatchManager : ManagerBehaviour
    {
        /// <summary>
        /// Gets a value indicating whether a match resolution is currently running.
        /// </summary>
        public bool IsResolvingMatch { get; private set; }

        /// <summary>
        /// Gets the bootstrap order for the match manager.
        /// </summary>
        public override int InitializationOrder => 30;

        /// <summary>
        /// Starts a match resolution block if none is active.
        /// </summary>
        /// <returns>True when the lock was acquired; otherwise false.</returns>
        public bool TryBeginResolution()
        {
            if (IsResolvingMatch)
            {
                return false;
            }

            IsResolvingMatch = true;
            return true;
        }

        /// <summary>
        /// Ends the current match resolution block.
        /// </summary>
        public void EndResolution()
        {
            IsResolvingMatch = false;
        }
    }
}
