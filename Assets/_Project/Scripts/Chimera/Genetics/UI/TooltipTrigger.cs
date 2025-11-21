using UnityEngine;
using UnityEngine.EventSystems;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Genetics.UI
{
    /// <summary>
    /// Tooltip trigger for showing detailed genetic information on hover
    /// Provides rich information about traits, alleles, and breeding outcomes
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Tooltip Content")]
        [SerializeField] private string _tooltipTitle;
        [SerializeField] private string _tooltipDescription;

        private static TooltipDisplay _tooltipDisplay;

        private void Start()
        {
            // Find tooltip display if not cached
            if (_tooltipDisplay == null)
            {
                _tooltipDisplay = FindFirstObjectByType<TooltipDisplay>();
            }
        }

        /// <summary>
        /// Show tooltip on pointer enter
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltipDisplay != null && !string.IsNullOrEmpty(_tooltipTitle))
            {
                _tooltipDisplay.ShowTooltip(_tooltipTitle, _tooltipDescription, Input.mousePosition);
            }
        }

        /// <summary>
        /// Hide tooltip on pointer exit
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltipDisplay != null)
            {
                _tooltipDisplay.HideTooltip();
            }
        }

        /// <summary>
        /// Set tooltip content dynamically
        /// </summary>
        public void SetTooltip(string title, string description)
        {
            _tooltipTitle = title;
            _tooltipDescription = description;
        }

        /// <summary>
        /// Set tooltip for genetic marker
        /// </summary>
        public void SetGeneticMarkerTooltip(GeneticMarkerFlags marker, string description)
        {
            _tooltipTitle = GetMarkerDisplayName(marker);
            _tooltipDescription = $"<color=#FFD700>{_tooltipTitle}</color>\n\n{description}";
        }

        /// <summary>
        /// Set tooltip for trait information
        /// </summary>
        public void SetTraitTooltip(string traitName, byte dominantValue, byte recessiveValue, bool isDominantExpressed)
        {
            _tooltipTitle = traitName;

            string expressedText = isDominantExpressed ? "Dominant" : "Recessive";
            string hiddenText = isDominantExpressed ? "Recessive" : "Dominant";
            byte expressedValue = isDominantExpressed ? dominantValue : recessiveValue;
            byte hiddenValue = isDominantExpressed ? recessiveValue : dominantValue;

            _tooltipDescription = $"<color=#FFD700>{traitName} Genetics</color>\n\n" +
                                 $"<color=#00FF00>Expressed ({expressedText}):</color> {expressedValue}/100\n" +
                                 $"<color=#888888>Hidden ({hiddenText}):</color> {hiddenValue}/100\n\n" +
                                 GetTraitDescription(traitName);
        }

        /// <summary>
        /// Set tooltip for breeding compatibility
        /// </summary>
        public void SetCompatibilityTooltip(float compatibility, string details)
        {
            _tooltipTitle = "Breeding Compatibility";

            Color compatibilityColor = compatibility > 0.7f ? Color.green :
                                     compatibility > 0.4f ? Color.yellow : Color.red;

            string colorHex = ColorUtility.ToHtmlStringRGB(compatibilityColor);

            _tooltipDescription = $"<color=#FFD700>Breeding Compatibility</color>\n\n" +
                                 $"<color=#{colorHex}>Compatibility: {compatibility:P0}</color>\n\n" +
                                 details;
        }

        /// <summary>
        /// Get display name for genetic markers
        /// </summary>
        private string GetMarkerDisplayName(GeneticMarkerFlags marker)
        {
            return marker switch
            {
                GeneticMarkerFlags.Bioluminescent => "Bioluminescent Gene",
                GeneticMarkerFlags.CamouflageGene => "Camouflage Adaptation",
                GeneticMarkerFlags.PackLeader => "Pack Leadership",
                GeneticMarkerFlags.SeasonalAdaptation => "Seasonal Adaptation",
                GeneticMarkerFlags.HybridVigor => "Hybrid Vigor",
                GeneticMarkerFlags.RareLineage => "Rare Bloodline",
                GeneticMarkerFlags.MutationCarrier => "Genetic Mutation",
                GeneticMarkerFlags.ElementalAffinity => "Elemental Affinity",
                _ => "Unknown Trait"
            };
        }

        /// <summary>
        /// Get detailed description for traits
        /// </summary>
        private string GetTraitDescription(string traitName)
        {
            return traitName switch
            {
                "Strength" => "Physical power affecting damage output, carrying capacity, and territorial behavior. High strength creatures are natural fighters and workers.",

                "Vitality" => "Health and endurance determining lifespan, disease resistance, and energy levels. Vital creatures live longer and recover faster from injuries.",

                "Agility" => "Speed and reflexes affecting movement, evasion, and reaction time. Agile creatures excel at hunting, escaping, and acrobatic feats.",

                "Intelligence" => "Learning ability and problem-solving determining training speed, tool use, and social complexity. Smart creatures adapt quickly and learn new behaviors.",

                "Adaptability" => "Environmental tolerance and stress resistance helping creatures survive in harsh conditions. Adaptable creatures thrive in diverse biomes.",

                "Social" => "Bonding capacity and pack behavior affecting breeding success, teamwork, and player relationships. Social creatures form strong communities.",

                _ => "This trait affects various aspects of creature behavior and abilities."
            };
        }
    }

    /// <summary>
    /// Basic tooltip display functionality
    /// </summary>
    public class TooltipDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TMPro.TextMeshProUGUI titleText;
        [SerializeField] private TMPro.TextMeshProUGUI descriptionText;
        [SerializeField] private RectTransform tooltipRect;
        [SerializeField] private Canvas tooltipCanvas;

        private bool isVisible = false;

        private void Awake()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        public void ShowTooltip(string title, string description, Vector3 position)
        {
            if (tooltipPanel == null) return;

            // Set tooltip content
            if (titleText != null)
                titleText.text = title;

            if (descriptionText != null)
                descriptionText.text = description;

            // Position tooltip
            if (tooltipRect != null && tooltipCanvas != null)
            {
                Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, position);
                Vector2 localPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    tooltipCanvas.transform as RectTransform,
                    screenPosition,
                    tooltipCanvas.worldCamera,
                    out localPosition);

                tooltipRect.localPosition = localPosition;

                // Keep tooltip on screen
                KeepTooltipOnScreen();
            }

            // Show tooltip
            tooltipPanel.SetActive(true);
            isVisible = true;
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);

            isVisible = false;
        }

        private void KeepTooltipOnScreen()
        {
            if (tooltipRect == null || tooltipCanvas == null) return;

            var canvasRect = tooltipCanvas.transform as RectTransform;
            var tooltipSize = tooltipRect.sizeDelta;
            var tooltipPos = tooltipRect.localPosition;

            // Adjust horizontal position
            if (tooltipPos.x + tooltipSize.x > canvasRect.rect.width / 2)
                tooltipPos.x = canvasRect.rect.width / 2 - tooltipSize.x;
            if (tooltipPos.x < -canvasRect.rect.width / 2)
                tooltipPos.x = -canvasRect.rect.width / 2;

            // Adjust vertical position
            if (tooltipPos.y + tooltipSize.y > canvasRect.rect.height / 2)
                tooltipPos.y = canvasRect.rect.height / 2 - tooltipSize.y;
            if (tooltipPos.y < -canvasRect.rect.height / 2)
                tooltipPos.y = -canvasRect.rect.height / 2;

            tooltipRect.localPosition = tooltipPos;
        }

        public bool IsVisible => isVisible;
    }
}