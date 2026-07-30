using System;
using System.Collections.Generic;

namespace MahjongOut3D.Data
{
    /// <summary>
    /// Stores persistent player data serialized to JSON on disk.
    /// </summary>
    [Serializable]
    public sealed class PlayerSaveData
    {
        public int currentLevel;
        public int highestUnlockedLevel = 1;
        public int coins;
        public string selectedSkin = "Default";
        public bool musicEnabled = true;
        public bool soundEnabled = true;
        public List<int> completedLevels = new List<int>();
    }
}
