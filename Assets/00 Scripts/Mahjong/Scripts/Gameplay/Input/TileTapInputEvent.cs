using UnityEngine;

namespace MahjongOut3D.GameplayInput
{
    /// <summary>
    /// Published when the player performs a valid gameplay tap.
    /// </summary>
    public readonly struct TileTapInputEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TileTapInputEvent"/> struct.
        /// </summary>
        /// <param name="screenPosition">Tap position in screen pixels.</param>
        /// <param name="pointerId">Pointer identifier.</param>
        public TileTapInputEvent(Vector2 screenPosition, int pointerId)
        {
            ScreenPosition = screenPosition;
            PointerId = pointerId;
        }

        /// <summary>
        /// Gets the tap position in screen pixels.
        /// </summary>
        public Vector2 ScreenPosition { get; }

        /// <summary>
        /// Gets the originating pointer identifier.
        /// </summary>
        public int PointerId { get; }
    }
}
