using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.AI;
using Laboratory.Chimera.Breeding;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// MonoBehaviour component for advanced genetic behavior management
    /// </summary>
    public class AdvancedGeneticBehaviorComponent : MonoBehaviour
    {
        [Header("Advanced Genetic Behaviors")]
        [SerializeField] public bool enableAdvancedGeneticBehaviors = true;
        [SerializeField] private bool showPersonalityDebug = false;
        
        [Header("Pack Behavior")]
        [SerializeField] private List<GameObject> packMembers = new List<GameObject>();
        [SerializeField] private float packDetectionRadius = 10f;
        
        [Header("Emotional State")]
        [SerializeField] private float currentMood = 0.5f;
        [SerializeField] private float stressLevel = 0f;
        [SerializeField] private float happinessLevel = 0.5f;
        
        // Component references
        private GeneticBehaviorAdapter geneticAdapter;
        private ChimeraMonsterAI monsterAI;
        private GeneticProfile genetics;
        
        // Personality cache
        private string personalityDescription;
        private bool personalityCalculated = false;
        
        private void Awake()
        {
            geneticAdapter = GetComponent<GeneticBehaviorAdapter>();
            monsterAI = GetComponent<ChimeraMonsterAI>();
        }
        
        private void Start()
        {
            if (enableAdvancedGeneticBehaviors)
            {
                InitializeAdvancedBehaviors();
            }
        }
        
        private void Update()
        {
            if (enableAdvancedGeneticBehaviors)
            {
                UpdatePackBehavior();
                UpdateEmotionalState();
            }
        }
        
        #region Initialization
        
        private void InitializeAdvancedBehaviors()
        {
            var creatureInstance = GetComponent<Laboratory.Chimera.CreatureInstanceComponent>();
            if (creatureInstance?.Instance?.GeneticProfile != null)
            {
                genetics = creatureInstance.Instance.GeneticProfile;
                CalculatePersonality();
                ApplyGeneticPersonalityToAI();
            }
        }
        
        private void CalculatePersonality()
        {
            if (genetics == null) return;
            
            var traits = new List<string>();
            var genes = genetics.Genes;
            
            foreach (var gene in genes.Where(g => g.isActive && g.value.HasValue))
            {
                switch (gene.traitName)
                {
                    case "Aggression" when gene.value > 0.7f:
                        traits.Add("Aggressive");
                        break;
                    case "Aggression" when gene.value < 0.3f:
                        traits.Add("Peaceful");
                        break;
                    case "Intelligence" when gene.value > 0.7f:
                        traits.Add("Intelligent");
                        break;
                    case "Curiosity" when gene.value > 0.7f:
                        traits.Add("Curious");
                        break;
                    case "Loyalty" when gene.value > 0.7f:
                        traits.Add("Loyal");
                        break;
                    case "Social" when gene.value > 0.7f:
                        traits.Add("Social");
                        break;
                    case "Playfulness" when gene.value > 0.7f:
                        traits.Add("Playful");
                        break;
                }
            }
            
            personalityDescription = traits.Count > 0 ? string.Join(", ", traits) : "Balanced";
            personalityCalculated = true;
            
            if (showPersonalityDebug)
            {
                UnityEngine.Debug.Log($"[{gameObject.name}] Personality: {personalityDescription}");
            }
        }
        
        private void ApplyGeneticPersonalityToAI()
        {
            if (genetics == null || monsterAI == null) return;
            
            // Apply genetic behavior using the static extension methods
            monsterAI.ApplyGeneticBehavior(genetics);
        }
        
        #endregion
        
        #region Pack Behavior
        
        private void UpdatePackBehavior()
        {
            if (Time.frameCount % 60 == 0) // Check every second
            {
                UpdatePackMembers();
            }
        }
        
        private void UpdatePackMembers()
        {
            packMembers.Clear();
            
            var nearbyCreatures = Physics.OverlapSphere(transform.position, packDetectionRadius);
            
            foreach (var collider in nearbyCreatures)
            {
                if (collider.gameObject == gameObject) continue;
                
                var otherAdvancedBehavior = collider.GetComponent<AdvancedGeneticBehaviorComponent>();
                if (otherAdvancedBehavior != null)
                {
                    // Check genetic compatibility
                    float compatibility = CalculateCompatibility(otherAdvancedBehavior);
                    if (compatibility > 0.6f)
                    {
                        packMembers.Add(collider.gameObject);
                    }
                }
            }
        }
        
        private float CalculateCompatibility(AdvancedGeneticBehaviorComponent other)
        {
            if (genetics == null || other.genetics == null) return 0.5f;
            
            return AdvancedGeneticBehaviorExtensions.CalculateSocialCompatibility(genetics, other.genetics);
        }
        
        #endregion
        
        #region Emotional State
        
        private void UpdateEmotionalState()
        {
            // Update emotional state based on recent events and genetics
            UpdateMoodFromGenetics();
            UpdateStressFromEnvironment();
            UpdateHappinessFromSocialInteractions();
        }
        
        private void UpdateMoodFromGenetics()
        {
            if (genetics == null) return;
            
            var stabilityGene = genetics.Genes.FirstOrDefault(g => g.traitName == "EmotionalStability" && g.isActive);
            if (!string.IsNullOrEmpty(stabilityGene.traitName) && stabilityGene.value.HasValue)
            {
                // More stable creatures have less mood swings
                float targetMood = 0.5f;
                float lerpSpeed = Time.deltaTime * stabilityGene.value.Value;
                currentMood = Mathf.Lerp(currentMood, targetMood, lerpSpeed);
            }
        }
        
        private void UpdateStressFromEnvironment()
        {
            // Check for stressful situations
            bool hasNearbyThreats = false;
            
            var nearbyObjects = Physics.OverlapSphere(transform.position, 5f);
            foreach (var obj in nearbyObjects)
            {
                if (obj.gameObject != gameObject && genetics != null)
                {
                    var otherBehavior = obj.GetComponent<AdvancedGeneticBehaviorComponent>();
                    if (otherBehavior?.genetics != null)
                    {
                        if (genetics.ShouldFear(otherBehavior.genetics))
                        {
                            hasNearbyThreats = true;
                            break;
                        }
                    }
                }
            }
            
            // Adjust stress level
            float targetStress = hasNearbyThreats ? 0.8f : 0.2f;
            stressLevel = Mathf.Lerp(stressLevel, targetStress, Time.deltaTime);
        }
        
        private void UpdateHappinessFromSocialInteractions()
        {
            if (genetics?.Genes != null)
            {
                var socialGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Social" && g.isActive);
                if (!string.IsNullOrEmpty(socialGene.traitName) && socialGene.value.HasValue)
                {
                    // Social creatures are happier when around pack members
                    float socialHappiness = socialGene.value.Value * (packMembers.Count * 0.1f);
                    float targetHappiness = Mathf.Clamp01(0.5f + socialHappiness);
                    happinessLevel = Mathf.Lerp(happinessLevel, targetHappiness, Time.deltaTime * 0.5f);
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        public string GetPersonalityDescription()
        {
            if (!personalityCalculated)
            {
                CalculatePersonality();
            }
            return personalityDescription ?? "Unknown";
        }
        
        public List<GameObject> GetPackMembers()
        {
            return new List<GameObject>(packMembers);
        }
        
        public float GetMood() => currentMood;
        public float GetStress() => stressLevel;
        public float GetHappiness() => happinessLevel;
        
        public string GetEmotionalState()
        {
            if (stressLevel > 0.7f) return "Stressed";
            if (happinessLevel > 0.7f) return "Happy";
            if (currentMood > 0.6f) return "Content";
            if (currentMood < 0.4f) return "Melancholy";
            return "Neutral";
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            // Draw pack detection radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, packDetectionRadius);
            
            // Draw connections to pack members
            Gizmos.color = Color.green;
            foreach (var member in packMembers)
            {
                if (member != null)
                {
                    Gizmos.DrawLine(transform.position, member.transform.position);
                }
            }
        }
        
        #endregion
    }
}
