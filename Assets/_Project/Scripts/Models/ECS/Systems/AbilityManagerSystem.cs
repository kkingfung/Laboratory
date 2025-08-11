using Unity.Entities;
using Infrastructure;
using UnityEngine;

namespace Models.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class AbilityManagerSystem : SystemBase
    {
        private AbilityManager _abilityManager = null!;

        protected override void OnCreate()
        {
            base.OnCreate();

            _abilityManager = Infrastructure.ServiceLocator.Instance.Resolve<AbilityManager>();
        }

        protected override void OnUpdate()
        {
            if (_abilityManager == null) return;

            float deltaTime = (float)SystemAPI.Time.DeltaTime;
            _abilityManager.Tick(deltaTime);
        }
    }
}
