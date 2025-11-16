using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Customization;
using Laboratory.Core.Equipment;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Visuals;
using Laboratory.Core.Equipment.Types;
using Laboratory.Core.MonsterTown;
using Laboratory.Core.Enums;
using EquipmentItem = Laboratory.Core.MonsterTown.Equipment;

namespace Laboratory.Chimera.Customization
{
    /// <summary>
    /// Comprehensive customization system for Chimera creatures.
    /// Handles both genetic-based procedural appearance and equipment/outfit visualization.
    /// Integrates with existing equipment system and visual genetics.
    /// </summary>
    [RequireComponent(typeof(CreatureInstanceComponent))]
    public class ChimeraCustomizationManager : MonoBehaviour, ICustomizationSystem
    {
        #region Serialized Fields

        [Header("🎭 Chimera Visual Components")]
        [SerializeField] private Transform[] bodyPartAnchors;
        [SerializeField] private Transform[] accessorySlots;
        [SerializeField] private Renderer[] customizableRenderers;
        [SerializeField] private ParticleSystem[] magicalEffects;

        [Header("🎒 Equipment Visualization")]
        [SerializeField] private Transform armorParent;
        [SerializeField] private Transform weaponParent;
        [SerializeField] private Transform accessoryParent;
        [SerializeField] private Transform ridingGearParent;

        [Header("🌈 Pattern & Color Systems")]
        [SerializeField] private bool enableGeneticPatterns = true;
        [SerializeField] private bool enableEquipmentVisuals = true;
        [SerializeField] private float patternComplexity = 1.0f;

        [Header("⚙️ Configuration")]
        [SerializeField] private ChimeraCustomizationConfig config;
        [SerializeField] private bool autoUpdateOnEquipment = true;

        #endregion

        #region Private Fields

        private CreatureInstanceComponent creatureInstance;
        private ProceduralVisualSystem visualSystem;
        private EquipmentManager equipmentManager;

        // Customization state
        private ChimeraCustomizationData currentCustomization;
        private Dictionary<Laboratory.Core.MonsterTown.EquipmentType, GameObject> equippedVisuals = new();
        private Dictionary<string, GameObject> customOutfits = new();
        private Dictionary<string, Material> materialCache = new();

        // System state
        private bool isInitialized = false;
        private bool isActive = true;

        #endregion

        #region Properties

        public bool IsActive => isActive;
        public bool IsInitialized => isInitialized;
        public ChimeraCustomizationData CurrentCustomization => currentCustomization;
        public CreatureInstanceComponent CreatureInstance => creatureInstance;

        #endregion

        #region ICustomizationSystem Implementation

        public void SetActive(bool active)
        {
            isActive = active;

            if (config?.EnableDebugLogging == true)
            {
                UnityEngine.Debug.Log($"ChimeraCustomizationManager: Active state changed to {active}");
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            InitializeCustomizationSystem();
        }

        private void Update()
        {
            if (isActive && autoUpdateOnEquipment)
            {
                CheckForEquipmentChanges();
            }
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            creatureInstance = GetComponent<CreatureInstanceComponent>();
            visualSystem = GetComponent<ProceduralVisualSystem>();
            equipmentManager = FindFirstObjectByType<EquipmentManager>();

            // Auto-find visual components if not assigned
            if (customizableRenderers == null || customizableRenderers.Length == 0)
            {
                customizableRenderers = GetComponentsInChildren<Renderer>();
            }

            if (magicalEffects == null || magicalEffects.Length == 0)
            {
                magicalEffects = GetComponentsInChildren<ParticleSystem>();
            }

            ValidateAnchors();
        }

        private void InitializeCustomizationSystem()
        {
            if (creatureInstance?.CreatureData?.GeneticProfile == null)
            {
                UnityEngine.Debug.LogWarning("ChimeraCustomizationManager: No genetic profile found, skipping initialization");
                return;
            }

            currentCustomization = new ChimeraCustomizationData
            {
                CreatureId = creatureInstance.CreatureData.UniqueId,
                GeneticAppearance = GenerateGeneticAppearance(),
                EquippedItems = new List<EquippedItemVisual>(),
                CustomPatterns = new List<CustomPattern>(),
                ColorOverrides = new Dictionary<string, Color>()
            };

            ApplyGeneticAppearance();
            ApplyEquippedItems();

            isInitialized = true;

            if (config?.EnableDebugLogging == true)
            {
                UnityEngine.Debug.Log($"🎭 ChimeraCustomizationManager initialized for {creatureInstance.CreatureData.UniqueId}");
            }
        }

        private void ValidateAnchors()
        {
            // Create anchor points if they don't exist
            if (armorParent == null)
                armorParent = CreateAnchor("ArmorParent");
            if (weaponParent == null)
                weaponParent = CreateAnchor("WeaponParent");
            if (accessoryParent == null)
                accessoryParent = CreateAnchor("AccessoryParent");
            if (ridingGearParent == null)
                ridingGearParent = CreateAnchor("RidingGearParent");
        }

        private Transform CreateAnchor(string name)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(transform);
            anchor.localPosition = Vector3.zero;
            return anchor;
        }

