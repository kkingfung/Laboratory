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
    /// DNA sequencing mini-game - arrange DNA sequences correctly
    /// Drag-and-drop puzzle game with genetic accuracy and speed bonuses
    /// </summary>
    public class DNASequencingGame : MonoBehaviour, IBreedingMiniGame
    {
        [Header("Sequencing UI")]
        [SerializeField] private Transform _sequenceContainer;
        [SerializeField] private Transform _targetContainer;
        [SerializeField] private GameObject _dnaBasePrefab;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Game Display")]
        [SerializeField] private TextMeshProUGUI _sequenceProgressText;
        [SerializeField] private TextMeshProUGUI _accuracyText;
        [SerializeField] private TextMeshProUGUI _instructionsText;
        [SerializeField] private Slider _accuracyMeter;
        [SerializeField] private Image _sequencePreview;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _correctSequenceParticles;
        [SerializeField] private ParticleSystem _helixFormationParticles;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _placeBaseSound;
        [SerializeField] private AudioClip _correctSequenceSound;
        [SerializeField] private AudioClip _errorSound;

        [Header("Sequence Settings")]
        [SerializeField] private float _sequenceTimeBonus = 10f;

        private BreedingGameData _gameData;
        private List<DNASequence> _sequences = new List<DNASequence>();
        private List<GameObject> _sequenceObjects = new List<GameObject>();
        private int _currentSequenceIndex = 0;
        private float _currentScore = 0f;
        private float _accuracyScore = 1.0f;
        private int _correctSequences = 0;
        private bool _sequenceInProgress = false;

        // DNA base colors
        private Color _adenineColor = new Color(1f, 0.2f, 0.2f);     // Red - A
        private Color _thymineColor = new Color(0.2f, 0.2f, 1f);     // Blue - T
        private Color _guanineColor = new Color(0.2f, 1f, 0.2f);     // Green - G
        private Color _cytosineColor = new Color(1f, 1f, 0.2f);      // Yellow - C

        public void Initialize(BreedingGameData gameData)
        {
            _gameData = gameData;
            _currentScore = 0f;
            _accuracyScore = 1.0f;
            _correctSequences = 0;
            _currentSequenceIndex = 0;
            _sequenceInProgress = false;

            GenerateSequences();
            StartNextSequence();

            if (_instructionsText != null)
                _instructionsText.text = "Drag DNA bases to create the target sequence. A pairs with T, G pairs with C!";
        }

        public void UpdateGame(float deltaTime)
        {
            // Update any ongoing animations or effects
            UpdateSequenceDisplay();
        }

        public float GetCurrentScore() => _currentScore;
        public float GetSkillPerformance() => _accuracyScore * ((float)_correctSequences / _gameData.DNASequencing.TotalSequences);
        public int GetPerfectMatches() => _correctSequences;
        public bool IsGameComplete() => _correctSequences >= _gameData.DNASequencing.TotalSequences;

        /// <summary>
        /// Generate DNA sequences based on parent genetics
        /// </summary>
        private void GenerateSequences()
        {
            _sequences.Clear();

            for (int i = 0; i < _gameData.DNASequencing.TotalSequences; i++)
            {
                var sequence = GenerateSequenceFromGenetics(i);
                _sequences.Add(sequence);
            }
        }

        /// <summary>
        /// Generate a sequence based on genetic data
        /// </summary>
        private DNASequence GenerateSequenceFromGenetics(int sequenceIndex)
        {
            // Use parent genetics to influence sequence patterns
            var parent1 = _gameData.Parent1Genetics;
            var parent2 = _gameData.Parent2Genetics;

            int sequenceLength = _gameData.DNASequencing.SequenceLength;
            var targetBases = new List<DNABase>();
            var scrambledBases = new List<DNABase>();

            // Create sequence based on trait combinations
            for (int i = 0; i < sequenceLength; i++)
            {
                DNABaseType baseType = DetermineBaseFromTraits(parent1, parent2, i, sequenceIndex);
                DNABaseType complementType = GetComplementaryBase(baseType);

                targetBases.Add(new DNABase { Type = baseType, Position = i, IsCorrect = false });
                scrambledBases.Add(new DNABase { Type = complementType, Position = -1, IsCorrect = false });
            }

            // Scramble the bases for player to arrange
            for (int i = 0; i < scrambledBases.Count; i++)
            {
                int randomIndex = Random.Range(i, scrambledBases.Count);
                var temp = scrambledBases[i];
                scrambledBases[i] = scrambledBases[randomIndex];
                scrambledBases[randomIndex] = temp;
            }

            return new DNASequence
            {
                SequenceID = sequenceIndex,
                TargetBases = targetBases,
                PlayerBases = new List<DNABase>(),
                ScrambledBases = scrambledBases,
                IsComplete = false,
                Accuracy = 0f,
                CompletionTime = 0f
            };
        }

        /// <summary>
        /// Determine DNA base type from genetic traits
        /// </summary>
        private DNABaseType DetermineBaseFromTraits(VisualGeneticData parent1, VisualGeneticData parent2, int position, int sequenceIndex)
        {
            // Use genetic data to create meaningful patterns
            byte[] traits1 = { parent1.Strength, parent1.Vitality, parent1.Agility, parent1.Intelligence, parent1.Adaptability, parent1.Social };
            byte[] traits2 = { parent2.Strength, parent2.Vitality, parent2.Agility, parent2.Intelligence, parent2.Adaptability, parent2.Social };

            int traitIndex = (position + sequenceIndex) % 6;
            int combinedValue = traits1[traitIndex] + traits2[traitIndex];

            return (combinedValue % 4) switch
            {
                0 => DNABaseType.Adenine,
                1 => DNABaseType.Thymine,
                2 => DNABaseType.Guanine,
                3 => DNABaseType.Cytosine,
                _ => DNABaseType.Adenine
            };
        }

        /// <summary>
        /// Get complementary DNA base
        /// </summary>
        private DNABaseType GetComplementaryBase(DNABaseType baseType)
        {
            return baseType switch
            {
                DNABaseType.Adenine => DNABaseType.Thymine,
                DNABaseType.Thymine => DNABaseType.Adenine,
                DNABaseType.Guanine => DNABaseType.Cytosine,
                DNABaseType.Cytosine => DNABaseType.Guanine,
                _ => DNABaseType.Adenine
            };
        }

        /// <summary>
        /// Start the next sequence
        /// </summary>
        private void StartNextSequence()
        {
            if (_currentSequenceIndex >= _sequences.Count)
            {
                CompleteAllSequences();
                return;
            }

            _sequenceInProgress = true;
            var sequence = _sequences[_currentSequenceIndex];

            ClearSequenceDisplay();
            SetupSequenceDisplay(sequence);
            UpdateUI();
        }

        /// <summary>
        /// Setup display for current sequence
        /// </summary>
        private void SetupSequenceDisplay(DNASequence sequence)
        {
            // Create target sequence display
            CreateTargetSequence(sequence.TargetBases);

            // Create scrambled bases for player to arrange
            CreateScrambledBases(sequence.ScrambledBases);
        }

        /// <summary>
        /// Create target sequence visualization
        /// </summary>
        private void CreateTargetSequence(List<DNABase> targetBases)
        {
            if (_targetContainer == null || _dnaBasePrefab == null) return;

            foreach (var baseData in targetBases)
            {
                GameObject baseObj = Instantiate(_dnaBasePrefab, _targetContainer);
                var baseComponent = baseObj.GetComponent<DNABaseDisplay>();

                if (baseComponent != null)
                {
                    baseComponent.SetupAsTarget(baseData, GetBaseColor(baseData.Type));
                }

                _sequenceObjects.Add(baseObj);
            }
        }

        /// <summary>
        /// Create scrambled bases for player interaction
        /// </summary>
        private void CreateScrambledBases(List<DNABase> scrambledBases)
        {
            if (_sequenceContainer == null || _dnaBasePrefab == null) return;

            for (int i = 0; i < scrambledBases.Count; i++)
            {
                var baseData = scrambledBases[i];
                GameObject baseObj = Instantiate(_dnaBasePrefab, _sequenceContainer);
                var baseComponent = baseObj.GetComponent<DNABaseDisplay>();

                if (baseComponent != null)
                {
                    baseComponent.SetupAsDraggable(baseData, GetBaseColor(baseData.Type));
                    baseComponent.OnBasePlaced += OnBasePlaced;
                    baseComponent.OnBaseRemoved += OnBaseRemoved;
                }

                _sequenceObjects.Add(baseObj);
            }
        }

        /// <summary>
        /// Handle base placement
        /// </summary>
        private void OnBasePlaced(DNABase placedBase, int targetPosition)
        {
            if (!_sequenceInProgress || _currentSequenceIndex >= _sequences.Count) return;

            var sequence = _sequences[_currentSequenceIndex];

            // Play audio feedback
            if (_audioSource != null && _placeBaseSound != null)
                _audioSource.PlayOneShot(_placeBaseSound);

            // Update base position
            placedBase.Position = targetPosition;

            // Check if placement is correct
            if (targetPosition < sequence.TargetBases.Count)
            {
                var targetBase = sequence.TargetBases[targetPosition];
                bool isCorrect = IsValidPairing(targetBase.Type, placedBase.Type);
                placedBase.IsCorrect = isCorrect;

                if (isCorrect)
                {
                    // Add to player sequence
                    sequence.PlayerBases.Add(placedBase);
                    _currentScore += 10f * _gameData.DNASequencing.AccuracyBonus;

                    // Visual feedback
                    if (_correctSequenceParticles != null)
                        _correctSequenceParticles.Play();
                }
                else
                {
                    // Incorrect placement
                    _accuracyScore *= 0.95f; // Small penalty

                    if (_audioSource != null && _errorSound != null)
                        _audioSource.PlayOneShot(_errorSound);
                }
            }

            // Check if sequence is complete
            CheckSequenceCompletion();
            UpdateUI();
        }

        /// <summary>
        /// Handle base removal
        /// </summary>
        private void OnBaseRemoved(DNABase removedBase)
        {
            if (!_sequenceInProgress || _currentSequenceIndex >= _sequences.Count) return;

            var sequence = _sequences[_currentSequenceIndex];
            sequence.PlayerBases.Remove(removedBase);
            removedBase.Position = -1;
            removedBase.IsCorrect = false;
        }

        /// <summary>
        /// Check if DNA bases form valid pairs
        /// </summary>
        private bool IsValidPairing(DNABaseType base1, DNABaseType base2)
        {
            return (base1 == DNABaseType.Adenine && base2 == DNABaseType.Thymine) ||
                   (base1 == DNABaseType.Thymine && base2 == DNABaseType.Adenine) ||
                   (base1 == DNABaseType.Guanine && base2 == DNABaseType.Cytosine) ||
                   (base1 == DNABaseType.Cytosine && base2 == DNABaseType.Guanine);
        }

        /// <summary>
        /// Check if current sequence is complete
        /// </summary>
        private void CheckSequenceCompletion()
        {
            var sequence = _sequences[_currentSequenceIndex];

            // Count correct placements
            int correctPlacements = 0;
            foreach (var playerBase in sequence.PlayerBases)
            {
                if (playerBase.IsCorrect) correctPlacements++;
            }

            // Check if sequence is complete
            if (correctPlacements >= sequence.TargetBases.Count)
            {
                CompleteCurrentSequence();
            }
        }

        /// <summary>
        /// Complete current sequence
        /// </summary>
        private void CompleteCurrentSequence()
        {
            var sequence = _sequences[_currentSequenceIndex];
            sequence.IsComplete = true;
            sequence.CompletionTime = _gameData.ElapsedTime;

            _correctSequences++;
            _sequenceInProgress = false;

            // Calculate sequence score
            float timeBonus = Mathf.Max(0.5f, _sequenceTimeBonus / sequence.CompletionTime);
            float sequenceScore = 100f * _accuracyScore * timeBonus;
            _currentScore += sequenceScore;

            // Play completion effects
            if (_audioSource != null && _correctSequenceSound != null)
                _audioSource.PlayOneShot(_correctSequenceSound);

            if (_helixFormationParticles != null)
                _helixFormationParticles.Play();

            // Move to next sequence
            _currentSequenceIndex++;
            StartCoroutine(TransitionToNextSequence());

            Debug.Log($"Sequence complete! Score: +{sequenceScore:F0} (Accuracy: {_accuracyScore:P0}, Time: {sequence.CompletionTime:F1}s)");
        }

        /// <summary>
        /// Transition to next sequence with delay
        /// </summary>
        private IEnumerator TransitionToNextSequence()
        {
            yield return new WaitForSeconds(2f);
            StartNextSequence();
        }

        /// <summary>
        /// Complete all sequences
        /// </summary>
        private void CompleteAllSequences()
        {
            Debug.Log("All DNA sequences completed!");
            // Game completion is handled by the main manager
        }

        /// <summary>
        /// Update UI elements
        /// </summary>
        private void UpdateUI()
        {
            if (_sequenceProgressText != null)
                _sequenceProgressText.text = $"Sequence: {_currentSequenceIndex + 1}/{_gameData.DNASequencing.TotalSequences}";

            if (_accuracyText != null)
                _accuracyText.text = $"Accuracy: {_accuracyScore:P0}";

            if (_accuracyMeter != null)
                _accuracyMeter.value = _accuracyScore;
        }

        /// <summary>
        /// Update sequence display
        /// </summary>
        private void UpdateSequenceDisplay()
        {
            // This could animate the DNA helix or show completion progress
            // For now, just ensure UI is up to date
        }

        /// <summary>
        /// Clear sequence display
        /// </summary>
        private void ClearSequenceDisplay()
        {
            foreach (var obj in _sequenceObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            _sequenceObjects.Clear();
        }

        /// <summary>
        /// Get color for DNA base type
        /// </summary>
        private Color GetBaseColor(DNABaseType baseType)
        {
            return baseType switch
            {
                DNABaseType.Adenine => _adenineColor,
                DNABaseType.Thymine => _thymineColor,
                DNABaseType.Guanine => _guanineColor,
                DNABaseType.Cytosine => _cytosineColor,
                _ => Color.white
            };
        }

        private void OnDestroy()
        {
            ClearSequenceDisplay();
        }
    }

    /// <summary>
    /// DNA sequence data structure
    /// </summary>
    [System.Serializable]
    public class DNASequence
    {
        public int SequenceID;
        public List<DNABase> TargetBases;
        public List<DNABase> PlayerBases;
        public List<DNABase> ScrambledBases;
        public bool IsComplete;
        public float Accuracy;
        public float CompletionTime;
    }

    /// <summary>
    /// Individual DNA base data
    /// </summary>
    [System.Serializable]
    public struct DNABase
    {
        public DNABaseType Type;
        public int Position;
        public bool IsCorrect;
    }

    /// <summary>
    /// DNA base types
    /// </summary>
    public enum DNABaseType
    {
        Adenine,    // A
        Thymine,    // T
        Guanine,    // G
        Cytosine    // C
    }

    /// <summary>
    /// DNA base display component
    /// </summary>
    public class DNABaseDisplay : MonoBehaviour
    {
        [SerializeField] private Image _baseImage;
        [SerializeField] private TextMeshProUGUI _baseLabel;
        [SerializeField] private Button _baseButton;
        [SerializeField] private GameObject _correctIndicator;
        [SerializeField] private GameObject _incorrectIndicator;

        private DNABase _baseData;
        private bool _isDraggable = false;
        private bool _isPlaced = false;

        public System.Action<DNABase, int> OnBasePlaced;
        public System.Action<DNABase> OnBaseRemoved;

        public void SetupAsTarget(DNABase baseData, Color baseColor)
        {
            _baseData = baseData;
            _isDraggable = false;

            if (_baseImage != null)
            {
                _baseImage.color = baseColor;
                _baseImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f); // Semi-transparent for target
            }

            if (_baseLabel != null)
                _baseLabel.text = GetBaseLetter(baseData.Type);

            if (_baseButton != null)
                _baseButton.interactable = false;

            if (_correctIndicator != null)
                _correctIndicator.SetActive(false);

            if (_incorrectIndicator != null)
                _incorrectIndicator.SetActive(false);
        }

        public void SetupAsDraggable(DNABase baseData, Color baseColor)
        {
            _baseData = baseData;
            _isDraggable = true;

            if (_baseImage != null)
                _baseImage.color = baseColor;

            if (_baseLabel != null)
                _baseLabel.text = GetBaseLetter(baseData.Type);

            if (_baseButton != null)
            {
                _baseButton.interactable = true;
                _baseButton.onClick.AddListener(OnBaseClicked);
            }

            if (_correctIndicator != null)
                _correctIndicator.SetActive(false);

            if (_incorrectIndicator != null)
                _incorrectIndicator.SetActive(false);
        }

        private void OnBaseClicked()
        {
            if (!_isDraggable) return;

            if (_isPlaced)
            {
                // Remove from current position
                OnBaseRemoved?.Invoke(_baseData);
                _isPlaced = false;
                UpdateVisualState();
            }
            else
            {
                // Try to place at next available position
                // This is simplified - in a real implementation, you'd have drop zones
                int targetPosition = FindNextAvailablePosition();
                if (targetPosition >= 0)
                {
                    OnBasePlaced?.Invoke(_baseData, targetPosition);
                    _isPlaced = true;
                    UpdateVisualState();
                }
            }
        }

        private void UpdateVisualState()
        {
            if (_correctIndicator != null)
                _correctIndicator.SetActive(_isPlaced && _baseData.IsCorrect);

            if (_incorrectIndicator != null)
                _incorrectIndicator.SetActive(_isPlaced && !_baseData.IsCorrect);
        }

        private int FindNextAvailablePosition()
        {
            // Simplified position finding - would be more sophisticated in real implementation
            return Random.Range(0, 8);
        }

        private string GetBaseLetter(DNABaseType baseType)
        {
            return baseType switch
            {
                DNABaseType.Adenine => "A",
                DNABaseType.Thymine => "T",
                DNABaseType.Guanine => "G",
                DNABaseType.Cytosine => "C",
                _ => "?"
            };
        }
    }
}