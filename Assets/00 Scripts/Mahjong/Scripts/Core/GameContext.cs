using MahjongOut3D.Data;

namespace MahjongOut3D.Core
{
    /// <summary>
    /// Aggregates shared runtime dependencies for the active gameplay session.
    /// </summary>
    public sealed class GameContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameContext"/> class.
        /// </summary>
        /// <param name="projectSettings">Project-wide runtime settings.</param>
        /// <param name="services">Shared service registry.</param>
        /// <param name="eventBus">Shared event bus.</param>
        public GameContext(MahjongProjectSettings projectSettings, IServiceRegistry services, EventBus eventBus)
        {
            ProjectSettings = projectSettings;
            Services = services;
            EventBus = eventBus;
        }

        /// <summary>
        /// Gets the project-wide runtime settings.
        /// </summary>
        public MahjongProjectSettings ProjectSettings { get; }

        /// <summary>
        /// Gets the shared service registry.
        /// </summary>
        public IServiceRegistry Services { get; }

        /// <summary>
        /// Gets the shared event bus used for low-coupling communication.
        /// </summary>
        public EventBus EventBus { get; }
    }
}
