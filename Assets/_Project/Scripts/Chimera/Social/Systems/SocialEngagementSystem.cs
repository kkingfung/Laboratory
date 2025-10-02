using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Laboratory.Chimera.Social.Core;
using Laboratory.Chimera.Discovery.Core;
using System.Collections.Generic;

namespace Laboratory.Chimera.Social.Systems
{
    /// <summary>
    /// ECS system managing social engagement and viral discovery propagation
    /// Handles likes, shares, trending calculations, and community features
    /// </summary>
    [BurstCompile]
    public partial struct SocialEngagementSystem : ISystem
    {
        private EntityQuery _shareQuery;
        private EntityQuery _interactionQuery;
        private ComponentLookup<SocialShareData> _shareLookup;

        // Engagement tracking
        private NativeHashMap<FixedString64Bytes, EngagementData> _engagementMap;
        private NativeList<TrendingShare> _trendingShares;
        private double _lastTrendingUpdate;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _shareQuery = SystemAPI.QueryBuilder()
                .WithAll<SocialShareData>()
                .Build();

            _interactionQuery = SystemAPI.QueryBuilder()
                .WithAll<Laboratory.Chimera.Social.Core.SocialInteraction>()
                .Build();

            _shareLookup = SystemAPI.GetComponentLookup<SocialShareData>(false);

            _engagementMap = new NativeHashMap<FixedString64Bytes, EngagementData>(1000, Allocator.Persistent);
            _trendingShares = new NativeList<TrendingShare>(100, Allocator.Persistent);
            _lastTrendingUpdate = SystemAPI.Time.ElapsedTime;

            state.RequireForUpdate(_shareQuery);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_engagementMap.IsCreated)
                _engagementMap.Dispose();
            if (_trendingShares.IsCreated)
                _trendingShares.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            _shareLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Process new interactions
            ProcessSocialInteractions(ref state, ecb);

            // Update trending calculations periodically
            if (SystemAPI.Time.ElapsedTime - _lastTrendingUpdate > 60.0) // Every minute
            {
                UpdateTrendingCalculations(ref state);
                _lastTrendingUpdate = SystemAPI.Time.ElapsedTime;
            }

