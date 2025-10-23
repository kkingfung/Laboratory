using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SocialTypes = Laboratory.Chimera.Social.Types;

namespace Laboratory.Chimera.Social.Systems
{
    /// <summary>
    /// Cultural trait evolution and innovation system
    /// </summary>
    public class CulturalEvolutionSystem : MonoBehaviour
    {
        [Header("Cultural Configuration")]
        [SerializeField] private float culturalTransmissionRate = 0.3f;
        [SerializeField] private float innovationRate = 0.01f;
        [SerializeField] private bool enableCulturalEvolution = true;
        [SerializeField] private float culturalDriftRate = 0.001f;

        private List<Laboratory.Chimera.Social.Data.CulturalTrait> globalCulture = new();
        private Dictionary<string, CulturalNorm> establishedNorms = new();
        private List<Innovation> culturalInnovations = new();
        private CulturalEvolutionEngine cultureEngine;

        public event Action<Laboratory.Chimera.Social.Data.CulturalTrait> OnCulturalInnovation;
        public event Action<Laboratory.Chimera.Social.Data.CulturalTrait> OnCulturalTraitSpread;
        public event Action<string, CulturalNorm> OnNormEstablished;

        private void Awake()
        {
            cultureEngine = new CulturalEvolutionEngine(culturalTransmissionRate, innovationRate);
            InitializeBaseCulture();
        }

        private void InitializeBaseCulture()
        {
            var baseCulturalTraits = new[]
            {
                new Laboratory.Chimera.Social.Data.CulturalTrait
                {
                    TraitName = "Cooperation",
                    Description = "Tendency to work together for mutual benefit",
                    Prevalence = 0.7f,
                    TransmissionRate = 0.4f,
                    EmergenceDate = DateTime.UtcNow
                },
                new Laboratory.Chimera.Social.Data.CulturalTrait
                {
                    TraitName = "Hierarchy Respect",
                    Description = "Acknowledgment of social ranking and leadership",
                    Prevalence = 0.5f,
                    TransmissionRate = 0.3f,
                    EmergenceDate = DateTime.UtcNow
                },
                new Laboratory.Chimera.Social.Data.CulturalTrait
                {
                    TraitName = "Knowledge Sharing",
                    Description = "Willingness to share information and skills",
                    Prevalence = 0.6f,
                    TransmissionRate = 0.5f,
                    EmergenceDate = DateTime.UtcNow
                }
            };

            globalCulture.AddRange(baseCulturalTraits);
        }

        public void ProcessCulturalInteraction(uint agent1Id, uint agent2Id, SocialTypes.InteractionType interactionType, SocialTypes.InteractionOutcome outcome)
        {
            if (!enableCulturalEvolution) return;

            var transmissionSuccess = cultureEngine.ProcessInteraction(agent1Id, agent2Id, interactionType, outcome);

            if (transmissionSuccess && UnityEngine.Random.value < culturalTransmissionRate)
            {
                TransmitCulturalTraits(agent1Id, agent2Id);
            }

            // Check for innovation emergence
            if (outcome == SocialTypes.InteractionOutcome.Transformative && UnityEngine.Random.value < innovationRate)
            {
                GenerateInnovation(agent1Id, agent2Id, interactionType);
            }
        }

        private void TransmitCulturalTraits(uint fromAgent, uint toAgent)
        {
            // Select a random cultural trait to potentially transmit
            if (globalCulture.Count == 0) return;

            var traitToTransmit = globalCulture[UnityEngine.Random.Range(0, globalCulture.Count)];
            float transmissionProbability = traitToTransmit.TransmissionRate * culturalTransmissionRate;

            if (UnityEngine.Random.value < transmissionProbability)
            {
                // Successful cultural transmission
                traitToTransmit.Prevalence += 0.01f;
                traitToTransmit.Prevalence = Mathf.Clamp01(traitToTransmit.Prevalence);

                OnCulturalTraitSpread?.Invoke(traitToTransmit);
                UnityEngine.Debug.Log($"Cultural trait '{traitToTransmit.TraitName}' transmitted from {fromAgent} to {toAgent}");
            }
        }

