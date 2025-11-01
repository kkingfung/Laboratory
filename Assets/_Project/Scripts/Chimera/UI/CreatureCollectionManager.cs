using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
using Laboratory.Chimera;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.UI
{
    public class CreatureCollectionManager : MonoBehaviour
    {
        [Header("Collection Display")]
        [SerializeField] private Transform creatureGridContainer;
        [SerializeField] private GameObject creatureCardPrefab;

        [Header("Collection Settings")]
        [SerializeField] private int maxCreatures = 100;
        [SerializeField] private bool autoSaveCollection = true;

        // Collection data
        private List<CreatureInstanceComponent> collectedCreatures = new List<CreatureInstanceComponent>();
        private HashSet<string> creatureIds = new HashSet<string>();
        
        private List<CollectionCreature> creatureCollection = new List<CollectionCreature>();
        
        private void Start()
        {
            UnityEngine.Debug.Log("Collection Manager initialized");
        }
        
        public void ShowCollectionUI()
        {
            UnityEngine.Debug.Log("Showing collection UI");
        }
        
        public void HideCollectionUI()
        {
            UnityEngine.Debug.Log("Hiding collection UI");
        }

        // Missing methods that are referenced in UI
        public void OpenCreatureSelection(System.Action<CreatureInstanceComponent> onCreatureSelected)
        {
            UnityEngine.Debug.Log("Opening creature selection dialog");

            // Show selection UI with available creatures
            if (collectedCreatures.Count == 0)
            {
                UnityEngine.Debug.LogWarning("No creatures in collection to select from");
                onCreatureSelected?.Invoke(null);
                return;
            }

            // For now, return the first creature as a selection
            // In a full implementation, this would show a proper selection UI
            var selectedCreature = collectedCreatures.FirstOrDefault();
            onCreatureSelected?.Invoke(selectedCreature);
        }

        public List<CreatureInstanceComponent> GetCreatures()
        {
            UnityEngine.Debug.Log($"Getting creatures from collection ({collectedCreatures.Count} creatures)");
            return new List<CreatureInstanceComponent>(collectedCreatures);
        }

        public void AddCreatureToCollection(CreatureInstanceComponent creature)
        {
            if (creature == null)
            {
                UnityEngine.Debug.LogWarning("Cannot add null creature to collection");
                return;
            }

            string creatureId = creature.name ?? creature.GetInstanceID().ToString();

            if (creatureIds.Contains(creatureId))
            {
                UnityEngine.Debug.LogWarning($"Creature {creatureId} is already in collection");
                return;
            }

            if (collectedCreatures.Count >= maxCreatures)
            {
                UnityEngine.Debug.LogWarning($"Collection is full (max: {maxCreatures})");
                return;
            }

            collectedCreatures.Add(creature);
            creatureIds.Add(creatureId);

            UnityEngine.Debug.Log($"Added creature {creatureId} to collection. Total: {collectedCreatures.Count}");

            // Update display if container exists
            RefreshCollectionDisplay();

            // Auto-save if enabled
            if (autoSaveCollection)
            {
                SaveCollection();
            }
        }

        public void RemoveCreatureFromCollection(CreatureInstanceComponent creature)
        {
            if (creature == null) return;

            string creatureId = creature.name ?? creature.GetInstanceID().ToString();

            if (creatureIds.Remove(creatureId))
            {
                collectedCreatures.Remove(creature);
                UnityEngine.Debug.Log($"Removed creature {creatureId} from collection. Total: {collectedCreatures.Count}");
                RefreshCollectionDisplay();

                if (autoSaveCollection)
                {
                    SaveCollection();
                }
            }
        }

        private void RefreshCollectionDisplay()
        {
            // Clear existing display
            if (creatureGridContainer != null)
            {
                foreach (Transform child in creatureGridContainer)
                {
                    if (child != null)
                        Destroy(child.gameObject);
                }

                // Create cards for creatures
                foreach (var creature in collectedCreatures)
                {
                    CreateCreatureCard(creature);
                }
            }
        }

        private void CreateCreatureCard(CreatureInstanceComponent creature)
        {
            if (creatureCardPrefab == null || creatureGridContainer == null)
                return;

            var cardObject = Instantiate(creatureCardPrefab, creatureGridContainer);

            // Set up the card display
            var cardTexts = cardObject.GetComponentsInChildren<TextMeshProUGUI>();
            if (cardTexts.Length > 0)
            {
                cardTexts[0].text = creature.name ?? "Unknown Creature";
            }

            // Add click handler for selection
            var button = cardObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnCreatureCardClicked(creature));
            }
        }

        private void OnCreatureCardClicked(CreatureInstanceComponent creature)
        {
            UnityEngine.Debug.Log($"Selected creature: {creature.name}");
            // This could trigger selection events or show creature details
        }

        private void SaveCollection()
        {
            // In a full implementation, this would save to PlayerPrefs, file, or server
            UnityEngine.Debug.Log($"Saving collection with {collectedCreatures.Count} creatures");
        }

        private void LoadCollection()
        {
            // In a full implementation, this would load from saved data
            UnityEngine.Debug.Log("Loading creature collection");
        }
    }
    
    [System.Serializable]
    public class CollectionCreature
    {
        public string UniqueId;
        public string Name;
        public int Generation;
        public float GeneticPurity;
        public bool CanBreed;
        public bool IsFavorite;

        // Additional properties for UI compatibility
        public List<string> DominantTraits;
        public Color PrimaryColor;
        public int MutationCount;
        public bool HasMagicalTraits;
        public int Health;
        public float Happiness;
        public bool IsInStorage;
        public Laboratory.Chimera.Core.RarityLevel RarityLevel;
        public GameObject GameObjectInstance;

        public CollectionCreature(CreatureInstanceComponent creature)
        {
            if (creature?.CreatureData != null)
            {
                UniqueId = creature.CreatureData.UniqueId;
                Name = creature.name;
                Generation = creature.CreatureData.GeneticProfile?.Generation ?? 1;
                GeneticPurity = creature.CreatureData.GeneticProfile?.GetGeneticPurity() ?? 0f;
                CanBreed = creature.CanBreed;
                IsFavorite = false;

                // Initialize additional properties
                DominantTraits = new List<string>();
                PrimaryColor = Color.white;
                MutationCount = creature.CreatureData.GeneticProfile?.Mutations?.Count ?? 0;
                HasMagicalTraits = false;
                Health = creature.CreatureData.CurrentHealth;
                Happiness = creature.CreatureData.Happiness;
                IsInStorage = false;
                RarityLevel = Laboratory.Chimera.Core.RarityLevel.Common;
                GameObjectInstance = creature.gameObject;

                // Extract dominant traits from genetics
                if (creature.CreatureData.GeneticProfile?.Genes != null)
                {
                    foreach (var gene in creature.CreatureData.GeneticProfile.Genes)
                    {
                        if (gene.dominance > 0.7f)
                        {
                            DominantTraits.Add(gene.traitName);
                        }

                        if (gene.traitType == Laboratory.Core.Enums.TraitType.Elemental)
                        {
                            HasMagicalTraits = true;
                        }
                    }
                }
            }
        }
    }
}