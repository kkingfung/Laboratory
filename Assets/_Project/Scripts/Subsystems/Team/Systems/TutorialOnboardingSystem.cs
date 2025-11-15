using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using Laboratory.Subsystems.Team.Core;

namespace Laboratory.Subsystems.Team.Systems
{
    /// <summary>
    /// Tutorial and Onboarding System - New Player Friendly Team Introduction
    /// PURPOSE: Guide new players through team mechanics with adaptive learning
    /// FEATURES: Progressive tutorials, contextual hints, adaptive difficulty, skill assessment
    /// PLAYER-FRIENDLY: Patient teaching, forgiving failures, celebrates successes
    /// RETENTION: Hooks players early, prevents overwhelming new users
    /// </summary>

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TutorialOnboardingSystem : SystemBase
    {
        private EntityQuery _tutorialQuery;
        private EntityQuery _teamMemberQuery;
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _tutorialQuery = GetEntityQuery(
                ComponentType.ReadWrite<TutorialProgressComponent>(),
                ComponentType.ReadWrite<AdaptiveTutorialComponent>()
            );

            _teamMemberQuery = GetEntityQuery(ComponentType.ReadOnly<TeamMemberComponent>());
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = _ecbSystem.CreateCommandBuffer();

            // Update tutorial progress
            foreach (var (tutorialProgress, adaptiveTutorial, hintBuffer, entity) in
                SystemAPI.Query<RefRW<TutorialProgressComponent>,
                               RefRW<AdaptiveTutorialComponent>,
                               DynamicBuffer<HintSystemComponent>>()
                        .WithEntityAccess())
            {
                UpdateTutorialStage(
                    ref tutorialProgress.ValueRW,
                    ref adaptiveTutorial.ValueRW,
                    hintBuffer,
                    entity,
                    currentTime,
                    deltaTime,
                    ecb);
            }

            // Process new players who need tutorials
            ProcessNewPlayers(ecb, currentTime);

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }

        private void UpdateTutorialStage(
            ref TutorialProgressComponent tutorial,
            ref AdaptiveTutorialComponent adaptive,
            DynamicBuffer<HintSystemComponent> hints,
            Entity playerEntity,
            float currentTime,
            float deltaTime,
            EntityCommandBuffer ecb)
        {
            switch (tutorial.CurrentStage)
            {
                case TutorialStage.Welcome:
                    ProcessWelcomeStage(ref tutorial, ref adaptive, hints, playerEntity, currentTime);
                    break;

                case TutorialStage.Basic_Controls:
                    ProcessBasicControlsStage(ref tutorial, ref adaptive, hints, playerEntity, deltaTime);
                    break;

                case TutorialStage.Team_Joining:
                    ProcessTeamJoiningStage(ref tutorial, ref adaptive, hints, playerEntity);
                    break;

                case TutorialStage.Role_Selection:
                    ProcessRoleSelectionStage(ref tutorial, ref adaptive, hints, playerEntity);
                    break;

                case TutorialStage.Basic_Teamwork:
                    ProcessBasicTeamworkStage(ref tutorial, ref adaptive, hints, playerEntity, deltaTime);
                    break;

                case TutorialStage.Communication:
                    ProcessCommunicationStage(ref tutorial, ref adaptive, hints, playerEntity);
                    break;

                case TutorialStage.Objectives:
                    ProcessObjectivesStage(ref tutorial, ref adaptive, hints, playerEntity, deltaTime);
                    break;

                case TutorialStage.Advanced_Tactics:
                    ProcessAdvancedTacticsStage(ref tutorial, ref adaptive, hints, playerEntity, deltaTime);
                    break;

                case TutorialStage.Graduation:
                    ProcessGraduationStage(ref tutorial, ref adaptive, playerEntity, ecb);
                    break;
            }

            // Update adaptive learning metrics
            UpdateAdaptiveLearning(ref adaptive, tutorial, deltaTime);
        }

        private void ProcessWelcomeStage(
            ref TutorialProgressComponent tutorial,
            ref AdaptiveTutorialComponent adaptive,
            DynamicBuffer<HintSystemComponent> hints,
            Entity playerEntity,
            float currentTime)
        {
            // Welcome message and initial assessment
            if (tutorial.StageProgress == 0f)
            {
                AddHint(hints, HintType.Control_Hint,
                    "Welcome to Project Chimera! Let's learn team basics together.",
                    float3.zero, Entity.Null, 1f, 10f, true);

                tutorial.TutorialStartTime = currentTime;
            }

            tutorial.StageProgress += 0.1f;

            if (tutorial.StageProgress >= 1f)
            {
                tutorial.CurrentStage = adaptive.SkipBasics ?
                    TutorialStage.Team_Joining : TutorialStage.Basic_Controls;
                tutorial.StageProgress = 0f;
            }
        }

