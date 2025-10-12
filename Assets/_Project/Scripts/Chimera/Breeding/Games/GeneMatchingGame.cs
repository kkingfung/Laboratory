using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Laboratory.Chimera.Breeding.Core;
using Laboratory.Chimera.Breeding.UI;
using Laboratory.Chimera.Genetics.Core;

namespace Laboratory.Chimera.Breeding.Games
{
    /// <summary>
    /// Gene matching mini-game - match complementary genetic traits
    /// Memory-style card game with genetic twist and combo bonuses
    /// </summary>
    public class GeneMatchingGame : MonoBehaviour, IBreedingMiniGame
    {
        [Header("Game Grid")]
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private GameObject _geneCardPrefab;
        [SerializeField] private GridLayoutGroup _gridLayout;

        [Header("Game UI")]
        [SerializeField] private TextMeshProUGUI _matchesFoundText;
        [SerializeField] private TextMeshProUGUI _comboText;
        [SerializeField] private TextMeshProUGUI _instructionsText;
        [SerializeField] private Slider _comboMeter;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _matchParticles;
        [SerializeField] private ParticleSystem _comboParticles;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _flipSound;
        [SerializeField] private AudioClip _matchSound;
        [SerializeField] private AudioClip _comboSound;

        [Header("Timing")]
        [SerializeField] private float _cardFlipDuration = 0.3f;
        [SerializeField] private float _mismatchDelay = 1.0f;
        [SerializeField] private float _comboDecayRate = 1.0f;

        private BreedingGameData _gameData;
        private List<GeneCard> _cards = new List<GeneCard>();
        private List<GameObject> _cardObjects = new List<GameObject>();
        private GeneCard? _firstSelected = null;
        private GeneCard? _secondSelected = null;
        private bool _processingMatch = false;
        private float _currentScore = 0f;
        private float _comboMultiplier = 1.0f;
        private int _matchesFound = 0;
        private int _consecutiveMatches = 0;

        // Color scheme for different traits
        private Color[] _traitColors = new Color[]
        {
            new Color(1f, 0.2f, 0.2f),   // Strength - Red
            new Color(0.2f, 1f, 0.2f),   // Vitality - Green
            new Color(1f, 1f, 0.2f),     // Agility - Yellow
            new Color(0.2f, 0.2f, 1f),   // Intelligence - Blue
            new Color(1f, 0.2f, 1f),     // Adaptability - Magenta
            new Color(1f, 0.5f, 0.2f)    // Social - Orange
        };

        public void Initialize(BreedingGameData gameData)
        {
            _gameData = gameData;
            _currentScore = 0f;
            _comboMultiplier = 1.0f;
            _matchesFound = 0;
            _consecutiveMatches = 0;

            SetupGrid();
            GenerateCards();
            UpdateUI();

            if (_instructionsText != null)
                _instructionsText.text = "Match complementary genetic traits! Higher values work better together.";
        }

        public void UpdateGame(float deltaTime)
        {
            // Decay combo multiplier over time
            if (_comboMultiplier > 1.0f)
            {
                _comboMultiplier = Mathf.Max(1.0f, _comboMultiplier - _comboDecayRate * deltaTime);
                UpdateComboDisplay();
            }
        }

        public float GetCurrentScore() => _currentScore;
        public float GetSkillPerformance() => (float)_matchesFound / _gameData.GeneMatching.MatchesRequired;
        public int GetPerfectMatches() => _matchesFound;
        public bool IsGameComplete() => _matchesFound >= _gameData.GeneMatching.MatchesRequired;

        /// <summary>
        /// Setup the grid layout based on difficulty
        /// </summary>
        private void SetupGrid()
        {
            if (_gridLayout != null)
            {
                int gridSize = _gameData.GeneMatching.GridSize;
                _gridLayout.constraintCount = gridSize;

                // Adjust cell size based on grid size
                float cellSize = Mathf.Max(80f, 400f / gridSize);
                _gridLayout.cellSize = Vector2.one * cellSize;
            }
        }

        /// <summary>
        /// Generate cards based on parent genetics
        /// </summary>
        private void GenerateCards()
        {
            ClearCards();

            int gridSize = _gameData.GeneMatching.GridSize;
            int totalCards = gridSize * gridSize;
            int pairsNeeded = totalCards / 2;

            // Generate trait pairs based on parent genetics
            var traitPairs = GenerateTraitPairs(pairsNeeded);

            // Create cards from pairs
            foreach (var pair in traitPairs)
            {
                _cards.Add(pair.card1);
                _cards.Add(pair.card2);
            }

            // Shuffle cards
            for (int i = 0; i < _cards.Count; i++)
            {
                int randomIndex = Random.Range(i, _cards.Count);
                var temp = _cards[i];
                _cards[i] = _cards[randomIndex];
                _cards[randomIndex] = temp;
            }

            // Create card GameObjects
            for (int i = 0; i < _cards.Count; i++)
            {
                CreateCardObject(_cards[i], i);
            }
        }

