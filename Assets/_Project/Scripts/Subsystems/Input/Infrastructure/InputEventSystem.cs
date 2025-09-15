using System;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Core.Input.Events;
using Laboratory.Core.Events;

namespace Laboratory.Infrastructure.Input
{
    /// <summary>
    /// Manages input event dispatching for the unified input system.
    /// Converts raw inputs into structured events for the rest of the system.
    /// </summary>
    public class InputEventSystem
    {
        private readonly IEventBus _eventBus;
        
        public InputEventSystem(IEventBus eventBus)
        {
            _eventBus = eventBus; // Allow null for cases where no event bus is available
        }
        
        /// <summary>
        /// Clear all subscriptions and cleanup.
        /// </summary>
        public void ClearSubscriptions()
        {
            // Clear any internal subscriptions if needed
            // This method exists for compatibility with UnifiedInputManager
        }
        
        /// <summary>
        /// Dispatches a movement input event.
        /// </summary>
        public void DispatchMovementInput(float2 movement, float timestamp)
        {
            var movementEvent = new MovementInputEvent
            {
                Movement = movement,
                Timestamp = timestamp,
                Source = "UnifiedInputManager"
            };
            
            _eventBus?.Publish(movementEvent);
        }
        
        /// <summary>
        /// Dispatches a look input event.
        /// </summary>
        public void DispatchLookInput(float2 lookDelta, float timestamp)
        {
            var lookEvent = new LookInputEvent
            {
                LookDelta = lookDelta,
                Timestamp = timestamp,
                Source = "UnifiedInputManager"
            };
            
            _eventBus?.Publish(lookEvent);
        }
        
        /// <summary>
        /// Dispatches an action input event (button press/release).
        /// </summary>
        public void DispatchActionInput(string actionName, bool isPressed, float timestamp, float holdDuration = 0f)
        {
            var actionEvent = new ActionInputEvent
            {
                ActionName = actionName,
                IsPressed = isPressed,
                Timestamp = timestamp,
                HoldDuration = holdDuration,
                Source = "UnifiedInputManager"
            };
            
            _eventBus?.Publish(actionEvent);
        }
        
        /// <summary>
        /// Dispatches a scroll input event.
        /// </summary>
        public void DispatchScrollInput(float2 scrollDelta, float timestamp)
        {
            var scrollEvent = new ScrollInputEvent
            {
                ScrollDelta = scrollDelta,
                Timestamp = timestamp,
                Source = "UnifiedInputManager"
            };
            
            _eventBus?.Publish(scrollEvent);
        }
        
        /// <summary>
        /// Dispatches a raw input event for debugging or advanced processing.
        /// </summary>
        public void DispatchRawInput(string inputName, object inputValue, float timestamp)
        {
            var rawEvent = new RawInputEvent
            {
                InputName = inputName,
                InputValue = inputValue,
                Timestamp = timestamp,
                Source = "UnifiedInputManager"
            };
            
            _eventBus?.Publish(rawEvent);
        }
    }
}
