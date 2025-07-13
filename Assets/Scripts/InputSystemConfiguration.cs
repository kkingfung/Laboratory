// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemConfiguration : MonoBehaviour
{
    public InputActionAsset inputActions;

    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActionAsset is not assigned. Please assign it in the inspector.");
            return;
        }

        // Load the "Player" action map from the Input Actions asset
        playerActionMap = inputActions.FindActionMap("Player", true);

        if (playerActionMap == null)
        {
            Debug.LogError("Player action map not found in the Input Actions asset.");
            return;
        }

        // Bind actions
        moveAction = playerActionMap.FindAction("Move", true);
        jumpAction = playerActionMap.FindAction("Jump", true);
        attackAction = playerActionMap.FindAction("Attack", true);
    }

    private void OnEnable()
    {
        if (playerActionMap != null)
        {
            playerActionMap.Enable();
        }
    }

    private void OnDisable()
    {
        if (playerActionMap != null)
        {
            playerActionMap.Disable();
        }
    }

    public Vector2 GetMoveInput()
    {
        return moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    }

    public bool IsJumping()
    {
        return jumpAction != null && jumpAction.triggered;
    }

    public bool IsAttacking()
    {
        return attackAction != null && attackAction.triggered;
    }
}