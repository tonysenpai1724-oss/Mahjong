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

        [field: SerializeField, Range(0f, 0.2f)]
        public float MatchViewportYOffset { get; private set; } = 0.05f;

        [field: SerializeField, Min(0f)]
        public float MatchDepthOffset { get; private set; } = 1.25f;

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
        public bool UseMatchUiShards { get; private set; } = true;

        [field: SerializeField, Range(1, 12)]
        public int MatchUiShardRows { get; private set; } = 10;

        [field: SerializeField, Range(1, 20)]
        public int MatchUiShardColumns { get; private set; } = 12;

        [field: SerializeField, Min(0.1f)]
        public float MatchUiShardScale { get; private set; } = 0.95f;

        [field: SerializeField, Min(0.1f)]
        public float MatchUiLifetimeSeconds { get; private set; } = 1.15f;

        [field: SerializeField, Min(0.01f)]
        public float MatchUiEmissionDurationSeconds { get; private set; } = 0.18f;

        [field: SerializeField, Min(0f)]
        public float MatchUiBurstSpeedMin { get; private set; } = 240f;

        [field: SerializeField, Min(0f)]
        public float MatchUiBurstSpeedMax { get; private set; } = 360f;

        [field: SerializeField, Min(0f)]
        public float MatchUiUpwardSpeedMin { get; private set; } = 320f;

        [field: SerializeField, Min(0f)]
        public float MatchUiUpwardSpeedMax { get; private set; } = 500f;

        [field: SerializeField, Min(0f)]
        public float MatchUiGravityDelaySeconds { get; private set; } = 0.055f;

        [field: SerializeField, Min(0f)]
        public float MatchUiGravity { get; private set; } = 2400f;

        [field: SerializeField, Min(0f)]
        public float MatchUiSpinSpeedMin { get; private set; } = 160f;

        [field: SerializeField, Min(0f)]
        public float MatchUiSpinSpeedMax { get; private set; } = 520f;

        [field: SerializeField, Range(0f, 1f)]
        public float MatchUiFadeStartNormalized { get; private set; } = 0.78f;

        [field: SerializeField]
        public Sprite[] MatchUiShardSprites { get; private set; }

        [field: SerializeField]
        public ParticleSystem MatchParticlePrefab { get; private set; }

        [field: SerializeField]
        public bool UseUnscaledTime { get; private set; } = true;
    }
}
