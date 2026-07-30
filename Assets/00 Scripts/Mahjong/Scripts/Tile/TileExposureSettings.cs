using UnityEngine;

namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Stores the rules used to determine whether a tile is exposed and selectable.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Tile/Tile Exposure Settings", fileName = "TileExposureSettings")]
    public sealed class TileExposureSettings : ScriptableObject
    {
        [field: Header("Surface Rules")]
        [field: SerializeField]
        public bool RequireSurfaceExposure { get; private set; } = true;

        [field: Header("Visibility Rules")]
        [field: SerializeField]
        public bool RequireDirectCameraVisibility { get; private set; } = true;

        [field: SerializeField, Range(0.1f, 1f)]
        public float RequiredVisibleSampleRatio { get; private set; } = 0.9f;

        [field: SerializeField, Min(0.001f)]
        public float VisibilitySampleInset { get; private set; } = 0.01f;

        [field: SerializeField, Min(0.01f)]
        public float VisibilityRayPadding { get; private set; } = 0.05f;

        [field: Header("X-Ray")]
        [field: SerializeField, Min(1)]
        public int XRayRevealDepth { get; private set; } = 1;

        [field: SerializeField, Min(0.1f)]
        public float XRayDurationSeconds { get; private set; } = 5f;

        [field: Header("Time")]
        [field: SerializeField]
        public bool UseUnscaledTime { get; private set; } = true;
    }
}
