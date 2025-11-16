using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Genetics.Core;
using Laboratory.Chimera.Social;
using Laboratory.Chimera.Consciousness;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// Advanced creature wisdom and sage system.
    /// Creates elderly creatures that gain wisdom through experience and can guide younger generations.
    /// </summary>
    public partial class CreatureWisdomSystem : SystemBase
    {
        private CreatureWisdomConfig _config;
        private EntityQuery _wisdomCreaturesQuery;
        private EntityQuery _youngCreaturesQuery;
        private EntityQuery _seekingGuidanceQuery;

        // Wisdom sharing network
        private Dictionary<int, List<WisdomEntry>> _wisdomDatabase = new Dictionary<int, List<WisdomEntry>>();
        private Dictionary<int, List<MentorshipRelation>> _mentorships = new Dictionary<int, List<MentorshipRelation>>();

        // Event system
        public static event Action<int, WisdomGained> OnWisdomGained;
        public static event Action<int, SageAscension> OnSageAscension;
        public static event Action<int, MentorshipFormed> OnMentorshipFormed;
        public static event Action<int, WisdomShared> OnWisdomShared;
        public static event Action<int, CreatureLegacy> OnLegacyCreated;

        protected override void OnCreate()
        {
            _config = Resources.Load<CreatureWisdomConfig>("Configs/CreatureWisdomConfig");
            if (_config == null)
            {
                UnityEngine.Debug.LogError("CreatureWisdomConfig not found in Resources/Configs/");
                return;
            }

            _wisdomCreaturesQuery = GetEntityQuery(
                ComponentType.ReadWrite<WisdomData>(),
                ComponentType.ReadOnly<CreatureGeneticsComponent>()
            );

            _youngCreaturesQuery = GetEntityQuery(
                ComponentType.ReadOnly<CreatureGeneticsComponent>(),
                ComponentType.ReadWrite<LearningData>()
            );

            _seekingGuidanceQuery = GetEntityQuery(
                ComponentType.ReadWrite<GuidanceSeekingData>()
            );
        }

        protected override void OnUpdate()
        {
            if (_config == null) return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Process wisdom accumulation
            ProcessWisdomAccumulation(deltaTime);

            // Handle sage ascensions
            ProcessSageAscensions();

            // Manage mentorship relationships
            ProcessMentorships(deltaTime);

            // Share wisdom between creatures
            ProcessWisdomSharing(deltaTime);

            // Update learning from mentors
            ProcessLearningFromMentors(deltaTime);

            // Generate creature legacies
            ProcessLegacyGeneration(deltaTime);

            // Handle wisdom seeking behavior
            ProcessWisdomSeeking(deltaTime);
        }

        private void ProcessWisdomAccumulation(float deltaTime)
        {
            foreach (var (wisdomData, genetics, entity) in SystemAPI.Query<RefRW<WisdomData>, RefRO<CreatureGeneticsComponent>>().WithEntityAccess())
            {
                // Increase age
                wisdomData.ValueRW.ageInDays += deltaTime / (24f * 3600f);

                // Accumulate wisdom based on experiences
                AccumulateWisdomFromExperiences(entity, ref wisdomData.ValueRW, genetics.ValueRO, deltaTime);

                // Process wisdom categories
                UpdateWisdomCategories(ref wisdomData.ValueRW, deltaTime);

                // Check for wisdom milestones
                CheckWisdomMilestones(entity, ref wisdomData.ValueRW, genetics.ValueRO);

                // Update sage status
                UpdateSageStatus(entity, ref wisdomData.ValueRW, genetics.ValueRO);
            }
        }

        private void AccumulateWisdomFromExperiences(Entity entity, ref WisdomData wisdomData, in CreatureGeneticsComponent genetics, float deltaTime)
        {
            // Base wisdom gain from aging
            float ageWisdomGain = _config.ageWisdomRate * deltaTime;
            wisdomData.totalWisdom += ageWisdomGain;

            // Wisdom from successful interactions
            if (wisdomData.recentSuccessfulInteractions > 0)
            {
                float interactionWisdom = wisdomData.recentSuccessfulInteractions * _config.interactionWisdomMultiplier;
                wisdomData.totalWisdom += interactionWisdom;

                // Update specific wisdom categories
                wisdomData.socialWisdom += interactionWisdom * 0.4f;
                wisdomData.practicalWisdom += interactionWisdom * 0.3f;
                wisdomData.emotionalWisdom += interactionWisdom * 0.3f;

                wisdomData.recentSuccessfulInteractions = 0;
            }

            // Wisdom from overcoming challenges
            if (wisdomData.challengesOvercome > wisdomData.lastChallengeCount)
            {
                int newChallenges = wisdomData.challengesOvercome - wisdomData.lastChallengeCount;
                float challengeWisdom = newChallenges * _config.challengeWisdomBonus;
                wisdomData.totalWisdom += challengeWisdom;
                wisdomData.survivalWisdom += challengeWisdom;

                CreateWisdomEntry(entity, WisdomType.Survival, challengeWisdom, "Learned from overcoming challenges");
                wisdomData.lastChallengeCount = wisdomData.challengesOvercome;
            }

            // Wisdom from teaching others
            if (wisdomData.timesSharedWisdom > wisdomData.lastTeachingCount)
            {
                int newTeaching = wisdomData.timesSharedWisdom - wisdomData.lastTeachingCount;
                float teachingWisdom = newTeaching * _config.teachingWisdomBonus;
                wisdomData.totalWisdom += teachingWisdom;
                wisdomData.mentoringWisdom += teachingWisdom;

                CreateWisdomEntry(entity, WisdomType.Mentoring, teachingWisdom, "Gained wisdom through teaching");
                wisdomData.lastTeachingCount = wisdomData.timesSharedWisdom;
            }
        }

        private void UpdateWisdomCategories(ref WisdomData wisdomData, float deltaTime)
        {
            // Natural wisdom category growth based on creature traits and behaviors
            if (wisdomData.ageInDays > _config.matureAgeThreshold)
            {
                float maturityBonus = (wisdomData.ageInDays - _config.matureAgeThreshold) * _config.maturityWisdomRate * deltaTime;

                // Distribute wisdom across categories based on creature's natural inclinations
                wisdomData.lifeWisdom += maturityBonus * 0.25f;
                wisdomData.spiritualWisdom += maturityBonus * 0.25f;
                wisdomData.practicalWisdom += maturityBonus * 0.25f;
                wisdomData.emotionalWisdom += maturityBonus * 0.25f;
            }

            // Calculate total specialized wisdom
            wisdomData.specializedWisdom = wisdomData.survivalWisdom + wisdomData.socialWisdom +
                                          wisdomData.mentoringWisdom + wisdomData.lifeWisdom + wisdomData.spiritualWisdom;
        }

        private void CheckWisdomMilestones(Entity entity, ref WisdomData wisdomData, in CreatureGeneticsComponent genetics)
        {
            // Check for wisdom milestones
            foreach (var milestone in _config.wisdomMilestones)
            {
                if (wisdomData.totalWisdom >= milestone.wisdomThreshold && !wisdomData.achievedMilestones.Contains(milestone.milestoneId))
                {
                    // Add milestone to achieved list
                    if (wisdomData.achievedMilestones.Length < wisdomData.achievedMilestones.Capacity)
                    {
                        wisdomData.achievedMilestones.Add(milestone.milestoneId);

                        var wisdomGained = new WisdomGained
                        {
                            creatureId = wisdomData.creatureId,
                            milestoneReached = milestone,
                            totalWisdom = wisdomData.totalWisdom,
                            newAbilities = GetMilestoneAbilities(milestone),
                            timestamp = (float)SystemAPI.Time.ElapsedTime
                        };

                        OnWisdomGained?.Invoke(wisdomData.creatureId, wisdomGained);

                        // Grant milestone abilities
                        GrantMilestoneAbilities(entity, ref wisdomData, milestone);
                    }
                }
            }
        }

        private void UpdateSageStatus(Entity entity, ref WisdomData wisdomData, in CreatureGeneticsComponent genetics)
        {
            var newSageLevel = CalculateSageLevel(wisdomData);

            if (newSageLevel > wisdomData.sageLevel)
            {
                var previousLevel = wisdomData.sageLevel;
                wisdomData.sageLevel = newSageLevel;

                var ascension = new SageAscension
                {
                    creatureId = wisdomData.creatureId,
                    previousLevel = previousLevel,
                    newLevel = newSageLevel,
                    totalWisdom = wisdomData.totalWisdom,
                    specializations = GetSageSpecializations(wisdomData),
                    timestamp = (float)SystemAPI.Time.ElapsedTime
                };

                OnSageAscension?.Invoke(wisdomData.creatureId, ascension);

                // Grant sage abilities
                GrantSageAbilities(entity, ref wisdomData, newSageLevel);
            }
        }

        private void ProcessSageAscensions()
        {
            foreach (var (wisdomData, entity) in SystemAPI.Query<RefRW<WisdomData>>().WithEntityAccess())
            {
                if (wisdomData.ValueRO.sageLevel >= SageLevel.Elder)
                {
                    // Process elder sage responsibilities
                    ProcessElderSageResponsibilities(entity, ref wisdomData.ValueRW);

                    // Handle succession planning
                    if (wisdomData.ValueRO.sageLevel == SageLevel.Ancient && wisdomData.ValueRO.ageInDays > _config.legacyPreparationAge)
                    {
                        PrepareSuccession(entity, ref wisdomData.ValueRW);
                    }
                }
            }
        }

        private void ProcessMentorships(float deltaTime)
        {
            foreach (var (mentorWisdom, mentorEntity) in SystemAPI.Query<RefRW<WisdomData>>().WithEntityAccess())
            {
                if (mentorWisdom.ValueRO.sageLevel < SageLevel.Sage) continue;

                // Update existing mentorships
                UpdateExistingMentorships(mentorEntity, ref mentorWisdom.ValueRW, deltaTime);

                // Look for new mentorship opportunities
                if (mentorWisdom.ValueRO.activeMentorships < _config.maxMentorshipsPerSage[(int)mentorWisdom.ValueRO.sageLevel])
                {
                    FindNewMentorshipOpportunities(mentorEntity, ref mentorWisdom.ValueRW);
                }
            }
        }

        private void UpdateExistingMentorships(Entity mentorEntity, ref WisdomData mentorWisdom, float deltaTime)
        {
            if (!_mentorships.ContainsKey(mentorWisdom.creatureId)) return;

            var mentorshipList = _mentorships[mentorWisdom.creatureId];

            for (int i = mentorshipList.Count - 1; i >= 0; i--)
            {
                var mentorship = mentorshipList[i];
                mentorship.duration += deltaTime;

                // Check if mentorship should continue
                if (ShouldEndMentorship(mentorship))
                {
                    CompleteMentorship(mentorEntity, ref mentorWisdom, mentorship);
                    mentorshipList.RemoveAt(i);
                }
                else
                {
                    // Progress the mentorship
                    ProgressMentorship(mentorEntity, ref mentorWisdom, mentorship, deltaTime);
                    mentorshipList[i] = mentorship;
                }
            }
        }

        private void FindNewMentorshipOpportunities(Entity mentorEntity, ref WisdomData mentorWisdom)
        {
            // Copy ref parameter to local variable
            var localMentorWisdom = mentorWisdom;

            // Look for young creatures seeking guidance
            foreach (var (learningData, studentEntity) in SystemAPI.Query<RefRW<LearningData>>().WithEntityAccess())
            {
                if (learningData.ValueRO.hasActiveMentor) continue;
                if (!IsCompatibleForMentorship(localMentorWisdom, learningData.ValueRO)) continue;

                // Form new mentorship
                var mentorship = new MentorshipRelation
                {
                    mentorId = localMentorWisdom.creatureId,
                    studentId = learningData.ValueRO.creatureId,
                    startTime = (float)SystemAPI.Time.ElapsedTime,
                    duration = 0f,
                    mentorshipType = DetermineMentorshipType(localMentorWisdom, learningData.ValueRO),
                    progress = 0f,
                    lessonsShared = 0,
                    wisdomTransferred = 0f
                };

                // Add to mentorship tracking
                if (!_mentorships.ContainsKey(localMentorWisdom.creatureId))
                    _mentorships[localMentorWisdom.creatureId] = new List<MentorshipRelation>();

                _mentorships[localMentorWisdom.creatureId].Add(mentorship);
                learningData.ValueRW.hasActiveMentor = true;
                learningData.ValueRW.mentorId = localMentorWisdom.creatureId;

                var mentorshipFormed = new MentorshipFormed
                {
                    mentorId = localMentorWisdom.creatureId,
                    studentId = learningData.ValueRO.creatureId,
                    mentorshipType = mentorship.mentorshipType,
                    expectedDuration = CalculateExpectedMentorshipDuration(mentorship),
                    timestamp = (float)SystemAPI.Time.ElapsedTime
                };

                OnMentorshipFormed?.Invoke(localMentorWisdom.creatureId, mentorshipFormed);
            }

            // Update the ref parameter with any changes
            mentorWisdom.activeMentorships++;
        }

        private void ProcessWisdomSharing(float deltaTime)
        {
            foreach (var (wisdomData, entity) in SystemAPI.Query<RefRW<WisdomData>>().WithEntityAccess())
            {
                if (wisdomData.ValueRO.sageLevel < SageLevel.Sage) continue;

                wisdomData.ValueRW.wisdomSharingCooldown -= deltaTime;

                if (wisdomData.ValueRO.wisdomSharingCooldown <= 0f)
                {
                    // Share wisdom with nearby creatures
                    var wisdomValue = wisdomData.ValueRW;
                    ShareWisdomWithNearbyCreatures(entity, ref wisdomValue);
                    wisdomData.ValueRW = wisdomValue;
                    wisdomData.ValueRW.wisdomSharingCooldown = _config.wisdomSharingInterval;
                }
            }
        }

        private void ShareWisdomWithNearbyCreatures(Entity sageEntity, ref WisdomData sageWisdom)
        {
            // Copy ref parameter to local variable
            var localSageWisdom = sageWisdom;

            // Find nearby creatures that can benefit from wisdom
            foreach (var (learningData, learnerEntity) in SystemAPI.Query<RefRW<LearningData>>().WithEntityAccess())
            {
                if (learningData.ValueRO.creatureId == localSageWisdom.creatureId) continue;

                // Check if wisdom sharing is beneficial
                if (CanBenefitFromWisdom(learningData.ValueRO, localSageWisdom))
                {
                    var wisdomToShare = SelectWisdomToShare(localSageWisdom, learningData.ValueRO);

                    if (wisdomToShare.HasValue)
                    {
                        // We'll handle the transfer outside the loop
                        // Store transfer data for processing
                    }
                }
            }
        }

        private void TransferWisdom(Entity sageEntity, ref WisdomData sageWisdom, Entity learnerEntity, ref LearningData learningData, WisdomEntry wisdom)
        {
            // Transfer wisdom to learner
            float wisdomAmount = wisdom.value * _config.wisdomTransferEfficiency;
            learningData.accumulatedWisdom += wisdomAmount;

            // Apply wisdom to appropriate category
            ApplyWisdomToLearner(ref learningData, wisdom, wisdomAmount);

            // Update sage sharing statistics
            sageWisdom.timesSharedWisdom++;
            sageWisdom.totalWisdomShared += wisdomAmount;

            var wisdomShared = new WisdomShared
            {
                sageId = sageWisdom.creatureId,
                learnerId = learningData.creatureId,
                wisdomType = wisdom.type,
                wisdomAmount = wisdomAmount,
                lessonTitle = wisdom.description.ToString(),
                timestamp = (float)SystemAPI.Time.ElapsedTime
            };

            OnWisdomShared?.Invoke(sageWisdom.creatureId, wisdomShared);

            // Create wisdom entry for learner
            CreateWisdomEntryForLearner(learnerEntity, learningData.creatureId, wisdom, wisdomAmount);
        }

        private void ProcessLearningFromMentors(float deltaTime)
        {
            Entities.WithAll<LearningData>().WithoutBurst().ForEach((Entity entity, ref LearningData learningData) =>
            {
                if (!learningData.hasActiveMentor) return;

                // Process learning progress
                learningData.learningTimer += deltaTime;

                if (learningData.learningTimer >= _config.learningInterval)
                {
                    // Process learning session inline instead of calling method with ref
                    float learningGain = _config.baseLearningRate * learningData.learningMultiplier;
                    learningData.accumulatedWisdom += learningGain;
                    learningData.learningTimer = 0f;
                }

                // Update learning capacity inline
                learningData.learningCapacity = _config.baseLearningCapacity +
                                               (learningData.accumulatedWisdom * _config.wisdomLearningMultiplier);
            }).Run();
        }


        private void ProcessLegacyGeneration(float deltaTime)
        {
            Entities.WithAll<WisdomData>().WithoutBurst().ForEach((Entity entity, ref WisdomData wisdomData) =>
            {
                if (wisdomData.sageLevel >= SageLevel.Elder && wisdomData.ageInDays > _config.legacyPreparationAge)
                {
                    wisdomData.legacyPreparationTimer += deltaTime;

                    if (wisdomData.legacyPreparationTimer >= _config.legacyCreationInterval && !wisdomData.hasCreatedLegacy)
                    {
                        CreateCreatureLegacy(entity, ref wisdomData);
                    }
                }
            }).Run();
        }

        private void CreateCreatureLegacy(Entity entity, ref WisdomData wisdomData)
        {
            var legacy = new CreatureLegacy
            {
                sageId = wisdomData.creatureId,
                totalWisdom = wisdomData.totalWisdom,
                mentorshipsCompleted = wisdomData.completedMentorships,
                wisdomShared = wisdomData.totalWisdomShared,
                specializations = GetSageSpecializations(wisdomData),
                lifeLessons = GenerateLifeLessons(wisdomData),
                wisdomQuotes = GenerateWisdomQuotes(wisdomData),
                mentoringPhilosophy = GenerateMentoringPhilosophy(wisdomData),
                timestamp = (float)SystemAPI.Time.ElapsedTime
            };

            wisdomData.hasCreatedLegacy = true;
            OnLegacyCreated?.Invoke(wisdomData.creatureId, legacy);

            // Store legacy in wisdom database for future generations
            StoreLegacyInDatabase(legacy);
        }

        private void ProcessWisdomSeeking(float deltaTime)
        {
            Entities.WithAll<GuidanceSeekingData>().WithoutBurst().ForEach((Entity entity, ref GuidanceSeekingData seekingData) =>
            {
                seekingData.seekingTimer += deltaTime;

                if (seekingData.seekingTimer >= _config.guidanceSeekingInterval)
                {
                    ProcessGuidanceSeeking(entity, ref seekingData);
                    seekingData.seekingTimer = 0f;
                }
            }).Run();
        }

        private void ProcessGuidanceSeeking(Entity entity, ref GuidanceSeekingData seekingData)
        {
            // Find appropriate sage for guidance
            var availableSages = FindAvailableSages(seekingData.guidanceType);

            if (availableSages.Any())
            {
                var selectedSage = SelectBestSage(availableSages, seekingData);
                RequestGuidance(entity, ref seekingData, selectedSage);
            }
        }

        #region Helper Methods

        private void CreateWisdomEntry(Entity entity, WisdomType type, float value, string description)
        {
            var wisdomData = SystemAPI.GetComponent<WisdomData>(entity);

            var entry = new WisdomEntry
            {
                type = type,
                value = value,
                description = description,
                timestamp = (float)SystemAPI.Time.ElapsedTime,
                sourceCreatureId = wisdomData.creatureId
            };

            if (!_wisdomDatabase.ContainsKey(wisdomData.creatureId))
                _wisdomDatabase[wisdomData.creatureId] = new List<WisdomEntry>();

            _wisdomDatabase[wisdomData.creatureId].Add(entry);

            // Limit wisdom entries per creature
            if (_wisdomDatabase[wisdomData.creatureId].Count > _config.maxWisdomEntriesPerCreature)
            {
                _wisdomDatabase[wisdomData.creatureId].RemoveAt(0);
            }
        }

        private SageLevel CalculateSageLevel(WisdomData wisdomData)
        {
            if (wisdomData.totalWisdom >= _config.ancientSageThreshold && wisdomData.ageInDays >= _config.ancientAgeRequirement)
                return SageLevel.Ancient;
            if (wisdomData.totalWisdom >= _config.elderSageThreshold && wisdomData.ageInDays >= _config.elderAgeRequirement)
                return SageLevel.Elder;
            if (wisdomData.totalWisdom >= _config.wiseSageThreshold && wisdomData.ageInDays >= _config.wiseAgeRequirement)
                return SageLevel.Wise;
            if (wisdomData.totalWisdom >= _config.basicSageThreshold && wisdomData.ageInDays >= _config.sageAgeRequirement)
                return SageLevel.Sage;

            return SageLevel.None;
        }

        private string[] GetMilestoneAbilities(WisdomMilestone milestone)
        {
            return milestone.abilitiesGranted;
        }

        private void GrantMilestoneAbilities(Entity entity, ref WisdomData wisdomData, WisdomMilestone milestone)
        {
            // Grant abilities based on milestone
            if (milestone.abilitiesGranted != null)
            {
                for (int i = 0; i < milestone.abilitiesGranted.Length && wisdomData.availableAbilities.Length < wisdomData.availableAbilities.Capacity; i++)
                {
                    wisdomData.availableAbilities.Add(milestone.abilitiesGranted[i]);
                }
            }
        }

        private string[] GetSageSpecializations(WisdomData wisdomData)
        {
            var specializations = new List<string>();

            if (wisdomData.survivalWisdom > _config.specializationThreshold)
                specializations.Add("Survival Master");
            if (wisdomData.socialWisdom > _config.specializationThreshold)
                specializations.Add("Social Wisdom");
            if (wisdomData.mentoringWisdom > _config.specializationThreshold)
                specializations.Add("Master Mentor");
            if (wisdomData.lifeWisdom > _config.specializationThreshold)
                specializations.Add("Life Philosopher");
            if (wisdomData.spiritualWisdom > _config.specializationThreshold)
                specializations.Add("Spiritual Guide");

            return specializations.ToArray();
        }

        private void GrantSageAbilities(Entity entity, ref WisdomData wisdomData, SageLevel newLevel)
        {
            var abilities = _config.GetSageAbilities(newLevel);
            if (abilities != null)
            {
                for (int i = 0; i < abilities.Length && wisdomData.availableAbilities.Length < wisdomData.availableAbilities.Capacity; i++)
                {
                    wisdomData.availableAbilities.Add(abilities[i]);
                }
            }
        }

        private void ProcessElderSageResponsibilities(Entity entity, ref WisdomData wisdomData)
        {
            // Handle community leadership responsibilities
            // Guide major decisions, resolve conflicts, etc.
        }

        private void PrepareSuccession(Entity entity, ref WisdomData wisdomData)
        {
            // Prepare for passing wisdom to next generation
            wisdomData.isPreparingSuccession = true;
        }

        private bool ShouldEndMentorship(MentorshipRelation mentorship)
        {
            return mentorship.duration > _config.maxMentorshipDuration ||
                   mentorship.progress >= 1f;
        }

        private void CompleteMentorship(Entity mentorEntity, ref WisdomData mentorWisdom, MentorshipRelation mentorship)
        {
            mentorWisdom.completedMentorships++;
            mentorWisdom.activeMentorships--;

            // Grant wisdom bonus for completed mentorship
            float completionBonus = _config.mentorshipCompletionBonus;
            mentorWisdom.totalWisdom += completionBonus;
            mentorWisdom.mentoringWisdom += completionBonus;
        }

        private void ProgressMentorship(Entity mentorEntity, ref WisdomData mentorWisdom, MentorshipRelation mentorship, float deltaTime)
        {
            mentorship.progress += _config.mentorshipProgressRate * deltaTime;
            mentorship.progress = Mathf.Clamp01(mentorship.progress);
        }

        private bool IsCompatibleForMentorship(WisdomData mentor, LearningData student)
        {
            // Check compatibility based on wisdom levels, personality, etc.
            return mentor.totalWisdom > student.accumulatedWisdom * 2f &&
                   student.learningCapacity > _config.minLearningCapacityForMentorship;
        }

        private MentorshipType DetermineMentorshipType(WisdomData mentor, LearningData student)
        {
            // Determine best mentorship type based on mentor's strengths and student's needs
            if (mentor.survivalWisdom > mentor.socialWisdom)
                return MentorshipType.Survival;
            if (mentor.socialWisdom > mentor.practicalWisdom)
                return MentorshipType.Social;
            if (mentor.spiritualWisdom > mentor.emotionalWisdom)
                return MentorshipType.Spiritual;

            return MentorshipType.General;
        }

        private float CalculateExpectedMentorshipDuration(MentorshipRelation mentorship)
        {
            return _config.baseMentorshipDuration * _config.GetMentorshipDurationMultiplier(mentorship.mentorshipType);
        }

        private bool CanBenefitFromWisdom(LearningData learner, WisdomData sage)
        {
            return learner.accumulatedWisdom < sage.totalWisdom * 0.5f &&
                   learner.learningCapacity > 0f;
        }

        private WisdomEntry? SelectWisdomToShare(WisdomData sage, LearningData learner)
        {
            if (!_wisdomDatabase.ContainsKey(sage.creatureId))
                return null;

            var availableWisdom = _wisdomDatabase[sage.creatureId];
            if (!availableWisdom.Any())
                return null;

            // Select wisdom entry that would be most beneficial to learner
            return availableWisdom.OrderByDescending(w => CalculateWisdomBenefit(w, learner)).First();
        }

        private float CalculateWisdomBenefit(WisdomEntry wisdom, LearningData learner)
        {
            // Calculate how beneficial this wisdom would be for the learner
            return wisdom.value * learner.learningMultiplier;
        }

        private void ApplyWisdomToLearner(ref LearningData learningData, WisdomEntry wisdom, float amount)
        {
            switch (wisdom.type)
            {
                case WisdomType.Survival:
                    learningData.survivalKnowledge += amount;
                    break;
                case WisdomType.Social:
                    learningData.socialSkills += amount;
                    break;
                case WisdomType.Mentoring:
                    learningData.leadershipPotential += amount;
                    break;
                case WisdomType.Life:
                    learningData.lifeExperience += amount;
                    break;
                case WisdomType.Spiritual:
                    learningData.spiritualAwareness += amount;
                    break;
            }
        }

        private void CreateWisdomEntryForLearner(Entity learnerEntity, int learnerId, WisdomEntry originalWisdom, float amount)
        {
            var entry = new WisdomEntry
            {
                type = originalWisdom.type,
                value = amount,
                description = $"Learned from sage: {originalWisdom.description}",
                timestamp = (float)SystemAPI.Time.ElapsedTime,
                sourceCreatureId = originalWisdom.sourceCreatureId
            };

            if (!_wisdomDatabase.ContainsKey(learnerId))
                _wisdomDatabase[learnerId] = new List<WisdomEntry>();

            _wisdomDatabase[learnerId].Add(entry);
        }

        private void UpdateLearningCapacity(ref LearningData learningData)
        {
            // Update learning capacity based on accumulated wisdom and mentor quality
            learningData.learningCapacity = _config.baseLearningCapacity +
                                           (learningData.accumulatedWisdom * _config.wisdomLearningMultiplier);
        }

        private void ApplyMentorSpecificLearning(ref LearningData learningData, MentorshipRelation mentorship)
        {
            float mentorBonus = _config.GetMentorshipBonus(mentorship.mentorshipType);
            learningData.learningMultiplier = 1f + mentorBonus;
        }

        private void CheckLearningMilestones(Entity entity, ref LearningData learningData)
        {
            // Check if learner has reached any learning milestones
            foreach (var milestone in _config.learningMilestones)
            {
                if (learningData.accumulatedWisdom >= milestone.wisdomRequired &&
                    !learningData.achievedMilestones.Contains(milestone.milestoneId))
                {
                    if (learningData.achievedMilestones.Length < learningData.achievedMilestones.Capacity)
                    {
                        learningData.achievedMilestones.Add(milestone.milestoneId);
                        GrantLearningMilestoneRewards(entity, ref learningData, milestone);
                    }
                }
            }
        }

        private void GrantLearningMilestoneRewards(Entity entity, ref LearningData learningData, LearningMilestone milestone)
        {
            learningData.learningCapacity += milestone.capacityBonus;
            learningData.learningMultiplier += milestone.learningBonus;
        }

        private string[] GenerateLifeLessons(WisdomData wisdomData)
        {
            var lessons = new List<string>();

            if (wisdomData.survivalWisdom > _config.specializationThreshold)
                lessons.Add("Adaptation is the key to survival in changing environments");

            if (wisdomData.socialWisdom > _config.specializationThreshold)
                lessons.Add("True strength comes from community bonds and mutual support");

            if (wisdomData.mentoringWisdom > _config.specializationThreshold)
                lessons.Add("The greatest teachers learn as much from their students");

            if (wisdomData.lifeWisdom > _config.specializationThreshold)
                lessons.Add("Every moment of existence holds precious meaning");

            if (wisdomData.spiritualWisdom > _config.specializationThreshold)
                lessons.Add("Inner peace reflects the harmony of all living things");

            return lessons.ToArray();
        }

        private string[] GenerateWisdomQuotes(WisdomData wisdomData)
        {
            return new[]
            {
                "Wisdom flows like water, finding its way to those who need it most",
                "In teaching others, we discover the depths of our own understanding",
                "The eldest trees provide shade for all who seek shelter",
                "Experience is the teacher that gives the test before the lesson"
            };
        }

        private string GenerateMentoringPhilosophy(WisdomData wisdomData)
        {
            if (wisdomData.mentoringWisdom > wisdomData.totalWisdom * 0.3f)
                return "Guide with patience, teach with compassion, and lead by example";

            return "Share knowledge freely, for wisdom grows when given away";
        }

        private void StoreLegacyInDatabase(CreatureLegacy legacy)
        {
            // Store legacy for future access by other creatures
            // This would typically integrate with a persistent storage system
        }

        private Entity[] FindAvailableSages(GuidanceType guidanceType)
        {
            var availableSages = new List<Entity>();

            foreach (var (wisdomData, entity) in SystemAPI.Query<RefRO<WisdomData>>().WithEntityAccess())
            {
                if (wisdomData.ValueRO.sageLevel >= SageLevel.Sage && CanProvideGuidance(wisdomData.ValueRO, guidanceType))
                {
                    availableSages.Add(entity);
                }
            }

            return availableSages.ToArray();
        }

        private bool CanProvideGuidance(WisdomData sage, GuidanceType guidanceType)
        {
            switch (guidanceType)
            {
                case GuidanceType.Survival:
                    return sage.survivalWisdom > _config.guidanceWisdomThreshold;
                case GuidanceType.Social:
                    return sage.socialWisdom > _config.guidanceWisdomThreshold;
                case GuidanceType.Spiritual:
                    return sage.spiritualWisdom > _config.guidanceWisdomThreshold;
                case GuidanceType.Life:
                    return sage.lifeWisdom > _config.guidanceWisdomThreshold;
                default:
                    return sage.totalWisdom > _config.guidanceWisdomThreshold;
            }
        }

        private Entity SelectBestSage(Entity[] availableSages, GuidanceSeekingData seekingData)
        {
            // Select the most appropriate sage based on specialization and availability
            Entity bestSage = availableSages[0];
            float bestScore = 0f;

            foreach (var sage in availableSages)
            {
                var wisdomData = SystemAPI.GetComponent<WisdomData>(sage);
                float score = CalculateSageScore(wisdomData, seekingData.guidanceType);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestSage = sage;
                }
            }

            return bestSage;
        }

        private float CalculateSageScore(WisdomData sage, GuidanceType guidanceType)
        {
            float baseScore = sage.totalWisdom;

            switch (guidanceType)
            {
                case GuidanceType.Survival:
                    baseScore += sage.survivalWisdom * 2f;
                    break;
                case GuidanceType.Social:
                    baseScore += sage.socialWisdom * 2f;
                    break;
                case GuidanceType.Spiritual:
                    baseScore += sage.spiritualWisdom * 2f;
                    break;
                case GuidanceType.Life:
                    baseScore += sage.lifeWisdom * 2f;
                    break;
            }

            return baseScore;
        }

        private void RequestGuidance(Entity entity, ref GuidanceSeekingData seekingData, Entity sage)
        {
            var sageWisdom = SystemAPI.GetComponent<WisdomData>(sage);

            // Process guidance request
            seekingData.hasRequestedGuidance = true;
            seekingData.guidanceProviderId = sageWisdom.creatureId;

            // Provide immediate wisdom boost
            float guidanceWisdom = _config.guidanceWisdomAmount * sageWisdom.totalWisdom / _config.maxWisdomForGuidance;
            seekingData.receivedWisdom += guidanceWisdom;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Component data for creature wisdom
    /// </summary>
    [Serializable]
    public struct WisdomData : IComponentData
    {
        public int creatureId;
        public float ageInDays;
        public float totalWisdom;
        public SageLevel sageLevel;

        // Wisdom categories
        public float survivalWisdom;
        public float socialWisdom;
        public float mentoringWisdom;
        public float lifeWisdom;
        public float spiritualWisdom;
        public float practicalWisdom;
        public float emotionalWisdom;
        public float specializedWisdom;

        // Experience tracking
        public int recentSuccessfulInteractions;
        public int challengesOvercome;
        public int lastChallengeCount;
        public int timesSharedWisdom;
        public int lastTeachingCount;
        public float totalWisdomShared;

        // Mentorship data
        public int activeMentorships;
        public int completedMentorships;
        public float wisdomSharingCooldown;

        // Achievements and milestones
        public FixedList128Bytes<int> achievedMilestones;
        public FixedList128Bytes<FixedString32Bytes> availableAbilities;

        // Legacy preparation
        public bool hasCreatedLegacy;
        public float legacyPreparationTimer;
        public bool isPreparingSuccession;
    }

    /// <summary>
    /// Component data for learning creatures
    /// </summary>
    [Serializable]
    public struct LearningData : IComponentData
    {
        public int creatureId;
        public float accumulatedWisdom;
        public float learningCapacity;
        public float learningMultiplier;
        public float learningTimer;

        // Mentor relationship
        public bool hasActiveMentor;
        public int mentorId;

        // Learning categories
        public float survivalKnowledge;
        public float socialSkills;
        public float leadershipPotential;
        public float lifeExperience;
        public float spiritualAwareness;

        // Achievements
        public FixedList128Bytes<int> achievedMilestones;
    }

    /// <summary>
    /// Component for creatures seeking guidance
    /// </summary>
    [Serializable]
    public struct GuidanceSeekingData : IComponentData
    {
        public int creatureId;
        public GuidanceType guidanceType;
        public float seekingTimer;
        public bool hasRequestedGuidance;
        public int guidanceProviderId;
        public float receivedWisdom;
        public FixedString128Bytes seekingReason;
    }

    /// <summary>
    /// Individual wisdom entry
    /// </summary>
    [Serializable]
    public struct WisdomEntry
    {
        public WisdomType type;
        public float value;
        public FixedString128Bytes description;
        public float timestamp;
        public int sourceCreatureId;
    }

    /// <summary>
    /// Mentorship relationship data
    /// </summary>
    [Serializable]
    public struct MentorshipRelation
    {
        public int mentorId;
        public int studentId;
        public float startTime;
        public float duration;
        public MentorshipType mentorshipType;
        public float progress;
        public int lessonsShared;
        public float wisdomTransferred;
    }

    /// <summary>
    /// Wisdom gained event data
    /// </summary>
    [Serializable]
    public struct WisdomGained
    {
        public int creatureId;
        public WisdomMilestone milestoneReached;
        public float totalWisdom;
        public string[] newAbilities;
        public float timestamp;
    }

    /// <summary>
    /// Sage ascension event data
    /// </summary>
    [Serializable]
    public struct SageAscension
    {
        public int creatureId;
        public SageLevel previousLevel;
        public SageLevel newLevel;
        public float totalWisdom;
        public string[] specializations;
        public float timestamp;
    }

    /// <summary>
    /// Mentorship formation event data
    /// </summary>
    [Serializable]
    public struct MentorshipFormed
    {
        public int mentorId;
        public int studentId;
        public MentorshipType mentorshipType;
        public float expectedDuration;
        public float timestamp;
    }

    /// <summary>
    /// Wisdom sharing event data
    /// </summary>
    [Serializable]
    public struct WisdomShared
    {
        public int sageId;
        public int learnerId;
        public WisdomType wisdomType;
        public float wisdomAmount;
        public string lessonTitle;
        public float timestamp;
    }

    /// <summary>
    /// Creature legacy data
    /// </summary>
    [Serializable]
    public struct CreatureLegacy
    {
        public int sageId;
        public float totalWisdom;
        public int mentorshipsCompleted;
        public float wisdomShared;
        public string[] specializations;
        public string[] lifeLessons;
        public string[] wisdomQuotes;
        public string mentoringPhilosophy;
        public float timestamp;
    }

    #endregion
}