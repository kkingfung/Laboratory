using Unity.Entities;
using Infrastructure;
using UnityEngine;

namespace Models.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class MatchTimerSystem : SystemBase
    {
        private MatchTimer _matchTimer = null!;

        protected override void OnCreate()
        {
            base.OnCreate();

            // Resolve MatchTimer instance from ServiceLocator or DI container
            _matchTimer = Infrastructure.ServiceLocator.Instance.Resolve<MatchTimer>();
        }

        protected override void OnUpdate()
        {
            if (_matchTimer == null) return;

            float deltaTime = (float)SystemAPI.Time.DeltaTime;
            _matchTimer.Tick(deltaTime);
        }
    }
}