        /// <summary>
        /// Generate complementary trait pairs
        /// </summary>
        private List<(GeneCard card1, GeneCard card2)> GenerateTraitPairs(int pairsNeeded)
        {
            var pairs = new List<(GeneCard, GeneCard)>();
            var parent1Traits = GetParentTraits(_gameData.Parent1Genetics);
            var parent2Traits = GetParentTraits(_gameData.Parent2Genetics);

            // Create complementary pairs
            for (int i = 0; i < pairsNeeded && i < 6; i++)
            {
                byte traitType = (byte)i;
                byte value1 = parent1Traits[i];
                byte value2 = parent2Traits[i];

                // Create complementary cards (high trait pairs with medium/low traits)
                var card1 = new GeneCard
                {
                    TraitType = traitType,
                    TraitValue = value1,
                    IsRevealed = false,
                    IsMatched = false,
                    GridPosition = Vector2.zero
                };

                var card2 = new GeneCard
                {
                    TraitType = traitType,
                    TraitValue = value2,
                    IsRevealed = false,
                    IsMatched = false,
                    GridPosition = Vector2.zero
                };

                pairs.Add((card1, card2));
            }

            // Fill remaining pairs with random complementary traits
            while (pairs.Count < pairsNeeded)
            {
                byte randomTrait = (byte)Random.Range(0, 6);
                byte value1 = (byte)Random.Range(40, 80);
                byte value2 = (byte)Random.Range(80, 100); // Complementary high value

                var card1 = new GeneCard
                {
                    TraitType = randomTrait,
                    TraitValue = value1,
                    IsRevealed = false,
                    IsMatched = false,
                    GridPosition = Vector2.zero
                };

                var card2 = new GeneCard
                {
                    TraitType = randomTrait,
                    TraitValue = value2,
                    IsRevealed = false,
                    IsMatched = false,
                    GridPosition = Vector2.zero
                };

                pairs.Add((card1, card2));
            }

            return pairs;
        }

        /// <summary>
        /// Get trait array from genetics
        /// </summary>
        private byte[] GetParentTraits(VisualGeneticData genetics)
        {
            return new byte[]
            {
                genetics.Strength,
                genetics.Vitality,
                genetics.Agility,
                genetics.Intelligence,
                genetics.Adaptability,
                genetics.Social
            };
        }

        /// <summary>
        /// Create a card GameObject
        /// </summary>
        private void CreateCardObject(GeneCard card, int index)
        {
            if (_geneCardPrefab == null || _gridContainer == null) return;

            GameObject cardObj = Instantiate(_geneCardPrefab, _gridContainer);
            _cardObjects.Add(cardObj);

            // Setup card appearance
            var cardComponent = cardObj.GetComponent<GeneCardDisplay>();
            if (cardComponent != null)
            {
                cardComponent.SetupCard(card, _traitColors[card.TraitType]);
                cardComponent.OnCardClicked += () => OnCardClicked(index);
            }

            // Set grid position
            int gridSize = _gameData.GeneMatching.GridSize;
            card.GridPosition = new Vector2(index % gridSize, index / gridSize);
        }

        /// <summary>
        /// Handle card click
        /// </summary>
        private void OnCardClicked(int cardIndex)
        {
            if (_processingMatch || cardIndex < 0 || cardIndex >= _cards.Count) return;

            var card = _cards[cardIndex];
            if (card.IsRevealed || card.IsMatched) return;

            // Play flip sound
            if (_audioSource != null && _flipSound != null)
                _audioSource.PlayOneShot(_flipSound);

            // Reveal card
            RevealCard(cardIndex);

            if (_firstSelected == null)
            {
                _firstSelected = card;
            }
            else if (_secondSelected == null && !card.Equals(_firstSelected))
            {
                _secondSelected = card;
                StartCoroutine(ProcessCardPair());
            }
        }

        /// <summary>
        /// Reveal a card with animation
        /// </summary>
        private void RevealCard(int cardIndex)
        {
            var card = _cards[cardIndex];
            card.IsRevealed = true;
            _cards[cardIndex] = card;

            if (cardIndex < _cardObjects.Count)
            {
                var cardComponent = _cardObjects[cardIndex].GetComponent<GeneCardDisplay>();
                cardComponent?.RevealCard();
            }
        }

