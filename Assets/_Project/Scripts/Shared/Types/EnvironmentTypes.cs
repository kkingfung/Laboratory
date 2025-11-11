using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Shared.Types
{
    /// <summary>
    /// Unified biome system: comprehensive biome types for ecosystem management
    /// High-performance enum for dictionary indexing: Dictionary<BiomeType, T>
    /// </summary>
    public enum BiomeType : byte
    {
        // Terrestrial Biomes (0-31)
        Grassland = 0,
        Forest = 1,
        Desert = 2,
        Tundra = 3,
        Mountain = 4,
        Swamp = 5,
        Temperate = 6,
        Tropical = 7,
        Arctic = 8,
        Savanna = 9,
        Prairie = 10,
        Taiga = 11,
        Steppe = 12,
        Shrubland = 13,
        Badlands = 14,
        Canyon = 15,

        // Aquatic Biomes (32-47)
        Ocean = 32,
        Lake = 33,
        River = 34,
        Wetland = 35,
        Reef = 36,
        DeepSea = 37,
        Coastal = 38,
        Estuary = 39,

        // Underground Biomes (48-63)
        Underground = 48,
        Cave = 49,
        Cavern = 50,
        TunnelSystem = 51,
        UndergroundLake = 52,
        GeothermalCave = 53,
        CrystalCave = 54,
        DeepMine = 55,

        // Aerial Biomes (56-79)
        Sky = 64,
        CloudLayer = 65,
        HighAltitude = 66,
        FloatingIsland = 67,
        Stratosphere = 68,

        // Extreme Biomes (80-95)
        Volcanic = 80,
        Lava = 81,
        Geothermal = 82,
        IceCap = 83,
        Glacier = 84,
        Permafrost = 85,

        // Magical Biomes (96-111)
        Crystal = 96,
        Magical = 97,
        Enchanted = 98,
        ArcaneForest = 99,
        MysticLake = 100,
        RuneStone = 101,

        // Dimensional Biomes (112-127)
        Shadow = 112,
        Light = 113,
        Void = 114,
        Ethereal = 115,
        Astral = 116,
        Temporal = 117,

        // Corrupted/Altered Biomes (128-143)
        Corrupted = 128,
        Poisoned = 129,
        Radioactive = 130,
        Blighted = 131,
        Cursed = 132,

        // Artificial Biomes (144-159)
        Urban = 144,
        Laboratory = 145,
        Facility = 146,
        Greenhouse = 147,
        Biodome = 148,

        // Hybrid/Special Biomes (160-175)
        Hybrid = 160,
        Transitional = 161,
        Seasonal = 162,
        Unknown = 163,
        Experimental = 164,

        // Celestial Biomes (176-191)
        Celestial = 176,
        Lunar = 177,
        Solar = 178,
        Stellar = 179,
        Cosmic = 180
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