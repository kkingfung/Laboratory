using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Service for managing communication systems and language evolution.
    /// Handles communication profiles, language learning, and vocabulary development.
    /// Extracted from AdvancedSocialSystem for single responsibility.
    /// </summary>
    public class CommunicationService
    {
        private readonly bool _enableLanguageEvolution;
        private readonly int _maxVocabularySize;
        private readonly float _communicationEfficiency;
        private readonly LanguageEvolutionEngine _languageEngine;
        private readonly List<CommunicationEvent> _recentCommunications;

        public CommunicationService(
            bool enableLanguageEvolution,
            int maxVocabularySize,
            float communicationEfficiency)
        {
            _enableLanguageEvolution = enableLanguageEvolution;
            _maxVocabularySize = maxVocabularySize;
            _communicationEfficiency = communicationEfficiency;
            _languageEngine = new LanguageEvolutionEngine(maxVocabularySize);
            _recentCommunications = new List<CommunicationEvent>();
        }

        /// <summary>
        /// Creates communication profile based on personality and social traits
        /// </summary>
        public CommunicationProfile CreateCommunicationProfile(
            Dictionary<string, float> personality,
            Dictionary<string, float> socialTraits)
        {
            return new CommunicationProfile
            {
                verbosity = personality.GetValueOrDefault("Extraversion", 0.5f),
                directness = personality.GetValueOrDefault("Assertiveness", 0.5f),
                emotionalExpressiveness = socialTraits.GetValueOrDefault("Emotional_Expression", 0.5f),
                listeningSkill = socialTraits.GetValueOrDefault("Active_Listening", 0.5f),
                nonVerbalSensitivity = socialTraits.GetValueOrDefault("Body_Language_Reading", 0.5f)
            };
        }

        /// <summary>
        /// Determines communication style based on personality traits
        /// </summary>
        public CommunicationStyle DetermineCommunicationStyle(Dictionary<string, float> personality)
        {
            float extraversion = personality.GetValueOrDefault("Extraversion", 0.5f);
            float agreeableness = personality.GetValueOrDefault("Agreeableness", 0.5f);

            if (extraversion > 0.7f && agreeableness > 0.6f)
                return CommunicationStyle.Expressive;
            else if (extraversion < 0.3f && agreeableness > 0.6f)
                return CommunicationStyle.Analytical;
            else if (extraversion > 0.6f && agreeableness < 0.4f)
                return CommunicationStyle.Driver;
            else
                return CommunicationStyle.Amiable;
        }

        /// <summary>
        /// Creates communication system for an agent
        /// </summary>
        public CommunicationSystem CreateCommunicationSystem(Dictionary<string, float> personality)
        {
            return new CommunicationSystem
            {
                vocabulary = new Dictionary<string, float>(),
                languageFamily = "Basic",
                communicationStyle = DetermineCommunicationStyle(personality),
                expressiveness = personality.GetValueOrDefault("Extraversion", 0.5f)
            };
        }

        /// <summary>
        /// Processes communication learning between two agents
        /// </summary>
        public void ProcessCommunicationLearning(
            uint agentA,
            uint agentB,
            SocialInteraction interaction,
            Dictionary<uint, CommunicationSystem> communicationSystems)
        {
            if (!_enableLanguageEvolution) return;

            if (interaction.success > 0.6f)
            {
                _languageEngine.ProcessLearning(
                    agentA,
                    agentB,
                    communicationSystems[agentA],
                    communicationSystems[agentB]);
            }
        }

        /// <summary>
        /// Calculates communication improvement from interaction
        /// </summary>
        public float CalculateCommunicationImprovement(SocialInteraction interaction)
        {
            if (interaction.success > 0.7f)
                return 0.05f * interaction.success;

            return 0f;
        }

        /// <summary>
        /// Records a communication event
        /// </summary>
        public void RecordCommunicationEvent(uint sender, uint receiver, string message, float effectiveness)
        {
            var communicationEvent = new CommunicationEvent
            {
                sender = sender,
                receiver = receiver,
                message = message,
                timestamp = Time.time,
                effectiveness = effectiveness
            };

            _recentCommunications.Add(communicationEvent);

            // Keep recent communications manageable
            if (_recentCommunications.Count > 1000)
            {
                _recentCommunications.RemoveAt(0);
            }
        }

        /// <summary>
        /// Analyzes communication patterns across all agents
        /// </summary>
        public CommunicationAnalysis AnalyzeCommunicationPatterns(
            Dictionary<uint, CommunicationSystem> communicationSystems)
        {
            return new CommunicationAnalysis
            {
                averageVocabularySize = communicationSystems.Count > 0
                    ? (float)communicationSystems.Values.Average(c => c.vocabulary.Count)
                    : 0f,
                communicationEfficiency = _communicationEfficiency,
                languageDiversity = CalculateLanguageDiversity(communicationSystems),
                communicationFrequency = CalculateCommunicationFrequency(communicationSystems.Count)
            };
        }

        /// <summary>
        /// Calculates language diversity across communication systems
        /// </summary>
        private float CalculateLanguageDiversity(Dictionary<uint, CommunicationSystem> communicationSystems)
        {
            if (communicationSystems.Count == 0) return 0f;

            var languageFamilies = new HashSet<string>();
            foreach (var system in communicationSystems.Values)
            {
                languageFamilies.Add(system.languageFamily);
            }

            return languageFamilies.Count / (float)UnityEngine.Mathf.Max(1, communicationSystems.Count);
        }

        /// <summary>
        /// Calculates communication frequency per agent
        /// </summary>
        private float CalculateCommunicationFrequency(int agentCount)
        {
            return _recentCommunications.Count / UnityEngine.Mathf.Max(1f, agentCount);
        }
    }
}
