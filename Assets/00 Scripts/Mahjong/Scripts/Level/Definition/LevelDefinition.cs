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

        [field: SerializeField, Min(1)]
        public int BlockCount { get; private set; } = 1;

        [field: SerializeField, Min(0)]
        public int BlockSpacingCells { get; private set; } = 1;

        [field: SerializeField, Min(0)]
        public int LayerCount { get; private set; }

        [field: SerializeField]
        public LevelDifficulty Difficulty { get; private set; } = LevelDifficulty.Easy;

        [field: Header("Gameplay")]
        [field: SerializeField, Range(0f, 1f)]
        public float FaceDownTileRatio { get; private set; }

        [field: SerializeField, Range(0f, 1f)]
        public float ComboTileRatio { get; private set; }

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
            VoxelGridSize singleBlockGridSize = GetSingleBlockRuntimeGridSize();
            int blockStrideWidth = GetBlockStrideWidth(singleBlockGridSize);
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
                if (tile == null || !singleBlockGridSize.Contains(tile.GridCoordinate))
                {
                    continue;
                }

                for (int blockIndex = 0; blockIndex < Mathf.Max(1, BlockCount); blockIndex++)
                {
                    Vector3Int blockCoordinate = new Vector3Int(
                        tile.GridCoordinate.x + (blockIndex * blockStrideWidth),
                        tile.GridCoordinate.y,
                        tile.GridCoordinate.z);

                    if (runtimeGridSize.Contains(blockCoordinate))
                    {
                        result[blockCoordinate.x, blockCoordinate.y, blockCoordinate.z] = tile.MatchId;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves the grid size required to hold every logical tile coordinate.
        /// </summary>
        /// <returns>Runtime-safe grid size.</returns>
        public VoxelGridSize GetRuntimeGridSize()
        {
            VoxelGridSize singleBlockGridSize = GetSingleBlockRuntimeGridSize();
            int resolvedBlockCount = Mathf.Max(1, BlockCount);
            int resolvedSpacing = Mathf.Max(0, BlockSpacingCells);
            int expandedWidth = (singleBlockGridSize.Width * resolvedBlockCount) + (resolvedSpacing * Mathf.Max(0, resolvedBlockCount - 1));
            return new VoxelGridSize(expandedWidth, singleBlockGridSize.Height, singleBlockGridSize.Depth);
        }

        /// <summary>
        /// Resolves the grid size required to hold one authored block before duplication is applied.
        /// </summary>
        /// <returns>Runtime-safe single-block grid size.</returns>
        public VoxelGridSize GetSingleBlockRuntimeGridSize()
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
        /// Resolves the number of cells between the origin of one duplicated block and the next.
        /// </summary>
        /// <param name="singleBlockGridSize">Single-block runtime grid size.</param>
        /// <returns>Horizontal stride between duplicated blocks.</returns>
        public int GetBlockStrideWidth(VoxelGridSize singleBlockGridSize)
        {
            return singleBlockGridSize.Width + Mathf.Max(0, BlockSpacingCells);
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

        /// <summary>
        /// Resolves the face-down tile ratio used by memory-style levels.
        /// Older assets fall back to the previous difficulty defaults until they are customized.
        /// </summary>
        public float GetResolvedFaceDownTileRatio()
        {
            if (FaceDownTileRatio > 0f)
            {
                return Mathf.Clamp01(FaceDownTileRatio);
            }

            switch (Difficulty)
            {
                case LevelDifficulty.Hard:
                    return 0.2f;
                case LevelDifficulty.Expert:
                    return 0.15f;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Resolves the combo-tile ratio for tiles that trigger three extra matches.
        /// Older assets fall back to the previous difficulty defaults until they are customized.
        /// </summary>
        public float GetResolvedComboTileRatio()
        {
            if (ComboTileRatio > 0f)
            {
                return Mathf.Clamp01(ComboTileRatio);
            }

            switch (Difficulty)
            {
                case LevelDifficulty.Hard:
                    return 0.08f;
                case LevelDifficulty.Expert:
                    return 0.1f;
                default:
                    return 0f;
            }
        }

        private void OnValidate()
        {
            BlockCount = Mathf.Max(1, BlockCount);
            BlockSpacingCells = Mathf.Max(0, BlockSpacingCells);
            FaceDownTileRatio = Mathf.Clamp01(FaceDownTileRatio);
            ComboTileRatio = Mathf.Clamp01(ComboTileRatio);
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
