using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Breeding
{
    /// <summary>
    /// Core data structure representing a creature instance.
    /// This is a lightweight data structure that can be serialized and passed around.
    /// </summary>
    [Serializable]
    public class CreatureInstance
    {
        [SerializeField] private CreatureDefinition definition;
        [SerializeField] private GeneticProfile geneticProfile;
        [SerializeField] private string uniqueId;
        [SerializeField] private int ageInDays;
        [SerializeField] private int currentHealth;
        [SerializeField] private float happiness;
        [SerializeField] private int experience;
        [SerializeField] private int level;
        [SerializeField] private bool isWild;
        [SerializeField] private float healthPercentage;
        [SerializeField] private DateTime birthDate;
        [SerializeField] private List<string> parents;
        [SerializeField] private DateTime lastBreedTime;
        [SerializeField] private float breedingCooldownHours = 24f;
        
        // Public properties for compatibility
        public CreatureDefinition Definition 
        {
            get => definition;
            set => definition = value;
        }
        
        public GeneticProfile GeneticProfile 
        {
            get => geneticProfile;
            set => geneticProfile = value;
        }
        
        public string UniqueId 
        {
            get => uniqueId;
            private set => uniqueId = value;
        }
        
        public string InstanceId 
        {
            get => uniqueId;
            private set => uniqueId = value;
        }
        
        public int AgeInDays 
        {
            get => ageInDays;
            set => ageInDays = value;
        }
        
        public float Age 
        {
            get => ageInDays;
            set => ageInDays = (int)value;
        }
        
        public int CurrentHealth 
        {
            get => currentHealth;
            set => currentHealth = value;
        }
        
        public float Happiness 
        {
            get => happiness;
            set => happiness = value;
        }
        
        public int Experience 
        {
            get => experience;
            set => experience = value;
        }
        
        public int Level 
        {
            get => level;
            set => level = value;
        }
        
        public bool IsWild 
        {
            get => isWild;
            set => isWild = value;
        }
        
        public bool IsAlive 
        {
            get => currentHealth > 0;
        }
        
        public float HealthPercentage 
        {
            get => definition != null ? (float)currentHealth / definition.baseStats.health : 0f;
        }
        
        public DateTime BirthDate 
        {
            get => birthDate;
            set => birthDate = value;
        }
        
        public List<string> Parents 
        {
            get => parents;
            private set => parents = value;
        }
        
        // Additional properties for compatibility
        public bool IsAdult => definition != null && ageInDays >= definition.maturationAge;
        
        public float Length => definition != null ? definition.baseHeight : 1.0f;
        
        public CreatureSpecies Species => new CreatureSpecies 
        { 
            Id = definition?.speciesName ?? "Unknown",
            MaturityAge = definition?.maturationAge ?? 90,
            MaxLifespan = definition?.maxLifespan ?? 365
        };
        
        public CreatureGenetics Genetics => new CreatureGenetics
        {
            BaseStats = definition?.baseStats ?? new CreatureStats(),
            InheritedTraits = geneticProfile?.GetTraitNames() ?? new List<string>()
        };
        
        public CreatureInstance()
        {
            uniqueId = Guid.NewGuid().ToString();
            birthDate = DateTime.UtcNow;
            lastBreedTime = DateTime.MinValue; // Initialize to far past so no initial cooldown
            ageInDays = 0;
            level = 1;
            happiness = 0.5f;
            isWild = true;
            parents = new List<string>();
        }
        
        public CreatureInstance(CreatureDefinition definition, GeneticProfile genetics) : this()
        {
            this.definition = definition;
            this.geneticProfile = genetics;
            if (definition != null)
            {
                currentHealth = definition.baseStats.health;
            }
        }
        
        /// <summary>
        /// Gets the effective health based on genetics and level
        /// </summary>
        public int GetEffectiveHealth()
        {
            if (definition == null) return 100;
            
            var stats = definition.baseStats;
            if (geneticProfile != null)
                stats = geneticProfile.ApplyModifiers(stats);
                
            return Mathf.RoundToInt(stats.health * (1f + (level - 1) * 0.05f));
        }
        
        /// <summary>
        /// Checks if the creature is mature enough to breed
        /// </summary>
        public bool CanBreed => definition != null && ageInDays >= definition.maturationAge && currentHealth > definition.baseStats.health * 0.5f && !IsInBreedingCooldown;

        /// <summary>
        /// Checks if the creature is currently in breeding cooldown
        /// </summary>
        public bool IsInBreedingCooldown => DateTime.UtcNow.Subtract(lastBreedTime).TotalHours < breedingCooldownHours;

        /// <summary>
        /// Gets the time when the creature last bred
        /// </summary>
        public DateTime LastBreedTime
        {
            get => lastBreedTime;
            set => lastBreedTime = value;
        }

        /// <summary>
        /// Gets the breeding cooldown duration in hours
        /// </summary>
        public float BreedingCooldownHours
        {
            get => breedingCooldownHours;
            set => breedingCooldownHours = value;
        }
        

        
        /// <summary>
        /// Gets a string representation of the creature for debugging
        /// </summary>
        public override string ToString()
        {
            return $"{definition?.speciesName ?? "Unknown"} (Level {level}, Age {ageInDays} days)";
        }
    }
}
