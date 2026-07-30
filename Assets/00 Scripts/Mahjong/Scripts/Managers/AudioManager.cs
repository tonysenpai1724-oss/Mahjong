using MahjongOut3D.Core;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Tracks audio state and later will route music, SFX and haptic cues.
    /// </summary>
    public sealed class AudioManager : ManagerBehaviour
    {
        /// <summary>
        /// Gets a value indicating whether audio output is muted.
        /// </summary>
        public bool IsMuted { get; private set; }

        /// <summary>
        /// Gets the bootstrap order for the audio manager.
        /// </summary>
        public override int InitializationOrder => 60;

        /// <summary>
        /// Updates the runtime mute state.
        /// </summary>
        /// <param name="isMuted">New mute state.</param>
        public void SetMuted(bool isMuted)
        {
            IsMuted = isMuted;
        }
    }
}
