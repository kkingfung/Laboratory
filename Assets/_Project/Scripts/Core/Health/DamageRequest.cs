using UnityEngine;

namespace Laboratory.Core.Health
{
    /// <summary>
    /// Standardized data structure for damage requests.
    /// Contains all information needed to process damage consistently across systems.
    /// </summary>
    [System.Serializable]
    public class DamageRequest
    {
        /// <summary>Amount of damage to apply.</summary>
        public float Amount { get; set; }
        
        /// <summary>Type of damage being applied.</summary>
        public DamageType Type { get; set; }
        
        /// <summary>Source of the damage (GameObject, component, etc.).</summary>
        public object Source { get; set; }
        
        /// <summary>Direction of the damage impact (for physics effects).</summary>
        public Vector3 Direction { get; set; }
        
        /// <summary>Position where the damage originated.</summary>
        public Vector3 SourcePosition { get; set; }
        
        /// <summary>Whether this damage can be blocked or mitigated.</summary>
        public bool CanBeBlocked { get; set; }
        
        /// <summary>Whether this damage should trigger invulnerability frames.</summary>
        public bool TriggerInvulnerability { get; set; }
        
        /// <summary>Additional metadata for the damage.</summary>
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; }
        
        #region Constructors
        
        public DamageRequest()
        {
            Type = DamageType.Normal;
            CanBeBlocked = true;
            TriggerInvulnerability = true;
            Metadata = new System.Collections.Generic.Dictionary<string, object>();
        }
        
        public DamageRequest(float amount, object source = null) : this()
        {
            Amount = amount;
            Source = source;
        }
        
        public DamageRequest(float amount, DamageType type, object source = null) : this(amount, source)
        {
            Type = type;
        }
        
        public DamageRequest(float amount, DamageType type, Vector3 direction, object source = null) : this(amount, type, source)
        {
            Direction = direction;
        }
        
        #endregion
        
        #region Factory Methods
        
        public static DamageRequest Create(float amount, object source = null)
        {
            return new DamageRequest(amount, source);
        }
        
        public static DamageRequest CreateTyped(float amount, DamageType type, object source = null)
        {
            return new DamageRequest(amount, type, source);
        }
        
        public static DamageRequest CreateDirectional(float amount, Vector3 direction, object source = null)
        {
            return new DamageRequest(amount, DamageType.Normal, direction, source);
        }
        
        #endregion
    }

    /// <summary>
    /// Types of damage that can be applied to entities.
    /// </summary>
    public enum DamageType
    {
        Normal,
        Critical,
        Fire,
        Ice,
        Lightning,
        Poison,
        Healing
    }
}