        #endregion

        #region Genetic Appearance

        private GeneticAppearanceData GenerateGeneticAppearance()
        {
            var genetics = creatureInstance.CreatureData.GeneticProfile;
            var appearance = new GeneticAppearanceData();

            // Generate body proportions from genetics
            appearance.BodyScale = CalculateBodyScale(genetics);
            appearance.LimbProportions = CalculateLimbProportions(genetics);

            // Generate color scheme from genetic traits
            appearance.PrimaryColors = GenerateGeneticColors(genetics, "primary");
            appearance.SecondaryColors = GenerateGeneticColors(genetics, "secondary");
            appearance.PatternColors = GenerateGeneticColors(genetics, "pattern");

            // Generate patterns from genetic diversity
            appearance.PatternTypes = GenerateGeneticPatterns(genetics);
            appearance.PatternIntensity = CalculatePatternIntensity(genetics);

            // Generate magical effects from special traits
            appearance.MagicalAura = GenerateMagicalEffects(genetics);

            return appearance;
        }

        private Vector3 CalculateBodyScale(GeneticProfile genetics)
        {
            // Use genetic traits to determine body proportions
            float sizeGene = GetGeneticValue(genetics, TraitType.Size, 1.0f);
            float strengthGene = GetGeneticValue(genetics, TraitType.Strength, 1.0f);
            float agilityGene = GetGeneticValue(genetics, TraitType.Agility, 1.0f);

            return new Vector3(
                0.8f + (strengthGene * 0.4f),  // Width influenced by strength
                0.8f + (sizeGene * 0.4f),      // Height influenced by size
                0.8f + (agilityGene * 0.2f)    // Depth slightly influenced by agility
            );
        }

        private Dictionary<string, Vector3> CalculateLimbProportions(GeneticProfile genetics)
        {
            var proportions = new Dictionary<string, Vector3>();

            float agilityFactor = GetGeneticValue(genetics, TraitType.Agility, 1.0f);
            float strengthFactor = GetGeneticValue(genetics, TraitType.Strength, 1.0f);

            proportions["legs"] = new Vector3(1.0f, 0.8f + (agilityFactor * 0.4f), 1.0f);
            proportions["arms"] = new Vector3(0.8f + (strengthFactor * 0.4f), 1.0f, 1.0f);
            proportions["tail"] = new Vector3(1.0f, 1.0f, 0.7f + (agilityFactor * 0.6f));

            return proportions;
        }

        private Color[] GenerateGeneticColors(GeneticProfile genetics, string colorType)
        {
            var colors = new List<Color>();

            // Base genetic color determination
            float hue = GetColorGeneticValue(genetics, colorType, "Hue", 0.5f);
            float saturation = GetColorGeneticValue(genetics, colorType, "Saturation", 0.8f);
            float brightness = GetColorGeneticValue(genetics, colorType, "Brightness", 0.7f);

            // Generate primary color
            Color baseColor = Color.HSVToRGB(hue, saturation, brightness);
            colors.Add(baseColor);

            // Generate complementary colors based on genetic diversity
            int colorCount = Mathf.RoundToInt(GetGeneticValue(genetics, TraitType.ColorComplexity, 2.0f));
            for (int i = 1; i < colorCount; i++)
            {
                float hueShift = (i * 120f) / 360f; // Shift hue for complementary colors
                Color complementaryColor = Color.HSVToRGB((hue + hueShift) % 1.0f, saturation * 0.8f, brightness * 0.9f);
                colors.Add(complementaryColor);
            }

            return colors.ToArray();
        }

