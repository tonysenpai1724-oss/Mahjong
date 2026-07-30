namespace MahjongOut3D.Data
{
    /// <summary>
    /// Published after the player save file has been loaded or created.
    /// </summary>
    public readonly struct SaveDataLoadedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveDataLoadedEvent"/> struct.
        /// </summary>
        /// <param name="saveData">Loaded player save data.</param>
        public SaveDataLoadedEvent(PlayerSaveData saveData)
        {
            SaveData = saveData;
        }

        /// <summary>
        /// Gets the loaded player save data.
        /// </summary>
        public PlayerSaveData SaveData { get; }
    }
}
