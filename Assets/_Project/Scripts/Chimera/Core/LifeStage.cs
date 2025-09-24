namespace Laboratory.Chimera.Core
{
    /// <summary>
    /// Defines the different life stages of creatures in Project Chimera
    /// </summary>
    public enum LifeStage
    {
        /// <summary>Newly hatched/born, completely dependent - same as Infant</summary>
        Baby = 0,

        /// <summary>Newly hatched/born, completely dependent</summary>
        Infant = 0,

        /// <summary>Young but mobile, learning basic behaviors</summary>
        Juvenile = 1,
        
        /// <summary>Approaching sexual maturity, rapid growth</summary>
        Adolescent = 2,
        
        /// <summary>Fully mature, capable of breeding and peak performance</summary>
        Adult = 3,
        
        /// <summary>Past prime, declining stats but potentially higher wisdom</summary>
        Elder = 4,
        
        /// <summary>Ancient creatures with unique properties and rare abilities</summary>
        Ancient = 5
    }
    
    /// <summary>
    /// Helper methods for life stage calculations
    /// </summary>
    public static class LifeStageExtensions
    {
        /// <summary>
        /// Calculates life stage based on age and species lifespan
        /// </summary>
        public static LifeStage CalculateLifeStage(int ageInDays, int maxLifespanDays)
        {
            float agePercentage = (float)ageInDays / maxLifespanDays;
            
            return agePercentage switch
            {
                < 0.05f => LifeStage.Infant,
                < 0.25f => LifeStage.Juvenile,
                < 0.40f => LifeStage.Adolescent,
                < 0.80f => LifeStage.Adult,
                < 0.95f => LifeStage.Elder,
                _ => LifeStage.Ancient
            };
        }
        
        /// <summary>
        /// Gets stat modifiers for this life stage
        /// </summary>
        public static (float health, float attack, float defense, float speed, float intelligence) GetStatModifiers(this LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Infant => (0.3f, 0.2f, 0.2f, 0.4f, 0.1f),
                LifeStage.Juvenile => (0.6f, 0.5f, 0.5f, 0.8f, 0.4f),
                LifeStage.Adolescent => (0.8f, 0.7f, 0.7f, 1.0f, 0.7f),
                LifeStage.Adult => (1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
                LifeStage.Elder => (0.8f, 0.9f, 1.1f, 0.7f, 1.3f),
                LifeStage.Ancient => (0.6f, 0.8f, 1.2f, 0.5f, 1.5f),
                _ => (1.0f, 1.0f, 1.0f, 1.0f, 1.0f)
            };
        }
        
        /// <summary>
        /// Checks if this life stage can breed
        /// </summary>
        public static bool CanBreed(this LifeStage stage)
        {
            return stage is LifeStage.Adult or LifeStage.Elder;
        }
        
        /// <summary>
        /// Gets breeding efficiency for this life stage
        /// </summary>
        public static float GetBreedingEfficiency(this LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Adult => 1.0f,
                LifeStage.Elder => 0.6f,
                _ => 0.0f
            };
        }
    }
}
