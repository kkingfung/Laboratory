using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Subsystems.Genetics;
using Laboratory.Chimera.Genetics;
using Laboratory.Subsystems.Research;
using Laboratory.Subsystems.Ecosystem;
using Laboratory.Subsystems.Analytics;

namespace Laboratory.Subsystems.AIDirector.Services
{
    /// <summary>
    /// Service responsible for handling game events and integrating them with AI Director systems.
    /// Processes breeding, mutation, discovery, environmental, and educational events.
    /// Extracted from AIDirectorSubsystemManager to improve maintainability.
    /// </summary>
    public class EventHandlerService
    {
        private readonly Dictionary<string, PlayerProfile> _playerProfiles;
        private readonly AIDirectorSubsystemConfig _config;
        private readonly PlayerProfileService _profileService;
        private readonly BehavioralAnalysisService _behavioralService;
        private readonly EducationalContentService _educationalService;
        private readonly DecisionExecutionService _decisionService;

        public EventHandlerService(
            Dictionary<string, PlayerProfile> playerProfiles,
            AIDirectorSubsystemConfig config,
            PlayerProfileService profileService,
            BehavioralAnalysisService behavioralService,
            EducationalContentService educationalService,
            DecisionExecutionService decisionService)
        {
            _playerProfiles = playerProfiles ?? throw new ArgumentNullException(nameof(playerProfiles));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _behavioralService = behavioralService ?? throw new ArgumentNullException(nameof(behavioralService));
            _educationalService = educationalService ?? throw new ArgumentNullException(nameof(educationalService));
            _decisionService = decisionService ?? throw new ArgumentNullException(nameof(decisionService));
        }

        #region Genetics Events

        /// <summary>
        /// Handles breeding event results
        /// </summary>
        public void HandleBreedingEvent(GeneticBreedingResult result)
        {
            if (result.isSuccessful && result.offspring != null)
            {
                _profileService.TrackPlayerAction("CurrentPlayer", "breeding_success", new Dictionary<string, object>
                {
                    ["parent1"] = result.parent1Id,
                    ["parent2"] = result.parent2Id,
                    ["offspring"] = result.offspringId,
                    ["mutationCount"] = result.mutations?.Count ?? 0,
                    ["difficulty"] = CalculateBreedingDifficulty(result)
                });

                // Provide positive reinforcement for successful breeding
                var profile = _profileService.GetOrCreatePlayerProfile("CurrentPlayer");
                _profileService.UpdatePlayerConfidence("CurrentPlayer", true);

                // Consider triggering celebration or educational content
                if (result.mutations?.Count > 0)
                {
                    _educationalService.ConsiderTriggeringMutationExplanation("CurrentPlayer", result.mutations, profile);
                }
            }
        }

        /// <summary>
        /// Handles mutation discovery events
        /// </summary>
        public void HandleMutationEvent(MutationEvent mutationEvent)
        {
            _profileService.TrackPlayerAction("CurrentPlayer", "mutation_discovery", new Dictionary<string, object>
            {
                ["creatureId"] = mutationEvent.creatureId,
                ["mutationType"] = mutationEvent.mutation.mutationType.ToString(),
                ["severity"] = mutationEvent.mutation.severity,
                ["isWorldFirst"] = false // Default value since property doesn't exist
            });

            // Generate educational content for rare mutations
            if (mutationEvent.mutation.severity > 0.7f)
            {
                var profile = _profileService.GetOrCreatePlayerProfile("CurrentPlayer");
                _educationalService.ConsiderTriggeringEducationalContent("CurrentPlayer", "rare_mutation", mutationEvent.mutation, profile);
            }
        }

        /// <summary>
        /// Handles trait discovery events
        /// </summary>
        public void HandleTraitDiscoveryEvent(TraitDiscoveryEvent discoveryEvent)
        {
            _profileService.TrackPlayerAction(discoveryEvent.discoverer ?? "CurrentPlayer", "trait_discovery", new Dictionary<string, object>
            {
                ["traitName"] = discoveryEvent.traitName,
                ["generation"] = discoveryEvent.generation,
                ["isWorldFirst"] = discoveryEvent.isWorldFirst
            });

            // Celebrate world-first discoveries
            if (discoveryEvent.isWorldFirst)
            {
                _decisionService.TriggerCelebrationEvent(discoveryEvent.discoverer ?? "CurrentPlayer", "world_first_trait", discoveryEvent.traitName);
            }
        }

        #endregion

        #region Research Events