        private string[] GenerateGeneticPatterns(GeneticProfile genetics)
        {
            var patterns = new List<string>();

            // Determine pattern types based on genetic traits
            if (GetGeneticValue(genetics, TraitType.HuntingDrive, 0.5f) > 0.6f) // Predator behavior
                patterns.Add("stripes");
            if (GetGeneticValue(genetics, TraitType.Camouflage, 0.5f) > 0.6f)
                patterns.Add("spots");
            if (GetGeneticValue(genetics, TraitType.Sociability, 0.5f) > 0.7f) // Using Sociability as closest match
                patterns.Add("geometric");
            if (GetGeneticValue(genetics, TraitType.Magical, 0.5f) > 0.5f)
                patterns.Add("runes");

            return patterns.ToArray();
        }

        private float CalculatePatternIntensity(GeneticProfile genetics)
        {
            // Pattern intensity based on genetic expression strength
            float diversityScore = GetGeneticValue(genetics, TraitType.Diversity, 0.5f);
            float expressionStrength = GetGeneticValue(genetics, TraitType.Expression, 0.5f);

            return Mathf.Clamp01(diversityScore * expressionStrength * patternComplexity);
        }

        private MagicalAuraData GenerateMagicalEffects(GeneticProfile genetics)
        {
            var aura = new MagicalAuraData();

            float magicalPotency = GetGeneticValue(genetics, TraitType.Magical, 0.0f);
            if (magicalPotency > 0.3f)
            {
                aura.HasAura = true;
                aura.AuraColor = GenerateGeneticColors(genetics, "magical")[0];
                aura.AuraIntensity = magicalPotency;
                aura.EffectType = DetermineEffectType(genetics);
            }

            return aura;
        }

