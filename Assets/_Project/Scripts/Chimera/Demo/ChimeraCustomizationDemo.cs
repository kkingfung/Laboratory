using UnityEngine;
using System.Collections;
using Laboratory.Chimera.Customization;
using Laboratory.Core.Equipment;
using Laboratory.Core.Equipment.Types;

namespace Laboratory.Chimera.Demo
{
    /// <summary>
    /// Demo script showcasing the Chimera Customization System features.
    /// Demonstrates equipment, outfits, color customization, and genetic appearance.
    /// </summary>
    public class ChimeraCustomizationDemo : MonoBehaviour
    {
        #region Serialized Fields

        [Header("🎭 Demo Configuration")]
        [SerializeField] private bool autoStartDemo = true;
        [SerializeField] private float demoStepDelay = 2f;
        [SerializeField] private bool enableDemoUI = true;

        [Header("🧬 Demo Chimera")]
        [SerializeField] private GameObject chimeraPrefab;
        [SerializeField] private Transform spawnPoint;

        [Header("🎒 Demo Equipment")]
        [SerializeField] private string[] demoEquipmentIds = {
            "demo_armor_basic",
            "demo_weapon_sword",
            "demo_accessory_crown",
            "demo_riding_gear_saddle"
        };

        [Header("👗 Demo Outfits")]
        [SerializeField] private string[] demoOutfitIds = {
            "royal_outfit",
            "battle_outfit",
            "casual_outfit"
        };

        [Header("🌈 Demo Colors")]
        [SerializeField] private Color[] demoColors = {
            Color.red, Color.blue, Color.green, Color.yellow, Color.purple
        };

        #endregion

        #region Private Fields

        private GameObject currentChimera;
        private ChimeraCustomizationManager customizationManager;
        private EquipmentManager equipmentManager;

        private int currentDemoStep = 0;
        private bool demoRunning = false;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (autoStartDemo)
            {
                StartCoroutine(StartDemoSequence());
            }
        }

        private void Update()
        {
            HandleDemoInput();
        }

        #endregion

        #region Demo Sequence

        private IEnumerator StartDemoSequence()
        {
            demoRunning = true;
            currentDemoStep = 0;

            yield return new WaitForSeconds(1f);

            UnityEngine.Debug.Log("🎭 Starting Chimera Customization Demo!");

            // Step 1: Spawn demo chimera
            yield return StartCoroutine(DemoStep_SpawnChimera());
            yield return new WaitForSeconds(demoStepDelay);

            // Step 2: Demonstrate genetic appearance
            yield return StartCoroutine(DemoStep_GeneticAppearance());
            yield return new WaitForSeconds(demoStepDelay);

            // Step 3: Demonstrate equipment system
            yield return StartCoroutine(DemoStep_EquipmentSystem());
            yield return new WaitForSeconds(demoStepDelay);

            // Step 4: Demonstrate color customization
            yield return StartCoroutine(DemoStep_ColorCustomization());
            yield return new WaitForSeconds(demoStepDelay);

            // Step 5: Demonstrate custom outfits
            yield return StartCoroutine(DemoStep_CustomOutfits());
            yield return new WaitForSeconds(demoStepDelay);

            // Step 6: Demonstrate save/load
            yield return StartCoroutine(DemoStep_SaveLoad());
            yield return new WaitForSeconds(demoStepDelay);

            UnityEngine.Debug.Log("🎉 Chimera Customization Demo Complete!");
            demoRunning = false;
        }

        private IEnumerator DemoStep_SpawnChimera()
        {
            currentDemoStep = 1;
            UnityEngine.Debug.Log("📍 Demo Step 1: Spawning Chimera");

            if (chimeraPrefab != null && spawnPoint != null)
            {
                currentChimera = Instantiate(chimeraPrefab, spawnPoint.position, spawnPoint.rotation);

                // Get components
                customizationManager = currentChimera.GetComponent<ChimeraCustomizationManager>();
                equipmentManager = FindFirstObjectByType<EquipmentManager>();

                // Initialize if needed
                if (customizationManager != null && !customizationManager.IsInitialized)
                {
                    yield return new WaitForSeconds(0.5f); // Allow initialization
                }

                UnityEngine.Debug.Log("✅ Demo chimera spawned and initialized");
            }
            else
            {
                UnityEngine.Debug.LogWarning("⚠️ Chimera prefab or spawn point not assigned");
            }

            yield return null;
        }

