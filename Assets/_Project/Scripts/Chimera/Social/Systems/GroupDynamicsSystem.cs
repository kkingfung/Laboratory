using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Profiling;
using Laboratory.Chimera.Social.Types;
using Laboratory.Chimera.Diagnostics;
using Laboratory.Chimera.Social.Core;


namespace Laboratory.Chimera.Social.Systems
{
    /// <summary>
    /// Group formation, leadership, and dynamics management system
    /// </summary>
    public class GroupDynamicsSystem : MonoBehaviour
    {
        [Header("Group Configuration")]
        [SerializeField] private int maxGroupSize = 25;
        [SerializeField] private float groupCohesionThreshold = 0.7f;
        [SerializeField] private float leadershipEmergenceRate = 0.05f;
        [SerializeField] private bool enableHierarchyFormation = true;

        private Dictionary<uint, Laboratory.Chimera.Social.Data.SocialGroup> activeGroups = new();
        private Dictionary<uint, Laboratory.Chimera.Social.Data.Leadership> groupLeaderships = new();
        private SocialNetworkSystem networkSystem;

        public event Action<uint, Laboratory.Chimera.Social.Data.SocialGroup> OnGroupFormed;
        public event Action<uint> OnGroupDisbanded;
        public event Action<uint, uint> OnLeadershipEmergence;
        public event Action<uint, uint> OnLeadershipChange;

        private static readonly ProfilerMarker s_UpdateGroupDynamicsMarker = new ProfilerMarker("GroupDynamicsSystem.UpdateGroupDynamics");
        private static readonly ProfilerMarker s_FormGroupMarker = new ProfilerMarker("GroupDynamicsSystem.FormGroup");
        private static readonly ProfilerMarker s_UpdateGroupCohesionMarker = new ProfilerMarker("GroupDynamicsSystem.UpdateGroupCohesion");
        private static readonly ProfilerMarker s_DetermineLeadershipMarker = new ProfilerMarker("GroupDynamicsSystem.DetermineLeadership");
        private static readonly ProfilerMarker s_UpdateLeadershipMarker = new ProfilerMarker("GroupDynamicsSystem.UpdateLeadership");

        private void Awake()
        {
            SocialServiceLocator.RegisterGroupDynamics(this);
        }

        private void Start()
        {
            networkSystem = SocialServiceLocator.SocialNetwork;
        }

        public void FormGroup(List<uint> memberIds, string groupName = "")
        {
            using (s_FormGroupMarker.Auto())
            {
                if (memberIds.Count < 2 || memberIds.Count > maxGroupSize)
                    return;

                var groupId = GenerateGroupId();
                var group = new Laboratory.Chimera.Social.Data.SocialGroup
                {
                    GroupId = groupId,
                    GroupName = string.IsNullOrEmpty(groupName) ? $"Group_{groupId}" : groupName,
                    Members = new List<uint>(memberIds),
                    LeaderId = 0, // Will be determined by leadership emergence
                    Cohesion = CalculateInitialCohesion(memberIds),
                    CenterPosition = CalculateGroupCenter(memberIds),
                    GroupStatus = Laboratory.Chimera.Social.Types.SocialStatus.Regular,
                    FormationDate = DateTime.UtcNow
                };

                activeGroups[groupId] = group;

                if (enableHierarchyFormation)
                {
                    DetermineLeadership(group);
                }

                OnGroupFormed?.Invoke(groupId, group);
                UnityEngine.Debug.Log($"Group formed: {group.GroupName} with {memberIds.Count} members");
            }
        }

        public void UpdateGroupDynamics()
        {
            using (s_UpdateGroupDynamicsMarker.Auto())
            {
                foreach (var group in activeGroups.Values.ToList())
                {
                    UpdateGroupCohesion(group);
                    UpdateLeadership(group);
                    CheckGroupStability(group);
                }
            }
        }

        public void AddMemberToGroup(uint groupId, uint memberId)
        {
            if (activeGroups.TryGetValue(groupId, out var group))
            {
                if (group.Members.Count < maxGroupSize && !group.Members.Contains(memberId))
                {
                    group.Members.Add(memberId);
                    UpdateGroupCohesion(group);
                    UnityEngine.Debug.Log($"Added member {memberId} to group {group.GroupName}");
                }
            }
        }

        public void RemoveMemberFromGroup(uint groupId, uint memberId)
        {
            if (activeGroups.TryGetValue(groupId, out var group))
            {
                group.Members.Remove(memberId);

                if (group.LeaderId == memberId)
                {
                    DetermineLeadership(group);
                }

                if (group.Members.Count < 2)
                {
                    DisbandGroup(groupId);
                }
                else
                {
                    UpdateGroupCohesion(group);
                }
            }
        }

