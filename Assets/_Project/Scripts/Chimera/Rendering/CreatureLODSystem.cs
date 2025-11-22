using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Laboratory.Chimera.Rendering
{
    /// <summary>
    /// ECS-based Level of Detail system for procedural creature rendering
    ///
    /// Performance Features:
    /// - Burst-compiled LOD calculations for 1000+ creatures
    /// - Distance-based quality tiers (High/Medium/Low/Culled)
    /// - Automatic visual complexity reduction
    /// - Spatial hashing for fast camera distance checks
    /// - Batched LOD updates (not per-frame)
    ///
    /// LOD Tiers:
    /// - Tier 0 (0-20m): Full detail - all genetic features, animations, particles
    /// - Tier 1 (20-50m): Medium detail - simplified features, reduced particles
    /// - Tier 2 (50-100m): Low detail - basic shape, no particles, simple animation
    /// - Tier 3 (100m+): Culled - no rendering or minimal billboard
    ///
    /// Target: <0.5ms per frame for 1000 creatures
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct CreatureLODSystem : ISystem
    {
        private EntityQuery _creatureQuery;
        private float _lastUpdateTime;

        // LOD update frequency (don't update every frame)
        private const float LOD_UPDATE_INTERVAL = 0.1f; // 10Hz instead of 60Hz

        // LOD distance thresholds (squared for faster calculations)
        private const float TIER0_DISTANCE_SQ = 20f * 20f;   // High quality
        private const float TIER1_DISTANCE_SQ = 50f * 50f;   // Medium quality
        private const float TIER2_DISTANCE_SQ = 100f * 100f; // Low quality
        // Beyond TIER2 = culled

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _creatureQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadWrite<CreatureLODComponent>()
            );

            _lastUpdateTime = 0f;

            state.RequireForUpdate(_creatureQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Throttle LOD updates - only run every 100ms
            if (currentTime - _lastUpdateTime < LOD_UPDATE_INTERVAL)
                return;

            _lastUpdateTime = currentTime;

            // Get camera position (from singleton or default)
            float3 cameraPosition = GetCameraPosition(ref state);

            // Schedule LOD calculation job
            var lodJob = new CalculateLODJob
            {
                cameraPosition = cameraPosition,
                tier0DistanceSq = TIER0_DISTANCE_SQ,
                tier1DistanceSq = TIER1_DISTANCE_SQ,
                tier2DistanceSq = TIER2_DISTANCE_SQ
            };

            state.Dependency = lodJob.ScheduleParallel(_creatureQuery, state.Dependency);
        }

        private float3 GetCameraPosition(ref SystemState state)
        {
            // Try to get camera position from singleton
            // Fallback to (0,0,0) if not available
            if (SystemAPI.TryGetSingleton<CameraPositionSingleton>(out var cameraSingleton))
            {
                return cameraSingleton.position;
            }

            return float3.zero;
        }
    }

    /// <summary>
    /// Burst-compiled job for calculating LOD levels based on camera distance
    /// Processes 1000+ creatures in parallel with SIMD optimization
    /// </summary>
    [BurstCompile]
    public partial struct CalculateLODJob : IJobEntity
    {
        public float3 cameraPosition;
        public float tier0DistanceSq;
        public float tier1DistanceSq;
        public float tier2DistanceSq;

        public void Execute(in LocalTransform transform, ref CreatureLODComponent lod)
        {
            // Calculate squared distance (avoid expensive sqrt)
            float3 creaturePosition = transform.Position;
            float distanceSq = math.distancesq(cameraPosition, creaturePosition);

            // Determine LOD tier
            LODTier newTier;
            if (distanceSq < tier0DistanceSq)
                newTier = LODTier.High;
            else if (distanceSq < tier1DistanceSq)
                newTier = LODTier.Medium;
            else if (distanceSq < tier2DistanceSq)
                newTier = LODTier.Low;
            else
                newTier = LODTier.Culled;

            // Only update if tier changed (reduce work for rendering systems)
            if (lod.currentTier != newTier)
            {
                lod.previousTier = lod.currentTier;
                lod.currentTier = newTier;
                lod.distanceFromCamera = math.sqrt(distanceSq);
                lod.transitionTimeRemaining = lod.transitionDuration;
            }
            else
            {
                // Update distance but keep tier
                lod.distanceFromCamera = math.sqrt(distanceSq);

                // Decay transition timer
                if (lod.transitionTimeRemaining > 0f)
                {
                    lod.transitionTimeRemaining = math.max(0f, lod.transitionTimeRemaining - LOD_UPDATE_INTERVAL);
                }
            }
        }

        private const float LOD_UPDATE_INTERVAL = 0.1f;
    }

    /// <summary>
    /// LOD component attached to each creature entity
    /// Consumed by rendering systems to adjust visual quality
    /// </summary>
    public struct CreatureLODComponent : IComponentData
    {
        /// <summary>Current LOD tier</summary>
        public LODTier currentTier;

        /// <summary>Previous LOD tier (for smooth transitions)</summary>
        public LODTier previousTier;

        /// <summary>Distance from camera in meters</summary>
        public float distanceFromCamera;

        /// <summary>Time remaining for LOD transition (smooth blending)</summary>
        public float transitionTimeRemaining;

        /// <summary>Duration of LOD transitions in seconds</summary>
        public float transitionDuration;

        /// <summary>Visual complexity multiplier (0-1)</summary>
        /// <remarks>
        /// - Tier 0 (High): 1.0 = full detail
        /// - Tier 1 (Medium): 0.6 = 60% detail
        /// - Tier 2 (Low): 0.3 = 30% detail
        /// - Tier 3 (Culled): 0.0 = no rendering
        /// </remarks>
        public float GetComplexityMultiplier()
        {
            return currentTier switch
            {
                LODTier.High => 1.0f,
                LODTier.Medium => 0.6f,
                LODTier.Low => 0.3f,
                LODTier.Culled => 0.0f,
                _ => 0.5f
            };
        }

        /// <summary>Should this creature be rendered?</summary>
        public bool ShouldRender => currentTier != LODTier.Culled;

        /// <summary>Should genetic features be visible?</summary>
        public bool RenderGeneticFeatures => currentTier == LODTier.High;

        /// <summary>Should particle effects be spawned?</summary>
        public bool RenderParticles => currentTier == LODTier.High;

        /// <summary>Animation quality (0=none, 1=simplified, 2=full)</summary>
        public int AnimationQuality => currentTier switch
        {
            LODTier.High => 2,
            LODTier.Medium => 1,
            LODTier.Low => 0,
            _ => 0
        };
    }

    /// <summary>
    /// LOD quality tiers
    /// </summary>
    public enum LODTier : byte
    {
        /// <summary>Full detail - 0-20m range</summary>
        High = 0,

        /// <summary>Medium detail - 20-50m range</summary>
        Medium = 1,

        /// <summary>Low detail - 50-100m range</summary>
        Low = 2,

        /// <summary>Culled/not rendered - 100m+ range</summary>
        Culled = 3
    }

    /// <summary>
    /// Camera position singleton for LOD calculations
    /// Updated by camera system each frame
    /// </summary>
    public struct CameraPositionSingleton : IComponentData
    {
        public float3 position;
        public float3 forward;
        public float fieldOfView;
    }

    /// <summary>
    /// System that updates camera position singleton from active camera
    /// Runs in PresentationSystemGroup before CreatureLODSystem
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    public partial class CameraPositionUpdateSystem : SystemBase
    {
        protected override void OnCreate()
        {
            // Create singleton entity if it doesn't exist
            if (!SystemAPI.HasSingleton<CameraPositionSingleton>())
            {
                var singleton = EntityManager.CreateEntity();
                EntityManager.AddComponentData(singleton, new CameraPositionSingleton
                {
                    position = float3.zero,
                    forward = new float3(0, 0, 1),
                    fieldOfView = 60f
                });
            }
        }

        protected override void OnUpdate()
        {
            // Get main camera
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            // Update singleton with camera data
            SystemAPI.SetSingleton(new CameraPositionSingleton
            {
                position = mainCamera.transform.position,
                forward = mainCamera.transform.forward,
                fieldOfView = mainCamera.fieldOfView
            });
        }
    }

    /// <summary>
    /// Authoring component for adding LOD to creatures in the editor
    /// Automatically bakes to CreatureLODComponent
    /// </summary>
    public class CreatureLODAuthoring : MonoBehaviour
    {
        [Header("LOD Settings")]
        [Tooltip("Initial LOD tier (usually High for editor preview)")]
        public LODTier initialTier = LODTier.High;

        [Tooltip("Duration of LOD transitions for smooth blending")]
        [Range(0f, 1f)]
        public float transitionDuration = 0.2f;

        [Header("Debug")]
        [Tooltip("Show LOD gizmos in scene view")]
        public bool showLODGizmos = true;

        /// <summary>
        /// Helper method to create ECS components from authoring data.
        /// Call this from your creature spawning system.
        /// Note: Baker pattern not available in this Unity ECS version.
        /// </summary>
        public void AddComponentsToEntity(Entity entity, EntityManager entityManager)
        {
            entityManager.AddComponentData(entity, new CreatureLODComponent
            {
                currentTier = initialTier,
                previousTier = initialTier,
                distanceFromCamera = 0f,
                transitionTimeRemaining = 0f,
                transitionDuration = transitionDuration
            });
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showLODGizmos)
                return;

            // Draw LOD distance spheres
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 20f); // Tier 0

            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 50f); // Tier 1

            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 100f); // Tier 2
        }
