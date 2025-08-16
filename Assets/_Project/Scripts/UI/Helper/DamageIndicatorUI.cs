using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Laboratory.Gameplay.UI;
using Laboratory.UI.Utils;
using Laboratory.Models.ECS.Components;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// Damage types for styling damage indicators.
    /// </summary>
    public enum DamageType
    {
        Normal,
        Critical,
        Fire,
        Ice
    }

    /// <summary>
    /// UI component for displaying directional damage indicators around the screen edges.
    /// Shows damage source direction, amount, and provides audio/visual feedback.
    /// </summary>
    public class DamageIndicatorUI : MonoBehaviour
    {
        #region Fields

        [Header("References")]
        [SerializeField] private RectTransform indicatorsParent;
        [SerializeField] private DamageIndicator indicatorPrefab;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip damageSound;
        [SerializeField] private AudioClip criticalDamageSound;
        [SerializeField] private UIShakeEffect shakeEffect;

        [Header("Settings")]
        [SerializeField] private float indicatorDuration = 1.5f;
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private float distanceFromCenter = 100f;

        private readonly Queue<DamageIndicator> _indicatorPool = new();
        private readonly List<DamageIndicator> _activeIndicators = new();
        private Camera _mainCamera;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize main camera reference.
        /// </summary>
        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        /// <summary>
        /// Subscribe to damage events.
        /// </summary>
        private void OnEnable()
        {
            MessageBus.OnDamage += OnDamageEvent;
        }

        /// <summary>
        /// Unsubscribe from damage events.
        /// </summary>
        private void OnDisable()
        {
            MessageBus.OnDamage -= OnDamageEvent;
        }

        /// <summary>
        /// Update all active damage indicators.
        /// </summary>
        private void Update()
        {
            UpdateActiveIndicators();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Spawns a damage indicator with optional damage amount, damage type, and sound/vibration effects.
        /// </summary>
        /// <param name="sourcePosition">World position where damage came from</param>
        /// <param name="damageAmount">Optional damage amount to display. Pass null to hide.</param>
        /// <param name="damageType">Damage type for style and effects</param>
        /// <param name="playSound">Whether to play sound effect (default true)</param>
        /// <param name="vibrate">Whether to trigger vibration (default false)</param>
        public void SpawnIndicator(
            Vector3 sourcePosition,
            int? damageAmount = null,
            DamageType damageType = DamageType.Normal,
            bool playSound = true,
            bool vibrate = false)
        {
            var indicator = GetIndicatorFromPool();
            indicator.RectTransform.gameObject.SetActive(true);

            SetupIndicatorPosition(indicator, sourcePosition);
            ConfigureIndicatorAppearance(indicator, damageAmount, damageType);

            indicator.StartLife(indicatorDuration, fadeDuration);
            _activeIndicators.Add(indicator);

            HandleAudioFeedback(damageType, playSound);
            HandleVibrationFeedback(vibrate);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handle damage event from message bus.
        /// </summary>
        /// <param name="damageEvent">Damage event data</param>
        private void OnDamageEvent(DamageEvent damageEvent)
        {
            if (damageEvent.TargetId != NetworkManager.Singleton.LocalClientId) return;

            var indicator = GetIndicatorFromPool();
            indicator.Setup(damageEvent.HitDirection);
            indicator.OnFinished += () => _indicatorPool.Enqueue(indicator);
        }

        /// <summary>
        /// Set up indicator position and rotation based on damage source position.
        /// </summary>
        /// <param name="indicator">Indicator to position</param>
        /// <param name="sourcePosition">World position of damage source</param>
        private void SetupIndicatorPosition(DamageIndicator indicator, Vector3 sourcePosition)
        {
            Vector3 playerForward = _mainCamera.transform.forward;
            Vector3 toSource = (sourcePosition - _mainCamera.transform.position).normalized;

            Vector3 flatForward = new Vector3(playerForward.x, 0, playerForward.z).normalized;
            Vector3 flatToSource = new Vector3(toSource.x, 0, toSource.z).normalized;
            float angle = Vector3.SignedAngle(flatForward, flatToSource, Vector3.up);

            indicator.RectTransform.localRotation = Quaternion.Euler(0, 0, -angle);

            float radius = distanceFromCenter;
            Vector2 pos = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * radius;
            indicator.RectTransform.anchoredPosition = pos;
        }

        /// <summary>
        /// Configure indicator appearance based on damage amount and type.
        /// </summary>
        /// <param name="indicator">Indicator to configure</param>
        /// <param name="damageAmount">Optional damage amount</param>
        /// <param name="damageType">Type of damage</param>
        private void ConfigureIndicatorAppearance(DamageIndicator indicator, int? damageAmount, DamageType damageType)
        {
            ConfigureDamageText(indicator, damageAmount, damageType);
            ConfigureIndicatorColor(indicator, damageType);
        }

        /// <summary>
        /// Configure damage text display.
        /// </summary>
        /// <param name="indicator">Indicator to configure</param>
        /// <param name="damageAmount">Optional damage amount</param>
        /// <param name="damageType">Type of damage</param>
        private void ConfigureDamageText(DamageIndicator indicator, int? damageAmount, DamageType damageType)
        {
            if (damageAmount.HasValue)
            {
                indicator.DamageText.gameObject.SetActive(true);
                indicator.DamageText.text = damageAmount.Value.ToString();

                var (color, fontStyle) = GetDamageTextStyle(damageType);
                indicator.DamageText.color = color;
                indicator.DamageText.fontStyle = fontStyle;
            }
            else
            {
                indicator.DamageText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Configure indicator icon color based on damage type.
        /// </summary>
        /// <param name="indicator">Indicator to configure</param>
        /// <param name="damageType">Type of damage</param>
        private void ConfigureIndicatorColor(DamageIndicator indicator, DamageType damageType)
        {
            indicator.Image.color = damageType switch
            {
                DamageType.Critical => Color.red,
                DamageType.Fire => new Color(1f, 0.5f, 0f),
                DamageType.Ice => Color.cyan,
                _ => Color.white
            };
        }

        /// <summary>
        /// Get text style for damage type.
        /// </summary>
        /// <param name="damageType">Type of damage</param>
        /// <returns>Color and font style tuple</returns>
        private (Color color, FontStyle fontStyle) GetDamageTextStyle(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Critical => (Color.red, FontStyle.Bold),
                DamageType.Fire => (new Color(1f, 0.5f, 0f), FontStyle.Normal),
                DamageType.Ice => (Color.cyan, FontStyle.Normal),
                _ => (Color.white, FontStyle.Normal)
            };
        }

        /// <summary>
        /// Get damage indicator from pool or create new one.
        /// </summary>
        /// <returns>Available damage indicator</returns>
        private DamageIndicator GetIndicatorFromPool()
        {
            if (_indicatorPool.Count > 0)
            {
                return _indicatorPool.Dequeue();
            }
            
            var go = Instantiate(indicatorPrefab, indicatorsParent);
            return new DamageIndicator(go);
        }

        /// <summary>
        /// Return indicator to pool for reuse.
        /// </summary>
        /// <param name="indicator">Indicator to recycle</param>
        private void RecycleIndicator(DamageIndicator indicator)
        {
            indicator.RectTransform.gameObject.SetActive(false);
            _indicatorPool.Enqueue(indicator);
        }

        /// <summary>
        /// Update all active indicators and remove expired ones.
        /// </summary>
        private void UpdateActiveIndicators()
        {
            float delta = Time.unscaledDeltaTime;
            for (int i = _activeIndicators.Count - 1; i >= 0; i--)
            {
                var indicator = _activeIndicators[i];
                indicator.UpdateIndicator(delta);
                
                if (indicator.IsExpired)
                {
                    RecycleIndicator(indicator);
                    _activeIndicators.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Handle audio feedback for damage.
        /// </summary>
        /// <param name="damageType">Type of damage</param>
        /// <param name="playSound">Whether to play sound</param>
        private void HandleAudioFeedback(DamageType damageType, bool playSound)
        {
            if (!playSound || audioSource == null) return;

            AudioClip clipToPlay = (damageType == DamageType.Critical && criticalDamageSound != null)
                ? criticalDamageSound
                : damageSound;

            if (clipToPlay != null)
                audioSource.PlayOneShot(clipToPlay);
        }

        /// <summary>
        /// Handle vibration feedback for damage.
        /// </summary>
        /// <param name="vibrate">Whether to vibrate</param>
        private void HandleVibrationFeedback(bool vibrate)
        {
            if (vibrate && shakeEffect != null)
                shakeEffect.Shake();
        }

        #endregion
    }
}
