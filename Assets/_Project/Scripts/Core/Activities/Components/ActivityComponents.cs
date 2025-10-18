using Unity.Entities;

namespace Laboratory.Core.Activities.Components
{
    /// <summary>
    /// Core activity participation component
    /// </summary>
    public struct ActivityParticipantComponent : IComponentData
    {
        public ActivityType CurrentActivity;
        public ActivityStatus Status;
        public Entity ActivityCenter;
        public float PerformanceScore;
        public float ActivityProgress;
        public float TimeInActivity;
        public int ExperienceGained;
        public bool HasRewards;
    }

    /// <summary>
    /// Activity center instance component
    /// </summary>
    public struct ActivityCenterComponent : IComponentData
    {
        public ActivityType ActivityType;
        public int MaxParticipants;
        public int CurrentParticipants;
        public float ActivityDuration;
        public float DifficultyLevel;
        public bool IsActive;
        public float QualityRating;
        public Entity OwnerCreature;
    }

    /// <summary>
    /// Activity performance tracking
    /// </summary>
    public struct ActivityPerformanceComponent : IComponentData
    {
        // Racing Performance
        public float RacingSpeed;
        public float RacingAgility;
        public float RacingEndurance;

        // Combat Performance
        public float CombatPower;
        public float CombatDefense;
        public float CombatStrategy;

        // Puzzle Performance
        public float PuzzleSolving;
        public float LogicSpeed;
        public float PatternRecognition;

        // Overall metrics
        public float OverallRating;
        public int TotalActivitiesCompleted;
        public float BestPerformance;
    }

    /// <summary>
    /// Equipment effects on activity performance
    /// </summary>
    public struct ActivityEquipmentComponent : IComponentData
    {
        public Entity EquippedGear;
        public float SpeedBonus;
        public float StrengthBonus;
        public float IntelligenceBonus;
        public float EnduranceBonus;
        public float SpecialtyBonus;
    }
}