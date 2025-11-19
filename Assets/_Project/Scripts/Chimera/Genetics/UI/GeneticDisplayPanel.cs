using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics.Visualization;
using System.Collections;

namespace Laboratory.Chimera.Genetics.UI
{
    /// <summary>
    /// UI panel for displaying creature genetics with beautiful visualizations
    /// Shows trait values, inheritance patterns, and breeding predictions
    /// </summary>
    public class GeneticDisplayPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private DNAHelixVisualizer _dnaVisualizer;
        [SerializeField] private Transform _traitContainer;
        [SerializeField] private GameObject _traitBarPrefab;
        [SerializeField] private TextMeshProUGUI _creatureName;
        [SerializeField] private TextMeshProUGUI _generationText;
        [SerializeField] private TextMeshProUGUI _rarityScore;

        [Header("Special Markers")]
        [SerializeField] private Transform _specialMarkersContainer;
        [SerializeField] private GameObject _markerIconPrefab;

        [Header("Breeding Prediction")]
        [SerializeField] private GameObject _breedingPredictionPanel;
        [SerializeField] private Transform _predictionContainer;
        [SerializeField] private TextMeshProUGUI _compatibilityScore;

        [Header("Animation Settings")]
        [SerializeField] private float _animationDuration = 1.0f;
        [SerializeField] private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private VisualGeneticData _currentGenetics;
        private TraitBar[] _traitBars;
        private readonly string[] _traitNames = { "Strength", "Vitality", "Agility", "Intelligence", "Adaptability", "Social" };

        /// <summary>
        /// Display creature genetics with smooth animations
        /// </summary>
        public void DisplayCreatureGenetics(VisualGeneticData genetics, string creatureName)
        {
            _currentGenetics = genetics;
            _creatureName.text = creatureName;

            StartCoroutine(AnimateGeneticDisplay());
        }

        /// <summary>
        /// Show breeding prediction between two creatures
        /// </summary>
        public void ShowBreedingPrediction(VisualGeneticData parent1, VisualGeneticData parent2)
        {
            _breedingPredictionPanel.SetActive(true);

            float compatibility = CalculateBreedingCompatibility(parent1, parent2);
            _compatibilityScore.text = $"Compatibility: {compatibility:P0}";

            StartCoroutine(AnimateBreedingPrediction(parent1, parent2));
        }

        /// <summary>
        /// Hide breeding prediction panel
        /// </summary>
        public void HideBreedingPrediction()
        {
            _breedingPredictionPanel.SetActive(false);
        }

        /// <summary>
        /// Animate the genetic display with smooth transitions
        /// </summary>
        private IEnumerator AnimateGeneticDisplay()
        {
            // Clear previous display
            ClearTraitBars();
            ClearSpecialMarkers();

            // Set basic info
            _generationText.text = $"Generation {_currentGenetics.GenerationCount}";
            _rarityScore.text = $"Rarity: {VisualGeneticUtility.GetRarityScore(_currentGenetics):P0}";

            // Animate DNA helix
            if (_dnaVisualizer != null)
            {
                _dnaVisualizer.DisplayGenetics(_currentGenetics);
            }

            // Create trait bars with animation
            yield return StartCoroutine(CreateAnimatedTraitBars());

            // Show special markers
            yield return StartCoroutine(DisplaySpecialMarkers());
        }

