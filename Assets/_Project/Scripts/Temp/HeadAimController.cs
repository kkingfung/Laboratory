using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Game.Character
{
    /// <summary>
    /// Controls the head aiming toward a target using Animation Rigging's MultiAimConstraint.
    /// </summary>
    public class HeadAimController : MonoBehaviour
    {
        [SerializeField] private TargetSelector targetSelector;
        [SerializeField] private MultiAimConstraint headConstraint;
        [SerializeField] private float aimSpeed = 5f;
        [SerializeField] private float maxAngle = 80f;

        private float currentWeight = 0f;
        private WeightedTransformArray sourceObjects;

        void Awake()
        {
            sourceObjects = headConstraint.data.sourceObjects;
        }

        void LateUpdate()
        {
            Transform target = targetSelector.CurrentTarget;
            if (target != null)
            {
                Vector3 dirToTarget = target.position - transform.position;
                float angle = Vector3.Angle(transform.forward, dirToTarget);

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

        private void SetConstraintTarget(Transform target)
        {
            sourceObjects.SetTransform(0, target);
            headConstraint.data.sourceObjects = sourceObjects;
        }
    }
}
