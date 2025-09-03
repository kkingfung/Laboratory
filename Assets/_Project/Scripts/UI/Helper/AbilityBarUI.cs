using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Laboratory.Gameplay.Abilities;
using Laboratory.Core.Abilities.Events;
using AbilityActivatedEvent = Laboratory.Core.Abilities.Events.AbilityActivatedEvent;
using AbilityStateChangedEvent = Laboratory.Core.Abilities.Events.AbilityStateChangedEvent;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// Enhanced UI component for displaying ability buttons and cooldown timers.
    /// Handles ability activation through the new unified event system.
    /// </summary>
    public class AbilityBarUI : MonoBehaviour, IDisposable
    {
        #region Fields

        [Header("UI References")]
        [SerializeField] private Button[] abilityButtons;
        [SerializeField] private TextMeshProUGUI[] cooldownTexts;
        [SerializeField] private Image[] cooldownFillImages;
        
        [Header("Settings")]
        [SerializeField] private bool enableAnimations = true;
        [SerializeField] private Color cooldownColor = Color.red;
        [SerializeField] private Color readyColor = Color.white;

        private AbilityManager _targetManager;
        private bool _isSubscribed = false;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize button click handlers and find target ability manager.
        /// </summary>
        private void Awake()
        {
            SetupButtonHandlers();
            FindTargetManager();
        }

        /// <summary>
        /// Subscribe to ability events when enabled.
        /// </summary>
        private void OnEnable()
        {
            SubscribeToEvents();
        }

        /// <summary>
        /// Unsubscribe from ability events when disabled.
        /// </summary>
        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the target ability manager to display.
        /// </summary>
        public void SetTargetManager(AbilityManager manager)
        {
            _targetManager = manager;
            RefreshDisplay();
        }

        /// <summary>
        /// Updates the cooldown display for a specific ability.
        /// This method provides direct access for external systems that need to update cooldowns.
        /// </summary>
        /// <param name="abilityIndex">Index of the ability to update</param>
        /// <param name="cooldownRemaining">Remaining cooldown time in seconds</param>
        public void UpdateCooldown(int abilityIndex, float cooldownRemaining)
        {
            if (!ValidateIndex(abilityIndex)) return;

            bool isOnCooldown = cooldownRemaining > 0f;
            
            // Update button interactability
            abilityButtons[abilityIndex].interactable = !isOnCooldown;
            
            // Update text display
            if (cooldownTexts[abilityIndex] != null)
            {
                cooldownTexts[abilityIndex].text = isOnCooldown
                    ? $"{Mathf.CeilToInt(cooldownRemaining)}s"
                    : "";
            }
            
            // Update fill image if available
            if (cooldownFillImages[abilityIndex] != null && _targetManager != null)
            {
                var abilityData = _targetManager.GetAbilityData(abilityIndex);
                if (abilityData != null)
                {
                    float fillAmount = isOnCooldown ? (cooldownRemaining / abilityData.cooldownDuration) : 0f;
                    cooldownFillImages[abilityIndex].fillAmount = fillAmount;
                    cooldownFillImages[abilityIndex].color = isOnCooldown ? cooldownColor : readyColor;
                }
            }
        }

        /// <summary>
        /// Refreshes the entire UI display based on current ability manager state.
        /// </summary>
        public void RefreshDisplay()
        {
            if (_targetManager == null) return;

            for (int i = 0; i < Mathf.Min(abilityButtons.Length, _targetManager.AbilityCount); i++)
            {
                UpdateCooldown(i, _targetManager.GetAbilityCooldown(i));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets up button click handlers for ability activation.
        /// </summary>
        private void SetupButtonHandlers()
        {
            for (int i = 0; i < abilityButtons.Length; i++)
            {
                int index = i; // Capture loop variable
                abilityButtons[i].onClick.AddListener(() => RequestAbilityActivation(index));
            }
        }

        /// <summary>
        /// Finds the target ability manager in the scene or on this GameObject.
        /// </summary>
        private void FindTargetManager()
        {
            // Try to find on same GameObject first
            _targetManager = GetComponent<AbilityManager>();
            
            // If not found, try to find in parent
            if (_targetManager == null)
            {
                _targetManager = GetComponentInParent<AbilityManager>();
            }
            
            // If still not found, try to find anywhere in scene
            if (_targetManager == null)
            {
                _targetManager = FindFirstObjectByType<AbilityManager>();
            }
        }

        /// <summary>
        /// Requests activation of an ability through the target manager.
        /// </summary>
        private void RequestAbilityActivation(int index)
        {
            if (_targetManager != null)
            {
                _targetManager.TryActivateAbility(index);
            }
            else
            {
                Debug.LogWarning("[AbilityBarUI] No target AbilityManager found for activation request");
            }
        }

        /// <summary>
        /// Validates that the ability index is within valid range.
        /// </summary>
        private bool ValidateIndex(int index)
        {
            return index >= 0 && index < abilityButtons.Length;
        }

        /// <summary>
        /// Subscribes to ability events.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_isSubscribed) return;
            
            AbilityEventBus.OnAbilityStateChanged.AddListener(OnAbilityStateChanged);
            AbilityEventBus.OnAbilityActivated.AddListener(OnAbilityActivated);
            _isSubscribed = true;
        }

        /// <summary>
        /// Unsubscribes from ability events.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (!_isSubscribed) return;
            
            AbilityEventBus.OnAbilityStateChanged.RemoveListener(OnAbilityStateChanged);
            AbilityEventBus.OnAbilityActivated.RemoveListener(OnAbilityActivated);
            _isSubscribed = false;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles ability state change events and updates UI accordingly.
        /// </summary>
        private void OnAbilityStateChanged(AbilityStateChangedEvent evt)
        {
            // Only respond to events from our target manager
            if (evt.Source != null && _targetManager != null && evt.Source.gameObject == _targetManager.gameObject)
            {
                UpdateCooldown(evt.AbilityIndex, evt.CooldownRemaining);
            }
        }

        /// <summary>
        /// Handles ability activation events.
        /// </summary>
        private void OnAbilityActivated(AbilityActivatedEvent evt)
        {
            // Only respond to events from our target manager
            if (evt.Source != null && _targetManager != null && evt.Source.gameObject == _targetManager.gameObject)
            {
                Debug.Log($"[AbilityBarUI] Ability {evt.AbilityIndex} activated (UI received event)");
                
                // Trigger visual feedback if enabled
                if (enableAnimations && ValidateIndex(evt.AbilityIndex))
                {
                    StartCoroutine(AnimateAbilityActivation(evt.AbilityIndex));
                }
            }
        }

        #endregion

        #region Animation

        /// <summary>
        /// Animates ability activation with visual feedback.
        /// </summary>
        private System.Collections.IEnumerator AnimateAbilityActivation(int abilityIndex)
        {
            if (!ValidateIndex(abilityIndex)) yield break;
            
            var button = abilityButtons[abilityIndex];
            var originalScale = button.transform.localScale;
            
            // Scale up animation
            float duration = 0.1f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 1.2f, elapsed / duration);
                button.transform.localScale = originalScale * scale;
                yield return null;
            }
            
            // Scale back down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1.2f, 1f, elapsed / duration);
                button.transform.localScale = originalScale * scale;
                yield return null;
            }
            
            button.transform.localScale = originalScale;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of event subscriptions and cleanup resources.
        /// </summary>
        public void Dispose()
        {
            UnsubscribeFromEvents();
        }

        #endregion
    }
}
