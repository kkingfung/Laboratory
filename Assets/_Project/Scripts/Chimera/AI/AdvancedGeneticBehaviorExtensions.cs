using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Creatures;
using Laboratory.Core.ECS;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// Extensions for applying genetic behavior modifications to AI and creatures
    /// </summary>
    public static class AdvancedGeneticBehaviorExtensions
    {
        #region Genetic Behavior Application
        
        /// <summary>
        /// Applies genetic modifiers to a ChimeraMonsterAI component
        /// </summary>
        public static void ApplyGeneticBehavior(this ChimeraMonsterAI ai, GeneticProfile genetics)
        {
            if (ai == null || genetics == null) return;
            
            var genes = genetics.Genes;
            
            // Apply aggression modifiers
            var aggressionGene = genes.FirstOrDefault(g => g.traitName == "Aggression" && g.isActive);
            if (aggressionGene.traitName != null && aggressionGene.value.HasValue)
            {
                float aggressionModifier = 0.5f + aggressionGene.value.Value;
                ai.SetGeneticAggressionModifier(aggressionModifier);
            }
            
            // Apply intelligence modifiers
            var intelligenceGene = genes.FirstOrDefault(g => g.traitName == "Intelligence" && g.isActive);
            if (intelligenceGene.traitName != null && intelligenceGene.value.HasValue)
            {
                float detectionModifier = 0.5f + intelligenceGene.value.Value;
                ai.SetGeneticDetectionRangeModifier(detectionModifier);
            }
            
            // Apply loyalty modifiers
            var loyaltyGene = genes.FirstOrDefault(g => g.traitName == "Loyalty" && g.isActive);
            if (loyaltyGene.traitName != null && loyaltyGene.value.HasValue)
            {
                float followDistance = Mathf.Lerp(10f, 2f, loyaltyGene.value.Value);
                ai.SetGeneticFollowDistance(followDistance);
            }
            
            // Apply curiosity modifiers
            var curiosityGene = genes.FirstOrDefault(g => g.traitName == "Curiosity" && g.isActive);
            if (curiosityGene.traitName != null && curiosityGene.value.HasValue)
            {
                float patrolRadius = Mathf.Lerp(5f, 30f, curiosityGene.value.Value);
                ai.SetGeneticPatrolRadius(patrolRadius);
            }
        }
        
        /// <summary>
        /// Calculates behavioral tendency based on genetic profile
        /// </summary>
        public static BehaviorTendency GetBehaviorTendency(this GeneticProfile genetics)
        {
            if (genetics?.Genes == null) return new BehaviorTendency();
            
            var tendency = new BehaviorTendency();
            var genes = genetics.Genes;
            
            foreach (var gene in genes.Where(g => g.isActive && g.value.HasValue))
            {
                switch (gene.traitName)
                {
                    case "Aggression":
                        tendency.Aggression = gene.value.Value;
                        break;
                    case "Intelligence":
                        tendency.Intelligence = gene.value.Value;
                        break;
                    case "Social":
                        tendency.Sociability = gene.value.Value;
                        break;
                    case "Curiosity":
                        tendency.Curiosity = gene.value.Value;
                        break;
                    case "Loyalty":
                        tendency.Loyalty = gene.value.Value;
                        break;
                    case "Speed":
                        tendency.ActivityLevel = gene.value.Value;
                        break;
                }
            }
            
            return tendency;
        }
        
        /// <summary>
        /// Applies genetic modifiers to creature movement
        /// </summary>
        public static void ApplyMovementGenetics(this UnityEngine.AI.NavMeshAgent agent, GeneticProfile genetics)
        {
            if (agent == null || genetics?.Genes == null) return;
            
            var speedGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Speed" && g.isActive);
            if (speedGene.traitName != null && speedGene.value.HasValue)
            {
                float speedModifier = 0.5f + speedGene.value.Value;
                agent.speed *= speedModifier;
                agent.acceleration *= speedModifier;
            }
            
            var agilityGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Agility" && g.isActive);
            if (agilityGene.traitName != null && agilityGene.value.HasValue)
            {
                float agilityModifier = 0.5f + agilityGene.value.Value;
                agent.angularSpeed *= agilityModifier;
            }
        }
        
        /// <summary>
        /// Gets recommended AI state based on genetic profile
        /// </summary>
        public static AIBehaviorState GetRecommendedIdleBehavior(this GeneticProfile genetics)
        {
            if (genetics?.Genes == null) return AIBehaviorState.Idle;
            
            var tendency = genetics.GetBehaviorTendency();
            
            // High curiosity creatures prefer patrolling
            if (tendency.Curiosity > 0.7f)
                return AIBehaviorState.Patrol;
                
            // High social creatures prefer following
            if (tendency.Sociability > 0.8f)
                return AIBehaviorState.Follow;
                
            // High aggression creatures prefer guarding
            if (tendency.Aggression > 0.8f)
                return AIBehaviorState.Guard;
                
            return AIBehaviorState.Idle;
        }
        
        #endregion
        
        #region Genetic Compatibility for AI
        
        /// <summary>
        /// Calculates how well two creatures might get along based on genetics
        /// </summary>
        public static float CalculateSocialCompatibility(GeneticProfile genetics1, GeneticProfile genetics2)
        {
            if (genetics1?.Genes == null || genetics2?.Genes == null) return 0.5f;
            
            var tendency1 = genetics1.GetBehaviorTendency();
            var tendency2 = genetics2.GetBehaviorTendency();
            
            float compatibility = 1f;
            
            // High aggression creatures don't get along well with other high aggression creatures
            if (tendency1.Aggression > 0.7f && tendency2.Aggression > 0.7f)
                compatibility -= 0.3f;
                
            // Social creatures get along well with others
            if (tendency1.Sociability > 0.6f && tendency2.Sociability > 0.6f)
                compatibility += 0.2f;
                
            // Loyal creatures get along with non-aggressive creatures
            if (tendency1.Loyalty > 0.7f && tendency2.Aggression < 0.3f)
                compatibility += 0.1f;
                
            return Mathf.Clamp01(compatibility);
        }
        
        /// <summary>
        /// Determines if a creature should be afraid of another based on genetics
        /// </summary>
        public static bool ShouldFear(this GeneticProfile fearfulCreature, GeneticProfile potentialThreat)
        {
            if (fearfulCreature?.Genes == null || potentialThreat?.Genes == null) return false;
            
            var fearfulTendency = fearfulCreature.GetBehaviorTendency();
            var threatTendency = potentialThreat.GetBehaviorTendency();
            
            // Low aggression creatures fear high aggression creatures
            if (fearfulTendency.Aggression < 0.3f && threatTendency.Aggression > 0.7f)
                return true;
                
            // High intelligence creatures can assess threats better
            if (fearfulTendency.Intelligence > 0.8f && threatTendency.Aggression > 0.6f)
                return true;
                
            return false;
        }
        
        #endregion
        
        #region Debug and Utility
        
        /// <summary>
        /// Gets a human-readable description of behavioral traits
        /// </summary>
        public static string GetBehaviorDescription(this GeneticProfile genetics)
        {
            if (genetics?.Genes == null) return "Unknown behavior";
            
            var tendency = genetics.GetBehaviorTendency();
            var traits = new List<string>();
            
            if (tendency.Aggression > 0.7f) traits.Add("Aggressive");
            else if (tendency.Aggression < 0.3f) traits.Add("Peaceful");
            
            if (tendency.Intelligence > 0.7f) traits.Add("Intelligent");
            if (tendency.Sociability > 0.7f) traits.Add("Social");
            if (tendency.Curiosity > 0.7f) traits.Add("Curious");
            if (tendency.Loyalty > 0.7f) traits.Add("Loyal");
            if (tendency.ActivityLevel > 0.7f) traits.Add("Energetic");
            
            return traits.Count > 0 ? string.Join(", ", traits) : "Balanced";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Data structure representing a creature's behavioral tendencies
    /// </summary>
    [System.Serializable]
    public struct BehaviorTendency
    {
        public float Aggression;
        public float Intelligence;
        public float Sociability;
        public float Curiosity;
        public float Loyalty;
        public float ActivityLevel;
        
        public BehaviorTendency(float aggression = 0.5f, float intelligence = 0.5f, float sociability = 0.5f, 
                               float curiosity = 0.5f, float loyalty = 0.5f, float activityLevel = 0.5f)
        {
            Aggression = Mathf.Clamp01(aggression);
            Intelligence = Mathf.Clamp01(intelligence);
            Sociability = Mathf.Clamp01(sociability);
            Curiosity = Mathf.Clamp01(curiosity);
            Loyalty = Mathf.Clamp01(loyalty);
            ActivityLevel = Mathf.Clamp01(activityLevel);
        }
        
        public override string ToString()
        {
            return $"Aggression: {Aggression:P0}, Intelligence: {Intelligence:P0}, Social: {Sociability:P0}, " +
                   $"Curiosity: {Curiosity:P0}, Loyalty: {Loyalty:P0}, Activity: {ActivityLevel:P0}";
        }
    }
}
