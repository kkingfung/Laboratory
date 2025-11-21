using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Laboratory.Chimera.Core;
using System.Collections;

namespace Laboratory.Chimera.Genetics.UI
{
    /// <summary>
    /// Individual trait display bar showing genetic values and inheritance
    /// Displays both dominant and recessive alleles with beautiful animations
    /// </summary>
    public class TraitBar : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI _traitNameText;
        [SerializeField] private TextMeshProUGUI _traitValueText;
        [SerializeField] private Slider _dominantSlider;
        [SerializeField] private Slider _recessiveSlider;
        [SerializeField] private Image _dominantFill;
        [SerializeField] private Image _recessiveFill;
        [SerializeField] private Image _backgroundImage;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _dominantParticles;
        [SerializeField] private ParticleSystem _recessiveParticles;
        [SerializeField] private Gradient _traitGradient;

        [Header("Animation")]
        [SerializeField] private RectTransform _containerRect;

        private TraitAllele _currentAlleles;
        private byte _displayedValue;
        private Color _traitColor;

        /// <summary>
        /// Initialize the trait bar with genetic data
        /// </summary>
        public void Initialize(string traitName, byte traitValue, TraitAllele alleles, Color traitColor)
        {
            _traitNameText.text = traitName;
            _displayedValue = traitValue;
            _currentAlleles = alleles;
            _traitColor = traitColor;

            SetupVisuals();
        }

        /// <summary>
        /// Setup visual appearance based on genetic data
        /// </summary>
        private void SetupVisuals()
        {
            // Set slider values (but don't animate yet)
            _dominantSlider.value = 0;
            _recessiveSlider.value = 0;

            // Set colors based on trait
            _dominantFill.color = _traitColor;
            _recessiveSlider.fillRect.GetComponent<Image>().color = new Color(_traitColor.r, _traitColor.g, _traitColor.b, 0.5f);

            // Set initial scale to zero for animation
            _containerRect.localScale = Vector3.zero;

            // Setup particle colors
            if (_dominantParticles != null)
            {
                var main = _dominantParticles.main;
                main.startColor = _traitColor;
            }

            if (_recessiveParticles != null)
            {
                var main = _recessiveParticles.main;
                main.startColor = new Color(_traitColor.r, _traitColor.g, _traitColor.b, 0.7f);
            }
        }

        /// <summary>
        /// Animate the appearance of this trait bar
        /// </summary>
        public IEnumerator AnimateAppearance(float duration, AnimationCurve curve)
        {
            // Scale in animation
            yield return StartCoroutine(AnimateScale(Vector3.one, duration * 0.5f, curve));

            // Animate trait values
            yield return StartCoroutine(AnimateTraitValues(duration * 0.5f));

            // Show dominance indicator
            ShowDominanceIndicator();
        }

        /// <summary>
        /// Animate scale with bounce effect
        /// </summary>
        private IEnumerator AnimateScale(Vector3 targetScale, float duration, AnimationCurve curve)
        {
            Vector3 startScale = _containerRect.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                _containerRect.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            _containerRect.localScale = targetScale;
        }

        /// <summary>
        /// Animate trait value sliders
        /// </summary>
        private IEnumerator AnimateTraitValues(float duration)
        {
            float elapsed = 0f;
            float targetDominant = _currentAlleles.DominantValue / 100f;
            float targetRecessive = _currentAlleles.RecessiveValue / 100f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Smooth step for better visual appeal
                t = Mathf.SmoothStep(0, 1, t);

                _dominantSlider.value = Mathf.Lerp(0, targetDominant, t);
                _recessiveSlider.value = Mathf.Lerp(0, targetRecessive, t);

                // Update text with current displayed value
                byte currentDisplayValue = (byte)Mathf.Lerp(0, _displayedValue, t);
                _traitValueText.text = $"{currentDisplayValue}/100";

                yield return null;
            }

