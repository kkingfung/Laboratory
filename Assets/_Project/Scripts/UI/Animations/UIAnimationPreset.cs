using UnityEngine;

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
        [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve hideCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private SlideDirection slideDirection = SlideDirection.Bottom;
        [SerializeField] private float startScale = 0.8f;

        [Header("Button Animation Settings")]
        [SerializeField] private bool enableHoverScale = true;
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float hoverDuration = 0.2f;
        [SerializeField] private AnimationCurve hoverCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField] private bool enablePressScale = true;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float pressDuration = 0.1f;

        [SerializeField] private bool enableClickPunch = true;
        [SerializeField] private float punchStrength = 0.1f;
        [SerializeField] private float punchDuration = 0.3f;

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
        public AnimationCurve ShowCurve => showCurve;
        public AnimationCurve HideCurve => hideCurve;
        public SlideDirection SlideDir => slideDirection;
        public float StartScale => startScale;

        public bool EnableHoverScale => enableHoverScale;
        public float HoverScale => hoverScale;
        public float HoverDuration => hoverDuration;
        public AnimationCurve HoverCurve => hoverCurve;

        public bool EnablePressScale => enablePressScale;
        public float PressScale => pressScale;
        public float PressDuration => pressDuration;

        public bool EnableClickPunch => enableClickPunch;
        public float PunchStrength => punchStrength;
        public float PunchDuration => punchDuration;

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
            SetFieldValue(animator, type, "showCurve", showCurve);
            SetFieldValue(animator, type, "hideTransition", GetHideTransition(transitionType));
            SetFieldValue(animator, type, "hideDuration", hideDuration);
            SetFieldValue(animator, type, "hideCurve", hideCurve);
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
            SetFieldValue(animator, type, "hoverCurve", hoverCurve);
            SetFieldValue(animator, type, "enablePressScale", enablePressScale);
            SetFieldValue(animator, type, "pressScale", pressScale);
            SetFieldValue(animator, type, "pressDuration", pressDuration);
            SetFieldValue(animator, type, "enableClickPunch", enableClickPunch);
            SetFieldValue(animator, type, "punchStrength", punchStrength);
            SetFieldValue(animator, type, "punchDuration", punchDuration);
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

            CreatePreset(directory, "Fast_Popup", TransitionType.FadeScale, 0.2f, 0.15f, AnimationCurve.EaseInOut(0, 0, 1, 1));
            CreatePreset(directory, "Smooth_SlideIn", TransitionType.FadeSlideIn, 0.4f, 0.3f, AnimationCurve.EaseInOut(0, 0, 1, 1));
            CreatePreset(directory, "Quick_Fade", TransitionType.Fade, 0.25f, 0.2f, AnimationCurve.Linear(0, 0, 1, 1));
            CreatePreset(directory, "Bouncy_Scale", TransitionType.Scale, 0.5f, 0.3f, CreateBounceCurve());

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"[UIAnimationPreset] Created 4 common presets in {directory}");
            #endif
        }

        #if UNITY_EDITOR
        private static void CreatePreset(string directory, string name, TransitionType transition, float showDuration, float hideDuration, AnimationCurve curve)
        {
            var preset = CreateInstance<UIAnimationPreset>();
            preset.presetName = name;
            preset.description = $"Auto-generated preset: {name}";
            preset.transitionType = transition;
            preset.showDuration = showDuration;
            preset.hideDuration = hideDuration;
            preset.showCurve = curve;
            preset.hideCurve = AnimationCurve.Linear(0, 0, 1, 1);

            string assetPath = $"{directory}/UIAnimationPreset_{name}.asset";
            UnityEditor.AssetDatabase.CreateAsset(preset, assetPath);
        }
        #endif

        /// <summary>
        /// Creates a bounce-like animation curve
        /// </summary>
        private static AnimationCurve CreateBounceCurve()
        {
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, 0f, 0f, 2f));
            curve.AddKey(new Keyframe(0.5f, 1.1f, 0f, 0f));
            curve.AddKey(new Keyframe(0.75f, 0.95f, 0f, 0f));
            curve.AddKey(new Keyframe(1f, 1f, 0f, 0f));
            return curve;
        }
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
