using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;
using Laboratory.Chimera.ECS;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Configuration;
using System.Collections;

namespace Laboratory.Chimera.UI
{
    /// <summary>
    /// Real-time creature inspection UI that displays genetics, personality, and current state.
    /// Attach to a UI Canvas and assign a creature to inspect its live data.
    /// </summary>
    public class CreatureInspectorUI : MonoBehaviour
    {
        [Header("🎯 Target Configuration")]
        [SerializeField] private GameObject targetCreature;
        [SerializeField] private bool autoFindNearestCreature = true;
        [SerializeField] private float autoFindRadius = 10f;
        
        [Header("📊 UI References")]
        [SerializeField] private TextMeshProUGUI creatureNameText;
        [SerializeField] private TextMeshProUGUI speciesText;
        [SerializeField] private TextMeshProUGUI ageText;
        [SerializeField] private TextMeshProUGUI lifeStageText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI behaviorStateText;
        
        [Header("🧬 Genetics Display")]
        [SerializeField] private Slider strengthSlider;
        [SerializeField] private Slider vitalitySlider;
        [SerializeField] private Slider agilitySlider;
        [SerializeField] private Slider resilienceSlider;
        [SerializeField] private Slider intellectSlider;
        [SerializeField] private Slider charmSlider;
        [SerializeField] private TextMeshProUGUI geneticSummaryText;
        [SerializeField] private TextMeshProUGUI lineageText;
        
        [Header("🎭 Personality Display")]
        [SerializeField] private Slider braverySlider;
        [SerializeField] private Slider loyaltySlider;
        [SerializeField] private Slider curiositySlider;
        [SerializeField] private Slider socialSlider;
        [SerializeField] private Slider playfulnessSlider;
        [SerializeField] private TextMeshProUGUI personalityDescText;
        
        [Header("💭 Needs Display")]
        [SerializeField] private Slider hungerSlider;
        [SerializeField] private Slider thirstSlider;
        [SerializeField] private Slider restSlider;
        [SerializeField] private Slider socialNeedSlider;
        [SerializeField] private Slider exerciseSlider;
        [SerializeField] private Slider mentalSlider;
        [SerializeField] private Slider happinessSlider;
        [SerializeField] private Slider stressSlider;
        
        [Header("🤝 Bonding Display")]
        [SerializeField] private Slider bondStrengthSlider;
        [SerializeField] private Slider trustSlider;
        [SerializeField] private Slider obedienceSlider;
        [SerializeField] private TextMeshProUGUI bondingStatusText;
        
        [Header("🌍 Environmental Display")]
        [SerializeField] private TextMeshProUGUI currentBiomeText;
        [SerializeField] private Slider biomeComfortSlider;
        [SerializeField] private Slider adaptationSlider;
        [SerializeField] private TextMeshProUGUI environmentalStressText;
        
        [Header("🔧 Update Settings")]
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private bool enableDebugInfo = false;
        [SerializeField] private bool autoHideWhenNoTarget = true;
        
        // Runtime state
        private EntityManager entityManager;
        private Entity targetEntity = Entity.Null;
        private EnhancedCreatureAuthoring targetAuthoring;
        private CreatureInstance targetInstance;
        private float lastUpdateTime;
        private Coroutine updateCoroutine;
        private CanvasGroup canvasGroup;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        private void Start()
        {
            entityManager = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
            
            if (autoFindNearestCreature && targetCreature == null)
            {
                FindNearestCreature();
            }
            
            if (targetCreature != null)
            {
                SetTargetCreature(targetCreature);
            }
            
            updateCoroutine = StartCoroutine(UpdateUICoroutine());
        }
        
        private void OnEnable()
        {
            if (updateCoroutine == null)
            {
                updateCoroutine = StartCoroutine(UpdateUICoroutine());
            }
        }
        
