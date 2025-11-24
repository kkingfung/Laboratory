using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.UI.Animations
{
    /// <summary>
    /// Component that applies a UIAnimationPreset to UI animation components
    /// Drag-and-drop a preset asset to apply consistent animation settings
    /// </summary>
    [ExecuteAlways]
    public class UIAnimationPresetApplicator : MonoBehaviour
    {
        [Header("Preset Configuration")]
        [SerializeField] private UIAnimationPreset preset;
        [SerializeField] private bool applyToTransitionAnimator = true;
        [SerializeField] private bool applyToButtonAnimator = true;
        [SerializeField] private bool autoApplyOnLoad = true;

        [Header("Component References (Auto-detected)")]
        [SerializeField] private UITransitionAnimator transitionAnimator;
        [SerializeField] private UIButtonAnimator buttonAnimator;

        private UIAnimationPreset _lastAppliedPreset;

        private void OnValidate()
        {
            // Auto-detect components
            if (transitionAnimator == null)
                transitionAnimator = GetComponent<UITransitionAnimator>();

            if (buttonAnimator == null)
                buttonAnimator = GetComponent<UIButtonAnimator>();

            // Auto-apply when preset changes in editor
            #if UNITY_EDITOR
            if (preset != null && preset != _lastAppliedPreset)
            {
                ApplyPreset();
                _lastAppliedPreset = preset;
            }
            #endif
        }

        private void Start()
        {
            if (autoApplyOnLoad && Application.isPlaying)
            {
                ApplyPreset();
            }
        }

        /// <summary>
        /// Applies the current preset to all enabled animation components
        /// </summary>
        [ContextMenu("Apply Preset")]
        public void ApplyPreset()
        {
            if (preset == null)
            {
                Debug.LogWarning("[UIAnimationPresetApplicator] No preset assigned", this);
                return;
            }

            int appliedCount = 0;

            if (applyToTransitionAnimator && transitionAnimator != null)
            {
                preset.ApplyToTransitionAnimator(transitionAnimator);
                appliedCount++;
                Debug.Log($"[UIAnimationPresetApplicator] Applied '{preset.PresetName}' to UITransitionAnimator", this);
            }

            if (applyToButtonAnimator && buttonAnimator != null)
            {
                preset.ApplyToButtonAnimator(buttonAnimator);
                appliedCount++;
                Debug.Log($"[UIAnimationPresetApplicator] Applied '{preset.PresetName}' to UIButtonAnimator", this);
            }

            if (appliedCount == 0)
            {
                Debug.LogWarning("[UIAnimationPresetApplicator] No animation components found to apply preset to", this);
            }
            else
            {
                Debug.Log($"[UIAnimationPresetApplicator] Successfully applied '{preset.PresetName}' to {appliedCount} component(s)", this);
            }
        }

        /// <summary>
        /// Changes the preset and applies it immediately
        /// </summary>
        public void SetPreset(UIAnimationPreset newPreset)
        {
            preset = newPreset;
            _lastAppliedPreset = newPreset;
            ApplyPreset();
        }

        /// <summary>
        /// Editor utility to detect and cache component references
        /// </summary>
        [ContextMenu("Detect Components")]
        private void DetectComponents()
        {
            transitionAnimator = GetComponent<UITransitionAnimator>();
            buttonAnimator = GetComponent<UIButtonAnimator>();

            int found = 0;
            if (transitionAnimator != null) found++;
            if (buttonAnimator != null) found++;

            Debug.Log($"[UIAnimationPresetApplicator] Found {found} animation component(s)", this);

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}
