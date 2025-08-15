using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Game.Character
{
    /// <summary>
    /// Subtle chest rotation toward target for more realistic aiming.
    /// </summary>
    public class UpperBodyAimController : MonoBehaviour
    {
        [SerializeField] private TargetSelector targetSelector;
        [SerializeField] private MultiAimConstraint chestConstraint;
        [SerializeField] private float aimSpeed = 3f;
        [SerializeField] private float chestWeightMax = 0.3f;

        private WeightedTransformArray sourceObjects;
        private float currentWeight = 0f;

        void Awake()
        {
            sourceObjects = chestConstraint.data.sourceObjects;
        }

        void LateUpdate()
        {
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

        private void SetConstraintTarget(Transform target)
        {
            sourceObjects.SetTransform(0, target);
            chestConstraint.data.sourceObjects = sourceObjects;
        }
    }
}
