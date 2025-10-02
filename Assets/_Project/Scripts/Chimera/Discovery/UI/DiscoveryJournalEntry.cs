using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Laboratory.Chimera.Discovery.Core;
using Laboratory.Chimera.Genetics.Core;

namespace Laboratory.Chimera.Discovery.UI
{
    /// <summary>
    /// Individual journal entry component for displaying discovery details
    /// Beautiful card-based design with animations and rich information
    /// </summary>
    public class DiscoveryJournalEntry : MonoBehaviour
    {
        [Header("Entry UI")]
        [SerializeField] private TextMeshProUGUI _discoveryTitle;
        [SerializeField] private TextMeshProUGUI _discoveryDescription;
        [SerializeField] private TextMeshProUGUI _rarityLabel;
        [SerializeField] private TextMeshProUGUI _dateLabel;
        [SerializeField] private TextMeshProUGUI _significanceScore;

        [Header("Visual Elements")]
        [SerializeField] private Image _backgroundCard;
        [SerializeField] private Image _rarityBorder;
        [SerializeField] private Image _discoveryIcon;
        [SerializeField] private Image _specialBadge;
        [SerializeField] private GameObject _worldFirstBanner;
        [SerializeField] private GameObject _firstTimeBanner;

        [Header("Genetic Display")]
        [SerializeField] private Transform _traitContainer;
        [SerializeField] private GameObject _miniTraitBarPrefab;
        [SerializeField] private TextMeshProUGUI _specialMarkersText;

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _animationDuration = 0.8f;
        [SerializeField] private GameObject _glowEffect;

        [Header("Rarity Styling")]
        [SerializeField] private Color _commonColor = Color.white;
        [SerializeField] private Color _uncommonColor = Color.green;
        [SerializeField] private Color _rareColor = Color.blue;
        [SerializeField] private Color _epicColor = Color.magenta;
        [SerializeField] private Color _legendaryColor = Color.yellow;
        [SerializeField] private Color _mythicalColor = Color.red;

        private DiscoveryEvent _discoveryData;
        private bool _isAnimating = false;

