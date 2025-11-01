using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Shared.Types
{
    /// <summary>
    /// Biome types - synchronized with Laboratory.Core.Enums.BiomeType
    /// Note: This mirrors the canonical enum due to assembly reference constraints
    /// </summary>
    public enum BiomeType : byte
    {
        /// <summary>Open plains with moderate temperature and abundant plant life. Good for most creature types.</summary>
        Grassland = 0,
        /// <summary>Dense woodland areas with rich biodiversity. Favors agile and intelligent creatures.</summary>
        Forest = 1,
        /// <summary>Arid environments with extreme temperatures. Requires high resilience and water efficiency.</summary>
        Desert = 2,
        /// <summary>Cold, barren landscapes with limited vegetation. Demands high endurance and cold resistance.</summary>
        Tundra = 3,
        /// <summary>Aquatic environments supporting marine life. Requires swimming abilities and water breathing.</summary>
        Ocean = 4,
        /// <summary>High-altitude rocky terrain with thin air. Favors creatures with climbing abilities.</summary>
        Mountain = 5,
        /// <summary>Wetland areas with murky water and dense vegetation. Supports amphibious creatures.</summary>
        Swamp = 6,
        /// <summary>Moderate climate zones with balanced seasons. Ideal for creature development and breeding.</summary>
        Temperate = 7,
        /// <summary>Hot, humid environments with lush vegetation. Supports high biodiversity and rapid evolution.</summary>
        Tropical = 8,
        /// <summary>Permanently frozen landscapes with extreme cold. Only the hardiest creatures survive.</summary>
        Arctic = 9,
        /// <summary>Geologically active areas with lava flows and extreme heat. Favors fire-resistant creatures.</summary>
        Volcanic = 10,
        /// <summary>Mystical environments with crystalline formations. Enhances magical creature abilities.</summary>
        Crystal = 11,
        /// <summary>Dark, mysterious realms that corrupt or transform creatures. Increases shadow affinity.</summary>
        Shadow = 12,
        /// <summary>Radiant environments that purify and heal. Enhances light-based creature abilities.</summary>
        Light = 13,
        /// <summary>Chaotic null-space that defies natural laws. Unpredictable effects on creature genetics.</summary>
        Void = 14,
        /// <summary>Subterranean cave systems and tunnels. Favors creatures with enhanced senses and burrowing.</summary>
        Underground = 15,
        /// <summary>Floating islands and aerial environments. Requires flight capabilities or levitation.</summary>
        Sky = 16
    }

    /// <summary>
    /// Environmental conditions component
    /// </summary>
    public struct EnvironmentComponent : IComponentData
    {
        public BiomeType biome;
        public float temperature;
        public float humidity;
        public float pressure;
        public float oxygenLevel;
        public float lightLevel;
        public float3 windDirection;
        public float windSpeed;
    }

    /// <summary>
    /// Interface for environment services that can be implemented by any assembly
    /// </summary>
    public interface IEnvironmentProvider
    {
        BiomeType GetBiomeAt(float3 position);
        float GetEnvironmentalPressure(float3 position);
        bool IsPositionValid(float3 position);
        float GetTemperature(float3 position);
        float GetHumidity(float3 position);
    }
}