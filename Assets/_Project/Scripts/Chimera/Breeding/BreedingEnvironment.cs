using System;
using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Shared.Types;

namespace Laboratory.Chimera.Breeding
{
    /// <summary>
    /// Represents environmental conditions that influence breeding success and genetic outcomes
    /// </summary>
    [Serializable]
    public class BreedingEnvironment
    {
        [Header("Biome Information")]
        [SerializeField] private BiomeType biomeType = BiomeType.Temperate;
        
        [Header("Environmental Conditions")]
        [SerializeField] private float temperature = 22f; // Celsius
        [SerializeField] private float humidity = 60f; // Percentage
        [SerializeField] private float lightLevel = 0.8f; // 0-1 scale
        [SerializeField] private float airQuality = 0.9f; // 0-1 scale
        
        [Header("Resources")]
        [SerializeField] private float foodQuality = 0.8f; // 0-1 scale
        [SerializeField] private float waterQuality = 0.9f; // 0-1 scale
        [SerializeField] private float shelterQuality = 0.7f; // 0-1 scale
        [SerializeField] private float foodAvailability = 0.8f; // 0-1 scale
        
        [Header("Environmental Pressures")]
        [SerializeField] private float predatorPressure = 0.2f; // 0-1 scale
        [SerializeField] private float populationDensity = 0.5f; // 0-1 scale
        
        [Header("Social Factors")]
        [SerializeField] private float crowdingLevel = 0.3f; // 0-1 scale (higher = more crowded)
        [SerializeField] private float stressLevel = 0.2f; // 0-1 scale
        [SerializeField] private float comfortLevel = 0.8f; // 0-1 scale
        
        [Header("Breeding Modifiers")]
        [SerializeField] private float breedingSuccessMultiplier = 1f;
        [SerializeField] private float geneticStabilityMultiplier = 1f;
        [SerializeField] private float mutationRateModifier = 1f;
        
        /// <summary>
        /// BiomeType for this environment
        /// </summary>
        public BiomeType BiomeType 
        {
            get => biomeType;
            set => biomeType = value;
        }
        
        /// <summary>
        /// Environmental temperature in Celsius
        /// </summary>
        public float Temperature 
        {
            get => temperature;
            set => temperature = value;
        }
        
        /// <summary>
        /// Environmental humidity as a percentage
        /// </summary>
        public float Humidity => humidity;
        
        /// <summary>
        /// Light level from 0 (dark) to 1 (bright)
        /// </summary>
        public float LightLevel => lightLevel;
        
        /// <summary>
        /// Air quality from 0 (poor) to 1 (excellent)
        /// </summary>
        public float AirQuality => airQuality;
        
        /// <summary>
        /// Food quality from 0 (poor) to 1 (excellent)
        /// </summary>
        public float FoodQuality => foodQuality;
        
        /// <summary>
        /// Food availability from 0 (scarce) to 1 (abundant)
        /// </summary>
        public float FoodAvailability 
        {
            get => foodAvailability;
            set => foodAvailability = value;
        }
        
        /// <summary>
        /// Predator pressure from 0 (safe) to 1 (dangerous)
        /// </summary>
        public float PredatorPressure 
        {
            get => predatorPressure;
            set => predatorPressure = value;
        }
        
        /// <summary>
        /// Population density from 0 (sparse) to 1 (overcrowded)
        /// </summary>
        public float PopulationDensity 
        {
            get => populationDensity;
            set => populationDensity = value;
        }
        
        /// <summary>
        /// Water quality from 0 (poor) to 1 (excellent)
        /// </summary>
        public float WaterQuality => waterQuality;
        
        /// <summary>
        /// Shelter quality from 0 (poor) to 1 (excellent)
        /// </summary>
        public float ShelterQuality => shelterQuality;
        
        /// <summary>
        /// Crowding level from 0 (spacious) to 1 (overcrowded)
        /// </summary>
        public float CrowdingLevel => crowdingLevel;
        
        /// <summary>
        /// Stress level from 0 (relaxed) to 1 (highly stressed)
        /// </summary>
        public float StressLevel => stressLevel;
        
        /// <summary>
        /// Comfort level from 0 (uncomfortable) to 1 (very comfortable)
        /// </summary>
        public float ComfortLevel => comfortLevel;
        
        /// <summary>
        /// Multiplier for breeding success rate
        /// </summary>
        public float BreedingSuccessMultiplier => breedingSuccessMultiplier;
        
        /// <summary>
        /// Multiplier for genetic stability
        /// </summary>
        public float GeneticStabilityMultiplier => geneticStabilityMultiplier;
        
        /// <summary>
        /// Modifier for mutation rate (1.0 = normal, >1.0 = more mutations)
        /// </summary>
        public float MutationRateModifier => mutationRateModifier;
        
        public BreedingEnvironment()
        {
            // Default to good breeding conditions
            biomeType = BiomeType.Temperate;
            temperature = 22f;
            humidity = 60f;
            lightLevel = 0.8f;
            airQuality = 0.9f;
            foodQuality = 0.8f;
            waterQuality = 0.9f;
            shelterQuality = 0.7f;
            foodAvailability = 0.8f;
            predatorPressure = 0.2f;
            populationDensity = 0.5f;
            crowdingLevel = 0.3f;
            stressLevel = 0.2f;
            comfortLevel = 0.8f;
            
            CalculateModifiers();
        }
        
