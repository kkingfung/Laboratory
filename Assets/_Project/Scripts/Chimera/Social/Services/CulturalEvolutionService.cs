using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Service for managing cultural evolution, transmission, and innovation.
    /// Handles cultural trait exchange, innovation generation, and cultural diversity analysis.
    /// Extracted from AdvancedSocialSystem for single responsibility.
    /// </summary>
    public class CulturalEvolutionService
    {
        private readonly bool _enableCulturalEvolution;
        private readonly float _culturalTransmissionRate;
        private readonly float _innovationRate;
        private readonly CulturalEvolutionEngine _cultureEngine;
        private readonly Action<CulturalTrait> _onCulturalInnovation;
        private readonly List<Innovation> _culturalInnovations;

        public CulturalEvolutionService(
            bool enableCulturalEvolution,
            float culturalTransmissionRate,
            float innovationRate,
            Action<CulturalTrait> onCulturalInnovation)
        {
            _enableCulturalEvolution = enableCulturalEvolution;
            _culturalTransmissionRate = culturalTransmissionRate;
            _innovationRate = innovationRate;
            _onCulturalInnovation = onCulturalInnovation;
            _cultureEngine = new CulturalEvolutionEngine(culturalTransmissionRate, innovationRate);
            _culturalInnovations = new List<Innovation>();
        }

        /// <summary>
        /// Initializes basic cultural traits for a new social agent
        /// </summary>
        public void InitializeBasicCulture(SocialAgent agent)
        {
            // Basic cultural traits every agent starts with
            agent.culturalTraits.AddRange(new[]
            {
                new CulturalTrait
                {
                    name = "Cooperation_Tendency",
                    value = agent.personality.GetValueOrDefault("Agreeableness", 0.5f),
                    stability = 0.7f,
                    transmissionRate = 0.3f
                },
                new CulturalTrait
                {
                    name = "Hierarchy_Respect",
                    value = agent.personality.GetValueOrDefault("Conscientiousness", 0.5f),
                    stability = 0.8f,
                    transmissionRate = 0.4f
                },
                new CulturalTrait
                {
                    name = "Innovation_Openness",
                    value = agent.personality.GetValueOrDefault("Openness", 0.5f),
                    stability = 0.6f,
                    transmissionRate = 0.2f
                }
            });
        }

        /// <summary>
        /// Processes cultural exchange between two agents during interaction
        /// </summary>
        public List<CulturalTrait> ProcessCulturalExchange(
            SocialAgent agentA,
            SocialAgent agentB,
            SocialInteraction interaction)
        {
            var exchangedTraits = new List<CulturalTrait>();

            if (interaction.success < 0.6f) return exchangedTraits;

            // Find traits that can be transmitted
            foreach (var trait in agentB.culturalTraits)
            {
                if (UnityEngine.Random.value < trait.transmissionRate * _culturalTransmissionRate)
                {
                    var existingTrait = agentA.culturalTraits.FirstOrDefault(t => t.name == trait.name);
                    if (existingTrait != null)
                    {
                        // Blend existing trait with new influence
                        existingTrait.value = (existingTrait.value + trait.value) * 0.5f;
                    }
                    else
                    {
                        // Adopt new trait (weakened)
                        agentA.culturalTraits.Add(new CulturalTrait
                        {
                            name = trait.name,
                            value = trait.value * 0.7f, // Weakened adoption
                            stability = trait.stability * 0.8f,
                            transmissionRate = trait.transmissionRate
                        });
                    }

                    exchangedTraits.Add(trait);
                }
            }

            return exchangedTraits;
        }

        /// <summary>
        /// Processes cultural transmission between agents if enabled
        /// </summary>
        public void ProcessCulturalTransmission(SocialAgent agentA, SocialAgent agentB, SocialInteraction interaction)
        {
            if (!_enableCulturalEvolution) return;
            if (interaction.success < 0.5f) return;

            _cultureEngine.ProcessTransmission(agentA, agentB, interaction);
        }

        /// <summary>
        /// Updates cultural evolution for all agents
        /// </summary>
        public void UpdateCulturalEvolution(
            Dictionary<uint, SocialAgent> socialAgents,
            List<CulturalTrait> globalCulture,
            float deltaTime)
        {
            if (!_enableCulturalEvolution) return;

            _cultureEngine.UpdateCulturalEvolution(socialAgents.Values, globalCulture, deltaTime);

            // Check for cultural innovations
            if (UnityEngine.Random.value < _innovationRate * deltaTime)
            {
                var innovation = _cultureEngine.GenerateInnovation(socialAgents.Values.ToList());
                if (innovation != null)
                {
                    _culturalInnovations.Add(innovation);
                    _onCulturalInnovation?.Invoke(innovation.culturalTrait);
                }
            }
        }

        /// <summary>
        /// Calculates cultural diversity across all agents
        /// </summary>
        public float CalculateCulturalDiversity(Dictionary<uint, SocialAgent> socialAgents)
        {
            if (socialAgents.Count == 0) return 0f;

            var allTraits = socialAgents.Values
                .SelectMany(a => a.culturalTraits)
                .GroupBy(t => t.name)
                .ToDictionary(g => g.Key, g => g.Select(t => t.value).ToList());

            float diversity = 0f;
            foreach (var traitValues in allTraits.Values)
            {
                if (traitValues.Count > 1)
                {
                    float variance = CalculateVariance(traitValues);
                    diversity += variance;
                }
            }

            return allTraits.Count > 0 ? diversity / allTraits.Count : 0f;
        }

        /// <summary>
        /// Calculates variance of a list of values
        /// </summary>
        private float CalculateVariance(List<float> values)
        {
            if (values.Count <= 1) return 0f;

            float mean = values.Average();
            float sumSquaredDiffs = values.Sum(v => (v - mean) * (v - mean));

            return sumSquaredDiffs / (values.Count - 1);
        }

        /// <summary>
        /// Analyzes cultural evolution patterns
        /// </summary>
        public CulturalAnalysis AnalyzeCulturalEvolution(Dictionary<uint, SocialAgent> socialAgents, float culturalDiversity)
        {
            return new CulturalAnalysis
            {
                culturalDiversity = culturalDiversity,
                innovationRate = socialAgents.Count > 0
                    ? _culturalInnovations.Count / UnityEngine.Mathf.Max(1f, socialAgents.Count)
                    : 0f,
                traditionalismIndex = CalculateTraditionalism(socialAgents),
                culturalTransmissionEfficiency = CalculateCulturalTransmissionEfficiency(),
                dominantCulturalTraits = IdentifyDominantCulturalTraits(socialAgents)
            };
        }

        /// <summary>
        /// Calculates traditionalism index across agents
        /// </summary>
        private float CalculateTraditionalism(Dictionary<uint, SocialAgent> socialAgents)
        {
            if (socialAgents.Count == 0) return 0f;

            return socialAgents.Values.Average(a => a.culturalTraits.Average(t => t.stability));
        }

        /// <summary>
        /// Calculates cultural transmission efficiency
        /// </summary>
        private float CalculateCulturalTransmissionEfficiency()
        {
            // Simplified calculation based on successful cultural exchanges
            return 0.7f; // Placeholder
        }

        /// <summary>
        /// Identifies dominant cultural traits across population
        /// </summary>
        private List<string> IdentifyDominantCulturalTraits(Dictionary<uint, SocialAgent> socialAgents)
        {
            var traitFrequency = new Dictionary<string, int>();

            foreach (var agent in socialAgents.Values)
            {
                foreach (var trait in agent.culturalTraits)
                {
                    traitFrequency[trait.name] = traitFrequency.GetValueOrDefault(trait.name, 0) + 1;
                }
            }

            return traitFrequency
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Gets list of all cultural innovations
        /// </summary>
        public List<Innovation> GetCulturalInnovations() => _culturalInnovations;
    }
}
