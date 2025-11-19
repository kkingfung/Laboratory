using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Consciousness.Core;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Equipment
{
    /// <summary>
    /// PERSONALITY EQUIPMENT SYSTEM
    ///
    /// Manages equipment that affects personality instead of stats
    ///
    /// NEW VISION: Equipment affects personality > stats
    /// - Equipment temporarily modifies personality traits
    /// - Chimeras have equipment preferences based on personality
    /// - Good fit = bonus cooperation, poor fit = cooperation penalty
    /// - Elderly chimeras resist personality changes (auto-revert)
    ///
    /// Responsibilities:
    /// - Apply personality modifiers when equipment equipped
    /// - Calculate equipment-personality fit scores
    /// - Adjust cooperation based on equipment fit
    /// - Trigger personality change events
    /// - Integrate with PersonalityStabilitySystem for age-based resistance
    ///
    /// Design Philosophy:
    /// "The right equipment makes chimeras happy and cooperative"
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PersonalityStabilitySystem))]
    public partial class PersonalityEquipmentSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            Debug.Log("Personality Equipment System initialized - equipment affects personality!");
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Initialize equipment preferences for chimeras
            InitializeEquipmentPreferences();

            // Update personality modifiers from equipped items
            UpdateEquipmentPersonalityEffects(deltaTime);

            // Process equip/unequip requests with personality consideration
            ProcessEquipmentRequests(currentTime);

            // Update equipment fit calculations
            UpdateEquipmentFit(currentTime);

            // Apply cooperation modifiers based on equipment fit
            ApplyEquipmentCooperationEffects();
        }

        /// <summary>
        /// Initializes equipment preferences for chimeras based on personality
        /// </summary>
        private void InitializeEquipmentPreferences()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (personality, entity) in
                SystemAPI.Query<RefRO<CreaturePersonality>>()
                .WithEntityAccess()
                .WithNone<EquipmentPreferenceComponent>())
            {
                var preferences = GenerateEquipmentPreferences(personality.ValueRO);
                ecb.AddComponent(entity, preferences);
            }
        }

        /// <summary>
        /// Updates personality effects from currently equipped items
        /// </summary>
        private void UpdateEquipmentPersonalityEffects(float deltaTime)
        {
            foreach (var (equipmentEffect, personality, stability, entity) in
                SystemAPI.Query<RefRW<PersonalityEquipmentEffect>, RefRW<CreaturePersonality>,
                    RefRO<PersonalityStabilityComponent>>().WithEntityAccess())
            {
                if (equipmentEffect.ValueRO.equippedItemId == 0)
                    continue; // No equipment

                // Apply personality modifiers (scaled by age malleability)
                float malleability = stability.ValueRO.personalityMalleability;

                // Temporary personality changes from equipment
                // These will auto-revert for elderly chimeras
                ApplyPersonalityModifier(ref personality.ValueRW.Curiosity,
                    equipmentEffect.ValueRO.curiosityModifier, malleability);
                ApplyPersonalityModifier(ref personality.ValueRW.Playfulness,
                    equipmentEffect.ValueRO.playfulnessModifier, malleability);
                ApplyPersonalityModifier(ref personality.ValueRW.Aggression,
                    equipmentEffect.ValueRO.aggressionModifier, malleability);
                ApplyPersonalityModifier(ref personality.ValueRW.Affection,
                    equipmentEffect.ValueRO.affectionModifier, malleability);
                ApplyPersonalityModifier(ref personality.ValueRW.Independence,
                    equipmentEffect.ValueRO.independenceModifier, malleability);
                ApplyPersonalityModifier(ref personality.ValueRW.Nervousness,
                    equipmentEffect.ValueRO.nervousnessModifier, malleability);
                ApplyPersonalityModifier(ref personality.ValueRW.Stubbornness,
                    equipmentEffect.ValueRO.stubbornnessModifier, malleability);
                ApplyPersonalityModifier(ref personality.ValueRW.Loyalty,
                    equipmentEffect.ValueRO.loyaltyModifier, malleability);

                // Apply mood/energy/stress modifiers directly
                personality.ValueRW.HappinessLevel = math.clamp(
                    personality.ValueRO.HappinessLevel + (equipmentEffect.ValueRO.moodModifier * deltaTime * 0.01f),
                    0f, 1f);

                personality.ValueRW.EnergyLevel = math.clamp(
                    personality.ValueRO.EnergyLevel + (equipmentEffect.ValueRO.energyModifier * deltaTime * 0.01f),
                    0f, 1f);

                personality.ValueRW.StressLevel = math.clamp(
                    personality.ValueRO.StressLevel + (equipmentEffect.ValueRO.stressModifier * deltaTime * 0.01f),
                    0f, 1f);

                // Track wear time
                equipmentEffect.ValueRW.totalWearTime += deltaTime;
            }
        }

        /// <summary>
        /// Processes equipment requests considering personality fit
        /// </summary>
        private void ProcessEquipmentRequests(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (equipRequest, entity) in
                SystemAPI.Query<RefRO<EquipItemRequest>>().WithEntityAccess())
            {
                var targetEntity = equipRequest.ValueRO.targetEntity;
                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Get personality for fit calculation
                if (!EntityManager.HasComponent<CreaturePersonality>(targetEntity))
                {
                    Debug.LogWarning("Cannot equip item on entity without personality!");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var personality = EntityManager.GetComponentData<CreaturePersonality>(targetEntity);

                // TODO: Get equipment profile from database
                // For now, create placeholder effect
                var equipmentEffect = new PersonalityEquipmentEffect
                {
                    equippedItemId = equipRequest.ValueRO.itemId,
                    equippedSlot = equipRequest.ValueRO.targetSlot,
                    equipTime = currentTime,
                    totalWearTime = 0f,
                    personalityFit = 0.7f, // TODO: Calculate from equipment profile
                    chimeraLikesEquipment = true // TODO: Check preferences
                };

                // Add or update equipment effect component
                if (EntityManager.HasComponent<PersonalityEquipmentEffect>(targetEntity))
                {
                    EntityManager.SetComponentData(targetEntity, equipmentEffect);
                }
                else
                {
                    ecb.AddComponent(targetEntity, equipmentEffect);
                }

                // Emit personality change event
                var changeEvent = EntityManager.CreateEntity();
                ecb.AddComponent(changeEvent, new EquipmentPersonalityChangeEvent
                {
                    chimeraEntity = targetEntity,
                    itemId = equipRequest.ValueRO.itemId,
                    wasEquipped = true,
                    personalityFit = equipmentEffect.personalityFit,
                    cooperationChange = EquipmentFitCalculator.GetCooperationModifier(equipmentEffect.personalityFit),
                    moodChange = equipmentEffect.moodModifier,
                    timestamp = currentTime,
                    description = equipmentEffect.chimeraLikesEquipment ?
                        "Equipped item - chimera likes it!" :
                        "Equipped item - chimera tolerates it"
                });

                Debug.Log($"Equipped item {equipRequest.ValueRO.itemId} - Fit: {equipmentEffect.personalityFit:F2}");
                ecb.DestroyEntity(entity);
            }

            // Process unequip requests
            foreach (var (unequipRequest, entity) in
                SystemAPI.Query<RefRO<UnequipItemRequest>>().WithEntityAccess())
            {
                var targetEntity = unequipRequest.ValueRO.targetEntity;
                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                if (EntityManager.HasComponent<PersonalityEquipmentEffect>(targetEntity))
                {
                    var effect = EntityManager.GetComponentData<PersonalityEquipmentEffect>(targetEntity);

                    // Emit unequip event
                    var changeEvent = EntityManager.CreateEntity();
                    ecb.AddComponent(changeEvent, new EquipmentPersonalityChangeEvent
                    {
                        chimeraEntity = targetEntity,
                        itemId = effect.equippedItemId,
                        wasEquipped = false,
                        personalityFit = 0.5f,
                        cooperationChange = -EquipmentFitCalculator.GetCooperationModifier(effect.personalityFit),
                        moodChange = -effect.moodModifier,
                        timestamp = currentTime,
                        description = "Unequipped item - returning to baseline"
                    });

                    // Remove equipment effect (personality will revert naturally for elderly)
                    ecb.RemoveComponent<PersonalityEquipmentEffect>(targetEntity);

                    Debug.Log($"Unequipped item {effect.equippedItemId}");
                }

                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Updates equipment fit calculations
        /// </summary>
        private void UpdateEquipmentFit(float currentTime)
        {
            foreach (var (equipmentEffect, personality, preferences, entity) in
                SystemAPI.Query<RefRW<PersonalityEquipmentEffect>, RefRO<CreaturePersonality>,
                    RefRO<EquipmentPreferenceComponent>>().WithEntityAccess())
            {
                if (equipmentEffect.ValueRO.equippedItemId == 0)
                    continue;

                // TODO: Get equipment profile from database and calculate actual fit
                // For now, use placeholder calculation based on preferences

                // Update happiness with current equipment
                float happinessBonus = equipmentEffect.ValueRO.chimeraLikesEquipment ? 0.1f : -0.05f;
                var newPreferences = preferences.ValueRO;
                newPreferences.happinessWithCurrentEquipment = math.clamp(
                    preferences.ValueRO.happinessWithCurrentEquipment + happinessBonus,
                    0f, 1f);

                EntityManager.SetComponentData(entity, newPreferences);
            }
        }

        /// <summary>
        /// Applies cooperation modifiers based on equipment fit
        /// </summary>
        private void ApplyEquipmentCooperationEffects()
        {
            foreach (var (equipmentEffect, partnershipSkill, entity) in
                SystemAPI.Query<RefRO<PersonalityEquipmentEffect>,
                    RefRW<Laboratory.Chimera.Progression.PartnershipSkillComponent>>().WithEntityAccess())
            {
                if (equipmentEffect.ValueRO.equippedItemId == 0)
                    continue;

                // Equipment fit affects cooperation directly
                float cooperationMod = equipmentEffect.ValueRO.cooperationModifier;
                float fitBonus = EquipmentFitCalculator.GetCooperationModifier(equipmentEffect.ValueRO.personalityFit);
                float totalCooperationEffect = cooperationMod + fitBonus;

                // Apply to partnership cooperation (clamped to reasonable range)
                partnershipSkill.ValueRW.cooperationLevel = math.clamp(
                    partnershipSkill.ValueRO.cooperationLevel + totalCooperationEffect,
                    0f, 1.2f); // Can go slightly above 1.0 with perfect equipment
            }
        }

        // Helper methods

        private void ApplyPersonalityModifier(ref byte trait, sbyte modifier, float malleability)
        {
            if (modifier == 0) return;

            // Scale modifier by age malleability
            // Elderly (5% malleability) barely affected
            // Baby (100% malleability) fully affected
            float scaledModifier = modifier * malleability;

            int newValue = trait + (int)scaledModifier;
            trait = (byte)math.clamp(newValue, 0, 100);
        }

        private EquipmentPreferenceComponent GenerateEquipmentPreferences(CreaturePersonality personality)
        {
            var preferences = new EquipmentPreferenceComponent();
            EquipmentPreferenceFlags preferred = EquipmentPreferenceFlags.None;
            EquipmentPreferenceFlags disliked = EquipmentPreferenceFlags.None;

            // Determine preferences based on personality
            if (personality.Playfulness > 60)
                preferred |= EquipmentPreferenceFlags.Playful | EquipmentPreferenceFlags.Cute;
            if (personality.Playfulness < 30)
                disliked |= EquipmentPreferenceFlags.Playful;

            if (personality.Aggression > 60)
                preferred |= EquipmentPreferenceFlags.Combat | EquipmentPreferenceFlags.Protective;
            if (personality.Aggression < 30)
                disliked |= EquipmentPreferenceFlags.Combat;

            if (personality.Curiosity > 60)
                preferred |= EquipmentPreferenceFlags.Scholarly | EquipmentPreferenceFlags.Technological;

            if (personality.Affection > 60)
                preferred |= EquipmentPreferenceFlags.Cute | EquipmentPreferenceFlags.Comfortable;

            if (personality.Independence > 60)
                preferred |= EquipmentPreferenceFlags.Simple | EquipmentPreferenceFlags.Practical;
            if (personality.Independence < 30)
                preferred |= EquipmentPreferenceFlags.Fancy | EquipmentPreferenceFlags.Expressive;

            if (personality.Nervousness > 60)
            {
                preferred |= EquipmentPreferenceFlags.Comfortable | EquipmentPreferenceFlags.Protective;
                disliked |= EquipmentPreferenceFlags.Combat;
            }

            preferences.preferredTypes = preferred;
            preferences.dislikedTypes = disliked;
            preferences.favoriteItemId = 0; // No favorite yet
            preferences.favoriteItemBondBonus = 0f;
            preferences.happinessWithCurrentEquipment = 0.5f; // Neutral
            preferences.consecutiveDaysWearingFavorite = 0;

            return preferences;
        }
    }
}