        private void ProcessBasicControlsStage(
            ref TutorialProgressComponent tutorial,
            ref AdaptiveTutorialComponent adaptive,
            DynamicBuffer<HintSystemComponent> hints,
            Entity playerEntity,
            float deltaTime)
        {
            if (tutorial.StageProgress == 0f)
            {
                AddHint(hints, HintType.Control_Hint,
                    "Let's learn the basic controls. Try moving around.",
                    float3.zero, Entity.Null, 1f, 15f, true);
            }

            // Simulate control practice (would integrate with actual input system)
            tutorial.StageProgress += deltaTime * 0.2f;

            if (tutorial.StageProgress >= 1f)
            {
                tutorial.CompletedTutorials |= TutorialCompletionFlags.Combat_Basics;
                tutorial.CurrentStage = TutorialStage.Team_Joining;
                tutorial.StageProgress = 0f;

                AddHint(hints, HintType.Tip,
                    "Great job! Now let's learn about teams.",
                    float3.zero, Entity.Null, 0.8f, 5f, true);
            }
        }

        private void ProcessTeamJoiningStage(
            ref TutorialProgressComponent tutorial,
            ref AdaptiveTutorialComponent adaptive,
            DynamicBuffer<HintSystemComponent> hints,
            Entity playerEntity)
        {
            if (tutorial.StageProgress == 0f)
            {
                AddHint(hints, HintType.Teamwork_Hint,
                    "Teams work together to achieve goals. You'll have teammates to help you!",
                    float3.zero, Entity.Null, 1f, 10f, true);
            }

            // Check if player has joined a team
            if (SystemAPI.HasComponent<TeamMemberComponent>(playerEntity))
            {
                tutorial.CompletedTutorials |= TutorialCompletionFlags.Joined_Team;
                tutorial.CurrentStage = TutorialStage.Role_Selection;
                tutorial.StageProgress = 0f;

                AddHint(hints, HintType.Achievement,
                    "✓ You joined a team! Now choose your role.",
                    float3.zero, Entity.Null, 1f, 5f, true);
            }
            else
            {
                tutorial.StageProgress += 0.05f;

                // Offer to auto-join after 10 seconds
                if (tutorial.StageProgress > 0.5f)
                {
                    AddHint(hints, HintType.Control_Hint,
                        "Tip: We can find a team for you automatically!",
                        float3.zero, Entity.Null, 0.8f, 8f, false);
                }
            }
        }

        private void ProcessRoleSelectionStage(
            ref TutorialProgressComponent tutorial,
            ref AdaptiveTutorialComponent adaptive,
            DynamicBuffer<HintSystemComponent> hints,
            Entity playerEntity)
        {
            if (tutorial.StageProgress == 0f)
            {
                AddHint(hints, HintType.Role_Hint,
                    "Choose a role that fits your playstyle:\n" +
                    "• Tank - Protect teammates\n" +
                    "• DPS - Deal damage\n" +
                    "• Healer - Support team\n" +
                    "• Support - Utility and buffs",
                    float3.zero, Entity.Null, 1f, 15f, true);
            }

            if (SystemAPI.HasComponent<TeamMemberComponent>(playerEntity))
            {
                var member = SystemAPI.GetComponent<TeamMemberComponent>(playerEntity);

                if (member.PrimaryRole != TeamRole.Generalist)
                {
                    tutorial.CompletedTutorials |= TutorialCompletionFlags.Selected_Role;
                    tutorial.CurrentStage = TutorialStage.Basic_Teamwork;
                    tutorial.StageProgress = 0f;

                    // Role-specific tip
                    string roleTip = GetRoleTip(member.PrimaryRole);
                    AddHint(hints, HintType.Role_Hint, roleTip,
                        float3.zero, Entity.Null, 0.9f, 8f, true);
                }
            }

            tutorial.StageProgress += 0.02f;
        }

