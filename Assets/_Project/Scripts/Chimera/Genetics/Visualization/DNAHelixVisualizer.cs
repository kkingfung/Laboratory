using UnityEngine;
using Laboratory.Chimera.Genetics.Core;
using System.Collections;
using Unity.Mathematics;

namespace Laboratory.Chimera.Genetics.Visualization
{
    /// <summary>
    /// Creates beautiful 3D DNA helix visualizations for genetic data
    /// Shows trait inheritance, mutations, and special genetic markers
    /// </summary>
    public class DNAHelixVisualizer : MonoBehaviour
    {
        [Header("Helix Structure")]
        [SerializeField] private int _basePairCount = 100;
        [SerializeField] private float _helixRadius = 1.0f;
        [SerializeField] private float _helixHeight = 5.0f;
        [SerializeField] private float _rotationSpeed = 20f;

        [Header("Visual Elements")]
        [SerializeField] private GameObject _basePairPrefab;
        [SerializeField] private LineRenderer _strand1Renderer;
        [SerializeField] private LineRenderer _strand2Renderer;
        [SerializeField] private ParticleSystem _mutationParticles;
        [SerializeField] private ParticleSystem _specialMarkerEffect;

        [Header("Materials")]
        [SerializeField] private Material _dominantTraitMaterial;
        [SerializeField] private Material _recessiveTraitMaterial;
        [SerializeField] private Material _mutationMaterial;
        [SerializeField] private Material _specialMarkerMaterial;

        private VisualGeneticData _currentGeneticData;
        private GameObject[] _basePairs;
        private bool _isAnimating = false;

        private void Start()
        {
            InitializeHelix();
        }

        /// <summary>
        /// Display genetic data with beautiful visualization
        /// </summary>
        public void DisplayGenetics(VisualGeneticData geneticData)
        {
            _currentGeneticData = geneticData;
            StartCoroutine(AnimateGeneticVisualization());
        }

        /// <summary>
        /// Initialize the helix structure
        /// </summary>
        private void InitializeHelix()
        {
            _basePairs = new GameObject[_basePairCount];

            // Create base pairs along the helix
            for (int i = 0; i < _basePairCount; i++)
            {
                float t = (float)i / _basePairCount;
                float angle = t * 4 * Mathf.PI; // Two full rotations
                float height = t * _helixHeight;

                Vector3 position = new Vector3(
                    _helixRadius * Mathf.Cos(angle),
                    height,
                    _helixRadius * Mathf.Sin(angle)
                );

                _basePairs[i] = Instantiate(_basePairPrefab, transform);
                _basePairs[i].transform.localPosition = position;
                _basePairs[i].transform.LookAt(transform.position + Vector3.up * height);
            }

            CreateHelixStrands();
        }

        /// <summary>
        /// Create the double helix strands using LineRenderer
        /// </summary>
        private void CreateHelixStrands()
        {
            Vector3[] strand1Points = new Vector3[_basePairCount];
            Vector3[] strand2Points = new Vector3[_basePairCount];

            for (int i = 0; i < _basePairCount; i++)
            {
                float t = (float)i / _basePairCount;
                float angle = t * 4 * Mathf.PI;
                float height = t * _helixHeight;

                strand1Points[i] = new Vector3(
                    _helixRadius * Mathf.Cos(angle),
                    height,
                    _helixRadius * Mathf.Sin(angle)
                );

                strand2Points[i] = new Vector3(
                    _helixRadius * Mathf.Cos(angle + Mathf.PI),
                    height,
                    _helixRadius * Mathf.Sin(angle + Mathf.PI)
                );
            }

            _strand1Renderer.positionCount = _basePairCount;
            _strand1Renderer.SetPositions(strand1Points);

            _strand2Renderer.positionCount = _basePairCount;
            _strand2Renderer.SetPositions(strand2Points);
        }

        /// <summary>
        /// Animate the genetic visualization with smooth transitions
        /// </summary>
        private IEnumerator AnimateGeneticVisualization()
        {
            if (_isAnimating) yield break;
            _isAnimating = true;

            // Animate helix colors based on genetic data
            yield return StartCoroutine(AnimateHelixColors());

            // Show trait inheritance patterns
            yield return StartCoroutine(AnimateTraitInheritance());

            // Display special markers
            yield return StartCoroutine(AnimateSpecialMarkers());

            // Show mutation effects
            if (_currentGeneticData.MutationCount > 0)
            {
                yield return StartCoroutine(AnimateMutations());
            }

            _isAnimating = false;
        }

