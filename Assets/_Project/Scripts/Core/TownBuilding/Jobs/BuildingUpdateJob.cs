using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Laboratory.Core.TownBuilding.Components;
using Laboratory.Core.TownBuilding.Types;

namespace Laboratory.Core.TownBuilding.Jobs
{

    public partial struct BuildingUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity,
            ref BuildingComponent building,
            in LocalTransform transform)
        {
            switch (building.Status)
            {
                case BuildingStatus.UnderConstruction:
                    UpdateConstruction(ref building, DeltaTime);
                    break;

                case BuildingStatus.Operational:
                    UpdateOperation(ref building, DeltaTime);
                    break;

                case BuildingStatus.Upgrading:
                    UpdateUpgrade(ref building, DeltaTime);
                    break;

                case BuildingStatus.Maintenance:
                    UpdateMaintenance(ref building, DeltaTime);
                    break;
            }

            // Handle durability degradation
            if (building.Status == BuildingStatus.Operational)
            {
                float degradationRate = CalculateDegradationRate(building.Type, building.Tier);
                building.Durability = math.max(0f, building.Durability - degradationRate * DeltaTime);

                // Building needs maintenance
                if (building.Durability < 0.3f && building.Status != BuildingStatus.Maintenance)
                {
                    building.Status = BuildingStatus.Maintenance;
                }
            }
        }


        private void UpdateConstruction(ref BuildingComponent building, float deltaTime)
        {
            float constructionSpeed = CalculateConstructionSpeed(building.Type, building.Tier);
            building.ConstructionProgress += constructionSpeed * deltaTime;

            if (building.ConstructionProgress >= 1f)
            {
                building.Status = BuildingStatus.Operational;
                building.ConstructionProgress = 1f;
                building.Durability = 1f;
                building.Efficiency = CalculateBaseEfficiency(building.Type, building.Tier);
            }
        }


        private void UpdateOperation(ref BuildingComponent building, float deltaTime)
        {
            // Buildings operating efficiently
            building.Efficiency = math.clamp(building.Efficiency, 0.1f, 2.0f);

            // Efficiency affects all building functions
            float efficiencyDecay = 0.001f * deltaTime; // Very slow efficiency decay
            building.Efficiency = math.max(0.5f, building.Efficiency - efficiencyDecay);
        }


        private void UpdateUpgrade(ref BuildingComponent building, float deltaTime)
        {
            float upgradeSpeed = CalculateUpgradeSpeed(building.Type, building.UpgradeLevel);
            building.ConstructionProgress += upgradeSpeed * deltaTime;

            if (building.ConstructionProgress >= 1f)
            {
                building.UpgradeLevel++;
                building.Status = BuildingStatus.Operational;
                building.ConstructionProgress = 0f;
                building.Efficiency += 0.2f; // Efficiency boost from upgrade
            }
        }


        private void UpdateMaintenance(ref BuildingComponent building, float deltaTime)
        {
            float maintenanceSpeed = 0.5f; // Fixed maintenance rate
            building.ConstructionProgress += maintenanceSpeed * deltaTime;

            if (building.ConstructionProgress >= 1f)
            {
                building.Status = BuildingStatus.Operational;
                building.ConstructionProgress = 0f;
                building.Durability = 1f; // Full repair
            }
        }


        private float CalculateConstructionSpeed(BuildingType type, BuildingTier tier)
        {
            float baseSpeed = type switch
            {
                BuildingType.Road => 0.5f, // Fast construction
                BuildingType.Park => 0.3f,
                BuildingType.MonsterHabitat => 0.2f,
                BuildingType.BreedingCenter => 0.1f, // Slow, complex construction
                BuildingType.ResearchLab => 0.08f,
                BuildingType.PowerPlant => 0.05f, // Very slow infrastructure
                _ => 0.2f
            };

            float tierMultiplier = (int)tier switch
            {
                1 => 1.0f,
                2 => 0.8f,
                3 => 0.6f,
                4 => 0.4f,
                5 => 0.2f,
                _ => 1.0f
            };

            return baseSpeed * tierMultiplier;
        }


        private float CalculateUpgradeSpeed(BuildingType type, int level)
        {
            float baseUpgradeSpeed = 0.1f;
            float levelPenalty = level * 0.02f; // Harder to upgrade higher levels
            return math.max(0.01f, baseUpgradeSpeed - levelPenalty);
        }


        private float CalculateBaseEfficiency(BuildingType type, BuildingTier tier)
        {
            float baseEfficiency = (int)tier * 0.2f + 0.6f; // 0.8 to 1.6 range
            return math.clamp(baseEfficiency, 0.5f, 2.0f);
        }


        private float CalculateDegradationRate(BuildingType type, BuildingTier tier)
        {
            float baseDegradation = type switch
            {
                BuildingType.Road => 0.0001f, // Roads degrade slowly
                BuildingType.Monument => 0.00001f, // Monuments are very durable
                BuildingType.PowerPlant => 0.0003f, // Infrastructure needs maintenance
                BuildingType.EquipmentShop => 0.0002f, // Commercial buildings
                _ => 0.0001f
            };

            float tierBonus = (int)tier * 0.5f; // Higher tiers are more durable
            return baseDegradation / (1f + tierBonus);
        }
    }
}