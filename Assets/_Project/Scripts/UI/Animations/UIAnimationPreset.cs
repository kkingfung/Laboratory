using UnityEngine;
using DG.Tweening;

namespace Laboratory.UI.Animations
{
    /// <summary>
    /// ScriptableObject preset for UI animation settings
    /// Provides designer-friendly configuration for common animation patterns
    /// Use with UITransitionAnimator, UIButtonAnimator, and other animation components
    /// </summary>
    [CreateAssetMenu(fileName = "UIAnimationPreset_", menuName = "Chimera/UI/Animation Preset")]
    public class UIAnimationPreset : ScriptableObject
    {
        [Header("Preset Information")]
        [SerializeField] private string presetName = "New Preset";
        [TextArea(2, 4)]
        [SerializeField] private string description = "Describe this animation preset...";

        [Header("Transition Settings")]
        [SerializeField] private TransitionType transitionType = TransitionType.FadeSlideIn;
        [SerializeField] private float showDuration = 0.4f;
        [SerializeField] private float hideDuration = 0.3f;
        [SerializeField] private Ease showEase = Ease.OutCubic;
        [SerializeField] private Ease hideEase = Ease.InCubic;
        [SerializeField] private SlideDirection slideDirection = SlideDirection.Bottom;
        [SerializeField] private float startScale = 0.8f;

        [Header("Button Animation Settings")]
        [SerializeField] private bool enableHoverScale = true;
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float hoverDuration = 0.2f;
        [SerializeField] private Ease hoverEase = Ease.OutBack;

        [SerializeField] private bool enablePressScale = true;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float pressDuration = 0.1f;

        [SerializeField] private bool enableClickPunch = true;
        [SerializeField] private float punchStrength = 0.1f;
        [SerializeField] private float punchDuration = 0.3f;
        [SerializeField] private int punchVibrato = 10;

        [Header("Color Settings")]
        [SerializeField] private bool enableColorChange = false;
        [SerializeField] private Color hoverColor = Color.white;
        [SerializeField] private float colorDuration = 0.2f;

        // Public properties
        public string PresetName => presetName;
        public string Description => description;
        public TransitionType Transition => transitionType;
        public float ShowDuration => showDuration;
        public float HideDuration => hideDuration;
        public Ease ShowEase => showEase;
        public Ease HideEase => hideEase;
        public SlideDirection SlideDir => slideDirection;
        public float StartScale => startScale;

        public bool EnableHoverScale => enableHoverScale;
        public float HoverScale => hoverScale;
        public float HoverDuration => hoverDuration;
        public Ease HoverEase => hoverEase;

        public bool EnablePressScale => enablePressScale;
        public float PressScale => pressScale;
        public float PressDuration => pressDuration;

        public bool EnableClickPunch => enableClickPunch;
        public float PunchStrength => punchStrength;
        public float PunchDuration => punchDuration;
        public int PunchVibrato => punchVibrato;

        public bool EnableColorChange => enableColorChange;
        public Color HoverColor => hoverColor;
        public float ColorDuration => colorDuration;

        /// <summary>
        /// Applies this preset to a UITransitionAnimator component
        /// </summary>
        public void ApplyToTransitionAnimator(UITransitionAnimator animator)
        {
            if (animator == null)
            {
                Debug.LogWarning("[UIAnimationPreset] Cannot apply to null UITransitionAnimator");
                return;
            }

            // Use reflection to set private fields (since they're SerializeField)
            var type = typeof(UITransitionAnimator);

            SetFieldValue(animator, type, "showTransition", transitionType);
            SetFieldValue(animator, type, "showDuration", showDuration);
            SetFieldValue(animator, type, "showEase", showEase);
            SetFieldValue(animator, type, "hideTransition", GetHideTransition(transitionType));
            SetFieldValue(animator, type, "hideDuration", hideDuration);
            SetFieldValue(animator, type, "hideEase", hideEase);
            SetFieldValue(animator, type, "slideDirection", slideDirection);
            SetFieldValue(animator, type, "startScale", startScale);

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(animator);
            #endif
        }

