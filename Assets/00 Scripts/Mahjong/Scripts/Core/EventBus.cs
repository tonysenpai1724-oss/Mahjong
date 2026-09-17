using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.Core
{
    /// <summary>
    /// Dispatches strongly typed runtime events without tight coupling between systems.
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, Delegate> subscribers = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Subscribes a callback to a strongly typed event.
        /// </summary>
        /// <typeparam name="TEvent">Event payload type.</typeparam>
        /// <param name="callback">Callback invoked when the event is published.</param>
        public void Subscribe<TEvent>(Action<TEvent> callback)
        {
            if (callback == null)
            {
                return;
            }

            Type eventType = typeof(TEvent);
            if (subscribers.TryGetValue(eventType, out Delegate existingDelegate))
            {
                subscribers[eventType] = Delegate.Combine(existingDelegate, callback);
                return;
            }

            subscribers.Add(eventType, callback);
        }

        /// <summary>
        /// Removes a previously registered callback.
        /// </summary>
        /// <typeparam name="TEvent">Event payload type.</typeparam>
        /// <param name="callback">Callback to remove.</param>
        public void Unsubscribe<TEvent>(Action<TEvent> callback)
        {
            if (callback == null)
            {
                return;
            }

            Type eventType = typeof(TEvent);
            if (!subscribers.TryGetValue(eventType, out Delegate existingDelegate))
            {
                return;
            }

            Delegate updatedDelegate = Delegate.Remove(existingDelegate, callback);
            if (updatedDelegate == null)
            {
                subscribers.Remove(eventType);
                return;
            }

            subscribers[eventType] = updatedDelegate;
        }

        /// <summary>
        /// Publishes a strongly typed event to all active subscribers.
        /// </summary>
        /// <typeparam name="TEvent">Event payload type.</typeparam>
        /// <param name="eventData">Event payload.</param>
        public void Publish<TEvent>(TEvent eventData)
        {
            Type eventType = typeof(TEvent);
            if (!subscribers.TryGetValue(eventType, out Delegate existingDelegate))
            {
                return;
            }

            Delegate[] invocationList = existingDelegate.GetInvocationList();
            for (int index = 0; index < invocationList.Length; index++)
            {
                try
                {
                    Action<TEvent> action = (Action<TEvent>)invocationList[index];
                    action.Invoke(eventData);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        /// <summary>
        /// Removes every registered subscriber from the bus.
        /// </summary>
        public void Clear()
        {
            subscribers.Clear();
        }
    }
}