        /// <summary>
        /// Setup the journal entry with discovery data
        /// </summary>
        public void SetupEntry(DiscoveryEvent discovery)
        {
            _discoveryData = discovery;

            SetupBasicInfo();
            SetupVisualStyling();
            SetupGeneticDisplay();
            SetupSpecialIndicators();

            // Start invisible for animation
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Setup basic text information
        /// </summary>
        private void SetupBasicInfo()
        {
            if (_discoveryTitle != null)
                _discoveryTitle.text = _discoveryData.DiscoveryName.ToString();

            if (_discoveryDescription != null)
                _discoveryDescription.text = GenerateRichDescription();

            if (_rarityLabel != null)
                _rarityLabel.text = _discoveryData.Rarity.ToString().ToUpper();

            if (_dateLabel != null)
                _dateLabel.text = FormatDiscoveryDate();

            if (_significanceScore != null)
                _significanceScore.text = $"Score: {_discoveryData.SignificanceScore:F0}";
        }

        /// <summary>
        /// Setup visual styling based on rarity
        /// </summary>
        private void SetupVisualStyling()
        {
            Color rarityColor = GetRarityColor(_discoveryData.Rarity);

            // Apply rarity color to borders and accents
            if (_rarityBorder != null)
                _rarityBorder.color = rarityColor;

            if (_rarityLabel != null)
                _rarityLabel.color = rarityColor;

            if (_backgroundCard != null)
            {
                Color bgColor = rarityColor;
                bgColor.a = 0.1f; // Subtle background tint
                _backgroundCard.color = bgColor;
            }

            // Set discovery type icon
            if (_discoveryIcon != null)
                _discoveryIcon.sprite = GetDiscoveryTypeIcon(_discoveryData.Type);

            // Setup glow effect for high-rarity discoveries
            if (_glowEffect != null)
            {
                _glowEffect.SetActive(_discoveryData.Rarity >= DiscoveryRarity.Epic);
                if (_discoveryData.Rarity >= DiscoveryRarity.Epic)
                {
                    var glowImage = _glowEffect.GetComponent<Image>();
                    if (glowImage != null)
                        glowImage.color = rarityColor;
                }
            }
        }

        /// <summary>
        /// Setup genetic trait display
        /// </summary>
        private void SetupGeneticDisplay()
        {
            if (_traitContainer == null || _miniTraitBarPrefab == null) return;

            // Clear existing traits
            foreach (Transform child in _traitContainer)
            {
                Destroy(child.gameObject);
            }

            // Create mini trait bars
            var genetics = _discoveryData.DiscoveredGenetics;
            CreateMiniTraitBar("STR", genetics.Strength);
            CreateMiniTraitBar("VIT", genetics.Vitality);
            CreateMiniTraitBar("AGI", genetics.Agility);
            CreateMiniTraitBar("INT", genetics.Intelligence);
            CreateMiniTraitBar("ADP", genetics.Adaptability);
            CreateMiniTraitBar("SOC", genetics.Social);

            // Display special markers
            if (_specialMarkersText != null)
            {
                _specialMarkersText.text = FormatSpecialMarkers(_discoveryData.SpecialMarkers);
            }
        }

        /// <summary>
        /// Create a mini trait bar for genetic display
        /// </summary>
        private void CreateMiniTraitBar(string traitName, byte value)
        {
            GameObject barGO = Instantiate(_miniTraitBarPrefab, _traitContainer);

            // Setup trait bar components
            var nameText = barGO.transform.Find("TraitName")?.GetComponent<TextMeshProUGUI>();
            var valueText = barGO.transform.Find("TraitValue")?.GetComponent<TextMeshProUGUI>();
            var fillBar = barGO.transform.Find("FillBar")?.GetComponent<Image>();

            if (nameText != null) nameText.text = traitName;
            if (valueText != null) valueText.text = value.ToString();
            if (fillBar != null)
            {
                fillBar.fillAmount = value / 100f;
                fillBar.color = GetTraitColor(value);
            }
        }

        /// <summary>
        /// Setup special indicators (world first, first time, etc.)
        /// </summary>
        private void SetupSpecialIndicators()
        {
            if (_worldFirstBanner != null)
                _worldFirstBanner.SetActive(_discoveryData.IsWorldFirst);

            if (_firstTimeBanner != null)
                _firstTimeBanner.SetActive(_discoveryData.IsFirstTimeDiscovery && !_discoveryData.IsWorldFirst);

            if (_specialBadge != null)
            {
                bool showBadge = _discoveryData.Rarity >= DiscoveryRarity.Legendary ||
                               _discoveryData.IsWorldFirst ||
                               _discoveryData.SpecialMarkers != GeneticMarkerFlags.None;
                _specialBadge.gameObject.SetActive(showBadge);

                if (showBadge)
                {
                    _specialBadge.color = GetRarityColor(_discoveryData.Rarity);
                }
            }
        }

        /// <summary>
        /// Animate entry appearance
        /// </summary>
        public IEnumerator AnimateAppearance(AnimationCurve curve)
        {
            if (_isAnimating) yield break;
            _isAnimating = true;

            float elapsed = 0f;

            while (elapsed < _animationDuration)
            {
                float t = elapsed / _animationDuration;
                float curveValue = curve.Evaluate(t);

                // Scale animation
                transform.localScale = Vector3.one * curveValue;

                // Fade in
                if (_canvasGroup != null)
                    _canvasGroup.alpha = curveValue;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure final values
            transform.localScale = Vector3.one;
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            _isAnimating = false;

            // Highlight animation for high-rarity discoveries
            if (_discoveryData.Rarity >= DiscoveryRarity.Epic)
            {
                StartCoroutine(PlayHighlightAnimation());
            }
        }

        /// <summary>
        /// Play highlight animation for special discoveries
        /// </summary>
        private IEnumerator PlayHighlightAnimation()
        {
            Color originalColor = _rarityBorder != null ? _rarityBorder.color : Color.white;
            Color highlightColor = Color.white;

            // Pulse effect
            for (int i = 0; i < 3; i++)
            {
                // Fade to highlight
                float elapsed = 0f;
                float duration = 0.3f;

                while (elapsed < duration)
                {
                    float t = elapsed / duration;
                    Color currentColor = Color.Lerp(originalColor, highlightColor, t);

                    if (_rarityBorder != null) _rarityBorder.color = currentColor;

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Fade back to original
                elapsed = 0f;
                while (elapsed < duration)
                {
                    float t = elapsed / duration;
                    Color currentColor = Color.Lerp(highlightColor, originalColor, t);

                    if (_rarityBorder != null) _rarityBorder.color = currentColor;

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Generate rich description with context
        /// </summary>
        private string GenerateRichDescription()
        {
            string baseDesc = _discoveryData.Type switch
            {
                DiscoveryType.NewTrait => "Unprecedented genetic combination discovered through careful breeding.",
                DiscoveryType.RareMutation => "Spontaneous genetic mutation has created unique traits.",
                DiscoveryType.SpecialMarker => "Ancient genetic markers have awakened in this creature.",
                DiscoveryType.PerfectGenetics => "Perfect genetic harmony achieved - the pinnacle of breeding science.",
                DiscoveryType.NewSpecies => "A completely new species born from visionary genetic engineering.",
                DiscoveryType.LegendaryLineage => "This bloodline has transcended to legendary status.",
                _ => "A remarkable genetic discovery that advances our understanding."
            };

            // Add context based on significance
            if (_discoveryData.SignificanceScore > 1000)
                baseDesc += " This discovery will be remembered for generations.";
            else if (_discoveryData.SignificanceScore > 500)
                baseDesc += " A breakthrough of immense scientific value.";
            else if (_discoveryData.SignificanceScore > 100)
                baseDesc += " A significant contribution to genetic research.";

            return baseDesc;
        }

        /// <summary>
        /// Format discovery date
        /// </summary>
        private string FormatDiscoveryDate()
        {
            // Convert timestamp to readable date
            var dateTime = System.DateTimeOffset.FromUnixTimeSeconds(_discoveryData.DiscoveryTimestamp);
            return dateTime.ToString("MMM dd, yyyy");
        }

        /// <summary>
        /// Format special markers for display
        /// </summary>
        private string FormatSpecialMarkers(GeneticMarkerFlags markers)
        {
            if (markers == GeneticMarkerFlags.None)
                return "No special markers";

            var markerList = new System.Collections.Generic.List<string>();

            if (markers.HasFlag(GeneticMarkerFlags.Bioluminescent))
                markerList.Add("🌟 Bioluminescent");
            if (markers.HasFlag(GeneticMarkerFlags.CamouflageGene))
                markerList.Add("👁️ Camouflage");
            if (markers.HasFlag(GeneticMarkerFlags.PackLeader))
                markerList.Add("👑 Pack Leader");
            if (markers.HasFlag(GeneticMarkerFlags.SeasonalAdaptation))
                markerList.Add("🍂 Seasonal");
            if (markers.HasFlag(GeneticMarkerFlags.HybridVigor))
                markerList.Add("⚡ Hybrid Vigor");
            if (markers.HasFlag(GeneticMarkerFlags.RareLineage))
                markerList.Add("💎 Rare Lineage");
            if (markers.HasFlag(GeneticMarkerFlags.MutationCarrier))
                markerList.Add("🧬 Mutation");
            if (markers.HasFlag(GeneticMarkerFlags.ElementalAffinity))
                markerList.Add("🔥 Elemental");

            return string.Join(" | ", markerList);
        }

        /// <summary>
        /// Get color for trait value
        /// </summary>
        private Color GetTraitColor(byte value)
        {
            if (value >= 90) return Color.red;
            if (value >= 70) return Color.yellow;
            if (value >= 50) return Color.green;
            if (value >= 30) return Color.cyan;
            return Color.gray;
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
        /// Get icon sprite for discovery type
        /// </summary>
        private Sprite GetDiscoveryTypeIcon(DiscoveryType type)
        {
            // This would be connected to a sprite library
            // For now, return null - would need sprite assets
            return null;
        }

        /// <summary>
        /// Public API for external interaction
        /// </summary>
        public DiscoveryEvent GetDiscoveryData() => _discoveryData;
        public bool IsAnimating => _isAnimating;

        /// <summary>
        /// Handle entry selection/click
        /// </summary>
        public void OnEntryClicked()
        {
            // Could open detailed view, share to social, etc.
            Debug.Log($"Journal entry clicked: {_discoveryData.DiscoveryName}");

            // Example: Open detailed discovery view
            // DiscoveryDetailView.ShowDiscovery(_discoveryData);
        }
    }
}