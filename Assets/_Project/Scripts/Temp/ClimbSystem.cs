using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ClimbSystem : MonoBehaviour
{
    [Header("References")]
    public Transform chestPosition;
    public Transform handPosition;
    public Transform playerCamera;
    public Animator animator; // optional for IK/animations

    [Header("Movement Settings")]
    public float climbSpeed = 3f;
    public float mantleSpeed = 3f;
    public float wallJumpForce = 5f;

    [Header("Detection Settings")]
    public float climbReach = 1f;
    public float ledgeReach = 1f;
    public LayerMask climbableLayer;

    [Header("Camera & Stamina")]
    public float cameraTiltAngle = 15f;
    public float cameraTiltSpeed = 3f;
    public float staminaMax = 100f;
    public float staminaDrainRate = 10f;
    public float staminaRecoverRate = 5f;
    public float coyoteTime = 0.2f; // grace period for leaving wall

    [HideInInspector]
    public float stamina;
    private float coyoteTimer;

    [HideInInspector]
    public enum ClimbState { Grounded, Climbing, Mantling, WallJumping }
    [HideInInspector]
    public ClimbState currentState = ClimbState.Grounded;

    private Rigidbody rb;
    private Vector3 climbNormal;
    private Vector3 mantleTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        stamina = staminaMax;
    }

    void Update()
    {
        HandleState();
        RecoverStamina();
        AdjustCamera();
    }

    void FixedUpdate()
    {
        switch(currentState)
        {
            case ClimbState.Climbing:
                ClimbMovement();
                break;
            case ClimbState.Mantling:
                MantleMovement();
                break;
        }
    }

    #region State Handling
    void HandleState()
    {
        bool canClimbWall = DetectClimbable();
        bool wantsClimb = Input.GetKey(KeyCode.Space) && stamina > 0;

        switch(currentState)
        {
            case ClimbState.Grounded:
                if(canClimbWall && wantsClimb)
                    EnterClimb();
                break;

            case ClimbState.Climbing:
                DrainStamina();
                if(!canClimbWall) coyoteTimer += Time.deltaTime;
                else coyoteTimer = 0;

                if(coyoteTimer > coyoteTime || stamina <= 0)
                    ExitClimb();
                else if(DetectLedge() && wantsClimb)
                    EnterMantle();
                else if(Input.GetKeyDown(KeyCode.Space))
                    WallJump();
                break;
        }
    }
    #endregion

    #region Climb Logic
    bool DetectClimbable()
    {
        RaycastHit hit;
        if(Physics.Raycast(chestPosition.position, transform.forward, out hit, climbReach, climbableLayer))
        {
            climbNormal = hit.normal;
            return true;
        }
        return false;
    }

    void EnterClimb()
    {
        currentState = ClimbState.Climbing;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        if(animator) animator.SetBool("Climbing", true);
    }

    void ExitClimb()
    {
        currentState = ClimbState.Grounded;
        rb.useGravity = true;
        if(animator) animator.SetBool("Climbing", false);
    }

    void ClimbMovement()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        Vector3 moveDir = (playerCamera.right * horizontal + playerCamera.up * vertical);
        moveDir = Vector3.ProjectOnPlane(moveDir, climbNormal).normalized;

        rb.velocity = moveDir * climbSpeed;

        // Face wall
        Vector3 lookDir = -climbNormal;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
    }
    #endregion

    #region Ledge & Mantle
    bool DetectLedge()
    {
        RaycastHit hit;
        if(Physics.Raycast(handPosition.position, Vector3.up, out hit, ledgeReach, climbableLayer))
        {
            mantleTarget = hit.point + Vector3.up * 1f; // adjust for top of ledge
            return true;
        }
        return false;
    }

    void EnterMantle()
    {
        currentState = ClimbState.Mantling;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        if(animator) animator.SetTrigger("Mantle");
    }

    void MantleMovement()
    {
        rb.velocity = (mantleTarget - transform.position) * mantleSpeed;
        if(Vector3.Distance(transform.position, mantleTarget) < 0.05f)
        {
            currentState = ClimbState.Grounded;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            if(animator) animator.SetBool("Climbing", false);
        }
    }
    #endregion

    #region Wall Jump
    void WallJump()
    {
        rb.velocity = Vector3.zero;
        rb.AddForce((-climbNormal + Vector3.up) * wallJumpForce, ForceMode.Impulse);
        currentState = ClimbState.WallJumping;
        rb.useGravity = true;
        if(animator) animator.SetTrigger("WallJump");
    }
    #endregion

    #region Stamina
    void DrainStamina()
    {
        stamina -= staminaDrainRate * Time.deltaTime;
        if(stamina < 0) stamina = 0;
    }

    void RecoverStamina()
    {
        if(currentState == ClimbState.Grounded && stamina < staminaMax)
            stamina += staminaRecoverRate * Time.deltaTime;
    }
    #endregion

    #region Camera
    void AdjustCamera()
    {
        float targetTilt = (currentState == ClimbState.Climbing || currentState == ClimbState.Mantling) ? cameraTiltAngle : 0f;
        Vector3 localEuler = playerCamera.localEulerAngles;
        localEuler.x = Mathf.LerpAngle(localEuler.x, targetTilt, Time.deltaTime * cameraTiltSpeed);
        playerCamera.localEulerAngles = localEuler;
    }
    #endregion
}
