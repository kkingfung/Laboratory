using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Models.ECS.Components;

public class PlayerInputSystem : MonoBehaviour
{
    private EntityManager _entityManager;
    private Entity _playerEntity;

    // Input System actions
    private PlayerControls _controls;

    private void Awake()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // TODO: Assign _playerEntity here or get via your own entity lookup
        // e.g. query by player tag or set manually

        _controls = new PlayerControls();
    }

    private void OnEnable()
    {
        _controls.Enable();
    }

    private void OnDisable()
    {
        _controls.Disable();
    }

    private void Update()
    {
        if (_playerEntity == Entity.Null || !_entityManager.HasComponent<PlayerInputComponent>(_playerEntity))
            return;

        var input = new PlayerInputComponent();

        // Read movement vector (assuming 2D Vector2 control)
        Vector2 move = _controls.Gameplay.Move.ReadValue<Vector2>();
        input.MoveDirection = new float2(move.x, move.y);

        // Read look direction (e.g. mouse delta or right stick)
        Vector2 look = _controls.Gameplay.Look.ReadValue<Vector2>();
        input.LookDirection = new float3(look.x, look.y, 0);

        // Read attack button (pressed this frame)
        input.AttackPressed = _controls.Gameplay.Attack.WasPressedThisFrame();

        // Read jump button
        input.JumpPressed = _controls.Gameplay.Jump.IsPressed();

        // Update ECS component
        _entityManager.SetComponentData(_playerEntity, input);
    }
}
