using UnityEngine;

namespace MahjongOut3D.Data
{
    /// <summary>
    /// Stores thresholds and behavior flags used by the gameplay input system.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Data/Input Settings", fileName = "InputSettings")]
    public sealed class InputSettings : ScriptableObject
    {
        [field: Header("General")]
        [field: SerializeField]
        public bool UseUnscaledTime { get; private set; } = true;

        [field: SerializeField]
        public bool BlockInputWhenPointerOverUi { get; private set; } = true;

        [field: Header("Tap")]
        [field: SerializeField, Min(0.01f)]
        public float MaxTapDurationSeconds { get; private set; } = 0.25f;

        [field: SerializeField, Min(1f)]
        public float TapMoveThresholdPixels { get; private set; } = 18f;

        [field: Header("Drag")]
        [field: SerializeField, Min(1f)]
        public float DragStartThresholdPixels { get; private set; } = 10f;

        [field: Header("Pinch")]
        [field: SerializeField, Min(0.1f)]
        public float PinchDeltaThresholdPixels { get; private set; } = 0.5f;

        [field: Header("Editor Simulation")]
        [field: SerializeField]
        public bool EnableMouseSimulationInEditor { get; private set; } = true;
    }
}
