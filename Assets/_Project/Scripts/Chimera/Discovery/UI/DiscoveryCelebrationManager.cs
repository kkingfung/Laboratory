using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Laboratory.Chimera.Discovery.Core;
using Laboratory.Chimera.Genetics.Core;
using CoreDiscoveryEvent = Laboratory.Chimera.Discovery.Core.DiscoveryEvent;
using CoreDiscoveryType = Laboratory.Chimera.Discovery.Core.DiscoveryType;

namespace Laboratory.Chimera.Discovery.UI
{
    /// <summary>
    /// Epic celebration manager for genetic discoveries
    /// Creates screenshot-worthy moments with particles, animations, and audio
    /// </summary>
    public class DiscoveryCelebrationManager : MonoBehaviour
    {
        [Header("Celebration UI")]
        [SerializeField] private Canvas _celebrationCanvas;
        [SerializeField] private GameObject _celebrationPanel;
        [SerializeField] private TextMeshProUGUI _discoveryTitle;
        [SerializeField] private TextMeshProUGUI _discoveryDescription;
        [SerializeField] private TextMeshProUGUI _rarityText;
        [SerializeField] private Image _discoveryIcon;
        [SerializeField] private Image _backgroundGlow;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _celebrationFireworks;
        [SerializeField] private ParticleSystem _goldenParticles;
        [SerializeField] private ParticleSystem _dnaHelixEffect;
        [SerializeField] private GameObject _lightBeamPrefab;
        [SerializeField] private Transform _effectsContainer;

        [Header("Animation")]
        [SerializeField] private AnimationCurve _popInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _glowPulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _celebrationDuration = 5.0f;
        [SerializeField] private float _fadeOutDuration = 2.0f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip[] _discoveryAudioClips;
        [SerializeField] private AudioClip _mythicalDiscoveryAudio;
        [SerializeField] private AudioClip _worldFirstAudio;

        [Header("Rarity Colors")]
        [SerializeField] private Color _commonColor = Color.white;
        [SerializeField] private Color _uncommonColor = Color.green;
        [SerializeField] private Color _rareColor = Color.blue;
        [SerializeField] private Color _epicColor = Color.magenta;
        [SerializeField] private Color _legendaryColor = Color.yellow;
        [SerializeField] private Color _mythicalColor = Color.red;

        [Header("Screen Shake")]
        [SerializeField] private float _maxShakeIntensity = 2.0f;
        [SerializeField] private float _shakeDuration = 1.0f;

        private Camera _mainCamera;
        private Vector3 _originalCameraPosition;
        private Coroutine _currentCelebration;
        private Queue<CoreDiscoveryEvent> _discoveryQueue = new Queue<CoreDiscoveryEvent>();

        private static DiscoveryCelebrationManager _instance;
        public static DiscoveryCelebrationManager Instance => _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (_mainCamera != null)
                _originalCameraPosition = _mainCamera.transform.position;

            // Ensure celebration canvas is initially hidden
            if (_celebrationCanvas != null)
                _celebrationCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Trigger epic celebration for a genetic discovery
        /// </summary>
        public void CelebrateDiscovery(CoreDiscoveryEvent discovery)
        {
            _discoveryQueue.Enqueue(discovery);

            if (_currentCelebration == null)
            {
                _currentCelebration = StartCoroutine(ProcessDiscoveryQueue());
            }
        }

        /// <summary>
        /// Process queued discoveries one by one
        /// </summary>
        private IEnumerator ProcessDiscoveryQueue()
        {
            while (_discoveryQueue.Count > 0)
            {
                var discovery = _discoveryQueue.Dequeue();
                yield return StartCoroutine(PlayCelebrationSequence(discovery));
                yield return new WaitForSeconds(1.0f); // Brief pause between celebrations
            }

            _currentCelebration = null;
        }

