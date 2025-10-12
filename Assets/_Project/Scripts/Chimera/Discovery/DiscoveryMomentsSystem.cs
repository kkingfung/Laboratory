using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Discovery
{
    /// <summary>
    /// Discovery Moments System for Project Chimera.
    /// Creates epic "holy crap" moments when players discover new genetic combinations,
    /// with screen-wide celebrations, community notifications, and lasting recognition.
    ///
    /// Features:
    /// - Epic Discovery Celebrations: Screen-wide visual celebrations for major discoveries
    /// - Community Fame System: Discoveries get named after their discoverers
    /// - Discovery Journals: Personal and community research documentation
    /// - Breakthrough Notifications: Server-wide announcements of major discoveries
    /// - Discovery Rarity System: Different celebration levels based on discovery significance
    /// - Legacy Tracking: Permanent records of who discovered what first
    /// </summary>
    public class DiscoveryMomentsSystem : MonoBehaviour
    {
        [Header("Discovery Configuration")]
        [SerializeField] private DiscoveryMomentsConfig config;
        [SerializeField] private bool enableDiscoveryCelebrations = true;
        [SerializeField] private bool enableCommunityNotifications = true;
        [SerializeField] private bool enableNamingRights = true;
        [SerializeField] private bool enableDiscoveryJournal = true;

        [Header("Celebration Settings")]
        [SerializeField] private float celebrationDuration = 5f;
        [SerializeField] private AnimationCurve celebrationIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AudioSource celebrationAudioSource;

        [Header("UI References")]
        [SerializeField] private Canvas celebrationCanvas;
        [SerializeField] private GameObject celebrationEffectsPrefab;
        [SerializeField] private Transform celebrationParent;

        // Discovery tracking and management
        private Dictionary<string, DiscoveryRecord> globalDiscoveryDatabase = new Dictionary<string, DiscoveryRecord>();
        private Dictionary<string, PlayerDiscoveryProfile> playerDiscoveryProfiles = new Dictionary<string, PlayerDiscoveryProfile>();
        private List<DiscoveryEvent> recentDiscoveries = new List<DiscoveryEvent>();

        // Active celebrations
        private List<ActiveCelebration> activeCelebrations = new List<ActiveCelebration>();

        // Discovery rarity tracking
        private Dictionary<DiscoveryType, float> discoveryRarityWeights = new Dictionary<DiscoveryType, float>();

        // Events for other systems
        public static event Action<DiscoveryEvent> OnMajorDiscovery;
        public static event Action<DiscoveryEvent> OnLegendaryDiscovery;
        public static event Action<DiscoveryEvent> OnWorldFirstDiscovery;
        public static event Action<DiscoveryEvent> OnDiscoveryJournalUpdated;

        void Start()
        {
            InitializeDiscoverySystem();
            InvokeRepeating(nameof(UpdateActiveCelebrations), 0.1f, 0.1f);
            LoadDiscoveryDatabase();
        }

        #region Initialization

        private void InitializeDiscoverySystem()
        {
            // Initialize discovery rarity weights
            InitializeRarityWeights();

            // Set up celebration canvas
            if (celebrationCanvas == null)
            {
                celebrationCanvas = FindFirstObjectByType<Canvas>();
            }

            UnityEngine.Debug.Log("Discovery Moments System initialized - Ready to celebrate breakthroughs!");
        }

        private void InitializeRarityWeights()
        {
            discoveryRarityWeights[DiscoveryType.NewTrait] = 1f;
            discoveryRarityWeights[DiscoveryType.RareGeneCombination] = 0.3f;
            discoveryRarityWeights[DiscoveryType.UniqueSpeciesVariant] = 0.1f;
            discoveryRarityWeights[DiscoveryType.LegendaryBloodline] = 0.05f;
            discoveryRarityWeights[DiscoveryType.MythicalCreature] = 0.01f;
            discoveryRarityWeights[DiscoveryType.WorldFirst] = 0.001f;
        }

        #endregion

        #region Discovery Processing

        /// <summary>
        /// Processes a potential genetic discovery and triggers celebrations if significant
        /// </summary>
        public DiscoveryEvent? ProcessPotentialDiscovery(GeneticProfile profile, string playerId, Vector3 discoveryLocation)
        {
            if (profile?.Genes == null) return null;

            // Analyze the genetic profile for significant discoveries
            var discoveryAnalysis = AnalyzeGeneticDiscovery(profile, playerId);

            if (discoveryAnalysis.significance < DiscoverySignificance.Minor)
                return null; // Not significant enough

            // Create discovery event
            var discoveryEvent = CreateDiscoveryEvent(discoveryAnalysis, profile, playerId, discoveryLocation);

            // Record the discovery
            RecordDiscovery(discoveryEvent);

            // Trigger appropriate celebrations
            TriggerDiscoveryCelebration(discoveryEvent);

            // Handle community notifications
            if (enableCommunityNotifications && discoveryEvent.significance >= DiscoverySignificance.Major)
            {
                BroadcastDiscoveryToCommuity(discoveryEvent);
            }

            // Update player discovery profile
            UpdatePlayerDiscoveryProfile(playerId, discoveryEvent);

            // Update discovery journal
            if (enableDiscoveryJournal)
            {
                UpdateDiscoveryJournal(discoveryEvent);
            }

            UnityEngine.Debug.Log($"DISCOVERY: {discoveryEvent.discoveryName} by {discoveryEvent.discovererName} ({discoveryEvent.significance})");

            return discoveryEvent;
        }

        private DiscoveryAnalysis AnalyzeGeneticDiscovery(GeneticProfile profile, string playerId)
        {
            var analysis = new DiscoveryAnalysis
            {
                playerId = playerId,
                geneticSignature = GenerateGeneticSignature(profile),
                traitCombinations = AnalyzeTraitCombinations(profile),
                rarityScore = CalculateGeneticRarity(profile),
                isWorldFirst = false
            };

            // Check if this genetic combination has been discovered before
            if (!globalDiscoveryDatabase.ContainsKey(analysis.geneticSignature))
            {
                analysis.isWorldFirst = true;
                analysis.rarityScore *= 2f; // World firsts are extra rare
            }

            // Determine discovery type and significance
            analysis.discoveryType = DetermineDiscoveryType(profile, analysis);
            analysis.significance = CalculateDiscoverySignificance(analysis);

            return analysis;
        }

        private string GenerateGeneticSignature(GeneticProfile profile)
        {
            // Create a unique signature based on genetic traits
            var significantGenes = profile.Genes
                .Where(g => g.isActive && g.value.HasValue && g.value.Value > 0.7f)
                .OrderBy(g => g.traitName)
                .Select(g => $"{g.traitName}:{g.value.Value:F2}");

            return string.Join("|", significantGenes);
        }

        private List<TraitCombination> AnalyzeTraitCombinations(GeneticProfile profile)
        {
            var combinations = new List<TraitCombination>();

            var activeGenes = profile.Genes.Where(g => g.isActive && g.value.HasValue).ToArray();

            // Look for interesting trait combinations
            for (int i = 0; i < activeGenes.Length; i++)
            {
                for (int j = i + 1; j < activeGenes.Length; j++)
                {
                    var gene1 = activeGenes[i];
                    var gene2 = activeGenes[j];

                    // Check if this combination is interesting
                    if (IsInterestingCombination(gene1, gene2))
                    {
                        combinations.Add(new TraitCombination
                        {
                            trait1 = gene1.traitName,
                            trait2 = gene2.traitName,
                            combinedValue = (gene1.value.Value + gene2.value.Value) / 2f,
                            synergistic = CheckSynergy(gene1, gene2)
                        });
                    }
                }
            }

            return combinations;
        }

        private bool IsInterestingCombination(Gene gene1, Gene gene2)
        {
            // Check for synergistic or rare combinations
            if (gene1.value.Value > 0.8f && gene2.value.Value > 0.8f)
                return true; // Both traits are very strong

            // Check for known interesting combinations
            var interestingPairs = new[]
            {
                ("Bioluminescence", "Transparency"),
                ("Fire Resistance", "Ice Resistance"),
                ("Photosynthesis", "Carnivorous"),
                ("Time Manipulation", "Phase Shift"),
                ("Telepathy", "Empathy")
            };

            foreach (var (trait1, trait2) in interestingPairs)
            {
                if ((gene1.traitName.Contains(trait1) && gene2.traitName.Contains(trait2)) ||
                    (gene1.traitName.Contains(trait2) && gene2.traitName.Contains(trait1)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckSynergy(Gene gene1, Gene gene2)
        {
            // Simple synergy check - could be expanded with more complex logic
            return gene1.traitType == gene2.traitType &&
                   Math.Abs(gene1.value.Value - gene2.value.Value) < 0.2f;
        }

        private float CalculateGeneticRarity(GeneticProfile profile)
        {
            float rarityScore = 0f;

            foreach (var gene in profile.Genes.Where(g => g.isActive && g.value.HasValue))
            {
                // High values are rarer
                rarityScore += Mathf.Pow(gene.value.Value, 3f);

                // Mutations increase rarity
                if (gene.isMutation)
                    rarityScore += 0.5f;

                // Enhanced expression is rarer
                if (gene.expression == GeneExpression.Enhanced)
                    rarityScore += 0.3f;
            }

            // Generation affects rarity (higher generation can be rarer due to accumulated traits)
            rarityScore += profile.Generation * 0.1f;

            // Genetic purity affects rarity
            rarityScore += profile.GetGeneticPurity() * 0.5f;

            return rarityScore;
        }

        private DiscoveryType DetermineDiscoveryType(GeneticProfile profile, DiscoveryAnalysis analysis)
        {
            if (analysis.isWorldFirst && analysis.rarityScore > 10f)
                return DiscoveryType.WorldFirst;

            if (analysis.rarityScore > 8f)
                return DiscoveryType.MythicalCreature;

            if (analysis.rarityScore > 6f)
                return DiscoveryType.LegendaryBloodline;

            if (analysis.rarityScore > 4f)
                return DiscoveryType.UniqueSpeciesVariant;

            if (analysis.rarityScore > 2f)
                return DiscoveryType.RareGeneCombination;

            return DiscoveryType.NewTrait;
        }

        private DiscoverySignificance CalculateDiscoverySignificance(DiscoveryAnalysis analysis)
        {
            if (analysis.discoveryType == DiscoveryType.WorldFirst)
                return DiscoverySignificance.WorldChanging;

            if (analysis.discoveryType == DiscoveryType.MythicalCreature)
                return DiscoverySignificance.Legendary;

            if (analysis.discoveryType == DiscoveryType.LegendaryBloodline)
                return DiscoverySignificance.Epic;

            if (analysis.discoveryType == DiscoveryType.UniqueSpeciesVariant)
                return DiscoverySignificance.Major;

            if (analysis.discoveryType == DiscoveryType.RareGeneCombination)
                return DiscoverySignificance.Notable;

            return DiscoverySignificance.Minor;
        }

        #endregion

        #region Discovery Event Creation

        private DiscoveryEvent CreateDiscoveryEvent(DiscoveryAnalysis analysis, GeneticProfile profile, string playerId, Vector3 location)
        {
            var discoveryEvent = new DiscoveryEvent
            {
                id = Guid.NewGuid().ToString(),
                discoveryType = analysis.discoveryType,
                significance = analysis.significance,
                discovererPlayerId = playerId,
                discovererName = GetPlayerDisplayName(playerId),
                geneticSignature = analysis.geneticSignature,
                discoveryLocation = location,
                timestamp = Time.time,
                rarityScore = analysis.rarityScore,
                isWorldFirst = analysis.isWorldFirst,
                traitCombinations = analysis.traitCombinations,
                relatedProfile = profile
            };

            // Generate discovery name
            discoveryEvent.discoveryName = GenerateDiscoveryName(discoveryEvent, profile);

            // Generate discovery description
            discoveryEvent.description = GenerateDiscoveryDescription(discoveryEvent, profile);

            return discoveryEvent;
        }

        private string GenerateDiscoveryName(DiscoveryEvent discoveryEvent, GeneticProfile profile)
        {
            if (enableNamingRights && discoveryEvent.isWorldFirst)
            {
                // World firsts get named after the discoverer
                return $"{discoveryEvent.discovererName}'s {GetDiscoveryTypeDisplayName(discoveryEvent.discoveryType)}";
            }

            // Generate descriptive names based on traits
            var prominentTraits = profile.Genes
                .Where(g => g.isActive && g.value.HasValue && g.value.Value > 0.8f)
                .OrderByDescending(g => g.value.Value)
                .Take(2)
                .Select(g => g.traitName)
                .ToArray();

            if (prominentTraits.Length >= 2)
            {
                return $"{prominentTraits[0]}-{prominentTraits[1]} Hybrid";
            }
            else if (prominentTraits.Length == 1)
            {
                return $"Enhanced {prominentTraits[0]} Variant";
            }

            return $"Unique {GetDiscoveryTypeDisplayName(discoveryEvent.discoveryType)}";
        }

        private string GetDiscoveryTypeDisplayName(DiscoveryType type)
        {
            return type switch
            {
                DiscoveryType.NewTrait => "Trait",
                DiscoveryType.RareGeneCombination => "Gene Combination",
                DiscoveryType.UniqueSpeciesVariant => "Species Variant",
                DiscoveryType.LegendaryBloodline => "Bloodline",
                DiscoveryType.MythicalCreature => "Mythical Creature",
                DiscoveryType.WorldFirst => "World Discovery",
                _ => "Discovery"
            };
        }

        private string GenerateDiscoveryDescription(DiscoveryEvent discoveryEvent, GeneticProfile profile)
        {
            var baseDescription = discoveryEvent.significance switch
            {
                DiscoverySignificance.WorldChanging => "A discovery that will reshape our understanding of genetics forever",
                DiscoverySignificance.Legendary => "An unprecedented genetic phenomenon that defies current scientific knowledge",
                DiscoverySignificance.Epic => "A remarkable genetic achievement that pushes the boundaries of what's possible",
                DiscoverySignificance.Major => "A significant genetic breakthrough with important implications",
                DiscoverySignificance.Notable => "An interesting genetic variation worthy of further study",
                DiscoverySignificance.Minor => "A noteworthy genetic observation",
                _ => "A genetic discovery"
            };

            var traitDescription = string.Join(", ",
                profile.Genes
                    .Where(g => g.isActive && g.value.HasValue && g.value.Value > 0.7f)
                    .Select(g => $"{g.traitName} ({g.value.Value:P0})")
                    .Take(3));

            return $"{baseDescription}. Key traits: {traitDescription}";
        }

        private string GetPlayerDisplayName(string playerId)
        {
            // In a real implementation, this would look up the player's display name
            return $"Researcher_{playerId[..Math.Min(8, playerId.Length)]}";
        }

        #endregion

        #region Celebration System

        private void TriggerDiscoveryCelebration(DiscoveryEvent discoveryEvent)
        {
            if (!enableDiscoveryCelebrations) return;

            var celebrationIntensity = GetCelebrationIntensity(discoveryEvent.significance);
            var celebration = new ActiveCelebration
            {
                discoveryEvent = discoveryEvent,
                intensity = celebrationIntensity,
                duration = celebrationDuration * celebrationIntensity,
                startTime = Time.time,
                celebrationEffects = CreateCelebrationEffects(discoveryEvent)
            };

            activeCelebrations.Add(celebration);

            // Play celebration audio
            if (celebrationAudioSource != null)
            {
                PlayCelebrationAudio(discoveryEvent.significance);
            }

            // Trigger appropriate events
            switch (discoveryEvent.significance)
            {
                case DiscoverySignificance.WorldChanging:
                    OnWorldFirstDiscovery?.Invoke(discoveryEvent);
                    break;
                case DiscoverySignificance.Legendary:
                case DiscoverySignificance.Epic:
                    OnLegendaryDiscovery?.Invoke(discoveryEvent);
                    break;
                case DiscoverySignificance.Major:
                case DiscoverySignificance.Notable:
                    OnMajorDiscovery?.Invoke(discoveryEvent);
                    break;
            }

            UnityEngine.Debug.Log($"CELEBRATION: {discoveryEvent.discoveryName} celebration started with intensity {celebrationIntensity:F2}");
        }

        private float GetCelebrationIntensity(DiscoverySignificance significance)
        {
            return significance switch
            {
                DiscoverySignificance.WorldChanging => 1f,
                DiscoverySignificance.Legendary => 0.9f,
                DiscoverySignificance.Epic => 0.8f,
                DiscoverySignificance.Major => 0.6f,
                DiscoverySignificance.Notable => 0.4f,
                DiscoverySignificance.Minor => 0.2f,
                _ => 0.1f
            };
        }

        private List<CelebrationEffect> CreateCelebrationEffects(DiscoveryEvent discoveryEvent)
        {
            var effects = new List<CelebrationEffect>();

            // Screen-wide particle effects
            effects.Add(new CelebrationEffect
            {
                type = CelebrationEffectType.ParticleExplosion,
                intensity = GetCelebrationIntensity(discoveryEvent.significance),
                color = GetDiscoveryColor(discoveryEvent.discoveryType),
                duration = celebrationDuration
            });

            // Screen flash for major discoveries
            if (discoveryEvent.significance >= DiscoverySignificance.Major)
            {
                effects.Add(new CelebrationEffect
                {
                    type = CelebrationEffectType.ScreenFlash,
                    intensity = GetCelebrationIntensity(discoveryEvent.significance) * 0.5f,
                    color = Color.white,
                    duration = 0.5f
                });
            }

            // Text announcement
            effects.Add(new CelebrationEffect
            {
                type = CelebrationEffectType.TextAnnouncement,
                intensity = 1f,
                color = GetDiscoveryColor(discoveryEvent.discoveryType),
                duration = celebrationDuration,
                text = $"DISCOVERY: {discoveryEvent.discoveryName}!"
            });

            // Camera shake for legendary discoveries
            if (discoveryEvent.significance >= DiscoverySignificance.Legendary)
            {
                effects.Add(new CelebrationEffect
                {
                    type = CelebrationEffectType.CameraShake,
                    intensity = GetCelebrationIntensity(discoveryEvent.significance) * 0.3f,
                    duration = 2f
                });
            }

            return effects;
        }

        private Color GetDiscoveryColor(DiscoveryType type)
        {
            return type switch
            {
                DiscoveryType.NewTrait => Color.green,
                DiscoveryType.RareGeneCombination => Color.blue,
                DiscoveryType.UniqueSpeciesVariant => Color.purple,
                DiscoveryType.LegendaryBloodline => Color.yellow,
                DiscoveryType.MythicalCreature => Color.red,
                DiscoveryType.WorldFirst => Color.white,
                _ => Color.cyan
            };
        }

        private void PlayCelebrationAudio(DiscoverySignificance significance)
        {
            // Play different audio based on significance
            // In a real implementation, this would reference specific audio clips
            if (celebrationAudioSource != null)
            {
                celebrationAudioSource.pitch = significance switch
                {
                    DiscoverySignificance.WorldChanging => 1.2f,
                    DiscoverySignificance.Legendary => 1.1f,
                    DiscoverySignificance.Epic => 1.05f,
                    _ => 1f
                };

                celebrationAudioSource.volume = GetCelebrationIntensity(significance);
                // celebrationAudioSource.Play(); // Would play actual audio clip
            }
        }

        private void UpdateActiveCelebrations()
        {
            for (int i = activeCelebrations.Count - 1; i >= 0; i--)
            {
                var celebration = activeCelebrations[i];
                var elapsedTime = Time.time - celebration.startTime;

                if (elapsedTime >= celebration.duration)
                {
                    // End celebration
                    EndCelebration(celebration);
                    activeCelebrations.RemoveAt(i);
                }
                else
                {
                    // Update celebration effects
                    UpdateCelebrationEffects(celebration, elapsedTime / celebration.duration);
                }
            }
        }

        private void UpdateCelebrationEffects(ActiveCelebration celebration, float progress)
        {
            var intensity = celebrationIntensityCurve.Evaluate(progress) * celebration.intensity;

            foreach (var effect in celebration.celebrationEffects)
            {
                UpdateCelebrationEffect(effect, intensity, progress);
            }
        }

        private void UpdateCelebrationEffect(CelebrationEffect effect, float intensity, float progress)
        {
            switch (effect.type)
            {
                case CelebrationEffectType.ParticleExplosion:
                    // Update particle system intensity
                    break;
                case CelebrationEffectType.ScreenFlash:
                    // Update screen flash alpha
                    break;
                case CelebrationEffectType.TextAnnouncement:
                    // Update text animation
                    break;
                case CelebrationEffectType.CameraShake:
                    // Update camera shake
                    break;
            }
        }

        private void EndCelebration(ActiveCelebration celebration)
        {
            // Clean up celebration effects
            foreach (var effect in celebration.celebrationEffects)
            {
                CleanupCelebrationEffect(effect);
            }
        }

        private void CleanupCelebrationEffect(CelebrationEffect effect)
        {
            // Clean up specific effect resources
            switch (effect.type)
            {
                case CelebrationEffectType.ParticleExplosion:
                    // Stop particle systems
                    break;
                case CelebrationEffectType.ScreenFlash:
                    // Reset screen overlay
                    break;
                case CelebrationEffectType.TextAnnouncement:
                    // Remove text elements
                    break;
                case CelebrationEffectType.CameraShake:
                    // Reset camera position
                    break;
            }
        }

        #endregion

        #region Community and Documentation

        private void BroadcastDiscoveryToCommuity(DiscoveryEvent discoveryEvent)
        {
            // Send discovery to community notification system
            var notification = new CommunityNotification
            {
                type = CommunityNotificationType.MajorDiscovery,
                title = $"New Discovery: {discoveryEvent.discoveryName}",
                message = $"{discoveryEvent.discovererName} has discovered {discoveryEvent.discoveryName}!",
                discoveryEvent = discoveryEvent,
                timestamp = Time.time
            };

            // Broadcast to all players
            BroadcastCommunityNotification(notification);

            UnityEngine.Debug.Log($"COMMUNITY: Broadcasting discovery {discoveryEvent.discoveryName} to all players");
        }

        private void BroadcastCommunityNotification(CommunityNotification notification)
        {
            // In a real implementation, this would send to all connected players
            // For now, we'll just log it
            UnityEngine.Debug.Log($"Community Notification: {notification.title} - {notification.message}");
        }

        private void UpdatePlayerDiscoveryProfile(string playerId, DiscoveryEvent discoveryEvent)
        {
            if (!playerDiscoveryProfiles.ContainsKey(playerId))
            {
                playerDiscoveryProfiles[playerId] = new PlayerDiscoveryProfile
                {
                    playerId = playerId,
                    playerName = GetPlayerDisplayName(playerId),
                    discoveries = new List<DiscoveryEvent>(),
                    totalDiscoveries = 0,
                    worldFirsts = 0,
                    legendaryDiscoveries = 0,
                    discoveryScore = 0f
                };
            }

            var profile = playerDiscoveryProfiles[playerId];
            profile.discoveries.Add(discoveryEvent);
            profile.totalDiscoveries++;

            if (discoveryEvent.isWorldFirst)
                profile.worldFirsts++;

            if (discoveryEvent.significance >= DiscoverySignificance.Legendary)
                profile.legendaryDiscoveries++;

            profile.discoveryScore += CalculateDiscoveryScore(discoveryEvent);

            playerDiscoveryProfiles[playerId] = profile;
        }

        private float CalculateDiscoveryScore(DiscoveryEvent discoveryEvent)
        {
            float baseScore = discoveryEvent.significance switch
            {
                DiscoverySignificance.WorldChanging => 1000f,
                DiscoverySignificance.Legendary => 500f,
                DiscoverySignificance.Epic => 250f,
                DiscoverySignificance.Major => 100f,
                DiscoverySignificance.Notable => 50f,
                DiscoverySignificance.Minor => 10f,
                _ => 1f
            };

            if (discoveryEvent.isWorldFirst)
                baseScore *= 2f;

            return baseScore + discoveryEvent.rarityScore * 10f;
        }

        private void UpdateDiscoveryJournal(DiscoveryEvent discoveryEvent)
        {
            recentDiscoveries.Add(discoveryEvent);

            // Keep only recent discoveries (last 100)
            if (recentDiscoveries.Count > 100)
            {
                recentDiscoveries.RemoveAt(0);
            }

            OnDiscoveryJournalUpdated?.Invoke(discoveryEvent);
        }

        private void RecordDiscovery(DiscoveryEvent discoveryEvent)
        {
            globalDiscoveryDatabase[discoveryEvent.geneticSignature] = new DiscoveryRecord
            {
                discoveryEvent = discoveryEvent,
                timesRediscovered = 0,
                lastRediscoveryTime = 0f,
                permanentlyNamed = discoveryEvent.isWorldFirst && enableNamingRights
            };
        }

        #endregion

        #region Discovery Database Management

        private void LoadDiscoveryDatabase()
        {
            // Load saved discovery data
            // In a real implementation, this would load from persistent storage
        }

        private void SaveDiscoveryDatabase()
        {
            // Save discovery data to persistent storage
            // In a real implementation, this would save to file/database
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveDiscoveryDatabase();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SaveDiscoveryDatabase();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets all discoveries made by a specific player
        /// </summary>
        public PlayerDiscoveryProfile? GetPlayerDiscoveries(string playerId)
        {
            return playerDiscoveryProfiles.TryGetValue(playerId, out var profile) ? profile : null;
        }

        /// <summary>
        /// Gets recent community discoveries
        /// </summary>
        public DiscoveryEvent[] GetRecentDiscoveries(int maxCount = 20)
        {
            return recentDiscoveries.OrderByDescending(d => d.timestamp).Take(maxCount).ToArray();
        }

        /// <summary>
        /// Gets all world-first discoveries
        /// </summary>
        public DiscoveryEvent[] GetWorldFirstDiscoveries()
        {
            return globalDiscoveryDatabase.Values
                .Where(r => r.discoveryEvent.isWorldFirst)
                .Select(r => r.discoveryEvent)
                .OrderByDescending(d => d.timestamp)
                .ToArray();
        }

        /// <summary>
        /// Checks if a genetic signature has been discovered before
        /// </summary>
        public bool IsKnownDiscovery(string geneticSignature)
        {
            return globalDiscoveryDatabase.ContainsKey(geneticSignature);
        }

        /// <summary>
        /// Forces a discovery event (for testing or special occasions)
        /// </summary>
        public void ForceDiscovery(GeneticProfile profile, string playerId, DiscoverySignificance forcedSignificance)
        {
            var analysis = AnalyzeGeneticDiscovery(profile, playerId);
            analysis.significance = forcedSignificance;

            var discoveryEvent = CreateDiscoveryEvent(analysis, profile, playerId, Vector3.zero);
            RecordDiscovery(discoveryEvent);
            TriggerDiscoveryCelebration(discoveryEvent);
        }

        /// <summary>
        /// Gets leaderboard of top discoverers
        /// </summary>
        public PlayerDiscoveryProfile[] GetDiscoveryLeaderboard(int maxCount = 10)
        {
            return playerDiscoveryProfiles.Values
                .OrderByDescending(p => p.discoveryScore)
                .Take(maxCount)
                .ToArray();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Analysis results for a potential genetic discovery
    /// </summary>
    [Serializable]
    public struct DiscoveryAnalysis
    {
        public string playerId;
        public string geneticSignature;
        public List<TraitCombination> traitCombinations;
        public float rarityScore;
        public bool isWorldFirst;
        public DiscoveryType discoveryType;
        public DiscoverySignificance significance;
    }

    /// <summary>
    /// Represents an interesting trait combination
    /// </summary>
    [Serializable]
    public struct TraitCombination
    {
        public string trait1;
        public string trait2;
        public float combinedValue;
        public bool synergistic;
    }

    /// <summary>
    /// Complete discovery event data
    /// </summary>
    [Serializable]
    public struct DiscoveryEvent
    {
        public string id;
        public DiscoveryType discoveryType;
        public DiscoverySignificance significance;
        public string discovererPlayerId;
        public string discovererName;
        public string discoveryName;
        public string description;
        public string geneticSignature;
        public Vector3 discoveryLocation;
        public float timestamp;
        public float rarityScore;
        public bool isWorldFirst;
        public List<TraitCombination> traitCombinations;
        public GeneticProfile relatedProfile;

        public string timeAgo
        {
            get
            {
                var elapsed = Time.time - timestamp;
                var hours = elapsed / 3600f;
                var days = elapsed / 86400f;

                return days >= 1 ? $"{Mathf.FloorToInt(days)} days ago" :
                       hours >= 1 ? $"{Mathf.FloorToInt(hours)} hours ago" :
                       "Just now";
            }
        }
    }

    /// <summary>
    /// Types of genetic discoveries
    /// </summary>
    public enum DiscoveryType
    {
        NewTrait,           // Basic new trait discovery
        RareGeneCombination, // Uncommon gene combination
        UniqueSpeciesVariant, // Unique species variant
        LegendaryBloodline,  // Legendary genetic line
        MythicalCreature,    // Mythical-level creature
        WorldFirst,         // First-ever discovery
        RareMutation,       // Genetic mutation occurred
        SpecialMarker,      // Special genetic marker activated
        PerfectGenetics,    // All traits at maximum values
        NewSpecies,         // Completely new genetic combination
        LegendaryLineage    // Breeding line reaches legendary status
    }

    /// <summary>
    /// Significance levels for discoveries
    /// </summary>
    public enum DiscoverySignificance
    {
        None,           // Not significant
        Minor,          // Minor interest
        Notable,        // Worth noting
        Major,          // Major breakthrough
        Epic,           // Epic discovery
        Legendary,      // Legendary find
        WorldChanging   // Changes everything
    }

    /// <summary>
    /// Record of a discovery in the global database
    /// </summary>
    [Serializable]
    public struct DiscoveryRecord
    {
        public DiscoveryEvent discoveryEvent;
        public int timesRediscovered;
        public float lastRediscoveryTime;
        public bool permanentlyNamed;
    }

    /// <summary>
    /// Player's discovery profile and achievements
    /// </summary>
    [Serializable]
    public struct PlayerDiscoveryProfile
    {
        public string playerId;
        public string playerName;
        public List<DiscoveryEvent> discoveries;
        public int totalDiscoveries;
        public int worldFirsts;
        public int legendaryDiscoveries;
        public float discoveryScore;

        public string rank => discoveryScore switch
        {
            >= 10000f => "Legendary Researcher",
            >= 5000f => "Master Geneticist",
            >= 2000f => "Expert Breeder",
            >= 1000f => "Skilled Researcher",
            >= 500f => "Apprentice Scientist",
            _ => "Novice Explorer"
        };
    }

    /// <summary>
    /// Active celebration being displayed
    /// </summary>
    [Serializable]
    public struct ActiveCelebration
    {
        public DiscoveryEvent discoveryEvent;
        public float intensity;
        public float duration;
        public float startTime;
        public List<CelebrationEffect> celebrationEffects;
    }

    /// <summary>
    /// Individual celebration effect
    /// </summary>
    [Serializable]
    public struct CelebrationEffect
    {
        public CelebrationEffectType type;
        public float intensity;
        public Color color;
        public float duration;
        public string text; // For text announcements
        public Vector3 position; // For positioned effects
        public GameObject effectObject; // Reference to effect GameObject
    }

    /// <summary>
    /// Types of celebration effects
    /// </summary>
    public enum CelebrationEffectType
    {
        ParticleExplosion,  // Particle burst
        ScreenFlash,        // Screen flash effect
        TextAnnouncement,   // Text overlay
        CameraShake,        // Camera shake
        ColorOverlay,       // Screen color overlay
        SoundEffect         // Audio effect
    }

    /// <summary>
    /// Community notification for discoveries
    /// </summary>
    [Serializable]
    public struct CommunityNotification
    {
        public CommunityNotificationType type;
        public string title;
        public string message;
        public DiscoveryEvent discoveryEvent;
        public float timestamp;
        public bool isGlobal;
    }

    /// <summary>
    /// Types of community notifications
    /// </summary>
    public enum CommunityNotificationType
    {
        MajorDiscovery,     // Major genetic discovery
        WorldFirst,         // World first discovery
        LegendaryFind,      // Legendary creature discovery
        CommunityGoal,      // Community research goal
        BreakthroughAlert   // Scientific breakthrough
    }

    #endregion
}