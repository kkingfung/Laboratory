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
        [SerializeField] private Color outlineColor = Color.yellow;
        [SerializeField] private float outlineWidth = 3f;
        [SerializeField] private bool startDisabled = true;

        private Renderer _targetRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");

        private void Awake()
        {
            _targetRenderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();

            SetOutlineActive(!startDisabled);
        }

        public void SetOutlineActive(bool active)
        {
            _targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(OutlineColorID, outlineColor);
            _propertyBlock.SetFloat(OutlineWidthID, active ? outlineWidth : 0f);
            _targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        public void SetOutlineColor(Color color)
        {
            outlineColor = color;
            SetOutlineActive(true);
        }
    }
}
