using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Shared.Types;
using Laboratory.Core.Enums;
using ProjectChimera.Core;

namespace Laboratory.Systems.Analytics.Services
{
    /// <summary>
    /// Analyzes player behavior patterns and identifies player archetypes.
    /// Extracted from PlayerAnalyticsTracker for single responsibility.
    /// </summary>
    public class BehaviorAnalysisService
    {
        // Events
        public System.Action<PlayerArchetype> OnPlayerArchetypeIdentified;
        public System.Action<BehaviorInsight> OnBehaviorInsightGenerated;

        // Behavior tracking
        private Dictionary<PlayerBehaviorTrait, float> _behaviorTraits = new Dictionary<PlayerBehaviorTrait, float>();
        private Dictionary<string, int> _behaviorPatternCounts = new Dictionary<string, int>();
        private PlayerArchetype _currentArchetype;
        private List<BehaviorInsight> _recentInsights = new List<BehaviorInsight>();

        // Configuration
        private float _behaviorAnalysisInterval;
        private float _lastBehaviorAnalysis;
        private bool _enablePersonalityProfiling;

        public IReadOnlyDictionary<PlayerBehaviorTrait, float> BehaviorTraits => _behaviorTraits;
        public PlayerArchetype CurrentArchetype => _currentArchetype;
        public IReadOnlyList<BehaviorInsight> RecentInsights => _recentInsights.AsReadOnly();

        public BehaviorAnalysisService(
            float behaviorAnalysisInterval = GameConstants.BEHAVIOR_ANALYSIS_INTERVAL,
            bool enablePersonalityProfiling = true)
        {
            _behaviorAnalysisInterval = behaviorAnalysisInterval;
            _enablePersonalityProfiling = enablePersonalityProfiling;
            InitializeBehaviorTraits();
        }

        /// <summary>
        /// Initializes behavior traits with default values
        /// </summary>
        private void InitializeBehaviorTraits()
        {
            _behaviorTraits[PlayerBehaviorTrait.Exploration] = 0.5f;
            _behaviorTraits[PlayerBehaviorTrait.Combat] = 0.5f;
            _behaviorTraits[PlayerBehaviorTrait.Social] = 0.5f;
            _behaviorTraits[PlayerBehaviorTrait.Collection] = 0.5f;
            _behaviorTraits[PlayerBehaviorTrait.Strategy] = 0.5f;
            _behaviorTraits[PlayerBehaviorTrait.Creativity] = 0.5f;
            _behaviorTraits[PlayerBehaviorTrait.Patience] = 0.5f;
            _behaviorTraits[PlayerBehaviorTrait.RiskTaking] = 0.5f;
        }

        /// <summary>
        /// Analyzes a player action and updates behavior traits
        /// </summary>
        public void AnalyzeAction(PlayerAction action)
        {
            if (action == null) return;

            UpdateBehaviorTraitsFromAction(action);
            DetectBehaviorPatterns(action);

            // Periodic full analysis
            if (Time.time - _lastBehaviorAnalysis >= _behaviorAnalysisInterval)
            {
                PerformFullBehaviorAnalysis();
                _lastBehaviorAnalysis = Time.time;
            }
        }

        /// <summary>
        /// Updates behavior traits based on action type
        /// </summary>
        private void UpdateBehaviorTraitsFromAction(PlayerAction action)
        {
            const float TRAIT_UPDATE_AMOUNT = 0.05f;

            switch (action.actionType)
            {
                case "Explore":
                case "Discovery":
                    AdjustTrait(PlayerBehaviorTrait.Exploration, TRAIT_UPDATE_AMOUNT);
                    break;

                case "Combat":
                case "Attack":
                    AdjustTrait(PlayerBehaviorTrait.Combat, TRAIT_UPDATE_AMOUNT);
                    AdjustTrait(PlayerBehaviorTrait.RiskTaking, TRAIT_UPDATE_AMOUNT * 0.5f);
                    break;

                case "Social":
                case "Trade":
                    AdjustTrait(PlayerBehaviorTrait.Social, TRAIT_UPDATE_AMOUNT);
                    break;

                case "Collect":
                case "Gather":
                    AdjustTrait(PlayerBehaviorTrait.Collection, TRAIT_UPDATE_AMOUNT);
                    AdjustTrait(PlayerBehaviorTrait.Patience, TRAIT_UPDATE_AMOUNT * 0.3f);
                    break;

                case "Plan":
                case "Strategy":
                    AdjustTrait(PlayerBehaviorTrait.Strategy, TRAIT_UPDATE_AMOUNT);
                    break;

                case "Build":
                case "Customize":
                    AdjustTrait(PlayerBehaviorTrait.Creativity, TRAIT_UPDATE_AMOUNT);
                    break;
            }
        }

        /// <summary>
        /// Adjusts a behavior trait value
        /// </summary>
        private void AdjustTrait(PlayerBehaviorTrait trait, float amount)
        {
            if (!_behaviorTraits.ContainsKey(trait))
            {
                _behaviorTraits[trait] = 0.5f;
            }

            _behaviorTraits[trait] = Mathf.Clamp01(_behaviorTraits[trait] + amount);
        }

