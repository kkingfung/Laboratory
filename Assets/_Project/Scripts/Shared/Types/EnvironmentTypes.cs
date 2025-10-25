using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Shared.Types
{
    /// <summary>
    /// Biome types used throughout the ecosystem
    /// </summary>
    public enum BiomeType : byte
    {
        None = 0,
        Temperate = 1,
        Desert = 2,
        Forest = 3,
        Ocean = 4,
        Mountain = 5,
        Arctic = 6,
        Swamp = 7,
        Volcanic = 8,
        Cave = 9,
        Sky = 10
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