        private void GenerateInnovation(uint innovator1, uint innovator2, SocialTypes.InteractionType context)
        {
            var innovation = new Innovation
            {
                InnovationId = Guid.NewGuid().ToString(),
                Name = GenerateInnovationName(context),
                Description = GenerateInnovationDescription(context),
                InnovatorIds = new List<uint> { innovator1, innovator2 },
                Context = context.ToString(),
                EmergenceDate = DateTime.UtcNow,
                AdoptionRate = 0.05f,
                CurrentAdopters = 2
            };

            culturalInnovations.Add(innovation);

            // Create corresponding cultural trait
            var newTrait = new Laboratory.Chimera.Social.Data.CulturalTrait
            {
                TraitName = innovation.Name,
                Description = innovation.Description,
                Prevalence = 0.01f, // Start with low prevalence
                TransmissionRate = innovation.AdoptionRate,
                EmergenceDate = DateTime.UtcNow
            };

            globalCulture.Add(newTrait);

            OnCulturalInnovation?.Invoke(newTrait);
            UnityEngine.Debug.Log($"Cultural innovation emerged: {innovation.Name}");
        }

        private string GenerateInnovationName(SocialTypes.InteractionType context)
        {
            var prefixes = new[] { "Enhanced", "Improved", "Collaborative", "Advanced", "Refined" };
            var suffixes = context switch
            {
                SocialTypes.InteractionType.Cooperation => new[] { "Teamwork", "Collaboration", "Unity", "Partnership" },
                SocialTypes.InteractionType.Competition => new[] { "Competition", "Excellence", "Achievement", "Performance" },
                SocialTypes.InteractionType.Conversation => new[] { "Communication", "Dialogue", "Expression", "Understanding" },
                _ => new[] { "Interaction", "Behavior", "Practice", "Method" }
            };

            var prefix = prefixes[UnityEngine.Random.Range(0, prefixes.Length)];
            var suffix = suffixes[UnityEngine.Random.Range(0, suffixes.Length)];

            return $"{prefix} {suffix}";
        }

        private string GenerateInnovationDescription(SocialTypes.InteractionType context)
        {
            return context switch
            {
                SocialTypes.InteractionType.Cooperation => "A new method of cooperative behavior that enhances group effectiveness",
                SocialTypes.InteractionType.Competition => "An innovative competitive strategy that improves individual performance",
                SocialTypes.InteractionType.Conversation => "An advanced communication technique that increases understanding",
                SocialTypes.InteractionType.Conflict => "A novel conflict resolution approach that maintains relationships",
                _ => "A new social behavior that improves interaction outcomes"
            };
        }

        public void UpdateCulturalEvolution()
        {
            if (!enableCulturalEvolution) return;

            // Cultural drift - slow changes over time
            foreach (var trait in globalCulture)
            {
                float drift = (UnityEngine.Random.value - 0.5f) * culturalDriftRate;
                trait.Prevalence += drift;
                trait.Prevalence = Mathf.Clamp01(trait.Prevalence);
            }

            // Remove extinct traits
            var extinctTraits = globalCulture.Where(t => t.Prevalence < 0.01f).ToList();
            foreach (var extinctTrait in extinctTraits)
            {
                globalCulture.Remove(extinctTrait);
                UnityEngine.Debug.Log($"Cultural trait '{extinctTrait.TraitName}' became extinct");
            }

            // Establish norms for highly prevalent traits
            var dominantTraits = globalCulture.Where(t => t.Prevalence > 0.8f && !establishedNorms.ContainsKey(t.TraitName)).ToList();
            foreach (var trait in dominantTraits)
            {
                EstablishNorm(trait);
            }

            // Update innovation adoption
            UpdateInnovationAdoption();
        }

        private void EstablishNorm(Laboratory.Chimera.Social.Data.CulturalTrait trait)
        {
            var norm = new CulturalNorm
            {
                Name = trait.TraitName,
                Description = $"Established norm: {trait.Description}",
                Strength = trait.Prevalence,
                EstablishedDate = DateTime.UtcNow,
                EnforcementLevel = CalculateEnforcementLevel(trait.Prevalence)
            };

            establishedNorms[trait.TraitName] = norm;
            OnNormEstablished?.Invoke(trait.TraitName, norm);
            UnityEngine.Debug.Log($"Cultural norm established: {trait.TraitName}");
        }

