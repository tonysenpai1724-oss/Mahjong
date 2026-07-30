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
        [SerializeField] private Vector3 localEulerAngles;

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
        /// Gets or sets the local Euler rotation for the tile mesh.
        /// </summary>
        public Vector3 LocalEulerAngles
        {
            get => localEulerAngles;
            set => localEulerAngles = value;
        }
    }
}
