using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Laboratory.Core.TownBuilding.Components;
using Laboratory.Core.TownBuilding.Types;

namespace Laboratory.Core.TownBuilding.Jobs
{

    [BurstCompile]
    public partial struct ResourceProductionJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(in BuildingComponent building,
            ref BuildingProductionComponent production,
            ref ResourceStorageComponent storage)
        {
            if (building.Status != BuildingStatus.Operational)
                return;

            // Calculate production based on building efficiency
            float effectiveProductionRate = production.ProductionRate * building.Efficiency * DeltaTime;

            // Produce resources based on building type
            switch (building.Type)
            {
                case BuildingType.BreedingCenter:
                    // Produces research points
                    storage.Research = math.min(storage.MaxResearch,
                        storage.Research + (int)(effectiveProductionRate * 2f));
                    break;

                case BuildingType.EquipmentShop:
                    // Generates currency
                    storage.Currency = math.min(storage.MaxCurrency,
                        storage.Currency + (int)(effectiveProductionRate * 5f));
                    break;

                case BuildingType.PowerPlant:
                    // Produces energy
                    storage.Energy = math.min(storage.MaxEnergy,
                        storage.Energy + (int)(effectiveProductionRate * 10f));
                    break;

                case BuildingType.CraftingWorkshop:
                    // Produces materials and equipment
                    storage.Materials = math.min(storage.MaxMaterials,
                        storage.Materials + (int)(effectiveProductionRate * 3f));
                    break;

                case BuildingType.MonsterHabitat:
                    // Produces food and happiness
                    storage.Food = math.min(storage.MaxFood,
                        storage.Food + (int)(effectiveProductionRate * 4f));
                    break;
            }

            // Update service capacity based on building
            UpdateServiceCapacity(ref production, building);
        }


        private void UpdateServiceCapacity(ref BuildingProductionComponent production, BuildingComponent building)
        {
            production.ServiceCapacity = building.Type switch
            {
                BuildingType.Hospital => 50 + building.UpgradeLevel * 20,
                BuildingType.BreedingCenter => 10 + building.UpgradeLevel * 5,
                BuildingType.TrainingGround => 20 + building.UpgradeLevel * 10,
                BuildingType.ResearchLab => 5 + building.UpgradeLevel * 3,
                BuildingType.EquipmentShop => 100 + building.UpgradeLevel * 50,
                _ => 10
            };

            production.ServiceQuality = building.Efficiency * (1f + building.UpgradeLevel * 0.1f);
        }
    }
}