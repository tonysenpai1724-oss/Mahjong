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
        public bool UseSurfaceTilePlacement { get; private set; }

        [field: SerializeField, Min(0)]
        public int LayerCount { get; private set; }

        [field: SerializeField]
        public LevelDifficulty Difficulty { get; private set; } = LevelDifficulty.Easy;

        [field: Header("Visuals")]
        [field: SerializeField]
        public List<string> FillCategoryNames { get; private set; } = new List<string>();

        [field: Header("Tiles")]
        [field: SerializeField]
        public List<LevelTileDefinition> Tiles { get; private set; } = new List<LevelTileDefinition>();

        /// <summary>
        /// Builds a dense 3D match-id array from the tile list.
        /// </summary>
        /// <returns>3D array where -1 means empty and any non-negative value is a match id.</returns>
        public int[,,] BuildMatchIdArray()
        {
            VoxelGridSize runtimeGridSize = GetRuntimeGridSize();
            int[,,] result = new int[runtimeGridSize.Width, runtimeGridSize.Height, runtimeGridSize.Depth];

            for (int x = 0; x < runtimeGridSize.Width; x++)
            {
                for (int y = 0; y < runtimeGridSize.Height; y++)
                {
                    for (int z = 0; z < runtimeGridSize.Depth; z++)
                    {
                        result[x, y, z] = -1;
                    }
                }
            }

            for (int index = 0; index < Tiles.Count; index++)
            {
                LevelTileDefinition tile = Tiles[index];
                if (tile == null || !runtimeGridSize.Contains(tile.GridCoordinate))
                {
                    continue;
                }

                result[tile.GridCoordinate.x, tile.GridCoordinate.y, tile.GridCoordinate.z] = tile.MatchId;
            }

            return result;
        }

        /// <summary>
        /// Resolves the grid size required to hold every logical tile coordinate.
        /// </summary>
        /// <returns>Runtime-safe grid size.</returns>
        public VoxelGridSize GetRuntimeGridSize()
        {
            int width = GridSize.Width;
            int height = GridSize.Height;
            int depth = GridSize.Depth;

            if (Tiles == null)
            {
                return new VoxelGridSize(width, height, depth);
            }

            for (int index = 0; index < Tiles.Count; index++)
            {
                LevelTileDefinition tile = Tiles[index];
                if (tile == null)
                {
                    continue;
                }

                width = Mathf.Max(width, tile.GridCoordinate.x + 1);
                height = Mathf.Max(height, tile.GridCoordinate.y + 1);
                depth = Mathf.Max(depth, tile.GridCoordinate.z + 1);
            }

            return new VoxelGridSize(width, height, depth);
        }

        /// <summary>
        /// Resolves the effective layer count stored in this asset.
        /// Falls back to tile shell indices when older assets have not been backfilled yet.
        /// </summary>
        /// <returns>Resolved shell-layer count.</returns>
        public int GetResolvedLayerCount()
        {
            return Mathf.Max(LayerCount, CalculateLayerCountFromTiles());
        }

        private void OnValidate()
        {
            LayerCount = CalculateLayerCountFromTiles();
        }

        private int CalculateLayerCountFromTiles()
        {
            if (Tiles == null || Tiles.Count == 0)
            {
                return 0;
            }

            bool hasAnyTile = false;
            int maxShellIndex = 0;
            for (int index = 0; index < Tiles.Count; index++)
            {
                LevelTileDefinition tile = Tiles[index];
                if (tile == null)
                {
                    continue;
                }

                hasAnyTile = true;
                maxShellIndex = Mathf.Max(maxShellIndex, tile.SurfaceShellIndex);
            }

            return hasAnyTile ? maxShellIndex + 1 : 0;
        }
    }
}
