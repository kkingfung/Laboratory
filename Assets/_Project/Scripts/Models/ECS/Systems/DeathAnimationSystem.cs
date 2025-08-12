using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class DeathAnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithAll<DeadTag>()
            .ForEach((Entity entity, ref DeathAnimationTrigger deathAnimTrigger) =>
            {
                if (!deathAnimTrigger.Triggered)
                {
                    if (EntityManager.HasComponent<Unity.Netcode.NetworkObject>(entity))
                    {
                        var networkObject = EntityManager.GetComponentObject<Unity.Netcode.NetworkObject>(entity);
                        var animator = networkObject.gameObject.GetComponent<Animator>();
                        if (animator != null)
                        {
                            animator.SetTrigger("Die"); // Make sure you have a "Die" trigger in your Animator Controller
                        }
                    }

                    deathAnimTrigger.Triggered = true;
                }
            }).WithoutBurst().Run();
    }
}
