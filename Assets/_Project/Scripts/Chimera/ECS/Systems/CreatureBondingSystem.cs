using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Discovery;
using Laboratory.Chimera.Social;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// Enhanced creature bonding system with generational memory and emotional connections.
    /// Creates deep relationships between players and creatures across generations.
    /// </summary>
    public partial class CreatureBondingSystem : SystemBase
    {
        private CreatureBondingConfig _config;
        private EntityQuery _creaturesWithBondsQuery;
        private EntityQuery _activePlayersQuery;

        // Event system for bonding moments
        public static event Action<int, BondingMoment> OnBondingMoment;
        public static event Action<int, LegacyConnection> OnLegacyDiscovered;
        public static event Action<int, EmotionalMemory> OnMemoryTriggered;

        protected override void OnCreate()
        {
            _config = Resources.Load<CreatureBondingConfig>("Configs/CreatureBondingConfig");
            if (_config == null)
            {
                UnityEngine.Debug.LogError("CreatureBondingConfig not found in Resources/Configs/");
                return;
            }

            _creaturesWithBondsQuery = GetEntityQuery(
                ComponentType.ReadWrite<CreatureBondData>(),
                ComponentType.ReadOnly<CreatureGeneticsComponent>()
            );

            _activePlayersQuery = GetEntityQuery(
                ComponentType.ReadOnly<PlayerData>(),
                ComponentType.ReadWrite<PlayerBondingHistory>()
            );
        }

        protected override void OnUpdate()
        {
            if (_config == null) return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Process bonding moments and interactions
            ProcessBondingInteractions(deltaTime);

            // Update generational memories
            UpdateGenerationalMemories(deltaTime);

            // Check for legacy connections
            CheckLegacyConnections();

            // Process emotional memory triggers
            ProcessEmotionalMemories(deltaTime);

            // Update bond strength over time
            UpdateBondStrengths(deltaTime);
        }

        private void ProcessBondingInteractions(float deltaTime)
        {
            Entities.WithAll<CreatureBondData, CreatureGeneticsComponent>().ForEach((Entity entity, ref CreatureBondData bondData, in CreatureGeneticsComponent genetics) =>
            {
                // Update interaction tracking
                bondData.timeSinceLastInteraction += deltaTime;
                bondData.timeAlive += deltaTime;

                // Check for special bonding moments
                CheckForBondingMoments(entity, ref bondData, genetics);

                // Process active memories
                ProcessActiveMemories(ref bondData, deltaTime);

                // Update emotional state based on interactions
                UpdateBondingEmotionalState(ref bondData, deltaTime);
            }).WithoutBurst().Run();
        }

        private void CheckForBondingMoments(Entity entity, ref CreatureBondData bondData, in CreatureGeneticsComponent genetics)
        {
            // First interaction moment
            if (!bondData.hasHadFirstInteraction && bondData.timeSinceLastInteraction < _config.interactionTimeThreshold)
            {
                bondData.hasHadFirstInteraction = true;
                var moment = new BondingMoment
                {
                    type = BondingMomentType.FirstMeeting,
                    intensity = CalculateMomentIntensity(BondingMomentType.FirstMeeting, bondData, genetics),
                    timestamp = (float)SystemAPI.Time.ElapsedTime,
                    description = GenerateMomentDescription(BondingMomentType.FirstMeeting, genetics),
                    memoryStrength = _config.firstMeetingMemoryStrength
                };

                RecordBondingMoment(entity, moment, ref bondData);
                OnBondingMoment?.Invoke(bondData.playerId, moment);
            }

            // Trust breakthrough moment
            if (bondData.bondStrength > _config.trustBreakthroughThreshold && !bondData.hasTrustBreakthrough)
            {
                bondData.hasTrustBreakthrough = true;
                var moment = new BondingMoment
                {
                    type = BondingMomentType.TrustBreakthrough,
                    intensity = CalculateMomentIntensity(BondingMomentType.TrustBreakthrough, bondData, genetics),
                    timestamp = (float)SystemAPI.Time.ElapsedTime,
                    description = GenerateMomentDescription(BondingMomentType.TrustBreakthrough, genetics),
                    memoryStrength = _config.trustBreakthroughMemoryStrength
                };

                RecordBondingMoment(entity, moment, ref bondData);
                OnBondingMoment?.Invoke(bondData.playerId, moment);
            }

            // Life milestone moments
            CheckLifeMilestones(entity, ref bondData, genetics);

            // Shared discovery moments
            CheckSharedDiscoveries(entity, ref bondData, genetics);
        }

        private void CheckLifeMilestones(Entity entity, ref CreatureBondData bondData, in CreatureGeneticsComponent genetics)
        {
            float ageInDays = bondData.timeAlive / (24f * 3600f);

            // Adolescence moment
            if (ageInDays > _config.adolescenceAgeThreshold && !bondData.hasAdolescenceMoment)
            {
                bondData.hasAdolescenceMoment = true;
                var moment = new BondingMoment
                {
                    type = BondingMomentType.Adolescence,
                    intensity = CalculateMomentIntensity(BondingMomentType.Adolescence, bondData, genetics),
                    timestamp = (float)SystemAPI.Time.ElapsedTime,
                    description = GenerateMomentDescription(BondingMomentType.Adolescence, genetics),
                    memoryStrength = _config.milestoneMemoryStrength
                };

                RecordBondingMoment(entity, moment, ref bondData);
                OnBondingMoment?.Invoke(bondData.playerId, moment);
            }

            // Maturity moment
            if (ageInDays > _config.maturityAgeThreshold && !bondData.hasMaturityMoment)
            {
                bondData.hasMaturityMoment = true;
                var moment = new BondingMoment
                {
                    type = BondingMomentType.Maturity,
                    intensity = CalculateMomentIntensity(BondingMomentType.Maturity, bondData, genetics),
                    timestamp = (float)SystemAPI.Time.ElapsedTime,
                    description = GenerateMomentDescription(BondingMomentType.Maturity, genetics),
                    memoryStrength = _config.milestoneMemoryStrength
                };

                RecordBondingMoment(entity, moment, ref bondData);
                OnBondingMoment?.Invoke(bondData.playerId, moment);
            }
        }

        private void CheckSharedDiscoveries(Entity entity, ref CreatureBondData bondData, in CreatureGeneticsComponent genetics)
        {
            // Check if creature participated in any recent discoveries
            // This would typically be triggered by other systems
            if (bondData.participatedInDiscovery && !bondData.hasSharedDiscoveryMoment)
            {
                bondData.hasSharedDiscoveryMoment = true;
                var moment = new BondingMoment
                {
                    type = BondingMomentType.SharedDiscovery,
                    intensity = CalculateMomentIntensity(BondingMomentType.SharedDiscovery, bondData, genetics),
                    timestamp = (float)SystemAPI.Time.ElapsedTime,
                    description = GenerateMomentDescription(BondingMomentType.SharedDiscovery, genetics),
                    memoryStrength = _config.discoveryMemoryStrength
                };

                RecordBondingMoment(entity, moment, ref bondData);
                OnBondingMoment?.Invoke(bondData.playerId, moment);
            }
        }

        private void UpdateGenerationalMemories(float deltaTime)
        {
            Entities.WithAll<PlayerBondingHistory>().ForEach((Entity playerEntity, ref PlayerBondingHistory history) =>
            {
                // Update memory fade over time
                for (int i = 0; i < history.legacyConnections.Length; i++)
                {
                    if (history.legacyConnections[i].isActive)
                    {
                        var connection = history.legacyConnections[i];
                        connection.memoryStrength -= _config.memoryFadeRate * deltaTime;

                        if (connection.memoryStrength <= 0f)
                        {
                            connection.isActive = false;
                        }

                        history.legacyConnections[i] = connection;
                    }
                }

                // Check for new legacy connections
                CheckForNewLegacyConnections(playerEntity, ref history);
            }).WithoutBurst().Run();
        }

        private void CheckForNewLegacyConnections(Entity playerEntity, ref PlayerBondingHistory history)
        {
            // Extract values from ref parameter to avoid lambda capture issues
            var playerId = history.playerId;
            var pastBonds = history.pastBonds;
            var newConnections = new List<LegacyConnection>();

            // Look for creatures that share genetic lineage with previously bonded creatures
            Entities.WithAll<CreatureBondData, CreatureGeneticsComponent>().WithoutBurst().ForEach((Entity creatureEntity, ref CreatureBondData bondData, in CreatureGeneticsComponent genetics) =>
            {
                if (bondData.playerId != playerId) return;

                // Check genetic connections to past creatures
                for (int i = 0; i < pastBonds.Length; i++)
                {
                    var pastBond = pastBonds[i];
                    if (!pastBond.isActive) continue;

                    float geneticSimilarity = CalculateGeneticSimilarityFromHash(genetics, pastBond.geneticHash);

                    if (geneticSimilarity > _config.legacyConnectionThreshold)
                    {
                        var connection = new LegacyConnection
                        {
                            currentCreatureId = bondData.creatureId,
                            ancestorCreatureId = pastBond.creatureId,
                            connectionType = DetermineLegacyType(geneticSimilarity, pastBond),
                            memoryStrength = geneticSimilarity * _config.legacyMemoryMultiplier,
                            discoveredAt = (float)SystemAPI.Time.ElapsedTime,
                            isActive = true,
                            description = GenerateLegacyDescription(genetics, pastBond, geneticSimilarity)
                        };

                        newConnections.Add(connection);

                        // Trigger emotional memory in current creature
                        TriggerAncestralMemory(ref bondData, connection);
                    }
                }
            }).Run();

            // Add all new connections after the lambda
            foreach (var connection in newConnections)
            {
                AddLegacyConnection(ref history, connection);
                OnLegacyDiscovered?.Invoke(playerId, connection);
            }
        }

        private void CheckLegacyConnections()
        {
            // This runs less frequently to check for deep ancestral connections
            if ((float)SystemAPI.Time.ElapsedTime - _lastLegacyCheck < _config.legacyCheckInterval) return;
            _lastLegacyCheck = (float)SystemAPI.Time.ElapsedTime;

            Entities.WithAll<PlayerBondingHistory>().WithoutBurst().ForEach((Entity playerEntity, ref PlayerBondingHistory history) =>
            {
                ProcessDeepLegacyConnections(playerEntity, ref history);
            }).Run();
        }

        private float _lastLegacyCheck = 0f;

        private void ProcessDeepLegacyConnections(Entity playerEntity, ref PlayerBondingHistory history)
        {
            // Look for connections spanning multiple generations
            for (int i = 0; i < history.legacyConnections.Length; i++)
            {
                if (!history.legacyConnections[i].isActive) continue;

                var connection = history.legacyConnections[i];

                // Check if this connection reveals deeper patterns
                if (connection.memoryStrength > _config.deepMemoryThreshold)
                {
                    CheckForGenerationalPatterns(ref history, connection);
                    CheckForTraitInheritance(ref history, connection);
                }
            }
        }

        private void CheckForGenerationalPatterns(ref PlayerBondingHistory history, LegacyConnection connection)
        {
            // Analyze patterns across generations
            int patternCount = 0;
            for (int i = 0; i < history.generationalPatterns.Length; i++)
            {
                if (history.generationalPatterns[i].isActive)
                {
                    if (DoesConnectionMatchPattern(connection, history.generationalPatterns[i]))
                    {
                        patternCount++;
                        var pattern = history.generationalPatterns[i];
                        pattern.strength += _config.patternStrengthIncrement;
                        history.generationalPatterns[i] = pattern;
                    }
                }
            }

            // Create new pattern if threshold reached
            if (patternCount == 0 && ShouldCreateNewPattern(connection, history))
            {
                CreateGenerationalPattern(ref history, connection);
            }
        }

        private void CheckForTraitInheritance(ref PlayerBondingHistory history, LegacyConnection connection)
        {
            // Track how traits pass through generations
            // This would analyze genetic data to find inherited behavioral traits
        }

        private void ProcessEmotionalMemories(float deltaTime)
        {
            Entities.WithAll<CreatureBondData>().WithoutBurst().ForEach((Entity entity, ref CreatureBondData bondData) =>
            {
                ProcessCreatureEmotionalMemories(entity, ref bondData, deltaTime);
            }).Run();
        }

        private void ProcessCreatureEmotionalMemories(Entity entity, ref CreatureBondData bondData, float deltaTime)
        {
            for (int i = 0; i < bondData.emotionalMemories.Length; i++)
            {
                if (!bondData.emotionalMemories[i].isActive) continue;

                var memory = bondData.emotionalMemories[i];

                // Check for memory triggers
                if (ShouldTriggerMemory(memory, bondData))
                {
                    TriggerEmotionalMemory(entity, ref bondData, memory);
                }

                // Update memory strength over time
                memory.strength -= _config.memoryFadeRate * deltaTime;
                if (memory.strength <= 0f)
                {
                    memory.isActive = false;
                }
                bondData.emotionalMemories[i] = memory;
            }
        }

        private void UpdateBondStrengths(float deltaTime)
        {
            Entities.WithAll<CreatureBondData>().WithoutBurst().ForEach((Entity entity, ref CreatureBondData bondData) =>
            {
                // Decay bond strength over time without interaction
                if (bondData.timeSinceLastInteraction > _config.interactionTimeThreshold)
                {
                    bondData.bondStrength -= _config.bondDecayRate * deltaTime;
                }

                // Increase bond strength with recent positive interactions
                if (bondData.recentPositiveInteractions > _config.positiveInteractionThreshold)
                {
                    bondData.bondStrength += _config.bondGrowthRate * deltaTime;
                    bondData.recentPositiveInteractions = 0; // Reset counter
                }

                // Clamp bond strength
                bondData.bondStrength = Mathf.Clamp(bondData.bondStrength, 0f, _config.maxBondStrength);

                // Update loyalty based on bond strength
                UpdateCreatureLoyalty(ref bondData);
            }).Run();
        }

        private void UpdateCreatureLoyalty(ref CreatureBondData bondData)
        {
            // Calculate loyalty based on bond strength and experiences
            float baseLoyalty = bondData.bondStrength / _config.maxBondStrength;

            // Modify based on positive/negative experiences
            float experienceModifier = (bondData.positiveExperiences - bondData.negativeExperiences) * _config.experienceImpact;

            // Apply generational bonus if legacy connections exist
            float legacyBonus = bondData.hasLegacyConnection ? _config.legacyLoyaltyBonus : 0f;

            bondData.loyaltyLevel = Mathf.Clamp01(baseLoyalty + experienceModifier + legacyBonus);
        }

        #region Helper Methods

        private float CalculateMomentIntensity(BondingMomentType type, CreatureBondData bondData, in CreatureGeneticsComponent genetics)
        {
            float baseIntensity = _config.GetMomentIntensity(type);

            // Modify based on bond strength
            float bondModifier = bondData.bondStrength / _config.maxBondStrength;

            // Modify based on creature rarity
            float rarityModifier = CalculateCreatureRarity(genetics);

            return baseIntensity * (1f + bondModifier + rarityModifier);
        }

        private string GenerateMomentDescription(BondingMomentType type, in CreatureGeneticsComponent genetics)
        {
            return _config.GetMomentDescription(type, new string[] { "Strength", "Vitality", "Agility", "Intelligence" });
        }

        private void RecordBondingMoment(Entity entity, BondingMoment moment, ref CreatureBondData bondData)
        {
            // Add to creature's memory
            var memory = new EmotionalMemory
            {
                type = EmotionalMemoryType.BondingMoment,
                strength = moment.memoryStrength,
                timestamp = moment.timestamp,
                triggerConditions = CreateTriggerConditionsList(GetMomentTriggerConditions(moment.type)),
                isActive = true,
                description = moment.description
            };

            AddEmotionalMemory(ref bondData, memory);

            // Update bond strength
            bondData.bondStrength += _config.GetMomentBondImpact(moment.type);
            bondData.positiveExperiences++;
        }

        private void AddEmotionalMemory(ref CreatureBondData bondData, EmotionalMemory memory)
        {
            // Find empty slot or replace oldest memory
            int targetIndex = -1;
            float oldestTime = float.MaxValue;

            for (int i = 0; i < bondData.emotionalMemories.Length; i++)
            {
                if (!bondData.emotionalMemories[i].isActive)
                {
                    targetIndex = i;
                    break;
                }

                if (bondData.emotionalMemories[i].timestamp < oldestTime)
                {
                    oldestTime = bondData.emotionalMemories[i].timestamp;
                    targetIndex = i;
                }
            }

            if (targetIndex >= 0)
            {
                bondData.emotionalMemories[targetIndex] = memory;
            }
        }

        private float CalculateGeneticSimilarity(in CreatureGeneticsComponent current, in CreatureGeneticsComponent past)
        {
            // Calculate similarity based on genetic traits
            float strengthDiff = Mathf.Abs(current.StrengthTrait - past.StrengthTrait);
            float vitalityDiff = Mathf.Abs(current.VitalityTrait - past.VitalityTrait);
            float agilityDiff = Mathf.Abs(current.AgilityTrait - past.AgilityTrait);
            float intellectDiff = Mathf.Abs(current.IntellectTrait - past.IntellectTrait);

            float avgDiff = (strengthDiff + vitalityDiff + agilityDiff + intellectDiff) / 4f;
            return 1f - avgDiff;
        }

        private float CalculateGeneticSimilarityFromHash(in CreatureGeneticsComponent current, uint geneticHash)
        {
            // Calculate similarity by comparing current genetic profile with stored hash
            // This is a simplified comparison since we only have the hash, not full genetic data
            uint currentHash = (uint)(current.StrengthTrait * 1000 + current.VitalityTrait * 100 + current.AgilityTrait * 10 + current.IntellectTrait).GetHashCode();

            // Simple hash-based similarity calculation
            uint xorResult = currentHash ^ geneticHash;
            int diffBits = CountSetBits(xorResult);

            // Convert bit differences to similarity percentage (0-1 range)
            float similarity = 1f - (diffBits / 32f); // Assuming 32-bit hash
            return Mathf.Clamp01(similarity);
        }

        private int CountSetBits(uint value)
        {
            int count = 0;
            while (value != 0)
            {
                count++;
                value &= (value - 1); // Clear the lowest set bit
            }
            return count;
        }

        private LegacyConnectionType DetermineLegacyType(float similarity, PastBondData pastBond)
        {
            if (similarity > 0.9f) return LegacyConnectionType.DirectDescendant;
            if (similarity > 0.7f) return LegacyConnectionType.CloseRelative;
            if (similarity > 0.5f) return LegacyConnectionType.DistantRelative;
            return LegacyConnectionType.SharedAncestor;
        }

        private string GenerateLegacyDescription(in CreatureGeneticsComponent current, PastBondData past, float similarity)
        {
            var connectionType = DetermineLegacyType(similarity, past);
            return _config.GetLegacyDescription(connectionType, past.creatureName.ToString(), similarity);
        }

        private void AddLegacyConnection(ref PlayerBondingHistory history, LegacyConnection connection)
        {
            // Find empty slot
            for (int i = 0; i < history.legacyConnections.Length; i++)
            {
                if (!history.legacyConnections[i].isActive)
                {
                    history.legacyConnections[i] = connection;
                    return;
                }
            }
        }

        private void TriggerAncestralMemory(ref CreatureBondData bondData, LegacyConnection connection)
        {
            var memory = new EmotionalMemory
            {
                type = EmotionalMemoryType.AncestralMemory,
                strength = connection.memoryStrength,
                timestamp = (float)SystemAPI.Time.ElapsedTime,
                triggerConditions = CreateTriggerConditionsList(GetAncestralTriggerConditions(connection)),
                isActive = true,
                description = $"Ancestral memory of {connection.ancestorCreatureId}"
            };

            AddEmotionalMemory(ref bondData, memory);
            bondData.hasLegacyConnection = true;
        }

        private bool ShouldTriggerMemory(EmotionalMemory memory, CreatureBondData bondData)
        {
            // Check if current conditions match memory trigger conditions
            return memory.strength > _config.memoryTriggerThreshold &&
                   CheckMemoryTriggerConditions(memory, bondData);
        }

        private void TriggerEmotionalMemory(Entity entity, ref CreatureBondData bondData, EmotionalMemory memory)
        {
            // Trigger memory effect
            OnMemoryTriggered?.Invoke(bondData.playerId, memory);

            // Temporarily boost bond strength
            bondData.bondStrength += memory.strength * _config.memoryBondBoost;

            // Update creature's emotional state
            ApplyMemoryEmotionalEffect(ref bondData, memory);
        }

        private void ApplyMemoryEmotionalEffect(ref CreatureBondData bondData, EmotionalMemory memory)
        {
            switch (memory.type)
            {
                case EmotionalMemoryType.BondingMoment:
                    bondData.currentBondingEmotionalState = BondingEmotionalState.Happy;
                    break;
                case EmotionalMemoryType.AncestralMemory:
                    bondData.currentBondingEmotionalState = BondingEmotionalState.Nostalgic;
                    break;
                case EmotionalMemoryType.TraumaticEvent:
                    bondData.currentBondingEmotionalState = BondingEmotionalState.Anxious;
                    break;
            }

            bondData.emotionalStateTimer = _config.emotionalStateDuration;
        }

        private void UpdateBondingEmotionalState(ref CreatureBondData bondData, float deltaTime)
        {
            bondData.emotionalStateTimer -= deltaTime;

            if (bondData.emotionalStateTimer <= 0f)
            {
                bondData.currentBondingEmotionalState = BondingEmotionalState.Neutral;
            }
        }

        private void ProcessActiveMemories(ref CreatureBondData bondData, float deltaTime)
        {
            // Process memory consolidation and strengthening
            for (int i = 0; i < bondData.emotionalMemories.Length; i++)
            {
                if (bondData.emotionalMemories[i].isActive)
                {
                    // Strengthen frequently accessed memories
                    if (bondData.emotionalMemories[i].strength > _config.memoryConsolidationThreshold)
                    {
                        var memory = bondData.emotionalMemories[i];
                        memory.strength += _config.memoryConsolidationRate * deltaTime;
                        bondData.emotionalMemories[i] = memory;
                    }
                }
            }
        }

        private float CalculateCreatureRarity(in CreatureGeneticsComponent genetics)
        {
            // Calculate rarity based on genetic traits
            float rarity = 0f;
            rarity += genetics.StrengthTrait * _config.GetTraitRarity("Strength");
            rarity += genetics.VitalityTrait * _config.GetTraitRarity("Vitality");
            rarity += genetics.AgilityTrait * _config.GetTraitRarity("Agility");
            rarity += genetics.IntellectTrait * _config.GetTraitRarity("Intelligence");
            return rarity / 4f;
        }

        private bool DoesConnectionMatchPattern(LegacyConnection connection, GenerationalPattern pattern)
        {
            return pattern.connectionTypes.Contains(connection.connectionType);
        }

        private bool ShouldCreateNewPattern(LegacyConnection connection, PlayerBondingHistory history)
        {
            return history.totalGenerations > _config.minGenerationsForPattern;
        }

        private void CreateGenerationalPattern(ref PlayerBondingHistory history, LegacyConnection connection)
        {
            var pattern = new GenerationalPattern
            {
                patternType = DeterminePatternType(connection, history),
                strength = _config.initialPatternStrength,
                connectionTypes = CreateConnectionTypesList(new[] { connection.connectionType }),
                isActive = true,
                discoveredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddGenerationalPattern(ref history, pattern);
        }

        private GenerationalPatternType DeterminePatternType(LegacyConnection connection, PlayerBondingHistory history)
        {
            // Analyze the type of pattern based on connection and history
            return GenerationalPatternType.TraitInheritance; // Simplified for example
        }

        private void AddGenerationalPattern(ref PlayerBondingHistory history, GenerationalPattern pattern)
        {
            for (int i = 0; i < history.generationalPatterns.Length; i++)
            {
                if (!history.generationalPatterns[i].isActive)
                {
                    history.generationalPatterns[i] = pattern;
                    return;
                }
            }
        }

        private Laboratory.Chimera.ECS.MemoryTriggerCondition[] GetMomentTriggerConditions(BondingMomentType type)
        {
            var socialConditions = _config.GetMomentTriggerConditions(type);
            var ecsConditions = new Laboratory.Chimera.ECS.MemoryTriggerCondition[socialConditions.Length];

            for (int i = 0; i < socialConditions.Length; i++)
            {
                ecsConditions[i] = ConvertToECSMemoryTriggerCondition(socialConditions[i]);
            }

            return ecsConditions;
        }

        private Laboratory.Chimera.ECS.MemoryTriggerCondition[] GetAncestralTriggerConditions(LegacyConnection connection)
        {
            var socialConditions = _config.GetAncestralTriggerConditions(connection.connectionType);
            var ecsConditions = new Laboratory.Chimera.ECS.MemoryTriggerCondition[socialConditions.Length];

            for (int i = 0; i < socialConditions.Length; i++)
            {
                ecsConditions[i] = ConvertToECSMemoryTriggerCondition(socialConditions[i]);
            }

            return ecsConditions;
        }

        private FixedList64Bytes<Laboratory.Chimera.ECS.MemoryTriggerCondition> CreateTriggerConditionsList(Laboratory.Chimera.ECS.MemoryTriggerCondition[] conditions)
        {
            var fixedList = new FixedList64Bytes<Laboratory.Chimera.ECS.MemoryTriggerCondition>();
            if (conditions != null)
            {
                for (int i = 0; i < conditions.Length && i < fixedList.Capacity; i++)
                {
                    fixedList.Add(conditions[i]);
                }
            }
            return fixedList;
        }

        private FixedList32Bytes<LegacyConnectionType> CreateConnectionTypesList(LegacyConnectionType[] types)
        {
            var fixedList = new FixedList32Bytes<LegacyConnectionType>();
            if (types != null)
            {
                for (int i = 0; i < types.Length && i < fixedList.Capacity; i++)
                {
                    fixedList.Add(types[i]);
                }
            }
            return fixedList;
        }

        private bool CheckMemoryTriggerConditions(EmotionalMemory memory, CreatureBondData bondData)
        {
            foreach (var condition in memory.triggerConditions)
            {
                if (EvaluateTriggerCondition(condition, bondData))
                    return true;
            }
            return false;
        }

        private bool EvaluateTriggerCondition(Laboratory.Chimera.ECS.MemoryTriggerCondition condition, CreatureBondData bondData)
        {
            switch (condition.type)
            {
                case TriggerConditionType.BondStrengthThreshold:
                    return bondData.bondStrength >= condition.threshold;
                case TriggerConditionType.BondingEmotionalState:
                    return bondData.currentBondingEmotionalState == condition.requiredState;
                case TriggerConditionType.TimeOfDay:
                    return CheckTimeCondition(condition);
                case TriggerConditionType.InteractionType:
                    return bondData.lastInteractionType == condition.requiredInteraction;
                default:
                    return false;
            }
        }

        private bool CheckTimeCondition(Laboratory.Chimera.ECS.MemoryTriggerCondition condition)
        {
            // Check if current time matches condition
            float currentTime = (float)SystemAPI.Time.ElapsedTime % 86400f; // Seconds in a day
            return currentTime >= condition.timeRange.x && currentTime <= condition.timeRange.y;
        }

        private Laboratory.Chimera.ECS.MemoryTriggerCondition ConvertToECSMemoryTriggerCondition(Laboratory.Chimera.Social.MemoryTriggerCondition socialCondition)
        {
            return new Laboratory.Chimera.ECS.MemoryTriggerCondition
            {
                type = (TriggerConditionType)socialCondition.type,
                threshold = socialCondition.threshold,
                requiredState = socialCondition.requiredState,
                timeRange = new Vector2(socialCondition.timeRange, socialCondition.timeRange),
                requiredInteraction = socialCondition.requiredInteraction
            };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Component data for creature bonding
    /// </summary>
    [Serializable]
    public struct CreatureBondData : IComponentData
    {
        public int playerId;
        public int creatureId;
        public float bondStrength;
        public float loyaltyLevel;
        public float timeSinceLastInteraction;
        public float timeAlive;

        // Bonding milestones
        public bool hasHadFirstInteraction;
        public bool hasTrustBreakthrough;
        public bool hasAdolescenceMoment;
        public bool hasMaturityMoment;
        public bool hasSharedDiscoveryMoment;
        public bool participatedInDiscovery;
        public bool hasLegacyConnection;

        // Experience tracking
        public int positiveExperiences;
        public int negativeExperiences;
        public int recentPositiveInteractions;
        public BondingInteractionType lastInteractionType;

        // Emotional state
        public BondingEmotionalState currentBondingEmotionalState;
        public float emotionalStateTimer;

        // Memory system
        public FixedList128Bytes<EmotionalMemory> emotionalMemories;
    }

    /// <summary>
    /// Player's bonding history across all creatures
    /// </summary>
    [Serializable]
    public struct PlayerBondingHistory : IComponentData
    {
        public int playerId;
        public int totalCreaturesBonded;
        public int totalGenerations;
        public float overallBondingExpertise;

        // Legacy connections
        public FixedList128Bytes<LegacyConnection> legacyConnections;
        public FixedList128Bytes<PastBondData> pastBonds;
        public FixedList128Bytes<GenerationalPattern> generationalPatterns;
    }

    /// <summary>
    /// Data about a bonding moment
    /// </summary>
    [Serializable]
    public struct BondingMoment
    {
        public BondingMomentType type;
        public float intensity;
        public float timestamp;
        public string description;
        public float memoryStrength;
    }

    /// <summary>
    /// Connection between current creature and past bonded creatures
    /// </summary>
    [Serializable]
    public struct LegacyConnection
    {
        public int currentCreatureId;
        public int ancestorCreatureId;
        public LegacyConnectionType connectionType;
        public float memoryStrength;
        public float discoveredAt;
        public bool isActive;
        public FixedString64Bytes description;
    }

    /// <summary>
    /// Emotional memory stored by creatures
    /// </summary>
    [Serializable]
    public struct EmotionalMemory
    {
        public EmotionalMemoryType type;
        public float strength;
        public float timestamp;
        public FixedList64Bytes<Laboratory.Chimera.ECS.MemoryTriggerCondition> triggerConditions;
        public bool isActive;
        public FixedString64Bytes description;
    }

    /// <summary>
    /// Data about past bonded creatures
    /// </summary>
    [Serializable]
    public struct PastBondData
    {
        public int creatureId;
        public FixedString32Bytes creatureName;
        public uint geneticHash; // Simplified genetics reference
        public float finalBondStrength;
        public float bondDurationDays;
        public bool isActive;
        public float endedAt;
    }

    /// <summary>
    /// Pattern recognition across generations
    /// </summary>
    [Serializable]
    public struct GenerationalPattern
    {
        public GenerationalPatternType patternType;
        public float strength;
        public FixedList32Bytes<LegacyConnectionType> connectionTypes;
        public bool isActive;
        public float discoveredAt;
    }

    /// <summary>
    /// Conditions that trigger memory recall
    /// </summary>
    [Serializable]
    public struct MemoryTriggerCondition
    {
        public TriggerConditionType type;
        public float threshold;
        public BondingEmotionalState requiredState;
        public Vector2 timeRange;
        public BondingInteractionType requiredInteraction;
    }

    #endregion
}