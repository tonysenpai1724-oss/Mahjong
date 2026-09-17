using MahjongOut3D.Gameplay;
using UnityEngine;

namespace MahjongOut3D.Data
{
    /// <summary>
    /// Stores project-wide tuning and bootstrap defaults for Mahjong Out 3D.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Data/Project Settings", fileName = "MahjongProjectSettings")]
    public sealed class MahjongProjectSettings : ScriptableObject
    {
        [field: Header("Runtime")]
        [field: SerializeField, Min(30)]
        public int TargetFrameRate { get; private set; } = 60;

        [field: SerializeField, Range(0, 4)]
        public int VSyncCount { get; private set; } = 0;

        [field: SerializeField]
        public bool EnableVerboseLogging { get; private set; }

        [field: Header("Flow")]
        [field: SerializeField]
        public GameFlowState InitialGameState { get; private set; } = GameFlowState.MainMenu;

        [field: SerializeField, Min(0)]
        public int DefaultLevelIndex { get; private set; }
    }
}
