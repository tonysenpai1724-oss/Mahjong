using System;

namespace MahjongOut3D.GameplayInput
{
    /// <summary>
    /// Defines the contract for raw gameplay input providers.
    /// </summary>
    public interface IInputSource
    {
        /// <summary>
        /// Occurs when the player taps a visible gameplay target.
        /// </summary>
        event Action<TileTapInputEvent> TileTapped;

        /// <summary>
        /// Occurs when the player drags to rotate the orbit camera.
        /// </summary>
        event Action<OrbitDragInputEvent> OrbitDragged;

        /// <summary>
        /// Occurs when the player pinches or scrolls to zoom the orbit camera.
        /// </summary>
        event Action<ZoomInputEvent> ZoomChanged;

        /// <summary>
        /// Occurs when the player begins any pointer or touch interaction.
        /// </summary>
        event Action<ScreenActivityInputEvent> ScreenActivity;

        /// <summary>
        /// Enables or disables raw input polling.
        /// </summary>
        /// <param name="isEnabled">True to enable polling; otherwise false.</param>
        void SetInputEnabled(bool isEnabled);
    }
}
