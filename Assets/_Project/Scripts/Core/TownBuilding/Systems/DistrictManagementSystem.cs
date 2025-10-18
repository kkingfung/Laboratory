using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Core.TownBuilding.Components;
using Laboratory.Core.TownBuilding.Types;

namespace Laboratory.Core.TownBuilding.Systems
{
    /// <summary>
    /// District management and city planning system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class DistrictManagementSystem : SystemBase
    {
        private EntityQuery districtQuery;

        protected override void OnCreate()
        {
            districtQuery = GetEntityQuery(ComponentType.ReadWrite<DistrictComponent>());
        }

        protected override void OnUpdate()
        {
            foreach (var district in SystemAPI.Query<RefRW<DistrictComponent>>())
            {
                UpdateDistrictMetrics(ref district.ValueRW);
            }
        }

        private void UpdateDistrictMetrics(ref DistrictComponent district)
        {
            // Calculate district efficiency based on building density and types
            float optimalDensity = 0.7f; // 70% capacity is optimal
            float currentDensity = (float)district.BuildingCount / district.MaxBuildings;

            if (currentDensity <= optimalDensity)
            {
                district.DistrictEfficiency = currentDensity / optimalDensity;
            }
            else
            {
                // Overcrowding penalty
                district.DistrictEfficiency = optimalDensity / currentDensity;
            }

            // Update happiness based on district type and efficiency
            district.HappinessLevel = district.Type switch
            {
                DistrictType.Recreational => district.DistrictEfficiency * 1.5f,
                DistrictType.Residential => district.DistrictEfficiency * 1.2f,
                DistrictType.Commercial => district.DistrictEfficiency * 1.0f,
                DistrictType.Industrial => district.DistrictEfficiency * 0.8f,
                _ => district.DistrictEfficiency
            };

            district.HappinessLevel = math.clamp(district.HappinessLevel, 0.1f, 2.0f);

            // Update development level
            float targetDevelopment = currentDensity * district.DistrictEfficiency;
            district.DevelopmentLevel = math.lerp(district.DevelopmentLevel, targetDevelopment, 0.01f);
        }
    }
}