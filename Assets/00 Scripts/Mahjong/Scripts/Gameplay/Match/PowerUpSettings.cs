using UnityEngine;

namespace MahjongOut3D.Gameplay
{
    /// <summary>
    /// Stores reward and power-up tuning used by match resolution.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Gameplay/Power Up Settings", fileName = "PowerUpSettings")]
    public sealed class PowerUpSettings : ScriptableObject
    {
        [field: Header("Economy")]
        [field: SerializeField, Min(0)]
        public int CoinsPerMatch { get; private set; } = 2;

        [field: SerializeField, Min(0)]
        public int CoinsPerLevelWin { get; private set; } = 30;

        [field: Header("Costs")]
        [field: SerializeField, Min(0)]
        public int HintCost { get; private set; } = 10;

        [field: SerializeField, Min(0)]
        public int UndoCost { get; private set; } = 15;

        [field: SerializeField, Min(0)]
        public int ShuffleCost { get; private set; } = 20;

        [field: SerializeField, Min(0)]
        public int BombCost { get; private set; } = 25;

        [field: SerializeField, Min(0)]
        public int XRayCost { get; private set; } = 18;

        [field: Header("Timing")]
        [field: SerializeField, Min(0.01f)]
        public float MismatchDelaySeconds { get; private set; } = 0.25f;

        [field: SerializeField, Min(0.01f)]
        public float HintHighlightDurationSeconds { get; private set; } = 0.75f;
    }
}
