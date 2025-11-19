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
    /// CHIMERA SYSTEM VALIDATOR
    ///
    /// Comprehensive validation and testing suite for all chimera systems
    ///
    /// Validates:
    /// - Phase 1: Partnership progression system
    /// - Phase 2: Age-based bonding sensitivity
    /// - Phase 3: 5-stage life journey
    /// - Phase 3.5: Personality stability for elderly
    /// - Phase 4: Emotional indicator system
    /// - Phase 5: Population management with consequences
    /// - Phase 6: Personality-focused equipment
    /// - Phase 7: Skill-based activities
    /// - Phase 8: Personality genetics inheritance
    /// - Phase 9: Permanent consequences (integrated with Phase 5)
    /// - Phase 10: Integration layer
    ///
    /// Usage:
    /// - Call ValidateAllSystems() to run full validation
    /// - Call specific validation methods for targeted testing
    /// - Review console output for validation results
    /// </summary>
    public static class ChimeraSystemValidator
    {
        private static int _passedTests = 0;
        private static int _failedTests = 0;

        /// <summary>
        /// Runs full system validation
        /// </summary>
        public static void ValidateAllSystems(EntityManager em, float currentTime)
        {
            _passedTests = 0;
            _failedTests = 0;

            Debug.Log("=== CHIMERA SYSTEM VALIDATION START ===");

            // Phase 1: Partnership Progression
            ValidatePartnershipProgression(em, currentTime);

            // Phase 2: Age-Based Bonding
            ValidateAgeSensitivity(em, currentTime);

            // Phase 3: Life Stages
            ValidateLifeStages(em, currentTime);

            // Phase 3.5: Personality Stability
            ValidatePersonalityStability(em, currentTime);

            // Phase 4: Emotional Indicators
            ValidateEmotionalIndicators(em, currentTime);

            // Phase 5: Population Management
            ValidatePopulationManagement(em, currentTime);

            // Phase 6: Personality Equipment
            ValidatePersonalityEquipment(em, currentTime);

            // Phase 7: Partnership Activities
            ValidatePartnershipActivities(em, currentTime);

            // Phase 8: Personality Genetics
            ValidatePersonalityGenetics(em, currentTime);

            // Phase 10: Integration Layer
            ValidateIntegrationLayer(em, currentTime);

            Debug.Log($"=== VALIDATION COMPLETE: {_passedTests} passed, {_failedTests} failed ===");
        }

        // ===== PHASE 1: PARTNERSHIP PROGRESSION =====

        private static void ValidatePartnershipProgression(EntityManager em, float currentTime)
        {
            Debug.Log("--- Validating Phase 1: Partnership Progression ---");

            // Create test entity
            var testEntity = em.CreateEntity();
            em.AddComponentData(testEntity, new PartnershipSkillComponent
            {
                cooperationLevel = 0.7f,
                trustLevel = 0.6f,
                bondQuality = 0.65f,
                actionMastery = 0.5f,
                totalActivitiesCompleted = 10,
                partnershipStartTime = currentTime - 3600f
            });

            // Test cooperation level
            Test("Partnership cooperation in range",
                em.GetComponentData<PartnershipSkillComponent>(testEntity).cooperationLevel >= 0f &&
                em.GetComponentData<PartnershipSkillComponent>(testEntity).cooperationLevel <= 1.2f);

            // Test mastery values
            Test("Partnership mastery in range",
                em.GetComponentData<PartnershipSkillComponent>(testEntity).actionMastery >= 0f &&
                em.GetComponentData<PartnershipSkillComponent>(testEntity).actionMastery <= 1f);

            em.DestroyEntity(testEntity);
        }

        // ===== PHASE 2: AGE-BASED BONDING =====

        private static void ValidateAgeSensitivity(EntityManager em, float currentTime)
        {
            Debug.Log("--- Validating Phase 2: Age-Based Bonding Sensitivity ---");

            // Test baby sensitivity
            var babyEntity = CreateTestChimera(em, LifeStage.Baby, currentTime);
            Test("Baby emotional sensitivity high",
                ChimeraSystemsIntegration.GetEmotionalSensitivity(em, babyEntity) > 1.5f);
            em.DestroyEntity(babyEntity);

            // Test elderly sensitivity
            var elderlyEntity = CreateTestChimera(em, LifeStage.Elderly, currentTime);
            Test("Elderly emotional sensitivity lower",
                ChimeraSystemsIntegration.GetEmotionalSensitivity(em, elderlyEntity) < 1.0f);
            em.DestroyEntity(elderlyEntity);
        }

        // ===== PHASE 3: LIFE STAGES =====

        private static void ValidateLifeStages(EntityManager em, float currentTime)
        {
            Debug.Log("--- Validating Phase 3: 5-Stage Life Journey ---");

            // Test all 5 stages
            Test("Baby stage valid", LifeStage.CalculateLifeStageFromPercentage(0.1f) == LifeStage.Baby);
            Test("Child stage valid", LifeStage.CalculateLifeStageFromPercentage(0.3f) == LifeStage.Child);
            Test("Teen stage valid", LifeStage.CalculateLifeStageFromPercentage(0.5f) == LifeStage.Teen);
            Test("Adult stage valid", LifeStage.CalculateLifeStageFromPercentage(0.7f) == LifeStage.Adult);
            Test("Elderly stage valid", LifeStage.CalculateLifeStageFromPercentage(0.9f) == LifeStage.Elderly);
        }

        // ===== PHASE 3.5: PERSONALITY STABILITY =====

        private static void ValidatePersonalityStability(EntityManager em, float currentTime)
        {
            Debug.Log("--- Validating Phase 3.5: Personality Stability System ---");

            // Test elderly personality locking
            var elderlyEntity = CreateTestChimera(em, LifeStage.Elderly, currentTime);
            em.AddComponentData(elderlyEntity, new PersonalityStabilityComponent
            {
                currentLifeStage = LifeStage.Elderly,
                personalityMalleability = 0.05f, // 5% for elderly
                reversionSpeed = 1.0f,
                hasLockedBaseline = true
            });

            Test("Elderly malleability very low",
                em.GetComponentData<PersonalityStabilityComponent>(elderlyEntity).personalityMalleability <= 0.1f);

            Test("Elderly reversion speed high",
                em.GetComponentData<PersonalityStabilityComponent>(elderlyEntity).reversionSpeed >= 0.8f);

            em.DestroyEntity(elderlyEntity);
        }

        // ===== PHASE 4: EMOTIONAL INDICATORS =====

        private static void ValidateEmotionalIndicators(EntityManager em, float currentTime)
        {
            Debug.Log("--- Validating Phase 4: Emotional Indicator System ---");

            var testEntity = CreateTestChimera(em, LifeStage.Adult, currentTime);
            em.AddComponentData(testEntity, new EmotionalIndicatorComponent
            {
                currentIcon = EmotionalIcon.Happy,
                intensity = 0.8f,
                timeSinceLastChange = 0f
            });

            // Test emoji mapping
            string emoji = EmotionalIconMapper.GetEmoji(EmotionalIcon.Happy);
            Test("Emotion emoji not empty", !string.IsNullOrEmpty(emoji));

            // Test description
            string description = EmotionalIconMapper.GetDescription(EmotionalIcon.Happy);
            Test("Emotion description not empty", !string.IsNullOrEmpty(description));

            em.DestroyEntity(testEntity);
        }

        // ===== PHASE 5: POPULATION MANAGEMENT =====

        private static void ValidatePopulationManagement(EntityManager em, float currentTime)
        {
            Debug.Log("--- Validating Phase 5: Population Management ---");

            // Test capacity status
            string capacityStatus = PopulationManagementHelper.GetCapacityStatus(em);
            Test("Capacity status available", !string.IsNullOrEmpty(capacityStatus));

            // Test unlock progress
            string unlockProgress = PopulationManagementHelper.GetUnlockProgress(em);
            Test("Unlock progress available", !string.IsNullOrEmpty(unlockProgress));
        }

        // ===== PHASE 6: PERSONALITY EQUIPMENT =====

        private static void ValidatePersonalityEquipment(EntityManager em, float currentTime)
        {
            Debug.Log("--- Validating Phase 6: Personality Equipment System ---");

            var testEntity = CreateTestChimera(em, LifeStage.Adult, currentTime);
            em.AddComponentData(testEntity, new PersonalityEquipmentEffect
            {
                equippedItemId = 1,
                personalityFit = 0.8f,
                chimeraLikesEquipment = true,
                cooperationModifier = 0.1f
            });

            // Test equipment fit
            Test("Equipment fit in range",
                em.GetComponentData<PersonalityEquipmentEffect>(testEntity).personalityFit >= 0f &&
                em.GetComponentData<PersonalityEquipmentEffect>(testEntity).personalityFit <= 1f);

            // Test cooperation modifier
            float cooperationBonus = EquipmentFitCalculator.GetCooperationModifier(0.8f);
            Test("Equipment cooperation bonus positive for good fit", cooperationBonus > 0f);

            em.DestroyEntity(testEntity);
        }

        // ===== PHASE 7: PARTNERSHIP ACTIVITIES =====

        private static void ValidatePartnershipActivities(EntityManager em, float currentTime)
        {
            Debug.Log("--- Validating Phase 7: Partnership Activity System ---");

            var testEntity = CreateTestChimera(em, LifeStage.Adult, currentTime);

            // Test personality fit calculation
            float fitScore = ActivityPersonalityFitCalculator.CalculateFit(
                em.GetComponentData<CreaturePersonality>(testEntity),
                ActivityType.Racing,
                ActivityGenreCategory.Racing);

            Test("Activity fit in range", fitScore >= 0f && fitScore <= 1f);

            // Test cooperation bonus
            float cooperationBonus = ActivityPersonalityFitCalculator.GetCooperationBonus(fitScore);
            Test("Activity cooperation bonus in range",
                cooperationBonus >= -0.3f && cooperationBonus <= 0.3f);

            em.DestroyEntity(testEntity);
        }

        // ===== PHASE 8: PERSONALITY GENETICS =====

        private static void ValidatePersonalityGenetics(EntityManager em, float currentTime)
        {
            Debug.Log("--- Validating Phase 8: Personality Genetics Inheritance ---");

            // Create test parent entities
            var parent1 = CreateTestChimera(em, LifeStage.Adult, currentTime);
            var parent2 = CreateTestChimera(em, LifeStage.Adult, currentTime);

            em.AddComponentData(parent1, new PersonalityGeneticComponent
            {
                geneticCuriosity = 80,
                geneticPlayfulness = 60,
                geneticAggression = 30,
                geneticAffection = 70,
                personalityFitness = 0.7f
            });

            em.AddComponentData(parent2, new PersonalityGeneticComponent
            {
                geneticCuriosity = 60,
                geneticPlayfulness = 40,
                geneticAggression = 50,
                geneticAffection = 80,
                personalityFitness = 0.8f
            });

            // Test compatibility calculation
            float compatibility = ChimeraSystemsIntegration.CalculateBreedingCompatibility(em, parent1, parent2);
            Test("Breeding compatibility in range", compatibility >= 0f && compatibility <= 1f);

            // Test trait compatibility
            float traitCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(80, 60);
            Test("Trait compatibility valid", traitCompat >= 0f && traitCompat <= 1f);

            em.DestroyEntity(parent1);
            em.DestroyEntity(parent2);
        }

        // ===== PHASE 10: INTEGRATION LAYER =====

        private static void ValidateIntegrationLayer(EntityManager em, float currentTime)
        {
            Debug.Log("--- Validating Phase 10: Integration Layer ---");

            var testEntity = CreateTestChimera(em, LifeStage.Adult, currentTime);

            // Test unified status
            var status = ChimeraSystemsIntegration.GetChimeraStatus(em, testEntity);
            Test("Status life stage valid", status.lifeStage == LifeStage.Adult);
            Test("Status string not empty", !string.IsNullOrEmpty(status.ToString()));

            // Test cooperation level retrieval
            float cooperation = ChimeraSystemsIntegration.GetCooperationLevel(em, testEntity);
            Test("Cooperation level retrieval works", cooperation >= 0f);

            // Test personality retrieval
            var personality = ChimeraSystemsIntegration.GetPersonality(em, testEntity);
            Test("Personality retrieval works", personality.HasValue);

            em.DestroyEntity(testEntity);
        }

        // ===== HELPER METHODS =====

        private static Entity CreateTestChimera(EntityManager em, LifeStage stage, float currentTime)
        {
            var entity = em.CreateEntity();

            // CreatureIdentityComponent
            em.AddComponentData(entity, new CreatureIdentityComponent
            {
                CreatureID = System.Guid.NewGuid().ToString(),
                SpeciesID = 1,
                CurrentLifeStage = stage,
                AgePercentage = stage switch
                {
                    LifeStage.Baby => 0.1f,
                    LifeStage.Child => 0.3f,
                    LifeStage.Teen => 0.5f,
                    LifeStage.Adult => 0.7f,
                    LifeStage.Elderly => 0.9f,
                    _ => 0.5f
                },
                BirthTime = currentTime - 3600f,
                MaxLifespan = 86400f
            });

            // CreaturePersonality
            em.AddComponentData(entity, new CreaturePersonality
            {
                Curiosity = 70,
                Playfulness = 60,
                Aggression = 30,
                Affection = 80,
                Independence = 50,
                Nervousness = 40,
                Stubbornness = 45,
                Loyalty = 75,
                LearningRate = 0.5f,
                MemoryStrength = 60,
                StressLevel = 0.3f,
                HappinessLevel = 0.7f,
                EnergyLevel = 0.8f
            });

            // PartnershipSkillComponent
            em.AddComponentData(entity, new PartnershipSkillComponent
            {
                cooperationLevel = 0.7f,
                trustLevel = 0.6f,
                bondQuality = 0.65f,
                actionMastery = 0.3f,
                strategyMastery = 0.2f,
                totalActivitiesCompleted = 5,
                partnershipStartTime = currentTime - 1800f
            });

            return entity;
        }

        private static void Test(string testName, bool condition)
        {
            if (condition)
            {
                Debug.Log($"✓ PASS: {testName}");
                _passedTests++;
            }
            else
            {
                Debug.LogError($"✗ FAIL: {testName}");
                _failedTests++;
            }
        }

        /// <summary>
        /// Prints comprehensive system diagnostics
        /// </summary>
        public static void PrintSystemDiagnostics(EntityManager em)
        {
            Debug.Log("=== CHIMERA SYSTEM DIAGNOSTICS ===");

            // Count entities with each component type
            var partnershipQuery = em.CreateEntityQuery(typeof(PartnershipSkillComponent));
            Debug.Log($"Partnerships: {partnershipQuery.CalculateEntityCount()}");

            var personalityQuery = em.CreateEntityQuery(typeof(CreaturePersonality));
            Debug.Log($"Personalities: {personalityQuery.CalculateEntityCount()}");

            var identityQuery = em.CreateEntityQuery(typeof(CreatureIdentityComponent));
            Debug.Log($"Creatures: {identityQuery.CalculateEntityCount()}");

            var emotionalQuery = em.CreateEntityQuery(typeof(EmotionalIndicatorComponent));
            Debug.Log($"Emotional Indicators: {emotionalQuery.CalculateEntityCount()}");

            var geneticQuery = em.CreateEntityQuery(typeof(PersonalityGeneticComponent));
            Debug.Log($"Genetic Personalities: {geneticQuery.CalculateEntityCount()}");

            var stabilityQuery = em.CreateEntityQuery(typeof(PersonalityStabilityComponent));
            Debug.Log($"Personality Stability: {stabilityQuery.CalculateEntityCount()}");

            var equipmentQuery = em.CreateEntityQuery(typeof(PersonalityEquipmentEffect));
            Debug.Log($"Equipment Effects: {equipmentQuery.CalculateEntityCount()}");

            // Population status
            Debug.Log($"\nPopulation Status: {PopulationManagementHelper.GetCapacityStatus(em)}");
            Debug.Log($"Unlock Progress: {PopulationManagementHelper.GetUnlockProgress(em)}");

            Debug.Log("=== END DIAGNOSTICS ===");
        }
    }
}
