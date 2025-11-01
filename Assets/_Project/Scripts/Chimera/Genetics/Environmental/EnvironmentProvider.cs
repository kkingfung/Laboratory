using Unity.Mathematics;
using Laboratory.Chimera.Core;
using Laboratory.Core.Enums;

namespace Laboratory.Chimera.Genetics.Environmental
{
    /// <summary>
    /// Interface for providing environmental data to genetic systems
    /// </summary>
    public interface IEnvironmentProvider
    {
        BiomeType GetBiomeAt(float3 position);
        float GetTemperatureAt(float3 position);
        float GetHumidityAt(float3 position);
        float GetResourceDensityAt(float3 position);
        float GetEnvironmentalPressure(float3 position);
        bool IsPositionValid(float3 position);
    }

    /// <summary>
    /// Concrete implementation of environment provider for genetic systems
    /// </summary>
    public class EnvironmentProvider : IEnvironmentProvider
    {
        private readonly float3 _worldSize;
        private readonly BiomeType[] _biomeMap;

        public EnvironmentProvider(float3 worldSize = default)
        {
            _worldSize = worldSize.Equals(float3.zero) ? new float3(1000f, 100f, 1000f) : worldSize;
            _biomeMap = GenerateBiomeMap();
        }

        public BiomeType GetBiomeAt(float3 position)
        {
            // Normalize position to world bounds
            var normalizedX = math.clamp(position.x / _worldSize.x, 0f, 1f);
            var normalizedZ = math.clamp(position.z / _worldSize.z, 0f, 1f);

            // Simple biome distribution based on position
            if (position.y > _worldSize.y * 0.8f)
                return BiomeType.Mountain;
            if (position.y < _worldSize.y * 0.1f)
                return BiomeType.Ocean;

            // Use noise-based biome selection for surface areas
            var biomeNoise = math.sin(normalizedX * 10f) * math.cos(normalizedZ * 10f);

            if (biomeNoise > 0.5f) return BiomeType.Forest;
            if (biomeNoise > 0.0f) return BiomeType.Temperate;
            if (biomeNoise > -0.5f) return BiomeType.Desert;

            return BiomeType.Swamp;
        }

        public float GetEnvironmentalPressure(float3 position)
        {
            var biome = GetBiomeAt(position);
            var basePressure = GetBasePressureForBiome(biome);

            // Add variation based on position
            var altitude = position.y / _worldSize.y;
            var altitudePressure = math.lerp(1.2f, 0.5f, altitude);

            return basePressure * altitudePressure;
        }

        public bool IsPositionValid(float3 position)
        {
            return position.x >= 0 && position.x <= _worldSize.x &&
                   position.y >= 0 && position.y <= _worldSize.y &&
                   position.z >= 0 && position.z <= _worldSize.z;
        }

        public float GetTemperature(float3 position)
        {
            var biome = GetBiomeAt(position);
            var baseTemp = GetBaseTemperatureForBiome(biome);

            // Altitude affects temperature
            var altitude = position.y / _worldSize.y;
            var altitudeEffect = altitude * -20f; // -20°C per normalized altitude unit

            return baseTemp + altitudeEffect;
        }

        public float GetHumidity(float3 position)
        {
            var biome = GetBiomeAt(position);
            return GetBaseHumidityForBiome(biome);
        }

        public float GetTemperatureAt(float3 position)
        {
            return GetTemperature(position);
        }

        public float GetHumidityAt(float3 position)
        {
            return GetHumidity(position);
        }

        public float GetResourceDensityAt(float3 position)
        {
            var biome = GetBiomeAt(position);
            var baseDensity = GetBaseResourceDensityForBiome(biome);

            // Add noise-based variation for more realistic distribution
            var noise = math.sin(position.x * 0.1f) * math.cos(position.z * 0.1f);
            var variation = noise * 0.2f; // ±20% variation

            return math.clamp(baseDensity + variation, 0f, 2f);
        }

        private BiomeType[] GenerateBiomeMap()
        {
            // This could be loaded from a file or generated procedurally
            return new BiomeType[]
            {
                BiomeType.Temperate, BiomeType.Forest, BiomeType.Desert,
                BiomeType.Mountain, BiomeType.Swamp
            };
        }

        private float GetBasePressureForBiome(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean => 1.5f,
                BiomeType.Desert => 0.8f,
                BiomeType.Mountain => 0.6f,
                BiomeType.Arctic => 1.2f,
                BiomeType.Volcanic => 1.8f,
                BiomeType.Swamp => 1.3f,
                _ => 1.0f
            };
        }

        private float GetBaseTemperatureForBiome(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Arctic => -10f,
                BiomeType.Mountain => 5f,
                BiomeType.Temperate => 20f,
                BiomeType.Forest => 18f,
                BiomeType.Desert => 35f,
                BiomeType.Swamp => 25f,
                BiomeType.Volcanic => 40f,
                BiomeType.Ocean => 15f,
                _ => 20f
            };
        }

        private float GetBaseHumidityForBiome(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Desert => 0.1f,
                BiomeType.Arctic => 0.2f,
                BiomeType.Mountain => 0.4f,
                BiomeType.Temperate => 0.6f,
                BiomeType.Forest => 0.8f,
                BiomeType.Swamp => 0.9f,
                BiomeType.Ocean => 1.0f,
                BiomeType.Volcanic => 0.3f,
                _ => 0.5f
            };
        }

        private float GetBaseResourceDensityForBiome(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest => 1.2f,      // Rich in organic resources
                BiomeType.Mountain => 1.5f,    // Rich in minerals and rare materials
                BiomeType.Ocean => 0.8f,       // Moderate resources, mainly food
                BiomeType.Desert => 0.3f,      // Very limited resources
                BiomeType.Swamp => 1.1f,       // Good biodiversity and materials
                BiomeType.Temperate => 1.0f,   // Balanced resource availability
                BiomeType.Arctic => 0.4f,      // Limited by harsh conditions
                BiomeType.Volcanic => 1.8f,    // Very rich in rare minerals
                BiomeType.Tropical => 1.3f,    // High biodiversity (jungle-like)
                BiomeType.Grassland => 0.9f,   // Good for basic resources
                _ => 0.7f                      // Default moderate density
            };
        }
    }
}