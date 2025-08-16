using UnityEngine;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Animator IK fallback for looking at targets when Animation Rigging is not available.
    /// Provides basic look-at functionality using Unity's built-in Animator IK system.
    /// </summary>
    public class LookAtIKFallback : MonoBehaviour
    {
        #region Fields

        [Header("Target Selection")]
        [Tooltip("Target selector component for finding targets")]
        [SerializeField] private TargetSelector targetSelector;

        [Header("Animation")]
        [Tooltip("Animator component for IK control")]
        [SerializeField] private Animator animator;

        [Header("Look At Settings")]
        [Tooltip("Weight of the look-at behavior")]
        [Range(0, 1f)]
        [SerializeField] private float lookWeight = 0.8f;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called during Animator IK pass
        /// </summary>
        /// <param name="layerIndex">Animation layer index</param>
        void OnAnimatorIK(int layerIndex)
        {
            if (targetSelector == null || animator == null) return;

            Transform target = targetSelector.CurrentTarget;
            
            if (target != null)
            {
                animator.SetLookAtWeight(lookWeight);
                animator.SetLookAtPosition(target.position);
            }
            else
            {
                animator.SetLookAtWeight(0f);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the look-at weight
        /// </summary>
        /// <param name="weight">Weight value between 0 and 1</param>
        public void SetLookWeight(float weight)
        {
            lookWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Gets the current look-at weight
        /// </summary>
        /// <returns>Current look weight</returns>
        public float GetLookWeight()
        {
            return lookWeight;
        }

        #endregion
    }
}
