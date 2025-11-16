using UnityEngine;
using Laboratory.Chimera;
using Laboratory.Chimera.AI;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Creatures;

namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// Integration script to connect your existing GeneticBehaviorAdapter 
    /// with the new AdvancedGeneticBehaviorExtensions system.
    /// 
    /// Add this to any creature that already has GeneticBehaviorAdapter.
    /// </summary>
    [RequireComponent(typeof(GeneticBehaviorAdapter))]
    public class GeneticBehaviorIntegration : MonoBehaviour
    {
        [Header("🔗 Integration Settings")]
        [SerializeField] private bool enableIntegration = true;
        [SerializeField] private bool showIntegrationDebug = false;
        
        // Component references
        private GeneticBehaviorAdapter basicAdapter;
        private ChimeraMonsterAI monsterAI;
        private Laboratory.Chimera.CreatureInstanceComponent creatureInstanceComponent;

        private void Awake()
        {
            InitializeIntegration();
        }

        private void Start()
        {
            if (enableIntegration)
            {
                ConnectSystems();
                if (showIntegrationDebug)
                {
                    LogIntegrationStatus();
                }
            }
        }

        private void InitializeIntegration()
        {
            basicAdapter = GetComponent<GeneticBehaviorAdapter>();
            monsterAI = GetComponent<ChimeraMonsterAI>();
            creatureInstanceComponent = GetComponent<Laboratory.Chimera.CreatureInstanceComponent>();

            if (basicAdapter == null || monsterAI == null)
            {
                UnityEngine.Debug.LogError($"[GeneticIntegration] Missing required components on {gameObject.name}");
                enabled = false;
            }
        }

        private void ConnectSystems()
        {
            // Apply genetic behaviors using the static extension methods
            if (creatureInstanceComponent?.Instance?.GeneticProfile != null)
            {
                // Apply genetic behavior to AI using extension methods
                monsterAI.ApplyGeneticBehavior(creatureInstanceComponent.Instance.GeneticProfile);

                // Apply movement genetics if NavMeshAgent exists
                var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    navAgent.ApplyMovementGenetics(creatureInstanceComponent.Instance.GeneticProfile);
                }

                UnityEngine.Debug.Log($"[GeneticIntegration] Applied genetic behaviors to {gameObject.name}: {creatureInstanceComponent.Instance.GeneticProfile.GetBehaviorDescription()}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[GeneticIntegration] No genetic profile found on {gameObject.name}");
            }
        }

        private void LogIntegrationStatus()
        {
            if (creatureInstanceComponent?.Instance?.GeneticProfile == null) return;

            var genetics = creatureInstanceComponent.Instance.GeneticProfile;
            var behaviorTendency = genetics.GetBehaviorTendency();
            var behaviorDescription = genetics.GetBehaviorDescription();
            var recommendedBehavior = genetics.GetRecommendedIdleBehavior();

            UnityEngine.Debug.Log($"[GeneticIntegration] {gameObject.name} Status:\n" +
                     $"Behavior Profile: {behaviorDescription}\n" +
                     $"Recommended State: {recommendedBehavior}\n" +
                     $"Tendencies: {behaviorTendency}\n" +
                     $"Generation: {genetics.Generation}");
        }

        /// <summary>
        /// Get a comprehensive behavior report for UI display
        /// </summary>
        public string GetComprehensiveBehaviorReport()
        {
            if (creatureInstanceComponent?.Instance?.GeneticProfile == null) return "No genetic profile available";

            var genetics = creatureInstanceComponent.Instance.GeneticProfile;
            var behaviorDescription = genetics.GetBehaviorDescription();
            var behaviorTendency = genetics.GetBehaviorTendency();
            var recommendedBehavior = genetics.GetRecommendedIdleBehavior();

            return $"🧬 Genetics: {behaviorDescription}\n" +
                   $"📊 Aggression: {behaviorTendency.Aggression:P0}, Intelligence: {behaviorTendency.Intelligence:P0}\n" +
                   $"🤝 Social: {behaviorTendency.Sociability:P0}, Curiosity: {behaviorTendency.Curiosity:P0}\n" +
                   $"💖 Loyalty: {behaviorTendency.Loyalty:P0}, Activity: {behaviorTendency.ActivityLevel:P0}\n" +
                   $"🎯 Current State: {monsterAI.CurrentState}\n" +
                   $"💡 Recommended: {recommendedBehavior}";
        }
    }
}
