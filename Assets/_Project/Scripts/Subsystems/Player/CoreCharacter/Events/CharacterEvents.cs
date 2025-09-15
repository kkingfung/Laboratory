using System;
using UnityEngine;

namespace Laboratory.Core.Character.Events
{
    /// <summary>
    /// Event fired when a target is detected by a target selector
    /// </summary>
    [System.Serializable]
    public class TargetDetectedEvent
    {
        public Transform Detector { get; }
        public Transform Target { get; }
        public float Distance { get; }
        public float Score { get; }
        public float Timestamp { get; }

        public TargetDetectedEvent(Transform detector, Transform target, float distance, float score)
        {
            Detector = detector;
            Target = target;
            Distance = distance;
            Score = score;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when a target is lost by a target selector
    /// </summary>
    [System.Serializable]
    public class TargetLostEvent
    {
        public Transform Detector { get; }
        public Transform Target { get; }
        public string Reason { get; }
        public float Timestamp { get; }

        public TargetLostEvent(Transform detector, Transform target, string reason)
        {
            Detector = detector;
            Target = target;
            Reason = reason;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when a target is selected by a target selector
    /// </summary>
    [System.Serializable]
    public class TargetSelectedEvent
    {
        public Transform Selector { get; }
        public Transform NewTarget { get; }
        public Transform PreviousTarget { get; }
        public float Distance { get; }
        public float Timestamp { get; }

        public TargetSelectedEvent(Transform selector, Transform newTarget, Transform previousTarget, float distance)
        {
            Selector = selector;
            NewTarget = newTarget;
            PreviousTarget = previousTarget;
            Distance = distance;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character state changes
    /// </summary>
    [System.Serializable]
    public class CharacterStateChangedEvent
    {
        public Transform Character { get; }
        public string PreviousState { get; }
        public string NewState { get; }
        public float Timestamp { get; }

        public CharacterStateChangedEvent(Transform character, string previousState, string newState)
        {
            Character = character;
            PreviousState = previousState;
            NewState = newState;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character movement state changes
    /// </summary>
    [System.Serializable]
    public class CharacterMovementEvent
    {
        public Transform Character { get; }
        public Vector3 Position { get; }
        public Vector3 Velocity { get; }
        public bool IsGrounded { get; }
        public float Timestamp { get; }

        public CharacterMovementEvent(Transform character, Vector3 position, Vector3 velocity, bool isGrounded)
        {
            Character = character;
            Position = position;
            Velocity = velocity;
            IsGrounded = isGrounded;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character performs an action
    /// </summary>
    [System.Serializable]
    public class CharacterActionEvent
    {
        public Transform Character { get; }
        public string ActionName { get; }
        public object ActionData { get; }
        public float Timestamp { get; }

        public CharacterActionEvent(Transform character, string actionName, object actionData = null)
        {
            Character = character;
            ActionName = actionName;
            ActionData = actionData;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character takes damage
    /// </summary>
    [System.Serializable]
    public class CharacterDamageEvent
    {
        public Transform Character { get; }
        public Transform DamageSource { get; }
        public float DamageAmount { get; }
        public string DamageType { get; }
        public Vector3 HitPoint { get; }
        public float Timestamp { get; }

        public CharacterDamageEvent(Transform character, Transform damageSource, float damageAmount, string damageType, Vector3 hitPoint)
        {
            Character = character;
            DamageSource = damageSource;
            DamageAmount = damageAmount;
            DamageType = damageType;
            HitPoint = hitPoint;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character dies
    /// </summary>
    [System.Serializable]
    public class CharacterDeathEvent
    {
        public Transform Character { get; }
        public Transform Killer { get; }
        public string DeathCause { get; }
        public Vector3 DeathPosition { get; }
        public float Timestamp { get; }

        public CharacterDeathEvent(Transform character, Transform killer, string deathCause, Vector3 deathPosition)
        {
            Character = character;
            Killer = killer;
            DeathCause = deathCause;
            DeathPosition = deathPosition;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character is revived
    /// </summary>
    [System.Serializable]
    public class CharacterReviveEvent
    {
        public Transform Character { get; }
        public Transform Reviver { get; }
        public Vector3 RevivePosition { get; }
        public float Timestamp { get; }

        public CharacterReviveEvent(Transform character, Transform reviver, Vector3 revivePosition)
        {
            Character = character;
            Reviver = reviver;
            RevivePosition = revivePosition;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character levels up or gains experience
    /// </summary>
    [System.Serializable]
    public class CharacterProgressEvent
    {
        public Transform Character { get; }
        public string ProgressType { get; }
        public float OldValue { get; }
        public float NewValue { get; }
        public float Timestamp { get; }

        public CharacterProgressEvent(Transform character, string progressType, float oldValue, float newValue)
        {
            Character = character;
            ProgressType = progressType;
            OldValue = oldValue;
            NewValue = newValue;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character interacts with objects
    /// </summary>
    [System.Serializable]
    public class CharacterInteractionEvent
    {
        public Transform Character { get; }
        public Transform InteractionTarget { get; }
        public string InteractionType { get; }
        public bool WasSuccessful { get; }
        public float Timestamp { get; }

        public CharacterInteractionEvent(Transform character, Transform interactionTarget, string interactionType, bool wasSuccessful)
        {
            Character = character;
            InteractionTarget = interactionTarget;
            InteractionType = interactionType;
            WasSuccessful = wasSuccessful;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character starts aiming at a target
    /// </summary>
    [System.Serializable]
    public class CharacterAimStartedEvent
    {
        public Transform Character { get; }
        public Transform Target { get; }
        public float Timestamp { get; }

        public CharacterAimStartedEvent(Transform character, Transform target)
        {
            Character = character;
            Target = target;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character stops aiming
    /// </summary>
    [System.Serializable]
    public class CharacterAimStoppedEvent
    {
        public Transform Character { get; }
        public Transform PreviousTarget { get; }
        public float Timestamp { get; }

        public CharacterAimStoppedEvent(Transform character, Transform previousTarget)
        {
            Character = character;
            PreviousTarget = previousTarget;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character's aim weight changes
    /// </summary>
    [System.Serializable]
    public class CharacterAimWeightChangedEvent
    {
        public Transform Character { get; }
        public float PreviousWeight { get; }
        public float NewWeight { get; }
        public float Timestamp { get; }

        public CharacterAimWeightChangedEvent(Transform character, float previousWeight, float newWeight)
        {
            Character = character;
            PreviousWeight = previousWeight;
            NewWeight = newWeight;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when character's climbing state changes
    /// </summary>
    [System.Serializable]
    public class CharacterClimbStateChangedEvent
    {
        public Transform Character { get; }
        public bool IsClimbing { get; }
        public Transform ClimbTarget { get; }
        public float Timestamp { get; }

        public CharacterClimbStateChangedEvent(Transform character, bool isClimbing, Transform climbTarget = null)
        {
            Character = character;
            IsClimbing = isClimbing;
            ClimbTarget = climbTarget;
            Timestamp = Time.time;
        }
    }
}
