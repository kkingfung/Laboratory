using UnityEngine;
using Laboratory.Core.Character;

namespace Laboratory.Core.Character.Events
{
    /// <summary>
    /// Base class for character-related events
    /// </summary>
    public abstract class CharacterEventBase
    {
        /// <summary>
        /// The character this event relates to
        /// </summary>
        public Transform Character { get; }

        /// <summary>
        /// Timestamp when the event occurred
        /// </summary>
        public float Timestamp { get; }

        protected CharacterEventBase(Transform character)
        {
            Character = character;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character starts aiming at a target
    /// </summary>
    public class CharacterAimStartedEvent : CharacterEventBase
    {
        /// <summary>
        /// The target being aimed at
        /// </summary>
        public Transform Target { get; }

        public CharacterAimStartedEvent(Transform character, Transform target) : base(character)
        {
            Target = target;
        }
    }

    /// <summary>
    /// Event fired when character stops aiming
    /// </summary>
    public class CharacterAimStoppedEvent : CharacterEventBase
    {
        /// <summary>
        /// The target that was being aimed at
        /// </summary>
        public Transform PreviousTarget { get; }

        public CharacterAimStoppedEvent(Transform character, Transform previousTarget) : base(character)
        {
            PreviousTarget = previousTarget;
        }
    }

    /// <summary>
    /// Event fired when character's aim weight changes significantly
    /// </summary>
    public class CharacterAimWeightChangedEvent : CharacterEventBase
    {
        /// <summary>
        /// Previous aim weight
        /// </summary>
        public float PreviousWeight { get; }

        /// <summary>
        /// New aim weight
        /// </summary>
        public float NewWeight { get; }

        public CharacterAimWeightChangedEvent(Transform character, float previousWeight, float newWeight) : base(character)
        {
            PreviousWeight = previousWeight;
            NewWeight = newWeight;
        }
    }

    /// <summary>
    /// Event fired when character state changes
    /// </summary>
    public class CharacterStateChangedEvent : CharacterEventBase
    {
        /// <summary>
        /// Previous character state
        /// </summary>
        public CharacterState PreviousState { get; }

        /// <summary>
        /// New character state
        /// </summary>
        public CharacterState NewState { get; }

        /// <summary>
        /// Time spent in previous state
        /// </summary>
        public float TimeInPreviousState { get; }

        public CharacterStateChangedEvent(Transform character, CharacterState previousState, CharacterState newState, float timeInPreviousState) : base(character)
        {
            PreviousState = previousState;
            NewState = newState;
            TimeInPreviousState = timeInPreviousState;
        }
    }

    /// <summary>
    /// Event fired when character customization changes
    /// </summary>
    public class CharacterCustomizationChangedEvent : CharacterEventBase
    {
        /// <summary>
        /// The customization category that changed
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Previous customization value
        /// </summary>
        public string PreviousValue { get; }

        /// <summary>
        /// New customization value
        /// </summary>
        public string NewValue { get; }

        public CharacterCustomizationChangedEvent(Transform character, string category, string previousValue, string newValue) : base(character)
        {
            Category = category;
            PreviousValue = previousValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// Event fired when character's climb state changes
    /// </summary>
    public class CharacterClimbStateChangedEvent : CharacterEventBase
    {
        /// <summary>
        /// New climb state
        /// </summary>
        public ClimbState ClimbState { get; }

        /// <summary>
        /// Whether climbing is now active
        /// </summary>
        public bool IsClimbing { get; }

        public CharacterClimbStateChangedEvent(Transform character, ClimbState climbState, bool isClimbing) : base(character)
        {
            ClimbState = climbState;
            IsClimbing = isClimbing;
        }
    }

    /// <summary>
    /// Event fired when a target is selected for aiming
    /// </summary>
    public class TargetSelectedEvent : CharacterEventBase
    {
        /// <summary>
        /// The newly selected target
        /// </summary>
        public Transform NewTarget { get; }

        /// <summary>
        /// The previously selected target
        /// </summary>
        public Transform PreviousTarget { get; }

        /// <summary>
        /// Distance to the new target
        /// </summary>
        public float DistanceToTarget { get; }

        public TargetSelectedEvent(Transform character, Transform newTarget, Transform previousTarget, float distanceToTarget) : base(character)
        {
            NewTarget = newTarget;
            PreviousTarget = previousTarget;
            DistanceToTarget = distanceToTarget;
        }
    }

    /// <summary>
    /// Event fired when a target is detected
    /// </summary>
    public class TargetDetectedEvent : CharacterEventBase
    {
        /// <summary>
        /// The detected target
        /// </summary>
        public Transform Target { get; }

        /// <summary>
        /// Distance to the detected target
        /// </summary>
        public float Distance { get; }

        /// <summary>
        /// Priority score of the target
        /// </summary>
        public float Score { get; }

        public TargetDetectedEvent(Transform character, Transform target, float distance, float score) : base(character)
        {
            Target = target;
            Distance = distance;
            Score = score;
        }
    }

    /// <summary>
    /// Event fired when a target is lost
    /// </summary>
    public class TargetLostEvent : CharacterEventBase
    {
        /// <summary>
        /// The lost target
        /// </summary>
        public Transform Target { get; }

        /// <summary>
        /// Reason the target was lost
        /// </summary>
        public string Reason { get; }

        public TargetLostEvent(Transform character, Transform target, string reason) : base(character)
        {
            Target = target;
            Reason = reason;
        }
    }
}
