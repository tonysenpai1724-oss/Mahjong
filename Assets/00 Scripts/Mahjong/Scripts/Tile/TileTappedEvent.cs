using UnityEngine;

namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Published when the player taps a tile collider in the 3D scene.
    /// </summary>
    public readonly struct TileTappedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TileTappedEvent"/> struct.
        /// </summary>
        /// <param name="tile">Tile that was hit by the raycast.</param>
        /// <param name="screenPosition">Tap position in screen pixels.</param>
        /// <param name="ray">Camera ray used for hit testing.</param>
        /// <param name="hitInfo">Physics hit information.</param>
        public TileTappedEvent(MahjongTile tile, Vector2 screenPosition, Ray ray, RaycastHit hitInfo)
        {
            Tile = tile;
            ScreenPosition = screenPosition;
            Ray = ray;
            HitInfo = hitInfo;
        }

        /// <summary>
        /// Gets the tapped tile.
        /// </summary>
        public MahjongTile Tile { get; }

        /// <summary>
        /// Gets the tap position in screen pixels.
        /// </summary>
        public Vector2 ScreenPosition { get; }

        /// <summary>
        /// Gets the camera ray used for hit testing.
        /// </summary>
        public Ray Ray { get; }

        /// <summary>
        /// Gets the physics hit information.
        /// </summary>
        public RaycastHit HitInfo { get; }
    }
}
