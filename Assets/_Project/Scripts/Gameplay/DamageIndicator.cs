// DamageIndicator.cs
using System;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;

namespace Laboratory.Gameplay.UI
{
    /// <summary>
    /// Displays a floating damage indicator at a given position.
    /// </summary>
    public class DamageIndicator : MonoBehaviour
    {
        #region Fields

        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private Image image;
        [SerializeField] private float floatSpeed = 1.5f;
        #pragma warning disable 0414 // Field assigned but never used - reserved for future fade configuration
        [SerializeField] private float fadeDuration = 0.75f;
        #pragma warning restore 0414

        private Color _originalColor;
        private float _timer;
        private float _lifeTime;
        private float _fadeTime;
        private bool _isExpired;
        private RectTransform _rectTransform;
        private System.Action _onFinished;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the damage text component.
        /// </summary>
        public TextMeshProUGUI DamageText => damageText;

        /// <summary>
        /// Gets the image component.
        /// </summary>
        public Image Image => image;

        /// <summary>
        /// Gets the RectTransform component.
        /// </summary>
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        /// <summary>
        /// Gets whether the indicator has expired.
        /// </summary>
        public bool IsExpired => _isExpired;

        /// <summary>
        /// Event called when the indicator finishes its lifecycle.
        /// </summary>
        public event System.Action OnFinished
        {
            add => _onFinished += value;
            remove => _onFinished -= value;
        }

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (damageText != null)
                _originalColor = damageText.color;
        }

        private void OnEnable()
        {
            _timer = 0f;
            _isExpired = false;
            if (damageText != null)
                damageText.color = _originalColor;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the damage value to display.
        /// </summary>
        public void ShowDamage(int amount)
        {
            if (damageText != null)
                damageText.text = amount.ToString();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Setup the indicator with hit direction.
        /// </summary>
        /// <param name="hitDirection">Direction of the hit</param>
        public void Setup(Vector3 hitDirection)
        {
            // Implementation can be added based on specific needs
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Start the indicator's lifecycle.
        /// </summary>
        /// <param name="lifeTime">Total lifetime of the indicator</param>
        /// <param name="fadeTime">Time to fade out</param>
        public void StartLife(float lifeTime, float fadeTime)
        {
            _lifeTime = lifeTime;
            _fadeTime = fadeTime;
            _timer = 0f;
            _isExpired = false;
        }

        /// <summary>
        /// Update the indicator state.
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        public void UpdateIndicator(float deltaTime)
        {
            _timer += deltaTime;
            
            // Handle floating animation
            transform.position += Vector3.up * floatSpeed * deltaTime;
            
            // Handle fading
            if (_timer > (_lifeTime - _fadeTime))
            {
                float fadeProgress = (_timer - (_lifeTime - _fadeTime)) / _fadeTime;
                float alpha = Mathf.Lerp(_originalColor.a, 0f, fadeProgress);
                
                if (damageText != null)
                {
                    var color = damageText.color;
                    damageText.color = new Color(color.r, color.g, color.b, alpha);
                }
                
                if (image != null)
                {
                    var color = image.color;
                    image.color = new Color(color.r, color.g, color.b, alpha);
                }
            }
            
            // Check if expired
            if (_timer >= _lifeTime)
            {
                _isExpired = true;
                _onFinished?.Invoke();
            }
        }

        #endregion
    }
}
