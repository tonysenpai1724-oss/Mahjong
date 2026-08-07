using System;
using MahjongOut3D.TileSystem;
using UnityEngine;

namespace MahjongOut3D.Gameplay
{
    /// <summary>
    /// Captures enough tile state to restore it later through Undo.
    /// </summary>
    [Serializable]
    public sealed class TileStateSnapshot
    {
        public int tileId;
        public int matchId;
        public Vector3Int gridCoordinate;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public TileState state;
        public bool isBufferedSelection;
        public Texture2D fillTexture;
    }
}