        /// <summary>
        /// Play the complete celebration sequence
        /// </summary>
        private IEnumerator PlayCelebrationSequence(CoreDiscoveryEvent discovery)
        {
            // Setup celebration
            SetupCelebrationUI(discovery);
            _celebrationCanvas.gameObject.SetActive(true);

            // Start with dramatic buildup
            yield return StartCoroutine(PlayDiscoveryBuildup(discovery));

            // Main celebration reveal
            yield return StartCoroutine(PlayMainCelebration(discovery));

            // Sustain the celebration
            yield return StartCoroutine(SustainCelebration(discovery));

            // Fade out gracefully
            yield return StartCoroutine(FadeOutCelebration());

            _celebrationCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Setup UI elements for the discovery
        /// </summary>
        private void SetupCelebrationUI(CoreDiscoveryEvent discovery)
        {
            // Set discovery details
            _discoveryTitle.text = discovery.DiscoveryName.ToString();
            _discoveryDescription.text = GenerateDiscoveryDescription(discovery);
            _rarityText.text = $"{discovery.Rarity.ToString().ToUpper()} DISCOVERY!";

            // Set colors based on rarity
            Color rarityColor = GetRarityColor(discovery.Rarity);
            _rarityText.color = rarityColor;
            _backgroundGlow.color = rarityColor;

            // Set discovery icon based on type
            _discoveryIcon.sprite = GetDiscoveryIcon(discovery.Type);

            // Initialize UI elements for animation
            _celebrationPanel.transform.localScale = Vector3.zero;
            _discoveryTitle.alpha = 0f;
            _discoveryDescription.alpha = 0f;
            _rarityText.alpha = 0f;
            _backgroundGlow.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0f);
        }

