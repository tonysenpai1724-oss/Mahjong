using System;

namespace MahjongOut3D.Core
{
    /// <summary>
    /// Provides a lightweight shared service container for runtime systems.
    /// </summary>
    public interface IServiceRegistry
    {
        /// <summary>
        /// Registers a service instance under the specified service type.
        /// </summary>
        /// <param name="serviceType">Service contract type.</param>
        /// <param name="service">Service instance.</param>
        /// <returns>True when registration succeeds; otherwise false.</returns>
        bool Register(Type serviceType, object service);

        /// <summary>
        /// Unregisters a previously stored service type.
        /// </summary>
        /// <param name="serviceType">Service contract type.</param>
        /// <returns>True when the service was removed; otherwise false.</returns>
        bool Unregister(Type serviceType);

        /// <summary>
        /// Tries to resolve a service by type.
        /// </summary>
        /// <typeparam name="TService">Service contract type.</typeparam>
        /// <param name="service">Resolved instance when available.</param>
        /// <returns>True when the service exists; otherwise false.</returns>
        bool TryGet<TService>(out TService service) where TService : class;

        /// <summary>
        /// Resolves a service by type and throws when it does not exist.
        /// </summary>
        /// <typeparam name="TService">Service contract type.</typeparam>
        /// <returns>Resolved service instance.</returns>
        TService Get<TService>() where TService : class;
    }
}
