using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Controls subtle chest rotation toward target for more realistic upper body aiming.
    /// Works in conjunction with head aiming for natural looking character poses.
    /// </summary>
    public class UpperBodyAimController : MonoBehaviour
    {
        #region Fields

        [Header("Target Selection")]
        [Tooltip("Target selector component for finding targets")]
        [SerializeField] private TargetSelector targetSelector;

        [Header("Animation Rigging")]
        [Tooltip("MultiAimConstraint component for chest aiming")]
        [SerializeField] private MultiAimConstraint chestConstraint;

        [Header("Aim Settings")]
        [Tooltip("Speed of aim weight transitions")]
        [SerializeField] private float aimSpeed = 3f;
        
        [Tooltip("Maximum weight for chest constraint")]
        [SerializeField] private float chestWeightMax = 0.3f;

        private WeightedTransformArray sourceObjects;
        private float currentWeight = 0f;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initializes component references
        /// </summary>
        void Awake()
        {
            if (chestConstraint != null)
                sourceObjects = chestConstraint.data.sourceObjects;
        }

        /// <summary>
        /// Updates chest aiming during late update
        /// </summary>
        void LateUpdate()
        {
            if (targetSelector == null || chestConstraint == null) return;

            Transform target = targetSelector.CurrentTarget;
            
            if (target != null)
            {
                SetConstraintTarget(target);
                currentWeight = Mathf.Lerp(currentWeight, chestWeightMax, Time.deltaTime * aimSpeed);
            }
            else
            {
                currentWeight = Mathf.Lerp(currentWeight, 0f, Time.deltaTime * aimSpeed);
            }

            chestConstraint.weight = currentWeight;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets the target for the constraint
        /// </summary>
        /// <param name="target">Target transform to aim at</param>
        private void SetConstraintTarget(Transform target)
        {
            sourceObjects.SetTransform(0, target);
            chestConstraint.data.sourceObjects = sourceObjects;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the current aim weight
        /// </summary>
        /// <returns>Current weight value between 0 and maximum weight</returns>
        public float GetCurrentWeight()
        {
            return currentWeight;
        }

        /// <summary>
        /// Sets the maximum weight for the chest constraint
        /// </summary>
        /// <param name="maxWeight">Maximum weight value</param>
        public void SetMaxWeight(float maxWeight)
        {
            chestWeightMax = Mathf.Clamp01(maxWeight);
        }

        /// <summary>
        /// Forces the aim weight to a specific value
        /// </summary>
        /// <param name="weight">Target weight between 0 and maximum weight</param>
        public void SetWeight(float weight)
        {
            currentWeight = Mathf.Clamp(weight, 0f, chestWeightMax);
            if (chestConstraint != null)
                chestConstraint.weight = currentWeight;
        }

        #endregion
    }
}
