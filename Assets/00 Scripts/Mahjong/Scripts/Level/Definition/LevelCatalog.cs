using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Stores an ordered list of playable level definitions for runtime selection.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Level/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [field: SerializeField]
        public List<LevelDefinition> Levels { get; private set; } = new List<LevelDefinition>();

        /// <summary>
        /// Tries to get a level definition by index.
        /// </summary>
        /// <param name="levelIndex">Zero-based level index.</param>
        /// <param name="definition">Resolved level definition.</param>
        /// <returns>True when the level exists; otherwise false.</returns>
        public bool TryGetLevel(int levelIndex, out LevelDefinition definition)
        {
            definition = null;
            if (Levels == null || levelIndex < 0 || levelIndex >= Levels.Count)
            {
                return false;
            }

            definition = Levels[levelIndex];
            return definition != null;
        }
    }
}
