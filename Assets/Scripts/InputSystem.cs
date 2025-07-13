// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Component to store input data
public struct InputData : IComponentData
{
    public float2 Movement; // Stores input for movement
    public bool Jump;       // Stores input for jumping
}

// System to handle player input
[UpdateInGroup(typeof(InitializationSystemGroup))] // Runs early in the frame
public class InputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Read input from Unity's Input system
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool jump = Input.GetButton("Jump");

        // Create an InputData struct with the current input values
        InputData inputData = new InputData
        {
            Movement = new float2(horizontal, vertical),
            Jump = jump
        };

        // Apply the input data to all entities with an InputData component
        Entities.ForEach((ref InputData input) =>
        {
            input = inputData;
        }).ScheduleParallel();
    }
}