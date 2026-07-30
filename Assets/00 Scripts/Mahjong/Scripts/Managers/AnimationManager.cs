using MahjongOut3D.Core;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Tracks animation locks and later will coordinate tile transitions and feedback timing.
    /// </summary>
    public sealed class AnimationManager : ManagerBehaviour
    {
        /// <summary>
        /// Gets a value indicating whether a blocking animation is currently running.
        /// </summary>
        public bool IsAnimationLocked { get; private set; }

        /// <summary>
        /// Gets the bootstrap order for the animation manager.
        /// </summary>
        public override int InitializationOrder => 90;

        /// <summary>
        /// Updates the current animation lock state.
        /// </summary>
        /// <param name="isLocked">New animation lock state.</param>
        public void SetAnimationLock(bool isLocked)
        {
            IsAnimationLocked = isLocked;
        }
    }
}