        private float CalculateEnforcementLevel(float prevalence)
        {
            return Mathf.Clamp01((prevalence - 0.8f) / 0.2f);
        }

        private void UpdateInnovationAdoption()
        {
            foreach (var innovation in culturalInnovations.ToList())
            {
                // Simulate adoption spread
                if (UnityEngine.Random.value < innovation.AdoptionRate)
                {
                    innovation.CurrentAdopters++;

                    // Update corresponding cultural trait
                    var correspondingTrait = globalCulture.FirstOrDefault(t => t.TraitName == innovation.Name);
                    if (correspondingTrait != null)
                    {
                        correspondingTrait.Prevalence += 0.005f;
                        correspondingTrait.Prevalence = Mathf.Clamp01(correspondingTrait.Prevalence);
                    }
                }

                // Remove old innovations that haven't spread
                if ((DateTime.UtcNow - innovation.EmergenceDate).TotalDays > 30 && innovation.CurrentAdopters < 5)
                {
                    culturalInnovations.Remove(innovation);
                }
            }
        }

        public List<Laboratory.Chimera.Social.Data.CulturalTrait> GetGlobalCulture()
        {
            return new List<Laboratory.Chimera.Social.Data.CulturalTrait>(globalCulture);
        }

        public Dictionary<string, CulturalNorm> GetEstablishedNorms()
        {
            return new Dictionary<string, CulturalNorm>(establishedNorms);
        }

        public List<Innovation> GetActiveInnovations()
        {
            return new List<Innovation>(culturalInnovations);
        }

        public Laboratory.Chimera.Social.Data.CulturalTrait GetCulturalTrait(string traitName)
        {
            return globalCulture.FirstOrDefault(t => t.TraitName == traitName);
        }

    }

    /// <summary>
    /// Cultural evolution engine
    /// </summary>
    public class CulturalEvolutionEngine
    {
        private readonly float transmissionRate;
        private readonly float innovationRate;
        private Dictionary<(uint, uint), List<string>> agentInteractionHistory = new();

        public CulturalEvolutionEngine(float transmissionRate, float innovationRate)
        {
            this.transmissionRate = transmissionRate;
            this.innovationRate = innovationRate;
        }

        public bool ProcessInteraction(uint agent1Id, uint agent2Id, SocialTypes.InteractionType interactionType, SocialTypes.InteractionOutcome outcome)
        {
            var key = agent1Id < agent2Id ? (agent1Id, agent2Id) : (agent2Id, agent1Id);

            if (!agentInteractionHistory.ContainsKey(key))
            {
                agentInteractionHistory[key] = new List<string>();
            }

            agentInteractionHistory[key].Add($"{interactionType}_{outcome}");

            // Return whether cultural transmission should occur
            return outcome == SocialTypes.InteractionOutcome.Positive || outcome == SocialTypes.InteractionOutcome.Transformative;
        }

        public float CalculateInnovationProbability(uint agent1Id, uint agent2Id)
        {
            var key = agent1Id < agent2Id ? (agent1Id, agent2Id) : (agent2Id, agent1Id);

            if (!agentInteractionHistory.TryGetValue(key, out var history))
                return innovationRate;

            // More diverse interactions increase innovation probability
            float diversityBonus = history.Distinct().Count() * 0.01f;
            return innovationRate + diversityBonus;
        }
    }

    /// <summary>
    /// Cultural innovation data structure
    /// </summary>
    [Serializable]
    public class Innovation
    {
        public string InnovationId;
        public string Name;
        public string Description;
        public List<uint> InnovatorIds = new();
        public string Context;
        public DateTime EmergenceDate;
        public float AdoptionRate;
        public int CurrentAdopters;
        public Dictionary<string, object> Properties = new();
    }

    /// <summary>
    /// Cultural norm structure
    /// </summary>
    [Serializable]
    public struct CulturalNorm
    {
        public string Name;
        public string Description;
        public float Strength;
        public DateTime EstablishedDate;
        public float EnforcementLevel;
    }
}