        private void DetermineLeadership(Laboratory.Chimera.Social.Data.SocialGroup group)
        {
            using (s_DetermineLeadershipMarker.Auto())
            {
                if (group.Members.Count == 0) return;

                // Find the member with highest charisma and social connections
                uint bestLeaderCandidate = 0;
                float bestLeadershipScore = -1f;

                foreach (var memberId in group.Members)
                {
                    float leadershipScore = CalculateLeadershipScore(memberId, group);
                    if (leadershipScore > bestLeadershipScore)
                    {
                        bestLeadershipScore = leadershipScore;
                        bestLeaderCandidate = memberId;
                    }
                }

                if (bestLeaderCandidate != 0 && bestLeaderCandidate != group.LeaderId)
                {
                    var previousLeader = group.LeaderId;
                    group.LeaderId = bestLeaderCandidate;

                    var leadership = new Data.Leadership
                    {
                        LeaderId = bestLeaderCandidate,
                        GroupId = group.GroupId,
                        Style = (Types.LeadershipStyle)(int)DetermineLeadershipStyle(bestLeaderCandidate),
                        Effectiveness = 0.5f, // Starting effectiveness
                        PopularityRating = 0.5f,
                        LeadershipStart = DateTime.UtcNow
                    };

                    groupLeaderships[group.GroupId] = leadership;

                    if (previousLeader != 0)
                        OnLeadershipChange?.Invoke(previousLeader, bestLeaderCandidate);
                    else
                        OnLeadershipEmergence?.Invoke(bestLeaderCandidate, group.GroupId);
                }
            }
        }

        private float CalculateLeadershipScore(uint agentId, Laboratory.Chimera.Social.Data.SocialGroup group)
        {
            // This would use actual agent data in a full implementation
            float charismaScore = UnityEngine.Random.Range(0.3f, 1f);
            float socialConnectionsScore = UnityEngine.Random.Range(0.2f, 0.9f);
            float experienceScore = UnityEngine.Random.Range(0.1f, 0.8f);

            return (charismaScore * 0.4f + socialConnectionsScore * 0.4f + experienceScore * 0.2f);
        }

        private LeadershipStyle DetermineLeadershipStyle(uint leaderId)
        {
            // This would analyze the leader's personality and behavior patterns
            var styles = Enum.GetValues(typeof(LeadershipStyle));
            return (LeadershipStyle)styles.GetValue(UnityEngine.Random.Range(0, styles.Length));
        }

        private float CalculateInitialCohesion(List<uint> memberIds)
        {
            if (networkSystem == null) return 0.5f;

            float totalRelationshipStrength = 0f;
            int relationshipCount = 0;

            for (int i = 0; i < memberIds.Count; i++)
            {
                for (int j = i + 1; j < memberIds.Count; j++)
                {
                    var relationship = networkSystem.GetRelationship(memberIds[i], memberIds[j]);
                    if (relationship != null)
                    {
                        totalRelationshipStrength += relationship.Strength;
                        relationshipCount++;
                    }
                }
            }

            return relationshipCount > 0 ? (totalRelationshipStrength / relationshipCount + 1f) / 2f : 0.3f;
        }

        private Vector3 CalculateGroupCenter(List<uint> memberIds)
        {
            // This would use actual agent positions in a full implementation
            return Vector3.zero;
        }

        private void UpdateGroupCohesion(Laboratory.Chimera.Social.Data.SocialGroup group)
        {
            using (s_UpdateGroupCohesionMarker.Auto())
            {
                group.Cohesion = CalculateInitialCohesion(group.Members);

                // Apply leadership effectiveness bonus
                if (groupLeaderships.TryGetValue(group.GroupId, out var leadership))
                {
                    group.Cohesion += leadership.Effectiveness * 0.2f;
                }

                group.Cohesion = Mathf.Clamp01(group.Cohesion);
            }
        }

        private void UpdateLeadership(Laboratory.Chimera.Social.Data.SocialGroup group)
        {
            using (s_UpdateLeadershipMarker.Auto())
            {
                if (groupLeaderships.TryGetValue(group.GroupId, out var leadership))
                {
                    // Update leadership effectiveness based on group performance
                    float performanceBonus = group.Cohesion > groupCohesionThreshold ? 0.01f : -0.01f;
                    leadership.Effectiveness += performanceBonus;
                    leadership.Effectiveness = Mathf.Clamp01(leadership.Effectiveness);

                    // Check for leadership challenges
                    if (leadership.Effectiveness < 0.3f && UnityEngine.Random.value < leadershipEmergenceRate)
                    {
                        DetermineLeadership(group);
                    }
                }
            }
        }

        private void CheckGroupStability(Laboratory.Chimera.Social.Data.SocialGroup group)
        {
            if (group.Cohesion < 0.2f)
            {
                // Group is becoming unstable, consider disbanding
                if (UnityEngine.Random.value < 0.1f) // 10% chance per update
                {
                    DisbandGroup(group.GroupId);
                }
            }
        }

        private void DisbandGroup(uint groupId)
        {
            if (activeGroups.TryGetValue(groupId, out var group))
            {
                activeGroups.Remove(groupId);
                groupLeaderships.Remove(groupId);
                OnGroupDisbanded?.Invoke(groupId);
                UnityEngine.Debug.Log($"Group disbanded: {group.GroupName}");
            }
        }

        private uint GenerateGroupId()
        {
            return (uint)UnityEngine.Random.Range(100000, 999999);
        }

        public Laboratory.Chimera.Social.Data.SocialGroup GetGroup(uint groupId)
        {
            return activeGroups.TryGetValue(groupId, out var group) ? group : null;
        }

        public List<Laboratory.Chimera.Social.Data.SocialGroup> GetAllGroups()
        {
            return activeGroups.Values.ToList();
        }

        public Laboratory.Chimera.Social.Data.Leadership GetGroupLeadership(uint groupId)
        {
            return groupLeaderships.TryGetValue(groupId, out var leadership) ? leadership : null;
        }
    }
}