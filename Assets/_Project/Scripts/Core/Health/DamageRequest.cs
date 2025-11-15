using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Core.Enums;

namespace Laboratory.Core.Health
{

    /// <summary>
    /// Represents a damage request with all necessary information
    /// </summary>
    [System.Serializable]
    public class DamageRequest
    {
        [Header("Damage Information")]
        [SerializeField] private float _amount;
        [SerializeField] private DamageType _type = DamageType.Physical;
        [SerializeField] private bool _canBeBlocked = true;
        [SerializeField] private bool _canBeDodged = true;
        [SerializeField] private bool _isCritical = false;
        [SerializeField] private bool _triggerInvulnerability = true;

        [Header("Source Information")]
        [SerializeField] private GameObject _source;
        [SerializeField] private Entity _sourceEntity = Entity.Null;
        [SerializeField] private string _sourceName;
        [SerializeField] private Vector3 _hitPoint;
        [SerializeField] private Vector3 _direction;

        [Header("Effects")]
        [SerializeField] private float _knockbackForce = 0f;
        [SerializeField] private GameObject _hitEffect;
        [SerializeField] private AudioClip _hitSound;

        [Header("Metadata")]
        [SerializeField] private float _timestamp;
        [SerializeField] private int _damageId;
        [SerializeField] private string _description;
        [SerializeField] private System.Collections.Generic.Dictionary<string, object> _metadata;

        // Static counter for unique damage IDs
        private static int _nextDamageId = 1;

        #region Properties

        /// <summary>
        /// Amount of damage to deal
        /// </summary>
        public float Amount 
        { 
            get => _amount; 
            set => _amount = Mathf.Max(0f, value); 
        }

        /// <summary>
        /// Type of damage being dealt
        /// </summary>
        public DamageType Type 
        { 
            get => _type; 
            set => _type = value; 
        }

        /// <summary>
        /// Whether this damage can be blocked
        /// </summary>
        public bool CanBeBlocked 
        { 
            get => _canBeBlocked; 
            set => _canBeBlocked = value; 
        }

        /// <summary>
        /// Whether this damage can be dodged
        /// </summary>
        public bool CanBeDodged 
        { 
            get => _canBeDodged; 
            set => _canBeDodged = value; 
        }

        /// <summary>
        /// Whether this damage is a critical hit
        /// </summary>
        public bool IsCritical 
        { 
            get => _isCritical; 
            set => _isCritical = value; 
        }

        /// <summary>
        /// Whether this damage should trigger invulnerability frames
        /// </summary>
        public bool TriggerInvulnerability 
        { 
            get => _triggerInvulnerability; 
            set => _triggerInvulnerability = value; 
        }

        /// <summary>
        /// Source GameObject that caused the damage (for MonoBehaviour systems)
        /// </summary>
        public GameObject Source 
        { 
            get => _source; 
            set => _source = value; 
        }

        /// <summary>
        /// Source Entity that caused the damage (for ECS systems)
        /// </summary>
        public Entity SourceEntity 
        { 
            get => _sourceEntity; 
            set => _sourceEntity = value; 
        }

        /// <summary>
        /// Name of the damage source
        /// </summary>
        public string SourceName 
        { 
            get => string.IsNullOrEmpty(_sourceName) ? (_source?.name ?? "Unknown") : _sourceName; 
            set => _sourceName = value; 
        }

        /// <summary>
        /// World position where the damage hit occurred
        /// </summary>
        public Vector3 HitPoint 
        { 
            get => _hitPoint; 
            set => _hitPoint = value; 
        }

        /// <summary>
        /// Source position for the damage (usually the same as HitPoint)
        /// </summary>
        public Vector3 SourcePosition 
        { 
            get => _hitPoint; 
            set => _hitPoint = value; 
        }

        /// <summary>
        /// Direction of the damage (for knockback, etc.)
        /// </summary>
        public Vector3 Direction 
        { 
            get => _direction; 
            set => _direction = value.normalized; 
        }

        /// <summary>
        /// Force to apply for knockback
        /// </summary>
        public float KnockbackForce 
        { 
            get => _knockbackForce; 
            set => _knockbackForce = Mathf.Max(0f, value); 
        }

