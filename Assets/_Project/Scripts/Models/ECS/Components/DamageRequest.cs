using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Core.Health;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// ECS component representing a damage request that needs to be processed.
    /// This component is added to entities that should take damage and is consumed
    /// by the DamageSystem during processing.
    /// </summary>
    public struct DamageRequest : IComponentData
    {
        #region Public Fields

        /// <summary>Amount of damage to apply</summary>
        public float Amount;
        
        /// <summary>Type of damage being applied</summary>
        public DamageType Type;
        
        /// <summary>Direction of the damage impact for physics effects</summary>
        public float3 Direction;
        
        /// <summary>Position where the damage originated</summary>
        public float3 SourcePosition;
        
        /// <summary>Whether this damage can be blocked or mitigated</summary>
        public bool CanBeBlocked;
        
        /// <summary>Whether this damage should trigger invulnerability frames</summary>
        public bool TriggerInvulnerability;
        
        /// <summary>Network ID of the entity that caused the damage</summary>
        public ulong SourceEntityId;
        
        /// <summary>Time when the damage request was created</summary>
        public float TimeStamp;

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a basic damage request with specified amount.
        /// </summary>
        /// <param name="amount">Damage amount</param>
        /// <param name="currentTime">Current game time</param>
        /// <returns>New damage request</returns>
        public static DamageRequest Create(float amount, float currentTime)
        {
            return new DamageRequest
            {
                Amount = amount,
                Type = DamageType.Normal,
                Direction = float3.zero,
                SourcePosition = float3.zero,
                CanBeBlocked = true,
                TriggerInvulnerability = true,
                SourceEntityId = 0,
                TimeStamp = currentTime
            };
        }

        /// <summary>
        /// Creates a damage request with full parameters.
        /// </summary>
        /// <param name="amount">Damage amount</param>
        /// <param name="damageType">Type of damage</param>
        /// <param name="direction">Impact direction</param>
        /// <param name="sourcePosition">Source position</param>
        /// <param name="sourceEntityId">Source entity network ID</param>
        /// <param name="currentTime">Current game time</param>
        /// <returns>New damage request</returns>
        public static DamageRequest Create(float amount, DamageType damageType, float3 direction, 
                                         float3 sourcePosition, ulong sourceEntityId, float currentTime)
        {
            return new DamageRequest
            {
                Amount = amount,
                Type = damageType,
                Direction = direction,
                SourcePosition = sourcePosition,
                CanBeBlocked = true,
                TriggerInvulnerability = true,
                SourceEntityId = sourceEntityId,
                TimeStamp = currentTime
            };
        }

        /// <summary>
        /// Creates an unblockable damage request (for environmental hazards, etc.).
        /// </summary>
        /// <param name="amount">Damage amount</param>
        /// <param name="currentTime">Current game time</param>
        /// <returns>New unblockable damage request</returns>
        public static DamageRequest CreateUnblockable(float amount, float currentTime)
        {
            return new DamageRequest
            {
                Amount = amount,
                Type = DamageType.Environmental,
                Direction = float3.zero,
                SourcePosition = float3.zero,
                CanBeBlocked = false,
                TriggerInvulnerability = false,
                SourceEntityId = 0,
                TimeStamp = currentTime
            };
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether this damage request is valid and ready to process
        /// </summary>
        public readonly bool IsValid => Amount > 0 && TimeStamp >= 0;

        /// <summary>
        /// Gets the magnitude of the impact direction
        /// </summary>
        public readonly float ImpactMagnitude => math.length(Direction);

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies a damage multiplier to this request
        /// </summary>
        /// <param name="multiplier">Damage multiplier</param>
        /// <returns>Modified damage request</returns>
        public readonly DamageRequest WithMultiplier(float multiplier)
        {
            var result = this;
            result.Amount *= multiplier;
            return result;
        }

        /// <summary>
        /// Creates a copy of this damage request with a different damage type
        /// </summary>
        /// <param name="newType">New damage type</param>
        /// <returns>Modified damage request</returns>
        public readonly DamageRequest WithType(DamageType newType)
        {
            var result = this;
            result.Type = newType;
            return result;
        }

        /// <summary>
        /// Creates a copy of this damage request with normalized direction
        /// </summary>
        /// <returns>Damage request with normalized direction</returns>
        public readonly DamageRequest WithNormalizedDirection()
        {
            var result = this;
            if (math.length(Direction) > 0)
            {
                result.Direction = math.normalize(Direction);
            }
            return result;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates and clamps the damage request values to safe ranges
        /// </summary>
        /// <returns>Validated damage request</returns>
        public readonly DamageRequest Validated()
        {
            var result = this;
            result.Amount = math.max(0, Amount);
            result.TimeStamp = math.max(0, TimeStamp);
            
            // Clamp direction to reasonable bounds
            if (math.length(Direction) > 100f)
            {
                result.Direction = math.normalize(Direction) * 100f;
            }
            
            return result;
        }

        #endregion
    }
}
