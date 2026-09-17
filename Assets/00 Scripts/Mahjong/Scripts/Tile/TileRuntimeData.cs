using System;
using UnityEngine;

namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Stores runtime spawn data for a Mahjong tile instance.
    /// </summary>
    [Serializable]
    public sealed class TileRuntimeData
    {
        [SerializeField] private int tileId;
        [SerializeField] private int matchId;
        [SerializeField] private Vector3Int gridCoordinate;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localEulerAngles;
        [SerializeField] private int surfaceShellIndex;
        [SerializeField] private int runtimeBlockIndex;

        /// <summary>
        /// Gets or sets the unique runtime tile identifier.
        /// </summary>
        public int TileId
        {
            get => tileId;
            set => tileId = value;
        }

        /// <summary>
        /// Gets or sets the match group identifier.
        /// </summary>
        public int MatchId
        {
            get => matchId;
            set => matchId = value;
        }

        /// <summary>
        /// Gets or sets the logical grid coordinate inside the voxel volume.
        /// </summary>
        public Vector3Int GridCoordinate
        {
            get => gridCoordinate;
            set => gridCoordinate = value;
        }

        /// <summary>
        /// Gets or sets the local-space spawn position.
        /// </summary>
        public Vector3 LocalPosition
        {
            get => localPosition;
            set => localPosition = value;
        }

        /// <summary>
        /// Gets or sets the local-space Euler rotation.
        /// </summary>
        public Vector3 LocalEulerAngles
        {
            get => localEulerAngles;
            set => localEulerAngles = value;
        }

        /// <summary>
        /// Gets or sets the nested shell depth for surface-generated levels, where zero is outermost.
        /// </summary>
        public int SurfaceShellIndex
        {
            get => Mathf.Max(0, surfaceShellIndex);
            set => surfaceShellIndex = Mathf.Max(0, value);
        }

        /// <summary>
        /// Gets or sets the duplicated runtime block index that owns this tile.
        /// </summary>
        public int RuntimeBlockIndex
        {
            get => Mathf.Max(0, runtimeBlockIndex);
            set => runtimeBlockIndex = Mathf.Max(0, value);
        }
    }
}