        /// <summary>
        /// Hide a card with animation
        /// </summary>
        private void HideCard(int cardIndex)
        {
            var card = _cards[cardIndex];
            card.IsRevealed = false;
            _cards[cardIndex] = card;

            if (cardIndex < _cardObjects.Count)
            {
                var cardComponent = _cardObjects[cardIndex].GetComponent<GeneCardDisplay>();
                cardComponent?.HideCard();
            }
        }

        /// <summary>
        /// Process the selected card pair
        /// </summary>
        private IEnumerator ProcessCardPair()
        {
            _processingMatch = true;

            yield return new WaitForSeconds(_cardFlipDuration);

            bool isMatch = CheckForMatch(_firstSelected.Value, _secondSelected.Value);

            if (isMatch)
            {
                ProcessSuccessfulMatch();
            }
            else
            {
                yield return new WaitForSeconds(_mismatchDelay);
                ProcessMismatch();
            }

            _firstSelected = null;
            _secondSelected = null;
            _processingMatch = false;
        }

        /// <summary>
        /// Check if two cards form a valid match
        /// </summary>
        private bool CheckForMatch(GeneCard card1, GeneCard card2)
        {
            // Same trait type is a match
            if (card1.TraitType == card2.TraitType)
                return true;

            // Complementary traits are matches (high + medium values)
            int totalValue = card1.TraitValue + card2.TraitValue;
            return totalValue >= 120; // Threshold for complementary match
        }

        /// <summary>
        /// Process successful match
        /// </summary>
        private void ProcessSuccessfulMatch()
        {
            _matchesFound++;
            _consecutiveMatches++;

            // Update combo multiplier
            _comboMultiplier += 0.2f * _consecutiveMatches;

            // Calculate score
            int baseScore = (_firstSelected.Value.TraitValue + _secondSelected.Value.TraitValue) / 2;
            float matchScore = baseScore * _comboMultiplier;
            _currentScore += matchScore;

            // Mark cards as matched
            MarkCardsAsMatched();

            // Play effects
            PlayMatchEffects();

            // Update UI
            UpdateUI();

            UnityEngine.Debug.Log($"Match found! Score: +{matchScore:F0} (Combo: {_comboMultiplier:F1}x)");
        }

        /// <summary>
        /// Process mismatch
        /// </summary>
        private void ProcessMismatch()
        {
            _consecutiveMatches = 0;
            _comboMultiplier = Mathf.Max(1.0f, _comboMultiplier - 0.1f);

            // Hide both cards
            int firstIndex = FindCardIndex(_firstSelected.Value);
            int secondIndex = FindCardIndex(_secondSelected.Value);

            if (firstIndex >= 0) HideCard(firstIndex);
            if (secondIndex >= 0) HideCard(secondIndex);

            UpdateComboDisplay();
        }

        /// <summary>
        /// Mark matched cards
        /// </summary>
        private void MarkCardsAsMatched()
        {
            int firstIndex = FindCardIndex(_firstSelected.Value);
            int secondIndex = FindCardIndex(_secondSelected.Value);

            if (firstIndex >= 0)
            {
                var card = _cards[firstIndex];
                card.IsMatched = true;
                _cards[firstIndex] = card;

                var cardComponent = _cardObjects[firstIndex].GetComponent<GeneCardDisplay>();
                cardComponent?.MarkAsMatched();
            }

            if (secondIndex >= 0)
            {
                var card = _cards[secondIndex];
                card.IsMatched = true;
                _cards[secondIndex] = card;

                var cardComponent = _cardObjects[secondIndex].GetComponent<GeneCardDisplay>();
                cardComponent?.MarkAsMatched();
            }
        }

        /// <summary>
        /// Find card index in the list
        /// </summary>
        private int FindCardIndex(GeneCard targetCard)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card.TraitType == targetCard.TraitType &&
                    card.TraitValue == targetCard.TraitValue &&
                    card.GridPosition.Equals(targetCard.GridPosition))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Play match effects
        /// </summary>
        private void PlayMatchEffects()
        {
            // Audio
            if (_audioSource != null)
            {
                AudioClip clip = _consecutiveMatches > 2 ? _comboSound : _matchSound;
                if (clip != null) _audioSource.PlayOneShot(clip);
            }

            // Particles
            if (_matchParticles != null)
                _matchParticles.Play();

            if (_consecutiveMatches > 2 && _comboParticles != null)
                _comboParticles.Play();
        }