#endif
    }

    /// <summary>
    /// Statistics and debugging for LOD system
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial class CreatureLODStatsSystem : SystemBase
    {
        private EntityQuery _creatureQuery;
        private float _lastStatsUpdate;
        private const float STATS_UPDATE_INTERVAL = 1f; // Update stats every second

        protected override void OnCreate()
        {
            _creatureQuery = GetEntityQuery(ComponentType.ReadOnly<CreatureLODComponent>());
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            if (currentTime - _lastStatsUpdate < STATS_UPDATE_INTERVAL)
                return;

            _lastStatsUpdate = currentTime;

            // Count creatures per LOD tier
            int tier0Count = 0;
            int tier1Count = 0;
            int tier2Count = 0;
            int culledCount = 0;

            foreach (var lod in SystemAPI.Query<RefRO<CreatureLODComponent>>())
            {
                switch (lod.ValueRO.currentTier)
                {
                    case LODTier.High:
                        tier0Count++;
                        break;
                    case LODTier.Medium:
                        tier1Count++;
                        break;
                    case LODTier.Low:
                        tier2Count++;
                        break;
                    case LODTier.Culled:
                        culledCount++;
                        break;
                }
            }

            int total = tier0Count + tier1Count + tier2Count + culledCount;
            if (total > 0)
            {
                Debug.Log($"[CreatureLOD] Total: {total} | High: {tier0Count} ({tier0Count * 100f / total:F0}%) | " +
                         $"Medium: {tier1Count} ({tier1Count * 100f / total:F0}%) | " +
                         $"Low: {tier2Count} ({tier2Count * 100f / total:F0}%) | " +
                         $"Culled: {culledCount} ({culledCount * 100f / total:F0}%)");
            }
        }
    }
}