        /// <summary>
        /// Detects behavior patterns from actions
        /// </summary>
        private void DetectBehaviorPatterns(PlayerAction action)
        {
            // Pattern detection logic
            string pattern = DetermineActionPattern(action);

            if (!string.IsNullOrEmpty(pattern))
            {
                if (!_behaviorPatternCounts.ContainsKey(pattern))
                {
                    _behaviorPatternCounts[pattern] = 0;
                }

                _behaviorPatternCounts[pattern]++;

                // Generate insight if pattern is strong
                if (_behaviorPatternCounts[pattern] >= 5)
                {
                    GenerateBehaviorInsight(pattern);
                }
            }
        }

        /// <summary>
        /// Determines the pattern category for an action
        /// </summary>
        private string DetermineActionPattern(PlayerAction action)
        {
            if (action.actionType.Contains("Explore"))
                return "Explorer";
            if (action.actionType.Contains("Combat"))
                return "Combatant";
            if (action.actionType.Contains("Social") || action.actionType.Contains("Trade"))
                return "Socializer";
            if (action.actionType.Contains("Collect") || action.actionType.Contains("Gather"))
                return "Collector";
            if (action.actionType.Contains("Build") || action.actionType.Contains("Customize"))
                return "Creator";

            return null;
        }

        /// <summary>
        /// Performs comprehensive behavior analysis
        /// </summary>
        private void PerformFullBehaviorAnalysis()
        {
            if (!_enablePersonalityProfiling) return;

            // Identify dominant archetype
            PlayerArchetype newArchetype = IdentifyPlayerArchetype();

            if (newArchetype != _currentArchetype)
            {
                _currentArchetype = newArchetype;
                OnPlayerArchetypeIdentified?.Invoke(_currentArchetype);
                Debug.Log($"[BehaviorAnalysisService] Player archetype identified: {_currentArchetype.archetypeType}");
            }
        }

        /// <summary>
        /// Identifies the player's archetype based on behavior traits
        /// </summary>
        private PlayerArchetype IdentifyPlayerArchetype()
        {
            var archetype = new PlayerArchetype();

            // Find dominant trait
            var dominantTrait = _behaviorTraits.OrderByDescending(kvp => kvp.Value).First();

            // Determine archetype type based on dominant behaviors
            if (dominantTrait.Key == PlayerBehaviorTrait.Exploration && dominantTrait.Value > 0.7f)
            {
                archetype.archetypeType = ArchetypeType.Explorer;
            }
            else if (dominantTrait.Key == PlayerBehaviorTrait.Combat && dominantTrait.Value > 0.7f)
            {
                archetype.archetypeType = ArchetypeType.Achiever;
            }
            else if (dominantTrait.Key == PlayerBehaviorTrait.Social && dominantTrait.Value > 0.7f)
            {
                archetype.archetypeType = ArchetypeType.Socializer;
            }
            else if (dominantTrait.Key == PlayerBehaviorTrait.Collection && dominantTrait.Value > 0.7f)
            {
                archetype.archetypeType = ArchetypeType.Collector;
            }
            else
            {
                archetype.archetypeType = ArchetypeType.Balanced;
            }

            archetype.confidence = dominantTrait.Value;
            archetype.primaryTrait = dominantTrait.Key;

            return archetype;
        }

        /// <summary>
        /// Generates a behavior insight
        /// </summary>
        private void GenerateBehaviorInsight(string pattern)
        {
            var insight = new BehaviorInsight
            {
                insightType = "Pattern",
                description = $"Player shows strong {pattern} behavior pattern",
                confidence = Mathf.Clamp01(_behaviorPatternCounts[pattern] / 10f),
                timestamp = Time.time
            };

            _recentInsights.Add(insight);

            // Keep only recent insights
            if (_recentInsights.Count > 20)
            {
                _recentInsights.RemoveAt(0);
            }

            OnBehaviorInsightGenerated?.Invoke(insight);
        }

        /// <summary>
        /// Gets behavior analysis summary
        /// </summary>
        public BehaviorAnalysisStats GetAnalysisStats()
        {
            return new BehaviorAnalysisStats
            {
                currentArchetype = _currentArchetype.archetypeType,
                dominantTrait = _behaviorTraits.OrderByDescending(kvp => kvp.Value).First().Key,
                traitDiversity = CalculateTraitDiversity(),
                patternCount = _behaviorPatternCounts.Count,
                insightCount = _recentInsights.Count
            };
        }

        private float CalculateTraitDiversity()
        {
            if (_behaviorTraits.Count == 0) return 0f;

            // Calculate variance of trait values (lower variance = more balanced/diverse)
            float mean = _behaviorTraits.Values.Average();
            float variance = _behaviorTraits.Values.Sum(v => Mathf.Pow(v - mean, 2)) / _behaviorTraits.Count;

            // Inverse variance for diversity score (higher variance = less diverse)
            return 1f - Mathf.Clamp01(variance);
        }
    }

    /// <summary>
    /// Behavior analysis statistics summary
    /// </summary>
    public struct BehaviorAnalysisStats
    {
        public ArchetypeType currentArchetype;
        public PlayerBehaviorTrait dominantTrait;
        public float traitDiversity;
        public int patternCount;
        public int insightCount;
    }
}