        /// <summary>
        /// Animate helix colors based on genetic rarity and traits
        /// </summary>
        private IEnumerator AnimateHelixColors()
        {
            float rarityScore = VisualGeneticUtility.GetRarityScore(_currentGeneticData);

            // Set strand colors based on genetic data
            Color primaryColor = _currentGeneticData.PrimaryHelixColor.ToColor();
            Color secondaryColor = _currentGeneticData.SecondaryHelixColor.ToColor();

            // Add golden glow for rare genetics
            if (rarityScore > 0.7f)
            {
                primaryColor = Color.Lerp(primaryColor, Color.gold, 0.3f);
                secondaryColor = Color.Lerp(secondaryColor, Color.gold, 0.3f);
            }

            // Animate color transition
            float duration = 1.0f;
            float elapsed = 0f;
            Color originalPrimary = _strand1Renderer.startColor;
            Color originalSecondary = _strand2Renderer.startColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                _strand1Renderer.startColor = Color.Lerp(originalPrimary, primaryColor, t);
                _strand1Renderer.endColor = Color.Lerp(originalPrimary, primaryColor, t);
                _strand2Renderer.startColor = Color.Lerp(originalSecondary, secondaryColor, t);
                _strand2Renderer.endColor = Color.Lerp(originalSecondary, secondaryColor, t);

                yield return null;
            }
        }

        /// <summary>
        /// Show trait inheritance patterns along the helix
        /// </summary>
        private IEnumerator AnimateTraitInheritance()
        {
            // Color base pairs based on trait values
            for (int i = 0; i < _basePairs.Length; i++)
            {
                if (_basePairs[i] == null) continue;

                // Determine which trait this base pair represents
                int traitIndex = i % 6; // 6 core traits
                byte traitValue = VisualGeneticUtility.GetTraitValue(_currentGeneticData, traitIndex);
                TraitAllele alleles = VisualGeneticUtility.GetTraitAlleles(_currentGeneticData, traitIndex);

                // Get renderer and set material based on dominance
                Renderer renderer = _basePairs[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material materialToUse = alleles.IsDominantExpressed ?
                        _dominantTraitMaterial : _recessiveTraitMaterial;

                    renderer.material = materialToUse;

                    // Set color intensity based on trait strength
                    Color baseColor = materialToUse.color;
                    float intensity = traitValue / 100f;
                    renderer.material.color = new Color(
                        baseColor.r * intensity,
                        baseColor.g * intensity,
                        baseColor.b * intensity,
                        baseColor.a
                    );
                }

                // Small delay for wave effect
                yield return new WaitForSeconds(0.01f);
            }
        }

        /// <summary>
        /// Animate special genetic markers with particle effects
        /// </summary>
        private IEnumerator AnimateSpecialMarkers()
        {
            if (_currentGeneticData.SpecialMarkers == GeneticMarkerFlags.None)
                yield break;

            // Activate special particle effects based on markers
            if (_currentGeneticData.SpecialMarkers.HasMarker(GeneticMarkerFlags.Bioluminescent))
            {
                _specialMarkerEffect.startColor = Color.cyan;
                _specialMarkerEffect.Play();
            }

            if (_currentGeneticData.SpecialMarkers.HasMarker(GeneticMarkerFlags.RareLineage))
            {
                _specialMarkerEffect.startColor = Color.gold;
                _specialMarkerEffect.Play();
            }

            if (_currentGeneticData.SpecialMarkers.HasMarker(GeneticMarkerFlags.ElementalAffinity))
            {
                _specialMarkerEffect.startColor = Color.red;
                _specialMarkerEffect.Play();
            }

            yield return new WaitForSeconds(2.0f);
        }

        /// <summary>
        /// Show mutation effects with special highlighting
        /// </summary>
        private IEnumerator AnimateMutations()
        {
            // Find random base pairs to highlight as mutations
            int mutationsToShow = Mathf.Min(_currentGeneticData.MutationCount, 5);

            for (int i = 0; i < mutationsToShow; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, _basePairs.Length);

                if (_basePairs[randomIndex] != null)
                {
                    // Highlight mutation with special material and particles
                    Renderer renderer = _basePairs[randomIndex].GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = _mutationMaterial;
                    }

                    // Spawn mutation particles
                    _mutationParticles.transform.position = _basePairs[randomIndex].transform.position;
                    _mutationParticles.Play();

                    yield return new WaitForSeconds(0.3f);
                }
            }
        }

        /// <summary>
        /// Continuous rotation animation
        /// </summary>
        private void Update()
        {
            if (_rotationSpeed > 0)
            {
                transform.Rotate(0, _rotationSpeed * Time.deltaTime, 0);
            }
        }

        /// <summary>
        /// Get trait color for UI displays
        /// </summary>
        public Color GetTraitColor(int traitIndex, byte traitValue)
        {
            // Color coding for different traits
            Color[] traitColors = {
                Color.red,      // Strength
                Color.green,    // Vitality
                Color.blue,     // Agility
                Color.yellow,   // Intelligence
                Color.magenta,  // Adaptability
                Color.cyan      // Social
            };

            if (traitIndex < 0 || traitIndex >= traitColors.Length)
                return Color.white;

            Color baseColor = traitColors[traitIndex];
            float intensity = traitValue / 100f;

            return new Color(
                baseColor.r * intensity,
                baseColor.g * intensity,
                baseColor.b * intensity,
                1f
            );
        }
    }
}