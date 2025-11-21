using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Profiling;
using UnityEngine;
using Laboratory.Subsystems.Team.Core;

namespace Laboratory.Subsystems.Team.Systems
{
    /// <summary>
    /// Team Communication System - Non-Verbal Coordination Tools
    /// PURPOSE: Enable effective team coordination without voice chat
    /// FEATURES: Smart pings, contextual markers, quick chat, tactical commands
    /// PLAYER-FRIENDLY: Accessible, clear, reduces communication barriers
    /// PERFORMANCE: Burst-compiled cleanup, profiled, zero GC allocations
    /// </summary>

    /// <summary>
    /// Burst-compiled job for cleaning up expired communications
    /// Runs in parallel across all teams for optimal performance
    /// </summary>
    [BurstCompile]
    public partial struct CleanupExpiredCommunicationsJob : IJobEntity
    {
        public float CurrentTime;
        public int MaxActivePings;

        public void Execute(DynamicBuffer<TeamCommunicationComponent> commBuffer)
        {
            // Remove expired communications
            for (int i = commBuffer.Length - 1; i >= 0; i--)
            {
                float age = CurrentTime - commBuffer[i].Timestamp;
                if (age > commBuffer[i].Duration)
                {
                    commBuffer.RemoveAt(i);
                }
            }

            // Limit buffer size to prevent memory bloat
            if (commBuffer.Length > MaxActivePings * 2)
            {
                int toRemove = commBuffer.Length - MaxActivePings;
                for (int i = 0; i < toRemove; i++)
                {
                    commBuffer.RemoveAt(0);
                }
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TeamCommunicationSystem : SystemBase
    {
        private EntityQuery _teamQuery;
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        // Communication settings
        private float _pingDisplayDuration = 5f;
        private int _maxActivePings = 10; // Per team

        private NativeHashMap<Entity, float> _lastPingTimes;

        // Performance profiling markers
        private static readonly ProfilerMarker s_ProcessCommunicationsMarker =
            new ProfilerMarker("Team.ProcessCommunications");
        private static readonly ProfilerMarker s_CleanupExpiredMarker =
            new ProfilerMarker("Team.CleanupExpired");
        private static readonly ProfilerMarker s_UpdateMetricsMarker =
            new ProfilerMarker("Team.UpdateMetrics");

        protected override void OnCreate()
        {
            _teamQuery = GetEntityQuery(ComponentType.ReadWrite<TeamComponent>());
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            _lastPingTimes = new NativeHashMap<Entity, float>(100, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (_lastPingTimes.IsCreated)
                _lastPingTimes.Dispose();
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Process team communications
            using (s_ProcessCommunicationsMarker.Auto())
            {
                ProcessTeamCommunications(currentTime, deltaTime);
            }

            // Cleanup expired communications using Burst-compiled job
            using (s_CleanupExpiredMarker.Auto())
            {
                CleanupExpiredCommunications(currentTime);
            }

            // Update communication scores
            using (s_UpdateMetricsMarker.Auto())
            {
                UpdateCommunicationMetrics();
            }
        }

        private void ProcessTeamCommunications(float currentTime, float deltaTime)
        {
            foreach (var (team, commBuffer, entity) in
                SystemAPI.Query<RefRW<TeamComponent>, DynamicBuffer<TeamCommunicationComponent>>()
                        .WithEntityAccess())
            {
                // Process incoming communications
                ProcessIncomingPings(commBuffer, team.ValueRO, currentTime);

                // Update team cohesion based on communication
                UpdateTeamCohesion(ref team.ValueRW, commBuffer, deltaTime);
            }
        }

        private void ProcessIncomingPings(
            DynamicBuffer<TeamCommunicationComponent> commBuffer,
            TeamComponent team,
            float currentTime)
        {
            // Sort pings by urgency and timestamp
            for (int i = 0; i < commBuffer.Length; i++)
            {
                var comm = commBuffer[i];

                // Process based on type
                switch (comm.Type)
                {
                    case CommunicationType.Ping_Danger:
                        ProcessDangerPing(comm, team);
                        break;

                    case CommunicationType.Ping_Help:
                        ProcessHelpPing(comm, team);
                        break;

                    case CommunicationType.Ping_Objective:
                        ProcessObjectivePing(comm, team);
                        break;

                    case CommunicationType.Command_Formation:
                        ProcessFormationCommand(comm, team);
                        break;

                    case CommunicationType.Chat_Thanks:
                    case CommunicationType.Chat_Good_Job:
                        ProcessPositiveFeedback(comm, team);
                        break;
                }
            }
        }

        private void ProcessDangerPing(TeamCommunicationComponent ping, TeamComponent team)
        {
            // High urgency - alert all team members
#if UNITY_EDITOR
            Debug.Log($"⚠️ DANGER ping from team {team.TeamName} at {ping.WorldPosition}");
#endif
            // Would trigger visual/audio alerts for team members
            // Could integrate with AI to make them more cautious in that area
        }

        private void ProcessHelpPing(TeamCommunicationComponent ping, TeamComponent team)
        {
            // Medium urgency - request assistance
#if UNITY_EDITOR
            Debug.Log($"🆘 HELP ping from team {team.TeamName} at {ping.WorldPosition}");
#endif
            // Would notify nearest teammates
            // Could integrate with AI to send support
        }

        private void ProcessObjectivePing(TeamCommunicationComponent ping, TeamComponent team)
        {
            // Tactical - mark objective
#if UNITY_EDITOR
            Debug.Log($"🎯 OBJECTIVE ping from team {team.TeamName} at {ping.WorldPosition}");
#endif
            // Would mark objective on team's shared map/UI
        }

        private void ProcessFormationCommand(TeamCommunicationComponent command, TeamComponent team)
        {
            // Tactical command - change formation
#if UNITY_EDITOR
            Debug.Log($"📐 FORMATION command for team {team.TeamName}");
#endif
            // Would trigger formation change for team members
        }

        private void ProcessPositiveFeedback(TeamCommunicationComponent chat, TeamComponent team)
        {
            // Positive communication - boost morale
            // Would increase team morale slightly
        }

        private void UpdateTeamCohesion(
            ref TeamComponent team,
            DynamicBuffer<TeamCommunicationComponent> commBuffer,
            float deltaTime)
        {
            // Good communication improves cohesion
            int recentComms = 0;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            for (int i = 0; i < commBuffer.Length; i++)
            {
                if (currentTime - commBuffer[i].Timestamp < 30f) // Last 30 seconds
                {
                    recentComms++;
                }
            }

            // Optimal communication rate: 1-3 pings per 30 seconds
            float idealRate = 2f;
            float communicationRate = recentComms / 30f;
            float communicationScore = 1f - math.abs(communicationRate - idealRate / 30f);

            // Adjust cohesion
            float cohesionChange = communicationScore * 0.1f * deltaTime;
            team.TeamCohesion = math.clamp(team.TeamCohesion + cohesionChange, 0f, 1f);

            // Too much communication can reduce cohesion (spam)
            if (recentComms > 10)
            {
                team.TeamCohesion = math.max(0.5f, team.TeamCohesion - deltaTime * 0.2f);
            }
        }

        private void CleanupExpiredCommunications(float currentTime)
        {
            // Use Burst-compiled job for parallel processing
            var cleanupJob = new CleanupExpiredCommunicationsJob
            {
                CurrentTime = currentTime,
                MaxActivePings = _maxActivePings
            };
            cleanupJob.ScheduleParallel();

            // Complete job before continuing (required for data dependency)
            Dependency.Complete();
        }

        private void UpdateCommunicationMetrics()
        {
            foreach (var (performance, commBuffer) in
                SystemAPI.Query<RefRW<TeamPerformanceComponent>,
                               DynamicBuffer<TeamCommunicationComponent>>())
            {
                // Calculate communication score based on usage
                float currentTime = (float)SystemAPI.Time.ElapsedTime;
                int recentTacticalComms = 0;
                int recentPositiveComms = 0;

                for (int i = 0; i < commBuffer.Length; i++)
                {
                    var comm = commBuffer[i];
                    float age = currentTime - comm.Timestamp;

                    if (age < 60f) // Last minute
                    {
                        switch (comm.Type)
                        {
                            case CommunicationType.Ping_Objective:
                            case CommunicationType.Ping_Enemy:
                            case CommunicationType.Command_Formation:
                                recentTacticalComms++;
                                break;

                            case CommunicationType.Chat_Thanks:
                            case CommunicationType.Chat_Good_Job:
                                recentPositiveComms++;
                                break;
                        }
                    }
                }

                // Score: balance of tactical and positive communication
                float tacticalScore = math.min(recentTacticalComms / 5f, 1f);
                float positiveScore = math.min(recentPositiveComms / 3f, 1f);
                performance.ValueRW.CommunicationScore = (tacticalScore * 0.7f + positiveScore * 0.3f);
            }
        }

        /// <summary>
        /// Public API: Send a ping from a player
        /// </summary>
        public static bool SendPing(
            EntityManager entityManager,
            Entity playerEntity,
            CommunicationType pingType,
            float3 worldPosition,
            Entity targetEntity = default,
            float urgency = 0.5f)
        {
            // Check if player is on a team
            if (!entityManager.HasComponent<TeamMemberComponent>(playerEntity))
            {
#if UNITY_EDITOR
                Debug.LogWarning("Player not on a team, cannot send ping");
#endif
                return false;
            }

            var member = entityManager.GetComponentData<TeamMemberComponent>(playerEntity);
            var teamEntity = member.TeamEntity;

            if (!entityManager.HasComponent<TeamComponent>(teamEntity))
            {
#if UNITY_EDITOR
                Debug.LogWarning("Team entity invalid");
#endif
                return false;
            }

            // Get communication buffer
            var commBuffer = entityManager.GetBuffer<TeamCommunicationComponent>(teamEntity);

            // Check ping limit
            if (commBuffer.Length >= 20) // Max pings per team
            {
#if UNITY_EDITOR
                Debug.LogWarning("Team ping buffer full");
#endif
                return false;
            }

            // Add ping to team
            commBuffer.Add(new TeamCommunicationComponent
            {
                Type = pingType,
                Sender = playerEntity,
                WorldPosition = worldPosition,
                TargetEntity = targetEntity,
                Message = GetPingMessage(pingType),
                Timestamp = (float)entityManager.World.Time.ElapsedTime,
                Duration = GetPingDuration(pingType),
                Urgency = urgency
            });

#if UNITY_EDITOR
            Debug.Log($"📍 Ping sent: {pingType} at {worldPosition}");
#endif
            return true;
        }

        /// <summary>
        /// Public API: Send quick chat message
        /// </summary>
        public static bool SendQuickChat(
            EntityManager entityManager,
            Entity playerEntity,
            CommunicationType chatType)
        {
            if (!entityManager.HasComponent<TeamMemberComponent>(playerEntity))
                return false;

            var member = entityManager.GetComponentData<TeamMemberComponent>(playerEntity);
            var teamEntity = member.TeamEntity;

            if (!entityManager.HasComponent<TeamComponent>(teamEntity))
                return false;

            var commBuffer = entityManager.GetBuffer<TeamCommunicationComponent>(teamEntity);

            commBuffer.Add(new TeamCommunicationComponent
            {
                Type = chatType,
                Sender = playerEntity,
                WorldPosition = float3.zero,
                TargetEntity = Entity.Null,
                Message = GetChatMessage(chatType),
                Timestamp = (float)entityManager.World.Time.ElapsedTime,
                Duration = 5f,
                Urgency = 0.3f
            });

#if UNITY_EDITOR
            Debug.Log($"💬 Quick chat: {chatType}");
#endif
            return true;
        }

        /// <summary>
        /// Public API: Send tactical command (leader only)
        /// </summary>
        public static bool SendTacticalCommand(
            EntityManager entityManager,
            Entity playerEntity,
            CommunicationType commandType,
            float3 targetPosition = default)
        {
            if (!entityManager.HasComponent<TeamMemberComponent>(playerEntity))
                return false;

            var member = entityManager.GetComponentData<TeamMemberComponent>(playerEntity);
            var teamEntity = member.TeamEntity;

            if (!entityManager.HasComponent<TeamComponent>(teamEntity))
                return false;

            var team = entityManager.GetComponentData<TeamComponent>(teamEntity);

            // Check if player is team leader
            if (team.TeamLeader != playerEntity)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Only team leader can send tactical commands");
#endif
                return false;
            }

            var commBuffer = entityManager.GetBuffer<TeamCommunicationComponent>(teamEntity);

            commBuffer.Add(new TeamCommunicationComponent
            {
                Type = commandType,
                Sender = playerEntity,
                WorldPosition = targetPosition,
                TargetEntity = Entity.Null,
                Message = GetCommandMessage(commandType),
                Timestamp = (float)entityManager.World.Time.ElapsedTime,
                Duration = 10f,
                Urgency = 0.8f
            });

#if UNITY_EDITOR
            Debug.Log($"⚔️ Tactical command: {commandType}");
#endif
            return true;
        }

        private static FixedString64Bytes GetPingMessage(CommunicationType type)
        {
            return type switch
            {
                CommunicationType.Ping_Danger => new FixedString64Bytes("⚠️ Danger!"),
                CommunicationType.Ping_Help => new FixedString64Bytes("🆘 Need help!"),
                CommunicationType.Ping_Objective => new FixedString64Bytes("🎯 Objective here!"),
                CommunicationType.Ping_Enemy => new FixedString64Bytes("👹 Enemy spotted!"),
                CommunicationType.Ping_Attack => new FixedString64Bytes("⚔️ Attack here!"),
                CommunicationType.Ping_Defend => new FixedString64Bytes("🛡️ Defend this!"),
                CommunicationType.Ping_Retreat => new FixedString64Bytes("🏃 Retreat!"),
                _ => new FixedString64Bytes("📍 Ping")
            };
        }

        private static FixedString64Bytes GetChatMessage(CommunicationType type)
        {
            return type switch
            {
                CommunicationType.Chat_Yes => new FixedString64Bytes("✓ Yes"),
                CommunicationType.Chat_No => new FixedString64Bytes("✗ No"),
                CommunicationType.Chat_Thanks => new FixedString64Bytes("🙏 Thanks!"),
                CommunicationType.Chat_Sorry => new FixedString64Bytes("😅 Sorry!"),
                CommunicationType.Chat_Good_Job => new FixedString64Bytes("👍 Good job!"),
                CommunicationType.Chat_Need_Help => new FixedString64Bytes("🆘 Need help!"),
                _ => new FixedString64Bytes("💬 ...")
            };
        }

        private static FixedString64Bytes GetCommandMessage(CommunicationType type)
        {
            return type switch
            {
                CommunicationType.Command_Follow => new FixedString64Bytes("➡️ Follow me!"),
                CommunicationType.Command_Hold => new FixedString64Bytes("✋ Hold position!"),
                CommunicationType.Command_Advance => new FixedString64Bytes("⬆️ Advance!"),
                CommunicationType.Command_Regroup => new FixedString64Bytes("🔄 Regroup!"),
                CommunicationType.Command_Formation => new FixedString64Bytes("📐 Formation!"),
                _ => new FixedString64Bytes("📣 Command")
            };
        }

        private static float GetPingDuration(CommunicationType type)
        {
            return type switch
            {
                CommunicationType.Ping_Danger => 8f,      // Danger lasts longer
                CommunicationType.Ping_Help => 10f,       // Help request persistent
                CommunicationType.Ping_Objective => 15f,  // Objectives stay marked
                CommunicationType.Ping_Location => 5f,
                CommunicationType.Ping_Enemy => 6f,
                _ => 5f
            };
        }
    }

    /// <summary>
    /// Smart Ping System - Context-aware ping assistance
    /// Automatically suggests appropriate pings based on game state
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TeamCommunicationSystem))]
    public partial class SmartPingAssistSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Analyze game state and suggest pings for new players
            foreach (var (member, tutorial, entity) in
                SystemAPI.Query<RefRO<TeamMemberComponent>,
                               RefRO<TutorialProgressComponent>>()
                        .WithEntityAccess())
            {
                if (tutorial.ValueRO.ShowHints)
                {
                    SuggestContextualPings(entity, member.ValueRO, currentTime);
                }
            }
        }

        private void SuggestContextualPings(
            Entity playerEntity,
            TeamMemberComponent member,
            float currentTime)
        {
            // Example: Suggest danger ping if player health is low
            // Example: Suggest objective ping if near unclaimed objective
            // Example: Suggest help ping if outnumbered
            // (Would integrate with actual game state)
        }
    }
}
