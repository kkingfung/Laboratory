using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Consciousness.Core
{
    /// <summary>
    /// EMOTIONAL INDICATOR COMPONENT
    ///
    /// Provides visual feedback of chimera's current emotional state
    /// Shows mood, stress, happiness through simple emoji/icon system
    ///
    /// Design Philosophy:
    /// - Baby chimeras: Simple emotions (happy, sad, playful, scared)
    /// - Teen chimeras: Complex emotions (frustrated, anxious, excited, content)
    /// - Adult chimeras: Nuanced emotions (melancholic, serene, passionate, devoted)
    /// - Elderly chimeras: Deep emotions (wise, nostalgic, protective, fulfilled)
    ///
    /// Integration:
    /// - Updates based on CreaturePersonality current mood
    /// - Reflects bond strength and recent interactions
    /// - Age-appropriate emotional depth
    /// </summary>
    public struct EmotionalIndicatorComponent : IComponentData
    {
        // Current displayed emotion
        public EmotionalIcon currentIcon;
        public FixedString32Bytes currentEmoji;        // Unicode emoji for display
        public FixedString64Bytes emotionDescription;  // "Happy and playful"

        // Emotional state tracking
        public float emotionalIntensity;    // How strongly they feel (0.0-1.0)
        public float timeSinceLastChange;   // Track mood stability
        public EmotionalIcon previousIcon;  // For smooth transitions

        // Visual feedback modifiers
        public bool isFluctuating;         // Unstable emotions (teen angst, stress)
        public bool isSerene;              // Calm, stable elderly emotions
        public float displayPriority;      // Which emotion to show if conflicted
    }

    /// <summary>
    /// EMOTIONAL CONTEXT BUFFER - Tracks recent experiences affecting mood
    /// Used to determine current emotional state
    /// </summary>
    public struct EmotionalContext : IBufferElementData
    {
        public EmotionalTrigger triggerType;
        public float intensity;            // How much this affects mood (0.0-1.0)
        public float timestamp;
        public float decayRate;            // How fast this fades
        public FixedString32Bytes source;  // "Player interaction", "Hunger", etc.
    }

    /// <summary>
    /// EMOTIONAL TRANSITION EVENT - Triggered when mood changes significantly
    /// Provides feedback to UI systems
    /// </summary>
    public struct EmotionalTransitionEvent : IComponentData
    {
        public Entity creature;
        public EmotionalIcon previousEmotion;
        public EmotionalIcon newEmotion;
        public LifeStage currentAge;
        public float transitionIntensity;
        public float timestamp;
        public FixedString64Bytes reason; // "Bond strengthened", "Feeling neglected", etc.
    }

    /// <summary>
    /// Emotional icons/emojis mapped to age-appropriate feelings
    /// </summary>
    public enum EmotionalIcon : byte
    {
        // Universal emotions (all ages)
        Neutral = 0,           // 😐 Calm, observing
        Happy = 1,             // 😊 Content, pleased
        VeryHappy = 2,         // 😄 Excited, joyful
        Loving = 3,            // 🥰 Affectionate, bonded

        // Negative emotions (all ages)
        Sad = 10,              // 😢 Unhappy, disappointed
        Scared = 11,           // 😨 Fearful, anxious
        Angry = 12,            // 😠 Frustrated, upset
        Hurt = 13,             // 💔 Emotionally wounded

        // Baby/Child specific (simple)
        Playful = 20,          // 😆 Want to play!
        Curious = 21,          // 🤔 Exploring, interested
        Sleepy = 22,           // 😴 Tired, low energy
        Hungry = 23,           // 🤤 Want food

        // Teen/Adult specific (complex)
        Frustrated = 30,       // 😤 Things not going well
        Anxious = 31,          // 😰 Stressed, worried
        Excited = 32,          // 🤩 Enthusiastic!
        Melancholic = 33,      // 😔 Thoughtfully sad
        Devoted = 34,          // 💙 Deep loyalty showing
        Proud = 35,            // 😌 Accomplished feeling

        // Elderly specific (profound)
        Wise = 40,             // 🧘 Peaceful wisdom
        Nostalgic = 41,        // 🥲 Remembering good times
        Protective = 42,       // 🛡️ Guarding partner
        Fulfilled = 43,        // ✨ Life well lived
        Serene = 44,           // 🕊️ Deep peace
        Bittersweet = 45,      // 😌💭 Complex elderly emotion

        // Warning states (any age)
        Traumatized = 50,      // 😱 Severe emotional damage
        Depressed = 51,        // 😞 Long-term sadness
        Betrayed = 52,         // 💔😡 Trust broken
        Abandoned = 53         // 😢🚪 Feeling left behind
    }

    /// <summary>
    /// Triggers that affect emotional state
    /// </summary>
    public enum EmotionalTrigger : byte
    {
        // Positive triggers
        PlayerInteraction = 0,
        ReceivedGift = 1,
        PlayedTogether = 2,
        FedFavoriteFood = 3,
        WonActivity = 4,
        MadeNewFriend = 5,
        ExploredTogether = 6,
        Praised = 7,

        // Negative triggers
        Ignored = 10,
        Scolded = 11,
        Separated = 12,
        Lost = 13,
        Hurt = 14,
        Scared = 15,
        Hungry = 16,
        Lonely = 17,

        // Neutral/environment
        Weather = 20,
        TimeOfDay = 21,
        Location = 22,
        OtherCreatures = 23,

        // Age transitions
        GrowingUp = 30,
        BecameElderly = 31
    }

    /// <summary>
    /// AGE EMOTIONAL RANGE - Defines which emotions are accessible at each life stage
    /// Babies don't feel "melancholic", elderly don't feel simple "playful"
    /// </summary>
    public struct AgeEmotionalRange : IComponentData
    {
        public LifeStage lifeStage;

        // Available emotion categories
        public bool canFeelSimpleEmotions;    // Happy, sad, scared
        public bool canFeelComplexEmotions;   // Frustrated, devoted, melancholic
        public bool canFeelProfoundEmotions;  // Wise, serene, fulfilled

        // Emotional depth
        public float emotionalNuance;         // How subtle emotions can be (0.0-1.0)
        public float moodStability;           // How stable moods are
        public float transitionSpeed;         // How fast moods change

        // Age-specific traits
        public bool showsPlayfulness;         // Baby/child trait
        public bool showsWisdom;              // Elderly trait
        public bool showsAngst;               // Teen trait
    }

    /// <summary>
    /// EMOJI MAPPING - Maps EmotionalIcon to actual Unicode emoji strings
    /// For UI display
    /// </summary>
    public static class EmotionalIconMapper
    {
        public static string GetEmoji(EmotionalIcon icon)
        {
            return icon switch
            {
                // Universal
                EmotionalIcon.Neutral => "😐",
                EmotionalIcon.Happy => "😊",
                EmotionalIcon.VeryHappy => "😄",
                EmotionalIcon.Loving => "🥰",

                // Negative
                EmotionalIcon.Sad => "😢",
                EmotionalIcon.Scared => "😨",
                EmotionalIcon.Angry => "😠",
                EmotionalIcon.Hurt => "💔",

                // Baby/Child
                EmotionalIcon.Playful => "😆",
                EmotionalIcon.Curious => "🤔",
                EmotionalIcon.Sleepy => "😴",
                EmotionalIcon.Hungry => "🤤",

                // Teen/Adult
                EmotionalIcon.Frustrated => "😤",
                EmotionalIcon.Anxious => "😰",
                EmotionalIcon.Excited => "🤩",
                EmotionalIcon.Melancholic => "😔",
                EmotionalIcon.Devoted => "💙",
                EmotionalIcon.Proud => "😌",

                // Elderly
                EmotionalIcon.Wise => "🧘",
                EmotionalIcon.Nostalgic => "🥲",
                EmotionalIcon.Protective => "🛡️",
                EmotionalIcon.Fulfilled => "✨",
                EmotionalIcon.Serene => "🕊️",
                EmotionalIcon.Bittersweet => "😌",

                // Warning
                EmotionalIcon.Traumatized => "😱",
                EmotionalIcon.Depressed => "😞",
                EmotionalIcon.Betrayed => "💢",
                EmotionalIcon.Abandoned => "😭",

                _ => "😐"
            };
        }

        public static string GetDescription(EmotionalIcon icon)
        {
            return icon switch
            {
                EmotionalIcon.Neutral => "Calm and observing",
                EmotionalIcon.Happy => "Content and pleased",
                EmotionalIcon.VeryHappy => "Excited and joyful",
                EmotionalIcon.Loving => "Feeling affectionate",

                EmotionalIcon.Sad => "Feeling sad",
                EmotionalIcon.Scared => "Frightened",
                EmotionalIcon.Angry => "Upset and frustrated",
                EmotionalIcon.Hurt => "Emotionally wounded",

                EmotionalIcon.Playful => "Wants to play!",
                EmotionalIcon.Curious => "Exploring curiously",
                EmotionalIcon.Sleepy => "Feeling tired",
                EmotionalIcon.Hungry => "Wants food",

                EmotionalIcon.Frustrated => "Feeling frustrated",
                EmotionalIcon.Anxious => "Anxious and worried",
                EmotionalIcon.Excited => "Very enthusiastic!",
                EmotionalIcon.Melancholic => "Thoughtfully sad",
                EmotionalIcon.Devoted => "Showing deep loyalty",
                EmotionalIcon.Proud => "Feeling accomplished",

                EmotionalIcon.Wise => "Peaceful and wise",
                EmotionalIcon.Nostalgic => "Remembering fondly",
                EmotionalIcon.Protective => "Protecting you",
                EmotionalIcon.Fulfilled => "Life well lived",
                EmotionalIcon.Serene => "Deeply peaceful",
                EmotionalIcon.Bittersweet => "Complex feelings",

                EmotionalIcon.Traumatized => "Severely distressed",
                EmotionalIcon.Depressed => "Deeply sad",
                EmotionalIcon.Betrayed => "Trust broken",
                EmotionalIcon.Abandoned => "Feeling abandoned",

                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets the appropriate emotional icon based on personality state and age
        /// </summary>
        public static EmotionalIcon DetermineIcon(
            CreaturePersonality personality,
            LifeStage age,
            float bondStrength,
            bool hasRecentPositiveInteraction,
            bool hasRecentNegativeInteraction)
        {
            // Priority 1: Severe negative states
            if (personality.StressLevel > 0.8f && bondStrength < 0.3f)
            {
                if (age == LifeStage.Elderly)
                    return EmotionalIcon.Betrayed;
                return EmotionalIcon.Traumatized;
            }

            if (personality.HappinessLevel < 0.2f && bondStrength < 0.4f)
            {
                if (personality.DaysSinceLastInteraction > 7)
                    return EmotionalIcon.Abandoned;
                return EmotionalIcon.Depressed;
            }

            // Priority 2: Strong positive states
            if (personality.HappinessLevel > 0.8f && bondStrength > 0.7f)
            {
                if (age == LifeStage.Elderly)
                    return EmotionalIcon.Fulfilled;
                if (age >= LifeStage.Adult)
                    return EmotionalIcon.Loving;
                if (personality.Playfulness > 70)
                    return EmotionalIcon.Playful;
                return EmotionalIcon.VeryHappy;
            }

            // Priority 3: Age-specific emotional nuances
            switch (age)
            {
                case LifeStage.Baby:
                    if (personality.EnergyLevel < 0.3f) return EmotionalIcon.Sleepy;
                    if (personality.Curiosity > 70) return EmotionalIcon.Curious;
                    if (personality.Playfulness > 60) return EmotionalIcon.Playful;
                    return personality.HappinessLevel > 0.5f ? EmotionalIcon.Happy : EmotionalIcon.Neutral;

                case LifeStage.Child:
                    if (personality.Playfulness > 70) return EmotionalIcon.Playful;
                    if (hasRecentPositiveInteraction) return EmotionalIcon.Happy;
                    if (hasRecentNegativeInteraction) return EmotionalIcon.Sad;
                    return EmotionalIcon.Neutral;

                case LifeStage.Teen:
                    if (personality.StressLevel > 0.6f) return EmotionalIcon.Anxious;
                    if (personality.Aggression > 70 && personality.HappinessLevel < 0.5f) return EmotionalIcon.Frustrated;
                    if (bondStrength > 0.6f) return EmotionalIcon.Devoted;
                    return EmotionalIcon.Neutral;

                case LifeStage.Adult:
                    if (bondStrength > 0.8f) return EmotionalIcon.Devoted;
                    if (personality.StressLevel > 0.5f) return EmotionalIcon.Anxious;
                    if (personality.HappinessLevel > 0.6f) return EmotionalIcon.Content;
                    if (personality.HappinessLevel < 0.4f) return EmotionalIcon.Melancholic;
                    return EmotionalIcon.Neutral;

                case LifeStage.Elderly:
                    if (bondStrength > 0.8f && personality.StressLevel < 0.3f) return EmotionalIcon.Serene;
                    if (bondStrength > 0.7f) return EmotionalIcon.Protective;
                    if (hasRecentPositiveInteraction) return EmotionalIcon.Nostalgic;
                    if (personality.HappinessLevel > 0.7f) return EmotionalIcon.Wise;
                    if (personality.HappinessLevel < 0.4f) return EmotionalIcon.Bittersweet;
                    return EmotionalIcon.Serene;
            }

            return EmotionalIcon.Neutral;
        }

        // Add missing Content emotion
        private static EmotionalIcon Content => EmotionalIcon.Happy; // Map to existing
    }
}
