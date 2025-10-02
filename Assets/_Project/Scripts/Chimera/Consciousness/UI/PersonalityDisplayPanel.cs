using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Laboratory.Chimera.Consciousness.Core;
using Laboratory.Chimera.Consciousness.Memory;
using Laboratory.Chimera.Consciousness.Behavior;
using System.Collections;
using System.Collections.Generic;

namespace Laboratory.Chimera.Consciousness.UI
{
    /// <summary>
    /// UI panel for displaying creature personality, memories, and relationships
    /// Creates deep emotional connection by showing the creature's inner life
    /// </summary>
    public class PersonalityDisplayPanel : MonoBehaviour
    {
        [Header("Personality Display")]
        [SerializeField] private TextMeshProUGUI _creatureName;
        [SerializeField] private TextMeshProUGUI _personalityDescription;
        [SerializeField] private Transform _personalityTraitsContainer;
        [SerializeField] private GameObject _personalityTraitPrefab;

        [Header("Emotional State")]
        [SerializeField] private Image _moodIcon;
        [SerializeField] private Slider _happinessSlider;
        [SerializeField] private Slider _stressSlider;
        [SerializeField] private Slider _energySlider;
        [SerializeField] private TextMeshProUGUI _currentMoodText;

        [Header("Memories & Relationships")]
        [SerializeField] private Transform _memoryContainer;
        [SerializeField] private GameObject _memoryItemPrefab;
        [SerializeField] private Transform _relationshipContainer;
        [SerializeField] private GameObject _relationshipItemPrefab;

        [Header("Preferences")]
        [SerializeField] private Transform _preferencesContainer;
        [SerializeField] private GameObject _preferenceItemPrefab;

        [Header("Animation")]
        [SerializeField] private float _animationDuration = 1.0f;
        [SerializeField] private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Mood Icons")]
        [SerializeField] private Sprite[] _moodSprites; // Array of mood icons

        private PersonalityBehaviorSystem _personalitySystem;
        private CreaturePersonality _currentPersonality;
        private CreatureMemory _currentMemory;

        /// <summary>
        /// Display creature personality with beautiful visualization
        /// </summary>
        public void DisplayCreaturePersonality(PersonalityBehaviorSystem personalitySystem, string creatureName)
        {
            _personalitySystem = personalitySystem;
            _currentPersonality = personalitySystem.GetPersonality();
            _currentMemory = personalitySystem.GetMemory();
            _creatureName.text = creatureName;

            StartCoroutine(AnimatePersonalityDisplay());
        }

        /// <summary>
        /// Update emotional state display in real-time
        /// </summary>
        public void UpdateEmotionalState()
        {
            if (_personalitySystem == null) return;

            _currentPersonality = _personalitySystem.GetPersonality();

            // Update mood display
            _currentMoodText.text = GetMoodDisplayName(_currentPersonality.CurrentMood);
            _moodIcon.sprite = GetMoodSprite(_currentPersonality.CurrentMood);

            // Update emotional sliders
            _happinessSlider.value = _currentPersonality.HappinessLevel;
            _stressSlider.value = _currentPersonality.StressLevel;
            _energySlider.value = _currentPersonality.EnergyLevel;

            // Color sliders based on values
            UpdateSliderColors();
        }

        /// <summary>
        /// Animate the complete personality display
        /// </summary>
        private IEnumerator AnimatePersonalityDisplay()
        {
            // Clear previous content
            ClearAllDisplays();

            // Show personality description
            _personalityDescription.text = _currentPersonality.GetPersonalityDescription();

            // Animate personality traits
            yield return StartCoroutine(AnimatePersonalityTraits());

            // Animate emotional state
            yield return StartCoroutine(AnimateEmotionalState());

            // Animate preferences
            yield return StartCoroutine(AnimatePreferences());

            // Animate memories and relationships
            yield return StartCoroutine(AnimateMemoriesAndRelationships());
        }