        /// <summary>
        /// Create trait bars with smooth animation
        /// </summary>
        private IEnumerator CreateAnimatedTraitBars()
        {
            _traitBars = new TraitBar[6];

            for (int i = 0; i < 6; i++)
            {
                GameObject traitBarObj = Instantiate(_traitBarPrefab, _traitContainer);
                TraitBar traitBar = traitBarObj.GetComponent<TraitBar>();

                if (traitBar != null)
                {
                    _traitBars[i] = traitBar;

                    byte traitValue = _currentGenetics.GetTraitValue(_traitNames[i]);
                    TraitAllele alleles = _currentGenetics.GetTraitAlleles(_traitNames[i]);
                    Color traitColor = _dnaVisualizer?.GetTraitColor(i, traitValue) ?? Color.white;

                    traitBar.Initialize(_traitNames[i], traitValue, alleles, traitColor);

                    // Animate trait bar appearance
                    yield return StartCoroutine(traitBar.AnimateAppearance(_animationDuration, _animationCurve));
                }

                // Small delay between trait bars for wave effect
                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// Display special genetic markers with icons
        /// </summary>
        private IEnumerator DisplaySpecialMarkers()
        {
            if (_currentGenetics.SpecialMarkers == GeneticMarkerFlags.None)
                yield break;

            var markerData = new[]
            {
                (GeneticMarkerFlags.Bioluminescent, "🔆", "Bioluminescent"),
                (GeneticMarkerFlags.CamouflageGene, "🎭", "Camouflage"),
                (GeneticMarkerFlags.PackLeader, "👑", "Pack Leader"),
                (GeneticMarkerFlags.SeasonalAdaptation, "🍂", "Seasonal"),
                (GeneticMarkerFlags.HybridVigor, "💪", "Hybrid Vigor"),
                (GeneticMarkerFlags.RareLineage, "⭐", "Rare Lineage"),
                (GeneticMarkerFlags.MutationCarrier, "🧬", "Mutation"),
                (GeneticMarkerFlags.ElementalAffinity, "🔥", "Elemental")
            };

            foreach (var (flag, icon, name) in markerData)
            {
                if (_currentGenetics.SpecialMarkers.HasMarker(flag))
                {
                    GameObject markerObj = Instantiate(_markerIconPrefab, _specialMarkersContainer);

                    // Set icon and tooltip
                    TextMeshProUGUI iconText = markerObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (iconText != null)
                    {
                        iconText.text = icon;
                    }

                    // Add tooltip component
                    var tooltip = markerObj.GetComponent<TooltipTrigger>();
                    if (tooltip != null)
                    {
                        tooltip.SetTooltip(name, GetMarkerDescription(flag));
                    }

                    // Animate marker appearance
                    markerObj.transform.localScale = Vector3.zero;
                    StartCoroutine(AnimateMarkerScale(markerObj.transform));

                    yield return new WaitForSeconds(0.2f);
                }
            }
        }

        /// <summary>
        /// Animate breeding prediction visualization
        /// </summary>
        private IEnumerator AnimateBreedingPrediction(VisualGeneticData parent1, VisualGeneticData parent2)
        {
            // Clear previous predictions
            foreach (Transform child in _predictionContainer)
            {
                Destroy(child.gameObject);
            }

            // Calculate possible offspring traits
            for (int traitIndex = 0; traitIndex < 6; traitIndex++)
            {
                var outcomes = CalculateOffspringPossibilities(
                    parent1.GetTraitAlleles(_traitNames[traitIndex]),
                    parent2.GetTraitAlleles(_traitNames[traitIndex])
                );

                // Create prediction visualization
                GameObject predictionObj = new GameObject($"Trait_{traitIndex}_Prediction");
                predictionObj.transform.SetParent(_predictionContainer);

                // Add prediction bars showing possible outcomes
                // This could be expanded to show Punnett square-style predictions

                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// Calculate breeding compatibility between two creatures
        /// </summary>
        private float CalculateBreedingCompatibility(VisualGeneticData parent1, VisualGeneticData parent2)
        {
            float compatibility = 1.0f;

            // Reduce compatibility for high inbreeding
            float avgInbreeding = (parent1.InbreedingCoefficient + parent2.InbreedingCoefficient) / 2f;
            compatibility -= avgInbreeding * 0.01f;

            // Increase compatibility for complementary traits
            float traitSynergy = 0f;
            for (int i = 0; i < 6; i++)
            {
                byte trait1 = parent1.GetTraitValue(_traitNames[i]);
                byte trait2 = parent2.GetTraitValue(_traitNames[i]);

                // Moderate differences are good for offspring
                float difference = Mathf.Abs(trait1 - trait2) / 100f;
                traitSynergy += Mathf.Clamp01(0.5f - Mathf.Abs(difference - 0.3f));
            }
            compatibility += traitSynergy / 6f * 0.3f;

            return Mathf.Clamp01(compatibility);
        }

        /// <summary>
        /// Calculate possible offspring trait outcomes
        /// </summary>
        private (byte min, byte max, float[] probabilities) CalculateOffspringPossibilities(TraitAllele allele1, TraitAllele allele2)
        {
            // Simplified Mendelian inheritance calculation
            byte[] possibleValues = {
                (byte)((allele1.DominantValue + allele2.DominantValue) / 2),  // Dom + Dom
                (byte)((allele1.DominantValue + allele2.RecessiveValue) / 2), // Dom + Rec
                (byte)((allele1.RecessiveValue + allele2.DominantValue) / 2), // Rec + Dom
                (byte)((allele1.RecessiveValue + allele2.RecessiveValue) / 2) // Rec + Rec
            };

            float[] probabilities = { 0.25f, 0.25f, 0.25f, 0.25f }; // Simplified equal probability

            byte min = possibleValues[0];
            byte max = possibleValues[0];
            foreach (byte value in possibleValues)
            {
                if (value < min) min = value;
                if (value > max) max = value;
            }

            return (min, max, probabilities);
        }

        /// <summary>
        /// Get description for genetic markers
        /// </summary>
        private string GetMarkerDescription(GeneticMarkerFlags marker)
        {
            return marker switch
            {
                GeneticMarkerFlags.Bioluminescent => "This creature can produce its own light",
                GeneticMarkerFlags.CamouflageGene => "Adaptive coloring for stealth and protection",
                GeneticMarkerFlags.PackLeader => "Natural leadership and social coordination abilities",
                GeneticMarkerFlags.SeasonalAdaptation => "Changes appearance and behavior with seasons",
                GeneticMarkerFlags.HybridVigor => "Enhanced traits from crossbreeding",
                GeneticMarkerFlags.RareLineage => "Descendant of an ancient or legendary bloodline",
                GeneticMarkerFlags.MutationCarrier => "Carries unique genetic mutations",
                GeneticMarkerFlags.ElementalAffinity => "Strong connection to environmental elements",
                _ => "Unknown genetic trait"
            };
        }

        /// <summary>
        /// Clear all trait bars
        /// </summary>
        private void ClearTraitBars()
        {
            foreach (Transform child in _traitContainer)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Clear special marker icons
        /// </summary>
        private void ClearSpecialMarkers()
        {
            foreach (Transform child in _specialMarkersContainer)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Animate marker scale with bounce effect
        /// </summary>
        private IEnumerator AnimateMarkerScale(Transform markerTransform)
        {
            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = _animationCurve.Evaluate(elapsed / _animationDuration);
                markerTransform.localScale = Vector3.one * t;
                yield return null;
            }
            markerTransform.localScale = Vector3.one;
        }
    }
}