        /// <summary>
        /// Creates a breeding environment with specific conditions
        /// </summary>
        public BreedingEnvironment(float temp, float humid, float food, float water, float comfort)
        {
            biomeType = BiomeType.Temperate;
            temperature = temp;
            humidity = humid;
            lightLevel = 0.8f;
            airQuality = 0.9f;
            foodQuality = food;
            waterQuality = water;
            shelterQuality = 0.7f;
            foodAvailability = food;
            predatorPressure = 0.2f;
            populationDensity = 0.5f;
            crowdingLevel = 0.3f;
            stressLevel = Mathf.Clamp01(1f - comfort);
            comfortLevel = comfort;
            
            CalculateModifiers();
        }
        
        /// <summary>
        /// Updates environmental conditions and recalculates modifiers
        /// </summary>
        public void UpdateConditions(float temp, float humid, float food, float water, float stress)
        {
            temperature = temp;
            humidity = humid;
            foodQuality = food;
            waterQuality = water;
            stressLevel = stress;
            comfortLevel = Mathf.Clamp01(1f - stress);
            
            CalculateModifiers();
        }
        
        /// <summary>
        /// Calculates breeding and genetic modifiers based on environmental conditions
        /// </summary>
        private void CalculateModifiers()
        {
            // Calculate overall environmental quality (0-1)
            float environmentalQuality = (
                (1f - Mathf.Abs(temperature - 22f) / 30f) * 0.2f + // Optimal temp around 22°C
                (1f - Mathf.Abs(humidity - 60f) / 50f) * 0.15f +   // Optimal humidity around 60%
                lightLevel * 0.1f +
                airQuality * 0.15f +
                foodQuality * 0.2f +
                waterQuality * 0.15f +
                shelterQuality * 0.05f
            );
            
            // Calculate stress impact
            float stressImpact = 1f - (stressLevel * 0.5f + crowdingLevel * 0.3f);
            
            // Apply comfort bonus
            float comfortBonus = comfortLevel * 0.2f;
            
            // Calculate final modifiers
            breedingSuccessMultiplier = Mathf.Clamp(environmentalQuality * stressImpact + comfortBonus, 0.1f, 2.0f);
            geneticStabilityMultiplier = Mathf.Clamp(environmentalQuality * stressImpact * 1.2f, 0.5f, 1.5f);
            
            // Poor conditions increase mutation rate, good conditions stabilize genetics
            mutationRateModifier = Mathf.Clamp(2f - environmentalQuality - stressImpact, 0.5f, 3f);
        }
        
        /// <summary>
        /// Gets the overall environmental quality score (0-1)
        /// </summary>
        public float GetEnvironmentalQuality()
        {
            return (breedingSuccessMultiplier - 0.1f) / 1.9f; // Normalize to 0-1
        }
        
        /// <summary>
        /// Checks if the environment is suitable for breeding
        /// </summary>
        public bool IsSuitableForBreeding()
        {
            return breedingSuccessMultiplier > 0.6f && stressLevel < 0.7f;
        }
        
        /// <summary>
        /// Gets a text description of the environmental conditions
        /// </summary>
        public string GetConditionDescription()
        {
            float quality = GetEnvironmentalQuality();
            
            if (quality >= 0.8f) return "Excellent breeding conditions";
            if (quality >= 0.6f) return "Good breeding conditions";
            if (quality >= 0.4f) return "Fair breeding conditions";
            if (quality >= 0.2f) return "Poor breeding conditions";
            return "Unsuitable for breeding";
        }
        
        /// <summary>
        /// Creates a default optimal breeding environment
        /// </summary>
        public static BreedingEnvironment CreateOptimal()
        {
            return new BreedingEnvironment
            {
                biomeType = BiomeType.Temperate,
                temperature = 22f,
                humidity = 60f,
                lightLevel = 0.8f,
                airQuality = 0.95f,
                foodQuality = 0.9f,
                waterQuality = 0.95f,
                shelterQuality = 0.8f,
                foodAvailability = 0.9f,
                predatorPressure = 0.1f,
                populationDensity = 0.3f,
                crowdingLevel = 0.2f,
                stressLevel = 0.1f,
                comfortLevel = 0.9f
            };
        }
        
        /// <summary>
        /// Creates a poor breeding environment for testing
        /// </summary>
        public static BreedingEnvironment CreatePoor()
        {
            return new BreedingEnvironment
            {
                biomeType = BiomeType.Desert,
                temperature = 35f, // Too hot
                humidity = 90f,    // Too humid
                lightLevel = 0.3f,
                airQuality = 0.4f,
                foodQuality = 0.3f,
                waterQuality = 0.4f,
                shelterQuality = 0.2f,
                foodAvailability = 0.2f,
                predatorPressure = 0.8f,
                populationDensity = 0.9f,
                crowdingLevel = 0.8f,
                stressLevel = 0.7f,
                comfortLevel = 0.3f
            };
        }
    }
}