        private string DetermineEffectType(GeneticProfile genetics)
        {
            float fireAffinity = GetGeneticValue(genetics, TraitType.FireAffinity, 0.0f);
            float waterAffinity = GetGeneticValue(genetics, TraitType.WaterAffinity, 0.0f);
            float earthAffinity = GetGeneticValue(genetics, TraitType.EarthAffinity, 0.0f);
            float airAffinity = GetGeneticValue(genetics, TraitType.AirAffinity, 0.0f);

            var affinities = new Dictionary<string, float>
            {
                ["fire"] = fireAffinity,
                ["water"] = waterAffinity,
                ["earth"] = earthAffinity,
                ["air"] = airAffinity
            };

            return affinities.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        private float GetGeneticValue(GeneticProfile genetics, TraitType traitType, float defaultValue)
        {
            if (genetics.TraitExpressions.ContainsKey(traitType))
            {
                return genetics.TraitExpressions[traitType].Value;
            }
            return defaultValue + UnityEngine.Random.Range(-0.2f, 0.2f);
        }

        // Method for handling color-specific genetic traits
        private float GetColorGeneticValue(GeneticProfile genetics, string colorType, string component, float defaultValue)
        {
            // Handle dynamic color traits like "Color_Primary_Hue"
            // This is acceptable for highly dynamic composite traits
            // Could be improved with a ColorTraitType enum in the future
            return defaultValue + UnityEngine.Random.Range(-0.2f, 0.2f);
        }

        #endregion

        #region Equipment Visualization

        public void EquipItem(EquipmentItem equipment, bool visualOnly = false)
        {
            if (!isActive || equipment == null) return;

            // Apply equipment to creature if not visual-only
            if (!visualOnly && equipmentManager != null)
            {
                var monster = GetMonsterFromCreature();
                if (monster != null)
                {
                    equipmentManager.EquipItem(monster, equipment.ItemId);
                }
            }

            // Create visual representation
            CreateEquipmentVisual(equipment);

            // Update customization data
            var equippedVisual = new EquippedItemVisual
            {
                ItemId = equipment.ItemId,
                EquipmentType = equipment.Type,
                VisualPrefabPath = GetEquipmentVisualPath(equipment),
                Modifications = new Dictionary<string, object>()
            };

            currentCustomization.EquippedItems.RemoveAll(e => e.EquipmentType == equipment.Type);
            currentCustomization.EquippedItems.Add(equippedVisual);

            if (config?.EnableDebugLogging == true)
            {
                UnityEngine.Debug.Log($"🎒 Equipped {equipment.Name} on {creatureInstance.CreatureData.UniqueId}");
            }
        }

        public void UnequipItem(Laboratory.Core.MonsterTown.EquipmentType equipmentType)
        {
            if (!isActive) return;

            // Remove from equipment manager
            if (equipmentManager != null)
            {
                var monster = GetMonsterFromCreature();
                if (monster != null)
                {
                    var equippedItem = monster.Equipment?.FirstOrDefault(e => e.Type == equipmentType);
                    if (equippedItem != null)
                    {
                        equipmentManager.UnequipItem(monster, equippedItem.ItemId);
                    }
                }
            }

            // Remove visual
            RemoveEquipmentVisual(equipmentType);

            // Update customization data
            currentCustomization.EquippedItems.RemoveAll(e => e.EquipmentType == equipmentType);

            if (config?.EnableDebugLogging == true)
            {
                UnityEngine.Debug.Log($"🎒 Unequipped {equipmentType} from {creatureInstance.CreatureData.UniqueId}");
            }
        }

        private void CreateEquipmentVisual(EquipmentItem equipment)
        {
            string visualPath = GetEquipmentVisualPath(equipment);
            GameObject visualPrefab = Resources.Load<GameObject>(visualPath);

            if (visualPrefab == null)
            {
                UnityEngine.Debug.LogWarning($"No visual found for equipment: {equipment.ItemId} at path: {visualPath}");
                return;
            }

            Transform parent = GetEquipmentParent(equipment.Type);
            GameObject visual = Instantiate(visualPrefab, parent);

            // Apply equipment-specific modifications
            ApplyEquipmentModifications(visual, equipment);

            // Cache the visual
            equippedVisuals[equipment.Type] = visual;
        }

        private void RemoveEquipmentVisual(Laboratory.Core.MonsterTown.EquipmentType equipmentType)
        {
            if (equippedVisuals.TryGetValue(equipmentType, out GameObject visual))
            {
                DestroyImmediate(visual);
                equippedVisuals.Remove(equipmentType);
            }
        }

        private string GetEquipmentVisualPath(EquipmentItem equipment)
        {
            return $"Equipment/Chimera/{equipment.Type}/{equipment.ItemId}";
        }

        private Transform GetEquipmentParent(Laboratory.Core.MonsterTown.EquipmentType equipmentType)
        {
            return equipmentType switch
            {
                Laboratory.Core.MonsterTown.EquipmentType.Armor => armorParent,
                Laboratory.Core.MonsterTown.EquipmentType.Weapon => weaponParent,
                Laboratory.Core.MonsterTown.EquipmentType.Accessory => accessoryParent,
                Laboratory.Core.MonsterTown.EquipmentType.Vehicle => ridingGearParent, // Vehicle is the closest equivalent to RidingGear
                _ => accessoryParent
            };
        }


        private void ApplyEquipmentModifications(GameObject visual, EquipmentItem equipment)
        {
            // Apply rarity-based effects
            ApplyRarityEffects(visual, equipment.Rarity);

            // Apply level-based scaling
            ApplyLevelScaling(visual, equipment.Level);

            // Apply stat-based visual effects
            ApplyStatEffects(visual, equipment.StatBonuses);
        }

        private void ApplyRarityEffects(GameObject visual, Laboratory.Core.MonsterTown.EquipmentRarity rarity)
        {
            // Add visual effects based on rarity
            switch (rarity)
            {
                case Laboratory.Core.MonsterTown.EquipmentRarity.Uncommon:
                    AddGlow(visual, Color.green, 0.3f);
                    break;
                case Laboratory.Core.MonsterTown.EquipmentRarity.Rare:
                    AddGlow(visual, Color.blue, 0.5f);
                    break;
                case Laboratory.Core.MonsterTown.EquipmentRarity.Epic:
                    AddGlow(visual, Color.magenta, 0.7f);
                    AddParticleEffect(visual, "epic_sparkles");
                    break;
                case Laboratory.Core.MonsterTown.EquipmentRarity.Legendary:
                    AddGlow(visual, Color.yellow, 1.0f);
                    AddParticleEffect(visual, "legendary_aura");
                    break;
            }
        }

        private void ApplyLevelScaling(GameObject visual, int level)
        {
            float scale = 1.0f + (level - 1) * 0.05f; // 5% size increase per level
            visual.transform.localScale *= scale;
        }

        private void ApplyStatEffects(GameObject visual, Dictionary<Laboratory.Core.MonsterTown.StatType, float> statBonuses)
        {
            foreach (var statBonus in statBonuses)
            {
                switch (statBonus.Key)
                {
                    case Laboratory.Core.MonsterTown.StatType.Strength:
                        AddStatEffect(visual, "strength_aura", statBonus.Value);
                        break;
                    case Laboratory.Core.MonsterTown.StatType.Agility:
                        AddStatEffect(visual, "speed_trails", statBonus.Value);
                        break;
                    case Laboratory.Core.MonsterTown.StatType.Intelligence:
                        AddStatEffect(visual, "wisdom_glow", statBonus.Value);
                        break;
                }
            }
        }

        #endregion

        #region Custom Outfits

        public void ApplyCustomOutfit(CustomOutfitData outfit)
        {
            if (!isActive || outfit == null) return;

            RemoveAllCustomOutfits();

            foreach (var piece in outfit.OutfitPieces)
            {
                CreateCustomOutfitPiece(piece);
            }

            // Apply outfit colors
            if (outfit.ColorScheme != null)
            {
                ApplyCustomColors(outfit.ColorScheme);
            }

            currentCustomization.CustomOutfit = outfit;

            if (config?.EnableDebugLogging == true)
            {
                UnityEngine.Debug.Log($"🎭 Applied custom outfit '{outfit.OutfitName}' to {creatureInstance.CreatureData.UniqueId}");
            }
        }

        public void CreateCustomOutfitPiece(OutfitPiece piece)
        {
            GameObject piecePrefab = Resources.Load<GameObject>(piece.PrefabPath);
            if (piecePrefab == null)
            {
                UnityEngine.Debug.LogWarning($"Custom outfit piece not found: {piece.PrefabPath}");
                return;
            }

            Transform parent = GetOutfitParent(piece.Category);
            GameObject pieceInstance = Instantiate(piecePrefab, parent);

            // Apply piece-specific modifications
            ApplyOutfitPieceModifications(pieceInstance, piece);

            customOutfits[piece.PieceId] = pieceInstance;
        }

        public void RemoveAllCustomOutfits()
        {
            foreach (var outfit in customOutfits.Values)
            {
                if (outfit != null)
                    DestroyImmediate(outfit);
            }
            customOutfits.Clear();
            currentCustomization.CustomOutfit = null;
        }

        private Transform GetOutfitParent(string category)
        {
            return category.ToLower() switch
            {
                "body" => armorParent,
                "head" => accessoryParent,
                "limbs" => armorParent,
                _ => accessoryParent
            };
        }

        private void ApplyOutfitPieceModifications(GameObject piece, OutfitPiece outfitPiece)
        {
            // Apply transformations
            if (outfitPiece.Scale != Vector3.zero)
                piece.transform.localScale = Vector3.Scale(piece.transform.localScale, outfitPiece.Scale);

            piece.transform.localPosition += outfitPiece.PositionOffset;
            piece.transform.localRotation *= Quaternion.Euler(outfitPiece.RotationOffset);

            // Apply materials if specified
            if (!string.IsNullOrEmpty(outfitPiece.MaterialPath))
            {
                Material customMaterial = Resources.Load<Material>(outfitPiece.MaterialPath);
                if (customMaterial != null)
                {
                    var renderers = piece.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.material = customMaterial;
                    }
                }
            }
        }

