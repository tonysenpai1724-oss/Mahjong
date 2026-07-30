using System;
using System.Collections.Generic;

namespace MahjongOut3D.Core
{
    /// <summary>
    /// Stores and resolves runtime services used by the active game session.
    /// </summary>
    public sealed class ServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        /// <summary>
        /// Registers a service instance under the specified type.
        /// </summary>
        /// <param name="serviceType">Service contract type.</param>
        /// <param name="service">Service instance.</param>
        /// <returns>True when registration succeeds; otherwise false.</returns>
        public bool Register(Type serviceType, object service)
        {
            if (serviceType == null || service == null)
            {
                return false;
            }

            services[serviceType] = service;
            return true;
        }

        /// <summary>
        /// Unregisters a stored service type.
        /// </summary>
        /// <param name="serviceType">Service contract type.</param>
        /// <returns>True when the service existed and was removed.</returns>
        public bool Unregister(Type serviceType)
        {
            if (serviceType == null)
            {
                return false;
            }

            return services.Remove(serviceType);
        }

        /// <summary>
        /// Tries to resolve a service by type.
        /// </summary>
        /// <typeparam name="TService">Service contract type.</typeparam>
        /// <param name="service">Resolved service instance.</param>
        /// <returns>True when the service exists; otherwise false.</returns>
        public bool TryGet<TService>(out TService service) where TService : class
        {
            if (services.TryGetValue(typeof(TService), out object instance) && instance is TService typedService)
            {
                service = typedService;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Resolves a service by type.
        /// </summary>
        /// <typeparam name="TService">Service contract type.</typeparam>
        /// <returns>Resolved service instance.</returns>
        public TService Get<TService>() where TService : class
        {
            if (TryGet(out TService service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service of type {typeof(TService).Name} has not been registered.");
        }
    }
}