            // Simulate viral growth for high-engagement content
            SimulateViralGrowth(ref state);
        }

        /// <summary>
        /// Process new social interactions (likes, comments, shares)
        /// </summary>
        private void ProcessSocialInteractions(ref SystemState state, EntityCommandBuffer ecb)
        {
            foreach (var (interaction, entity) in SystemAPI.Query<RefRO<Laboratory.Chimera.Social.Core.SocialInteraction>>().WithEntityAccess())
            {
                var shareID = interaction.ValueRO.ShareID;

                // Update engagement data
                if (_engagementMap.TryGetValue(shareID, out EngagementData engagement))
                {
                    engagement = UpdateEngagementData(engagement, interaction.ValueRO);
                    _engagementMap[shareID] = engagement;
                }
                else
                {
                    engagement = CreateEngagementData(interaction.ValueRO);
                    _engagementMap.TryAdd(shareID, engagement);
                }

                // Update the share entity with new engagement
                UpdateShareEngagement(shareID, engagement, ref state);

                // Process specific interaction types
                ProcessInteractionEffects(interaction.ValueRO, engagement);

                // Remove processed interaction
                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Update trending calculations
        /// </summary>
        private void UpdateTrendingCalculations(ref SystemState state)
        {
            _trendingShares.Clear();

            foreach (var (shareData, entity) in SystemAPI.Query<RefRO<SocialShareData>>().WithEntityAccess())
            {
                float trendingScore = CalculateTrendingScore(shareData.ValueRO, (float)SystemAPI.Time.ElapsedTime);

                if (trendingScore > 10f) // Trending threshold
                {
                    _trendingShares.Add(new TrendingShare
                    {
                        ShareID = shareData.ValueRO.ShareID,
                        TrendingScore = trendingScore,
                        ShareEntity = entity
                    });
                }
            }

            // Sort by trending score
            _trendingShares.Sort(new TrendingShareComparer());

            // Mark top shares as trending and update their data
            UpdateTrendingStatus(ref state);
        }

        /// <summary>
        /// Simulate viral growth for high-engagement content
        /// </summary>
        private void SimulateViralGrowth(ref SystemState state)
        {
            foreach (var (shareData, entity) in SystemAPI.Query<RefRW<SocialShareData>>().WithEntityAccess())
            {
                if (shareData.ValueRO.PopularityScore > 100f && UnityEngine.Random.value < 0.1f)
                {
                    // Simulate viral engagement growth
                    int viralLikes = UnityEngine.Random.Range(1, 10);
                    int viralComments = UnityEngine.Random.Range(0, 3);
                    int viralShares = UnityEngine.Random.Range(0, 2);

                    var updatedShare = shareData.ValueRO;
                    updatedShare.UpdateEngagement(
                        updatedShare.LikeCount + viralLikes,
                        updatedShare.CommentCount + viralComments,
                        updatedShare.ShareCount + viralShares
                    );

                    shareData.ValueRW = updatedShare;

                    // Trigger viral milestone events
                    CheckViralMilestones(updatedShare, entity);
                }
            }
        }

        /// <summary>
        /// Check for viral milestones and create events
        /// </summary>
        private void CheckViralMilestones(SocialShareData shareData, Entity shareEntity)
        {
            // Create viral milestone events for significant engagement
            if (shareData.LikeCount == 100 || shareData.LikeCount == 500 || shareData.LikeCount == 1000)
            {
                CreateViralMilestoneEvent(shareEntity, ViralMilestoneType.Likes, shareData.LikeCount);
            }

            if (shareData.ShareCount == 50 || shareData.ShareCount == 100)
            {
                CreateViralMilestoneEvent(shareEntity, ViralMilestoneType.Shares, shareData.ShareCount);
            }
        }

        /// <summary>
        /// Create viral milestone event
        /// </summary>
        private void CreateViralMilestoneEvent(Entity shareEntity, ViralMilestoneType type, int count)
        {
            // This would trigger notifications, achievements, etc.
            Debug.Log($"🔥 Viral milestone reached! {type}: {count}");

            // Could create achievement entities, notifications, etc.
            // var achievementEntity = ecb.CreateEntity();
            // ecb.AddComponent(achievementEntity, new ViralAchievement { ... });
        }

        /// <summary>
        /// Update engagement data from interaction
        /// </summary>
        [BurstCompile]
        private static EngagementData UpdateEngagementData(EngagementData current, Laboratory.Chimera.Social.Core.SocialInteraction interaction)
        {
            switch (interaction.Type)
            {
                case Laboratory.Chimera.Social.Core.InteractionType.Like:
                    current.LikeCount++;
                    current.LastInteractionTime = interaction.Timestamp;
                    break;
                case Laboratory.Chimera.Social.Core.InteractionType.Comment:
                    current.CommentCount++;
                    current.LastInteractionTime = interaction.Timestamp;
                    break;
                case Laboratory.Chimera.Social.Core.InteractionType.Share:
                    current.ShareCount++;
                    current.LastInteractionTime = interaction.Timestamp;
                    break;
            }

            current.TotalEngagement = current.LikeCount + (current.CommentCount * 2) + (current.ShareCount * 5);
            return current;
        }

        /// <summary>
        /// Create initial engagement data
        /// </summary>
        [BurstCompile]
        private static EngagementData CreateEngagementData(Laboratory.Chimera.Social.Core.SocialInteraction interaction)
        {
            var engagement = new EngagementData
            {
                ShareID = interaction.ShareID,
                FirstInteractionTime = interaction.Timestamp,
                LastInteractionTime = interaction.Timestamp
            };

            return UpdateEngagementData(engagement, interaction);
        }

        /// <summary>
        /// Calculate trending score for a share
        /// </summary>
        [BurstCompile]
        private static float CalculateTrendingScore(SocialShareData shareData, float currentTime)
        {
            float hoursSincePost = (currentTime - shareData.ShareTimestamp) / 3600f;
            float engagementRate = shareData.PopularityScore / Unity.Mathematics.math.max(hoursSincePost, 0.1f);

            // Boost score for high-quality content
            float qualityMultiplier = 1f;
            if (shareData.IsFeatured) qualityMultiplier += 0.5f;
            if (shareData.IsVerified) qualityMultiplier += 1f;
            if (shareData.VisualAppeal > 0.8f) qualityMultiplier += 0.3f;

            return engagementRate * qualityMultiplier;
        }

        /// <summary>
        /// Update share engagement from engagement data
        /// </summary>
        private void UpdateShareEngagement(FixedString64Bytes shareID, EngagementData engagement, ref SystemState state)
        {
            // Find and update the share entity
            foreach (var (shareData, entity) in SystemAPI.Query<RefRW<SocialShareData>>().WithEntityAccess())
            {
                if (shareData.ValueRO.ShareID.Equals(shareID))
                {
                    var updatedShare = shareData.ValueRO;
                    updatedShare.UpdateEngagement(engagement.LikeCount, engagement.CommentCount, engagement.ShareCount);
                    shareData.ValueRW = updatedShare;
                    break;
                }
            }
        }

        /// <summary>
        /// Process specific interaction effects
        /// </summary>
        private static void ProcessInteractionEffects(Laboratory.Chimera.Social.Core.SocialInteraction interaction, EngagementData engagement)
        {
            switch (interaction.Type)
            {
                case Laboratory.Chimera.Social.Core.InteractionType.Like:
                    // Likes boost visibility slightly
                    break;
                case Laboratory.Chimera.Social.Core.InteractionType.Comment:
                    // Comments boost engagement significantly
                    break;
                case Laboratory.Chimera.Social.Core.InteractionType.Share:
                    // Shares create viral potential
                    if (engagement.ShareCount > 10)
                    {
                        // Mark as viral candidate
                    }
                    break;
            }
        }

        /// <summary>
        /// Update trending status for top shares
        /// </summary>
        private void UpdateTrendingStatus(ref SystemState state)
        {
            int trendingCount = Unity.Mathematics.math.min(_trendingShares.Length, 20); // Top 20 trending

            for (int i = 0; i < trendingCount; i++)
            {
                var trending = _trendingShares[i];

                // Update share as trending
                foreach (var (shareData, entity) in SystemAPI.Query<RefRW<SocialShareData>>().WithEntityAccess())
                {
                    if (shareData.ValueRO.ShareID.Equals(trending.ShareID))
                    {
                        var updatedShare = shareData.ValueRO;
                        // Add trending flag or boost visibility
                        shareData.ValueRW = updatedShare;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Engagement tracking data
    /// </summary>
    [System.Serializable]
    public struct EngagementData
    {
        public FixedString64Bytes ShareID;
        public int LikeCount;
        public int CommentCount;
        public int ShareCount;
        public int TotalEngagement;
        public uint FirstInteractionTime;
        public uint LastInteractionTime;
    }

    /// <summary>
    /// Trending share tracking
    /// </summary>
    [System.Serializable]
    public struct TrendingShare
    {
        public FixedString64Bytes ShareID;
        public float TrendingScore;
        public Entity ShareEntity;
    }

    /// <summary>
    /// Comparer for sorting trending shares
    /// </summary>
    public struct TrendingShareComparer : System.Collections.Generic.IComparer<TrendingShare>
    {
        public int Compare(TrendingShare x, TrendingShare y)
        {
            return y.TrendingScore.CompareTo(x.TrendingScore); // Descending order
        }
    }

    /// <summary>
    /// Viral milestone types
    /// </summary>
    public enum ViralMilestoneType
    {
        Likes,
        Comments,
        Shares,
        Views
    }

    /// <summary>
    /// System for automatic content generation and community simulation
    /// </summary>
    [BurstCompile]
    public partial struct CommunitySimulationSystem : ISystem
    {
        private double _lastSimulationUpdate;
        private Unity.Mathematics.Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _lastSimulationUpdate = SystemAPI.Time.ElapsedTime;
            _random = new Unity.Mathematics.Random(1234);
        }

        public void OnUpdate(ref SystemState state)
        {
            // Run community simulation every 30 seconds
            if (SystemAPI.Time.ElapsedTime - _lastSimulationUpdate < 30.0)
                return;

            _lastSimulationUpdate = SystemAPI.Time.ElapsedTime;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Simulate community interactions
            SimulateCommunityActivity(ref state, ecb);
        }

        /// <summary>
        /// Simulate ongoing community activity
        /// </summary>
        private void SimulateCommunityActivity(ref SystemState state, EntityCommandBuffer ecb)
        {
            // Generate random interactions for existing shares
            foreach (var (shareData, entity) in SystemAPI.Query<RefRO<SocialShareData>>().WithEntityAccess())
            {
                if (_random.NextFloat() < 0.3f) // 30% chance of new interaction
                {
                    GenerateRandomInteraction(shareData.ValueRO, ecb, ref state);
                }
            }

            // Occasionally generate new community shares
            if (_random.NextFloat() < 0.1f) // 10% chance of new share
            {
                GenerateRandomCommunityShare(ecb);
            }
        }

        /// <summary>
        /// Generate random interaction for a share
        /// </summary>
        private void GenerateRandomInteraction(SocialShareData shareData, EntityCommandBuffer ecb, ref SystemState state)
        {
            var interactionTypes = new Laboratory.Chimera.Social.Core.InteractionType[] { Laboratory.Chimera.Social.Core.InteractionType.Like, Laboratory.Chimera.Social.Core.InteractionType.Comment, Laboratory.Chimera.Social.Core.InteractionType.Share };
            var randomType = interactionTypes[_random.NextInt(0, interactionTypes.Length)];

            // Weight interactions based on share quality
            float interactionChance = 0.1f + (shareData.VisualAppeal * 0.4f);
            if (_random.NextFloat() > interactionChance) return;

            var interaction = new Laboratory.Chimera.Social.Core.SocialInteraction
            {
                InteractionID = new FixedString64Bytes(System.Guid.NewGuid().ToString("N")[..12]),
                ShareID = shareData.ShareID,
                PlayerName = new FixedString64Bytes(GenerateRandomPlayerName()),
                Type = randomType,
                Timestamp = (uint)SystemAPI.Time.ElapsedTime,
                Content = randomType == Laboratory.Chimera.Social.Core.InteractionType.Comment ? new FixedString128Bytes(GenerateRandomComment()) : default
            };

            var interactionEntity = ecb.CreateEntity();
            ecb.AddComponent(interactionEntity, interaction);
        }

        /// <summary>
        /// Generate random community share
        /// </summary>
        private void GenerateRandomCommunityShare(EntityCommandBuffer ecb)
        {
            // This would create realistic community content
            // For now, just create a placeholder
            var shareEntity = ecb.CreateEntity();
            // ecb.AddComponent(shareEntity, GenerateRandomShare());
        }

        /// <summary>
        /// Generate random player name for simulation
        /// </summary>
        private string GenerateRandomPlayerName()
        {
            string[] prefixes = { "Dragon", "Gene", "Bio", "Evo", "Chimera", "Nova", "Apex", "Hyper" };
            string[] suffixes = { "Master", "Lord", "Expert", "Hunter", "Keeper", "Weaver", "Forge", "Prime" };

            return $"{prefixes[_random.NextInt(0, prefixes.Length)]}{suffixes[_random.NextInt(0, suffixes.Length)]}{_random.NextInt(100, 1000)}";
        }

        /// <summary>
        /// Generate random comment for simulation
        /// </summary>
        private string GenerateRandomComment()
        {
            string[] comments = {
                "Amazing genetics! How did you breed this?",
                "This is incredible! 🔥",
                "Wow, those stats are insane!",
                "I need to try this breeding combo!",
                "Absolutely stunning creature!",
                "This is going to inspire my next project!",
                "Such beautiful genetic markers!",
                "Goals! 🎯",
                "The RNG gods have blessed you!",
                "Teaching moment right here!"
            };

            return comments[_random.NextInt(0, comments.Length)];
        }
    }
}