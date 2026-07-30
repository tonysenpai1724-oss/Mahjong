using UnityEngine;

namespace MahjongOut3D.Core
{
    /// <summary>
    /// Base behaviour for every manager owned by the Mahjong runtime bootstrapper.
    /// </summary>
    public abstract class ManagerBehaviour : MonoBehaviour, IManager
    {
        /// <summary>
        /// Gets a value indicating whether the manager has completed initialization.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets the initialization order used by the bootstrapper.
        /// </summary>
        public virtual int InitializationOrder => 0;

        /// <summary>
        /// Gets the shared runtime context.
        /// </summary>
        protected GameContext Context { get; private set; }

        /// <summary>
        /// Initializes the manager and registers it into the shared service registry.
        /// </summary>
        /// <param name="context">Shared runtime context.</param>
        public void Initialize(GameContext context)
        {
            if (IsInitialized)
            {
                return;
            }

            Context = context;
            Context.Services.Register(GetType(), this);
            OnInitialize();
            IsInitialized = true;
        }

        /// <summary>
        /// Shuts the manager down and removes it from the shared registry.
        /// </summary>
        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            OnShutdown();
            Context.Services.Unregister(GetType());
            Context = null;
            IsInitialized = false;
        }

        /// <summary>
        /// Executes manager-specific initialization logic.
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// Executes manager-specific shutdown logic.
        /// </summary>
        protected virtual void OnShutdown()
        {
        }
    }
}
