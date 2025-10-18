using Unity.Entities;
using Laboratory.Core.Progression.Components;

namespace Laboratory.Core.Progression.Systems
{
    /// <summary>
    /// Skill point allocation system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class SkillAllocationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // This system would handle automatic or player-directed skill point allocation
            // For now, it's a placeholder for future skill tree implementation

            foreach (var (progression, skills) in
                SystemAPI.Query<RefRW<CreatureProgressionComponent>, RefRW<CreatureSkillsComponent>>())
            {
                // Auto-allocate skill points based on creature genetics and activity preferences
                if (progression.ValueRO.AvailableSkillPoints > 0)
                {
                    // Simple auto-allocation logic (could be expanded)
                    var progressionData = progression.ValueRO;
                    var skillsData = skills.ValueRO;

                    if (progressionData.RacingExperience > progressionData.CombatExperience)
                    {
                        skillsData.SpeedMastery += 5;
                        skillsData.AgilityMastery += 3;
                    }
                    else if (progressionData.CombatExperience > progressionData.PuzzleExperience)
                    {
                        skillsData.AttackMastery += 5;
                        skillsData.DefenseMastery += 3;
                    }
                    else
                    {
                        skillsData.LogicMastery += 5;
                        skillsData.CreativityMastery += 3;
                    }

                    progressionData.AvailableSkillPoints--;
                    progressionData.SpentSkillPoints++;

                    progression.ValueRW = progressionData;
                    skills.ValueRW = skillsData;
                }
            }
        }
    }
}