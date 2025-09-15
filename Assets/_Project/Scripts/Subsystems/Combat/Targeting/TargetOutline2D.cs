using UnityEngine;

namespace Laboratory.Gameplay.Visual
{
    /// <summary>
    /// Provides 2D outline effect for sprites using a scaled child SpriteRenderer.
    /// Creates visual feedback for targeting, selection, or highlighting 2D objects.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TargetOutline2D : MonoBehaviour
    {
        #region Fields

        [Header("Outline Configuration")]
        [SerializeField] private Color outlineColor = Color.yellow;
        [SerializeField, Min(0f)] private float outlineThickness = 0.05f;
        [SerializeField] private bool startDisabled = true;
        [SerializeField] private int sortingOrderDelta = -1; // outline behind by default

        [Header("Component References")]
        [SerializeField] private SpriteRenderer mainRenderer;    // assign the host's SpriteRenderer
        [SerializeField] private SpriteRenderer outlineRenderer; // assign a child SpriteRenderer used as outline

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize outline settings and validate components
        /// </summary>
        private void Awake()
        {
            if (!ValidateComponents()) 
            { 
                enabled = false; 
                return; 
            }

            InitializeOutline();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Update outline properties during editor modifications
        /// </summary>
        private void OnValidate()
        {
            if (!ValidateComponents()) return;

            // Keep outline in sync while editing
            ApplyColor(outlineColor);
            ApplyThickness(outlineThickness);
            ApplySortingOrder();
        }
#endif

        #endregion

        #region Public Methods

        /// <summary>
        /// Enable or disable the outline effect
        /// </summary>
        /// <param name="active">True to show outline, false to hide</param>
        public void SetOutlineActive(bool active)
        {
            if (outlineRenderer != null)
                outlineRenderer.gameObject.SetActive(active);
        }

        /// <summary>
        /// Change the outline color and apply immediately
        /// </summary>
        /// <param name="color">New outline color</param>
        public void SetOutlineColor(Color color)
        {
            outlineColor = color;
            ApplyColor(outlineColor);
        }

        /// <summary>
        /// Change the outline thickness and apply immediately
        /// </summary>
        /// <param name="thickness">New outline thickness (0 or greater)</param>
        public void SetOutlineThickness(float thickness)
        {
            outlineThickness = Mathf.Max(0f, thickness);
            ApplyThickness(outlineThickness);
        }

        /// <summary>
        /// Get the current outline color
        /// </summary>
        /// <returns>Current outline color</returns>
        public Color GetOutlineColor() => outlineColor;

        /// <summary>
        /// Get the current outline thickness
        /// </summary>
        /// <returns>Current outline thickness</returns>
        public float GetOutlineThickness() => outlineThickness;

        /// <summary>
        /// Check if the outline is currently active
        /// </summary>
        /// <returns>True if outline is visible</returns>
        public bool IsOutlineActive() => outlineRenderer != null && outlineRenderer.gameObject.activeInHierarchy;

        #endregion

        #region Private Methods - Initialization

        /// <summary>
        /// Initialize all outline visual properties
        /// </summary>
        private void InitializeOutline()
        {
            SyncSpriteOnce();
            ApplyColor(outlineColor);
            ApplyThickness(outlineThickness);
            ApplySortingOrder();
            SetOutlineActive(!startDisabled);
        }

        /// <summary>
        /// Validate that required component references are assigned
        /// </summary>
        /// <returns>True if all components are valid</returns>
        private bool ValidateComponents()
        {
            if (mainRenderer == null || outlineRenderer == null)
            {
                Debug.LogError($"[{nameof(TargetOutline2D)}] Missing component references. " +
                               $"Assign both Main Renderer and Outline Renderer in the Inspector.", this);
                return false;
            }
            return true;
        }

        #endregion

        #region Private Methods - Visual Updates

        /// <summary>
        /// Synchronize the outline sprite with the main sprite
        /// </summary>
        private void SyncSpriteOnce()
        {
            // Copy the current sprite once on init 
            // NOTE: Wire your own sprite-change event if dynamic sprite changes are needed
            outlineRenderer.sprite = mainRenderer.sprite;
        }

        /// <summary>
        /// Apply color to the outline renderer
        /// </summary>
        /// <param name="color">Color to apply</param>
        private void ApplyColor(Color color)
        {
            if (outlineRenderer != null)
                outlineRenderer.color = color;
        }

        /// <summary>
        /// Apply thickness scaling to the outline renderer
        /// </summary>
        /// <param name="thickness">Thickness value to apply</param>
        private void ApplyThickness(float thickness)
        {
            if (outlineRenderer != null)
                outlineRenderer.transform.localScale = Vector3.one * (1f + thickness);
        }

        /// <summary>
        /// Apply sorting order settings to maintain proper layering
        /// </summary>
        private void ApplySortingOrder()
        {
            if (outlineRenderer == null || mainRenderer == null) return;

            outlineRenderer.sortingLayerID = mainRenderer.sortingLayerID;
            outlineRenderer.sortingOrder   = mainRenderer.sortingOrder + sortingOrderDelta;
        }

        #endregion
    }
}
