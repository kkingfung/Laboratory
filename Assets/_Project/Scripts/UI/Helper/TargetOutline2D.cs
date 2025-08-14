using UnityEngine;

namespace Gameplay.Targeting
{
    /// <summary>
    /// Simple 2D outline by rendering a pre-assigned child SpriteRenderer,
    /// scaled slightly larger and tinted.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TargetOutline2D : MonoBehaviour
    {
        #region Fields

        [Header("Outline Settings")]
        [SerializeField] private Color outlineColor = Color.yellow;
        [SerializeField, Min(0f)] private float outlineThickness = 0.05f;
        [SerializeField] private bool startDisabled = true;
        [SerializeField] private int sortingOrderDelta = -1; // outline behind by default

        [Header("References (Assign in Inspector)")]
        [SerializeField] private SpriteRenderer mainRenderer;    // assign the host's SpriteRenderer
        [SerializeField] private SpriteRenderer outlineRenderer; // assign a child SpriteRenderer used as outline

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            if (!ValidateRefs()) { enabled = false; return; }

            // Initialize outline visuals from current settings
            SyncSpriteOnce();
            ApplyColor(outlineColor);
            ApplyThickness(outlineThickness);
            ApplySortingOrder();

            SetOutlineActive(!startDisabled);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!ValidateRefs()) return;

            // Keep outline in sync while editing
            ApplyColor(outlineColor);
            ApplyThickness(outlineThickness);
            ApplySortingOrder();
        }
#endif

        #endregion

        #region Public Methods

        public void SetOutlineActive(bool active)
        {
            if (outlineRenderer != null)
                outlineRenderer.gameObject.SetActive(active);
        }

        public void SetOutlineColor(Color color)
        {
            outlineColor = color;
            ApplyColor(outlineColor);
        }

        public void SetOutlineThickness(float thickness)
        {
            outlineThickness = Mathf.Max(0f, thickness);
            ApplyThickness(outlineThickness);
        }

        #endregion

        #region Private Methods

        private bool ValidateRefs()
        {
            if (mainRenderer == null || outlineRenderer == null)
            {
                Debug.LogError($"[{nameof(TargetOutline2D)}] Missing references. " +
                               $"Assign both Main Renderer and Outline Renderer in the Inspector.", this);
                return false;
            }
            return true;
        }

        private void SyncSpriteOnce()
        {
            // Copy the current sprite once on init (optional: wire your own sprite-change event if needed)
            outlineRenderer.sprite = mainRenderer.sprite;
        }

        private void ApplyColor(Color color)
        {
            if (outlineRenderer != null)
                outlineRenderer.color = color;
        }

        private void ApplyThickness(float thickness)
        {
            if (outlineRenderer != null)
                outlineRenderer.transform.localScale = Vector3.one * (1f + thickness);
        }

        private void ApplySortingOrder()
        {
            if (outlineRenderer == null || mainRenderer == null) return;

            outlineRenderer.sortingLayerID = mainRenderer.sortingLayerID;
            outlineRenderer.sortingOrder   = mainRenderer.sortingOrder + sortingOrderDelta;
        }

        #endregion
    }
}
