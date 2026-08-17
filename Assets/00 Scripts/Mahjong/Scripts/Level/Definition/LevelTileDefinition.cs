using System;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Stores a single tile entry inside a level definition asset or JSON payload.
    /// </summary>
    [Serializable]
    public sealed class LevelTileDefinition
    {
        [SerializeField] private int matchId;
        [SerializeField] private Vector3Int gridCoordinate;
        [SerializeField] private bool useCustomLocalPosition;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localEulerAngles;
        [SerializeField] private int surfaceShellIndex;

        /// <summary>
        /// Gets or sets the match identifier for the tile.
        /// </summary>
        public int MatchId
        {
            get => matchId;
            set => matchId = value;
        }

        /// <summary>
        /// Gets or sets the grid coordinate occupied by the tile.
        /// </summary>
        public Vector3Int GridCoordinate
        {
            get => gridCoordinate;
            set => gridCoordinate = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this tile should use a custom local-space position.
        /// </summary>
        public bool UseCustomLocalPosition
        {
            get => useCustomLocalPosition;
            set => useCustomLocalPosition = value;
        }

        /// <summary>
        /// Gets or sets the custom local-space position used when <see cref="UseCustomLocalPosition"/> is enabled.
        /// </summary>
        public Vector3 LocalPosition
        {
            get => localPosition;
            set => localPosition = value;
        }

        /// <summary>
        /// Gets or sets the local Euler rotation for the tile mesh.
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
        /// Gets or sets the runtime-only duplicated block index that owns this tile.
        /// </summary>
        public int RuntimeBlockIndex { get; set; }

        /// <summary>
        /// Gets or sets the runtime-only source match group copied from the authored level.
        /// Duplicated blocks may use distinct runtime match ids while still sharing this source group.
        /// </summary>
        public int RuntimeSourceMatchId { get; set; } = -1;
    }
}
