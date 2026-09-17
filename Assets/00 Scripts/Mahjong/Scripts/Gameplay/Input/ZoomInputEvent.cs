namespace MahjongOut3D.GameplayInput
{
    /// <summary>
    /// Published when the player zooms using pinch or mouse wheel input.
    /// </summary>
    public readonly struct ZoomInputEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomInputEvent"/> struct.
        /// </summary>
        /// <param name="delta">Signed zoom delta.</param>
        public ZoomInputEvent(float delta)
        {
            Delta = delta;
        }

        /// <summary>
        /// Gets the signed zoom delta.
        /// </summary>
        public float Delta { get; }
    }
}
