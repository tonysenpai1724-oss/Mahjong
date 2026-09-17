using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.Utilities
{
    /// <summary>
    /// Provides a lightweight reusable component pool for frequently spawned runtime objects.
    /// </summary>
    /// <typeparam name="TComponent">Pooled component type.</typeparam>
    public sealed class ComponentPool<TComponent> where TComponent : Component
    {
        private readonly TComponent prefab;
        private readonly Stack<TComponent> availableComponents = new Stack<TComponent>();

        /// <summary>
        /// Gets the number of currently inactive pooled instances ready for reuse.
        /// </summary>
        public int AvailableCount => availableComponents.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentPool{TComponent}"/> class.
        /// </summary>
        /// <param name="prefab">Prefab used when the pool is empty.</param>
        public ComponentPool(TComponent prefab)
        {
            this.prefab = prefab;
        }

        /// <summary>
        /// Gets an instance from the pool or instantiates a new one when needed.
        /// </summary>
        /// <param name="parent">Optional parent transform.</param>
        /// <returns>Pooled component instance.</returns>
        public TComponent Get(Transform parent = null)
        {
            TComponent instance = availableComponents.Count > 0 ? availableComponents.Pop() : Object.Instantiate(prefab, parent);
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            instance.gameObject.SetActive(true);
            return instance;
        }

        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        /// <param name="instance">Instance to release.</param>
        /// <param name="parent">Optional parent transform.</param>
        public void Release(TComponent instance, Transform parent = null)
        {
            if (instance == null)
            {
                return;
            }

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            instance.gameObject.SetActive(false);
            availableComponents.Push(instance);
        }

        /// <summary>
        /// Destroys every currently pooled instance.
        /// </summary>
        public void Clear()
        {
            while (availableComponents.Count > 0)
            {
                TComponent instance = availableComponents.Pop();
                if (instance != null)
                {
                    Object.Destroy(instance.gameObject);
                }
            }
        }
    }
}
