using System;

namespace MahjongOut3D.Core
{
    /// <summary>
    /// Published after a runtime manager finishes initialization.
    /// </summary>
    public readonly struct ManagerInitializedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManagerInitializedEvent"/> struct.
        /// </summary>
        /// <param name="managerType">Concrete manager type that finished initializing.</param>
        public ManagerInitializedEvent(Type managerType)
        {
            ManagerType = managerType;
        }

        /// <summary>
        /// Gets the concrete manager type that finished initializing.
        /// </summary>
        public Type ManagerType { get; }
    }
}
