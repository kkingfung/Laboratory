using Unity.Entities;
using UnityEngine;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// POPULATION MANAGEMENT HELPER
    ///
    /// Utility methods for interacting with the population management system
    /// Makes it easy for other systems to check capacity, acquire chimeras, etc.
    ///
    /// Usage Examples:
    /// - Can player acquire chimera? → CanAcquireChimera(em, playerEntity)
    /// - Acquire chimera → RequestChimeraAcquisition(em, playerEntity, chimeraEntity, method)
    /// - Release chimera → RequestChimeraRelease(em, playerEntity, chimeraEntity, reason)
    /// - Check capacity → GetCurrentCapacity(em, playerEntity)
    /// </summary>
    public static class PopulationManagementHelper
    {
        /// <summary>
        /// Gets the player's population capacity data
        /// </summary>
        public static ChimeraPopulationCapacity? GetPopulationCapacity(EntityManager em)
        {
            // For single-player, get the singleton capacity
            var query = em.CreateEntityQuery(typeof(ChimeraPopulationCapacity));
            if (query.CalculateEntityCount() > 0)
            {
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                if (entities.Length > 0)
                {
                    var capacity = em.GetComponentData<ChimeraPopulationCapacity>(entities[0]);
                    entities.Dispose();
                    return capacity;
                }
                entities.Dispose();
            }

            return null;
        }

        /// <summary>
        /// Gets the player entity that owns the population capacity
        /// </summary>
        public static Entity GetPlayerEntity(EntityManager em)
        {
            var query = em.CreateEntityQuery(typeof(ChimeraPopulationCapacity));
            if (query.CalculateEntityCount() > 0)
            {
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                if (entities.Length > 0)
                {
                    var playerEntity = entities[0];
                    entities.Dispose();
                    return playerEntity;
                }
                entities.Dispose();
            }

            return Entity.Null;
        }

        /// <summary>
        /// Checks if player can acquire a new chimera (has capacity available)
        /// </summary>
        public static bool CanAcquireChimera(EntityManager em)
        {
            var capacity = GetPopulationCapacity(em);
            if (!capacity.HasValue)
                return true; // No capacity system = unlimited (for backward compatibility)

            return capacity.Value.currentCapacity < capacity.Value.maxCapacity;
        }

        /// <summary>
        /// Gets current capacity status as string for UI display
        /// </summary>
        public static string GetCapacityStatus(EntityManager em)
        {
            var capacity = GetPopulationCapacity(em);
            if (!capacity.HasValue)
                return "Capacity: Unlimited";

            return $"Chimeras: {capacity.Value.currentCapacity}/{capacity.Value.maxCapacity}";
        }

        /// <summary>
        /// Gets unlock progress status for UI display
        /// </summary>
        public static string GetUnlockProgress(EntityManager em)
        {
            var capacity = GetPopulationCapacity(em);
            if (!capacity.HasValue)
                return "";

            if (capacity.Value.capacityUnlocked >= 5)
                return "Max capacity unlocked!";

            if (capacity.Value.canUnlockNext)
                return $"Ready to unlock slot {capacity.Value.capacityUnlocked + 1}!";

            return $"Need {capacity.Value.strongBondsRequired} chimeras with {capacity.Value.bondStrengthRequired:P0} bond " +
                   $"to unlock slot {capacity.Value.capacityUnlocked + 1}";
        }

        /// <summary>
        /// Requests to acquire a new chimera
        /// </summary>
        public static void RequestChimeraAcquisition(
            EntityManager em,
            Entity chimeraEntity,
            AcquisitionMethod method,
            float currentTime)
        {
            var playerEntity = GetPlayerEntity(em);
            if (playerEntity == Entity.Null)
            {
                UnityEngine.Debug.LogError("Cannot acquire chimera: No player entity found!");
                return;
            }

            if (!CanAcquireChimera(em))
            {
                UnityEngine.Debug.LogWarning("Cannot acquire chimera: At capacity!");
                return;
            }

            // Create acquisition request
            var requestEntity = em.CreateEntity();
            em.AddComponentData(requestEntity, new ChimeraAcquisitionRequest
            {
                playerEntity = playerEntity,
                chimeraEntity = chimeraEntity,
                method = method,
                requestTime = currentTime
            });

            UnityEngine.Debug.Log($"Chimera acquisition requested (Method: {method})");
        }

        /// <summary>
        /// Requests to release a chimera
        /// </summary>
        public static void RequestChimeraRelease(
            EntityManager em,
            Entity chimeraEntity,
            ReleaseReason reason,
            bool isTemporary,
            float currentTime)
        {
            var playerEntity = GetPlayerEntity(em);
            if (playerEntity == Entity.Null)
            {
                UnityEngine.Debug.LogError("Cannot release chimera: No player entity found!");
                return;
            }

            // Create release request
            var requestEntity = em.CreateEntity();
            em.AddComponentData(requestEntity, new ChimeraReleaseRequest
            {
                playerEntity = playerEntity,
                chimeraEntity = chimeraEntity,
                reason = reason,
                isTemporary = isTemporary,
                requestTime = currentTime
            });

            if (!isTemporary)
            {
                UnityEngine.Debug.LogWarning($"Chimera release requested - PERMANENT CAPACITY REDUCTION! (Reason: {reason})");
            }
            else
            {
                UnityEngine.Debug.Log($"Chimera temporary rehoming requested (Reason: {reason})");
            }
        }

        /// <summary>
        /// Gets capacity reduction warning for UI
        /// </summary>
        public static string GetCapacityReductionWarning()
        {
            return "WARNING: Sending this chimera away will PERMANENTLY reduce your maximum capacity.\n\n" +
                   "You will NEVER be able to get this slot back.\n\n" +
                   "Are you absolutely sure?";
        }

        /// <summary>
        /// Quick helpers for common operations
        /// </summary>
        public static class QuickActions
        {
            public static void AcquireBreedingChimera(EntityManager em, Entity chimeraEntity, float time)
            {
                RequestChimeraAcquisition(em, chimeraEntity, AcquisitionMethod.Breeding, time);
            }

            public static void AcquireHatchedChimera(EntityManager em, Entity chimeraEntity, float time)
            {
                RequestChimeraAcquisition(em, chimeraEntity, AcquisitionMethod.Hatched, time);
            }

            public static void AcquireRescuedChimera(EntityManager em, Entity chimeraEntity, float time)
            {
                RequestChimeraAcquisition(em, chimeraEntity, AcquisitionMethod.Rescued, time);
            }

            public static void ReleaseChimera(EntityManager em, Entity chimeraEntity, float time)
            {
                RequestChimeraRelease(em, chimeraEntity, ReleaseReason.PlayerChoice, isTemporary: false, time);
            }

            public static void RehomeChimera(EntityManager em, Entity chimeraEntity, float time)
            {
                RequestChimeraRelease(em, chimeraEntity, ReleaseReason.Rehoming, isTemporary: true, time);
            }
        }

        /// <summary>
        /// Debugging helpers
        /// </summary>
        public static class Debug
        {
            public static void PrintCapacityStatus(EntityManager em)
            {
                var capacity = GetPopulationCapacity(em);
                if (!capacity.HasValue)
                {
                    UnityEngine.Debug.Log("No population capacity data found");
                    return;
                }

                var c = capacity.Value;
                UnityEngine.Debug.Log($"=== POPULATION CAPACITY STATUS ===\n" +
                    $"Current: {c.currentCapacity}/{c.maxCapacity} chimeras\n" +
                    $"Unlocked: {c.capacityUnlocked}/5 slots\n" +
                    $"Lost Permanently: {c.capacityLostPermanently} slots\n" +
                    $"Can Unlock Next: {c.canUnlockNext}\n" +
                    $"Next Requirements: {c.strongBondsRequired} bonds at {c.bondStrengthRequired:P0}\n" +
                    $"Lifetime Total: {c.totalChimerasEverOwned} owned\n" +
                    $"Sent Away: {c.totalChimerasSentAway}\n" +
                    $"Natural Deaths: {c.totalChimerasNaturalDeath}");
            }

            public static void PrintBondTrackers(EntityManager em)
            {
                var playerEntity = GetPlayerEntity(em);
                if (playerEntity == Entity.Null || !em.HasBuffer<ChimeraBondTracker>(playerEntity))
                {
                    UnityEngine.Debug.Log("No bond trackers found");
                    return;
                }

                var buffer = em.GetBuffer<ChimeraBondTracker>(playerEntity);
                UnityEngine.Debug.Log($"=== BOND TRACKERS ({buffer.Length}) ===");

                for (int i = 0; i < buffer.Length; i++)
                {
                    var tracker = buffer[i];
                    UnityEngine.Debug.Log($"  [{i}] {tracker.chimeraName}: {tracker.bondStrength:P0} " +
                        $"(Counts: {tracker.countsForCapacity})");
                }
            }
        }
    }
}
