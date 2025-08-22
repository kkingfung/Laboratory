using UnityEngine;

namespace Laboratory.Gameplay.Character
{
    /// <summary>
    /// Advanced foot placement system that uses Animator IK and raycasts to plant feet 
    /// and adjust pelvis height over uneven terrain for humanoid characters.
    /// Supports smooth transitions, pelvis adjustment, and dynamic IK weighting.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class FootPlacementSystem : MonoBehaviour
    {
        #region Fields

        #region Serialized Fields

        [Header("Core Settings")]
        [SerializeField, Tooltip("Master switch for IK. Toggle at runtime for debugging or cutscenes")]
        private bool enableIK = true;

        [SerializeField, Range(0f, 1f), Tooltip("Global IK weight. Blend with animation for polish")]
        private float ikWeight = 1f;

        [Header("Raycast Detection")]
        [SerializeField, Tooltip("Layers considered 'ground' for feet")]
        private LayerMask groundLayers = ~0;

        [SerializeField, Tooltip("How far above the foot we begin the raycast"), Range(0.1f, 2f)]
        private float raycastOriginHeight = 0.5f;

        [SerializeField, Tooltip("Max distance the ray can search downward for ground"), Range(0.5f, 5f)]
        private float raycastDistance = 1.2f;

        [Header("Foot Adjustments")]
        [SerializeField, Tooltip("Offset to prevent foot clipping into ground"), Range(0f, 0.1f)]
        private float footToGroundOffset = 0.02f;

        [SerializeField, Tooltip("Max foot position correction speed (m/s)"), Range(1f, 20f)]
        private float footPosLerpSpeed = 8f;

        [SerializeField, Tooltip("Max foot rotation correction speed (deg/s)"), Range(90f, 1800f)]
        private float footRotLerpSpeed = 720f;

        [Header("Pelvis Adjustment")]
        [SerializeField, Tooltip("Speed of pelvis height adjustment (m/s)"), Range(1f, 10f)]
        private float pelvisAdjustSpeed = 4f;

        [SerializeField, Tooltip("Extra down offset to prevent knee locking on gentle slopes"), Range(0f, 0.1f)]
        private float pelvisDownBias = 0.02f;

        [SerializeField, Tooltip("Maximum pelvis vertical offset (+/- meters)"), Range(0.1f, 1f)]
        private float pelvisMaxOffset = 0.2f;

        [Header("Advanced Settings")]
        [SerializeField, Tooltip("Project feet forward vector onto ground to maintain natural twist")]
        private bool projectFootForwardOnGround = true;

        #pragma warning disable 0414 // Field assigned but never used - planned for future airborne detection
        [SerializeField, Tooltip("Fade out IK when character is airborne")]
        private bool autoDisableWhenAirborne = true;
        #pragma warning restore 0414

        [SerializeField, Tooltip("Animator parameter (float) representing locomotion speed for high-speed IK damping")]
        private string speedParameter = "Speed";

        [SerializeField, Range(0f, 1f), Tooltip("IK reduction amount at high speeds (1 = fully reduce)")]
        private float highSpeedIKDamping = 0.5f;

        [SerializeField, Tooltip("Speed threshold where damping begins"), Range(1f, 10f)]
        private float speedDampingThreshold = 3.0f;

        [Header("Debug")]
        [SerializeField, Tooltip("Draw debug gizmos in Scene view")]
        private bool drawGizmos = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets whether IK is enabled
        /// </summary>
        public bool EnableIK 
        { 
            get => enableIK; 
            set => enableIK = value; 
        }

        /// <summary>
        /// Gets or sets the global IK weight
        /// </summary>
        public float IKWeight 
        { 
            get => ikWeight; 
            set => ikWeight = Mathf.Clamp01(value); 
        }

        /// <summary>
        /// Gets or sets the ground layers mask
        /// </summary>
        public LayerMask GroundLayers 
        { 
            get => groundLayers; 
            set => groundLayers = value; 
        }

        #endregion

        #region Private Fields

        private Animator animator;
        private Transform leftFoot;
        private Transform rightFoot;
        private Transform hips;
        private FootIKData leftFootData;
        private FootIKData rightFootData;
        private float pelvisOffsetCurrent = 0f;
        private float pelvisOffsetTarget = 0f;
        private int speedParameterHash = -1;

        #endregion

        #endregion

        #region Nested Types

        /// <summary>
        /// Data structure containing IK information for a single foot
        /// </summary>
        private struct FootIKData
        {
            /// <summary>Distance of the last successful ground hit</summary>
            public float lastHitDistance;
            
            /// <summary>Whether the last raycast hit ground</summary>
            public bool hasHit;
            
            /// <summary>Target IK position for the foot</summary>
            public Vector3 ikPosition;
            
            /// <summary>Target IK rotation for the foot</summary>
            public Quaternion ikRotation;
            
            /// <summary>Smoothed position to avoid snapping</summary>
            public Vector3 smoothPosition;
            
            /// <summary>Smoothed rotation to avoid snapping</summary>
            public Quaternion smoothRotation;
            
            /// <summary>Cached raycast origin to avoid allocations</summary>
            public Vector3 rayOrigin;
            
            /// <summary>Cached ray to avoid allocations</summary>
            public Ray ray;
            
            /// <summary>Cached raycast hit result to avoid allocations</summary>
            public RaycastHit hit;
        }

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize components and validate setup
        /// </summary>
        private void Awake()
        {
            InitializeComponents();
            ValidateSetup();
            InitializeFootData();
            CacheSpeedParameter();
        }

        /// <summary>
        /// Handle IK calculations and application during Animator IK callback
        /// </summary>
        /// <param name="layerIndex">Animator layer index</param>
        private void OnAnimatorIK(int layerIndex)
        {
            if (!enableIK || animator == null) 
                return;

            float effectiveWeight = CalculateEffectiveIKWeight();
            
            UpdateFootTargets();
            ComputePelvisOffset();
            ApplyPelvisOffset();
            ApplyFootIK(effectiveWeight);
        }

        /// <summary>
        /// Draw debug gizmos in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) 
                return;

            DrawRaycastGizmos();
            DrawPelvisGizmo();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the IK enabled state
        /// </summary>
        /// <param name="value">Whether to enable IK</param>
        public void SetIKEnabled(bool value)
        {
            enableIK = value;
        }

        /// <summary>
        /// Sets the global IK weight
        /// </summary>
        /// <param name="value">IK weight (0-1)</param>
        public void SetIKWeight(float value)
        {
            ikWeight = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Sets the ground layers mask
        /// </summary>
        /// <param name="mask">LayerMask for ground detection</param>
        public void SetGroundLayers(LayerMask mask)
        {
            groundLayers = mask;
        }

        /// <summary>
        /// Resets the foot placement system to default state
        /// </summary>
        public void ResetSystem()
        {
            pelvisOffsetCurrent = 0f;
            pelvisOffsetTarget = 0f;
            
            if (leftFoot != null && rightFoot != null)
            {
                InitializeFootData();
            }
        }

        #endregion

        #region Private Methods

        #region Initialization

        /// <summary>
        /// Initialize required components
        /// </summary>
        private void InitializeComponents()
        {
            animator = GetComponent<Animator>();
        }

        /// <summary>
        /// Validate that the setup is correct for the system to function
        /// </summary>
        private void ValidateSetup()
        {
            if (animator == null || !animator.isHuman)
            {
                Debug.LogWarning($"[{nameof(FootPlacementSystem)}] Animator missing or not Humanoid; disabling.", this);
                enabled = false;
                return;
            }

            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            if (leftFoot == null || rightFoot == null)
            {
                Debug.LogWarning($"[{nameof(FootPlacementSystem)}] Could not find foot bones; disabling.", this);
                enabled = false;
            }
        }

        /// <summary>
        /// Initialize foot data structures with default values
        /// </summary>
        private void InitializeFootData()
        {
            leftFootData.ikPosition = leftFoot.position;
            rightFootData.ikPosition = rightFoot.position;
            
            leftFootData.smoothPosition = leftFootData.ikPosition;
            rightFootData.smoothPosition = rightFootData.ikPosition;
            
            leftFootData.ikRotation = leftFoot.rotation;
            rightFootData.ikRotation = rightFoot.rotation;
            
            leftFootData.smoothRotation = leftFootData.ikRotation;
            rightFootData.smoothRotation = rightFootData.ikRotation;
        }

        /// <summary>
        /// Cache the speed parameter hash if specified
        /// </summary>
        private void CacheSpeedParameter()
        {
            if (!string.IsNullOrEmpty(speedParameter))
            {
                speedParameterHash = Animator.StringToHash(speedParameter);
            }
        }

        #endregion

        #region IK Calculations

        /// <summary>
        /// Calculate the effective IK weight considering speed damping and other factors
        /// </summary>
        /// <returns>Effective IK weight to apply</returns>
        private float CalculateEffectiveIKWeight()
        {
            float effectiveWeight = ikWeight;

            // Apply speed-based damping if speed parameter is configured
            if (speedParameterHash != -1)
            {
                float currentSpeed = Mathf.Abs(animator.GetFloat(speedParameterHash));
                if (currentSpeed > speedDampingThreshold)
                {
                    float speedFactor = Mathf.InverseLerp(speedDampingThreshold, speedDampingThreshold * 2f, currentSpeed);
                    float dampingAmount = Mathf.Lerp(1f, 1f - highSpeedIKDamping, Mathf.Clamp01(speedFactor));
                    effectiveWeight *= dampingAmount;
                }
            }

            return effectiveWeight;
        }

        /// <summary>
        /// Update target positions and rotations for both feet
        /// </summary>
        private void UpdateFootTargets()
        {
            UpdateFootTarget(HumanBodyBones.LeftFoot, ref leftFootData);
            UpdateFootTarget(HumanBodyBones.RightFoot, ref rightFootData);
        }

        /// <summary>
        /// Update the target position and rotation for a specific foot
        /// </summary>
        /// <param name="footBone">Which foot bone to update</param>
        /// <param name="foot">Foot data structure to update</param>
        private void UpdateFootTarget(HumanBodyBones footBone, ref FootIKData foot)
        {
            Transform footTransform = (footBone == HumanBodyBones.LeftFoot) ? leftFoot : rightFoot;

            // Setup raycast from above the foot
            Vector3 rayOrigin = footTransform.position + Vector3.up * raycastOriginHeight;
            foot.rayOrigin = rayOrigin;
            foot.ray.origin = rayOrigin;
            foot.ray.direction = Vector3.down;

            // Perform ground detection raycast
            foot.hasHit = Physics.Raycast(foot.ray, out foot.hit, raycastDistance + raycastOriginHeight, groundLayers, QueryTriggerInteraction.Ignore);

            if (foot.hasHit)
            {
                CalculateGroundedFootTarget(ref foot, footTransform);
            }
            else
            {
                SetDefaultFootTarget(ref foot, footTransform);
            }
        }

        /// <summary>
        /// Calculate foot target when ground is detected
        /// </summary>
        /// <param name="foot">Foot data to update</param>
        /// <param name="footTransform">Transform of the foot</param>
        private void CalculateGroundedFootTarget(ref FootIKData foot, Transform footTransform)
        {
            Vector3 targetPosition = foot.hit.point + foot.hit.normal * footToGroundOffset;
            Quaternion targetRotation = CalculateFootRotation(foot.hit, footTransform);

            foot.ikPosition = targetPosition;
            foot.ikRotation = targetRotation;
            foot.lastHitDistance = foot.hit.distance;
        }

        /// <summary>
        /// Set default foot target when no ground is detected
        /// </summary>
        /// <param name="foot">Foot data to update</param>
        /// <param name="footTransform">Transform of the foot</param>
        private void SetDefaultFootTarget(ref FootIKData foot, Transform footTransform)
        {
            foot.ikPosition = footTransform.position;
            foot.ikRotation = footTransform.rotation;
            foot.lastHitDistance = raycastDistance + raycastOriginHeight;
        }

        /// <summary>
        /// Calculate the appropriate rotation for a foot based on ground normal
        /// </summary>
        /// <param name="hit">Ground hit information</param>
        /// <param name="footTransform">Foot transform for reference</param>
        /// <returns>Calculated foot rotation</returns>
        private Quaternion CalculateFootRotation(RaycastHit hit, Transform footTransform)
        {
            if (projectFootForwardOnGround)
            {
                Vector3 characterForward = transform.forward;
                Vector3 projectedForward = Vector3.ProjectOnPlane(characterForward, hit.normal);
                
                if (projectedForward.sqrMagnitude < 1e-4f)
                    projectedForward = characterForward;
                
                return Quaternion.LookRotation(projectedForward.normalized, hit.normal);
            }
            else
            {
                return Quaternion.FromToRotation(Vector3.up, hit.normal) * footTransform.rotation;
            }
        }

        #endregion

        #region Pelvis Adjustment

        /// <summary>
        /// Compute the target pelvis offset based on foot positions
        /// </summary>
        private void ComputePelvisOffset()
        {
            float leftOffset = GetFootVerticalOffset(leftFoot.position.y, leftFootData.ikPosition.y);
            float rightOffset = GetFootVerticalOffset(rightFoot.position.y, rightFootData.ikPosition.y);

            float targetOffset = Mathf.Min(leftOffset, rightOffset) - pelvisDownBias;
            pelvisOffsetTarget = Mathf.Clamp(targetOffset, -pelvisMaxOffset, pelvisMaxOffset);
        }

        /// <summary>
        /// Calculate the vertical offset between animation and IK positions
        /// </summary>
        /// <param name="animationY">Y position from animation</param>
        /// <param name="ikY">Target IK Y position</param>
        /// <returns>Vertical offset required</returns>
        private float GetFootVerticalOffset(float animationY, float ikY)
        {
            return ikY - animationY;
        }

        /// <summary>
        /// Apply the calculated pelvis offset smoothly
        /// </summary>
        private void ApplyPelvisOffset()
        {
            if (hips == null) 
                return;

            pelvisOffsetCurrent = Mathf.MoveTowards(pelvisOffsetCurrent, pelvisOffsetTarget, pelvisAdjustSpeed * Time.deltaTime);

            Vector3 hipsPosition = hips.position;
            hipsPosition.y += pelvisOffsetCurrent;
            hips.position = hipsPosition;
        }

        #endregion

        #region IK Application

        /// <summary>
        /// Apply foot IK to both feet with the specified weight
        /// </summary>
        /// <param name="effectiveWeight">Weight to apply to IK</param>
        private void ApplyFootIK(float effectiveWeight)
        {
            ApplyFootIK(AvatarIKGoal.LeftFoot, ref leftFootData, effectiveWeight);
            ApplyFootIK(AvatarIKGoal.RightFoot, ref rightFootData, effectiveWeight);
        }

        /// <summary>
        /// Apply IK for a specific foot
        /// </summary>
        /// <param name="goal">IK goal (left or right foot)</param>
        /// <param name="foot">Foot data containing target positions</param>
        /// <param name="weight">Weight to apply</param>
        private void ApplyFootIK(AvatarIKGoal goal, ref FootIKData foot, float weight)
        {
            // Smooth position and rotation to avoid jarring transitions
            foot.smoothPosition = Vector3.MoveTowards(foot.smoothPosition, foot.ikPosition, footPosLerpSpeed * Time.deltaTime);
            foot.smoothRotation = Quaternion.RotateTowards(foot.smoothRotation, foot.ikRotation, footRotLerpSpeed * Time.deltaTime);

            // Reduce IK weight if foot is not grounded to avoid locking in mid-air
            float localWeight = foot.hasHit ? weight : Mathf.Clamp01(weight * 0.25f);

            // Apply IK settings to animator
            animator.SetIKPositionWeight(goal, localWeight);
            animator.SetIKRotationWeight(goal, localWeight);
            animator.SetIKPosition(goal, foot.smoothPosition);
            animator.SetIKRotation(goal, foot.smoothRotation);
        }

        #endregion

        #region Debug Visualization

        /// <summary>
        /// Draw raycast visualization gizmos
        /// </summary>
        private void DrawRaycastGizmos()
        {
            Gizmos.color = Color.yellow;
            
            if (leftFoot != null)
            {
                Vector3 origin = leftFoot.position + Vector3.up * raycastOriginHeight;
                Vector3 end = origin + Vector3.down * (raycastDistance + raycastOriginHeight);
                Gizmos.DrawLine(origin, end);
            }
            
            if (rightFoot != null)
            {
                Vector3 origin = rightFoot.position + Vector3.up * raycastOriginHeight;
                Vector3 end = origin + Vector3.down * (raycastDistance + raycastOriginHeight);
                Gizmos.DrawLine(origin, end);
            }
        }

        /// <summary>
        /// Draw pelvis position indicator gizmo
        /// </summary>
        private void DrawPelvisGizmo()
        {
            Gizmos.color = Color.cyan;
            Vector3 pelvisIndicator = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawWireSphere(pelvisIndicator, 0.03f);
        }

        #endregion

        #endregion
    }
}
