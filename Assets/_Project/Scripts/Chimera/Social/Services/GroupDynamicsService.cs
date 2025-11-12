using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Service for managing group dynamics, hierarchy, and leadership.
    /// Handles group formation, cohesion calculation, leadership emergence, and group dissolution.
    /// Extracted from AdvancedSocialSystem for single responsibility.
    /// </summary>
    public class GroupDynamicsService
    {
        private readonly int _maxGroupSize;
        private readonly float _groupCohesionThreshold;
        private readonly float _leadershipEmergenceRate;
        private readonly bool _enableHierarchyFormation;
        private readonly Action<uint, SocialGroup> _onGroupFormed;
        private readonly Action<uint, uint> _onLeadershipEmergence;
        private readonly SocialLearningSystem _socialLearning;

        public GroupDynamicsService(
            int maxGroupSize,
            float groupCohesionThreshold,
            float leadershipEmergenceRate,
            bool enableHierarchyFormation,
            Action<uint, SocialGroup> onGroupFormed,
            Action<uint, uint> onLeadershipEmergence,
            SocialLearningSystem socialLearning)
        {
            _maxGroupSize = maxGroupSize;
            _groupCohesionThreshold = groupCohesionThreshold;
            _leadershipEmergenceRate = leadershipEmergenceRate;
            _enableHierarchyFormation = enableHierarchyFormation;
            _onGroupFormed = onGroupFormed;
            _onLeadershipEmergence = onLeadershipEmergence;
            _socialLearning = socialLearning;
        }

        /// <summary>
        /// Forms social groups based on relationship strength and compatibility
        /// </summary>
        public SocialGroup FormSocialGroup(
            List<uint> memberIds,
            string groupPurpose,
            Dictionary<uint, SocialAgent> socialAgents)
        {
            if (memberIds.Count < 2 || memberIds.Count > _maxGroupSize)
            {
                UnityEngine.Debug.LogWarning($"Invalid group size: {memberIds.Count}");
                return null;
            }

            // Validate all members exist
            foreach (var memberId in memberIds)
            {
                if (!socialAgents.ContainsKey(memberId))
                {
                    UnityEngine.Debug.LogError($"Cannot form group: Agent {memberId} not found");
                    return null;
                }
            }

            var groupId = GenerateGroupId();
            var group = new SocialGroup
            {
                groupId = groupId,
                memberIds = new List<uint>(memberIds),
                purpose = groupPurpose,
                formationTime = Time.time,
                cohesion = CalculateGroupCohesion(memberIds, socialAgents),
                hierarchy = new Dictionary<uint, SocialRank>(),
                groupNorms = new List<CulturalTrait>(),
                leadership = null,
                communicationNetwork = new Dictionary<uint, List<uint>>(),
                collectiveMemory = new List<GroupMemory>()
            };

            // Initialize hierarchy
            InitializeGroupHierarchy(group, socialAgents);

            // Initialize group norms from member traits
            InitializeGroupNorms(group, socialAgents);

            // Set up communication network within group
            EstablishCommunicationNetwork(group);

            // Register group membership for all members
            foreach (var memberId in memberIds)
            {
                socialAgents[memberId].groupMemberships.Add(groupId);
            }

            _onGroupFormed?.Invoke(groupId, group);

            UnityEngine.Debug.Log($"Social group {groupId} formed with {memberIds.Count} members, cohesion: {group.cohesion:F3}");
            return group;
        }

        /// <summary>
        /// Calculates group cohesion based on relationship strengths
        /// </summary>
        public float CalculateGroupCohesion(List<uint> memberIds, Dictionary<uint, SocialAgent> socialAgents)
        {
            if (memberIds.Count < 2) return 0f;

            float totalCohesion = 0f;
            int relationshipCount = 0;

            for (int i = 0; i < memberIds.Count; i++)
            {
                for (int j = i + 1; j < memberIds.Count; j++)
                {
                    var agentA = socialAgents[memberIds[i]];
                    if (agentA.relationships.TryGetValue(memberIds[j], out var relationship))
                    {
                        totalCohesion += math.max(0f, relationship.strength);
                    }
                    relationshipCount++;
                }
            }

            return relationshipCount > 0 ? totalCohesion / relationshipCount : 0.3f;
        }

        /// <summary>
        /// Initializes group hierarchy based on member traits
        /// </summary>
        private void InitializeGroupHierarchy(SocialGroup group, Dictionary<uint, SocialAgent> socialAgents)
        {
            if (!_enableHierarchyFormation) return;

            foreach (var memberId in group.memberIds)
            {
                var agent = socialAgents[memberId];

                // Calculate social ranking based on traits and relationships
                float ranking = CalculateSocialRanking(agent, group, socialAgents);

                group.hierarchy[memberId] = new SocialRank
                {
                    agentId = memberId,
                    rank = ranking,
                    influence = ranking * 0.8f,
                    leadership = ranking > 0.8f
                };
            }

            // Establish leadership if someone has high ranking
            var potentialLeader = group.hierarchy.Values.OrderByDescending(r => r.rank).First();
            if (potentialLeader.rank > 0.7f)
            {
                group.leadership = new Leadership
                {
                    leaderId = potentialLeader.agentId,
                    leadershipStyle = DetermineLeadershipStyle(socialAgents[potentialLeader.agentId]),
                    authority = potentialLeader.rank,
                    emergenceTime = Time.time
                };

                _onLeadershipEmergence?.Invoke(group.groupId, potentialLeader.agentId);
            }
        }

        /// <summary>
        /// Calculates social ranking for an agent within a group
        /// </summary>
        private float CalculateSocialRanking(SocialAgent agent, SocialGroup group, Dictionary<uint, SocialAgent> socialAgents)
        {
            float ranking = 0f;

            // Personality factors
            ranking += agent.personality.GetValueOrDefault("Extraversion", 0.5f) * 0.3f;
            ranking += agent.personality.GetValueOrDefault("Conscientiousness", 0.5f) * 0.2f;
            ranking += agent.socialTraits.GetValueOrDefault("Leadership", 0.5f) * 0.4f;

            // Social network position
            var relationships = agent.relationships.Values.Where(r => group.memberIds.Contains(r.targetAgent));
            float avgRelationshipStrength = relationships.Any() ? relationships.Average(r => r.strength) : 0f;
            ranking += avgRelationshipStrength * 0.1f;

            return math.clamp(ranking, 0f, 1f);
        }

        /// <summary>
        /// Determines leadership style based on personality traits
        /// </summary>
        private LeadershipStyle DetermineLeadershipStyle(SocialAgent agent)
        {
            float agreeableness = agent.personality.GetValueOrDefault("Agreeableness", 0.5f);
            float conscientiousness = agent.personality.GetValueOrDefault("Conscientiousness", 0.5f);

            if (agreeableness > 0.7f && conscientiousness > 0.7f)
                return LeadershipStyle.Democratic;
            else if (agreeableness < 0.3f && conscientiousness > 0.7f)
                return LeadershipStyle.Autocratic;
            else if (agreeableness > 0.6f)
                return LeadershipStyle.Collaborative;
            else
                return LeadershipStyle.Laissez_Faire;
        }

        /// <summary>
        /// Initializes group norms as average of member cultural traits
        /// </summary>
        private void InitializeGroupNorms(SocialGroup group, Dictionary<uint, SocialAgent> socialAgents)
        {
            var memberTraits = new Dictionary<string, List<float>>();

            // Collect all cultural traits from members
            foreach (var memberId in group.memberIds)
            {
                var agent = socialAgents[memberId];
                foreach (var trait in agent.culturalTraits)
                {
                    if (!memberTraits.ContainsKey(trait.name))
                        memberTraits[trait.name] = new List<float>();

                    memberTraits[trait.name].Add(trait.value);
                }
            }

            // Create group norms as average of member traits
            foreach (var traitSet in memberTraits)
            {
                var groupNorm = new CulturalTrait
                {
                    name = traitSet.Key,
                    value = traitSet.Value.Average(),
                    stability = 0.8f, // Group norms are more stable
                    transmissionRate = 0.5f // Higher transmission within group
                };

                group.groupNorms.Add(groupNorm);
            }
        }

        /// <summary>
        /// Establishes communication network within group
        /// </summary>
        private void EstablishCommunicationNetwork(SocialGroup group)
        {
            foreach (var memberId in group.memberIds)
            {
                group.communicationNetwork[memberId] = group.memberIds.Where(id => id != memberId).ToList();
            }
        }

        /// <summary>
        /// Updates all group dynamics including cohesion, leadership, and learning
        /// </summary>
        public List<uint> UpdateGroupDynamics(
            Dictionary<uint, SocialGroup> activeGroups,
            Dictionary<uint, SocialAgent> socialAgents,
            Dictionary<uint, Leadership> groupLeaderships,
            float deltaTime)
        {
            var groupsToRemove = new List<uint>();

            foreach (var group in activeGroups.Values)
            {
                // Update group cohesion
                group.cohesion = CalculateGroupCohesion(group.memberIds, socialAgents);

                // Check for group dissolution
                if (group.cohesion < _groupCohesionThreshold * 0.3f)
                {
                    groupsToRemove.Add(group.groupId);
                    continue;
                }

                // Update leadership
                if (group.leadership != null)
                {
                    UpdateLeadership(group, socialAgents, deltaTime);
                }

                // Process group learning and cultural evolution
                ProcessGroupLearning(group, socialAgents, deltaTime);
            }

            return groupsToRemove;
        }

        /// <summary>
        /// Updates leadership dynamics within a group
        /// </summary>
        private void UpdateLeadership(SocialGroup group, Dictionary<uint, SocialAgent> socialAgents, float deltaTime)
        {
            var leader = socialAgents[group.leadership.leaderId];

            // Leadership can change based on performance and group dynamics
            if (UnityEngine.Random.value < _leadershipEmergenceRate * deltaTime)
            {
                var challengerRank = group.hierarchy.Values
                    .Where(r => r.agentId != group.leadership.leaderId)
                    .OrderByDescending(r => r.rank)
                    .FirstOrDefault();

                if (challengerRank != null && challengerRank.rank > group.leadership.authority + 0.2f)
                {
                    // Leadership change
                    group.leadership.leaderId = challengerRank.agentId;
                    group.leadership.authority = challengerRank.rank;
                    group.leadership.emergenceTime = Time.time;

                    _onLeadershipEmergence?.Invoke(group.groupId, challengerRank.agentId);
                }
            }
        }

        /// <summary>
        /// Processes group learning and collective intelligence
        /// </summary>
        private void ProcessGroupLearning(SocialGroup group, Dictionary<uint, SocialAgent> socialAgents, float deltaTime)
        {
            // Groups can develop collective intelligence and shared culture
            if (group.memberIds.Count > 3 && group.cohesion > 0.6f)
            {
                _socialLearning.ProcessGroupLearning(group, socialAgents, deltaTime);
            }
        }

        /// <summary>
        /// Dissolves a group and removes membership from all members
        /// </summary>
        public void DissolveGroup(
            uint groupId,
            Dictionary<uint, SocialGroup> activeGroups,
            Dictionary<uint, SocialAgent> socialAgents,
            Dictionary<uint, Leadership> groupLeaderships)
        {
            if (activeGroups.TryGetValue(groupId, out var group))
            {
                // Remove group membership from all members
                foreach (var memberId in group.memberIds)
                {
                    if (socialAgents.TryGetValue(memberId, out var agent))
                    {
                        agent.groupMemberships.Remove(groupId);
                    }
                }

                // Remove leadership if exists
                groupLeaderships.Remove(groupId);

                UnityEngine.Debug.Log($"Group {groupId} dissolved due to low cohesion");
            }

            activeGroups.Remove(groupId);
        }

        /// <summary>
        /// Generates unique group ID
        /// </summary>
        private uint GenerateGroupId() => (uint)UnityEngine.Random.Range(1, int.MaxValue);
    }
}