        /// <summary>
        /// Update UI elements
        /// </summary>
        private void UpdateUI()
        {
            if (_matchesFoundText != null)
                _matchesFoundText.text = $"Matches: {_matchesFound}/{_gameData.GeneMatching.MatchesRequired}";

            UpdateComboDisplay();
        }

        /// <summary>
        /// Update combo display
        /// </summary>
        private void UpdateComboDisplay()
        {
            if (_comboText != null)
            {
                if (_comboMultiplier > 1.0f)
                {
                    _comboText.text = $"Combo: {_comboMultiplier:F1}x";
                    _comboText.color = Color.Lerp(Color.white, Color.yellow, (_comboMultiplier - 1f) / 2f);
                }
                else
                {
                    _comboText.text = "";
                }
            }

            if (_comboMeter != null)
            {
                _comboMeter.value = Mathf.Clamp01((_comboMultiplier - 1f) / 2f);
            }
        }

        /// <summary>
        /// Clear all cards
        /// </summary>
        private void ClearCards()
        {
            _cards.Clear();

            foreach (var cardObj in _cardObjects)
            {
                if (cardObj != null)
                    Destroy(cardObj);
            }
            _cardObjects.Clear();
        }

        private void OnDestroy()
        {
            ClearCards();
        }
    }

    /// <summary>
    /// Individual gene card display component
    /// </summary>
    public class GeneCardDisplay : MonoBehaviour
    {
        [SerializeField] private Button _cardButton;
        [SerializeField] private Image _cardBack;
        [SerializeField] private Image _cardFront;
        [SerializeField] private TextMeshProUGUI _traitName;
        [SerializeField] private TextMeshProUGUI _traitValue;
        [SerializeField] private Image _traitIcon;
        [SerializeField] private GameObject _matchGlow;

        private GeneCard _cardData;
        private bool _isRevealed = false;
        private Color _traitColor;

        public System.Action OnCardClicked;

        private void Start()
        {
            if (_cardButton != null)
                _cardButton.onClick.AddListener(() => OnCardClicked?.Invoke());

            // Start with card hidden
            ShowBack();
        }

        public void SetupCard(GeneCard cardData, Color traitColor)
        {
            _cardData = cardData;
            _traitColor = traitColor;

            // Setup front face
            if (_traitName != null)
                _traitName.text = GetTraitName(cardData.TraitType);

            if (_traitValue != null)
                _traitValue.text = cardData.TraitValue.ToString();

            if (_traitIcon != null)
                _traitIcon.color = traitColor;

            if (_cardFront != null)
            {
                Color frontColor = traitColor;
                frontColor.a = 0.3f;
                _cardFront.color = frontColor;
            }

            if (_matchGlow != null)
                _matchGlow.SetActive(false);
        }

        public void RevealCard()
        {
            if (_isRevealed) return;
            StartCoroutine(FlipToFront());
        }

        public void HideCard()
        {
            if (!_isRevealed) return;
            StartCoroutine(FlipToBack());
        }

        public void MarkAsMatched()
        {
            if (_matchGlow != null)
                _matchGlow.SetActive(true);

            // Disable button
            if (_cardButton != null)
                _cardButton.interactable = false;
        }

        private IEnumerator FlipToFront()
        {
            yield return StartCoroutine(FlipCard(false, true));
            _isRevealed = true;
        }

        private IEnumerator FlipToBack()
        {
            yield return StartCoroutine(FlipCard(true, false));
            _isRevealed = false;
        }

        private IEnumerator FlipCard(bool showBack, bool showFront)
        {
            // Scale down
            float elapsed = 0f;
            float halfDuration = 0.15f;

            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0f, 1f, 1f), t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Switch faces
            ShowBack();
            if (showFront) ShowFront();

            // Scale back up
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(new Vector3(0f, 1f, 1f), Vector3.one, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = Vector3.one;
        }

        private void ShowBack()
        {
            if (_cardBack != null) _cardBack.gameObject.SetActive(true);
            if (_cardFront != null) _cardFront.gameObject.SetActive(false);
        }

        private void ShowFront()
        {
            if (_cardBack != null) _cardBack.gameObject.SetActive(false);
            if (_cardFront != null) _cardFront.gameObject.SetActive(true);
        }

        private string GetTraitName(byte traitType)
        {
            return traitType switch
            {
                0 => "STR",
                1 => "VIT",
                2 => "AGI",
                3 => "INT",
                4 => "ADP",
                5 => "SOC",
                _ => "???"
            };
        }
    }
}