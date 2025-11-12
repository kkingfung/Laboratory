using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Service for social system analytics, metrics calculation, and comprehensive reporting.
    /// Handles all analysis and reporting functionality for the social system.
    /// Extracted from AdvancedSocialSystem for single responsibility.
    /// </summary>
    public class SocialAnalyticsService
    {
        private readonly float _groupCohesionThreshold;
        private readonly float _leadershipEmergenceRate;
        private SocialMetrics _globalSocialMetrics;

        public SocialAnalyticsService(float groupCohesionThreshold, float leadershipEmergenceRate)
        {
            _groupCohesionThreshold = groupCohesionThreshold;
            _leadershipEmergenceRate = leadershipEmergenceRate;
            _globalSocialMetrics = new SocialMetrics();
        }

        /// <summary>
        /// Updates global social metrics
        /// </summary>
        public void UpdateSocialMetrics(
            Dictionary<uint, SocialAgent> socialAgents,
            Dictionary<uint, SocialGroup> activeGroups,
            SocialNetworkGraph socialNetwork,
            float culturalDiversity,
            float averageEmpathy)
        {
            _globalSocialMetrics.totalAgents = socialAgents.Count;
            _globalSocialMetrics.totalGroups = activeGroups.Count;
            _globalSocialMetrics.averageGroupSize = activeGroups.Any()
                ? (float)activeGroups.Values.Average(g => g.memberIds.Count)
                : 0f;
            _globalSocialMetrics.averageEmpathy = averageEmpathy;
            _globalSocialMetrics.culturalDiversity = culturalDiversity;
            _globalSocialMetrics.socialNetworkDensity = socialNetwork.CalculateNetworkDensity();
        }

        /// <summary>
        /// Gets current global social metrics
        /// </summary>
        public SocialMetrics GetGlobalMetrics() => _globalSocialMetrics;

        /// <summary>
        /// Generates comprehensive social analysis report
        /// </summary>
        public SocialAnalysisReport GenerateSocialReport(
            Dictionary<uint, SocialAgent> socialAgents,
            Dictionary<uint, SocialGroup> activeGroups,
            Dictionary<uint, Leadership> groupLeaderships,
            SocialNetworkGraph socialNetwork,
            List<SocialEvent> socialEvents,
            List<Innovation> culturalInnovations,
            EmpathyNetworkAnalysis empathyNetworkAnalysis,
            CommunicationAnalysis communicationAnalysis,
            CulturalAnalysis culturalAnalysis)
        {
            return new SocialAnalysisReport
            {
                globalMetrics = _globalSocialMetrics,
                networkAnalysis = socialNetwork.AnalyzeNetwork(),
                groupDynamics = AnalyzeGroupDynamics(activeGroups, groupLeaderships),
                culturalAnalysis = culturalAnalysis,
                communicationPatterns = communicationAnalysis,
                leadershipAnalysis = AnalyzeLeadership(activeGroups, groupLeaderships),
                conflictAnalysis = AnalyzeConflicts(socialEvents),
                empathyNetworkAnalysis = empathyNetworkAnalysis,
                socialTrends = IdentifySocialTrends(socialAgents, activeGroups, culturalInnovations),
                recommendations = GenerateSocialRecommendations(socialAgents, activeGroups, socialEvents)
            };
        }

        /// <summary>
        /// Analyzes group dynamics and stability
        /// </summary>
        private GroupDynamicsAnalysis AnalyzeGroupDynamics(
            Dictionary<uint, SocialGroup> activeGroups,
            Dictionary<uint, Leadership> groupLeaderships)
        {
            return new GroupDynamicsAnalysis
            {
                totalGroups = activeGroups.Count,
                averageCohesion = activeGroups.Values.Any()
                    ? activeGroups.Values.Average(g => g.cohesion)
                    : 0f,
                leadershipDistribution = CalculateLeadershipDistribution(groupLeaderships),
                groupStability = CalculateGroupStability(activeGroups),
                hierarchyComplexity = CalculateHierarchyComplexity(activeGroups)
            };
        }

        /// <summary>
        /// Calculates leadership style distribution
        /// </summary>
        private Dictionary<LeadershipStyle, int> CalculateLeadershipDistribution(Dictionary<uint, Leadership> groupLeaderships)
        {
            return groupLeaderships.Values
                .GroupBy(l => l.leadershipStyle)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Calculates group stability metric
        /// </summary>
        private float CalculateGroupStability(Dictionary<uint, SocialGroup> activeGroups)
        {
            if (activeGroups.Count == 0) return 1f;

            return activeGroups.Values.Count(g => g.cohesion > _groupCohesionThreshold) / (float)activeGroups.Count;
        }

        /// <summary>
        /// Calculates hierarchy complexity across groups
        /// </summary>
        private float CalculateHierarchyComplexity(Dictionary<uint, SocialGroup> activeGroups)
        {
            if (activeGroups.Count == 0) return 0f;

            return activeGroups.Values.Average(g =>
                g.hierarchy.Values.Any()
                    ? g.hierarchy.Values.Max(h => h.rank) - g.hierarchy.Values.Min(h => h.rank)
                    : 0f);
        }

        /// <summary>
        /// Analyzes leadership patterns and effectiveness
        /// </summary>
        private LeadershipAnalysis AnalyzeLeadership(
            Dictionary<uint, SocialGroup> activeGroups,
            Dictionary<uint, Leadership> groupLeaderships)
        {
            return new LeadershipAnalysis
            {
                leadershipEmergenceRate = _leadershipEmergenceRate,
                averageLeadershipTenure = CalculateAverageLeadershipTenure(groupLeaderships),
                leadershipStyleDistribution = CalculateLeadershipDistribution(groupLeaderships),
                leadershipEffectiveness = CalculateLeadershipEffectiveness(activeGroups, groupLeaderships)
            };
        }

        /// <summary>
        /// Calculates average leadership tenure
        /// </summary>
        private float CalculateAverageLeadershipTenure(Dictionary<uint, Leadership> groupLeaderships)
        {
            if (groupLeaderships.Count == 0) return 0f;

            return groupLeaderships.Values.Average(l => Time.time - l.emergenceTime);
        }

        /// <summary>
        /// Calculates leadership effectiveness based on group cohesion
        /// </summary>
        private float CalculateLeadershipEffectiveness(
            Dictionary<uint, SocialGroup> activeGroups,
            Dictionary<uint, Leadership> groupLeaderships)
        {
            if (groupLeaderships.Count == 0) return 0f;

            float totalEffectiveness = 0f;
            int effectiveLeadershipCount = 0;

            foreach (var leadership in groupLeaderships.Values)
            {
                // Find the group this leadership belongs to by checking which group contains this leader
                var group = activeGroups.Values.FirstOrDefault(g => g.leadership?.leaderId == leadership.leaderId);
                if (group != null)
                {
                    totalEffectiveness += group.cohesion;
                    effectiveLeadershipCount++;
                }
            }

            return effectiveLeadershipCount > 0 ? totalEffectiveness / effectiveLeadershipCount : 0f;
        }

        /// <summary>
        /// Analyzes conflict patterns and resolution
        /// </summary>
        private ConflictAnalysis AnalyzeConflicts(List<SocialEvent> socialEvents)
        {
            var conflicts = socialEvents.OfType<SocialConflict>().ToList();

            return new ConflictAnalysis
            {
                totalConflicts = conflicts.Count,
                conflictResolutionRate = CalculateConflictResolutionRate(conflicts),
                commonConflictCauses = IdentifyCommonConflictCauses(conflicts),
                conflictIntensityDistribution = CalculateConflictIntensityDistribution(conflicts)
            };
        }

        /// <summary>
        /// Calculates conflict resolution rate
        /// </summary>
        private float CalculateConflictResolutionRate(List<SocialConflict> conflicts)
        {
            if (conflicts.Count == 0) return 1f;

            return conflicts.Count(c => c.resolved) / (float)conflicts.Count;
        }

        /// <summary>
        /// Identifies common conflict causes
        /// </summary>
        private List<string> IdentifyCommonConflictCauses(List<SocialConflict> conflicts)
        {
            return conflicts
                .GroupBy(c => c.cause)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();
        }

        /// <summary>
        /// Calculates conflict intensity distribution
        /// </summary>
        private Dictionary<ConflictIntensity, int> CalculateConflictIntensityDistribution(List<SocialConflict> conflicts)
        {
            return conflicts
                .GroupBy(c => c.intensity)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Identifies emerging social trends
        /// </summary>
        private List<string> IdentifySocialTrends(
            Dictionary<uint, SocialAgent> socialAgents,
            Dictionary<uint, SocialGroup> activeGroups,
            List<Innovation> culturalInnovations)
        {
            var trends = new List<string>();

            if (_globalSocialMetrics.averageEmpathy > 0.7f)
                trends.Add("High empathy levels promoting social cohesion");

            if (_globalSocialMetrics.culturalDiversity > 0.6f)
                trends.Add("Rich cultural diversity with active trait exchange");

            if (activeGroups.Values.Any() && activeGroups.Values.Average(g => g.cohesion) > 0.7f)
                trends.Add("Strong group formation and maintenance");

            if (socialAgents.Count > 0 && culturalInnovations.Count > socialAgents.Count * 0.1f)
                trends.Add("High rate of cultural innovation");

            return trends;
        }

        /// <summary>
        /// Generates social system recommendations
        /// </summary>
        private List<string> GenerateSocialRecommendations(
            Dictionary<uint, SocialAgent> socialAgents,
            Dictionary<uint, SocialGroup> activeGroups,
            List<SocialEvent> socialEvents)
        {
            var recommendations = new List<string>();

            if (_globalSocialMetrics.averageEmpathy < 0.4f)
                recommendations.Add("Promote empathy development through positive social interactions");

            if (socialAgents.Count > 0 && activeGroups.Count < socialAgents.Count * 0.3f)
                recommendations.Add("Facilitate group formation opportunities");

            if (_globalSocialMetrics.culturalDiversity < 0.3f)
                recommendations.Add("Encourage cultural exchange and innovation");

            var conflicts = socialEvents.OfType<SocialConflict>().ToList();
            if (socialAgents.Count > 0 && conflicts.Count > socialAgents.Count * 0.1f)
                recommendations.Add("Implement conflict resolution training and mediation");

            return recommendations;
        }
    }
}
