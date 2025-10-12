using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Laboratory.Chimera.Social.Core;
using Laboratory.Chimera.Discovery.Core;
using Laboratory.Chimera.Genetics.Core;

namespace Laboratory.Chimera.Social.UI
{
    /// <summary>
    /// Individual social media card for displaying shares in the feed
    /// Beautiful Instagram-like card design with engagement features
    /// </summary>
    public class SocialShareCard : MonoBehaviour
    {
        [Header("Card UI")]
        [SerializeField] private TextMeshProUGUI _playerName;
        [SerializeField] private TextMeshProUGUI _shareTitle;
        [SerializeField] private TextMeshProUGUI _shareDescription;
        [SerializeField] private TextMeshProUGUI _timeStamp;
        [SerializeField] private Image _playerAvatar;

        [Header("Content Display")]
        [SerializeField] private Image _mainImage;
        [SerializeField] private Transform _traitContainer;
        [SerializeField] private GameObject _miniTraitPrefab;
        [SerializeField] private TextMeshProUGUI _specialMarkersText;

        [Header("Engagement")]
        [SerializeField] private Button _likeButton;
        [SerializeField] private Button _commentButton;
        [SerializeField] private Button _shareButton;
        [SerializeField] private TextMeshProUGUI _likeCount;
        [SerializeField] private TextMeshProUGUI _commentCount;
        [SerializeField] private TextMeshProUGUI _shareCount;

        [Header("Visual Styling")]
        [SerializeField] private Image _cardBackground;
        [SerializeField] private Image _rarityBorder;
        [SerializeField] private GameObject _featuredBadge;
        [SerializeField] private GameObject _verifiedBadge;
        [SerializeField] private GameObject _trendingIndicator;
        [SerializeField] private GameObject _worldFirstBanner;

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _cardRect;
        [SerializeField] private float _animationDuration = 0.8f;
        [SerializeField] private GameObject _likeAnimation;
        [SerializeField] private ParticleSystem _likeParticles;

        [Header("Discovery Specific")]
        [SerializeField] private GameObject _discoveryPanel;
        [SerializeField] private TextMeshProUGUI _discoveryType;
        [SerializeField] private TextMeshProUGUI _rarityLabel;
        [SerializeField] private TextMeshProUGUI _significanceScore;
        [SerializeField] private Image _discoveryIcon;

        private SocialShareData _shareData;
        private bool _isLiked = false;
        private bool _isAnimating = false;
        private Coroutine _likeAnimationCoroutine;