        /// <summary>
        /// Effect to spawn at hit point
        /// </summary>
        public GameObject HitEffect 
        { 
            get => _hitEffect; 
            set => _hitEffect = value; 
        }

        /// <summary>
        /// Sound to play when damage is dealt
        /// </summary>
        public AudioClip HitSound 
        { 
            get => _hitSound; 
            set => _hitSound = value; 
        }

        /// <summary>
        /// Timestamp when the damage was created
        /// </summary>
        public float Timestamp 
        { 
            get => _timestamp; 
            private set => _timestamp = value; 
        }

        /// <summary>
        /// Unique identifier for this damage instance
        /// </summary>
        public int DamageId 
        { 
            get => _damageId; 
            private set => _damageId = value; 
        }

        /// <summary>
        /// Optional description of the damage
        /// </summary>
        public string Description 
        { 
            get => _description; 
            set => _description = value; 
        }

        /// <summary>
        /// Optional metadata dictionary for custom data
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> Metadata 
        { 
            get => _metadata ??= new System.Collections.Generic.Dictionary<string, object>(); 
            set => _metadata = value; 
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public DamageRequest()
        {
            _timestamp = Time.time;
            _damageId = _nextDamageId++;
        }

        /// <summary>
        /// Creates a damage request with basic parameters
        /// </summary>
        /// <param name="amount">Damage amount</param>
        /// <param name="source">Source of the damage</param>
        /// <param name="type">Type of damage</param>
        public DamageRequest(float amount, GameObject source = null, DamageType type = DamageType.Physical) : this()
        {
            _amount = Mathf.Max(0f, amount);
            _source = source;
            _type = type;
        }

        /// <summary>
        /// Creates a damage request with position and direction
        /// </summary>
        /// <param name="amount">Damage amount</param>
        /// <param name="source">Source of the damage</param>
        /// <param name="hitPoint">World position of the hit</param>
        /// <param name="direction">Direction of the damage</param>
        /// <param name="type">Type of damage</param>
        public DamageRequest(float amount, GameObject source, Vector3 hitPoint, Vector3 direction, DamageType type = DamageType.Physical) : this(amount, source, type)
        {
            _hitPoint = hitPoint;
            _direction = direction.normalized;
        }

        /// <summary>
        /// Creates damage request from Entity source (ECS)
        /// </summary>
        public DamageRequest(float amount, Entity sourceEntity, DamageType type = DamageType.Physical) : this()
        {
            _amount = Mathf.Max(0f, amount);
            _sourceEntity = sourceEntity;
            _type = type;
        }

        /// <summary>
        /// Creates damage request with position and direction (ECS)
        /// </summary>
        public DamageRequest(float amount, Entity sourceEntity, Vector3 hitPoint, Vector3 direction, DamageType type = DamageType.Physical) : this(amount, sourceEntity, type)
        {
            _hitPoint = hitPoint;
            _direction = direction.normalized;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a copy of this damage request
        /// </summary>
        /// <returns>A new DamageRequest with the same values</returns>
        public DamageRequest Clone()
        {
            var clone = new DamageRequest
            {
                _amount = this._amount,
                _type = this._type,
                _canBeBlocked = this._canBeBlocked,
                _canBeDodged = this._canBeDodged,
                _isCritical = this._isCritical,
                _triggerInvulnerability = this._triggerInvulnerability,
                _source = this._source,
                _sourceEntity = this._sourceEntity,
                _sourceName = this._sourceName,
                _hitPoint = this._hitPoint,
                _direction = this._direction,
                _knockbackForce = this._knockbackForce,
                _hitEffect = this._hitEffect,
                _hitSound = this._hitSound,
                _timestamp = Time.time, // New timestamp for the clone
                _damageId = _nextDamageId++, // New ID for the clone
                _description = this._description
            };

            // Deep copy metadata
            if (_metadata != null)
            {
                clone._metadata = new System.Collections.Generic.Dictionary<string, object>(_metadata);
            }

            return clone;
        }

        /// <summary>
        /// Scales the damage amount by a multiplier
        /// </summary>
        /// <param name="multiplier">Damage multiplier</param>
        /// <returns>This damage request for chaining</returns>
        public DamageRequest ScaleDamage(float multiplier)
        {
            _amount *= Mathf.Max(0f, multiplier);
            return this;
        }

        /// <summary>
        /// Marks this damage as a critical hit
        /// </summary>
        /// <param name="criticalMultiplier">Critical damage multiplier</param>
        /// <returns>This damage request for chaining</returns>
        public DamageRequest AsCritical(float criticalMultiplier = 2f)
        {
            _isCritical = true;
            _amount *= Mathf.Max(1f, criticalMultiplier);
            return this;
        }

        /// <summary>
        /// Sets the knockback force
        /// </summary>
        /// <param name="force">Knockback force</param>
        /// <returns>This damage request for chaining</returns>
        public DamageRequest WithKnockback(float force)
        {
            _knockbackForce = Mathf.Max(0f, force);
            return this;
        }

        /// <summary>
        /// Sets visual and audio effects
        /// </summary>
        /// <param name="effect">Hit effect prefab</param>
        /// <param name="sound">Hit sound</param>
        /// <returns>This damage request for chaining</returns>
        public DamageRequest WithEffects(GameObject effect = null, AudioClip sound = null)
        {
            _hitEffect = effect;
            _hitSound = sound;
            return this;
        }

        /// <summary>
        /// Checks if this damage can affect the target based on filters
        /// </summary>
        /// <param name="target">Target to check against</param>
        /// <returns>True if the damage can affect the target</returns>
        public bool CanAffectTarget(GameObject target)
        {
            if (target == null) return false;
            if (target == _source) return false; // Can't damage self by default

            // Add more sophisticated filtering logic here as needed
            return true;
        }

        /// <summary>
        /// Gets a formatted string representation of this damage
        /// </summary>
        /// <returns>Formatted damage information</returns>
        public override string ToString()
        {
            string critText = _isCritical ? " (CRITICAL)" : "";
            string sourceText = SourceName;
            return $"{_amount:F1} {_type} damage from {sourceText}{critText}";
        }

        /// <summary>
        /// Gets detailed damage information for debugging
        /// </summary>
        /// <returns>Detailed damage information</returns>
        public string ToDetailedString()
        {
            return $"DamageRequest [ID:{_damageId}] " +
                   $"Amount:{_amount:F1} Type:{_type} " +
                   $"Source:{SourceName} " +
                   $"Critical:{_isCritical} " +
                   $"Blockable:{_canBeBlocked} " +
                   $"Dodgeable:{_canBeDodged} " +
                   $"Knockback:{_knockbackForce:F1} " +
                   $"Time:{_timestamp:F3}";
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a simple damage request
        /// </summary>
        public static DamageRequest CreateSimple(float amount, GameObject source = null)
        {
            return new DamageRequest(amount, source);
        }

        /// <summary>
        /// Creates a critical damage request
        /// </summary>
        public static DamageRequest CreateCritical(float amount, GameObject source = null, float critMultiplier = 2f)
        {
            return new DamageRequest(amount, source).AsCritical(critMultiplier);
        }

        /// <summary>
        /// Creates a damage request with knockback
        /// </summary>
        public static DamageRequest CreateWithKnockback(float amount, GameObject source, Vector3 direction, float knockbackForce)
        {
            var damageRequest = new DamageRequest(amount, source)
                .WithKnockback(knockbackForce);
            damageRequest.Direction = direction;
            return damageRequest;
        }

        /// <summary>
        /// Creates a damage request of a specific type
        /// </summary>
        public static DamageRequest CreateTyped(float amount, DamageType type, GameObject source = null)
        {
            return new DamageRequest(amount, source, type);
        }

        /// <summary>Creates a simple damage request (ECS)</summary>
        public static DamageRequest CreateSimple(float amount, Entity source)
        {
            return new DamageRequest(amount, source);
        }

        /// <summary>Creates an unavoidable damage request</summary>
        public static DamageRequest CreateUnavoidable(float amount, GameObject source = null)
        {
            return new DamageRequest(amount, source).AsUnavoidable();
        }

        #endregion

        #region ECS Support Methods

        /// <summary>
        /// Sets damage as unblockable and undodgeable
        /// </summary>
        public DamageRequest AsUnavoidable()
        {
            _canBeBlocked = false;
            _canBeDodged = false;
            return this;
        }

        /// <summary>
        /// Adds metadata to the damage request
        /// </summary>
        public DamageRequest WithMetadata(string key, object value)
        {
            Metadata[key] = value;
            return this;
        }

        /// <summary>
        /// Checks if this damage can affect the target (ECS)
        /// </summary>
        public bool CanAffectTarget(Entity target)
        {
            if (target == Entity.Null) return false;
            if (target == _sourceEntity) return false; // Can't damage self
            return true;
        }

        /// <summary>
        /// Converts to ECS-compatible struct for use in DOTS systems
        /// </summary>
        public DamageRequestECS ToECS()
        {
            return new DamageRequestECS
            {
                Amount = _amount,
                DamageType = (int)_type,
                CanBeBlocked = _canBeBlocked,
                CanBeDodged = _canBeDodged,
                IsCritical = _isCritical,
                Source = _sourceEntity,
                HitPoint = _hitPoint,
                Direction = _direction,
                KnockbackForce = _knockbackForce,
                Timestamp = _timestamp,
                DamageId = _damageId
            };
        }

        /// <summary>
        /// Creates DamageRequest from ECS struct
        /// </summary>
        public static DamageRequest FromECS(DamageRequestECS ecsRequest)
        {
            return new DamageRequest
            {
                _amount = ecsRequest.Amount,
                _type = (DamageType)ecsRequest.DamageType,
                _canBeBlocked = ecsRequest.CanBeBlocked,
                _canBeDodged = ecsRequest.CanBeDodged,
                _isCritical = ecsRequest.IsCritical,
                _sourceEntity = ecsRequest.Source,
                _hitPoint = ecsRequest.HitPoint,
                _direction = ecsRequest.Direction,
                _knockbackForce = ecsRequest.KnockbackForce,
                _timestamp = ecsRequest.Timestamp,
                _damageId = ecsRequest.DamageId
            };
        }

        #endregion
    }

    /// <summary>
    /// ECS-compatible damage request struct for DOTS systems
    /// This replaces the previous Models.ECS.Components.DamageRequest
    /// </summary>
    public struct DamageRequestECS : IComponentData
    {
        public float Amount;
        public int DamageType; // Using int for ECS compatibility
        public bool CanBeBlocked;
        public bool CanBeDodged;
        public bool IsCritical;
        public Entity Source;
        public float3 HitPoint;
        public float3 Direction;
        public float KnockbackForce;
        public float Timestamp;
        public int DamageId;

        /// <summary>Creates ECS damage request with default values</summary>
        public static DamageRequestECS Create(float amount, Entity source = default)
        {
            return new DamageRequestECS
            {
                Amount = amount,
                DamageType = 0, // Physical
                CanBeBlocked = true,
                CanBeDodged = true,
                IsCritical = false,
                Source = source,
                HitPoint = float3.zero,
                Direction = float3.zero,
                KnockbackForce = 0f,
                Timestamp = UnityEngine.Time.time,
                DamageId = UnityEngine.Random.Range(1, int.MaxValue)
            };
        }

        /// <summary>Converts to full DamageRequest for interop</summary>
        public DamageRequest ToManaged()
        {
            return DamageRequest.FromECS(this);
        }
    }

    /// <summary>
    /// Utility class for converting between damage request formats
    /// Provides centralized conversion logic for hybrid systems
    /// </summary>
    public static class DamageRequestConverter
    {
        /// <summary>Converts GameObject-based damage to Entity-based</summary>
        public static DamageRequest ConvertToEntity(DamageRequest original, Entity sourceEntity)
        {
            var converted = original.Clone();
            converted.SourceEntity = sourceEntity;
            return converted;
        }

        /// <summary>Converts Entity-based damage to GameObject-based</summary>
        public static DamageRequest ConvertToGameObject(DamageRequest original, GameObject sourceGameObject)
        {
            var converted = original.Clone();
            converted.Source = sourceGameObject;
            return converted;
        }

        /// <summary>Batch converts multiple damage requests to ECS format</summary>
        public static DamageRequestECS[] BatchToECS(IEnumerable<DamageRequest> requests)
        {
            return requests.Select(r => r.ToECS()).ToArray();
        }

        /// <summary>Batch converts multiple ECS damage requests to managed format</summary>
        public static DamageRequest[] BatchFromECS(IEnumerable<DamageRequestECS> requests)
        {
            return requests.Select(r => DamageRequest.FromECS(r)).ToArray();
        }
    }
}