        private void OnDisable()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }
        }
        
        private void OnDestroy()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Set the creature to inspect
        /// </summary>
        public void SetTargetCreature(GameObject creature)
        {
            if (creature == null)
            {
                ClearTarget();
                return;
            }
            
            targetCreature = creature;
            targetAuthoring = creature.GetComponent<EnhancedCreatureAuthoring>();
            
            if (targetAuthoring != null)
            {
                // Get ECS entity from authoring component
                var linkComponent = creature.GetComponent<EntityLinkComponent>();
                if (linkComponent.InstanceID != 0 && linkComponent.LinkedEntity != Unity.Entities.Entity.Null)
                {
                    targetEntity = linkComponent.LinkedEntity;
                }
                
                // Create creature instance for display
                targetInstance = CreateDisplayInstance();
            }
            
            UpdateVisibility();
            
            if (enableDebugInfo)
            {
                Debug.Log($"[CreatureInspector] Set target: {creature.name}");
            }
        }
        
        /// <summary>
        /// Clear the current target
        /// </summary>
        public void ClearTarget()
        {
            targetCreature = null;
            targetAuthoring = null;
            targetEntity = Entity.Null;
            targetInstance = null;
            
            UpdateVisibility();
        }
        
        /// <summary>
        /// Find and select the nearest creature
        /// </summary>
        [ContextMenu("Find Nearest Creature")]
        public void FindNearestCreature()
        {
            var playerPos = Camera.main?.transform.position ?? transform.position;
            
            var nearestCreature = FindFirstObjectByType<EnhancedCreatureAuthoring>();
            float nearestDistance = float.MaxValue;
            
            var allCreatures = FindObjectsByType<EnhancedCreatureAuthoring>(FindObjectsSortMode.None);
            foreach (var creature in allCreatures)
            {
                float distance = Vector3.Distance(playerPos, creature.transform.position);
                if (distance < nearestDistance && distance <= autoFindRadius)
                {
                    nearestDistance = distance;
                    nearestCreature = creature;
                }
            }
            
            if (nearestCreature != null)
            {
                SetTargetCreature(nearestCreature.gameObject);
            }
        }
        
        /// <summary>
        /// Toggle UI visibility
        /// </summary>
        public void ToggleVisibility()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = canvasGroup.alpha > 0.5f ? 0f : 1f;
                canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.5f;
            }
            else
            {
                gameObject.SetActive(!gameObject.activeSelf);
            }
        }
        
        #endregion
        
        #region UI Updates
        
        private IEnumerator UpdateUICoroutine()
        {
            while (true)
            {
                if (targetCreature != null && (targetAuthoring != null || targetEntity != Entity.Null))
                {
                    UpdateAllDisplays();
                }
                
                yield return new WaitForSeconds(updateInterval);
            }
        }
        
        private void UpdateAllDisplays()
        {
            try
            {
                UpdateBasicInfo();
                UpdateGeneticsDisplay();
                UpdatePersonalityDisplay();
                UpdateNeedsDisplay();
                UpdateBondingDisplay();
                UpdateEnvironmentalDisplay();
            }
            catch (System.Exception ex)
            {
                if (enableDebugInfo)
                {
                    Debug.LogWarning($"[CreatureInspector] Update error: {ex.Message}");
                }
            }
        }
        
        private void UpdateBasicInfo()
        {
            if (targetAuthoring != null)
            {
                SetText(creatureNameText, targetCreature.name);
                SetText(speciesText, GetSpeciesName());
                SetText(ageText, GetAgeString());
                SetText(lifeStageText, GetLifeStageString());
                SetText(healthText, GetHealthString());
                SetText(behaviorStateText, GetBehaviorStateString());
            }
        }
        
        private void UpdateGeneticsDisplay()
        {
            if (entityManager.Exists(targetEntity) && entityManager.HasComponent<CreatureGeneticsComponent>(targetEntity))
            {
                var genetics = entityManager.GetComponentData<CreatureGeneticsComponent>(targetEntity);
                
                SetSlider(strengthSlider, genetics.strengthTrait);
                SetSlider(vitalitySlider, genetics.vitalityTrait);
                SetSlider(agilitySlider, genetics.agilityTrait);
                SetSlider(resilienceSlider, genetics.resilienceTrait);
                SetSlider(intellectSlider, genetics.intellectTrait);
                SetSlider(charmSlider, genetics.charmTrait);
                
                SetText(geneticSummaryText, GetGeneticSummary(genetics));
                SetText(lineageText, $"Generation {genetics.generation} | Lineage: {genetics.lineageId}");
            }
            else if (targetAuthoring != null)
            {
                // Fallback to authoring component data
                var summary = targetAuthoring.GetGeneticSummary();
                SetText(geneticSummaryText, summary);
            }
        }
        
        private void UpdatePersonalityDisplay()
        {
            if (entityManager.Exists(targetEntity) && entityManager.HasComponent<CreaturePersonalityComponent>(targetEntity))
            {
                var personality = entityManager.GetComponentData<CreaturePersonalityComponent>(targetEntity);
                
                SetSlider(braverySlider, personality.bravery);
                SetSlider(loyaltySlider, personality.loyalty);
                SetSlider(curiositySlider, personality.curiosity);
                SetSlider(socialSlider, personality.socialNeed);
                SetSlider(playfulnessSlider, personality.playfulness);
                
                SetText(personalityDescText, GetPersonalityDescription(personality));
            }
            else if (targetAuthoring != null)
            {
                // Fallback to authoring component
                var description = targetAuthoring.GetPersonalityDescription();
                SetText(personalityDescText, description);
            }
        }
        
        private void UpdateNeedsDisplay()
        {
            if (entityManager.Exists(targetEntity) && entityManager.HasComponent<CreatureNeedsComponent>(targetEntity))
            {
                var needs = entityManager.GetComponentData<CreatureNeedsComponent>(targetEntity);
                
                SetSlider(hungerSlider, needs.hunger);
                SetSlider(thirstSlider, needs.thirst);
                SetSlider(restSlider, needs.rest);
                SetSlider(socialNeedSlider, needs.social);
                SetSlider(exerciseSlider, needs.exercise);
                SetSlider(mentalSlider, needs.mental);
                SetSlider(happinessSlider, needs.happiness);
                SetSlider(stressSlider, needs.stress);
                
                // Color code based on values
                UpdateNeedsColors(needs);
            }
        }
        
        private void UpdateBondingDisplay()
        {
            if (entityManager.Exists(targetEntity) && entityManager.HasComponent<CreatureBondingComponent>(targetEntity))
            {
                var bonding = entityManager.GetComponentData<CreatureBondingComponent>(targetEntity);
                
                SetSlider(bondStrengthSlider, bonding.bondStrength);
                SetSlider(trustSlider, bonding.trustLevel);
                SetSlider(obedienceSlider, bonding.obedience);
                
                SetText(bondingStatusText, GetBondingStatus(bonding));
            }
            else
            {
                SetText(bondingStatusText, "No player bond");
            }
        }
        
        private void UpdateEnvironmentalDisplay()
        {
            if (entityManager.Exists(targetEntity))
            {
                if (entityManager.HasComponent<CreatureBiomeComponent>(targetEntity))
                {
                    var biome = entityManager.GetComponentData<CreatureBiomeComponent>(targetEntity);
                    SetText(currentBiomeText, biome.currentBiome.ToString());
                    SetSlider(biomeComfortSlider, biome.biomeComfortLevel);
                    SetSlider(adaptationSlider, biome.adaptationLevel);
                }
                
                if (entityManager.HasComponent<CreatureEnvironmentalComponent>(targetEntity))
                {
                    var env = entityManager.GetComponentData<CreatureEnvironmentalComponent>(targetEntity);
                    SetText(environmentalStressText, $"Environmental Stress: {env.environmentalStress:P0}");
                }
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private void UpdateVisibility()
        {
            bool shouldShow = targetCreature != null || !autoHideWhenNoTarget;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = shouldShow ? 1f : 0f;
                canvasGroup.blocksRaycasts = shouldShow;
            }
            else
            {
                gameObject.SetActive(shouldShow);
            }
        }
        
        private void SetText(TextMeshProUGUI textComponent, string text)
        {
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
        
        private void SetSlider(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.value = Mathf.Clamp01(value);
            }
        }
        
        private CreatureInstance CreateDisplayInstance()
        {
            if (targetAuthoring == null) return null;
            
            // This would need to be implemented to extract data from the authoring component
            // For now, return a basic instance
            return new CreatureInstance
            {
                AgeInDays = 100,
                CurrentHealth = 100,
                Happiness = 0.8f,
                Level = 1,
                IsWild = false
            };
        }
        
        private string GetSpeciesName()
        {
            // Extract from authoring component or ECS data
            return targetAuthoring?.name ?? "Unknown Species";
        }
        
        private string GetAgeString()
        {
            if (entityManager.Exists(targetEntity) && entityManager.HasComponent<CreatureAgeComponent>(targetEntity))
            {
                var age = entityManager.GetComponentData<CreatureAgeComponent>(targetEntity);
                return $"{age.ageInDays:F1} days ({age.maturationProgress:P0} mature)";
            }
            return "Unknown age";
        }
        
        private string GetLifeStageString()
        {
            if (entityManager.Exists(targetEntity) && entityManager.HasComponent<CreatureAgeComponent>(targetEntity))
            {
                var age = entityManager.GetComponentData<CreatureAgeComponent>(targetEntity);
                return age.currentLifeStage.ToString();
            }
            return "Unknown";
        }
        
        private string GetHealthString()
        {
            if (entityManager.Exists(targetEntity) && entityManager.HasComponent<CreatureStatsComponent>(targetEntity))
            {
                var stats = entityManager.GetComponentData<CreatureStatsComponent>(targetEntity);
                return $"{stats.currentHealth}/{stats.maxHealth} HP";
            }
            return "Unknown";
        }
        
        private string GetBehaviorStateString()
        {
            if (entityManager.Exists(targetEntity) && entityManager.HasComponent<CreatureBehaviorComponent>(targetEntity))
            {
                var behavior = entityManager.GetComponentData<CreatureBehaviorComponent>(targetEntity);
                return $"{behavior.currentState} ({behavior.behaviorType})";
            }
            return "Unknown";
        }
        
        private string GetGeneticSummary(CreatureGeneticsComponent genetics)
        {
            var dominant = new[]
            {
                ("Strength", genetics.strengthTrait),
                ("Vitality", genetics.vitalityTrait),
                ("Agility", genetics.agilityTrait),
                ("Resilience", genetics.resilienceTrait),
                ("Intelligence", genetics.intellectTrait),
                ("Charm", genetics.charmTrait)
            };
            
            System.Array.Sort(dominant, (a, b) => b.Item2.CompareTo(a.Item2));
            
            return $"Dominant: {dominant[0].Item1} ({dominant[0].Item2:P0}), {dominant[1].Item1} ({dominant[1].Item2:P0})";
        }
        
        private string GetPersonalityDescription(CreaturePersonalityComponent personality)
        {
            string desc = "";
            
            if (personality.bravery > 0.7f) desc += "Brave ";
            else if (personality.bravery < 0.3f) desc += "Timid ";
            
            if (personality.loyalty > 0.7f) desc += "Loyal ";
            else if (personality.loyalty < 0.3f) desc += "Independent ";
            
            if (personality.curiosity > 0.7f) desc += "Curious ";
            if (personality.playfulness > 0.7f) desc += "Playful ";
            if (personality.socialNeed > 0.7f) desc += "Social ";
            
            return string.IsNullOrEmpty(desc) ? "Balanced personality" : desc.Trim();
        }
        
        private string GetBondingStatus(CreatureBondingComponent bonding)
        {
            if (bonding.bondStrength > 0.8f)
                return "Deeply bonded";
            else if (bonding.bondStrength > 0.5f)
                return "Well bonded";
            else if (bonding.bondStrength > 0.2f)
                return "Forming bond";
            else
                return "No bond yet";
        }
        
        private void UpdateNeedsColors(CreatureNeedsComponent needs)
        {
            // Color sliders based on need levels
            UpdateSliderColor(hungerSlider, needs.Hunger);
            UpdateSliderColor(thirstSlider, needs.Thirst);
            UpdateSliderColor(restSlider, needs.Energy);
            UpdateSliderColor(socialNeedSlider, needs.Social);
            UpdateSliderColor(exerciseSlider, needs.Energy);
            UpdateSliderColor(mentalSlider, needs.Comfort);
            
            // Special colors for emotional states
            if (happinessSlider != null)
            {
                var colors = happinessSlider.colors;
                colors.normalColor = Color.Lerp(Color.red, Color.green, needs.Happiness);
                happinessSlider.colors = colors;
            }
            
            if (stressSlider != null)
            {
                var colors = stressSlider.colors;
                float stress = 1.0f - needs.Happiness; // Stress is inverse of happiness
                colors.normalColor = Color.Lerp(Color.green, Color.red, stress);
                stressSlider.colors = colors;
            }
        }
        
        private void UpdateSliderColor(Slider slider, float value)
        {
            if (slider == null) return;
            
            var colors = slider.colors;
            if (value < 0.3f)
                colors.normalColor = Color.red;
            else if (value < 0.6f)
                colors.normalColor = Color.yellow;
            else
                colors.normalColor = Color.green;
                
            slider.colors = colors;
        }
        
        #endregion
        
        #region Debug & Utilities
        
        [ContextMenu("Debug Current Target")]
        private void DebugCurrentTarget()
        {
            if (targetCreature == null)
            {
                Debug.Log("[CreatureInspector] No target creature");
                return;
            }
            
            Debug.Log($"[CreatureInspector] Target: {targetCreature.name}");
            Debug.Log($"  - Has Authoring: {targetAuthoring != null}");
            Debug.Log($"  - ECS Entity: {targetEntity}");
            Debug.Log($"  - Entity Exists: {entityManager.Exists(targetEntity)}");
            
            if (entityManager.Exists(targetEntity))
            {
                Debug.Log($"  - Has Genetics: {entityManager.HasComponent<CreatureGeneticsComponent>(targetEntity)}");
                Debug.Log($"  - Has Personality: {entityManager.HasComponent<CreaturePersonalityComponent>(targetEntity)}");
                Debug.Log($"  - Has Needs: {entityManager.HasComponent<CreatureNeedsComponent>(targetEntity)}");
            }
        }
        
        #endregion
    }
}