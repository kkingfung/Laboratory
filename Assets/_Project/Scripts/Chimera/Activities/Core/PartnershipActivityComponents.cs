using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Progression;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// PARTNERSHIP ACTIVITY COMPONENT
    ///
    /// NEW VISION: Victory comes from player skill + chimera cooperation, not stats
    ///
    /// Design Philosophy:
    /// - NO LEVELS - Skill improves through practice
    /// - NO STAT BONUSES - Equipment affects personality/cooperation
    /// - PLAYER SKILL FIRST - Player ability is primary factor
    /// - COOPERATION MULTIPLIER - Chimera cooperation affects success
    /// - PERSONALITY FIT - Right chimera for right activity
    ///
    /// Success Formula:
    /// FinalScore = PlayerPerformance × CooperationMultiplier × PersonalityFitBonus
    /// </summary>
    public struct PartnershipActivityComponent : IComponentData
    {
        public Entity partnershipEntity;     // Player-Chimera partnership
        public Entity chimeraEntity;         // Chimera participating in activity
        public ActivityType currentActivity;
        public ActivityGenreCategory genre;
        public ActivityDifficulty difficulty;

        // Player performance tracking
        public float playerPerformance;      // 0.0-1.0 (how well player did)
        public float playerInputQuality;     // Timing, accuracy, decisions

        // Chimera cooperation tracking
        public float chimeraCooperation;     // From PartnershipSkillComponent
        public float personalityFitBonus;    // Does personality match activity?
        public float moodBonus;              // Is chimera happy right now?
        public float equipmentCooperationBonus; // Equipment fit bonus

        // Activity progress
        public float startTime;
        public float duration;
        public float elapsedTime;
        public bool isComplete;

        // Results
        public float finalScore;             // Combined score
        public ActivityResultStatus resultStatus;
        public float skillImprovement;       // How much skill improved (0.0-1.0)
    }

    /// <summary>
    /// ACTIVITY MASTERY TRACKER - Tracks skill mastery per activity genre
    /// Replaces old level-based system
    /// </summary>
    public struct ActivityMasteryTracker : IBufferElementData
    {
        public ActivityGenreCategory genre;
        public float masteryLevel;           // 0.0-1.0 (0% to 100% mastery)

        // Practice tracking
        public int totalAttempts;
        public int successfulCompletions;
        public float successRate;            // successfulCompletions / totalAttempts

        // Performance tracking
        public float averagePerformance;     // Moving average of recent performances
        public float bestPerformance;        // Personal best
        public float recentTrend;            // Improving (+) or declining (-)

        // Partnership quality
        public float averageCooperation;     // How well partnership works in this genre
        public float lastActivityTime;
    }

    /// <summary>
    /// ACTIVITY PERSONALITY FIT - Calculates how well chimera's personality fits activity
    /// </summary>
    public struct ActivityPersonalityFit : IComponentData
    {
        public Entity chimeraEntity;
        public ActivityType activityType;
        public ActivityGenreCategory genre;

        // Personality fit calculation
        public float calculatedFitScore;     // 0.0 (terrible) to 1.0 (perfect)
        public float cooperationBonus;       // -0.3 to +0.3
        public FixedString128Bytes fitReason; // "Playful chimera + Racing = good fit"

        // Preferences
        public bool chimeraEnjoysActivity;
        public bool chimeraDislikesActivity;
    }

    /// <summary>
    /// PARTNERSHIP ACTIVITY REQUEST - Start activity with partnership approach
    /// </summary>
    public struct StartPartnershipActivityRequest : IComponentData
    {
        public Entity partnershipEntity;
        public Entity chimeraEntity;
        public ActivityType activityType;
        public ActivityDifficulty difficulty;
        public float requestTime;

        // Pre-calculated factors
        public float currentCooperation;     // From partnership component
        public float personalityFit;         // How well chimera fits this activity
        public float equipmentBonus;         // Equipment cooperation bonus
    }

    /// <summary>
    /// PARTNERSHIP ACTIVITY RESULT - Results emphasizing cooperation over stats
    /// </summary>
    public struct PartnershipActivityResult : IComponentData
    {
        public Entity partnershipEntity;
        public Entity chimeraEntity;
        public ActivityType activityType;
        public ActivityGenreCategory genre;

        // Performance breakdown
        public float playerPerformance;      // Player's raw skill (0.0-1.0)
        public float cooperationMultiplier;  // Chimera cooperation (0.5-1.5)
        public float personalityFitBonus;    // Personality match bonus (0.0-0.3)
        public float finalScore;             // Combined result

        // Results
        public ActivityResultStatus status;
        public float completionTime;
        public float timestamp;

        // Skill improvement
        public float skillGained;            // How much mastery improved
        public bool cooperationImproved;     // Did partnership get better?
        public float bondStrengthChange;     // Bond impact (+/-)

        // Rewards (cosmetic only!)
        public int cosmeticRewardsUnlocked;
        public FixedString64Bytes achievementUnlocked; // "First Racing Victory!"

        // Emotional impact
        public EmotionalTrigger emotionalImpact; // How chimera felt about this
    }

    /// <summary>
    /// ACTIVITY COOPERATION CALCULATOR - Calculates cooperation multiplier for activities
    /// </summary>
    public struct ActivityCooperationCalculation : IComponentData
    {
        public Entity partnershipEntity;
        public ActivityType activityType;

        // Cooperation factors
        public float baseCooperation;        // From PartnershipSkillComponent
        public float genreMasteryBonus;      // Experience in this genre
        public float personalityFitBonus;    // Personality match
        public float moodBonus;              // Current emotional state
        public float equipmentBonus;         // Equipment fit
        public float bondStrengthBonus;      // Bond quality

        // Final multiplier
        public float totalCooperationMultiplier; // 0.5 to 1.5
    }

    /// <summary>
    /// ACTIVITY SKILL MILESTONE EVENT - Triggered when reaching skill milestones
    /// Replaces old level-up events
    /// </summary>
    public struct ActivitySkillMilestoneEvent : IComponentData
    {
        public Entity partnershipEntity;
        public ActivityGenreCategory genre;
        public SkillMilestoneType milestoneType; // Beginner, Competent, Proficient, Expert, Master
        public float previousMastery;
        public float newMastery;
        public float timestamp;
        public FixedString128Bytes description; // "Reached 50% Racing mastery!"
    }

    /// <summary>
    /// DEPRECATED - Old stat-based activity progress
    /// Use ActivityMasteryTracker instead
    /// </summary>
    [System.Obsolete("Use ActivityMasteryTracker - no levels, skill-based progression")]
    public struct LegacyActivityProgress : IBufferElementData
    {
        public ActivityType activityType;
        public int experiencePoints;
        public int level;
        public float masteryMultiplier;
    }

    /// <summary>
    /// Helper class for calculating activity personality fit
    /// </summary>
    public static class ActivityPersonalityFitCalculator
    {
        /// <summary>
        /// Calculates how well chimera's personality fits an activity
        /// </summary>
        public static float CalculateFit(
            Laboratory.Chimera.Consciousness.Core.CreaturePersonality personality,
            ActivityType activityType,
            ActivityGenreCategory genre)
        {
            float fitScore = 0.5f; // Start neutral

            switch (genre)
            {
                case ActivityGenreCategory.Action:
                    // Action activities benefit from high playfulness and energy
                    if (personality.Playfulness > 60) fitScore += 0.2f;
                    if (personality.Aggression > 50) fitScore += 0.1f; // Combat games
                    if (personality.Nervousness > 60) fitScore -= 0.2f; // Nervous dislikes action
                    break;

                case ActivityGenreCategory.Strategy:
                    // Strategy benefits from high curiosity and patience
                    if (personality.Curiosity > 60) fitScore += 0.25f;
                    if (personality.Playfulness > 70) fitScore -= 0.15f; // Too playful for strategy
                    if (personality.Stubbornness > 60) fitScore += 0.1f; // Persistence helps
                    break;

                case ActivityGenreCategory.Puzzle:
                    // Puzzles need curiosity and patience
                    if (personality.Curiosity > 70) fitScore += 0.3f;
                    if (personality.Playfulness > 60) fitScore -= 0.1f; // Playful gets bored
                    if (personality.Nervousness > 50) fitScore -= 0.1f; // Stress hurts puzzle solving
                    break;

                case ActivityGenreCategory.Racing:
                    // Racing needs energy and confidence
                    if (personality.Playfulness > 65) fitScore += 0.2f;
                    if (personality.Aggression > 55) fitScore += 0.15f; // Competitive spirit
                    if (personality.Nervousness > 60) fitScore -= 0.25f; // Too nervous for racing
                    break;

                case ActivityGenreCategory.Rhythm:
                    // Rhythm needs playfulness and timing
                    if (personality.Playfulness > 70) fitScore += 0.25f;
                    if (personality.Curiosity > 60) fitScore += 0.1f;
                    if (personality.Nervousness > 55) fitScore -= 0.15f; // Stress hurts timing
                    break;

                case ActivityGenreCategory.Exploration:
                    // Exploration needs curiosity and independence
                    if (personality.Curiosity > 75) fitScore += 0.3f;
                    if (personality.Independence > 60) fitScore += 0.15f;
                    if (personality.Nervousness > 65) fitScore -= 0.2f; // Too scared to explore
                    break;

                case ActivityGenreCategory.Economics:
                    // Economics needs patience and social skills
                    if (personality.Curiosity > 60) fitScore += 0.15f;
                    if (personality.Affection > 60) fitScore += 0.1f; // Social trading
                    if (personality.Playfulness > 70) fitScore -= 0.15f; // Too playful for economics
                    break;
            }

            return Unity.Mathematics.math.clamp(fitScore, 0f, 1f);
        }

        /// <summary>
        /// Gets cooperation bonus based on personality fit
        /// </summary>
        public static float GetCooperationBonus(float fitScore)
        {
            if (fitScore > 0.8f) return 0.3f;  // Perfect fit: +30% cooperation
            if (fitScore > 0.6f) return 0.15f; // Good fit: +15% cooperation
            if (fitScore > 0.4f) return 0.0f;  // Neutral fit: no change
            if (fitScore > 0.2f) return -0.15f; // Poor fit: -15% cooperation
            return -0.3f;                       // Terrible fit: -30% cooperation
        }

        /// <summary>
        /// Determines if chimera would enjoy this activity
        /// </summary>
        public static bool WouldEnjoyActivity(float fitScore)
        {
            return fitScore > 0.6f;
        }

        /// <summary>
        /// Determines if chimera would dislike this activity
        /// </summary>
        public static bool WouldDislikeActivity(float fitScore)
        {
            return fitScore < 0.3f;
        }
    }

    /// <summary>
    /// Emotional triggers from activities for EmotionalContext system
    /// </summary>
    public enum EmotionalTrigger : byte
    {
        WonActivity = 0,
        LostActivity = 1,
        EnjoyedActivity = 2,
        FrustratedByActivity = 3,
        PerfectCooperation = 4,
        PoorCooperation = 5
    }
}
