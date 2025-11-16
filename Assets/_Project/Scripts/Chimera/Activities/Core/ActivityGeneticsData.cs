using Unity.Entities;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// Lightweight struct containing genetic trait values for activity performance calculations.
    /// </summary>
    public struct ActivityGeneticsData : IComponentData
    {
        // Physical traits
        public float StrengthTrait;
        public float VitalityTrait;
        public float AgilityTrait;
        public float ResilienceTrait;
        public float IntellectTrait;
        public float CharmTrait;

        // Performance traits for activities
        public float Speed;
        public float Stamina;
        public float Agility;
        public float Intelligence;
        public float Aggression;
        public float Curiosity;
        public float Caution;
        public float Dominance;
        public float Sociability;
        public float Adaptability;
        public float Size;
    }
}