        /// <summary>
        /// Play dramatic buildup before the main reveal
        /// </summary>
        private IEnumerator PlayDiscoveryBuildup(CoreDiscoveryEvent discovery)
        {
            // Screen shake for impact
            if (discovery.CelebrationIntensity > 0.7f)
            {
                StartCoroutine(ScreenShake(discovery.CelebrationIntensity));
            }

            // Audio buildup
            if (_audioSource != null && _discoveryAudioClips.Length > 0)
            {
                AudioClip buildupClip = GetAudioClip(discovery);
                _audioSource.PlayOneShot(buildupClip);
            }

            // Particle pre-effects
            if (_goldenParticles != null)
            {
                var emission = _goldenParticles.emission;
                emission.rateOverTime = 50f * discovery.CelebrationIntensity;
                _goldenParticles.Play();
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// Play the main celebration reveal
        /// </summary>
        private IEnumerator PlayMainCelebration(CoreDiscoveryEvent discovery)
        {
            float intensity = discovery.CelebrationIntensity;

            // Panel pop-in animation
            yield return StartCoroutine(AnimatePanelPopIn(intensity));

            // Text reveals with stagger
            yield return StartCoroutine(AnimateTextReveals(intensity));

            // Massive particle explosion
            TriggerMainParticleEffects(discovery);

            // Special effects for rare discoveries
            if (discovery.Rarity >= DiscoveryRarity.Epic)
            {
                yield return StartCoroutine(PlayEpicEffects(discovery));
            }

            if (discovery.IsWorldFirst)
            {
                yield return StartCoroutine(PlayWorldFirstEffects());
            }
        }

        /// <summary>
        /// Sustain the celebration with pulsing effects
        /// </summary>
        private IEnumerator SustainCelebration(CoreDiscoveryEvent discovery)
        {
            float sustainTime = _celebrationDuration * discovery.CelebrationIntensity;
            float elapsed = 0f;

            while (elapsed < sustainTime)
            {
                // Pulsing glow effect
                float pulse = _glowPulseCurve.Evaluate((elapsed % 2f) / 2f);
                Color glowColor = _backgroundGlow.color;
                glowColor.a = 0.3f + (pulse * 0.7f);
                _backgroundGlow.color = glowColor;

                // Gentle particle effects
                UpdateSustainParticles(discovery, pulse);

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Gracefully fade out the celebration
        /// </summary>
        private IEnumerator FadeOutCelebration()
        {
            float elapsed = 0f;

            // Get initial values
            Vector3 initialScale = _celebrationPanel.transform.localScale;
            float initialTitleAlpha = _discoveryTitle.alpha;
            float initialDescAlpha = _discoveryDescription.alpha;
            float initialRarityAlpha = _rarityText.alpha;
            Color initialGlowColor = _backgroundGlow.color;

            while (elapsed < _fadeOutDuration)
            {
                float t = elapsed / _fadeOutDuration;
                float fadeAlpha = 1f - t;

                // Fade all UI elements
                _celebrationPanel.transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
                _discoveryTitle.alpha = initialTitleAlpha * fadeAlpha;
                _discoveryDescription.alpha = initialDescAlpha * fadeAlpha;
                _rarityText.alpha = initialRarityAlpha * fadeAlpha;

                Color glowColor = initialGlowColor;
                glowColor.a *= fadeAlpha;
                _backgroundGlow.color = glowColor;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Stop all particle effects
            StopAllParticleEffects();
        }

        /// <summary>
        /// Animate panel pop-in with intensity-based scaling
        /// </summary>
        private IEnumerator AnimatePanelPopIn(float intensity)
        {
            float duration = 0.8f / intensity; // Faster for more intense discoveries
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float scale = _popInCurve.Evaluate(t) * intensity;
                _celebrationPanel.transform.localScale = Vector3.one * scale;

                elapsed += Time.deltaTime;
                yield return null;
            }

            _celebrationPanel.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Animate text reveals with staggered timing
        /// </summary>
        private IEnumerator AnimateTextReveals(float intensity)
        {
            // Rarity text first (dramatic impact)
            yield return StartCoroutine(FadeInText(_rarityText, 0.3f));

            yield return new WaitForSeconds(0.2f / intensity);

            // Discovery title
            yield return StartCoroutine(FadeInText(_discoveryTitle, 0.4f));

            yield return new WaitForSeconds(0.3f / intensity);

            // Description last
            yield return StartCoroutine(FadeInText(_discoveryDescription, 0.6f));
        }

        /// <summary>
        /// Fade in text element
        /// </summary>
        private IEnumerator FadeInText(TextMeshProUGUI text, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                text.alpha = elapsed / duration;
                elapsed += Time.deltaTime;
                yield return null;
            }

            text.alpha = 1f;
        }

        /// <summary>
        /// Trigger main particle effects explosion
        /// </summary>
        private void TriggerMainParticleEffects(CoreDiscoveryEvent discovery)
        {
            Color rarityColor = GetRarityColor(discovery.Rarity);

            // Celebration fireworks
            if (_celebrationFireworks != null)
            {
                var main = _celebrationFireworks.main;
                main.startColor = rarityColor;
                var emission = _celebrationFireworks.emission;
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0.0f, (short)(50 * discovery.CelebrationIntensity))
                });
                _celebrationFireworks.Play();
            }

            // DNA helix effect for genetic discoveries
            if (_dnaHelixEffect != null)
            {
                var main = _dnaHelixEffect.main;
                main.startColor = rarityColor;
                main.startSize = 2f * discovery.CelebrationIntensity;
                _dnaHelixEffect.Play();
            }

            // Golden particles intensity
            if (_goldenParticles != null)
            {
                var emission = _goldenParticles.emission;
                emission.rateOverTime = 100f * discovery.CelebrationIntensity;
                var main = _goldenParticles.main;
                main.startColor = Color.Lerp(Color.white, rarityColor, 0.7f);
            }
        }

        /// <summary>
        /// Play special effects for epic+ discoveries
        /// </summary>
        private IEnumerator PlayEpicEffects(CoreDiscoveryEvent discovery)
        {
            // Spawn light beams
            if (_lightBeamPrefab != null && _effectsContainer != null)
            {
                int beamCount = (int)discovery.Rarity + 1;
                for (int i = 0; i < beamCount; i++)
                {
                    GameObject beam = Instantiate(_lightBeamPrefab, _effectsContainer);
                    beam.transform.rotation = Quaternion.Euler(0, 0, i * (360f / beamCount));

                    // Auto-destroy after celebration
                    Destroy(beam, _celebrationDuration + _fadeOutDuration);
                }
            }

            // Enhanced screen shake for legendary+
            if (discovery.Rarity >= DiscoveryRarity.Legendary)
            {
                yield return StartCoroutine(ScreenShake(discovery.CelebrationIntensity * 1.5f));
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// Play special world-first discovery effects
        /// </summary>
        private IEnumerator PlayWorldFirstEffects()
        {
            // Special audio for world first
            if (_audioSource != null && _worldFirstAudio != null)
            {
                _audioSource.PlayOneShot(_worldFirstAudio);
            }

            // Rainbow particle effect
            if (_celebrationFireworks != null)
            {
                StartCoroutine(RainbowParticleEffect());
            }

            yield return new WaitForSeconds(1.0f);
        }

        /// <summary>
        /// Create rainbow particle effect for world firsts
        /// </summary>
        private IEnumerator RainbowParticleEffect()
        {
            float duration = 3.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float hue = (elapsed / duration) % 1f;
                Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);

                if (_celebrationFireworks != null)
                {
                    var main = _celebrationFireworks.main;
                    main.startColor = rainbowColor;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Screen shake for impact
        /// </summary>
        private IEnumerator ScreenShake(float intensity)
        {
            if (_mainCamera == null) yield break;

            float shakeIntensity = _maxShakeIntensity * intensity;
            float elapsed = 0f;

            while (elapsed < _shakeDuration)
            {
                float dampening = 1f - (elapsed / _shakeDuration);
                Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity * dampening;
                shakeOffset.z = 0f; // Keep camera on same Z plane

                _mainCamera.transform.position = _originalCameraPosition + shakeOffset;

                elapsed += Time.deltaTime;
                yield return null;
            }

            _mainCamera.transform.position = _originalCameraPosition;
        }

        /// <summary>
        /// Update sustain particles during celebration
        /// </summary>
        private void UpdateSustainParticles(CoreDiscoveryEvent discovery, float pulse)
        {
            if (_goldenParticles != null)
            {
                var emission = _goldenParticles.emission;
                emission.rateOverTime = (20f + pulse * 30f) * discovery.CelebrationIntensity;
            }
        }

        /// <summary>
        /// Stop all particle effects
        /// </summary>
        private void StopAllParticleEffects()
        {
            if (_celebrationFireworks != null) _celebrationFireworks.Stop();
            if (_goldenParticles != null) _goldenParticles.Stop();
            if (_dnaHelixEffect != null) _dnaHelixEffect.Stop();
        }

        /// <summary>
        /// Get color for discovery rarity
        /// </summary>
        private Color GetRarityColor(DiscoveryRarity rarity)
        {
            return rarity switch
            {
                DiscoveryRarity.Common => _commonColor,
                DiscoveryRarity.Uncommon => _uncommonColor,
                DiscoveryRarity.Rare => _rareColor,
                DiscoveryRarity.Epic => _epicColor,
                DiscoveryRarity.Legendary => _legendaryColor,
                DiscoveryRarity.Mythical => _mythicalColor,
                _ => Color.white
            };
        }

        /// <summary>
        /// Get audio clip for discovery
        /// </summary>
        private AudioClip GetAudioClip(CoreDiscoveryEvent discovery)
        {
            if (discovery.IsWorldFirst && _worldFirstAudio != null)
                return _worldFirstAudio;

            if (discovery.Rarity == DiscoveryRarity.Mythical && _mythicalDiscoveryAudio != null)
                return _mythicalDiscoveryAudio;

            if (_discoveryAudioClips.Length > 0)
            {
                int index = Mathf.Min((int)discovery.Rarity, _discoveryAudioClips.Length - 1);
                return _discoveryAudioClips[index];
            }

            return null;
        }

        /// <summary>
        /// Get icon sprite for discovery type
        /// </summary>
        private Sprite GetDiscoveryIcon(CoreDiscoveryType type)
        {
            // This would be connected to a sprite library
            // For now, return null - would need sprite assets
            return null;
        }

        /// <summary>
        /// Generate rich description for discovery
        /// </summary>
        private string GenerateDiscoveryDescription(CoreDiscoveryEvent discovery)
        {
            string baseDesc = discovery.Type switch
            {
                CoreDiscoveryType.NewTrait => "You've discovered a never-before-seen genetic combination!",
                CoreDiscoveryType.RareMutation => "A rare genetic mutation has emerged in your creature!",
                CoreDiscoveryType.SpecialMarker => "Special genetic markers have activated in this creature!",
                CoreDiscoveryType.PerfectGenetics => "Perfect genetic alignment achieved - a scientific breakthrough!",
                CoreDiscoveryType.NewSpecies => "A completely new species has been created through your breeding!",
                CoreDiscoveryType.LegendaryLineage => "Your breeding line has achieved legendary status!",
                _ => "An incredible genetic discovery has been made!"
            };

            if (discovery.IsWorldFirst)
                baseDesc += "\n<color=#FFD700>★ WORLD FIRST DISCOVERY! ★</color>";

            if (discovery.IsFirstTimeDiscovery)
                baseDesc += "\n<color=#00FF00>First time discovery bonus!</color>";

            return baseDesc;
        }

        /// <summary>
        /// Public API for external systems to trigger celebrations
        /// </summary>
        public static void TriggerCelebration(CoreDiscoveryEvent discovery)
        {
            if (Instance != null)
            {
                Instance.CelebrateDiscovery(discovery);
            }
        }
    }
}