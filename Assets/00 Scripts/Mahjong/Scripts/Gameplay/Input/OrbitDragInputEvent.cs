using UnityEngine;

namespace MahjongOut3D.GameplayInput
{
    /// <summary>
    /// Published when the player drags to rotate the orbit camera.
    /// </summary>
    public readonly struct OrbitDragInputEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitDragInputEvent"/> struct.
        /// </summary>
        /// <param name="screenDelta">Pointer movement delta in screen pixels.</param>
        /// <param name="screenPosition">Current pointer position in screen pixels.</param>
        public OrbitDragInputEvent(Vector2 screenDelta, Vector2 screenPosition)
        {
            ScreenDelta = screenDelta;
            ScreenPosition = screenPosition;
        }

        /// <summary>
        /// Gets the pointer movement delta in screen pixels.
        /// </summary>
        public Vector2 ScreenDelta { get; }

        /// <summary>
        /// Gets the current pointer position in screen pixels.
        /// </summary>
        public Vector2 ScreenPosition { get; }
    }
}
