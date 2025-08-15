using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// Animator IK fallback for looking at target when Animation Rigging is not in use.
    /// </summary>
    public class LookAtIKFallback : MonoBehaviour
    {
        [SerializeField] private TargetSelector targetSelector;
        [SerializeField] private Animator animator;
        [Range(0, 1f)]
        [SerializeField] private float lookWeight = 0.8f;

        void OnAnimatorIK(int layerIndex)
        {
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
    }
}
