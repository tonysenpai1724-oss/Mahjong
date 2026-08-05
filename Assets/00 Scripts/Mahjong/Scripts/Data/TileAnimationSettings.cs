using UnityEngine;

namespace MahjongOut3D.Data
{
    /// <summary>
    /// Stores timing and feedback tuning for tile animations and camera shake.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Data/Tile Animation Settings", fileName = "TileAnimationSettings")]
    public sealed class TileAnimationSettings : ScriptableObject
    {
        [field: Header("Match")]
        [field: SerializeField, Min(0.05f)]
        public float MatchDurationSeconds { get; private set; } = 0.35f;

        [field: SerializeField, Min(0.1f)]
        public float MatchSlideDistance { get; private set; } = 1.25f;

        [field: SerializeField, Min(0f)]
        public float MatchRotationDegrees { get; private set; } = 55f;

        [field: SerializeField, Min(0.05f)]
        public float HintDurationSeconds { get; private set; } = 0.7f;

        [field: SerializeField, Min(0.05f)]
        public float MismatchDelaySeconds { get; private set; } = 0.2f;

        [field: Header("Tray")]
        [field: SerializeField, Min(0.05f)]
        public float TrayMoveDurationSeconds { get; private set; } = 0.22f;

        [field: SerializeField, Range(0.1f, 0.95f)]
        public float TrayViewportY { get; private set; } = 0.84f;

        [field: SerializeField, Range(0.02f, 0.3f)]
        public float TrayViewportSlotSpacing { get; private set; } = 0.15f;

        [field: SerializeField, Min(0.5f)]
        public float TrayDistanceFromCamera { get; private set; } = 8f;

        [field: SerializeField, Min(0f)]
        public float TrayDistancePadding { get; private set; } = 0.75f;

        [field: Header("Camera Shake")]
        [field: SerializeField, Min(0f)]
        public float ShakeDurationSeconds { get; private set; } = 0.18f;

        [field: SerializeField, Min(0f)]
        public float ShakeAmplitude { get; private set; } = 0.12f;

        [field: Header("FX")]
        [field: SerializeField]
        public ParticleSystem MatchParticlePrefab { get; private set; }

        [field: SerializeField]
        public bool UseUnscaledTime { get; private set; } = true;
    }
}