        private IEnumerator DemoStep_GeneticAppearance()
        {
            currentDemoStep = 2;
            UnityEngine.Debug.Log("🧬 Demo Step 2: Genetic Appearance Demonstration");

            if (customizationManager != null)
            {
                UnityEngine.Debug.Log("Demonstrating genetic appearance generation...");

                // Show original genetic appearance
                yield return new WaitForSeconds(1f);

                // Regenerate appearance 3 times
                for (int i = 0; i < 3; i++)
                {
                    UnityEngine.Debug.Log($"Generating genetic variant {i + 1}...");
                    customizationManager.ResetToGeneticDefaults();
                    yield return new WaitForSeconds(1.5f);
                }

                UnityEngine.Debug.Log("✅ Genetic appearance demonstration complete");
            }

            yield return null;
        }

        private IEnumerator DemoStep_EquipmentSystem()
        {
            currentDemoStep = 3;
            UnityEngine.Debug.Log("🎒 Demo Step 3: Equipment System Demonstration");

            if (customizationManager != null && equipmentManager != null)
            {
                UnityEngine.Debug.Log("Demonstrating equipment system...");

                foreach (string equipmentId in demoEquipmentIds)
                {
                    UnityEngine.Debug.Log($"Equipping demo item: {equipmentId}");

                    // Create demo equipment item
                    var demoItem = CreateDemoEquipmentItem(equipmentId);
                    if (demoItem != null)
                    {
                        customizationManager.EquipItem(demoItem);
                        yield return new WaitForSeconds(1f);
                    }
                }

                UnityEngine.Debug.Log("Demonstrating equipment removal...");
                yield return new WaitForSeconds(1f);

                // Remove all equipment
                foreach (var equipmentType in System.Enum.GetValues(typeof(Laboratory.Core.MonsterTown.EquipmentType)))
                {
                    var type = (Laboratory.Core.MonsterTown.EquipmentType)equipmentType;
                    customizationManager.UnequipItem(type);
                    yield return new WaitForSeconds(0.5f);
                }

                UnityEngine.Debug.Log("✅ Equipment system demonstration complete");
            }

            yield return null;
        }

        private IEnumerator DemoStep_ColorCustomization()
        {
            currentDemoStep = 4;
            UnityEngine.Debug.Log("🌈 Demo Step 4: Color Customization Demonstration");

            if (customizationManager != null)
            {
                UnityEngine.Debug.Log("Demonstrating color customization...");

                foreach (Color color in demoColors)
                {
                    UnityEngine.Debug.Log($"Applying color: {color}");

                    customizationManager.SetPrimaryColor(color);
                    yield return new WaitForSeconds(0.8f);

                    customizationManager.SetSecondaryColor(Color.Lerp(color, Color.white, 0.5f));
                    yield return new WaitForSeconds(0.8f);

                    customizationManager.SetAccentColor(Color.Lerp(color, Color.black, 0.3f));
                    yield return new WaitForSeconds(0.8f);
                }

                UnityEngine.Debug.Log("✅ Color customization demonstration complete");
            }

            yield return null;
        }

        private IEnumerator DemoStep_CustomOutfits()
        {
            currentDemoStep = 5;
            UnityEngine.Debug.Log("👗 Demo Step 5: Custom Outfits Demonstration");

            if (customizationManager != null)
            {
                UnityEngine.Debug.Log("Demonstrating custom outfits...");

                foreach (string outfitId in demoOutfitIds)
                {
                    UnityEngine.Debug.Log($"Applying demo outfit: {outfitId}");

                    var demoOutfit = CreateDemoOutfit(outfitId);
                    if (demoOutfit != null)
                    {
                        customizationManager.ApplyCustomOutfit(demoOutfit);
                        yield return new WaitForSeconds(2f);
                    }
                }

                UnityEngine.Debug.Log("Removing all custom outfits...");
                customizationManager.RemoveAllCustomOutfits();
                yield return new WaitForSeconds(1f);

                UnityEngine.Debug.Log("✅ Custom outfits demonstration complete");
            }

            yield return null;
        }

