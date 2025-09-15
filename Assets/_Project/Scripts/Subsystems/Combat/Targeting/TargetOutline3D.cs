using UnityEngine;

namespace Laboratory.Gameplay.Visual
{
    /// <summary>
    /// Provides 3D outline effect for GameObjects using shader properties.
    /// Creates visual feedback for targeting, selection, or highlighting 3D objects.
    /// Requires materials with outline shader support (_OutlineColor and _OutlineWidth properties).
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class TargetOutline3D : MonoBehaviour
    {
        #region Fields

        [Header("Outline Configuration")]
        [Tooltip("Outline color tint")]
        [SerializeField] private Color outlineColor = Color.yellow;

        [Tooltip("Outline width/thickness")]
        [SerializeField] private float outlineWidth = 3f;

        [Tooltip("If true, outline starts disabled")]
        [SerializeField] private bool startDisabled = true;

        [Header("Component References")]
        [Tooltip("Renderer component to apply outline to")]
        [SerializeField] private Renderer targetRenderer = null!;

        private MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();

        #endregion

        #region Constants

        private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize component and validate references
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
            
            // Apply changes immediately in editor
            if (Application.isPlaying)
            {
                ApplyOutlineProperties();
            }
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
            if (!ValidateComponents()) return;
            
            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(OutlineColorID, outlineColor);
            _propertyBlock.SetFloat(OutlineWidthID, active ? outlineWidth : 0f);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Change the outline color and enable the outline
        /// </summary>
        /// <param name="color">New outline color</param>
        public void SetOutlineColor(Color color)
        {
            outlineColor = color;
            SetOutlineActive(true);
        }

        /// <summary>
        /// Change the outline width and apply immediately
        /// </summary>
        /// <param name="width">New outline width</param>
        public void SetOutlineWidth(float width)
        {
            outlineWidth = Mathf.Max(0f, width);
            ApplyOutlineProperties();
        }

        /// <summary>
        /// Get the current outline color
        /// </summary>
        /// <returns>Current outline color</returns>
        public Color GetOutlineColor() => outlineColor;

        /// <summary>
        /// Get the current outline width
        /// </summary>
        /// <returns>Current outline width</returns>
        public float GetOutlineWidth() => outlineWidth;

        /// <summary>
        /// Check if the outline is currently enabled
        /// </summary>
        /// <returns>True if outline width is greater than 0</returns>
        public bool IsOutlineActive()
        {
            if (!ValidateComponents()) return false;
            
            targetRenderer.GetPropertyBlock(_propertyBlock);
            return _propertyBlock.GetFloat(OutlineWidthID) > 0f;
        }

        /// <summary>
        /// Toggle outline on/off
        /// </summary>
        public void ToggleOutline()
        {
            SetOutlineActive(!IsOutlineActive());
        }

        #endregion

        #region Private Methods - Initialization

        /// <summary>
        /// Initialize outline settings
        /// </summary>
        private void InitializeOutline()
        {
            _propertyBlock = new MaterialPropertyBlock();
            SetOutlineActive(!startDisabled);
        }

        /// <summary>
        /// Validate that required components are assigned and functional
        /// </summary>
        /// <returns>True if components are valid</returns>
        private bool ValidateComponents()
        {
            if (targetRenderer == null)
            {
                Debug.LogError($"[{nameof(TargetOutline3D)}] Target Renderer is not assigned. " +
                               $"Please assign a Renderer component in the Inspector.", this);
                return false;
            }

            if (targetRenderer.material == null)
            {
                Debug.LogWarning($"[{nameof(TargetOutline3D)}] Target Renderer has no material. " +
                                 $"Outline effects may not work properly.", this);
                return false;
            }

            return true;
        }

        #endregion

        #region Private Methods - Utility

        /// <summary>
        /// Apply current outline properties to the renderer
        /// </summary>
        private void ApplyOutlineProperties()
        {
            if (!ValidateComponents()) return;
            
            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(OutlineColorID, outlineColor);
            _propertyBlock.SetFloat(OutlineWidthID, outlineWidth);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        /// <summary>
        /// Provide helpful information in the inspector
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void LogShaderInfo()
        {
            if (targetRenderer?.material != null)
            {
                var shader = targetRenderer.material.shader;
                bool hasOutlineColor = shader.FindPropertyIndex("_OutlineColor") >= 0;
                bool hasOutlineWidth = shader.FindPropertyIndex("_OutlineWidth") >= 0;
                
                if (!hasOutlineColor || !hasOutlineWidth)
                {
                    Debug.LogWarning($"[{nameof(TargetOutline3D)}] Shader '{shader.name}' may not support outline properties. " +
                                     $"Expected properties: _OutlineColor, _OutlineWidth", this);
                }
            }
        }
#endif

        #endregion
    }
}