        private void ProcessBasicTeamworkStage(
            ref TutorialProgressComponent tutorial,
            ref AdaptiveTutorialComponent adaptive,
            DynamicBuffer<HintSystemComponent> hints,
            Entity playerEntity,
            float deltaTime)
        {
            if (tutorial.StageProgress == 0f)
            {
                AddHint(hints, HintType.Teamwork_Hint,
                    "Stay near teammates for bonuses! Teamwork makes you stronger.",
                    float3.zero, Entity.Null, 1f, 12f, true);
            }

            // Track teamwork progress
            tutorial.StageProgress += deltaTime * 0.15f;

            // Check for teamwork actions
            if (SystemAPI.HasComponent<TeamMemberComponent>(playerEntity))
            {
                var member = SystemAPI.GetComponent<TeamMemberComponent>(playerEntity);

                if (member.ContributionScore > 0f)
                {
                    tutorial.CompletedTutorials |= TutorialCompletionFlags.Helped_Teammate;
                    tutorial.StageProgress += 0.3f;

                    AddHint(hints, HintType.Achievement,
                        "✓ Great teamwork! Keep it up!",
                        float3.zero, Entity.Null, 1f, 5f, true);
                }
            }

            if (tutorial.StageProgress >= 1f)
            {
                tutorial.CurrentStage = TutorialStage.Communication;
                tutorial.StageProgress = 0f;
            }
        }

        private void ProcessCommunicationStage(
            ref TutorialProgressComponent tutorial,
            ref AdaptiveTutorialComponent adaptive,
            DynamicBuffer<HintSystemComponent> hints,
            Entity playerEntity)
        {
            if (tutorial.StageProgress == 0f)
            {
                AddHint(hints, HintType.Teamwork_Hint,
                    "Use pings to communicate:\n" +
                    "• Ping locations to guide teammates\n" +
                    "• Ping enemies to coordinate attacks\n" +
                    "• Use quick chat for fast responses",
                    float3.zero, Entity.Null, 1f, 15f, true);
            }

            // Check for communication usage
            bool usedPing = (tutorial.CompletedTutorials & TutorialCompletionFlags.Used_Ping) != 0;
            bool usedChat = (tutorial.CompletedTutorials & TutorialCompletionFlags.Used_Quick_Chat) != 0;

            if (usedPing)
            {
                tutorial.StageProgress += 0.5f;
                AddHint(hints, HintType.Achievement,
                    "✓ You used a ping! Communication is key.",
                    float3.zero, Entity.Null, 1f, 5f, true);
            }

            if (usedChat)
            {
                tutorial.StageProgress += 0.5f;
            }

            if (tutorial.StageProgress >= 1f)
            {
                tutorial.CurrentStage = TutorialStage.Objectives;
                tutorial.StageProgress = 0f;
            }
            else
            {
                tutorial.StageProgress += 0.01f; // Slow auto-progress
            }
        }

        private void ProcessObjectivesStage(
            ref TutorialProgressComponent tutorial,
            ref AdaptiveTutorialComponent adaptive,
            DynamicBuffer<HintSystemComponent> hints,
            Entity playerEntity,
            float deltaTime)
        {
            if (tutorial.StageProgress == 0f)
            {
                AddHint(hints, HintType.Objective_Hint,
                    "Work together to complete objectives! Check your team goals.",
                    float3.zero, Entity.Null, 1f, 12f, true);
            }

            // Check for objective completion
            if ((tutorial.CompletedTutorials & TutorialCompletionFlags.Completed_Objective) != 0)
            {
                tutorial.StageProgress = 1f;

                AddHint(hints, HintType.Achievement,
                    "✓ Objective complete! Excellent teamwork!",
                    float3.zero, Entity.Null, 1f, 6f, true);
            }
            else
            {
                tutorial.StageProgress += deltaTime * 0.05f;
            }

            if (tutorial.StageProgress >= 1f)
            {
                // Check if player is ready for advanced tactics
                if (adaptive.SuccessRate > 0.7f)
                {
                    tutorial.CurrentStage = TutorialStage.Advanced_Tactics;
                }
                else
                {
                    tutorial.CurrentStage = TutorialStage.Graduation;
                }
                tutorial.StageProgress = 0f;
            }
        }

        private void ProcessAdvancedTacticsStage(
            ref TutorialProgressComponent tutorial,
            ref AdaptiveTutorialComponent adaptive,
            DynamicBuffer<HintSystemComponent> hints,
            Entity playerEntity,
            float deltaTime)
        {
            if (tutorial.StageProgress == 0f)
            {
                AddHint(hints, HintType.Strategy_Hint,
                    "Advanced tactics:\n" +
                    "• Use formations for bonuses\n" +
                    "• Coordinate ultimate abilities\n" +
                    "• Adapt strategy to enemies",
                    float3.zero, Entity.Null, 1f, 18f, true);
            }

            // Check for formation usage
            if ((tutorial.CompletedTutorials & TutorialCompletionFlags.Used_Formation) != 0)
            {
                tutorial.StageProgress += 0.5f;
            }

            tutorial.StageProgress += deltaTime * 0.1f;

            if (tutorial.StageProgress >= 1f)
            {
                tutorial.CurrentStage = TutorialStage.Graduation;
                tutorial.StageProgress = 0f;
            }
        }