        private IEnumerator DemoStep_SaveLoad()
        {
            currentDemoStep = 6;
            UnityEngine.Debug.Log("💾 Demo Step 6: Save/Load Demonstration");

            if (customizationManager != null)
            {
                UnityEngine.Debug.Log("Setting up custom appearance for save test...");

                // Apply some customizations
                customizationManager.SetPrimaryColor(Color.magenta);
                customizationManager.SetSecondaryColor(Color.cyan);

                var demoEquipment = CreateDemoEquipmentItem("demo_save_test_item");
                if (demoEquipment != null)
                {
                    customizationManager.EquipItem(demoEquipment);
                }

                yield return new WaitForSeconds(1f);

                UnityEngine.Debug.Log("Saving customization...");
                customizationManager.SaveCustomization();
                yield return new WaitForSeconds(1f);

                UnityEngine.Debug.Log("Resetting to defaults...");
                customizationManager.ResetToGeneticDefaults();
                yield return new WaitForSeconds(1f);

                UnityEngine.Debug.Log("Loading saved customization...");
                bool loaded = customizationManager.LoadCustomization();
                if (loaded)
                {
                    UnityEngine.Debug.Log("✅ Save/Load successful!");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("⚠️ Save/Load failed");
                }

                yield return new WaitForSeconds(1f);
                UnityEngine.Debug.Log("✅ Save/Load demonstration complete");
            }

            yield return null;
        }

        #endregion

        #region Demo Helpers

        private Laboratory.Core.MonsterTown.Equipment CreateDemoEquipmentItem(string itemId)
        {
            // Create a demo equipment item for testing
            var demoEquipment = new Laboratory.Core.MonsterTown.Equipment
            {
                ItemId = itemId,
                Name = $"Demo {itemId}",
                Description = $"Demo equipment item: {itemId}",
                Type = GetEquipmentTypeFromId(itemId),
                Rarity = Laboratory.Core.MonsterTown.EquipmentRarity.Rare,
                Level = 1,
                IsEquipped = false,
                StatBonuses = new System.Collections.Generic.Dictionary<Laboratory.Core.MonsterTown.StatType, float>
                {
                    [Laboratory.Core.MonsterTown.StatType.Strength] = 10f,
                    [Laboratory.Core.MonsterTown.StatType.Agility] = 5f
                },
                ActivityBonuses = new System.Collections.Generic.List<Laboratory.Core.MonsterTown.ActivityType>
                {
                    Laboratory.Core.MonsterTown.ActivityType.Combat
                }
            };

            return demoEquipment;
        }

        private Laboratory.Core.MonsterTown.EquipmentType GetEquipmentTypeFromId(string itemId)
        {
            if (itemId.Contains("armor"))
                return Laboratory.Core.MonsterTown.EquipmentType.Armor;
            if (itemId.Contains("weapon"))
                return Laboratory.Core.MonsterTown.EquipmentType.Weapon;
            if (itemId.Contains("accessory"))
                return Laboratory.Core.MonsterTown.EquipmentType.Accessory;
            if (itemId.Contains("riding"))
                return Laboratory.Core.MonsterTown.EquipmentType.Vehicle;

            return Laboratory.Core.MonsterTown.EquipmentType.Accessory;
        }

        private CustomOutfitData CreateDemoOutfit(string outfitId)
        {
            var outfit = new CustomOutfitData
            {
                OutfitName = outfitId,
                Description = $"Demo outfit: {outfitId}",
                OutfitPieces = new OutfitPiece[]
                {
                    new OutfitPiece
                    {
                        PieceId = $"{outfitId}_body",
                        PieceName = $"{outfitId} Body",
                        Category = "body",
                        PrefabPath = $"Outfits/Demo/{outfitId}_body",
                        Scale = Vector3.one,
                        PositionOffset = Vector3.zero,
                        RotationOffset = Vector3.zero
                    },
                    new OutfitPiece
                    {
                        PieceId = $"{outfitId}_head",
                        PieceName = $"{outfitId} Head",
                        Category = "head",
                        PrefabPath = $"Outfits/Demo/{outfitId}_head",
                        Scale = Vector3.one,
                        PositionOffset = Vector3.zero,
                        RotationOffset = Vector3.zero
                    }
                },
                ColorScheme = new System.Collections.Generic.Dictionary<string, Color>
                {
                    ["primary"] = GetOutfitColor(outfitId, "primary"),
                    ["secondary"] = GetOutfitColor(outfitId, "secondary"),
                    ["accent"] = GetOutfitColor(outfitId, "accent")
                }
            };

            return outfit;
        }

