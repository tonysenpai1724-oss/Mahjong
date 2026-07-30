using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Defines a Mahjong Out 3D level as a voxel-backed tile layout.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Level/Level Definition", fileName = "LevelDefinition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [field: Header("Identity")]
        [field: SerializeField]
        public string LevelName { get; private set; } = "New Level";

        [field: Header("Grid")]
        [field: SerializeField]
        public VoxelGridSize GridSize { get; private set; } = new VoxelGridSize(4, 4, 4);

        [field: SerializeField]
        public VoxelGridLayoutSettings LayoutOverride { get; private set; }

        [field: Header("Presentation")]
        [field: SerializeField]
        public LevelShapeType Shape { get; private set; } = LevelShapeType.Cube;

        [field: SerializeField]
        public LevelDifficulty Difficulty { get; private set; } = LevelDifficulty.Easy;

        [field: Header("Tiles")]
        [field: SerializeField]
        public List<LevelTileDefinition> Tiles { get; private set; } = new List<LevelTileDefinition>();

        /// <summary>
        /// Builds a dense 3D match-id array from the tile list.
        /// </summary>
        /// <returns>3D array where -1 means empty and any non-negative value is a match id.</returns>
        public int[,,] BuildMatchIdArray()
        {
            int[,,] result = new int[GridSize.Width, GridSize.Height, GridSize.Depth];

            for (int x = 0; x < GridSize.Width; x++)
            {
                for (int y = 0; y < GridSize.Height; y++)
                {
                    for (int z = 0; z < GridSize.Depth; z++)
                    {
                        result[x, y, z] = -1;
                    }
                }
            }

            for (int index = 0; index < Tiles.Count; index++)
            {
                LevelTileDefinition tile = Tiles[index];
                if (tile == null || !GridSize.Contains(tile.GridCoordinate))
                {
                    continue;
                }

                result[tile.GridCoordinate.x, tile.GridCoordinate.y, tile.GridCoordinate.z] = tile.MatchId;
            }

            return result;
        }
    }
}
