using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Customization;
using Laboratory.Core.Equipment;
using Laboratory.Chimera.Visuals;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Customization;

namespace Laboratory.Chimera.Integration
{
    /// <summary>
    /// Integration system that connects Chimera Customization with existing equipment, visual, and genetic systems.
    /// Provides seamless bridging between different subsystems and ensures consistency.
    /// </summary>
    [System.Serializable]
    public class ChimeraCustomizationIntegrator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("🔗 System References")]
        [SerializeField] private ChimeraCustomizationManager customizationManager;
        [SerializeField] private EquipmentManager equipmentManager;
        [SerializeField] private ProceduralVisualSystem visualSystem;
        [SerializeField] private CreatureInstanceComponent creatureInstance;

        [Header("⚙️ Integration Settings")]
        [SerializeField] private bool autoIntegrateOnStart = true;
        [SerializeField] private bool enableRealTimeSync = true;
        [SerializeField] private float syncInterval = 1.0f;
        [SerializeField] private bool enableDebugMode = false;

        [Header("🎭 Customization Priorities")]
        [SerializeField] private CustomizationPriority defaultPriority = CustomizationPriority.Genetic;
        [SerializeField] private bool equipmentOverridesGenetics = true;
        [SerializeField] private bool outfitsOverrideEquipment = false;

        #endregion

        #region Private Fields

        private Dictionary<System.Type, ICustomizationSystem> registeredSystems = new();
        private List<ICustomizationIntegration> integrationHandlers = new();
        private bool isInitialized = false;
        private float lastSyncTime = 0f;

        // State tracking
        private string lastGeneticHash = "";
        private string lastEquipmentHash = "";
        private string lastCustomizationHash = "";

        #endregion

        #region Properties

        public bool IsInitialized => isInitialized;
        public ChimeraCustomizationManager CustomizationManager => customizationManager;
        public EquipmentManager EquipmentManager => equipmentManager;
        public ProceduralVisualSystem VisualSystem => visualSystem;
        public CreatureInstanceComponent CreatureInstance => creatureInstance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
            RegisterSystems();
        }

        private void Start()
        {
            if (autoIntegrateOnStart)
            {
                InitializeIntegration();
            }
        }

        private void Update()
        {
            if (isInitialized && enableRealTimeSync && Time.time - lastSyncTime > syncInterval)
            {
                SynchronizeSystems();
                lastSyncTime = Time.time;
            }
        }

        #endregion

        #region Initialization

        private void ValidateReferences()
        {
            // Auto-find components if not assigned
            if (customizationManager == null)
                customizationManager = GetComponent<ChimeraCustomizationManager>();

            if (equipmentManager == null)
                equipmentManager = FindFirstObjectByType<EquipmentManager>();

            if (visualSystem == null)
                visualSystem = GetComponent<ProceduralVisualSystem>();

            if (creatureInstance == null)
                creatureInstance = GetComponent<CreatureInstanceComponent>();

            // Validate critical components
            if (customizationManager == null)
                UnityEngine.Debug.LogError("ChimeraCustomizationIntegrator: ChimeraCustomizationManager not found!");

            if (creatureInstance == null)
                UnityEngine.Debug.LogError("ChimeraCustomizationIntegrator: CreatureInstanceComponent not found!");
        }

        private void RegisterSystems()
        {
            // Register all customization systems
            if (customizationManager != null)
                RegisterSystem<ChimeraCustomizationManager>(customizationManager);

            // Register other systems that implement ICustomizationSystem
            var customizationSystems = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var system in customizationSystems)
            {
                if (system is ICustomizationSystem customSystem && system != customizationManager)
                {
                    RegisterSystem(system.GetType(), customSystem);
                }
            }

            // Register integration handlers
            RegisterIntegrationHandlers();
        }

        private void RegisterSystem<T>(T system) where T : MonoBehaviour, ICustomizationSystem
        {
            registeredSystems[typeof(T)] = system;

            if (enableDebugMode)
            {
                UnityEngine.Debug.Log($"Registered customization system: {typeof(T).Name}");
            }
        }

        private void RegisterSystem(System.Type systemType, ICustomizationSystem system)
        {
            registeredSystems[systemType] = system;

            if (enableDebugMode)
            {
                UnityEngine.Debug.Log($"Registered customization system: {systemType.Name}");
            }
        }

        private void RegisterIntegrationHandlers()
        {
            // Register built-in integration handlers
            integrationHandlers.Add(new EquipmentVisualIntegration(this));
            integrationHandlers.Add(new GeneticAppearanceIntegration(this));
            integrationHandlers.Add(new CustomOutfitIntegration(this));
            integrationHandlers.Add(new ColorSystemIntegration(this));
        }

        public void InitializeIntegration()
        {
            if (isInitialized) return;

            // Initialize all integration handlers
            foreach (var handler in integrationHandlers)
            {
                try
                {
                    handler.Initialize();
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"Failed to initialize integration handler {handler.GetType().Name}: {e.Message}");
                }
            }

            // Perform initial synchronization
            SynchronizeSystems();

            isInitialized = true;

            if (enableDebugMode)
            {
                UnityEngine.Debug.Log("ChimeraCustomizationIntegrator: Integration initialized successfully");
            }
        }

        #endregion

        #region System Synchronization

        private void SynchronizeSystems()
        {
            if (!isInitialized || customizationManager == null) return;

            try
            {
                // Check for changes in each system
                bool geneticsChanged = CheckGeneticsChanges();
                bool equipmentChanged = CheckEquipmentChanges();
                bool customizationChanged = CheckCustomizationChanges();

                // Apply changes based on priority system
                if (geneticsChanged || equipmentChanged || customizationChanged)
                {
                    ApplyChangesWithPriority(geneticsChanged, equipmentChanged, customizationChanged);
                }

                // Update state hashes
                UpdateStateHashes();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error during system synchronization: {e.Message}");
            }
        }

        private bool CheckGeneticsChanges()
        {
            if (creatureInstance?.CreatureData?.GeneticProfile == null) return false;

            string currentHash = GenerateGeneticHash(creatureInstance.CreatureData.GeneticProfile);
            bool changed = currentHash != lastGeneticHash;

            if (changed && enableDebugMode)
            {
                UnityEngine.Debug.Log("Genetics changes detected");
            }

            return changed;
        }

        private bool CheckEquipmentChanges()
        {
            if (equipmentManager == null) return false;

            var monster = GetMonsterFromCreature();
            if (monster?.Equipment == null) return false;

            string currentHash = GenerateEquipmentHash(monster.Equipment);
            bool changed = currentHash != lastEquipmentHash;

            if (changed && enableDebugMode)
            {
                UnityEngine.Debug.Log("Equipment changes detected");
            }

            return changed;
        }

        private bool CheckCustomizationChanges()
        {
            if (customizationManager?.CurrentCustomization == null) return false;

            string currentHash = GenerateCustomizationHash(customizationManager.CurrentCustomization);
            bool changed = currentHash != lastCustomizationHash;

            if (changed && enableDebugMode)
            {
                UnityEngine.Debug.Log("Customization changes detected");
            }

            return changed;
        }

        private void ApplyChangesWithPriority(bool geneticsChanged, bool equipmentChanged, bool customizationChanged)
        {
            switch (defaultPriority)
            {
                case CustomizationPriority.Genetic:
                    ApplyGeneticPriority(geneticsChanged, equipmentChanged, customizationChanged);
                    break;
                case CustomizationPriority.Equipment:
                    ApplyEquipmentPriority(geneticsChanged, equipmentChanged, customizationChanged);
                    break;
                case CustomizationPriority.Custom:
                    ApplyCustomPriority(geneticsChanged, equipmentChanged, customizationChanged);
                    break;
            }
        }

        private void ApplyGeneticPriority(bool geneticsChanged, bool equipmentChanged, bool customizationChanged)
        {
            // Genetics has highest priority, then equipment, then custom outfits, then customization
            if (geneticsChanged)
            {
                SyncGeneticsToCustomization();
            }

            if (equipmentChanged && equipmentOverridesGenetics)
            {
                SyncEquipmentToCustomization();
            }

            if (customizationChanged && outfitsOverrideEquipment)
            {
                SyncOutfitToCustomization();
            }

            if (customizationChanged)
            {
                SyncCustomizationToVisuals();
            }
        }

        private void ApplyEquipmentPriority(bool geneticsChanged, bool equipmentChanged, bool customizationChanged)
        {
            // Equipment has highest priority, then custom outfits, then genetics
            if (equipmentChanged)
            {
                SyncEquipmentToCustomization();
            }
            else if (geneticsChanged)
            {
                SyncGeneticsToCustomization();
            }

            if (customizationChanged && outfitsOverrideEquipment)
            {
                SyncOutfitToCustomization();
            }

            if (customizationChanged)
            {
                SyncCustomizationToVisuals();
            }
        }

        private void ApplyCustomPriority(bool geneticsChanged, bool equipmentChanged, bool customizationChanged)
        {
            // Custom changes have highest priority (including custom outfits)
            if (customizationChanged)
            {
                if (outfitsOverrideEquipment)
                {
                    SyncOutfitToCustomization();
                }
                SyncCustomizationToVisuals();
            }
            else if (equipmentChanged)
            {
                SyncEquipmentToCustomization();
            }
            else if (geneticsChanged)
            {
                SyncGeneticsToCustomization();
            }
        }

        #endregion

        #region System Integration Methods

        public void SyncGeneticsToCustomization()
        {
            if (customizationManager == null) return;

            foreach (var handler in integrationHandlers)
            {
                if (handler is GeneticAppearanceIntegration geneticHandler)
                {
                    geneticHandler.SyncGeneticsToCustomization();
                }
            }
        }

        public void SyncEquipmentToCustomization()
        {
            if (customizationManager == null || equipmentManager == null) return;

            foreach (var handler in integrationHandlers)
            {
                if (handler is EquipmentVisualIntegration equipmentHandler)
                {
                    equipmentHandler.SyncEquipmentToCustomization();
                }
            }
        }

        public void SyncCustomizationToVisuals()
        {
            if (customizationManager == null) return;

            foreach (var handler in integrationHandlers)
            {
                handler.SyncToVisuals();
            }
        }

        public void SyncCustomizationToEquipment()
        {
            if (customizationManager == null || equipmentManager == null) return;

            foreach (var handler in integrationHandlers)
            {
                if (handler is EquipmentVisualIntegration equipmentHandler)
                {
                    equipmentHandler.SyncCustomizationToEquipment();
                }
            }
        }

        public void SyncOutfitToCustomization()
        {
            if (customizationManager == null) return;

            foreach (var handler in integrationHandlers)
            {
                if (handler is CustomOutfitIntegration outfitHandler)
                {
                    outfitHandler.SyncToVisuals();
                }
            }
        }

        #endregion

        #region Integration Events

        public void OnEquipmentChanged(Laboratory.Core.MonsterTown.Equipment equipment, bool equipped)
        {
            if (!isInitialized) return;

            if (enableDebugMode)
            {
                UnityEngine.Debug.Log($"Equipment {(equipped ? "equipped" : "unequipped")}: {equipment.Name}");
            }

            // Trigger equipment integration
            foreach (var handler in integrationHandlers)
            {
                if (handler is EquipmentVisualIntegration equipmentHandler)
                {
                    if (equipped)
                        equipmentHandler.OnEquipmentEquipped(equipment);
                    else
                        equipmentHandler.OnEquipmentUnequipped(equipment);
                }
            }
        }

        public void OnGeneticsChanged(GeneticProfile newProfile)
        {
            if (!isInitialized) return;

            if (enableDebugMode)
            {
                UnityEngine.Debug.Log("Genetics changed - triggering integration");
            }

            // Trigger genetic integration
            foreach (var handler in integrationHandlers)
            {
                if (handler is GeneticAppearanceIntegration geneticHandler)
                {
                    geneticHandler.OnGeneticsChanged(newProfile);
                }
            }
        }

        public void OnCustomizationChanged(ChimeraCustomizationData customization)
        {
            if (!isInitialized) return;

            if (enableDebugMode)
            {
                UnityEngine.Debug.Log("Customization changed - triggering integration");
            }

            // Trigger all relevant integrations
            foreach (var handler in integrationHandlers)
            {
                handler.OnCustomizationChanged(customization);
            }
        }

        #endregion

        #region Hash Generation

        private string GenerateGeneticHash(GeneticProfile genetics)
        {
            var hashElements = new List<string>();

            if (genetics.TraitExpressions != null)
            {
                foreach (var trait in genetics.TraitExpressions)
                {
                    hashElements.Add($"{trait.Key}:{trait.Value.Value:F3}");
                }
            }

            hashElements.Add($"ProfileId:{genetics.ProfileId}");
            hashElements.Sort();

            return string.Join("|", hashElements).GetHashCode().ToString();
        }

        private string GenerateEquipmentHash(List<Laboratory.Core.MonsterTown.Equipment> equipment)
        {
            var hashElements = new List<string>();

            foreach (var item in equipment.Where(e => e.IsEquipped))
            {
                hashElements.Add($"{item.ItemId}:{item.Level}:{item.IsEquipped}");
            }

            hashElements.Sort();
            return string.Join("|", hashElements).GetHashCode().ToString();
        }

        private string GenerateCustomizationHash(ChimeraCustomizationData customization)
        {
            var hashElements = new List<string>();

            hashElements.Add($"CreatureId:{customization.CreatureId}");

            if (customization.EquippedItems != null)
            {
                foreach (var item in customization.EquippedItems)
                {
                    hashElements.Add($"Equipped:{item.ItemId}:{item.EquipmentType}");
                }
            }

            if (customization.ColorOverrides != null)
            {
                foreach (var color in customization.ColorOverrides)
                {
                    hashElements.Add($"Color:{color.Key}:{color.Value}");
                }
            }

            if (customization.CustomOutfit != null)
            {
                hashElements.Add($"Outfit:{customization.CustomOutfit.OutfitName}");
            }

            hashElements.Sort();
            return string.Join("|", hashElements).GetHashCode().ToString();
        }

        private void UpdateStateHashes()
        {
            if (creatureInstance?.CreatureData?.GeneticProfile != null)
            {
                lastGeneticHash = GenerateGeneticHash(creatureInstance.CreatureData.GeneticProfile);
            }

            var monster = GetMonsterFromCreature();
            if (monster?.Equipment != null)
            {
                lastEquipmentHash = GenerateEquipmentHash(monster.Equipment);
            }

            if (customizationManager?.CurrentCustomization != null)
            {
                lastCustomizationHash = GenerateCustomizationHash(customizationManager.CurrentCustomization);
            }
        }

        #endregion

        #region Utility Methods

        public Laboratory.Core.MonsterTown.Monster GetMonsterFromCreature()
        {
            if (creatureInstance?.CreatureData == null) return null;

            return new Laboratory.Core.MonsterTown.Monster
            {
                UniqueId = creatureInstance.CreatureData.UniqueId,
                Name = creatureInstance.CreatureData.Definition?.speciesName ?? "Unnamed Chimera",
                Level = 1, // Would get from creature level system
                Equipment = new List<Laboratory.Core.MonsterTown.Equipment>()
            };
        }

        public T GetSystem<T>() where T : class, ICustomizationSystem
        {
            if (registeredSystems.TryGetValue(typeof(T), out var system))
            {
                return system as T;
            }
            return null;
        }

        public void RegisterCustomIntegration(ICustomizationIntegration integration)
        {
            if (!integrationHandlers.Contains(integration))
            {
                integrationHandlers.Add(integration);

                if (isInitialized)
                {
                    integration.Initialize();
                }
            }
        }

        public void UnregisterCustomIntegration(ICustomizationIntegration integration)
        {
            integrationHandlers.Remove(integration);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Force synchronization of all systems
        /// </summary>
        public void ForceSynchronization()
        {
            SynchronizeSystems();
        }

        /// <summary>
        /// Enable or disable real-time synchronization
        /// </summary>
        public void SetRealTimeSync(bool enabled)
        {
            enableRealTimeSync = enabled;
        }

        /// <summary>
        /// Set the customization priority
        /// </summary>
        public void SetCustomizationPriority(CustomizationPriority priority)
        {
            defaultPriority = priority;
        }

        /// <summary>
        /// Get integration status information
        /// </summary>
        public IntegrationStatus GetIntegrationStatus()
        {
            return new IntegrationStatus
            {
                IsInitialized = isInitialized,
                RegisteredSystemsCount = registeredSystems.Count,
                IntegrationHandlersCount = integrationHandlers.Count,
                LastSyncTime = lastSyncTime,
                RealTimeSyncEnabled = enableRealTimeSync
            };
        }

        #endregion
    }

    #region Integration Handler Interfaces

    public interface ICustomizationIntegration
    {
        void Initialize();
        void SyncToVisuals();
        void OnCustomizationChanged(ChimeraCustomizationData customization);
    }

    #endregion

    #region Integration Handlers

    public class EquipmentVisualIntegration : ICustomizationIntegration
    {
        private ChimeraCustomizationIntegrator integrator;

        public EquipmentVisualIntegration(ChimeraCustomizationIntegrator integrator)
        {
            this.integrator = integrator;
        }

        public void Initialize()
        {
            // Initialize equipment visual integration
        }

        public void SyncToVisuals()
        {
            // Sync equipment visuals to the creature
            SyncEquipmentToCustomization();
        }

        public void OnCustomizationChanged(ChimeraCustomizationData customization)
        {
            // Handle customization changes affecting equipment
            if (customization.EquippedItems != null)
            {
                foreach (var equippedItem in customization.EquippedItems)
                {
                    // Update equipment visual
                    UpdateEquipmentVisual(equippedItem);
                }
            }
        }

        public void SyncEquipmentToCustomization()
        {
            var monster = integrator.GetMonsterFromCreature();
            if (monster?.Equipment == null) return;

            foreach (var equipment in monster.Equipment.Where(e => e.IsEquipped))
            {
                integrator.CustomizationManager?.EquipItem(equipment, true); // Visual only
            }
        }

        public void SyncCustomizationToEquipment()
        {
            var customization = integrator.CustomizationManager?.CurrentCustomization;
            if (customization?.EquippedItems == null) return;

            var monster = integrator.GetMonsterFromCreature();
            if (monster == null) return;

            foreach (var equippedItem in customization.EquippedItems)
            {
                // Find equipment item and equip it
                var equipment = monster.Equipment?.FirstOrDefault(e => e.ItemId == equippedItem.ItemId);
                if (equipment != null)
                {
                    integrator.EquipmentManager?.EquipItem(monster, equipment.ItemId);
                }
            }
        }

        public void OnEquipmentEquipped(Laboratory.Core.MonsterTown.Equipment equipment)
        {
            integrator.CustomizationManager?.EquipItem(equipment, true);
        }

        public void OnEquipmentUnequipped(Laboratory.Core.MonsterTown.Equipment equipment)
        {
            integrator.CustomizationManager?.UnequipItem(equipment.Type);
        }

        private void UpdateEquipmentVisual(EquippedItemVisual equippedItem)
        {
            // Update visual representation of equipped item
        }
    }

    public class GeneticAppearanceIntegration : ICustomizationIntegration
    {
        private ChimeraCustomizationIntegrator integrator;

        public GeneticAppearanceIntegration(ChimeraCustomizationIntegrator integrator)
        {
            this.integrator = integrator;
        }

        public void Initialize()
        {
            // Initialize genetic appearance integration
        }

        public void SyncToVisuals()
        {
            // Sync genetic appearance to visual system
            SyncGeneticsToCustomization();
        }

        public void OnCustomizationChanged(ChimeraCustomizationData customization)
        {
            // Handle genetic appearance changes
            if (customization.GeneticAppearance != null)
            {
                ApplyGeneticAppearance(customization.GeneticAppearance);
            }
        }

        public void SyncGeneticsToCustomization()
        {
            var genetics = integrator.CreatureInstance?.CreatureData?.GeneticProfile;
            if (genetics == null) return;

            // Generate appearance from genetics and apply to customization
            var customizationManager = integrator.CustomizationManager;
            if (customizationManager != null)
            {
                // This would trigger the genetic appearance generation in the customization manager
                customizationManager.ResetToGeneticDefaults();
            }
        }

        public void OnGeneticsChanged(GeneticProfile newProfile)
        {
            SyncGeneticsToCustomization();
        }

        private void ApplyGeneticAppearance(GeneticAppearanceData appearance)
        {
            // Apply genetic appearance to visual system
            if (integrator.VisualSystem != null)
            {
                // This would integrate with the ProceduralVisualSystem
                // to apply genetic appearance data
            }
        }
    }

    public class CustomOutfitIntegration : ICustomizationIntegration
    {
        private ChimeraCustomizationIntegrator integrator;

        public CustomOutfitIntegration(ChimeraCustomizationIntegrator integrator)
        {
            this.integrator = integrator;
        }

        public void Initialize()
        {
            // Initialize custom outfit integration
        }

        public void SyncToVisuals()
        {
            // Sync custom outfits to visual system
            var customization = integrator.CustomizationManager?.CurrentCustomization;
            if (customization?.CustomOutfit != null)
            {
                ApplyCustomOutfit(customization.CustomOutfit);
            }
        }

        public void OnCustomizationChanged(ChimeraCustomizationData customization)
        {
            // Handle custom outfit changes
            if (customization.CustomOutfit != null)
            {
                ApplyCustomOutfit(customization.CustomOutfit);
            }
        }

        private void ApplyCustomOutfit(CustomOutfitData outfit)
        {
            // Apply custom outfit to the creature
            integrator.CustomizationManager?.ApplyCustomOutfit(outfit);
        }
    }

    public class ColorSystemIntegration : ICustomizationIntegration
    {
        private ChimeraCustomizationIntegrator integrator;

        public ColorSystemIntegration(ChimeraCustomizationIntegrator integrator)
        {
            this.integrator = integrator;
        }

        public void Initialize()
        {
            // Initialize color system integration
        }

        public void SyncToVisuals()
        {
            // Sync color customizations to visual system
            var customization = integrator.CustomizationManager?.CurrentCustomization;
            if (customization?.ColorOverrides != null)
            {
                ApplyColorOverrides(customization.ColorOverrides);
            }
        }

        public void OnCustomizationChanged(ChimeraCustomizationData customization)
        {
            // Handle color changes
            if (customization.ColorOverrides != null)
            {
                ApplyColorOverrides(customization.ColorOverrides);
            }
        }

        private void ApplyColorOverrides(Dictionary<string, Color> colorOverrides)
        {
            // Apply color overrides to the creature
            integrator.CustomizationManager?.ApplyCustomColors(colorOverrides);
        }
    }

    #endregion

    #region Supporting Data Structures

    public enum CustomizationPriority
    {
        Genetic,    // Genetics override everything else
        Equipment,  // Equipment overrides genetics
        Custom      // Custom settings override everything
    }

    [System.Serializable]
    public class IntegrationStatus
    {
        public bool IsInitialized;
        public int RegisteredSystemsCount;
        public int IntegrationHandlersCount;
        public float LastSyncTime;
        public bool RealTimeSyncEnabled;
    }

    #endregion
}