using Unity.Entities;
using UnityEngine;
using Laboratory.Chimera.Consciousness.Core;
using Laboratory.Chimera.Progression;
using Laboratory.Chimera.Activities;
using Laboratory.Chimera.Equipment;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Social;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Integration
{
    /// <summary>
    /// CHIMERA SYSTEMS INTEGRATION
    ///
    /// Central integration layer connecting all chimera systems
    ///
    /// This class provides unified APIs for interacting with the complete chimera system:
    /// - Partnership progression (Phase 1)
    /// - Age-based bonding (Phase 2)
    /// - Life stages (Phase 3)
    /// - Personality stability (Phase 3.5)
    /// - Emotional indicators (Phase 4)
    /// - Population management (Phase 5)
    /// - Personality equipment (Phase 6)
    /// - Partnership activities (Phase 7)
    /// - Personality genetics (Phase 8)
    ///
    /// Design Philosophy:
    /// "One entry point for all chimera interactions - complexity hidden, power exposed"
    /// </summary>
    public static class ChimeraSystemsIntegration
    {
        // ===== PARTNERSHIP OPERATIONS (Phase 1) =====

        /// <summary>
        /// Gets partnership skill data for a chimera
        /// </summary>
        public static PartnershipSkillComponent? GetPartnershipSkill(EntityManager em, Entity chimeraEntity)
        {
            if (!em.Exists(chimeraEntity) || !em.HasComponent<PartnershipSkillComponent>(chimeraEntity))
                return null;

            return em.GetComponentData<PartnershipSkillComponent>(chimeraEntity);
        }

        /// <summary>
        /// Gets current cooperation level (0.0-1.2)
        /// </summary>
        public static float GetCooperationLevel(EntityManager em, Entity chimeraEntity)
        {
            var skill = GetPartnershipSkill(em, chimeraEntity);
            return skill?.cooperationLevel ?? 0.5f;
        }

        /// <summary>
        /// Gets mastery for a specific activity genre (0.0-1.0)
        /// </summary>
        public static float GetActivityMastery(EntityManager em, Entity chimeraEntity, ActivityGenreCategory genre)
        {
            var skill = GetPartnershipSkill(em, chimeraEntity);
            if (!skill.HasValue) return 0f;

            return genre switch
            {
                ActivityGenreCategory.Action => skill.Value.actionMastery,
                ActivityGenreCategory.Strategy => skill.Value.strategyMastery,
                ActivityGenreCategory.Puzzle => skill.Value.puzzleMastery,
                ActivityGenreCategory.Racing => skill.Value.racingMastery,
                ActivityGenreCategory.Rhythm => skill.Value.rhythmMastery,
                ActivityGenreCategory.Exploration => skill.Value.explorationMastery,
                ActivityGenreCategory.Economics => skill.Value.economicsMastery,
                _ => 0f
            };
        }

        // ===== LIFE STAGE & AGE OPERATIONS (Phases 2-3) =====

        /// <summary>
        /// Gets current life stage
        /// </summary>
        public static LifeStage GetLifeStage(EntityManager em, Entity chimeraEntity)
        {
            if (!em.Exists(chimeraEntity) || !em.HasComponent<CreatureIdentityComponent>(chimeraEntity))
                return LifeStage.Adult;

            return em.GetComponentData<CreatureIdentityComponent>(chimeraEntity).CurrentLifeStage;
        }

        /// <summary>
        /// Gets age percentage (0.0-1.0 of lifespan)
        /// </summary>
        public static float GetAgePercentage(EntityManager em, Entity chimeraEntity)
        {
            if (!em.Exists(chimeraEntity) || !em.HasComponent<CreatureIdentityComponent>(chimeraEntity))
                return 0.5f;

            return em.GetComponentData<CreatureIdentityComponent>(chimeraEntity).AgePercentage;
        }

        /// <summary>
        /// Checks if chimera is elderly (special personality mechanics)
        /// </summary>
        public static bool IsElderly(EntityManager em, Entity chimeraEntity)
        {
            return GetLifeStage(em, chimeraEntity) == LifeStage.Elderly;
        }

        /// <summary>
        /// Gets age-based emotional sensitivity (baby=high, elderly=profound)
        /// </summary>
        public static float GetEmotionalSensitivity(EntityManager em, Entity chimeraEntity)
        {
            return GetLifeStage(em, chimeraEntity) switch
            {
                LifeStage.Baby => 2.0f,      // Extra sensitive
                LifeStage.Child => 1.5f,     // Very sensitive
                LifeStage.Teen => 1.2f,      // Heightened sensitivity
                LifeStage.Adult => 1.0f,     // Standard sensitivity
                LifeStage.Elderly => 0.8f,   // Less reactive but deeply affected
                _ => 1.0f
            };
        }

        // ===== PERSONALITY OPERATIONS (Phases 3.5 & 8) =====

        /// <summary>
        /// Gets current personality
        /// </summary>
        public static CreaturePersonality? GetPersonality(EntityManager em, Entity chimeraEntity)
        {
            if (!em.Exists(chimeraEntity) || !em.HasComponent<CreaturePersonality>(chimeraEntity))
                return null;

            return em.GetComponentData<CreaturePersonality>(chimeraEntity);
        }

        /// <summary>
        /// Gets genetic personality (inherited from parents)
        /// </summary>
        public static PersonalityGeneticComponent? GetGeneticPersonality(EntityManager em, Entity chimeraEntity)
        {
            if (!em.Exists(chimeraEntity) || !em.HasComponent<PersonalityGeneticComponent>(chimeraEntity))
                return null;

            return em.GetComponentData<PersonalityGeneticComponent>(chimeraEntity);
        }

        /// <summary>
        /// Gets personality malleability (how resistant to change)
        /// </summary>
        public static float GetPersonalityMalleability(EntityManager em, Entity chimeraEntity)
        {
            if (!em.Exists(chimeraEntity) || !em.HasComponent<PersonalityStabilityComponent>(chimeraEntity))
                return 0.5f;

            return em.GetComponentData<PersonalityStabilityComponent>(chimeraEntity).personalityMalleability;
        }

        /// <summary>
        /// Checks if personality baseline is locked (elderly chimeras)
        /// </summary>
        public static bool HasLockedBaseline(EntityManager em, Entity chimeraEntity)
        {
            if (!em.Exists(chimeraEntity) || !em.HasComponent<PersonalityStabilityComponent>(chimeraEntity))
                return false;

            return em.GetComponentData<PersonalityStabilityComponent>(chimeraEntity).hasLockedBaseline;
        }

        // ===== EMOTIONAL OPERATIONS (Phase 4) =====

        /// <summary>
        /// Gets current emotional indicator
        /// </summary>
        public static EmotionalIndicatorComponent? GetEmotionalIndicator(EntityManager em, Entity chimeraEntity)
        {
            if (!em.Exists(chimeraEntity) || !em.HasComponent<EmotionalIndicatorComponent>(chimeraEntity))
                return null;

            return em.GetComponentData<EmotionalIndicatorComponent>(chimeraEntity);
        }

        /// <summary>
        /// Gets current emotion emoji for UI display
        /// </summary>
        public static string GetEmotionEmoji(EntityManager em, Entity chimeraEntity)
        {
            var indicator = GetEmotionalIndicator(em, chimeraEntity);
            if (!indicator.HasValue) return "😐";

            return EmotionalIconMapper.GetEmoji(indicator.Value.currentIcon);
        }

        /// <summary>
        /// Gets emotion description for UI
        /// </summary>
        public static string GetEmotionDescription(EntityManager em, Entity chimeraEntity)
        {
            var indicator = GetEmotionalIndicator(em, chimeraEntity);
            if (!indicator.HasValue) return "Neutral";

            return EmotionalIconMapper.GetDescription(indicator.Value.currentIcon);
        }

        // ===== POPULATION MANAGEMENT (Phase 5) =====

        /// <summary>
        /// Checks if player can acquire a new chimera
        /// </summary>
        public static bool CanAcquireChimera(EntityManager em)
        {
            return PopulationManagementHelper.CanAcquireChimera(em);
        }

        /// <summary>
        /// Gets capacity status string for UI
        /// </summary>
        public static string GetCapacityStatus(EntityManager em)
        {
            return PopulationManagementHelper.GetCapacityStatus(em);
        }

        /// <summary>
        /// Gets unlock progress for next capacity slot
        /// </summary>
        public static string GetUnlockProgress(EntityManager em)
        {
            return PopulationManagementHelper.GetUnlockProgress(em);
        }

        /// <summary>
        /// Requests to acquire a chimera (checks capacity)
        /// </summary>
        public static void AcquireChimera(EntityManager em, Entity chimeraEntity, AcquisitionMethod method, float currentTime)
        {
            PopulationManagementHelper.RequestChimeraAcquisition(em, chimeraEntity, method, currentTime);
        }

        /// <summary>
        /// WARNING: Permanently reduces capacity - use with extreme caution!
        /// </summary>
        public static void ReleaseChimeraPermanent(EntityManager em, Entity chimeraEntity, ReleaseReason reason, float currentTime)
        {
            Debug.LogWarning("PERMANENT CAPACITY REDUCTION! You can never get this slot back.");
            PopulationManagementHelper.RequestChimeraRelease(em, chimeraEntity, reason, isTemporary: false, currentTime);
        }

        // ===== EQUIPMENT OPERATIONS (Phase 6) =====

        /// <summary>
        /// Gets equipment personality effect
        /// </summary>
        public static PersonalityEquipmentEffect? GetEquipmentEffect(EntityManager em, Entity chimeraEntity)
        {
            if (!em.Exists(chimeraEntity) || !em.HasComponent<PersonalityEquipmentEffect>(chimeraEntity))
                return null;

            return em.GetComponentData<PersonalityEquipmentEffect>(chimeraEntity);
        }

        /// <summary>
        /// Gets equipment fit score (0.0-1.0)
        /// </summary>
        public static float GetEquipmentFit(EntityManager em, Entity chimeraEntity)
        {
            var effect = GetEquipmentEffect(em, chimeraEntity);
            return effect?.personalityFit ?? 0.5f;
        }

        /// <summary>
        /// Checks if chimera likes their current equipment
        /// </summary>
        public static bool LikesEquipment(EntityManager em, Entity chimeraEntity)
        {
            var effect = GetEquipmentEffect(em, chimeraEntity);
            return effect?.chimeraLikesEquipment ?? false;
        }

        /// <summary>
        /// Gets equipment cooperation modifier (-0.5 to +0.5)
        /// </summary>
        public static float GetEquipmentCooperationBonus(EntityManager em, Entity chimeraEntity)
        {
            var effect = GetEquipmentEffect(em, chimeraEntity);
            if (!effect.HasValue) return 0f;

            float fitBonus = EquipmentFitCalculator.GetCooperationModifier(effect.Value.personalityFit);
            return effect.Value.cooperationModifier + fitBonus;
        }

        // ===== ACTIVITY OPERATIONS (Phase 7) =====

        /// <summary>
        /// Calculates activity personality fit
        /// </summary>
        public static float CalculateActivityFit(EntityManager em, Entity chimeraEntity, ActivityType activityType, ActivityGenreCategory genre)
        {
            var personality = GetPersonality(em, chimeraEntity);
            if (!personality.HasValue) return 0.5f;

            return ActivityPersonalityFitCalculator.CalculateFit(personality.Value, activityType, genre);
        }

        /// <summary>
        /// Gets activity cooperation bonus based on personality fit
        /// </summary>
        public static float GetActivityCooperationBonus(EntityManager em, Entity chimeraEntity, ActivityType activityType, ActivityGenreCategory genre)
        {
            float fitScore = CalculateActivityFit(em, chimeraEntity, activityType, genre);
            return ActivityPersonalityFitCalculator.GetCooperationBonus(fitScore);
        }

        /// <summary>
        /// Checks if chimera would enjoy this activity
        /// </summary>
        public static bool WouldEnjoyActivity(EntityManager em, Entity chimeraEntity, ActivityType activityType, ActivityGenreCategory genre)
        {
            float fitScore = CalculateActivityFit(em, chimeraEntity, activityType, genre);
            return ActivityPersonalityFitCalculator.WouldEnjoyActivity(fitScore);
        }

        /// <summary>
        /// Starts partnership activity
        /// </summary>
        public static void StartActivity(
            EntityManager em,
            Entity partnershipEntity,
            Entity chimeraEntity,
            ActivityType activityType,
            ActivityDifficulty difficulty,
            float currentTime)
        {
            var personality = GetPersonality(em, chimeraEntity);
            var skill = GetPartnershipSkill(em, partnershipEntity);
            var equipmentEffect = GetEquipmentEffect(em, chimeraEntity);

            if (!personality.HasValue || !skill.HasValue) return;

            // Map activity type to genre category
            var genre = MapActivityToGenre(activityType);

            // Create start request
            var requestEntity = em.CreateEntity();
            em.AddComponentData(requestEntity, new StartPartnershipActivityRequest
            {
                partnershipEntity = partnershipEntity,
                chimeraEntity = chimeraEntity,
                activityType = activityType,
                difficulty = difficulty,
                requestTime = currentTime,
                currentCooperation = skill.Value.cooperationLevel,
                personalityFit = CalculateActivityFit(em, chimeraEntity, activityType, genre),
                equipmentBonus = GetEquipmentCooperationBonus(em, chimeraEntity)
            });
        }

        // ===== BREEDING OPERATIONS (Phase 8) =====

        /// <summary>
        /// Calculates breeding compatibility based on personality
        /// </summary>
        public static float CalculateBreedingCompatibility(EntityManager em, Entity parent1Entity, Entity parent2Entity)
        {
            var parent1Genetics = GetGeneticPersonality(em, parent1Entity);
            var parent2Genetics = GetGeneticPersonality(em, parent2Entity);

            if (!parent1Genetics.HasValue || !parent2Genetics.HasValue)
                return 0.5f; // Default compatibility

            // Quick compatibility check
            float totalCompat = 0f;
            totalCompat += PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1Genetics.Value.geneticCuriosity, parent2Genetics.Value.geneticCuriosity);
            totalCompat += PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1Genetics.Value.geneticPlayfulness, parent2Genetics.Value.geneticPlayfulness);
            totalCompat += PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1Genetics.Value.geneticAggression, parent2Genetics.Value.geneticAggression);
            totalCompat += PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1Genetics.Value.geneticAffection, parent2Genetics.Value.geneticAffection);
            totalCompat += PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1Genetics.Value.geneticIndependence, parent2Genetics.Value.geneticIndependence);
            totalCompat += PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1Genetics.Value.geneticNervousness, parent2Genetics.Value.geneticNervousness);
            totalCompat += PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1Genetics.Value.geneticStubbornness, parent2Genetics.Value.geneticStubbornness);
            totalCompat += PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1Genetics.Value.geneticLoyalty, parent2Genetics.Value.geneticLoyalty);

            return totalCompat / 8f;
        }

        /// <summary>
        /// Checks if breeding is viable (compatibility check)
        /// </summary>
        public static bool CanBreed(EntityManager em, Entity parent1Entity, Entity parent2Entity)
        {
            float compatibility = CalculateBreedingCompatibility(em, parent1Entity, parent2Entity);
            return compatibility >= PersonalityGeneticsHelper.MIN_BREEDING_COMPATIBILITY;
        }

        /// <summary>
        /// Requests personality-based breeding
        /// </summary>
        public static void RequestBreeding(
            EntityManager em,
            Entity parent1Entity,
            Entity parent2Entity,
            bool allowMutations,
            float currentTime)
        {
            if (!CanBreed(em, parent1Entity, parent2Entity))
            {
                Debug.LogWarning("Cannot breed: Personality compatibility too low!");
                return;
            }

            var requestEntity = em.CreateEntity();
            em.AddComponentData(requestEntity, new PersonalityBreedingRequest
            {
                parent1Entity = parent1Entity,
                parent2Entity = parent2Entity,
                requestTime = currentTime,
                prioritizeBalance = true,
                allowPersonalityMutations = allowMutations,
                mutationRate = PersonalityGeneticsHelper.DEFAULT_MUTATION_RATE
            });
        }

        // ===== UNIFIED STATUS QUERIES =====

        /// <summary>
        /// Gets complete chimera status summary for UI
        /// </summary>
        public static ChimeraStatusSummary GetChimeraStatus(EntityManager em, Entity chimeraEntity)
        {
            return new ChimeraStatusSummary
            {
                lifeStage = GetLifeStage(em, chimeraEntity),
                agePercentage = GetAgePercentage(em, chimeraEntity),
                cooperationLevel = GetCooperationLevel(em, chimeraEntity),
                currentEmotion = GetEmotionEmoji(em, chimeraEntity),
                emotionDescription = GetEmotionDescription(em, chimeraEntity),
                personalityLocked = HasLockedBaseline(em, chimeraEntity),
                equipmentFit = GetEquipmentFit(em, chimeraEntity),
                likesEquipment = LikesEquipment(em, chimeraEntity)
            };
        }

        /// <summary>
        /// Maps ActivityType to ActivityGenreCategory for skill progression
        /// </summary>
        private static ActivityGenreCategory MapActivityToGenre(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Racing => ActivityGenreCategory.Racing,
                ActivityType.Combat => ActivityGenreCategory.Action,
                ActivityType.Puzzle => ActivityGenreCategory.Puzzle,
                ActivityType.Strategy => ActivityGenreCategory.Strategy,
                ActivityType.Adventure => ActivityGenreCategory.Exploration,
                ActivityType.Platforming => ActivityGenreCategory.Action,
                ActivityType.Music => ActivityGenreCategory.Rhythm,
                ActivityType.Crafting => ActivityGenreCategory.Economics,
                ActivityType.Exploration => ActivityGenreCategory.Exploration,
                ActivityType.Social => ActivityGenreCategory.Economics,
                _ => ActivityGenreCategory.Action // Default fallback
            };
        }
    }

    /// <summary>
    /// Unified chimera status summary for UI
    /// </summary>
    public struct ChimeraStatusSummary
    {
        public LifeStage lifeStage;
        public float agePercentage;
        public float cooperationLevel;
        public string currentEmotion;
        public string emotionDescription;
        public bool personalityLocked;
        public float equipmentFit;
        public bool likesEquipment;

        public override string ToString()
        {
            return $"[{lifeStage}] {currentEmotion} | " +
                   $"Cooperation: {cooperationLevel:F2} | " +
                   $"Equipment Fit: {equipmentFit:P0} | " +
                   $"Personality: {(personalityLocked ? "LOCKED" : "Flexible")}";
        }
    }
}
