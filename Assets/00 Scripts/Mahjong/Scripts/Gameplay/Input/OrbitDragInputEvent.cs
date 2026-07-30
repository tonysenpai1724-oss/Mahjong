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
        public OrbitDragInputEvent(Vector2 screenDelta)
        {
            ScreenDelta = screenDelta;
        }

        /// <summary>
        /// Gets the pointer movement delta in screen pixels.
        /// </summary>
        public Vector2 ScreenDelta { get; }
    }
}