        /// <summary>
        /// Setup the card with share data
        /// </summary>
        public void SetupCard(SocialShareData shareData)
        {
            _shareData = shareData;

            SetupBasicInfo();
            SetupContent();
            SetupEngagement();
            SetupStyling();
            SetupSpecialIndicators();

            // Start invisible for animation
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Setup basic card information
        /// </summary>
        private void SetupBasicInfo()
        {
            if (_playerName != null)
                _playerName.text = _shareData.PlayerName.ToString();

            if (_shareTitle != null)
                _shareTitle.text = _shareData.ShareTitle.ToString();

            if (_shareDescription != null)
                _shareDescription.text = _shareData.ShareDescription.ToString();

            if (_timeStamp != null)
                _timeStamp.text = FormatTimeStamp(_shareData.ShareTimestamp);

            // Setup player avatar (would load from player data)
            if (_playerAvatar != null)
                _playerAvatar.color = GeneratePlayerColor(_shareData.PlayerName.ToString());
        }

        /// <summary>
        /// Setup content based on share type
        /// </summary>
        private void SetupContent()
        {
            // Show/hide discovery-specific panel
            bool isDiscovery = _shareData.Type != ShareType.CreatureShowcase;
            if (_discoveryPanel != null)
                _discoveryPanel.SetActive(isDiscovery);

            if (isDiscovery)
            {
                SetupDiscoveryContent();
            }

            SetupGeneticDisplay();
            SetupMainImage();
        }

        /// <summary>
        /// Setup discovery-specific content
        /// </summary>
        private void SetupDiscoveryContent()
        {
            if (_discoveryType != null)
                _discoveryType.text = _shareData.Type.ToString();

            // For actual discoveries, we'd have discovery event data
            if (_shareData.SharedDiscovery.Type != default)
            {
                if (_rarityLabel != null)
                    _rarityLabel.text = _shareData.SharedDiscovery.Rarity.ToString().ToUpper();

                if (_significanceScore != null)
                    _significanceScore.text = $"Score: {_shareData.SharedDiscovery.SignificanceScore:F0}";

                if (_discoveryIcon != null)
                    _discoveryIcon.color = GetRarityColor(_shareData.SharedDiscovery.Rarity);
            }
        }

        /// <summary>
        /// Setup genetic trait display
        /// </summary>
        private void SetupGeneticDisplay()
        {
            if (_traitContainer == null || _miniTraitPrefab == null) return;

            // Clear existing traits
            foreach (Transform child in _traitContainer)
            {
                Destroy(child.gameObject);
            }

            // Create mini trait displays
            var genetics = _shareData.GeneticData;
            CreateMiniTrait("STR", genetics.Strength);
            CreateMiniTrait("VIT", genetics.Vitality);
            CreateMiniTrait("AGI", genetics.Agility);
            CreateMiniTrait("INT", genetics.Intelligence);
            CreateMiniTrait("ADP", genetics.Adaptability);
            CreateMiniTrait("SOC", genetics.Social);

            // Display special markers
            if (_specialMarkersText != null)
            {
                _specialMarkersText.text = FormatSpecialMarkers(_shareData.HighlightMarkers);
            }
        }

        /// <summary>
        /// Create mini trait display
        /// </summary>
        private void CreateMiniTrait(string name, byte value)
        {
            GameObject traitGO = Instantiate(_miniTraitPrefab, _traitContainer);

            var nameText = traitGO.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            var valueText = traitGO.transform.Find("Value")?.GetComponent<TextMeshProUGUI>();
            var bar = traitGO.transform.Find("Bar")?.GetComponent<Image>();

            if (nameText != null) nameText.text = name;
            if (valueText != null) valueText.text = value.ToString();
            if (bar != null)
            {
                bar.fillAmount = value / 100f;
                bar.color = GetTraitColor(value);
            }
        }

        /// <summary>
        /// Setup main image (procedural for now)
        /// </summary>
        private void SetupMainImage()
        {
            if (_mainImage != null)
            {
                // Create procedural gradient based on genetics
                _mainImage.color = _shareData.PrimaryColor.ToColor();

                // In a real implementation, this would show creature photos or genetic visualizations
                // For now, use the primary color as a placeholder
            }
        }

        /// <summary>
        /// Setup engagement counters and buttons
        /// </summary>
        private void SetupEngagement()
        {
            if (_likeCount != null)
                _likeCount.text = FormatCount(_shareData.LikeCount);

            if (_commentCount != null)
                _commentCount.text = FormatCount(_shareData.CommentCount);

            if (_shareCount != null)
                _shareCount.text = FormatCount(_shareData.ShareCount);

            // Setup button events
            if (_likeButton != null)
                _likeButton.onClick.AddListener(OnLikeClicked);

            if (_commentButton != null)
                _commentButton.onClick.AddListener(OnCommentClicked);

            if (_shareButton != null)
                _shareButton.onClick.AddListener(OnShareClicked);
        }

        /// <summary>
        /// Setup visual styling based on share data
        /// </summary>
        private void SetupStyling()
        {
            // Card background tint
            if (_cardBackground != null)
            {
                Color bgTint = _shareData.PrimaryColor.ToColor();
                bgTint.a = 0.05f;
                _cardBackground.color = bgTint;
            }

            // Rarity border for discoveries
            if (_rarityBorder != null && _shareData.SharedDiscovery.Type != default)
            {
                _rarityBorder.color = GetRarityColor(_shareData.SharedDiscovery.Rarity);
                _rarityBorder.gameObject.SetActive(true);
            }
            else if (_rarityBorder != null)
            {
                _rarityBorder.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Setup special indicators (featured, verified, trending, etc.)
        /// </summary>
        private void SetupSpecialIndicators()
        {
            if (_featuredBadge != null)
                _featuredBadge.SetActive(_shareData.IsFeatured);

            if (_verifiedBadge != null)
                _verifiedBadge.SetActive(_shareData.IsVerified);

            if (_trendingIndicator != null)
                _trendingIndicator.SetActive(_shareData.IsTrending());

            if (_worldFirstBanner != null)
                _worldFirstBanner.SetActive(_shareData.SharedDiscovery.IsWorldFirst);
        }

        /// <summary>
        /// Animate card appearance
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
        }

        /// <summary>
        /// Animate card shifting down when new card is added above
        /// </summary>
        public IEnumerator AnimateShiftDown()
        {
            if (_cardRect == null) yield break;

            Vector3 originalPos = _cardRect.localPosition;
            Vector3 targetPos = originalPos + Vector3.down * 50f; // Shift down 50 pixels

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                _cardRect.localPosition = Vector3.Lerp(originalPos, targetPos, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Spring back to original position
            elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                _cardRect.localPosition = Vector3.Lerp(targetPos, originalPos, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _cardRect.localPosition = originalPos;
        }

        /// <summary>
        /// Handle like button click
        /// </summary>
        private void OnLikeClicked()
        {
            _isLiked = !_isLiked;

            // Update local count
            int newLikeCount = _shareData.LikeCount + (_isLiked ? 1 : -1);
            _shareData.UpdateEngagement(newLikeCount, _shareData.CommentCount, _shareData.ShareCount);

            if (_likeCount != null)
                _likeCount.text = FormatCount(newLikeCount);

            // Play like animation
            if (_isLiked)
            {
                PlayLikeAnimation();
            }

            // Update button visual state
            UpdateLikeButtonState();

            // In real implementation, would send to server
            UnityEngine.Debug.Log($"Like toggled for share: {_shareData.ShareID} (Now: {newLikeCount} likes)");
        }

        /// <summary>
        /// Handle comment button click
        /// </summary>
        private void OnCommentClicked()
        {
            // In real implementation, would open comment dialog
            UnityEngine.Debug.Log($"Comments clicked for share: {_shareData.ShareID}");

            // For now, just simulate adding a comment
            int newCommentCount = _shareData.CommentCount + 1;
            _shareData.UpdateEngagement(_shareData.LikeCount, newCommentCount, _shareData.ShareCount);

            if (_commentCount != null)
                _commentCount.text = FormatCount(newCommentCount);
        }

        /// <summary>
        /// Handle share button click
        /// </summary>
        private void OnShareClicked()
        {
            // In real implementation, would open share options
            UnityEngine.Debug.Log($"Share clicked for share: {_shareData.ShareID}");

            // For now, just simulate resharing
            int newShareCount = _shareData.ShareCount + 1;
            _shareData.UpdateEngagement(_shareData.LikeCount, _shareData.CommentCount, newShareCount);

            if (_shareCount != null)
                _shareCount.text = FormatCount(newShareCount);

            // Could also add to player's own feed as a reshare
        }

        /// <summary>
        /// Play like animation with particles
        /// </summary>
        private void PlayLikeAnimation()
        {
            if (_likeAnimationCoroutine != null)
                StopCoroutine(_likeAnimationCoroutine);

            _likeAnimationCoroutine = StartCoroutine(LikeAnimationCoroutine());
        }

        /// <summary>
        /// Like animation coroutine
        /// </summary>
        private IEnumerator LikeAnimationCoroutine()
        {
            // Heart pop animation
            if (_likeAnimation != null)
            {
                _likeAnimation.SetActive(true);
                _likeAnimation.transform.localScale = Vector3.zero;

                float elapsed = 0f;
                float duration = 0.5f;

                while (elapsed < duration)
                {
                    float t = elapsed / duration;
                    float scale = Mathf.Sin(t * Mathf.PI) * 1.2f;
                    _likeAnimation.transform.localScale = Vector3.one * scale;

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                _likeAnimation.SetActive(false);
            }

            // Particle burst
            if (_likeParticles != null)
            {
                _likeParticles.Play();
            }

            _likeAnimationCoroutine = null;
        }

        /// <summary>
        /// Update like button visual state
        /// </summary>
        private void UpdateLikeButtonState()
        {
            if (_likeButton != null)
            {
                var buttonImage = _likeButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = _isLiked ? Color.red : Color.white;
                }
            }
        }

        /// <summary>
        /// Helper methods for formatting
        /// </summary>
        private string FormatCount(int count)
        {
            if (count >= 1000000)
                return $"{count / 1000000f:F1}M";
            if (count >= 1000)
                return $"{count / 1000f:F1}K";
            return count.ToString();
        }

        private string FormatTimeStamp(uint timestamp)
        {
            float secondsAgo = Time.time - timestamp;

            if (secondsAgo < 60)
                return "Just now";
            if (secondsAgo < 3600)
                return $"{(int)(secondsAgo / 60)}m";
            if (secondsAgo < 86400)
                return $"{(int)(secondsAgo / 3600)}h";
            if (secondsAgo < 604800)
                return $"{(int)(secondsAgo / 86400)}d";
            return $"{(int)(secondsAgo / 604800)}w";
        }

        private string FormatSpecialMarkers(GeneticMarkerFlags markers)
        {
            if (markers == GeneticMarkerFlags.None)
                return "";

            var markerList = new System.Collections.Generic.List<string>();

            if (markers.HasFlag(GeneticMarkerFlags.Bioluminescent))
                markerList.Add("🌟");
            if (markers.HasFlag(GeneticMarkerFlags.CamouflageGene))
                markerList.Add("👁️");
            if (markers.HasFlag(GeneticMarkerFlags.PackLeader))
                markerList.Add("👑");
            if (markers.HasFlag(GeneticMarkerFlags.ElementalAffinity))
                markerList.Add("🔥");

            return string.Join(" ", markerList);
        }

        private Color GetRarityColor(DiscoveryRarity rarity)
        {
            return rarity switch
            {
                DiscoveryRarity.Common => Color.white,
                DiscoveryRarity.Uncommon => Color.green,
                DiscoveryRarity.Rare => Color.blue,
                DiscoveryRarity.Epic => Color.magenta,
                DiscoveryRarity.Legendary => Color.yellow,
                DiscoveryRarity.Mythical => Color.red,
                _ => Color.gray
            };
        }

        private Color GetTraitColor(byte value)
        {
            if (value >= 90) return Color.red;
            if (value >= 70) return Color.yellow;
            if (value >= 50) return Color.green;
            return Color.gray;
        }

        private Color GeneratePlayerColor(string playerName)
        {
            // Generate consistent color from player name hash
            int hash = playerName.GetHashCode();
            float hue = (hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.7f, 0.9f);
        }

        /// <summary>
        /// Public API
        /// </summary>
        public SocialShareData GetShareData() => _shareData;
        public bool IsAnimating => _isAnimating;
    }
}