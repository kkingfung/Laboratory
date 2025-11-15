using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Advanced
{
    /// <summary>
    /// Procedural generation system for terrain, creatures, and biomes.
    /// Uses noise functions, seed-based generation, and configurable rules.
    /// Enables infinite worlds with deterministic reproduction.
    /// </summary>
    public class ProceduralGenerationSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Generation Settings")]
        [SerializeField] private int globalSeed = 12345;
        [SerializeField] private bool useSeedFromDateTime = false;

        [Header("Terrain Generation")]
        [SerializeField] private float terrainScale = 100f;
        [SerializeField] private int terrainOctaves = 4;
        [SerializeField] private float terrainPersistence = 0.5f;
        [SerializeField] private float terrainLacunarity = 2f;

        [Header("Creature Generation")]
        [SerializeField] private int maxCreatureVariations = 1000;
        [SerializeField] private bool useGeneticMixing = true;

        [Header("Biome Generation")]
        [SerializeField] private float biomeScale = 200f;
        [SerializeField] private float moistureScale = 150f;
        [SerializeField] private float temperatureScale = 180f;

        [Header("Performance")]
        [SerializeField] private bool useMultithreading = true;
        [SerializeField] private int chunkGenerationBudget = 1; // Per frame

        #endregion

        #region Private Fields

        private static ProceduralGenerationSystem _instance;

        // Noise generators
        private System.Random _random;
        private readonly Dictionary<string, NoiseGenerator> _noiseGenerators = new Dictionary<string, NoiseGenerator>();

        // Generation cache
        private readonly Dictionary<Vector2Int, TerrainChunk> _generatedChunks = new Dictionary<Vector2Int, TerrainChunk>();
        private readonly Dictionary<string, CreatureVariation> _creatureVariations = new Dictionary<string, CreatureVariation>();
        private readonly Dictionary<Vector2Int, BiomeData> _biomeCache = new Dictionary<Vector2Int, BiomeData>();

        // Statistics
        private int _totalChunksGenerated = 0;
        private int _totalCreaturesGenerated = 0;
        private int _cacheHits = 0;

        // Events
        public event Action<TerrainChunk> OnChunkGenerated;
        public event Action<CreatureVariation> OnCreatureGenerated;
        public event Action<Vector2Int, BiomeData> OnBiomeDetermined;

        #endregion

        #region Properties

        public static ProceduralGenerationSystem Instance => _instance;
        public int GlobalSeed => globalSeed;
        public int CachedChunkCount => _generatedChunks.Count;
        public int CreatureVariationCount => _creatureVariations.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[ProceduralGenerationSystem] Initializing...");

            // Initialize seed
            if (useSeedFromDateTime)
            {
                globalSeed = DateTime.Now.GetHashCode();
            }

            _random = new System.Random(globalSeed);

            // Initialize noise generators
            InitializeNoiseGenerators();

            Debug.Log($"[ProceduralGenerationSystem] Initialized with seed: {globalSeed}");
        }

        private void InitializeNoiseGenerators()
        {
            _noiseGenerators["elevation"] = new NoiseGenerator(globalSeed);
            _noiseGenerators["moisture"] = new NoiseGenerator(globalSeed + 1);
            _noiseGenerators["temperature"] = new NoiseGenerator(globalSeed + 2);
            _noiseGenerators["biome"] = new NoiseGenerator(globalSeed + 3);
            _noiseGenerators["detail"] = new NoiseGenerator(globalSeed + 4);
        }

        #endregion

        #region Terrain Generation

        /// <summary>
        /// Generate terrain chunk at coordinates.
        /// </summary>
        public TerrainChunk GenerateTerrainChunk(Vector2Int chunkCoords, int resolution = 33)
        {
            // Check cache
            if (_generatedChunks.TryGetValue(chunkCoords, out var cachedChunk))
            {
                _cacheHits++;
                return cachedChunk;
            }

            var chunk = new TerrainChunk
            {
                coordinates = chunkCoords,
                resolution = resolution,
                heightmap = new float[resolution, resolution],
                normalmap = new Vector3[resolution, resolution],
                biomes = new BiomeType[resolution, resolution]
            };

            // Generate heightmap
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 worldPos = new Vector2(
                        chunkCoords.x * (resolution - 1) + x,
                        chunkCoords.y * (resolution - 1) + y
                    );

                    chunk.heightmap[x, y] = GenerateHeight(worldPos);
                    chunk.biomes[x, y] = DetermineBiome(worldPos).type;
                }
            }

            // Calculate normals
            CalculateNormals(chunk);

            _generatedChunks[chunkCoords] = chunk;
            _totalChunksGenerated++;

            OnChunkGenerated?.Invoke(chunk);

            return chunk;
        }

        private float GenerateHeight(Vector2 worldPos)
        {
            var elevationNoise = _noiseGenerators["elevation"];

            float height = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;

            for (int i = 0; i < terrainOctaves; i++)
            {
                float sampleX = worldPos.x / terrainScale * frequency;
                float sampleY = worldPos.y / terrainScale * frequency;

                float noiseValue = elevationNoise.GetNoise(sampleX, sampleY);
                height += noiseValue * amplitude;

                maxValue += amplitude;

                amplitude *= terrainPersistence;
                frequency *= terrainLacunarity;
            }

            return height / maxValue;
        }

        private void CalculateNormals(TerrainChunk chunk)
        {
            int res = chunk.resolution;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float heightL = x > 0 ? chunk.heightmap[x - 1, y] : chunk.heightmap[x, y];
                    float heightR = x < res - 1 ? chunk.heightmap[x + 1, y] : chunk.heightmap[x, y];
                    float heightD = y > 0 ? chunk.heightmap[x, y - 1] : chunk.heightmap[x, y];
                    float heightU = y < res - 1 ? chunk.heightmap[x, y + 1] : chunk.heightmap[x, y];

                    Vector3 normal = new Vector3(heightL - heightR, 2f, heightD - heightU);
                    chunk.normalmap[x, y] = normal.normalized;
                }
            }
        }

        #endregion

        #region Biome Generation

        /// <summary>
        /// Determine biome at world position.
        /// </summary>
        public BiomeData DetermineBiome(Vector2 worldPos)
        {
            Vector2Int chunkCoord = new Vector2Int(
                Mathf.FloorToInt(worldPos.x / biomeScale),
                Mathf.FloorToInt(worldPos.y / biomeScale)
            );

            // Check cache
            if (_biomeCache.TryGetValue(chunkCoord, out var cachedBiome))
            {
                return cachedBiome;
            }

            float elevation = _noiseGenerators["elevation"].GetNoise(worldPos.x / terrainScale, worldPos.y / terrainScale);
            float moisture = _noiseGenerators["moisture"].GetNoise(worldPos.x / moistureScale, worldPos.y / moistureScale);
            float temperature = _noiseGenerators["temperature"].GetNoise(worldPos.x / temperatureScale, worldPos.y / temperatureScale);

            BiomeType biomeType = DetermineBiomeType(elevation, moisture, temperature);

            var biomeData = new BiomeData
            {
                type = biomeType,
                elevation = elevation,
                moisture = moisture,
                temperature = temperature
            };

            _biomeCache[chunkCoord] = biomeData;

            OnBiomeDetermined?.Invoke(chunkCoord, biomeData);

            return biomeData;
        }

        private BiomeType DetermineBiomeType(float elevation, float moisture, float temperature)
        {
            // Ocean
            if (elevation < 0.3f)
                return BiomeType.Ocean;

            // Beach
            if (elevation < 0.35f)
                return BiomeType.Beach;

            // Mountains
            if (elevation > 0.7f)
            {
                if (temperature < 0.3f)
                    return BiomeType.SnowyMountains;
                return BiomeType.Mountains;
            }

            // Temperature-based biomes
            if (temperature < 0.3f)
            {
                return BiomeType.Tundra;
            }
            else if (temperature > 0.7f)
            {
                if (moisture < 0.3f)
                    return BiomeType.Desert;
                else if (moisture > 0.6f)
                    return BiomeType.Rainforest;
                else
                    return BiomeType.Savanna;
            }
            else
            {
                if (moisture < 0.4f)
                    return BiomeType.Plains;
                else if (moisture > 0.6f)
                    return BiomeType.Forest;
                else
                    return BiomeType.Grassland;
            }
        }

        #endregion

        #region Creature Generation

        /// <summary>
        /// Generate unique creature variation.
        /// </summary>
        public CreatureVariation GenerateCreature(string speciesId, Vector2 position)
        {
            // Create variation ID from species + position
            string variationId = $"{speciesId}_{position.GetHashCode()}";

            // Check cache
            if (_creatureVariations.TryGetValue(variationId, out var cachedVariation))
            {
                _cacheHits++;
                return cachedVariation;
            }

            // Generate variation
            int seed = HashPosition(position) + speciesId.GetHashCode();
            var localRandom = new System.Random(seed);

            var variation = new CreatureVariation
            {
                variationId = variationId,
                speciesId = speciesId,
                position = position,
                seed = seed,

                // Physical attributes (randomized)
                scale = new Vector3(
                    (float)(0.8 + localRandom.NextDouble() * 0.4),
                    (float)(0.8 + localRandom.NextDouble() * 0.4),
                    (float)(0.8 + localRandom.NextDouble() * 0.4)
                ),

                // Color variation
                colorTint = new Color(
                    (float)localRandom.NextDouble(),
                    (float)localRandom.NextDouble(),
                    (float)localRandom.NextDouble(),
                    1f
                ),

                // Stats
                health = 50 + localRandom.Next(0, 50),
                strength = 5 + localRandom.Next(0, 10),
                speed = 3f + (float)(localRandom.NextDouble() * 3f),

                // Traits
                traits = GenerateTraits(localRandom)
            };

            _creatureVariations[variationId] = variation;
            _totalCreaturesGenerated++;

            OnCreatureGenerated?.Invoke(variation);

            return variation;
        }

        /// <summary>
        /// Generate creature offspring from two parents (genetic mixing).
        /// </summary>
        public CreatureVariation BreedCreatures(CreatureVariation parent1, CreatureVariation parent2, Vector2 position)
        {
            if (!useGeneticMixing)
            {
                return GenerateCreature(parent1.speciesId, position);
            }

            string variationId = $"offspring_{parent1.variationId}_{parent2.variationId}";

            int seed = parent1.seed ^ parent2.seed ^ position.GetHashCode();
            var localRandom = new System.Random(seed);

            var offspring = new CreatureVariation
            {
                variationId = variationId,
                speciesId = parent1.speciesId, // Same species
                position = position,
                seed = seed,

                // Inherit mixed traits
                scale = Vector3.Lerp(parent1.scale, parent2.scale, (float)localRandom.NextDouble()),
                colorTint = Color.Lerp(parent1.colorTint, parent2.colorTint, (float)localRandom.NextDouble()),

                // Stats from parents with mutation
                health = (parent1.health + parent2.health) / 2 + localRandom.Next(-10, 10),
                strength = (parent1.strength + parent2.strength) / 2 + localRandom.Next(-2, 2),
                speed = (parent1.speed + parent2.speed) / 2f + (float)(localRandom.NextDouble() * 1f - 0.5f),

                // Inherit and mutate traits
                traits = MixTraits(parent1.traits, parent2.traits, localRandom)
            };

            _creatureVariations[variationId] = offspring;
            _totalCreaturesGenerated++;

            OnCreatureGenerated?.Invoke(offspring);

            return offspring;
        }

        private string[] GenerateTraits(System.Random random)
        {
            var possibleTraits = new[] { "Aggressive", "Docile", "Fast", "Armored", "Venomous", "Camouflaged", "Pack Hunter", "Nocturnal" };
            int traitCount = random.Next(2, 5);
            var traits = new string[traitCount];

            for (int i = 0; i < traitCount; i++)
            {
                traits[i] = possibleTraits[random.Next(possibleTraits.Length)];
            }

            return traits;
        }

        private string[] MixTraits(string[] traits1, string[] traits2, System.Random random)
        {
            var combinedTraits = new List<string>(traits1);
            combinedTraits.AddRange(traits2);

            // Remove duplicates
            var uniqueTraits = new HashSet<string>(combinedTraits);

            // Randomly select subset
            var result = new List<string>();
            foreach (var trait in uniqueTraits)
            {
                if (random.NextDouble() > 0.5f)
                {
                    result.Add(trait);
                }
            }

            // Ensure at least 2 traits
            if (result.Count < 2 && uniqueTraits.Count >= 2)
            {
                result.Clear();
                var allTraits = new List<string>(uniqueTraits);
                result.Add(allTraits[random.Next(allTraits.Count)]);
                result.Add(allTraits[random.Next(allTraits.Count)]);
            }

            return result.ToArray();
        }

        #endregion

        #region Utility

        private int HashPosition(Vector2 position)
        {
            return (position.x.GetHashCode() * 397) ^ position.y.GetHashCode();
        }

        /// <summary>
        /// Clear generation cache.
        /// </summary>
        public void ClearCache()
        {
            _generatedChunks.Clear();
            _creatureVariations.Clear();
            _biomeCache.Clear();

            Debug.Log("[ProceduralGenerationSystem] Cache cleared");
        }

        /// <summary>
        /// Set new global seed.
        /// </summary>
        public void SetSeed(int seed)
        {
            globalSeed = seed;
            _random = new System.Random(seed);
            InitializeNoiseGenerators();
            ClearCache();

            Debug.Log($"[ProceduralGenerationSystem] Seed set to: {seed}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get procedural generation statistics.
        /// </summary>
        public ProceduralGenStats GetStats()
        {
            return new ProceduralGenStats
            {
                globalSeed = globalSeed,
                cachedChunks = _generatedChunks.Count,
                creatureVariations = _creatureVariations.Count,
                totalChunksGenerated = _totalChunksGenerated,
                totalCreaturesGenerated = _totalCreaturesGenerated,
                cacheHits = _cacheHits
            };
        }

        #endregion

        #region Context Menu

        [ContextMenu("Generate Test Chunk")]
        private void GenerateTestChunk()
        {
            GenerateTerrainChunk(Vector2Int.zero);
        }

        [ContextMenu("Generate Test Creature")]
        private void GenerateTestCreature()
        {
            GenerateCreature("test_species", Vector2.zero);
        }

        [ContextMenu("Clear Cache")]
        private void ClearCacheMenu()
        {
            ClearCache();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Procedural Generation Statistics ===\n" +
                      $"Global Seed: {stats.globalSeed}\n" +
                      $"Cached Chunks: {stats.cachedChunks}\n" +
                      $"Creature Variations: {stats.creatureVariations}\n" +
                      $"Total Chunks Generated: {stats.totalChunksGenerated}\n" +
                      $"Total Creatures Generated: {stats.totalCreaturesGenerated}\n" +
                      $"Cache Hits: {stats.cacheHits}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Terrain chunk data.
    /// </summary>
    [Serializable]
    public class TerrainChunk
    {
        public Vector2Int coordinates;
        public int resolution;
        public float[,] heightmap;
        public Vector3[,] normalmap;
        public BiomeType[,] biomes;
    }

    /// <summary>
    /// Biome data.
    /// </summary>
    [Serializable]
    public class BiomeData
    {
        public BiomeType type;
        public float elevation;
        public float moisture;
        public float temperature;
    }

    /// <summary>
    /// Creature variation.
    /// </summary>
    [Serializable]
    public class CreatureVariation
    {
        public string variationId;
        public string speciesId;
        public Vector2 position;
        public int seed;

        // Physical
        public Vector3 scale;
        public Color colorTint;

        // Stats
        public int health;
        public int strength;
        public float speed;

        // Traits
        public string[] traits;
    }

    /// <summary>
    /// Procedural generation statistics.
    /// </summary>
    [Serializable]
    public struct ProceduralGenStats
    {
        public int globalSeed;
        public int cachedChunks;
        public int creatureVariations;
        public int totalChunksGenerated;
        public int totalCreaturesGenerated;
        public int cacheHits;
    }

    /// <summary>
    /// Biome types.
    /// </summary>
    public enum BiomeType
    {
        Ocean,
        Beach,
        Plains,
        Grassland,
        Forest,
        Rainforest,
        Desert,
        Savanna,
        Mountains,
        SnowyMountains,
        Tundra
    }

    /// <summary>
    /// Simple Perlin noise generator.
    /// </summary>
    public class NoiseGenerator
    {
        private int seed;
        private int[] permutation;

        public NoiseGenerator(int seed)
        {
            this.seed = seed;
            InitializePermutation();
        }

        private void InitializePermutation()
        {
            permutation = new int[512];
            var p = new int[256];

            var random = new System.Random(seed);
            for (int i = 0; i < 256; i++)
            {
                p[i] = i;
            }

            // Shuffle
            for (int i = 0; i < 256; i++)
            {
                int j = random.Next(256);
                int temp = p[i];
                p[i] = p[j];
                p[j] = temp;
            }

            // Duplicate
            for (int i = 0; i < 256; i++)
            {
                permutation[i] = permutation[i + 256] = p[i];
            }
        }

        public float GetNoise(float x, float y)
        {
            // Simplified Perlin noise
            int X = (int)Mathf.Floor(x) & 255;
            int Y = (int)Mathf.Floor(y) & 255;

            x -= Mathf.Floor(x);
            y -= Mathf.Floor(y);

            float u = Fade(x);
            float v = Fade(y);

            int aa = permutation[permutation[X] + Y];
            int ab = permutation[permutation[X] + Y + 1];
            int ba = permutation[permutation[X + 1] + Y];
            int bb = permutation[permutation[X + 1] + Y + 1];

            float res = Mathf.Lerp(
                Mathf.Lerp(Grad(aa, x, y), Grad(ba, x - 1, y), u),
                Mathf.Lerp(Grad(ab, x, y - 1), Grad(bb, x - 1, y - 1), u),
                v
            );

            return (res + 1f) / 2f; // Normalize to 0-1
        }

        private float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private float Grad(int hash, float x, float y)
        {
            int h = hash & 15;
            float grad = 1 + (h & 7);
            if ((h & 8) != 0) grad = -grad;
            return (((h & 1) != 0) ? grad * x : 0) + (((h & 2) != 0) ? grad * y : 0);
        }
    }

    #endregion
}
