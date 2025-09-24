using UnityEngine;
using System;

#if UNITY_ENTITIES
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
#endif

namespace ProjectChimera.ECS
{
    /// <summary>
    /// Safe ECS utilities for Project Chimera to prevent common ECS errors
    /// Handles entity creation, component management, and system lifecycle
    /// Now with bulletproof package detection!
    /// </summary>
    public static class ChimeraECSUtilities
    {
        private static bool _ecsAvailable = false;
        private static bool _checkedECS = false;

        public static bool IsECSAvailable
        {
            get
            {
                if (!_checkedECS)
                {
                    #if UNITY_ENTITIES
                    _ecsAvailable = true;
                    Debug.Log("✅ Unity ECS is available");
                    #else
                    _ecsAvailable = false;
                    Debug.LogWarning("⚠️ Unity ECS not available - install Entities package for full ECS support");
                    #endif
                    _checkedECS = true;
                }
                return _ecsAvailable;
            }
        }

        #if UNITY_ENTITIES

        #region Entity Management

        /// <summary>
        /// Safely create an entity with error handling
        /// </summary>
        public static bool TryCreateEntity(EntityManager entityManager, out Entity entity, string debugName = "Unknown")
        {
            entity = Entity.Null;
            
            try
            {
                if (entityManager == null || !entityManager.IsCreated)
                {
                    Debug.LogError($"❌ EntityManager is null or destroyed for {debugName}");
                    return false;
                }

                entity = entityManager.CreateEntity();
                
                #if UNITY_EDITOR
                entityManager.SetName(entity, debugName);
                #endif
                
                Debug.Log($"✅ Created entity: {debugName} ({entity.Index})");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to create entity '{debugName}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely destroy an entity with validation
        /// </summary>
        public static bool TryDestroyEntity(EntityManager entityManager, Entity entity, string debugName = "Unknown")
        {
            try
            {
                if (entityManager == null || !entityManager.IsCreated)
                {
                    Debug.LogError($"❌ EntityManager is null or destroyed when trying to destroy {debugName}");
                    return false;
                }

                if (entity == Entity.Null)
                {
                    Debug.LogWarning($"⚠️ Trying to destroy null entity: {debugName}");
                    return false;
                }

                if (!entityManager.Exists(entity))
                {
                    Debug.LogWarning($"⚠️ Entity doesn't exist when trying to destroy: {debugName}");
                    return false;
                }

                entityManager.DestroyEntity(entity);
                Debug.Log($"🗑️ Destroyed entity: {debugName} ({entity.Index})");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to destroy entity '{debugName}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely check if entity exists and is valid
        /// </summary>
        public static bool IsEntityValid(EntityManager entityManager, Entity entity)
        {
            return entityManager != null && 
                   entityManager.IsCreated && 
                   entity != Entity.Null && 
                   entityManager.Exists(entity);
        }

        #endregion

        #region Component Management

        /// <summary>
        /// Safely add a component to an entity with error handling
        /// </summary>
        public static bool TryAddComponent<T>(EntityManager entityManager, Entity entity, T component, string debugName = "Unknown") 
            where T : unmanaged, IComponentData
        {
            try
            {
                if (!IsEntityValid(entityManager, entity))
                {
                    Debug.LogError($"❌ Invalid entity when adding {typeof(T).Name} to {debugName}");
                    return false;
                }

                if (entityManager.HasComponent<T>(entity))
                {
                    Debug.LogWarning($"⚠️ Entity {debugName} already has component {typeof(T).Name}");
                    entityManager.SetComponentData(entity, component);
                    return true;
                }

                entityManager.AddComponentData(entity, component);
                Debug.Log($"➕ Added {typeof(T).Name} to {debugName}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to add {typeof(T).Name} to {debugName}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely get a component from an entity
        /// </summary>
        public static bool TryGetComponent<T>(EntityManager entityManager, Entity entity, out T component, string debugName = "Unknown")
            where T : unmanaged, IComponentData
        {
            component = default(T);
            
            try
            {
                if (!IsEntityValid(entityManager, entity))
                {
                    Debug.LogError($"❌ Invalid entity when getting {typeof(T).Name} from {debugName}");
                    return false;
                }

                if (!entityManager.HasComponent<T>(entity))
                {
                    return false; // Not an error, just doesn't have the component
                }

                component = entityManager.GetComponentData<T>(entity);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to get {typeof(T).Name} from {debugName}: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Monster-Specific ECS Helpers

        /// <summary>
        /// Create a basic monster entity with essential components
        /// Perfect for Project Chimera's breeding system!
        /// </summary>
        public static Entity CreateMonsterEntity(EntityManager entityManager, float3 position, quaternion rotation, string monsterName = "Unknown Monster")
        {
            if (!TryCreateEntity(entityManager, out Entity monster, monsterName))
            {
                return Entity.Null;
            }

            try
            {
                // Add transform components (required for positioning)
                TryAddComponent(entityManager, monster, new Translation { Value = position }, monsterName);
                TryAddComponent(entityManager, monster, new Rotation { Value = rotation }, monsterName);

                // Add monster-specific components
                TryAddComponent(entityManager, monster, new MonsterData
                {
                    health = 100f,
                    maxHealth = 100f,
                    energy = 100f,
                    maxEnergy = 100f,
                    happiness = 50f,
                    breedingCooldown = 0f,
                    generation = 1,
                    speciesID = (uint)UnityEngine.Random.Range(1, 1000)
                }, monsterName);

                TryAddComponent(entityManager, monster, new MonsterStats
                {
                    strength = UnityEngine.Random.Range(1, 10),
                    intelligence = UnityEngine.Random.Range(1, 10),
                    agility = UnityEngine.Random.Range(1, 10),
                    vitality = UnityEngine.Random.Range(1, 10),
                    elementalAffinity = UnityEngine.Random.Range(0, 4),
                    size = 1f,
                    speed = 5f
                }, monsterName);

                Debug.Log($"🐲 Created monster entity: {monsterName} at position {position}");
                return monster;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to setup monster entity {monsterName}: {e.Message}");
                TryDestroyEntity(entityManager, monster, monsterName);
                return Entity.Null;
            }
        }

        #endregion

        #region Memory Management

        /// <summary>
        /// Safely dispose of a NativeArray with error handling
        /// </summary>
        public static void SafeDispose<T>(ref NativeArray<T> array, string arrayName = "NativeArray") where T : struct
        {
            try
            {
                if (array.IsCreated)
                {
                    array.Dispose();
                    Debug.Log($"🗑️ Disposed {arrayName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to dispose {arrayName}: {e.Message}");
            }
        }

        #endregion

        #else

        #region Fallback Non-ECS Methods

        // Fallback methods when ECS is not available
        public static bool TryCreateEntity(object entityManager, out object entity, string debugName = "Unknown")
        {
            entity = null;
            Debug.LogWarning($"⚠️ ECS not available - cannot create entity: {debugName}");
            return false;
        }

        public static object CreateMonsterEntity(object entityManager, Vector3 position, Quaternion rotation, string monsterName = "Unknown Monster")
        {
            Debug.LogWarning($"⚠️ ECS not available - cannot create monster entity: {monsterName}");
            return null;
        }

        #endregion

        #endif
    }

    #region Monster Component Definitions

    #if UNITY_ENTITIES

    /// <summary>
    /// Core monster data component for Project Chimera
    /// </summary>
    [System.Serializable]
    public struct MonsterData : IComponentData
    {
        public float health;
        public float maxHealth;
        public float energy;
        public float maxEnergy;
        public float happiness;
        public float breedingCooldown;
        public int generation;
        public uint speciesID;
    }

    /// <summary>
    /// Monster statistics for breeding and combat
    /// </summary>
    [System.Serializable]
    public struct MonsterStats : IComponentData
    {
        public int strength;
        public int intelligence;
        public int agility;
        public int vitality;
        public int elementalAffinity; // 0=Fire, 1=Water, 2=Earth, 3=Air
        public float size;
        public float speed;
    }

    /// <summary>
    /// Monster breeding information
    /// </summary>
    [System.Serializable]
    public struct BreedingData : IComponentData
    {
        public Entity parentA;
        public Entity parentB;
        public float fertilityRate;
        public float mutationChance;
        public bool isPregnant;
        public float gestationTime;
        public int expectedOffspring;
    }

    /// <summary>
    /// Monster ownership and networking data
    /// </summary>
    [System.Serializable]
    public struct NetworkedMonster : IComponentData
    {
        public ulong ownerClientID;
        public uint networkID;
        public bool isServerAuthoritative;
        public float lastSyncTime;
    }

    #region Example Systems

    /// <summary>
    /// Example system showing safe ECS patterns for Project Chimera
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class MonsterHealthSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            try
            {
                float deltaTime = Time.DeltaTime;

                // Safely iterate through monsters and update health
                Entities.ForEach((ref MonsterData monsterData) =>
                {
                    // Regenerate energy over time
                    monsterData.energy = math.min(monsterData.maxEnergy, monsterData.energy + deltaTime * 5f);
                    
                    // Decrease breeding cooldown
                    if (monsterData.breedingCooldown > 0)
                    {
                        monsterData.breedingCooldown = math.max(0, monsterData.breedingCooldown - deltaTime);
                    }
                    
                    // Happiness affects health regeneration
                    if (monsterData.happiness > 75f)
                    {
                        monsterData.health = math.min(monsterData.maxHealth, monsterData.health + deltaTime * 2f);
                    }
                    
                }).Run();

            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ MonsterHealthSystem update failed: {e.Message}");
                Enabled = false; // Disable system to prevent spam
            }
        }
    }

    #endregion

    #endif

    #endregion

    #region Non-ECS Monster Management

    /// <summary>
    /// Traditional MonoBehaviour-based monster for when ECS is not available
    /// </summary>
    [System.Serializable]
    public class MonsterDataMono
    {
        public float health = 100f;
        public float maxHealth = 100f;
        public float energy = 100f;
        public float maxEnergy = 100f;
        public float happiness = 50f;
        public float breedingCooldown = 0f;
        public int generation = 1;
        public uint speciesID;
    }

    /// <summary>
    /// Traditional MonoBehaviour monster component
    /// </summary>
    public class MonsterComponent : MonoBehaviour
    {
        [Header("🐲 Monster Data")]
        public MonsterDataMono monsterData = new MonsterDataMono();
        
        [Header("📊 Monster Stats")]
        public int strength = 5;
        public int intelligence = 5;
        public int agility = 5;
        public int vitality = 5;
        public int elementalAffinity = 0; // 0=Fire, 1=Water, 2=Earth, 3=Air
        public float size = 1f;
        public float speed = 5f;

        void Start()
        {
            // Initialize monster with random stats
            strength = UnityEngine.Random.Range(1, 10);
            intelligence = UnityEngine.Random.Range(1, 10);
            agility = UnityEngine.Random.Range(1, 10);
            vitality = UnityEngine.Random.Range(1, 10);
            elementalAffinity = UnityEngine.Random.Range(0, 4);
            
            monsterData.speciesID = (uint)UnityEngine.Random.Range(1, 1000);
            
            Debug.Log($"🐲 Traditional monster initialized: {gameObject.name}");
        }

        void Update()
        {
            // Update monster data over time
            float deltaTime = Time.deltaTime;
            
            // Regenerate energy
            monsterData.energy = Mathf.Min(monsterData.maxEnergy, monsterData.energy + deltaTime * 5f);
            
            // Decrease breeding cooldown
            if (monsterData.breedingCooldown > 0)
            {
                monsterData.breedingCooldown = Mathf.Max(0, monsterData.breedingCooldown - deltaTime);
            }
            
            // Happiness affects health regeneration
            if (monsterData.happiness > 75f)
            {
                monsterData.health = Mathf.Min(monsterData.maxHealth, monsterData.health + deltaTime * 2f);
            }
        }

        public void TakeDamage(float damage)
        {
            monsterData.health = Mathf.Max(0, monsterData.health - damage);
            if (monsterData.health <= 0)
            {
                Debug.Log($"💀 Monster {gameObject.name} has died");
                // Handle death
            }
        }

        public bool CanBreed()
        {
            return monsterData.health > 50f && 
                   monsterData.energy > 25f && 
                   monsterData.breedingCooldown <= 0f;
        }
    }

    #endregion
}
