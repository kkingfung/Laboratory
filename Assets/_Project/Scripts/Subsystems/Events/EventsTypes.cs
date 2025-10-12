using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.Events
{
    #region Core Event Data

    [Serializable]
    public class EventTrigger
    {
        public EventTriggerType triggerType;
        public DateTime timestamp;
        public Dictionary<string, object> data = new();
    }

    [Serializable]
    public class ActiveEvent
    {
        public object eventData;
        public DateTime startTime;
        public bool isActive;
        public Dictionary<string, object> metadata = new();
    }

    [Serializable]
    public class EventMetrics
    {
        public int totalCelebrations;
        public int totalWorldFirsts;
        public int activeEvents;
        public int activePlayers;
        public int seasonalEventsThisYear;
        public int tournamentsCompleted;
        public float averageParticipation;
    }

    public enum EventTriggerType
    {
        Discovery,
        Seasonal,
        Tournament,
        Community,
        Research,
        Environmental
    }

    #endregion

    #region Discovery Celebrations

    [Serializable]
    public class DiscoveryCelebrationEvent
    {
        public CelebrationData celebration;
        public string playerId;
        public bool isWorldFirst;
        public DateTime timestamp;
    }

    [Serializable]
    public class CelebrationData
    {
        public string eventId;
        public CelebrationType celebrationType;
        public string title;
        public string description;
        public DateTime startTime;
        public TimeSpan duration;
        public bool isWorldFirst;
        public VisualEffects visualEffects;
        public AudioEffects audioEffects;
        public Dictionary<string, object> celebrationData = new();
    }

    [Serializable]
    public class CelebrationTemplate
    {
        public CelebrationType celebrationType;
        public string templateName;
        public string titleFormat;
        public string descriptionFormat;
        public float baseDurationMinutes = 2f;
        public VisualEffects defaultVisualEffects;
        public AudioEffects defaultAudioEffects;
        public bool enableWorldFirstVariant = true;
        public List<CelebrationTrigger> triggers = new();
    }

    [Serializable]
    public class CelebrationTrigger
    {
        public DiscoveryType requiredDiscoveryType;
        public float minimumSignificance = 0.5f;
        public bool requiresWorldFirst = false;
        public List<string> requiredTraits = new();
        public float cooldownHours = 1f;
    }

    [Serializable]
    public class VisualEffects
    {
        public ParticleSystem celebrationParticles;
        public GameObject uiOverlay;
        public Color primaryColor = Color.gold;
        public Color secondaryColor = Color.white;
        public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public bool enableScreenFlash = true;
        public bool enableConfetti = true;
        public bool enableAuraEffect = false;
    }

    [Serializable]
    public class AudioEffects
    {
        public AudioClip celebrationSound;
        public AudioClip worldFirstSound;
        public AudioClip backgroundMusic;
        public float volume = 0.8f;
        public bool enableSpatialAudio = false;
        public bool fadeInMusic = true;
    }

    public enum CelebrationType
    {
        TraitDiscovery,
        SpeciesDiscovery,
        MutationDiscovery,
        BreedingSuccess,
        ResearchPublication,
        WorldFirst,
        Milestone,
        Achievement
    }

    #endregion

    #region Seasonal Events

    [Serializable]
    public class SeasonalEvent
    {
        public SeasonalEventData eventData;
        public DateTime timestamp;
    }

    [Serializable]
    public class SeasonalEventData
    {
        public string eventId;
        public string eventName;
        public SeasonalEventType eventType;
        public string description;
        public DateTime startTime;
        public DateTime endTime;
        public TimeSpan duration;
        public bool isActive;
        public SeasonalTrigger trigger;
        public List<SeasonalReward> rewards = new();
        public Dictionary<string, object> eventConfiguration = new();
    }

    [Serializable]
    public class SeasonalTrigger
    {
        public SeasonalTriggerType triggerType;
        public int month = 1; // 1-12
        public int day = 1;   // 1-31
        public float timeOfDay = 0f; // 0-24 hours
        public string requiredWeather;
        public float requiredTemperature = 20f;
        public bool recurring = true;
    }

    [Serializable]
    public class SeasonalReward
    {
        public string rewardType;
        public string rewardId;
        public int quantity = 1;
        public float bonusMultiplier = 1f;
        public List<string> eligiblePlayers = new();
        public Dictionary<string, object> rewardData = new();
    }

    public enum SeasonalEventType
    {
        BreedingSeason,
        Migration,
        WeatherCelebration,
        EnvironmentalChange,
        CommunityChallenge,
        ConservationEvent,
        ResearchSymposium,
        DiscoveryFestival
    }

    public enum SeasonalTriggerType
    {
        DateBased,
        WeatherBased,
        PopulationBased,
        CommunityBased,
        Manual
    }

    #endregion

    #region Tournaments

    [Serializable]
    public class TournamentEvent
    {
        public Tournament tournament;
        public TournamentEventType eventType;
        public DateTime timestamp;
    }

    [Serializable]
    public class Tournament
    {
        public string tournamentId;
        public string title;
        public string description;
        public TournamentType tournamentType;
        public DateTime startTime;
        public DateTime endTime;
        public DateTime registrationDeadline;
        public TournamentStatus status;
        public string organizerId;
        public TournamentRules rules;
        public List<TournamentEntry> participants = new();
        public List<TournamentRound> rounds = new();
        public TournamentRewards rewards;
        public Dictionary<string, object> tournamentData = new();
    }

    [Serializable]
    public class TournamentRequest
    {
        public string title;
        public string description;
        public TournamentType tournamentType;
        public DateTime preferredStartTime;
        public TimeSpan duration;
        public int maxParticipants = 32;
        public TournamentRules rules;
        public bool isPublic = true;
        public Dictionary<string, object> customSettings = new();
    }

    [Serializable]
    public class TournamentEntry
    {
        public string playerId;
        public string playerName;
        public DateTime registrationTime;
        public List<string> enteredCreatures = new();
        public TournamentEntryStatus status;
        public int currentRound = 0;
        public float currentScore = 0f;
        public Dictionary<string, object> entryData = new();
    }

    [Serializable]
    public class TournamentRules
    {
        public int maxCreaturesPerPlayer = 3;
        public List<string> allowedSpecies = new();
        public List<string> bannedTraits = new();
        public float minGenerationCount = 0f;
        public float maxGenerationCount = 100f;
        public bool allowMutations = true;
        public bool requireOriginalBreeding = false;
        public Dictionary<string, object> customRules = new();
    }

    [Serializable]
    public class TournamentRound
    {
        public int roundNumber;
        public string roundName;
        public DateTime startTime;
        public DateTime endTime;
        public List<TournamentMatch> matches = new();
        public TournamentRoundStatus status;
    }

    [Serializable]
    public class TournamentMatch
    {
        public string matchId;
        public List<string> participantIds = new();
        public string winnerId;
        public float[] scores;
        public MatchStatus status;
        public DateTime startTime;
        public DateTime endTime;
    }

    [Serializable]
    public class TournamentRewards
    {
        public List<TournamentReward> rewards = new();
        public bool distributeAutomatically = true;
        public DateTime distributionTime;
    }

    [Serializable]
    public class TournamentReward
    {
        public int placement; // 1st, 2nd, 3rd, etc.
        public string rewardType;
        public string rewardId;
        public int quantity;
        public string specialTitle;
        public Dictionary<string, object> rewardData = new();
    }

    public enum TournamentType
    {
        BreedingCompetition,
        BeautyContest,
        CombatTournament,
        ResearchChallenge,
        SpeedBreeding,
        ConservationChallenge,
        CreativityContest
    }

    public enum TournamentStatus
    {
        Planned,
        RegistrationOpen,
        RegistrationClosed,
        InProgress,
        Completed,
        Cancelled
    }

    public enum TournamentEntryStatus
    {
        Registered,
        Confirmed,
        Active,
        Eliminated,
        Withdrawn,
        Disqualified
    }

    public enum TournamentRoundStatus
    {
        Pending,
        InProgress,
        Completed
    }

    public enum MatchStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Forfeit
    }

    public enum TournamentEventType
    {
        Created,
        RegistrationOpened,
        RegistrationClosed,
        Started,
        RoundCompleted,
        Completed,
        Cancelled
    }

    #endregion

    #region Community Events

    [Serializable]
    public class CommunityEvent
    {
        public CommunityEventData eventData;
        public DateTime timestamp;
    }

    [Serializable]
    public class CommunityEventData
    {
        public string eventId;
        public CommunityEventType eventType;
        public string title;
        public string description;
        public DateTime startTime;
        public TimeSpan duration;
        public bool isActive;
        public CommunityEventGoal goal;
        public CommunityEventProgress progress;
        public List<CommunityParticipant> participants = new();
        public Dictionary<string, object> eventData = new();
    }

    [Serializable]
    public class CommunityEventGoal
    {
        public string goalType;
        public float targetValue;
        public string targetDescription;
        public CommunityRewards rewards;
        public bool isCompleted = false;
    }

    [Serializable]
    public class CommunityEventProgress
    {
        public float currentValue;
        public float progressPercentage;
        public int contributingPlayers;
        public DateTime lastUpdate;
    }

    [Serializable]
    public class CommunityParticipant
    {
        public string playerId;
        public float contribution;
        public DateTime joinTime;
        public DateTime lastActivity;
        public Dictionary<string, object> participantData = new();
    }

    [Serializable]
    public class CommunityRewards
    {
        public List<CommunityReward> individualRewards = new();
        public List<CommunityReward> communityRewards = new();
        public float participationThreshold = 0.1f;
    }

    [Serializable]
    public class CommunityReward
    {
        public string rewardType;
        public string rewardId;
        public int quantity;
        public float bonusMultiplier = 1f;
        public Dictionary<string, object> rewardData = new();
    }

    public enum CommunityEventType
    {
        MassDiscovery,
        ConservationCrisis,
        EnvironmentalChallenge,
        ResearchBreakthrough,
        SeasonalCelebration,
        PopulationRecovery,
        GeneticPreservation,
        EcosystemRestoration
    }

    #endregion

    #region Player Event History

    [Serializable]
    public class PlayerEventHistory
    {
        public string playerId;
        public int totalCelebrations;
        public int worldFirstDiscoveries;
        public DateTime lastCelebration;
        public List<CelebrationHistoryEntry> celebrationHistory = new();
        public List<TournamentHistoryEntry> tournamentHistory = new();
        public List<SeasonalParticipation> seasonalParticipation = new();
        public PlayerEventStats stats = new();
    }

    [Serializable]
    public class CelebrationHistoryEntry
    {
        public string celebrationId;
        public CelebrationType celebrationType;
        public DateTime timestamp;
        public bool isWorldFirst;
        public float significanceScore;
    }

    [Serializable]
    public class TournamentHistoryEntry
    {
        public string tournamentId;
        public TournamentType tournamentType;
        public DateTime participationDate;
        public int finalPlacement;
        public float finalScore;
        public bool wasWinner;
    }

    [Serializable]
    public class SeasonalParticipation
    {
        public string eventId;
        public SeasonalEventType eventType;
        public DateTime participationDate;
        public float contribution;
        public List<string> rewardsReceived = new();
    }

    [Serializable]
    public class PlayerEventStats
    {
        public float averageCelebrationSignificance;
        public int consecutiveWorldFirsts;
        public int tournamentWins;
        public float communityContributionScore;
        public int seasonalEventsParticipated;
        public DateTime mostRecentAchievement;
    }

    #endregion

    #region Service Interfaces

    /// <summary>
    /// Discovery celebration service interface
    /// </summary>
    public interface IDiscoveryCelebrationService
    {
        Task<bool> InitializeAsync();
        Task<CelebrationData> CreateCelebrationAsync(DiscoveryData discovery, string playerId, bool isWorldFirst);
        List<CelebrationTemplate> GetAvailableTemplates();
        bool IsOnCooldown(string playerId, CelebrationType celebrationType);
        void SetCelebrationCooldown(string playerId, CelebrationType celebrationType, TimeSpan cooldown);
    }

    /// <summary>
    /// Seasonal event service interface
    /// </summary>
    public interface ISeasonalEventService
    {
        Task<bool> InitializeAsync();
        Task<bool> StartEventAsync(string eventId, bool forceStart = false);
        List<SeasonalEventData> GetActiveEvents();
        Task CheckAndActivateSeasonalEvents(DateTime currentTime);
        void CheckSeasonalTriggers(DateTime currentTime);
        Task UpdateSeasonalEvents();
    }

    /// <summary>
    /// Tournament service interface
    /// </summary>
    public interface ITournamentService
    {
        Task<bool> InitializeAsync();
        Task<Tournament> CreateTournamentAsync(TournamentRequest request, string organizerId);
        Task<bool> JoinTournamentAsync(string tournamentId, string playerId, TournamentEntry entry);
        List<Tournament> GetActiveTournaments();
        Tournament GetTournament(string tournamentId);
        Task UpdateTournaments();
        Task<bool> StartTournament(string tournamentId);
    }

    /// <summary>
    /// Community event service interface
    /// </summary>
    public interface ICommunityEventService
    {
        Task<bool> InitializeAsync();
        Task<bool> TriggerEventAsync(CommunityEventData eventData);
        List<CommunityEventData> GetActiveEvents();
        Task<bool> ParticipateInEvent(string eventId, string playerId, float contribution);
        CommunityEventProgress GetEventProgress(string eventId);
        Task UpdateCommunityEvents();
    }

    #endregion

    #region Discovery Integration

    public enum DiscoveryType
    {
        NewTrait,
        NewSpecies,
        Mutation,
        BreedingSuccess,
        Research,
        Other
    }

    [Serializable]
    public class DiscoveryData
    {
        public DiscoveryType discoveryType;
        public string title;
        public string description;
        public DateTime timestamp;
        public bool isSignificant;
        public Dictionary<string, object> data = new();
    }

    #endregion
}