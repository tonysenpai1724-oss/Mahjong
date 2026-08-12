using System;

namespace MahjongOut3D.Data
{
    /// <summary>
    /// Stores persistent player data serialized to JSON on disk.
    /// </summary>
    [Serializable]
    public sealed class PlayerSaveData
    {
        public int coins;
        public string selectedSkin = "Default";
        public bool musicEnabled = true;
        public bool soundEnabled = true;
    }
}