        /// <summary>
        /// Applies this preset to a UIButtonAnimator component
        /// </summary>
        public void ApplyToButtonAnimator(UIButtonAnimator animator)
        {
            if (animator == null)
            {
                Debug.LogWarning("[UIAnimationPreset] Cannot apply to null UIButtonAnimator");
                return;
            }

            var type = typeof(UIButtonAnimator);

            SetFieldValue(animator, type, "enableHoverScale", enableHoverScale);
            SetFieldValue(animator, type, "hoverScale", hoverScale);
            SetFieldValue(animator, type, "hoverDuration", hoverDuration);
            SetFieldValue(animator, type, "hoverEase", hoverEase);
            SetFieldValue(animator, type, "enablePressScale", enablePressScale);
            SetFieldValue(animator, type, "pressScale", pressScale);
            SetFieldValue(animator, type, "pressDuration", pressDuration);
            SetFieldValue(animator, type, "enableClickPunch", enableClickPunch);
            SetFieldValue(animator, type, "punchStrength", punchStrength);
            SetFieldValue(animator, type, "punchDuration", punchDuration);
            SetFieldValue(animator, type, "punchVibrato", punchVibrato);
            SetFieldValue(animator, type, "enableColorChange", enableColorChange);
            SetFieldValue(animator, type, "hoverColor", hoverColor);
            SetFieldValue(animator, type, "colorDuration", colorDuration);

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(animator);
            #endif
        }

        private void SetFieldValue(object obj, System.Type type, string fieldName, object value)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"[UIAnimationPreset] Field '{fieldName}' not found in {type.Name}");
            }
        }

        private TransitionType GetHideTransition(TransitionType showTransition)
        {
            return showTransition switch
            {
                TransitionType.FadeSlideIn => TransitionType.FadeSlideOut,
                TransitionType.FadeScale => TransitionType.FadeScale,
                TransitionType.FadeSlideOut => TransitionType.FadeSlideIn,
                _ => showTransition
            };
        }

        /// <summary>
        /// Creates a runtime animation preset with custom settings
        /// </summary>
        public static UIAnimationPreset CreateRuntimePreset(string name, TransitionType transition, float duration)
        {
            var preset = CreateInstance<UIAnimationPreset>();
            preset.presetName = name;
            preset.transitionType = transition;
            preset.showDuration = duration;
            preset.hideDuration = duration * 0.75f;
            return preset;
        }

        /// <summary>
        /// Editor utility to create common presets
        /// </summary>
        [ContextMenu("Create Common Presets")]
        private void CreateCommonPresets()
        {
            #if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            string directory = System.IO.Path.GetDirectoryName(path);

            CreatePreset(directory, "Fast_Popup", TransitionType.FadeScale, 0.2f, 0.15f, Ease.OutBack, Ease.InBack);
            CreatePreset(directory, "Smooth_SlideIn", TransitionType.FadeSlideIn, 0.4f, 0.3f, Ease.OutCubic, Ease.InCubic);
            CreatePreset(directory, "Quick_Fade", TransitionType.Fade, 0.25f, 0.2f, Ease.Linear, Ease.Linear);
            CreatePreset(directory, "Bouncy_Scale", TransitionType.Scale, 0.5f, 0.3f, Ease.OutBounce, Ease.InCubic);

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"[UIAnimationPreset] Created 4 common presets in {directory}");
            #endif
        }

        #if UNITY_EDITOR
        private static void CreatePreset(string directory, string name, TransitionType transition, float showDuration, float hideDuration, Ease showEase, Ease hideEase)
        {
            var preset = CreateInstance<UIAnimationPreset>();
            preset.presetName = name;
            preset.description = $"Auto-generated preset: {name}";
            preset.transitionType = transition;
            preset.showDuration = showDuration;
            preset.hideDuration = hideDuration;
            preset.showEase = showEase;
            preset.hideEase = hideEase;

            string assetPath = $"{directory}/UIAnimationPreset_{name}.asset";
            UnityEditor.AssetDatabase.CreateAsset(preset, assetPath);
        }
        #endif
    }

    // Enums matching the animation components
    public enum TransitionType
    {
        Fade,
        Slide,
        Scale,
        FadeSlideIn,
        FadeSlideOut,
        FadeScale,
        SlideScale,
        FullTransition
    }

    public enum SlideDirection
    {
        Top,
        Bottom,
        Left,
        Right
    }
}