        /// <summary>
        /// Handles research discovery events
        /// </summary>
        public void HandleResearchDiscoveryEvent(DiscoveryJournalEvent discoveryEvent)
        {
            _profileService.TrackPlayerAction(discoveryEvent.PlayerId ?? "CurrentPlayer", "research_discovery", new Dictionary<string, object>
            {
                ["discoveryType"] = discoveryEvent.Discovery.discoveryType.ToString(),
                ["isSignificant"] = discoveryEvent.Discovery.isSignificant
            });

            // Track research engagement patterns
            var profile = _profileService.GetOrCreatePlayerProfile(discoveryEvent.PlayerId ?? "CurrentPlayer");
            // Note: profile.researchEngagement property not available - using generic tracking instead
        }

        /// <summary>
        /// Handles publication events
        /// </summary>
        public void HandlePublicationEvent(PublicationEvent publicationEvent)
        {
            _profileService.TrackPlayerAction(publicationEvent.publication.authorId, "publication_created", new Dictionary<string, object>
            {
                ["publicationType"] = publicationEvent.publication.publicationType.ToString(),
                ["collaborators"] = publicationEvent.publication.coAuthors?.Count ?? 0
            });

            // Encourage collaborative research
            if (publicationEvent.publication.coAuthors?.Count > 0)
            {
                _decisionService.EncourageCollaborativeResearch(publicationEvent.publication.authorId);
            }
        }

        #endregion

        #region Environmental Events

        /// <summary>
        /// Handles environmental change events
        /// </summary>
        public void HandleEnvironmentalEvent(EnvironmentalEvent envEvent)
        {
            // Track environmental awareness
            foreach (var playerId in GetActivePlayerIds())
            {
                if (IsPlayerInAffectedArea(playerId, envEvent.affectedBiomes?.FirstOrDefault() ?? envEvent.affectedBiomeId))
                {
                    _profileService.TrackPlayerAction(playerId, "environmental_exposure", new Dictionary<string, object>
                    {
                        ["eventType"] = envEvent.eventType.ToString(),
                        ["severity"] = envEvent.severity
                    });

                    // Provide environmental education if needed
                    var profile = _profileService.GetOrCreatePlayerProfile(playerId);
                    _educationalService.ConsiderEnvironmentalEducation(playerId, envEvent, profile);
                }
            }
        }

        /// <summary>
        /// Handles population change events
        /// </summary>
        public void HandlePopulationEvent(PopulationEvent populationEvent)
        {
            // Track ecosystem management engagement
            _profileService.TrackPlayerAction("EcosystemManager", "population_change", new Dictionary<string, object>
            {
                ["changeType"] = populationEvent.changeType.ToString(),
                ["populationId"] = populationEvent.populationId
            });

            // Update global ecosystem state for all players
            foreach (var playerId in GetActivePlayerIds())
            {
                var profile = _profileService.GetOrCreatePlayerProfile(playerId);
                _educationalService.UpdatePlayerEcosystemAwareness(playerId, populationEvent, profile);
            }
        }

        #endregion

        #region Player Action Events

        /// <summary>
        /// Handles player action events from analytics
        /// </summary>
        public void HandlePlayerActionEvent(PlayerActionEvent actionEvent)
        {
            // Integrate analytics data into AI Director decisions
            var profile = _profileService.GetOrCreatePlayerProfile(actionEvent.playerId);

            // Convert to behavioral analysis format
            var behavioralEvent = new BehavioralAnalysisService.PlayerActionEvent
            {
                playerId = actionEvent.playerId,
                actionType = actionEvent.actionType,
                timestamp = DateTime.Now,
                actionData = new Dictionary<string, object>()
            };

            _behavioralService.AnalyzePlayerActionPattern(profile, behavioralEvent);

            // Detect if player needs guidance
            if (_behavioralService.DetectPlayerStruggle(profile, behavioralEvent))
            {
                _educationalService.ConsiderProvidingGuidance(actionEvent.playerId, actionEvent.actionType);
            }
        }

