namespace MahjongOut3D.Gameplay
{
    /// <summary>
    /// Published after a gameplay power-up is successfully consumed.
    /// </summary>
    public readonly struct PowerUpUsedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PowerUpUsedEvent"/> struct.
        /// </summary>
        public PowerUpUsedEvent(PowerUpType powerUpType)
        {
            PowerUpType = powerUpType;
        }

        /// <summary>
        /// Gets the consumed power-up type.
        /// </summary>
        public PowerUpType PowerUpType { get; }
    }
}