        /// <summary>
        /// Create animated personality trait bars
        /// </summary>
        private IEnumerator AnimatePersonalityTraits()
        {
            var traits = new (string name, byte value)[]
            {
                ("Curiosity", _currentPersonality.Curiosity),
                ("Playfulness", _currentPersonality.Playfulness),
                ("Aggression", _currentPersonality.Aggression),
                ("Affection", _currentPersonality.Affection),
                ("Independence", _currentPersonality.Independence),
                ("Nervousness", _currentPersonality.Nervousness),
                ("Stubbornness", _currentPersonality.Stubbornness),
                ("Loyalty", _currentPersonality.Loyalty)
            };

            foreach (var (name, value) in traits)
            {
                GameObject traitObj = Instantiate(_personalityTraitPrefab, _personalityTraitsContainer);
                PersonalityTraitBar traitBar = traitObj.GetComponent<PersonalityTraitBar>();

                if (traitBar != null)
                {
                    Color traitColor = GetTraitColor(name, value);
                    traitBar.Initialize(name, value, traitColor);
                    yield return StartCoroutine(traitBar.AnimateAppearance(_animationDuration * 0.3f, _animationCurve));
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// Animate emotional state sliders
        /// </summary>
        private IEnumerator AnimateEmotionalState()
        {
            // Reset sliders to zero
            _happinessSlider.value = 0;
            _stressSlider.value = 0;
            _energySlider.value = 0;

            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = _animationCurve.Evaluate(elapsed / _animationDuration);

                _happinessSlider.value = Mathf.Lerp(0, _currentPersonality.HappinessLevel, t);
                _stressSlider.value = Mathf.Lerp(0, _currentPersonality.StressLevel, t);
                _energySlider.value = Mathf.Lerp(0, _currentPersonality.EnergyLevel, t);

                yield return null;
            }

            // Set final values
            UpdateEmotionalState();
        }

        /// <summary>
        /// Display creature preferences and likes/dislikes
        /// </summary>
        private IEnumerator AnimatePreferences()
        {
            var preferences = GetPreferencesList();

            foreach (var preference in preferences)
            {
                GameObject prefObj = Instantiate(_preferenceItemPrefab, _preferencesContainer);

                // Setup preference display
                TextMeshProUGUI prefText = prefObj.GetComponentInChildren<TextMeshProUGUI>();
                Image prefIcon = prefObj.GetComponentInChildren<Image>();

                if (prefText != null)
                {
                    prefText.text = preference.description;
                }

                if (prefIcon != null)
                {
                    prefIcon.color = preference.isLiked ? Color.green : Color.red;
                }

                // Animate appearance
                prefObj.transform.localScale = Vector3.zero;
                StartCoroutine(AnimateScale(prefObj.transform, Vector3.one, 0.3f));

                yield return new WaitForSeconds(0.15f);
            }
        }

        /// <summary>
        /// Display memories and relationships
        /// </summary>
        private IEnumerator AnimateMemoriesAndRelationships()
        {
            // Display important memories
            for (int i = 0; i < _currentMemory.SignificantEvents.Length && i < 5; i++)
            {
                var memory = _currentMemory.SignificantEvents[i];
                GameObject memoryObj = Instantiate(_memoryItemPrefab, _memoryContainer);

                MemoryDisplayItem memoryDisplay = memoryObj.GetComponent<MemoryDisplayItem>();
                if (memoryDisplay != null)
                {
                    memoryDisplay.SetupMemory(memory);
                    yield return StartCoroutine(memoryDisplay.AnimateAppearance(0.4f));
                }

                yield return new WaitForSeconds(0.2f);
            }

            // Display player relationships
            for (int i = 0; i < _currentMemory.KnownPlayers.Length && i < 3; i++)
            {
                var relationship = _currentMemory.KnownPlayers[i];
                GameObject relationshipObj = Instantiate(_relationshipItemPrefab, _relationshipContainer);

                RelationshipDisplayItem relationshipDisplay = relationshipObj.GetComponent<RelationshipDisplayItem>();
                if (relationshipDisplay != null)
                {
                    relationshipDisplay.SetupRelationship(relationship);
                    yield return StartCoroutine(relationshipDisplay.AnimateAppearance(0.4f));
                }

                yield return new WaitForSeconds(0.2f);
            }
        }

        /// <summary>
        /// Get list of creature preferences for display
        /// </summary>
        private List<(string description, bool isLiked)> GetPreferencesList()
        {
            var preferences = new List<(string, bool)>();

            // Food preferences
            if (_currentPersonality.FoodLikes.PrefersMeat)
                preferences.Add(("🥩 Loves meat", true));
            if (_currentPersonality.FoodLikes.PrefersVegetation)
                preferences.Add(("🌿 Enjoys plants", true));
            if (_currentPersonality.FoodLikes.PrefersSweets)
                preferences.Add(("🍯 Has sweet tooth", true));

            // Activity preferences
            if (_currentPersonality.PreferredActivities.LikesExploring)
                preferences.Add(("🗺️ Loves exploring", true));
            if (_currentPersonality.PreferredActivities.LikesSwimming)
                preferences.Add(("🏊 Enjoys swimming", true));
            if (_currentPersonality.PreferredActivities.LikesClimbing)
                preferences.Add(("🧗 Likes climbing", true));

            // Social preferences
            if (_currentPersonality.SocialBehavior.PrefersGroups)
                preferences.Add(("👥 Enjoys company", true));
            else
                preferences.Add(("🧘 Prefers solitude", true));

            // Environmental preferences
            if (_currentPersonality.HabitatLikes.PrefersWarmth)
                preferences.Add(("☀️ Likes warmth", true));
            if (_currentPersonality.HabitatLikes.LikesWater)
                preferences.Add(("💧 Loves water", true));

            return preferences;
        }

        /// <summary>
        /// Get color for personality traits
        /// </summary>
        private Color GetTraitColor(string traitName, byte value)
        {
            // Color intensity based on trait strength
            float intensity = value / 100f;

            Color baseColor = traitName switch
            {
                "Curiosity" => Color.yellow,
                "Playfulness" => Color.green,
                "Aggression" => Color.red,
                "Affection" => Color.magenta,
                "Independence" => Color.blue,
                "Nervousness" => Color.cyan,
                "Stubbornness" => Color.gray,
                "Loyalty" => new Color(1f, 0.5f, 0f), // Orange
                _ => Color.white
            };

            return new Color(baseColor.r * intensity, baseColor.g * intensity, baseColor.b * intensity, 1f);
        }

        /// <summary>
        /// Get display name for mood states
        /// </summary>
        private string GetMoodDisplayName(Laboratory.Chimera.Consciousness.Core.EmotionalState mood)
        {
            return mood switch
            {
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Depressed => "😢 Depressed",
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Sad => "😟 Sad",
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Neutral => "😐 Neutral",
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Content => "🙂 Content",
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Happy => "😊 Happy",
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Excited => "🤩 Excited",
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Angry => "😡 Angry",
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Fearful => "😨 Fearful",
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Playful => "😄 Playful",
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Loving => "🥰 Loving",
                _ => "😐 Unknown"
            };
        }

        /// <summary>
        /// Get sprite for mood display
        /// </summary>
        private Sprite GetMoodSprite(Laboratory.Chimera.Consciousness.Core.EmotionalState mood)
        {
            if (_moodSprites == null || _moodSprites.Length == 0)
                return null;

            int index = (int)mood;
            return index < _moodSprites.Length ? _moodSprites[index] : _moodSprites[0];
        }

        /// <summary>
        /// Update slider colors based on values
        /// </summary>
        private void UpdateSliderColors()
        {
            // Happiness: green when high
            _happinessSlider.fillRect.GetComponent<Image>().color = Color.Lerp(Color.gray, Color.green, _happinessSlider.value);

            // Stress: red when high
            _stressSlider.fillRect.GetComponent<Image>().color = Color.Lerp(Color.gray, Color.red, _stressSlider.value);

            // Energy: yellow when high
            _energySlider.fillRect.GetComponent<Image>().color = Color.Lerp(Color.gray, Color.yellow, _energySlider.value);
        }

        /// <summary>
        /// Animate scale with smooth transition
        /// </summary>
        private IEnumerator AnimateScale(Transform target, Vector3 finalScale, float duration)
        {
            Vector3 startScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = _animationCurve.Evaluate(elapsed / duration);
                target.localScale = Vector3.Lerp(startScale, finalScale, t);
                yield return null;
            }

            target.localScale = finalScale;
        }

        /// <summary>
        /// Clear all display containers
        /// </summary>
        private void ClearAllDisplays()
        {
            ClearContainer(_personalityTraitsContainer);
            ClearContainer(_memoryContainer);
            ClearContainer(_relationshipContainer);
            ClearContainer(_preferencesContainer);
        }

        /// <summary>
        /// Clear container of child objects
        /// </summary>
        private void ClearContainer(Transform container)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Start real-time emotional updates
        /// </summary>
        private void Update()
        {
            if (_personalitySystem != null)
            {
                UpdateEmotionalState();
            }
        }

        /// <summary>
        /// Public API for triggering personality interactions
        /// </summary>
        public void TriggerPlayerInteraction(string playerID, Laboratory.Chimera.Consciousness.Memory.InteractionType interactionType, float intensity)
        {
            if (_personalitySystem != null)
            {
                _personalitySystem.ReactToPlayerInteraction(playerID, interactionType, intensity);

                // Show visual feedback for the interaction
                StartCoroutine(ShowInteractionFeedback(interactionType, intensity));
            }
        }

        /// <summary>
        /// Show visual feedback for player interactions
        /// </summary>
        private IEnumerator ShowInteractionFeedback(Laboratory.Chimera.Consciousness.Memory.InteractionType interactionType, float intensity)
        {
            // Flash the appropriate emotional element
            Image targetImage = interactionType == Laboratory.Chimera.Consciousness.Memory.InteractionType.Positive ?
                _happinessSlider.fillRect.GetComponent<Image>() :
                _stressSlider.fillRect.GetComponent<Image>();

            Color originalColor = targetImage.color;
            Color flashColor = interactionType == InteractionType.Positive ? Color.white : Color.red;

            // Flash effect
            for (int i = 0; i < 3; i++)
            {
                targetImage.color = flashColor;
                yield return new WaitForSeconds(0.1f);
                targetImage.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}