using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Combat
{
    /// <summary>
    /// Manages global faction relationships and diplomatic systems.
    /// Provides centralized faction relationship data and change handling.
    /// </summary>
    [CreateAssetMenu(fileName = "FactionManager", menuName = "Laboratory/Combat/Faction Manager")]
    public class FactionManager : ScriptableObject
    {
        [Header("Default Faction Relationships")]
        [SerializeField] private List<FactionRelationshipData> defaultRelationships = new();

        [Header("Faction Definitions")]
        [SerializeField] private List<FactionDefinition> factionDefinitions = new();

        private readonly Dictionary<string, Dictionary<string, FactionRelationship>> _relationships = new();
        private readonly Dictionary<string, FactionDefinition> _factions = new();

        /// <summary>Event fired when any faction relationship changes globally</summary>
        public static event Action<FactionRelationshipChangeEventArgs> OnGlobalRelationshipChanged;

        public void Initialize()
        {
            // Initialize faction definitions
            foreach (var faction in factionDefinitions)
            {
                _factions[faction.factionId] = faction;
            }

            // Initialize default relationships
            foreach (var relationship in defaultRelationships)
            {
                SetRelationship(relationship.factionId1, relationship.factionId2, relationship.relationship);
            }

            Debug.Log($"[FactionManager] Initialized with {_factions.Count} factions and {defaultRelationships.Count} default relationships");
        }

        /// <summary>Gets relationship between two factions</summary>
        public FactionRelationship GetRelationship(string factionId1, string factionId2)
        {
            if (string.IsNullOrEmpty(factionId1) || string.IsNullOrEmpty(factionId2))
                return FactionRelationship.Neutral;

            if (factionId1 == factionId2)
                return FactionRelationship.Allied;

            if (_relationships.TryGetValue(factionId1, out var faction1Relations))
            {
                if (faction1Relations.TryGetValue(factionId2, out var relationship))
                    return relationship;
            }

            // Check reverse relationship
            if (_relationships.TryGetValue(factionId2, out var faction2Relations))
            {
                if (faction2Relations.TryGetValue(factionId1, out var relationship))
                    return relationship;
            }

            // Default to neutral if no relationship defined
            return FactionRelationship.Neutral;
        }

        /// <summary>Sets relationship between two factions</summary>
        public void SetRelationship(string factionId1, string factionId2, FactionRelationship relationship)
        {
            if (string.IsNullOrEmpty(factionId1) || string.IsNullOrEmpty(factionId2))
                return;

            if (factionId1 == factionId2)
                return; // Can't set relationship with self

            var oldRelationship = GetRelationship(factionId1, factionId2);

            // Ensure both factions have relationship dictionaries
            if (!_relationships.ContainsKey(factionId1))
                _relationships[factionId1] = new Dictionary<string, FactionRelationship>();

            if (!_relationships.ContainsKey(factionId2))
                _relationships[factionId2] = new Dictionary<string, FactionRelationship>();

            // Set bidirectional relationship
            _relationships[factionId1][factionId2] = relationship;
            _relationships[factionId2][factionId1] = relationship;

            // Fire events if relationship changed
            if (oldRelationship != relationship)
            {
                var eventArgs = new FactionRelationshipChangeEventArgs(factionId1, factionId2, oldRelationship, relationship);
                OnGlobalRelationshipChanged?.Invoke(eventArgs);

                Debug.Log($"[FactionManager] Relationship changed: {factionId1} <-> {factionId2}: {oldRelationship} -> {relationship}");
            }
        }

        /// <summary>Checks if two factions are hostile</summary>
        public bool AreHostile(string factionId1, string factionId2)
        {
            var relationship = GetRelationship(factionId1, factionId2);
            return relationship <= FactionRelationship.Unfriendly;
        }

        /// <summary>Checks if two factions are allied</summary>
        public bool AreAllied(string factionId1, string factionId2)
        {
            var relationship = GetRelationship(factionId1, factionId2);
            return relationship >= FactionRelationship.Allied;
        }

        /// <summary>Checks if two factions are neutral</summary>
        public bool AreNeutral(string factionId1, string factionId2)
        {
            var relationship = GetRelationship(factionId1, factionId2);
            return relationship == FactionRelationship.Neutral;
        }

        /// <summary>Gets faction definition by ID</summary>
        public FactionDefinition GetFactionDefinition(string factionId)
        {
            _factions.TryGetValue(factionId, out var faction);
            return faction;
        }

        /// <summary>Gets all faction IDs</summary>
        public List<string> GetAllFactionIds()
        {
            return new List<string>(_factions.Keys);
        }

        /// <summary>Gets all factions of a specific type</summary>
        public List<FactionDefinition> GetFactionsOfType(FactionType factionType)
        {
            var result = new List<FactionDefinition>();
            foreach (var faction in _factions.Values)
            {
                if (faction.factionType == factionType)
                    result.Add(faction);
            }
            return result;
        }

        /// <summary>Temporarily modifies relationship (useful for events/conditions)</summary>
        public void ModifyRelationshipTemporarily(string factionId1, string factionId2, FactionRelationship newRelationship, float duration)
        {
            var oldRelationship = GetRelationship(factionId1, factionId2);
            SetRelationship(factionId1, factionId2, newRelationship);

            // Schedule restoration of original relationship
            if (Application.isPlaying)
            {
                var gameObject = new GameObject("TempRelationshipRestore");
                var restorer = gameObject.AddComponent<TemporaryRelationshipRestorer>();
                restorer.Initialize(this, factionId1, factionId2, oldRelationship, duration);
            }
        }

        [System.Serializable]
        public class FactionRelationshipData
        {
            public string factionId1;
            public string factionId2;
            public FactionRelationship relationship;
        }

        [System.Serializable]
        public class FactionDefinition
        {
            public string factionId;
            public string factionName;
            public FactionType factionType;
            public DiplomaticStance defaultStance;
            public Color factionColor = Color.white;
            public bool canChangeDiplomacy = true;
            [TextArea(3, 5)]
            public string description;
        }
    }

    /// <summary>Component to temporarily restore faction relationships</summary>
    public class TemporaryRelationshipRestorer : MonoBehaviour
    {
        private FactionManager _factionManager;
        private string _factionId1;
        private string _factionId2;
        private FactionRelationship _originalRelationship;
        private float _duration;

        public void Initialize(FactionManager factionManager, string factionId1, string factionId2,
            FactionRelationship originalRelationship, float duration)
        {
            _factionManager = factionManager;
            _factionId1 = factionId1;
            _factionId2 = factionId2;
            _originalRelationship = originalRelationship;
            _duration = duration;

            Invoke(nameof(RestoreRelationship), duration);
        }

        private void RestoreRelationship()
        {
            if (_factionManager != null)
            {
                _factionManager.SetRelationship(_factionId1, _factionId2, _originalRelationship);
            }
            Destroy(gameObject);
        }
    }
}