using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Controls head aiming toward a target using Animation Rigging's MultiAimConstraint.
    /// Provides smooth aiming with angle limitations and weight control.
    /// </summary>
    public class HeadAimController : MonoBehaviour
    {
        #region Fields

        [Header("Target Selection")]
        [Tooltip("Target selector component for finding targets")]
        [SerializeField] private TargetSelector targetSelector;

        [Header("Animation Rigging")]
        [Tooltip("MultiAimConstraint component for head aiming")]
        [SerializeField] private MultiAimConstraint headConstraint;

        [Header("Aim Settings")]
        [Tooltip("Speed of aim weight transitions")]
        [SerializeField] private float aimSpeed = 5f;
        
        [Tooltip("Maximum angle in degrees for head aiming")]
        [SerializeField] private float maxAngle = 80f;

        private float currentWeight = 0f;
        private WeightedTransformArray sourceObjects;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initializes component references
        /// </summary>
        void Awake()
        {
            if (headConstraint != null)
                sourceObjects = headConstraint.data.sourceObjects;
        }

        /// <summary>
        /// Updates head aiming during late update
        /// </summary>
        void LateUpdate()
        {
            if (targetSelector == null || headConstraint == null) return;

            Transform target = targetSelector.CurrentTarget;
            
            if (target != null)
            {
                Vector3 directionToTarget = target.position - transform.position;
                float angle = Vector3.Angle(transform.forward, directionToTarget);

                if (angle < maxAngle)
                {
                    SetConstraintTarget(target);
                    currentWeight = Mathf.Lerp(currentWeight, 1f, Time.deltaTime * aimSpeed);
                }
                else
                {
                    currentWeight = Mathf.Lerp(currentWeight, 0f, Time.deltaTime * aimSpeed);
                }
            }
            else
            {
                currentWeight = Mathf.Lerp(currentWeight, 0f, Time.deltaTime * aimSpeed);
            }

            headConstraint.weight = currentWeight;
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
            headConstraint.data.sourceObjects = sourceObjects;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the current aim weight
        /// </summary>
        /// <returns>Current weight value between 0 and 1</returns>
        public float GetCurrentWeight()
        {
            return currentWeight;
        }

        /// <summary>
        /// Forces the aim weight to a specific value
        /// </summary>
        /// <param name="weight">Target weight between 0 and 1</param>
        public void SetWeight(float weight)
        {
            currentWeight = Mathf.Clamp01(weight);
            if (headConstraint != null)
                headConstraint.weight = currentWeight;
        }

        #endregion
    }
}
