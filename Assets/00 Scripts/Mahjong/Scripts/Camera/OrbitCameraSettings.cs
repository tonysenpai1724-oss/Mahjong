using UnityEngine;

namespace MahjongOut3D.CameraSystem
{
    /// <summary>
    /// Holds tuning values for the orbit camera that will be implemented in Step 3.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Camera/Orbit Camera Settings", fileName = "OrbitCameraSettings")]
    public sealed class OrbitCameraSettings : ScriptableObject
    {
        [field: Header("Distance")]
        [field: SerializeField, Min(1f)]
        public float DefaultZoomDistance { get; private set; } = 10f;

        [field: SerializeField, Min(0.1f)]
        public float RotationSpeed { get; private set; } = 0.2f;

        [field: SerializeField, Min(0.1f)]
        public float VerticalRotationMultiplier { get; private set; } = 1.5f;

        [field: SerializeField, Min(0.1f)]
        public float ZoomSpeed { get; private set; } = 3f;

        [field: SerializeField, Min(0.001f)]
        public float ZoomInputScale { get; private set; } = 0.02f;

        [field: SerializeField, Min(1f)]
        public float MinZoomDistance { get; private set; } = 6f;

        [field: SerializeField, Min(1f)]
        public float MaxZoomDistance { get; private set; } = 24f;

        [field: Header("Angles")]
        [field: SerializeField, Range(-89f, 89f)]
        public float DefaultPitch { get; private set; } = 25f;

        [field: SerializeField, Range(-89f, 89f)]
        public float MinPitch { get; private set; } = -15f;

        [field: SerializeField, Range(-89f, 89f)]
        public float MaxPitch { get; private set; } = 70f;

        [field: Header("Smoothing")]
        [field: SerializeField, Range(0f, 1f)]
        public float RotationInertia { get; private set; } = 0.88f;

        [field: SerializeField, Min(0.01f)]
        public float RotationSmoothing { get; private set; } = 0.08f;

        [field: SerializeField, Range(0f, 1f)]
        public float ZoomSmoothing { get; private set; } = 0.2f;

        [field: SerializeField, Min(0.01f)]
        public float FocusSmoothing { get; private set; } = 0.12f;

        [field: Header("Accessibility")]
        [field: SerializeField]
        public bool UseUnscaledTime { get; private set; } = true;
    }
}
