using UnityEngine;

namespace Laboratory.Genres.Platforming
{
    /// <summary>
    /// ScriptableObject configuration for Platforming genre
    /// Designer-friendly settings for platformer mechanics
    /// </summary>
    [CreateAssetMenu(fileName = "PlatformingConfig", menuName = "Chimera/Genres/Platforming Config")]
    public class PlatformingConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float acceleration = 50f;
        [SerializeField] private float deceleration = 50f;
        [SerializeField] private float airControl = 0.5f;

        [Header("Jumping")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private int maxJumps = 2; // Double jump
        [SerializeField] private float jumpBufferTime = 0.1f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private bool variableJumpHeight = true;

        [Header("Gravity")]
        [SerializeField] private float gravity = -30f;
        [SerializeField] private float fallMultiplier = 2.5f;
        [SerializeField] private float maxFallSpeed = -25f;

        [Header("Wall Mechanics")]
        [SerializeField] private bool enableWallSlide = true;
        [SerializeField] private float wallSlideSpeed = -3f;
        [SerializeField] private bool enableWallJump = true;
        [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 15f);

        [Header("Dash")]
        [SerializeField] private bool enableDash = true;
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 1f;

        [Header("Level Settings")]
        [SerializeField] private int livesCount = 3;
        [SerializeField] private bool enableCheckpoints = true;
        [SerializeField] private float respawnDelay = 1f;

        [Header("Collectibles")]
        [SerializeField] private int coinsToExtraLife = 100;
        [SerializeField] private bool enableTimeTrial = false;
        [SerializeField] private float targetTime = 60f;

        // Properties
        public float MoveSpeed => moveSpeed;
        public float SprintMultiplier => sprintMultiplier;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float AirControl => airControl;

        public float JumpForce => jumpForce;
        public int MaxJumps => maxJumps;
        public float JumpBufferTime => jumpBufferTime;
        public float CoyoteTime => coyoteTime;
        public bool VariableJumpHeight => variableJumpHeight;

        public float Gravity => gravity;
        public float FallMultiplier => fallMultiplier;
        public float MaxFallSpeed => maxFallSpeed;

        public bool EnableWallSlide => enableWallSlide;
        public float WallSlideSpeed => wallSlideSpeed;
        public bool EnableWallJump => enableWallJump;
        public Vector2 WallJumpForce => wallJumpForce;

        public bool EnableDash => enableDash;
        public float DashSpeed => dashSpeed;
        public float DashDuration => dashDuration;
        public float DashCooldown => dashCooldown;

        public int LivesCount => livesCount;
        public bool EnableCheckpoints => enableCheckpoints;
        public float RespawnDelay => respawnDelay;

        public int CoinsToExtraLife => coinsToExtraLife;
        public bool EnableTimeTrial => enableTimeTrial;
        public float TargetTime => targetTime;
    }
}
