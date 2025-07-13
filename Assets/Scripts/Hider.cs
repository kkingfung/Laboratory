// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using UnityEngine;

public struct Hider : IComponentData
{
    public int Health;
    public float HideRange;
    public LayerMask TreeLayer;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct HiderHideSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (hider, localTransform) in SystemAPI.Query<Hider, LocalTransform>())
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                var colliders = Physics.OverlapSphere(localTransform.Position, hider.HideRange, hider.TreeLayer);
                if (colliders.Length > 0)
                {
                    // Successfully hiding in a tree
                    UnityEngine.Debug.Log("Hider is hiding in a tree!");
                }
                else
                {
                    UnityEngine.Debug.Log("No trees nearby to hide in.");
                }
            }
        }
    }
}