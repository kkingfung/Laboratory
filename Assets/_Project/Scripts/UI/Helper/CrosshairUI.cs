using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// UI component for managing crosshair appearance and behavior.
    /// Provides visual feedback for shooting, hit confirmation, and dynamic sizing.
    /// </summary>
    public class CrosshairUI : MonoBehaviour
    {
        #region Fields

        [Header("UI References")]
        [SerializeField] private Image crosshairImage;

        [Header("Size Settings")]
        [SerializeField] private float defaultSize = 40f;
        [SerializeField] private float expandedSize = 60f;
        [SerializeField] private float expandDuration = 0.2f;

        [Header("Color Settings")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private float hitFlashDuration = 0.15f;

        private Coroutine _currentExpandCoroutine;
        private Coroutine _currentHitCoroutine;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize crosshair image and reset to default appearance.
        /// </summary>
        private void Awake()
        {
            if (crosshairImage == null)
            {
                Debug.LogError("Crosshair Image not assigned!");
                enabled = false;
                return;
            }
            ResetCrosshair();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets crosshair to default appearance.
        /// </summary>
        public void ResetCrosshair()
        {
            crosshairImage.rectTransform.sizeDelta = new Vector2(defaultSize, defaultSize);
            crosshairImage.color = defaultColor;
        }

        /// <summary>
        /// Expands crosshair size temporarily (e.g., when shooting or moving).
        /// </summary>
        public void ExpandCrosshair()
        {
            if (_currentExpandCoroutine != null)
                StopCoroutine(_currentExpandCoroutine);
            _currentExpandCoroutine = StartCoroutine(ExpandRoutine());
        }

        /// <summary>
        /// Flashes the crosshair color to indicate a hit.
        /// </summary>
        public void ShowHitFeedback()
        {
            if (_currentHitCoroutine != null)
                StopCoroutine(_currentHitCoroutine);
            _currentHitCoroutine = StartCoroutine(HitFlashRoutine());
        }

        /// <summary>
        /// Changes crosshair sprite.
        /// </summary>
        /// <param name="newSprite">New sprite to use for the crosshair</param>
        public void SetCrosshairSprite(Sprite newSprite)
        {
            crosshairImage.sprite = newSprite;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Coroutine for expanding and contracting the crosshair.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator ExpandRoutine()
        {
            // Expand phase
            float elapsed = 0f;
            Vector2 startSize = crosshairImage.rectTransform.sizeDelta;
            Vector2 targetSize = new Vector2(expandedSize, expandedSize);

            while (elapsed < expandDuration)
            {
                elapsed += Time.deltaTime;
                crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(startSize, targetSize, elapsed / expandDuration);
                yield return null;
            }

            // Contract phase
            elapsed = 0f;
            while (elapsed < expandDuration)
            {
                elapsed += Time.deltaTime;
                crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(targetSize, new Vector2(defaultSize, defaultSize), elapsed / expandDuration);
                yield return null;
            }

            crosshairImage.rectTransform.sizeDelta = new Vector2(defaultSize, defaultSize);
        }

        /// <summary>
        /// Coroutine for flashing the crosshair color on hit.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator HitFlashRoutine()
        {
            crosshairImage.color = hitColor;
            yield return new WaitForSeconds(hitFlashDuration);
            crosshairImage.color = defaultColor;
        }

        #endregion
    }
}