            // Set final values
            _dominantSlider.value = targetDominant;
            _recessiveSlider.value = targetRecessive;
            _traitValueText.text = $"{_displayedValue}/100";
        }

        /// <summary>
        /// Show which allele is dominant with visual effects
        /// </summary>
        private void ShowDominanceIndicator()
        {
            if (_currentAlleles.IsDominantExpressed)
            {
                // Highlight dominant allele
                _dominantFill.color = _traitColor;

                if (_dominantParticles != null)
                {
                    _dominantParticles.Play();
                }

                // Add glow effect to dominant slider
                StartCoroutine(PulseGlow(_dominantFill, 1.0f));
            }
            else
            {
                // Highlight recessive allele
                var recessiveFill = _recessiveSlider.fillRect.GetComponent<Image>();
                recessiveFill.color = _traitColor;

                if (_recessiveParticles != null)
                {
                    _recessiveParticles.Play();
                }

                // Add glow effect to recessive slider
                StartCoroutine(PulseGlow(recessiveFill, 1.0f));
            }
        }

        /// <summary>
        /// Create pulsing glow effect for active allele
        /// </summary>
        private IEnumerator PulseGlow(Image targetImage, float duration)
        {
            Color originalColor = targetImage.color;
            Color glowColor = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float pulse = Mathf.Sin(elapsed * 4f) * 0.3f + 0.7f; // Pulse between 0.4 and 1.0

                targetImage.color = Color.Lerp(originalColor, glowColor, pulse);
                yield return null;
            }

            targetImage.color = originalColor;
        }

        /// <summary>
        /// Update trait bar when genetics change (for breeding predictions)
        /// </summary>
        public void UpdateGenetics(TraitAllele newAlleles, byte newValue)
        {
            _currentAlleles = newAlleles;
            _displayedValue = newValue;

            StartCoroutine(AnimateTraitValues(0.5f));
            ShowDominanceIndicator();
        }

        /// <summary>
        /// Show breeding compatibility with another trait
        /// </summary>
        public void ShowBreedingCompatibility(TraitAllele otherAlleles)
        {
            // Calculate compatibility and show visual feedback
            float compatibility = CalculateAlleleCompatibility(_currentAlleles, otherAlleles);

            // Change background color based on compatibility
            Color compatibilityColor = Color.Lerp(Color.red, Color.green, compatibility);
            StartCoroutine(FlashBackgroundColor(compatibilityColor, 0.5f));
        }

        /// <summary>
        /// Calculate compatibility between two allele pairs
        /// </summary>
        private float CalculateAlleleCompatibility(TraitAllele alleles1, TraitAllele alleles2)
        {
            // Good compatibility when traits are complementary but not too different
            float avgDifference = (
                Mathf.Abs(alleles1.DominantValue - alleles2.DominantValue) +
                Mathf.Abs(alleles1.RecessiveValue - alleles2.RecessiveValue)
            ) / 2f;

            // Optimal difference is around 20-30 points
            float normalizedDifference = avgDifference / 100f;
            float compatibility = 1f - Mathf.Abs(normalizedDifference - 0.25f) * 2f;

            return Mathf.Clamp01(compatibility);
        }

        /// <summary>
        /// Flash background color for visual feedback
        /// </summary>
        private IEnumerator FlashBackgroundColor(Color flashColor, float duration)
        {
            Color originalColor = _backgroundImage.color;

            // Flash to new color
            float elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                _backgroundImage.color = Color.Lerp(originalColor, flashColor, t);
                yield return null;
            }

            // Flash back to original
            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                _backgroundImage.color = Color.Lerp(flashColor, originalColor, t);
                yield return null;
            }

            _backgroundImage.color = originalColor;
        }

        /// <summary>
        /// Get the current genetic information for external access
        /// </summary>
        public (TraitAllele alleles, byte value) GetGeneticInfo()
        {
            return (_currentAlleles, _displayedValue);
        }
    }
}