        #endregion

        #region Visual Effects

        private void AddGlow(GameObject target, Color color, float intensity)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var material = renderer.material;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * intensity);
                }
            }
        }

        private void AddParticleEffect(GameObject target, string effectName)
        {
            GameObject effectPrefab = Resources.Load<GameObject>($"Effects/Equipment/{effectName}");
            if (effectPrefab != null)
            {
                Instantiate(effectPrefab, target.transform);
            }
        }

        private void AddStatEffect(GameObject target, string effectType, float intensity)
        {
            GameObject effectPrefab = Resources.Load<GameObject>($"Effects/Stats/{effectType}");
            if (effectPrefab != null)
            {
                var effect = Instantiate(effectPrefab, target.transform);
                var particleSystem = effect.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    var main = particleSystem.main;
                    main.startLifetime = intensity * 2f;
                    main.startSize = intensity * 0.5f;
                }
            }
        }

        #endregion

        #region Genetic Integration

        private void ApplyGeneticAppearance()
        {
            if (!enableGeneticPatterns || currentCustomization?.GeneticAppearance == null) return;

            var appearance = currentCustomization.GeneticAppearance;

            // Apply body scaling
            ApplyBodyScaling(appearance.BodyScale, appearance.LimbProportions);

            // Apply genetic colors
            ApplyGeneticColoring(appearance);

            // Apply genetic patterns
            ApplyGeneticPatterns(appearance);

            // Apply magical effects
            ApplyMagicalEffects(appearance.MagicalAura);
        }

        private void ApplyBodyScaling(Vector3 bodyScale, Dictionary<string, Vector3> limbProportions)
        {
            // Apply overall body scale
            transform.localScale = Vector3.Scale(transform.localScale, bodyScale);

            // Apply limb proportions
            if (bodyPartAnchors != null)
            {
                foreach (var anchor in bodyPartAnchors)
                {
                    if (anchor != null && limbProportions.ContainsKey(anchor.name.ToLower()))
                    {
                        anchor.localScale = Vector3.Scale(anchor.localScale, limbProportions[anchor.name.ToLower()]);
                    }
                }
            }
        }

        private void ApplyGeneticColoring(GeneticAppearanceData appearance)
        {
            if (customizableRenderers == null) return;

            for (int i = 0; i < customizableRenderers.Length; i++)
            {
                var renderer = customizableRenderers[i];
                if (renderer == null) continue;

                var material = renderer.material;

                // Apply primary color
                if (appearance.PrimaryColors.Length > 0)
                {
                    material.color = appearance.PrimaryColors[0];
                }

                // Apply secondary color if material supports it
                if (appearance.SecondaryColors.Length > 0 && material.HasProperty("_SecondaryColor"))
                {
                    material.SetColor("_SecondaryColor", appearance.SecondaryColors[0]);
                }
            }
        }

        private void ApplyGeneticPatterns(GeneticAppearanceData appearance)
        {
            if (appearance.PatternTypes == null || appearance.PatternTypes.Length == 0) return;

            foreach (var patternType in appearance.PatternTypes)
            {
                ApplyPattern(patternType, appearance.PatternIntensity, appearance.PatternColors);
            }
        }

        private void ApplyPattern(string patternType, float intensity, Color[] patternColors)
        {
            // Load pattern material
            Material patternMaterial = Resources.Load<Material>($"Materials/Patterns/{patternType}");
            if (patternMaterial == null) return;

            // Apply to first available renderer
            if (customizableRenderers.Length > 0)
            {
                var renderer = customizableRenderers[0];
                renderer.material = patternMaterial;

                // Configure pattern properties
                if (patternMaterial.HasProperty("_PatternIntensity"))
                    patternMaterial.SetFloat("_PatternIntensity", intensity);

                if (patternColors.Length > 0 && patternMaterial.HasProperty("_PatternColor"))
                    patternMaterial.SetColor("_PatternColor", patternColors[0]);
            }
        }

        private void ApplyMagicalEffects(MagicalAuraData auraData)
        {
            if (!auraData.HasAura || magicalEffects == null) return;

            foreach (var effect in magicalEffects)
            {
                if (effect == null) continue;

                var main = effect.main;
                main.startColor = auraData.AuraColor;
                main.startLifetime = auraData.AuraIntensity * 5f;

                if (!effect.isPlaying)
                    effect.Play();
            }
        }

        #endregion

        #region Equipment Integration

        private void ApplyEquippedItems()
        {
            if (!enableEquipmentVisuals || equipmentManager == null) return;

            var monster = GetMonsterFromCreature();
            if (monster?.Equipment == null) return;

            foreach (var equipment in monster.Equipment.Where(e => e.IsEquipped))
            {
                CreateEquipmentVisual(equipment);
            }
        }

        private void CheckForEquipmentChanges()
        {
            var monster = GetMonsterFromCreature();
            if (monster?.Equipment == null) return;

            // Check for newly equipped items
            foreach (var equipment in monster.Equipment.Where(e => e.IsEquipped))
            {
                if (!equippedVisuals.ContainsKey(equipment.Type))
                {
                    CreateEquipmentVisual(equipment);
                }
            }

            // Check for removed items
            var equippedTypes = monster.Equipment.Where(e => e.IsEquipped)
                                              .Select(e => e.Type)
                                              .ToHashSet();

            var visualsToRemove = equippedVisuals.Keys.Where(type => !equippedTypes.Contains(type)).ToList();
            foreach (var type in visualsToRemove)
            {
                RemoveEquipmentVisual(type);
            }
        }


        private Monster GetMonsterFromCreature()
        {
            // Convert CreatureInstanceComponent to Monster for equipment system
            // This would need proper integration with your monster system
            if (creatureInstance?.CreatureData == null) return null;

            return new Monster
            {
                UniqueId = creatureInstance.CreatureData.UniqueId,
                Name = creatureInstance.CreatureData.Definition?.speciesName ?? "Unnamed Chimera",
                Level = 1, // Would get from creature level system
                Equipment = new List<EquipmentItem>()
            };
        }

        #endregion

        #region Color Customization

        public void ApplyCustomColors(Dictionary<string, Color> colorScheme)
        {
            foreach (var colorEntry in colorScheme)
            {
                ApplyColorToTarget(colorEntry.Key, colorEntry.Value);
            }

            // Update customization data
            currentCustomization.ColorOverrides = new Dictionary<string, Color>(colorScheme);
        }

        public void SetPrimaryColor(Color color)
        {
            ApplyColorToTarget("primary", color);
            currentCustomization.ColorOverrides["primary"] = color;
        }

        public void SetSecondaryColor(Color color)
        {
            ApplyColorToTarget("secondary", color);
            currentCustomization.ColorOverrides["secondary"] = color;
        }

        public void SetAccentColor(Color color)
        {
            ApplyColorToTarget("accent", color);
            currentCustomization.ColorOverrides["accent"] = color;
        }

        private void ApplyColorToTarget(string target, Color color)
        {
            if (customizableRenderers == null) return;

            foreach (var renderer in customizableRenderers)
            {
                if (renderer?.material == null) continue;

                string propertyName = target switch
                {
                    "primary" => "_Color",
                    "secondary" => "_SecondaryColor",
                    "accent" => "_AccentColor",
                    _ => "_Color"
                };

                if (renderer.material.HasProperty(propertyName))
                {
                    renderer.material.SetColor(propertyName, color);
                }
            }
        }

        #endregion

        #region Save/Load System

        public void SaveCustomization()
        {
            if (currentCustomization == null) return;

            string json = JsonUtility.ToJson(currentCustomization, true);
            string key = $"ChimeraCustomization_{currentCustomization.CreatureId}";
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();

            if (config?.EnableDebugLogging == true)
            {
                UnityEngine.Debug.Log($"💾 Saved customization for {currentCustomization.CreatureId}");
            }
        }

        public bool LoadCustomization(string creatureId = null)
        {
            string targetId = creatureId ?? creatureInstance?.CreatureData?.UniqueId;
            if (string.IsNullOrEmpty(targetId)) return false;

            string key = $"ChimeraCustomization_{targetId}";
            if (!PlayerPrefs.HasKey(key)) return false;

            try
            {
                string json = PlayerPrefs.GetString(key);
                var customization = JsonUtility.FromJson<ChimeraCustomizationData>(json);

                if (customization != null)
                {
                    ApplyCustomizationData(customization);
                    return true;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to load customization for {targetId}: {e.Message}");
            }

            return false;
        }

        public void ApplyCustomizationData(ChimeraCustomizationData customization)
        {
            if (customization == null) return;

            currentCustomization = customization;

            // Apply genetic appearance
            if (customization.GeneticAppearance != null)
            {
                ApplyGeneticAppearance();
            }

            // Apply equipped items
            foreach (var equippedItem in customization.EquippedItems)
            {
                // Load and apply equipped item visual
                ApplyEquippedItemVisual(equippedItem);
            }

            // Apply custom outfit
            if (customization.CustomOutfit != null)
            {
                ApplyCustomOutfit(customization.CustomOutfit);
            }

            // Apply color overrides
            if (customization.ColorOverrides != null)
            {
                ApplyCustomColors(customization.ColorOverrides);
            }

            if (config?.EnableDebugLogging == true)
            {
                UnityEngine.Debug.Log($"🎭 Applied customization data for {customization.CreatureId}");
            }
        }

        private void ApplyEquippedItemVisual(EquippedItemVisual equippedItem)
        {
            if (string.IsNullOrEmpty(equippedItem.VisualPrefabPath)) return;

            GameObject visualPrefab = Resources.Load<GameObject>(equippedItem.VisualPrefabPath);
            if (visualPrefab == null) return;

            Transform parent = GetEquipmentParent(equippedItem.EquipmentType);
            GameObject visual = Instantiate(visualPrefab, parent);

            // Apply stored modifications
            ApplyStoredModifications(visual, equippedItem.Modifications);

            equippedVisuals[equippedItem.EquipmentType] = visual;
        }

        private void ApplyStoredModifications(GameObject visual, Dictionary<string, object> modifications)
        {
            foreach (var modification in modifications)
            {
                // Apply stored visual modifications
                switch (modification.Key)
                {
                    case "scale":
                        if (modification.Value is Vector3 scale)
                            visual.transform.localScale = scale;
                        break;
                    case "color":
                        if (modification.Value is Color color)
                            ApplyColorToVisual(visual, color);
                        break;
                    // Add more modification types as needed
                }
            }
        }

        private void ApplyColorToVisual(GameObject visual, Color color)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.color = color;
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Reset all customizations to genetic defaults
        /// </summary>
        public void ResetToGeneticDefaults()
        {
            RemoveAllCustomOutfits();
            ClearAllEquipmentVisuals();

            currentCustomization.GeneticAppearance = GenerateGeneticAppearance();
            currentCustomization.EquippedItems.Clear();
            currentCustomization.CustomOutfit = null;
            currentCustomization.ColorOverrides.Clear();

            ApplyGeneticAppearance();

            if (config?.EnableDebugLogging == true)
            {
                UnityEngine.Debug.Log($"🔄 Reset {creatureInstance.CreatureData.UniqueId} to genetic defaults");
            }
        }

        /// <summary>
        /// Get list of all available equipment visuals for this chimera
        /// </summary>
        public List<string> GetAvailableEquipmentVisuals(Laboratory.Core.MonsterTown.EquipmentType equipmentType)
        {
            var availableVisuals = new List<string>();
            // Implementation would scan Resources folder for available equipment visuals
            // This is a placeholder for the actual implementation
            return availableVisuals;
        }

        /// <summary>
        /// Get list of all available custom outfit pieces
        /// </summary>
        public List<string> GetAvailableOutfitPieces(string category)
        {
            var availablePieces = new List<string>();
            // Implementation would scan Resources folder for available outfit pieces
            // This is a placeholder for the actual implementation
            return availablePieces;
        }

        private void ClearAllEquipmentVisuals()
        {
            foreach (var visual in equippedVisuals.Values)
            {
                if (visual != null)
                    DestroyImmediate(visual);
            }
            equippedVisuals.Clear();
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class ChimeraCustomizationData
    {
        public string CreatureId;
        public GeneticAppearanceData GeneticAppearance;
        public List<EquippedItemVisual> EquippedItems = new();
        public List<CustomPattern> CustomPatterns = new();
        public CustomOutfitData CustomOutfit;
        public Dictionary<string, Color> ColorOverrides = new();
        public DateTime LastModified = DateTime.Now;
    }

    [System.Serializable]
    public class GeneticAppearanceData
    {
        public Vector3 BodyScale = Vector3.one;
        public Dictionary<string, Vector3> LimbProportions = new();
        public Color[] PrimaryColors = new Color[0];
        public Color[] SecondaryColors = new Color[0];
        public Color[] PatternColors = new Color[0];
        public string[] PatternTypes = new string[0];
        public float PatternIntensity = 0.5f;
        public MagicalAuraData MagicalAura = new();
    }

    [System.Serializable]
    public class MagicalAuraData
    {
        public bool HasAura = false;
        public Color AuraColor = Color.white;
        public float AuraIntensity = 0.5f;
        public string EffectType = "none";
    }

    [System.Serializable]
    public class EquippedItemVisual
    {
        public string ItemId;
        public Laboratory.Core.MonsterTown.EquipmentType EquipmentType;
        public string VisualPrefabPath;
        public Dictionary<string, object> Modifications = new();
    }

    [System.Serializable]
    public class CustomPattern
    {
        public string PatternName;
        public string PatternType;
        public Color PatternColor;
        public float Intensity;
        public Vector2 Scale = Vector2.one;
        public Vector2 Offset = Vector2.zero;
    }

    [System.Serializable]
    public class CustomOutfitData
    {
        public string OutfitName;
        public string Description;
        public OutfitPiece[] OutfitPieces;
        public Dictionary<string, Color> ColorScheme;
        public string Category = "custom";
    }

    [System.Serializable]
    public class OutfitPiece
    {
        public string PieceId;
        public string PieceName;
        public string Category; // "body", "head", "limbs", etc.
        public string PrefabPath;
        public string MaterialPath;
        public Vector3 Scale = Vector3.one;
        public Vector3 PositionOffset = Vector3.zero;
        public Vector3 RotationOffset = Vector3.zero;
        public Dictionary<string, object> Properties = new();
    }

    #endregion
}