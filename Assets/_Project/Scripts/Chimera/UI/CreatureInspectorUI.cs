using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;
using Laboratory.Core.ECS;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.ECS;
using Laboratory.Chimera.Core;
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
                UnityEngine.Debug.Log($"[CreatureInspector] Set target: {creature.name}");
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
                    UnityEngine.Debug.LogWarning($"[CreatureInspector] Update error: {ex.Message}");
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
            if (entityManager.Exists(targetEntity) && entityManager.HasComponent<Laboratory.Chimera.ECS.CreatureGeneticsComponent>(targetEntity))
            {
                var genetics = entityManager.GetComponentData<Laboratory.Chimera.ECS.CreatureGeneticsComponent>(targetEntity);
                
                SetSlider(strengthSlider, genetics.StrengthTrait);
                SetSlider(vitalitySlider, genetics.VitalityTrait);
                SetSlider(agilitySlider, genetics.AgilityTrait);
                SetSlider(resilienceSlider, genetics.ResilienceTrait);
                SetSlider(intellectSlider, genetics.IntellectTrait);
                SetSlider(charmSlider, genetics.CharmTrait);

                SetText(geneticSummaryText, GetGeneticSummary(genetics));
                SetText(lineageText, $"Generation {genetics.Generation} | Lineage: {genetics.LineageId}");
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
            if (entityManager.Exists(targetEntity))
            {
                // Try to get available personality component with safe access
                if (entityManager.HasComponent<Laboratory.Chimera.ECS.CreaturePersonalityComponent>(targetEntity))
                {
                    var component = entityManager.GetComponentData<Laboratory.Chimera.ECS.CreaturePersonalityComponent>(targetEntity);
                    ExtractPersonalityData(component);
                    return;
                }
                else if (entityManager.HasComponent<BehaviorStateComponent>(targetEntity))
                {
                    var component = entityManager.GetComponentData<BehaviorStateComponent>(targetEntity);
                    ExtractPersonalityDataFromBehavior(component);
                    return;
                }
                else if (entityManager.HasComponent<CreatureAIComponent>(targetEntity))
                {
                    var component = entityManager.GetComponentData<CreatureAIComponent>(targetEntity);
                    ExtractPersonalityDataFromAI(component);
                    return;
                }
            }

            // Fallback to authoring component
            if (targetAuthoring != null && targetAuthoring.GetType().GetMethod("GetPersonalityDescription") != null)
            {
                var description = (string)targetAuthoring.GetType().GetMethod("GetPersonalityDescription").Invoke(targetAuthoring, null);
                SetText(personalityDescText, description);
            }
            else
            {
                SetText(personalityDescText, "Balanced personality");
            }
        }
        
        private void UpdateNeedsDisplay()
        {
            if (entityManager.Exists(targetEntity))
            {
                // Try to get available needs component with safe access
                if (entityManager.HasComponent<CreatureNeedsComponent>(targetEntity))
                {
                    var component = entityManager.GetComponentData<CreatureNeedsComponent>(targetEntity);
                    ExtractNeedsData(component);
                    return;
                }
                else if (entityManager.HasComponent<BehaviorStateComponent>(targetEntity))
                {
                    var component = entityManager.GetComponentData<BehaviorStateComponent>(targetEntity);
                    ExtractNeedsDataFromBehavior(component);
                    return;
                }
            }

            // Set default values if no component found
            SetSlider(hungerSlider, 0.5f);
            SetSlider(thirstSlider, 0.5f);
            SetSlider(restSlider, 0.5f);
            SetSlider(socialNeedSlider, 0.5f);
            SetSlider(exerciseSlider, 0.5f);
            SetSlider(mentalSlider, 0.5f);
            SetSlider(happinessSlider, 0.5f);
            SetSlider(stressSlider, 0.3f);
        }
        
        private void UpdateBondingDisplay()
        {
            if (entityManager.Exists(targetEntity))
            {
                // Try to find any social/bonding components
                if (entityManager.HasComponent<SocialTerritoryComponent>(targetEntity))
                {
                    var component = entityManager.GetComponentData<SocialTerritoryComponent>(targetEntity);
                    ExtractBondingDataFromSocial(component);
                    return;
                }
                else if (entityManager.HasComponent<CreatureAIComponent>(targetEntity))
                {
                    var component = entityManager.GetComponentData<CreatureAIComponent>(targetEntity);
                    ExtractBondingDataFromAI(component);
                    return;
                }
            }

            // Default values if no bonding component found
            SetSlider(bondStrengthSlider, 0.3f);
            SetSlider(trustSlider, 0.3f);
            SetSlider(obedienceSlider, 0.3f);
            SetText(bondingStatusText, "No player bond");
        }
        
        private void UpdateEnvironmentalDisplay()
        {
            if (entityManager.Exists(targetEntity))
            {
                // Try to find environmental components
                if (entityManager.HasComponent<EnvironmentalComponent>(targetEntity))
                {
                    var component = entityManager.GetComponentData<EnvironmentalComponent>(targetEntity);
                    ExtractEnvironmentalData(component);
                    return;
                }
                else if (entityManager.HasComponent<BiomeComponent>(targetEntity))
                {
                    var component = entityManager.GetComponentData<BiomeComponent>(targetEntity);
                    ExtractEnvironmentalDataFromBiome(component);
                    return;
                }
            }

            // Default values if no environmental component found
            SetText(currentBiomeText, "Unknown Biome");
            SetSlider(biomeComfortSlider, 0.5f);
            SetSlider(adaptationSlider, 0.5f);
            SetText(environmentalStressText, "Environmental Stress: Unknown");
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
            if (entityManager.Exists(targetEntity))
            {
                // Try to find age-related components
                if (entityManager.HasComponent<CreatureIdentityComponent>(targetEntity))
                {
                    var identity = entityManager.GetComponentData<CreatureIdentityComponent>(targetEntity);
                    var maturation = identity.Age / identity.MaxLifespan;
                    return $"{identity.Age:F1} years ({maturation:P0} mature)";
                }
            }
            return "Unknown age";
        }
        
        private string GetLifeStageString()
        {
            if (entityManager.Exists(targetEntity))
            {
                if (entityManager.HasComponent<CreatureIdentityComponent>(targetEntity))
                {
                    var identity = entityManager.GetComponentData<CreatureIdentityComponent>(targetEntity);
                    return identity.CurrentLifeStage.ToString();
                }
            }
            return "Unknown";
        }
        
        private string GetHealthString()
        {
            if (entityManager.Exists(targetEntity))
            {
                if (entityManager.HasComponent<Laboratory.Core.ECS.CreatureStats>(targetEntity))
                {
                    var stats = entityManager.GetComponentData<Laboratory.Core.ECS.CreatureStats>(targetEntity);
                    return $"{stats.health}/{stats.maxHealth} HP";
                }
            }
            return "Unknown";
        }
        
        private string GetBehaviorStateString()
        {
            if (entityManager.Exists(targetEntity) && entityManager.HasComponent<Laboratory.Chimera.ECS.CreatureBehaviorComponent>(targetEntity))
            {
                var behavior = entityManager.GetComponentData<Laboratory.Chimera.ECS.CreatureBehaviorComponent>(targetEntity);
                return $"{behavior.currentState} ({behavior.behaviorType})";
            }
            return "Unknown";
        }
        
        private string GetGeneticSummary(Laboratory.Chimera.ECS.CreatureGeneticsComponent genetics)
        {
            var dominant = new[]
            {
                ("Strength", genetics.StrengthTrait),
                ("Vitality", genetics.VitalityTrait),
                ("Agility", genetics.AgilityTrait),
                ("Resilience", genetics.ResilienceTrait),
                ("Intelligence", genetics.IntellectTrait),
                ("Charm", genetics.CharmTrait)
            };
            
            System.Array.Sort(dominant, (a, b) => b.Item2.CompareTo(a.Item2));
            
            return $"Dominant: {dominant[0].Item1} ({dominant[0].Item2:P0}), {dominant[1].Item1} ({dominant[1].Item2:P0})";
        }
        
        private string GetPersonalityDescription(Laboratory.Chimera.ECS.CreaturePersonalityComponent personality)
        {
            return "Balanced personality";
        }
        
        private string GetBondingStatus(Laboratory.Chimera.ECS.CreatureBondingComponent bonding)
        {
            return "No player bond";
        }

        // Helper methods for extracting data from different component types
        private void ExtractPersonalityData(object component)
        {
            // Use reflection to safely extract personality-related data from various component types
            var type = component.GetType();

            // Look for common personality fields and map them to UI sliders
            ExtractAndSetSlider(component, type, new[] { "bravery", "Bravery", "AggressionLevel" }, braverySlider, 0.5f);
            ExtractAndSetSlider(component, type, new[] { "loyalty", "Loyalty", "LoyaltyLevel" }, loyaltySlider, 0.5f);
            ExtractAndSetSlider(component, type, new[] { "curiosity", "Curiosity", "CuriosityLevel" }, curiositySlider, 0.5f);
            ExtractAndSetSlider(component, type, new[] { "social", "Social", "Sociability" }, socialSlider, 0.5f);
            ExtractAndSetSlider(component, type, new[] { "playfulness", "Playfulness" }, playfulnessSlider, 0.5f);
        }

        private void ExtractNeedsData(object component)
        {
            var type = component.GetType();

            // Look for needs-related fields
            ExtractAndSetSlider(component, type, new[] { "Hunger", "hunger" }, hungerSlider, 0.5f);
            ExtractAndSetSlider(component, type, new[] { "Thirst", "thirst" }, thirstSlider, 0.5f);
            ExtractAndSetSlider(component, type, new[] { "Energy", "energy", "rest" }, restSlider, 0.5f);
            ExtractAndSetSlider(component, type, new[] { "Social", "social", "SocialConnection" }, socialNeedSlider, 0.5f);
            ExtractAndSetSlider(component, type, new[] { "Comfort", "comfort" }, exerciseSlider, 0.5f);
            ExtractAndSetSlider(component, type, new[] { "Satisfaction", "satisfaction", "Happiness" }, happinessSlider, 0.5f);
            ExtractAndSetSlider(component, type, new[] { "Stress", "stress" }, stressSlider, 0.3f);
        }

        private void ExtractBondingData(object component)
        {
            var type = component.GetType();

            // Look for bonding-related fields
            ExtractAndSetSlider(component, type, new[] { "PackLoyalty", "TrustLevel", "bondStrength" }, bondStrengthSlider, 0.3f);
            ExtractAndSetSlider(component, type, new[] { "TrustLevel", "trust" }, trustSlider, 0.3f);
            ExtractAndSetSlider(component, type, new[] { "obedience", "LoyaltyLevel" }, obedienceSlider, 0.3f);
        }

        private void ExtractEnvironmentalData(object component)
        {
            var type = component.GetType();

            // Look for environmental fields
            var biomeField = type.GetField("CurrentBiome") ?? type.GetField("currentBiome") ?? type.GetField("BiomeType");
            if (biomeField != null)
            {
                var biomeValue = biomeField.GetValue(component);
                SetText(currentBiomeText, biomeValue?.ToString() ?? "Unknown");
            }

            ExtractAndSetSlider(component, type, new[] { "BiomeComfortLevel", "biomeComfort", "AdaptationLevel" }, biomeComfortSlider, 0.5f);
            ExtractAndSetSlider(component, type, new[] { "AdaptationLevel", "adaptation", "BiomeAdaptation" }, adaptationSlider, 0.5f);
        }

        private void ExtractAndSetSlider(object component, System.Type type, string[] fieldNames, Slider slider, float defaultValue)
        {
            foreach (var fieldName in fieldNames)
            {
                var field = type.GetField(fieldName);
                if (field != null)
                {
                    var value = field.GetValue(component);
                    if (value is float floatValue)
                    {
                        SetSlider(slider, floatValue);
                        return;
                    }
                }
            }

            // Fallback to default value
            SetSlider(slider, defaultValue);
        }
        
        private void UpdateNeedsColors(Laboratory.Chimera.ECS.CreatureNeedsComponent needs)
        {
            // Color sliders based on need levels
            UpdateSliderColor(hungerSlider, needs.Hunger);
            UpdateSliderColor(thirstSlider, needs.Thirst);
            UpdateSliderColor(restSlider, needs.Energy);
            UpdateSliderColor(socialNeedSlider, needs.SocialConnection);
            UpdateSliderColor(exerciseSlider, needs.Energy);
            UpdateSliderColor(mentalSlider, needs.Comfort);
            
            // Special colors for emotional states
            if (happinessSlider != null)
            {
                var colors = happinessSlider.colors;
                colors.normalColor = Color.Lerp(Color.red, Color.green, needs.Comfort);
                happinessSlider.colors = colors;
            }
            
            if (stressSlider != null)
            {
                var colors = stressSlider.colors;
                float stress = 1.0f - needs.Comfort; // Stress is inverse of comfort
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
                UnityEngine.Debug.Log("[CreatureInspector] No target creature");
                return;
            }
            
            UnityEngine.Debug.Log($"[CreatureInspector] Target: {targetCreature.name}");
            UnityEngine.Debug.Log($"  - Has Authoring: {targetAuthoring != null}");
            UnityEngine.Debug.Log($"  - ECS Entity: {targetEntity}");
            UnityEngine.Debug.Log($"  - Entity Exists: {entityManager.Exists(targetEntity)}");
            
            if (entityManager.Exists(targetEntity))
            {
                UnityEngine.Debug.Log($"  - Has Genetics: {entityManager.HasComponent<Laboratory.Chimera.ECS.CreatureGeneticsComponent>(targetEntity)}");
                UnityEngine.Debug.Log($"  - Has Personality: {entityManager.HasComponent<Laboratory.Chimera.ECS.CreaturePersonalityComponent>(targetEntity)}");
                UnityEngine.Debug.Log($"  - Has Needs: {entityManager.HasComponent<Laboratory.Chimera.ECS.CreatureNeedsComponent>(targetEntity)}");
            }
        }

        // Specialized extraction methods for specific component types
        private void ExtractPersonalityDataFromBehavior(BehaviorStateComponent behaviorComponent)
        {
            // Extract personality-like data from behavior state
            SetSlider(braverySlider, 1f - behaviorComponent.Stress);
            SetSlider(socialSlider, behaviorComponent.Satisfaction);
            SetSlider(curiositySlider, behaviorComponent.BehaviorIntensity);
        }

        private void ExtractPersonalityDataFromAI(CreatureAIComponent aiComponent)
        {
            // Extract personality data from AI component using reflection
            ExtractPersonalityData(aiComponent);
        }

        private void ExtractNeedsDataFromBehavior(BehaviorStateComponent behaviorComponent)
        {
            // Map behavior state to needs
            SetSlider(happinessSlider, behaviorComponent.Satisfaction);
            SetSlider(stressSlider, behaviorComponent.Stress);
            SetSlider(socialNeedSlider, behaviorComponent.DecisionConfidence);

            // Set defaults for other needs
            SetSlider(hungerSlider, 0.6f);
            SetSlider(thirstSlider, 0.7f);
            SetSlider(restSlider, 1f - behaviorComponent.Stress);
        }

        private void ExtractBondingDataFromSocial(SocialTerritoryComponent socialComponent)
        {
            // Extract bonding data from social territory component
            SetSlider(bondStrengthSlider, socialComponent.PackLoyalty);
            SetSlider(trustSlider, socialComponent.TerritoryQuality);
            SetSlider(obedienceSlider, socialComponent.PackLoyalty * 0.8f);
        }

        private void ExtractBondingDataFromAI(CreatureAIComponent aiComponent)
        {
            // Extract bonding data from AI component using reflection
            ExtractBondingData(aiComponent);
        }

        private void ExtractEnvironmentalDataFromBiome(BiomeComponent biomeComponent)
        {
            // Extract environmental data from biome component
            SetText(currentBiomeText, biomeComponent.BiomeType.ToString());
            SetSlider(biomeComfortSlider, biomeComponent.ResourceDensity);
            SetSlider(adaptationSlider, 0.8f); // Default adaptation level
            SetText(environmentalStressText, $"Environmental Stress: {(1f - biomeComponent.ResourceDensity) * 100f:F0}%");
        }

        #endregion
    }
}