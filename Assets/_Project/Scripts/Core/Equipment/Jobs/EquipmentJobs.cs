using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Laboratory.Core.Equipment.Components;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities.Components;

namespace Laboratory.Core.Equipment.Jobs
{

    public partial struct EquipmentEffectsJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref CreatureEquipmentComponent equipment,
            RefRO<GeneticDataComponent> genetics,
            RefRO<ActivityParticipantComponent> activity)
        {
            // Calculate total equipment bonuses
            float totalStatBonus = 0f;
            int equippedCount = 0;

            // Count equipped items and calculate bonuses
            if (equipment.HeadGear != Entity.Null) equippedCount++;
            if (equipment.BodyArmor != Entity.Null) equippedCount++;
            if (equipment.Weapon != Entity.Null) equippedCount++;
            if (equipment.Accessory1 != Entity.Null) equippedCount++;
            if (equipment.Accessory2 != Entity.Null) equippedCount++;
            if (equipment.SpecialGear != Entity.Null) equippedCount++;

            equipment.EquippedItemCount = equippedCount;

            // Set bonus for complete equipment sets
            equipment.HasCompleteSet = equippedCount >= 4;
            if (equipment.HasCompleteSet)
            {
                totalStatBonus += 0.2f; // 20% set bonus
            }

            equipment.TotalStatBonus = totalStatBonus;
        }
    }


    public partial struct EquipmentDurabilityJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref EquipmentComponent equipment, in EquipmentStatsComponent stats)
        {
            if (!equipment.IsEquipped || equipment.Durability <= 0f)
                return;

            // Equipment loses durability over time when equipped
            float durabilityLoss = stats.DurabilityLoss * DeltaTime;
            equipment.Durability = math.max(0f, equipment.Durability - durabilityLoss);
        }
    }
}