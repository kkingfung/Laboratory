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
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float fadeDuration = 0.75f;

        private Color _originalColor;
        private float _timer;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            if (damageText != null)
                _originalColor = damageText.color;
        }

        private void OnEnable()
        {
            _timer = 0f;
            if (damageText != null)
                damageText.color = _originalColor;
        }

        private void Update()
        {
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            _timer += Time.deltaTime;

            if (damageText != null)
            {
                float alpha = Mathf.Lerp(_originalColor.a, 0f, _timer / fadeDuration);
                damageText.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, alpha);
            }

            if (_timer >= fadeDuration)
            {
                gameObject.SetActive(false);
            }
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

        #endregion
    }
}
