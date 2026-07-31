using System;
using System.Collections.Generic;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Serializable DTO used for importing and exporting level data as JSON.
    /// </summary>
    [Serializable]
    public sealed class LevelJsonData
    {
        public string levelName;
        public int width;
        public int height;
        public int depth;
        public bool useSurfaceTilePlacement;
        public LevelShapeType shape;
        public LevelDifficulty difficulty;
        public List<LevelJsonTileData> tiles = new List<LevelJsonTileData>();
    }

    /// <summary>
    /// Serializable DTO representing a single JSON tile entry.
    /// </summary>
    [Serializable]
    public sealed class LevelJsonTileData
    {
        public int matchId;
        public int x;
        public int y;
        public int z;
        public bool useCustomLocalPosition;
        public float posX;
        public float posY;
        public float posZ;
        public float rotX;
        public float rotY;
        public float rotZ;
    }
}
