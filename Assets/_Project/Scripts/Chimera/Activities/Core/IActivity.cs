using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Creatures;
using Unity.Entities;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// Interface for all activity mini-games
    /// Defines the contract for activity execution and performance calculation
    /// </summary>
    public interface IActivity
    {
        /// <summary>
        /// Type of activity
        /// </summary>
        ActivityType Type { get; }

        /// <summary>
        /// Calculates performance score based on monster genetics and equipment
        /// </summary>
        /// <param name="genetics">Monster's genetic component</param>
        /// <param name="difficulty">Activity difficulty level</param>
        /// <param name="equipmentBonus">Bonus from equipped items (0.0 to 1.0)</param>
        /// <param name="masteryBonus">Bonus from activity mastery (1.0 to 1.5)</param>
        /// <returns>Performance score (0.0 to 1.0)</returns>
        float CalculatePerformance(
            in ActivityGeneticsData genetics,
            ActivityDifficulty difficulty,
            float equipmentBonus,
            float masteryBonus);

        /// <summary>
        /// Calculates rewards based on performance
        /// </summary>
        /// <param name="performanceScore">Performance score (0.0 to 1.0)</param>
        /// <param name="difficulty">Activity difficulty level</param>
        /// <returns>Activity result with rewards</returns>
        ActivityResult CalculateRewards(
            float performanceScore,
            ActivityDifficulty difficulty,
            float completionTime);

        /// <summary>
        /// Gets the rank based on performance score
        /// </summary>
        /// <param name="performanceScore">Performance score (0.0 to 1.0)</param>
        /// <returns>Result status (Failed, Bronze, Silver, Gold, Platinum)</returns>
        ActivityResultStatus GetRank(float performanceScore);

        /// <summary>
        /// Gets base duration for this activity in seconds
        /// </summary>
        float GetBaseDuration(ActivityDifficulty difficulty);
    }
}
