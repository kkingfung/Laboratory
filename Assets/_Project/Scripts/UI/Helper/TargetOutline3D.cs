using UnityEngine;

namespace Gameplay.Targeting
{
    /// <summary>
    /// Adds or toggles an outline effect for 3D objects when targeted or nearby.
    /// Requires an Outline-capable shader/material.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class TargetOutline3D : MonoBehaviour
    {
        [Header("Outline Settings")]
        [Tooltip("Outline color.")]
        [SerializeField] private Color outlineColor = Color.yellow;

        [Tooltip("Outline width.")]
        [SerializeField] private float outlineWidth = 3f;

        [Tooltip("If true, outline starts disabled.")]
        [SerializeField] private bool startDisabled = true;

        [Tooltip("Renderer component to apply outline to.")]
        [SerializeField] private Renderer targetRenderer = null!;

        private MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");


        private void Awake()
        {
            if (targetRenderer == null)
            {
                Debug.LogError($"{nameof(TargetOutline3D)} requires a Renderer assigned in the Inspector.", this);
                enabled = false;
                return;
            }

            _propertyBlock = new MaterialPropertyBlock();
            SetOutlineActive(!startDisabled);
        }


        /// <summary>
        /// Enables or disables the outline effect.
        /// </summary>
        /// <param name="active">Whether the outline should be visible.</param>
        public void SetOutlineActive(bool active)
        {
            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(OutlineColorID, outlineColor);
            _propertyBlock.SetFloat(OutlineWidthID, active ? outlineWidth : 0f);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Changes the outline color and enables the outline.
        /// </summary>
        /// <param name="color">New outline color.</param>
        public void SetOutlineColor(Color color)
        {
            outlineColor = color;
            SetOutlineActive(true);
        }
    }
}
