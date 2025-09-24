using UnityEngine;
using Laboratory.Chimera;
using Laboratory.Chimera.Genetics;
using System.Linq;

namespace Laboratory.Chimera.Visuals
{
    /// <summary>
    /// Integration component that connects the ProceduralVisualSystem 
    /// to your existing CreatureInstanceComponent system.
    /// 
    /// Add this to creatures that already have CreatureInstanceComponent
    /// to enable advanced genetic visuals.
    /// </summary>
    [RequireComponent(typeof(CreatureInstanceComponent))]
    [RequireComponent(typeof(ProceduralVisualSystem))]
    public class GeneticVisualIntegration : MonoBehaviour
    {
        [Header("Integration Settings")]
        [SerializeField] private bool autoApplyOnStart = true;
        [SerializeField] private bool updateOnGeneticChanges = true;
        [SerializeField] private bool enableDebugLogs = false;
        
        private CreatureInstanceComponent creatureInstance;
        private ProceduralVisualSystem visualSystem;
        private GeneticProfile lastKnownGenetics;
        
        private void Awake()
        {
            creatureInstance = GetComponent<CreatureInstanceComponent>();
            visualSystem = GetComponent<ProceduralVisualSystem>();
            
            if (creatureInstance == null)
            {
                Debug.LogError($"{name}: GeneticVisualIntegration requires CreatureInstanceComponent!");
            }
            
            if (visualSystem == null)
            {
                Debug.LogError($"{name}: GeneticVisualIntegration requires ProceduralVisualSystem!");
            }
        }
        
        private void Start()
        {
            if (autoApplyOnStart)
            {
                ApplyGeneticVisualsWithDelay();
            }
        }
        
        private void Update()
        {
            if (updateOnGeneticChanges && HasGeneticsChanged())
            {
                ApplyCurrentGenetics();
            }
        }
        
        /// <summary>
        /// Apply genetic visuals with a small delay to ensure creature is fully initialized
        /// </summary>
        private void ApplyGeneticVisualsWithDelay()
        {
            Invoke(nameof(ApplyCurrentGenetics), 0.1f);
        }
        
        /// <summary>
        /// Apply genetic visuals based on current creature data
        /// </summary>
        [ContextMenu("Apply Genetic Visuals")]
        public void ApplyCurrentGenetics()
        {
            if (creatureInstance?.CreatureData?.GeneticProfile == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"{name}: No genetic data available for visual application");
                return;
            }
            
            var genetics = creatureInstance.CreatureData.GeneticProfile;
            
            if (visualSystem != null)
            {
                visualSystem.ApplyGeneticVisuals(genetics);
                lastKnownGenetics = genetics;
                
                if (enableDebugLogs)
                    Debug.Log($"✅ Applied genetic visuals to {name} - Gen {genetics.Generation}, Purity: {genetics.GetGeneticPurity():P1}");
            }
        }
        
        /// <summary>
        /// Check if the genetics have changed since last application
        /// </summary>
        private bool HasGeneticsChanged()
        {
            if (creatureInstance?.CreatureData?.GeneticProfile == null) return false;
            
            var currentGenetics = creatureInstance.CreatureData.GeneticProfile;
            
            // Simple change detection - could be made more sophisticated
            return lastKnownGenetics == null || 
                   lastKnownGenetics.Generation != currentGenetics.Generation ||
                   lastKnownGenetics.Genes.Count != currentGenetics.Genes.Count;
        }
        
        /// <summary>
        /// Force refresh of all genetic visuals
        /// </summary>
        [ContextMenu("Force Refresh Visuals")]
        public void ForceRefreshVisuals()
        {
            lastKnownGenetics = null;
            ApplyCurrentGenetics();
        }
        
        /// <summary>
        /// Get debug info about current genetic visual state
        /// </summary>
        public string GetVisualDebugInfo()
        {
            if (creatureInstance?.CreatureData?.GeneticProfile == null)
                return "No genetic data available";
            
            var genetics = creatureInstance.CreatureData.GeneticProfile;
            var info = $"🎨 Genetic Visual Debug Info:\n";
            info += $"Generation: {genetics.Generation}\n";
            info += $"Genetic Purity: {genetics.GetGeneticPurity():P1}\n";
            info += $"Active Genes: {genetics.Genes.Count(g => g.isActive)}\n";
            info += $"Mutations: {genetics.Mutations.Count}\n";
            info += $"Dominant Traits: {genetics.GetTraitSummary(3)}\n";
            
            return info;
        }
    }
}