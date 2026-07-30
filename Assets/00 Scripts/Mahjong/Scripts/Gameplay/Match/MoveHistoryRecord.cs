using System;
using System.Collections.Generic;

namespace MahjongOut3D.Gameplay
{
    /// <summary>
    /// Captures a reversible gameplay operation such as match removal or shuffle.
    /// </summary>
    [Serializable]
    public sealed class MoveHistoryRecord
    {
        public string actionName;
        public List<TileStateSnapshot> snapshots = new List<TileStateSnapshot>();
    }
}
