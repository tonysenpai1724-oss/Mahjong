namespace MahjongOut3D.GameplayInput
{
    /// <summary>
    /// Published when gameplay input is enabled or disabled.
    /// </summary>
    public readonly struct InputEnabledChangedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputEnabledChangedEvent"/> struct.
        /// </summary>
        /// <param name="isEnabled">Current input enabled state.</param>
        public InputEnabledChangedEvent(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// Gets a value indicating whether gameplay input is enabled.
        /// </summary>
        public bool IsEnabled { get; }
    }
}
