using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Core;
using Laboratory.Shared.Types;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// Bootstrap manager for setting up creatures in a scene.
    /// Drop this on an empty GameObject to create a creature testing environment.
    /// </summary>
    public class CreatureSceneBootstrap : MonoBehaviour
    {
        [Header("Scene Setup")]
        [SerializeField] private bool autoSpawnCreatures = true;
        [SerializeField] private int creatureCount = 5;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private Vector3 spawnCenter = Vector3.zero;
        
        [Header("Creature Configuration")]
        [SerializeField] private GameObject creaturePrefab;
        [SerializeField] private Laboratory.Chimera.Creatures.CreatureDefinition[] availableSpecies;

        [SerializeField] private bool mixWildAndDomestic = true;
        
        [Header("Environment")]
        [SerializeField] private BiomeType sceneBiome = BiomeType.Forest;
        [SerializeField] private bool enableECSSimulation = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugUI = true;
        [SerializeField] private bool showCreatureGizmos = true;
        
        private CreatureAuthoring[] spawnedCreatures;
        
        private void Start()
        {
            if (autoSpawnCreatures)
            {
                SpawnCreatures();
            }
        }
        
        /// <summary>
        /// Spawn creatures in the scene for testing
        /// </summary>
        [ContextMenu("Spawn Creatures")]
        public void SpawnCreatures()
        {
            if (availableSpecies == null || availableSpecies.Length == 0)
            {
                UnityEngine.Debug.LogError("[CreatureSceneBootstrap] No available species configured!");
                return;
            }
            
            spawnedCreatures = new CreatureAuthoring[creatureCount];
            
            for (int i = 0; i < creatureCount; i++)
            {
                Vector3 spawnPosition = spawnCenter + Random.insideUnitSphere * spawnRadius;
                spawnPosition.y = Mathf.Max(spawnPosition.y, 0f); // Keep above ground
                
                var creature = SpawnSingleCreature(spawnPosition, i);
                spawnedCreatures[i] = creature;
            }
            
            UnityEngine.Debug.Log($"[CreatureSceneBootstrap] Spawned {creatureCount} creatures");
        }
        
        /// <summary>
        /// Spawn a single creature at the specified position
        /// </summary>
        private CreatureAuthoring SpawnSingleCreature(Vector3 position, int index)
        {
            // Choose random species
            var creatureDefinition = availableSpecies[Random.Range(0, availableSpecies.Length)];
            
            // Create creature GameObject
            GameObject creatureGO;
            if (creaturePrefab != null)
            {
                creatureGO = Instantiate(creaturePrefab, position, Quaternion.identity);
            }
            else
            {
                creatureGO = new GameObject($"Creature_{creatureDefinition.speciesName}_{index}");
                creatureGO.transform.position = position;
                
                // Add basic components for visualization
                var meshRenderer = creatureGO.AddComponent<MeshRenderer>();
                var meshFilter = creatureGO.AddComponent<MeshFilter>();
                meshFilter.mesh = CreateSimpleCreatureMesh();
                
                // Create simple material
                var material = new Material(Shader.Find("Standard"));
                material.color = Color.white; // Default color
                meshRenderer.material = material;
                
                // Add collider for interaction
                var collider = creatureGO.AddComponent<CapsuleCollider>();
                collider.height = 2f; // Default height
                collider.radius = 0.6f; // Default radius
            }
            
            // Add CreatureAuthoring component
            var authoring = creatureGO.GetComponent<CreatureAuthoring>();
            if (authoring == null)
            {
                authoring = creatureGO.AddComponent<CreatureAuthoring>();
            }
            
            // Configure the creature
            ConfigureCreatureAuthoring(authoring, creatureDefinition, index);
            
            return authoring;
        }
        
        /// <summary>
        /// Configure a creature authoring component
        /// </summary>
        private void ConfigureCreatureAuthoring(CreatureAuthoring authoring, Laboratory.Chimera.Creatures.CreatureDefinition creatureDefinition, int index)
        {
            // Use reflection to set private fields (since they're SerializeField)
            var authoringType = typeof(CreatureAuthoring);
            
            // Set creature definition
            var definitionField = authoringType.GetField("creatureDefinition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            definitionField?.SetValue(authoring, creatureDefinition);
            
            // Set wild/domestic status
            var isWildField = authoringType.GetField("isWild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isWild = !mixWildAndDomestic || Random.value > 0.5f;
            isWildField?.SetValue(authoring, isWild);
            
            // Set biome
            var biomeField = authoringType.GetField("startingBiome", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            biomeField?.SetValue(authoring, sceneBiome);
            
            // Set ECS conversion
            var ecsField = authoringType.GetField("convertToECS", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ecsField?.SetValue(authoring, enableECSSimulation);
            
            // Set debug options
            var debugField = authoringType.GetField("enableDebugLogging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            debugField?.SetValue(authoring, enableDebugUI);
            
            var gizmosField = authoringType.GetField("showGizmosInScene", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            gizmosField?.SetValue(authoring, showCreatureGizmos);
        }
        
        /// <summary>
        /// Create a simple mesh for creature visualization
        /// </summary>
        private Mesh CreateSimpleCreatureMesh()
        {
            var mesh = new Mesh();
            mesh.name = "SimpleCreature";
            
            // Simple capsule-like shape
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),     // bottom center
                new Vector3(0.5f, 0, 0.5f),   // bottom corners
                new Vector3(-0.5f, 0, 0.5f),
                new Vector3(-0.5f, 0, -0.5f),
                new Vector3(0.5f, 0, -0.5f),
                new Vector3(0, 1, 0),     // top center
                new Vector3(0.3f, 1, 0.3f),   // top corners
                new Vector3(-0.3f, 1, 0.3f),
                new Vector3(-0.3f, 1, -0.3f),
                new Vector3(0.3f, 1, -0.3f)
            };
            
            int[] triangles = new int[]
            {
                // Bottom face
                0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4,
                // Top face  
                5, 6, 7, 5, 7, 8, 5, 8, 9, 5, 9, 6,
                // Side faces
                1, 2, 7, 1, 7, 6,
                2, 3, 8, 2, 8, 7,
                3, 4, 9, 3, 9, 8,
                4, 1, 6, 4, 6, 9
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        /// <summary>
        /// Clear all spawned creatures
        /// </summary>
        [ContextMenu("Clear Creatures")]
        public void ClearCreatures()
        {
            if (spawnedCreatures != null)
            {
                foreach (var creature in spawnedCreatures)
                {
                    if (creature != null && creature.gameObject != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(creature.gameObject);
                        }
                        else
                        {
                            DestroyImmediate(creature.gameObject);
                        }
                    }
                }
                spawnedCreatures = null;
            }
            
            // Also clear any other creatures in scene
            var allCreatures = FindObjectsByType<CreatureAuthoring>(FindObjectsSortMode.None);
            foreach (var creature in allCreatures)
            {
                if (Application.isPlaying)
                {
                    Destroy(creature.gameObject);
                }
                else
                {
                    DestroyImmediate(creature.gameObject);
                }
            }
            
            UnityEngine.Debug.Log("[CreatureSceneBootstrap] Cleared all creatures");
        }
        
        /// <summary>
        /// Get status of all creatures in the scene
        /// </summary>
        public string GetSceneStatus()
        {
            var creatures = FindObjectsByType<CreatureAuthoring>(FindObjectsSortMode.None);
            string status = $"Creatures in Scene: {creatures.Length}\n";
            status += $"ECS Simulation: {enableECSSimulation}\n";
            status += $"Scene Biome: {sceneBiome}\n\n";
            
            int wildCount = 0;
            int domesticCount = 0;
            int ecsCount = 0;
            
            foreach (var creature in creatures)
            {
                if (creature.CreatureInstance != null)
                {
                    if (creature.CreatureInstance.IsWild)
                        wildCount++;
                    else
                        domesticCount++;
                        
                    if (creature.IsConvertedToECS)
                        ecsCount++;
                }
            }
            
            status += $"Wild: {wildCount}, Domestic: {domesticCount}\n";
            status += $"ECS Entities: {ecsCount}";
            
            return status;
        }
        
        private void OnDrawGizmos()
        {
            if (!showCreatureGizmos) return;
            
            // Draw spawn area
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
            
            // Draw biome indicator
            Gizmos.color = GetBiomeColor(sceneBiome);
            Gizmos.DrawWireCube(spawnCenter + Vector3.up * 5f, Vector3.one);
        }
        
        private Color GetBiomeColor(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest => Color.green,
                BiomeType.Desert => Color.yellow,
                BiomeType.Ocean => Color.blue,
                BiomeType.Mountain => Color.gray,
                BiomeType.Tundra => Color.white,
                BiomeType.Swamp => new Color(0.4f, 0.6f, 0.3f),
                BiomeType.Volcanic => Color.red,
                BiomeType.Underground => Color.black,
                BiomeType.Sky => Color.cyan,
                _ => Color.magenta
            };
        }
    }
}