        private void ProcessGraduationStage(
            ref TutorialProgressComponent tutorial,
            ref AdaptiveTutorialComponent adaptive,
            Entity playerEntity,
            EntityCommandBuffer ecb)
        {
            if (tutorial.StageProgress == 0f)
            {
                Debug.Log($"🎓 Tutorial completed! Player graduated to full gameplay.");

                // Unlock full features
                adaptive.CurrentDifficulty = DifficultyLevel.Normal;
                tutorial.ShowHints = false; // Disable auto-hints
                tutorial.ShowAdvancedTips = true; // Enable advanced tips
            }

            tutorial.StageProgress += 0.2f;

            if (tutorial.StageProgress >= 1f)
            {
                tutorial.CurrentStage = TutorialStage.Completed;

                // Award completion rewards (would integrate with progression system)
                Debug.Log("✅ Tutorial rewards granted!");
            }
        }

        private void UpdateAdaptiveLearning(
            ref AdaptiveTutorialComponent adaptive,
            TutorialProgressComponent tutorial,
            float deltaTime)
        {
            // Track learning speed based on progress rate
            float expectedProgress = deltaTime * 0.1f;
            float actualProgress = tutorial.StageProgress;

            if (actualProgress > expectedProgress * 1.5f)
            {
                adaptive.LearningSpeed = math.min(1f, adaptive.LearningSpeed + 0.01f);
            }
            else if (actualProgress < expectedProgress * 0.5f)
            {
                adaptive.LearningSpeed = math.max(0f, adaptive.LearningSpeed - 0.01f);
                adaptive.NeedsExtraHelp = true;
            }

            // Adjust difficulty based on success
            if (tutorial.TotalMistakesMade > 5 && adaptive.ConsecutiveFailures > 2)
            {
                if (adaptive.CurrentDifficulty > DifficultyLevel.Tutorial)
                {
                    adaptive.CurrentDifficulty = (DifficultyLevel)((int)adaptive.CurrentDifficulty - 1);
                    Debug.Log($"📉 Difficulty reduced to {adaptive.CurrentDifficulty} for better learning");
                }
            }
        }

        private void ProcessNewPlayers(EntityCommandBuffer ecb, float currentTime)
        {
            // Find players without tutorial components and add them
            foreach (var (member, entity) in SystemAPI.Query<RefRO<TeamMemberComponent>>().WithEntityAccess())
            {
                if (!SystemAPI.HasComponent<TutorialProgressComponent>(entity) &&
                    member.ValueRO.SkillLevel <= PlayerSkillLevel.Beginner)
                {
                    // Add tutorial components to new players
                    ecb.AddComponent(entity, new TutorialProgressComponent
                    {
                        CurrentStage = TutorialStage.Welcome,
                        StageProgress = 0f,
                        ShowHints = true,
                        ShowAdvancedTips = false,
                        TutorialStartTime = currentTime
                    });

                    ecb.AddComponent(entity, new AdaptiveTutorialComponent
                    {
                        LearningSpeed = 0.5f,
                        RepetitionsNeeded = 2,
                        SkipBasics = false,
                        CurrentDifficulty = DifficultyLevel.Tutorial,
                        SuccessRate = 0.5f,
                        NeedsExtraHelp = false
                    });

                    ecb.AddBuffer<HintSystemComponent>(entity);

                    Debug.Log($"📚 New player detected, starting tutorial");
                }
            }
        }

        private string GetRoleTip(TeamRole role)
        {
            return role switch
            {
                TeamRole.Tank => "As Tank, protect your team by absorbing damage!",
                TeamRole.DPS => "As DPS, focus on dealing maximum damage to enemies!",
                TeamRole.Healer => "As Healer, keep your teammates healthy!",
                TeamRole.Support => "As Support, use utility skills to help your team!",
                TeamRole.Leader => "As Leader, coordinate your team's strategy!",
                _ => "Work with your team to achieve objectives!"
            };
        }

        private void AddHint(
            DynamicBuffer<HintSystemComponent> hints,
            HintType type,
            string text,
            float3 position,
            Entity target,
            float priority,
            float duration,
            bool onlyShowOnce)
        {
            hints.Add(new HintSystemComponent
            {
                Type = type,
                HintText = new FixedString128Bytes(text),
                WorldPosition = position,
                TargetEntity = target,
                Priority = priority,
                DisplayDuration = duration,
                Dismissible = true,
                OnlyShowOnce = onlyShowOnce,
                HintId = (uint)text.GetHashCode()
            });
        }
    }
}