        private Color GetOutfitColor(string outfitId, string colorType)
        {
            // Generate demo colors based on outfit type
            switch (outfitId)
            {
                case "royal_outfit":
                    return colorType switch
                    {
                        "primary" => Color.blue,
                        "secondary" => Color.gold,
                        "accent" => Color.white,
                        _ => Color.white
                    };
                case "battle_outfit":
                    return colorType switch
                    {
                        "primary" => Color.red,
                        "secondary" => Color.black,
                        "accent" => Color.silver,
                        _ => Color.gray
                    };
                case "casual_outfit":
                    return colorType switch
                    {
                        "primary" => Color.green,
                        "secondary" => Color.brown,
                        "accent" => Color.yellow,
                        _ => Color.white
                    };
                default:
                    return Color.white;
            }
        }

        #endregion

        #region Demo Controls

        private void HandleDemoInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!demoRunning)
                {
                    StartCoroutine(StartDemoSequence());
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartDemo();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                ClearDemo();
            }

            // Individual demo step controls
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                StartCoroutine(DemoStep_SpawnChimera());
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                StartCoroutine(DemoStep_GeneticAppearance());
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                StartCoroutine(DemoStep_EquipmentSystem());
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                StartCoroutine(DemoStep_ColorCustomization());
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                StartCoroutine(DemoStep_CustomOutfits());
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                StartCoroutine(DemoStep_SaveLoad());
            }
        }

        public void RestartDemo()
        {
            StopAllCoroutines();
            ClearDemo();
            StartCoroutine(StartDemoSequence());
        }

        public void ClearDemo()
        {
            if (currentChimera != null)
            {
                DestroyImmediate(currentChimera);
                currentChimera = null;
            }

            customizationManager = null;
            demoRunning = false;
            currentDemoStep = 0;
        }

        #endregion

        #region GUI Display

        private void OnGUI()
        {
            if (!enableDemoUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");

            GUILayout.Label("🎭 Chimera Customization Demo");
            GUILayout.Space(10);

            if (demoRunning)
            {
                GUILayout.Label($"🔄 Running Demo Step {currentDemoStep}/6");
                GUILayout.Space(5);

                if (GUILayout.Button("Stop Demo"))
                {
                    StopAllCoroutines();
                    demoRunning = false;
                }
            }
            else
            {
                if (GUILayout.Button("▶️ Start Full Demo (Space)"))
                {
                    StartCoroutine(StartDemoSequence());
                }

                GUILayout.Space(10);
                GUILayout.Label("Individual Steps:");

                if (GUILayout.Button("1. Spawn Chimera"))
                    StartCoroutine(DemoStep_SpawnChimera());

                if (GUILayout.Button("2. Genetic Appearance"))
                    StartCoroutine(DemoStep_GeneticAppearance());

                if (GUILayout.Button("3. Equipment System"))
                    StartCoroutine(DemoStep_EquipmentSystem());

                if (GUILayout.Button("4. Color Customization"))
                    StartCoroutine(DemoStep_ColorCustomization());

                if (GUILayout.Button("5. Custom Outfits"))
                    StartCoroutine(DemoStep_CustomOutfits());

                if (GUILayout.Button("6. Save/Load"))
                    StartCoroutine(DemoStep_SaveLoad());
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🔄 Restart (R)"))
            {
                RestartDemo();
            }

            if (GUILayout.Button("🗑️ Clear (C)"))
            {
                ClearDemo();
            }

            GUILayout.Space(10);
            GUILayout.Label("Controls:");
            GUILayout.Label("Space - Start Demo");
            GUILayout.Label("1-6 - Individual Steps");
            GUILayout.Label("R - Restart");
            GUILayout.Label("C - Clear");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion
    }
}