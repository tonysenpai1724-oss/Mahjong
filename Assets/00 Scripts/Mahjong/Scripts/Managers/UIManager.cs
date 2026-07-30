using MahjongOut3D.Core;
using MahjongOut3D.UI;

namespace MahjongOut3D.Managers
{
    /// <summary>
    /// Tracks the active UI screen and later will orchestrate panels and transitions.
    /// </summary>
    public sealed class UIManager : ManagerBehaviour
    {
        /// <summary>
        /// Gets the currently active UI screen.
        /// </summary>
        public UIScreenType CurrentScreen { get; private set; } = UIScreenType.None;

        /// <summary>
        /// Gets the bootstrap order for the UI manager.
        /// </summary>
        public override int InitializationOrder => 50;

        /// <summary>
        /// Displays the specified screen type.
        /// </summary>
        /// <param name="screenType">Screen to display.</param>
        public void ShowScreen(UIScreenType screenType)
        {
            CurrentScreen = screenType;
        }
    }
}
