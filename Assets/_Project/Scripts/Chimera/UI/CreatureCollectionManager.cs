using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Laboratory.Chimera;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.UI
{
    public class CreatureCollectionManager : MonoBehaviour
    {
        [Header("Collection Display")]
        [SerializeField] private Transform creatureGridContainer;
        [SerializeField] private GameObject creatureCardPrefab;
        
        private List<CollectionCreature> creatureCollection = new List<CollectionCreature>();
        
        private void Start()
        {
            Debug.Log("Collection Manager initialized");
        }
        
        public void ShowCollectionUI()
        {
            Debug.Log("Showing collection UI");
        }
        
        public void HideCollectionUI()
        {
            Debug.Log("Hiding collection UI");
        }

        // Missing methods that are referenced in UI
        public void OpenCreatureSelection(System.Action<CreatureInstanceComponent> onCreatureSelected)
        {
            Debug.Log("Opening creature selection dialog");
            // Placeholder implementation - would show a creature selection UI
            // For now, just call the callback with null
            onCreatureSelected?.Invoke(null);
        }

        public List<CreatureInstanceComponent> GetCreatures()
        {
            Debug.Log("Getting creatures from collection");
            // Placeholder - would return actual creature collection
            return new List<CreatureInstanceComponent>();
        }

        public void AddCreatureToCollection(CreatureInstanceComponent creature)
        {
            Debug.Log($"Adding creature {creature.name} to collection");
            // Placeholder - would add creature to the actual collection
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

                        if (gene.traitType == Laboratory.Chimera.Genetics.TraitType.Elemental)
                        {
                            HasMagicalTraits = true;
                        }
                    }
                }
            }
        }
    }
}