        /// <summary>
        /// Handles educational progress events
        /// </summary>
        public void HandleEducationalProgressEvent(EducationalProgressEvent progressEvent)
        {
            _profileService.TrackPlayerAction("CurrentPlayer", "educational_progress", new Dictionary<string, object>
            {
                ["lessonId"] = progressEvent.lessonId,
                ["conceptMastered"] = progressEvent.conceptMastered,
                ["confidenceLevel"] = progressEvent.confidenceLevel
            });

            // Adapt content difficulty based on educational progress
            var profile = _profileService.GetOrCreatePlayerProfile("CurrentPlayer");
            _educationalService.AdaptContentDifficulty("CurrentPlayer", progressEvent.confidenceLevel, profile);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculates breeding difficulty based on result complexity
        /// </summary>
        private float CalculateBreedingDifficulty(GeneticBreedingResult result)
        {
            var difficulty = 0.5f; // Base difficulty

            // Increase difficulty based on mutation count
            if (result.mutations?.Count > 0)
                difficulty += result.mutations.Count * 0.1f;

            // Factor in genetic complexity
            if (result.offspring?.Genes?.Count > 10)
                difficulty += 0.2f;

            return Mathf.Clamp(difficulty, 0.1f, 1.0f);
        }

        /// <summary>
        /// Checks if player is in an affected area/biome
        /// </summary>
        private bool IsPlayerInAffectedArea(string playerId, object affectedArea)
        {
            if (string.IsNullOrEmpty(playerId) || affectedArea == null)
                return false;

            // Get player's current biome/location
            var playerCurrentBiome = GetPlayerCurrentBiome(playerId);
            if (string.IsNullOrEmpty(playerCurrentBiome))
                return false; // Player location unknown

            // Handle different types of affected area specifications
            switch (affectedArea)
            {
                case string singleBiome when !string.IsNullOrEmpty(singleBiome):
                    return string.Equals(playerCurrentBiome, singleBiome, StringComparison.OrdinalIgnoreCase);

                case IEnumerable<string> multipleBiomes:
                    return multipleBiomes.Any(biome =>
                        string.Equals(playerCurrentBiome, biome, StringComparison.OrdinalIgnoreCase));

                default:
                    // Try to convert to string as fallback
                    var affectedBiomeStr = affectedArea.ToString();
                    return !string.IsNullOrEmpty(affectedBiomeStr) &&
                           string.Equals(playerCurrentBiome, affectedBiomeStr, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets player's current biome from profile data
        /// </summary>
        private string GetPlayerCurrentBiome(string playerId)
        {
            // Try to get player's current biome from various possible sources
            var profile = _profileService.GetOrCreatePlayerProfile(playerId);
            if (profile.customAttributes.TryGetValue("current_biome", out var biomeObj))
            {
                return biomeObj?.ToString();
            }

            // Check if player focus indicates current biome
            if (!string.IsNullOrEmpty(profile.currentFocus))
            {
                // Extract biome from focus if it contains biome information
                var focusLower = profile.currentFocus.ToLowerInvariant();
                if (focusLower.Contains("forest")) return "Forest";
                if (focusLower.Contains("desert")) return "Desert";
                if (focusLower.Contains("ocean") || focusLower.Contains("marine")) return "Ocean";
                if (focusLower.Contains("mountain") || focusLower.Contains("alpine")) return "Mountain";
                if (focusLower.Contains("grassland") || focusLower.Contains("plains")) return "Grassland";
                if (focusLower.Contains("tundra") || focusLower.Contains("arctic")) return "Tundra";
                if (focusLower.Contains("wetland") || focusLower.Contains("marsh")) return "Wetland";
            }

            // Try to get from interests (player might be interested in certain biomes)
            var biomePreference = profile.interests.FirstOrDefault(interest =>
                interest.EndsWith("_biome") || IsKnownBiomeName(interest));

            if (!string.IsNullOrEmpty(biomePreference))
                return biomePreference.Replace("_biome", "");

            // Default to a common biome if no specific location can be determined
            return GetDefaultPlayerBiome(playerId);
        }

        /// <summary>
        /// Checks if name matches known biome types
        /// </summary>
        private bool IsKnownBiomeName(string name)
        {
            var knownBiomes = new[] { "Forest", "Desert", "Ocean", "Mountain", "Grassland", "Tundra", "Wetland", "Savanna", "Taiga", "Rainforest" };
            return knownBiomes.Any(biome => string.Equals(biome, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets default biome for player based on player ID hash
        /// </summary>
        private string GetDefaultPlayerBiome(string playerId)
        {
            // Use a hash of player ID to consistently assign a default biome
            var playerHash = Math.Abs(playerId.GetHashCode());
            var biomes = new[] { "Forest", "Grassland", "Mountain", "Ocean" };
            return biomes[playerHash % biomes.Length];
        }

        /// <summary>
        /// Gets list of active player IDs
        /// </summary>
        private List<string> GetActivePlayerIds()
        {
            return _playerProfiles.Keys.ToList();
        }

        #endregion
    }
}
