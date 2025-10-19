using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Laboratory.Core.MonsterTown;
using Laboratory.Core.Equipment;
using Laboratory.Core.Events;
using EquipmentItem = Laboratory.Core.MonsterTown.Equipment;

namespace Laboratory.Core.Social
{
    /// <summary>
    /// Social Features Manager - Handles friend system, tournaments, trading, and collaboration
    ///
    /// Key Features:
    /// - Friend system with monster trading and breeding collaboration
    /// - Tournament competitions with leaderboards
    /// - Community breeding pools for rare genetics access
    /// - Player-to-player equipment trading
    /// - Social achievements and recognition system
    /// - Cross-platform social features
    /// </summary>
    public class SocialFeaturesManager : MonoBehaviour
    {
        [Header("🤝 Social System Configuration")]
        [SerializeField] private SocialConfig socialConfig;
        [SerializeField] private bool enableMultiplayerFeatures = true;
        [SerializeField] private bool enableTournaments = true;
        [SerializeField] private bool enableTrading = true;
        [SerializeField] private int maxFriends = 50;

        [Header("🏆 Tournament Settings")]
        [SerializeField] private float tournamentDuration = 3600f; // 1 hour
        [SerializeField] private int maxTournamentParticipants = 100;
        [SerializeField] private TournamentRewards defaultTournamentRewards;

        [Header("💱 Trading System")]
        [SerializeField] private float tradingFeePercentage = 0.05f;
        [SerializeField] private int maxActiveTradeOffers = 10;
        [SerializeField] private float tradeOfferDuration = 86400f; // 24 hours

        // Social state
        private Dictionary<string, PlayerProfile> _playerProfiles = new();
        private Dictionary<string, List<string>> _friendships = new();
        private Dictionary<string, Tournament> _activeTournaments = new();
        private Dictionary<string, TradeOffer> _activeTradeOffers = new();
        private Dictionary<string, List<SocialAchievement>> _socialAchievements = new();
        private List<CommunityBreedingPool> _breedingPools = new();

        // Events
        public event Action<string, string> OnFriendshipFormed;
        public event Action<Tournament> OnTournamentStarted;
        public event Action<Tournament> OnTournamentEnded;
        public event Action<TradeOffer> OnTradeOfferCreated;
        public event Action<TradeOffer> OnTradeCompleted;
        public event Action<SocialAchievement> OnSocialAchievementUnlocked;

        #region Initialization

        public void InitializeSocialFeatures(SocialConfig config)
        {
            socialConfig = config;
            LoadPlayerProfiles();
            InitializeTournamentSystem();
            InitializeTradingSystem();
            InitializeBreedingPools();

            Debug.Log("🤝 Social Features Manager initialized");
        }

        private void LoadPlayerProfiles()
        {
            // Load existing player profiles from save data
            // For now, create a demo profile
            var demoProfile = new PlayerProfile
            {
                PlayerId = "LocalPlayer",
                PlayerName = "Demo Player",
                JoinedDate = DateTime.UtcNow,
                SocialRating = 1000,
                TournamentWins = 0,
                SuccessfulTrades = 0
            };

            _playerProfiles["LocalPlayer"] = demoProfile;
        }

        private void InitializeTournamentSystem()
        {
            if (!enableTournaments) return;

            // Create demo tournaments for each activity type
            var activityTypes = Enum.GetValues(typeof(ActivityType)).Cast<ActivityType>().ToArray();
            foreach (var activityType in activityTypes.Take(3)) // Start with 3 tournaments
            {
                CreateDemoTournament(activityType);
            }

            InvokeRepeating(nameof(UpdateTournaments), 60f, 60f); // Update every minute
        }

        private void InitializeTradingSystem()
        {
            if (!enableTrading) return;

            InvokeRepeating(nameof(UpdateTradeOffers), 30f, 30f); // Update every 30 seconds
        }

        private void InitializeBreedingPools()
        {
            // Create community breeding pools for rare genetics
            var rarePools = new[]
            {
                new CommunityBreedingPool
                {
                    PoolId = "legendary_genetics",
                    Name = "Legendary Genetics Pool",
                    Description = "Share and access rare legendary monster genetics",
                    RequiredSocialRating = 1500,
                    AccessCost = new TownResources { gems = 50 }
                },
                new CommunityBreedingPool
                {
                    PoolId = "speed_specialists",
                    Name = "Speed Specialist Pool",
                    Description = "High-speed genetics for racing excellence",
                    RequiredSocialRating = 1200,
                    AccessCost = new TownResources { coins = 1000 }
                }
            };

            _breedingPools.AddRange(rarePools);
        }

        #endregion

        #region Friend System

        /// <summary>
        /// Send a friend request to another player
        /// </summary>
        public async UniTask<bool> SendFriendRequest(string targetPlayerId, string message = "")
        {
            if (!_playerProfiles.ContainsKey(targetPlayerId))
            {
                Debug.LogWarning($"Player {targetPlayerId} not found");
                return false;
            }

            var friendRequest = new FriendRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                FromPlayerId = "LocalPlayer",
                ToPlayerId = targetPlayerId,
                Message = message,
                SentAt = DateTime.UtcNow,
                Status = FriendRequestStatus.Pending
            };

            // In a real implementation, this would send via network
            await SimulateFriendRequestResponse(friendRequest);

            Debug.Log($"🤝 Friend request sent to {targetPlayerId}");
            return true;
        }

        /// <summary>
        /// Accept a friend request
        /// </summary>
        public bool AcceptFriendRequest(string requestId)
        {
            // Simulate accepting a friend request
            var friendship = CreateFriendship("LocalPlayer", "DemoFriend");
            return friendship;
        }

        /// <summary>
        /// Create a friendship between two players
        /// </summary>
        private bool CreateFriendship(string player1Id, string player2Id)
        {
            if (!_friendships.ContainsKey(player1Id))
                _friendships[player1Id] = new List<string>();
            if (!_friendships.ContainsKey(player2Id))
                _friendships[player2Id] = new List<string>();

            if (_friendships[player1Id].Contains(player2Id))
                return false; // Already friends

            _friendships[player1Id].Add(player2Id);
            _friendships[player2Id].Add(player1Id);

            OnFriendshipFormed?.Invoke(player1Id, player2Id);

            // Award social achievement
            CheckSocialAchievements(player1Id);
            CheckSocialAchievements(player2Id);

            Debug.Log($"🤝 Friendship created between {player1Id} and {player2Id}");
            return true;
        }

        /// <summary>
        /// Get a player's friend list
        /// </summary>
        public List<string> GetFriendList(string playerId)
        {
            return _friendships.TryGetValue(playerId, out var friends) ? new List<string>(friends) : new List<string>();
        }

        private async UniTask SimulateFriendRequestResponse(FriendRequest request)
        {
            await UniTask.Delay(1000); // Simulate network delay

            // 80% chance of acceptance in demo
            if (UnityEngine.Random.value < 0.8f)
            {
                CreateFriendship(request.FromPlayerId, request.ToPlayerId);
            }
        }

        #endregion

        #region Tournament System

        /// <summary>
        /// Create a new tournament
        /// </summary>
        public Tournament CreateTournament(ActivityType activityType, string name, TournamentRewards rewards)
        {
            var tournament = new Tournament
            {
                TournamentId = Guid.NewGuid().ToString(),
                Name = name,
                ActivityType = activityType,
                StartTime = DateTime.UtcNow.AddMinutes(5), // Start in 5 minutes
                EndTime = DateTime.UtcNow.AddSeconds(tournamentDuration),
                MaxParticipants = maxTournamentParticipants,
                Rewards = rewards,
                Status = TournamentStatus.Registration
            };

            _activeTournaments[tournament.TournamentId] = tournament;
            OnTournamentStarted?.Invoke(tournament);

            Debug.Log($"🏆 Tournament created: {name} ({activityType})");
            return tournament;
        }

        /// <summary>
        /// Join a tournament
        /// </summary>
        public async UniTask<bool> JoinTournament(string tournamentId, Monster monster)
        {
            if (!_activeTournaments.TryGetValue(tournamentId, out var tournament))
            {
                Debug.LogWarning($"Tournament {tournamentId} not found");
                return false;
            }

            if (tournament.Status != TournamentStatus.Registration)
            {
                Debug.LogWarning($"Tournament {tournamentId} is not accepting registrations");
                return false;
            }

            if (tournament.Participants.Count >= tournament.MaxParticipants)
            {
                Debug.LogWarning($"Tournament {tournamentId} is full");
                return false;
            }

            var participant = new TournamentParticipant
            {
                PlayerId = "LocalPlayer",
                Monster = monster,
                JoinedAt = DateTime.UtcNow,
                Score = 0f
            };

            tournament.Participants.Add(participant);

            Debug.Log($"🏆 Joined tournament: {tournament.Name} with {monster.Name}");
            return true;
        }

        /// <summary>
        /// Run tournament activities and calculate results
        /// </summary>
        public async UniTask<TournamentResults> RunTournamentActivity(string tournamentId, Monster monster)
        {
            if (!_activeTournaments.TryGetValue(tournamentId, out var tournament))
                return null;

            // Calculate monster performance for tournament activity
            var performance = CalculateTournamentPerformance(monster, tournament.ActivityType);
            var score = performance.CalculateTotal() * UnityEngine.Random.Range(0.8f, 1.2f); // Add some RNG

            // Update participant score
            var participant = tournament.Participants.FirstOrDefault(p => p.PlayerId == "LocalPlayer");
            if (participant != null)
            {
                participant.Score = Mathf.Max(participant.Score, score);
            }

            // Create tournament results
            var results = new TournamentResults
            {
                TournamentId = tournamentId,
                ParticipantId = "LocalPlayer",
                Score = score,
                Ranking = CalculateRanking(tournament, score),
                RewardsEarned = CalculateTournamentRewards(tournament, score)
            };

            await UniTask.Delay(100); // Simulate processing time

            Debug.Log($"🏆 Tournament result: Score {score:F2}, Rank {results.Ranking}");
            return results;
        }

        private void UpdateTournaments()
        {
            var currentTime = DateTime.UtcNow;
            var tournamentsToEnd = new List<string>();

            foreach (var tournament in _activeTournaments.Values)
            {
                // Start tournaments that are ready
                if (tournament.Status == TournamentStatus.Registration && currentTime >= tournament.StartTime)
                {
                    tournament.Status = TournamentStatus.Active;
                    Debug.Log($"🏆 Tournament started: {tournament.Name}");
                }

                // End tournaments that have expired
                if (tournament.Status == TournamentStatus.Active && currentTime >= tournament.EndTime)
                {
                    tournament.Status = TournamentStatus.Completed;
                    FinalizeTournament(tournament);
                    tournamentsToEnd.Add(tournament.TournamentId);
                }
            }

            // Remove completed tournaments
            foreach (var tournamentId in tournamentsToEnd)
            {
                _activeTournaments.Remove(tournamentId);
            }
        }

        private void FinalizeTournament(Tournament tournament)
        {
            // Sort participants by score
            tournament.Participants = tournament.Participants.OrderByDescending(p => p.Score).ToList();

            OnTournamentEnded?.Invoke(tournament);

            Debug.Log($"🏆 Tournament completed: {tournament.Name} with {tournament.Participants.Count} participants");
        }

        private void CreateDemoTournament(ActivityType activityType)
        {
            var rewards = new TournamentRewards
            {
                FirstPlace = new TownResources { coins = 1000, gems = 50 },
                SecondPlace = new TownResources { coins = 500, gems = 25 },
                ThirdPlace = new TownResources { coins = 250, gems = 10 },
                ParticipationReward = new TownResources { coins = 50 }
            };

            CreateTournament(activityType, $"Weekly {activityType} Championship", rewards);
        }

        #endregion

        #region Trading System

        /// <summary>
        /// Create a trade offer
        /// </summary>
        public bool CreateTradeOffer(TradeOfferData offerData)
        {
            if (_activeTradeOffers.Count(kvp => kvp.Value.OffererId == "LocalPlayer") >= maxActiveTradeOffers)
            {
                Debug.LogWarning("Maximum active trade offers reached");
                return false;
            }

            var tradeOffer = new TradeOffer
            {
                OfferId = Guid.NewGuid().ToString(),
                OffererId = "LocalPlayer",
                OfferedItems = offerData.OfferedItems,
                RequestedItems = offerData.RequestedItems,
                OfferedCurrency = offerData.OfferedCurrency,
                RequestedCurrency = offerData.RequestedCurrency,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tradeOfferDuration),
                Status = TradeOfferStatus.Active
            };

            _activeTradeOffers[tradeOffer.OfferId] = tradeOffer;
            OnTradeOfferCreated?.Invoke(tradeOffer);

            Debug.Log($"💱 Trade offer created: {tradeOffer.OfferId}");
            return true;
        }

        /// <summary>
        /// Accept a trade offer
        /// </summary>
        public async UniTask<bool> AcceptTradeOffer(string offerId, string acceptingPlayerId)
        {
            if (!_activeTradeOffers.TryGetValue(offerId, out var tradeOffer))
            {
                Debug.LogWarning($"Trade offer {offerId} not found");
                return false;
            }

            if (tradeOffer.Status != TradeOfferStatus.Active)
            {
                Debug.LogWarning($"Trade offer {offerId} is not active");
                return false;
            }

            // Process the trade
            var success = await ProcessTrade(tradeOffer, acceptingPlayerId);

            if (success)
            {
                tradeOffer.Status = TradeOfferStatus.Completed;
                tradeOffer.AccepterId = acceptingPlayerId;
                tradeOffer.CompletedAt = DateTime.UtcNow;

                OnTradeCompleted?.Invoke(tradeOffer);

                // Update social ratings
                UpdateSocialRating(tradeOffer.OffererId, 10);
                UpdateSocialRating(acceptingPlayerId, 10);

                Debug.Log($"💱 Trade completed: {offerId}");
            }

            return success;
        }

        /// <summary>
        /// Get active trade offers
        /// </summary>
        public List<TradeOffer> GetActiveTradeOffers(string category = null)
        {
            var offers = _activeTradeOffers.Values.Where(o => o.Status == TradeOfferStatus.Active).ToList();

            if (!string.IsNullOrEmpty(category))
            {
                offers = offers.Where(o => o.OfferedItems.Any(item => item.Category == category)).ToList();
            }

            return offers.OrderBy(o => o.CreatedAt).ToList();
        }

        private async UniTask<bool> ProcessTrade(TradeOffer tradeOffer, string acceptingPlayerId)
        {
            // Calculate trading fee
            var fee = tradeOffer.OfferedCurrency * tradingFeePercentage;

            // In a real implementation, this would:
            // 1. Verify both players have the required items/currency
            // 2. Transfer items between inventories
            // 3. Deduct trading fees
            // 4. Update player profiles

            await UniTask.Delay(500); // Simulate processing time

            Debug.Log($"💱 Processing trade between {tradeOffer.OffererId} and {acceptingPlayerId}");
            return true; // Demo always succeeds
        }

        private void UpdateTradeOffers()
        {
            var currentTime = DateTime.UtcNow;
            var expiredOffers = _activeTradeOffers.Values
                .Where(o => o.Status == TradeOfferStatus.Active && currentTime >= o.ExpiresAt)
                .ToList();

            foreach (var offer in expiredOffers)
            {
                offer.Status = TradeOfferStatus.Expired;
                Debug.Log($"💱 Trade offer expired: {offer.OfferId}");
            }
        }

        #endregion

        #region Community Breeding

        /// <summary>
        /// Access a community breeding pool
        /// </summary>
        public async UniTask<List<GeneticProfile>> AccessBreedingPool(string poolId, string playerId)
        {
            var pool = _breedingPools.FirstOrDefault(p => p.PoolId == poolId);
            if (pool == null)
            {
                Debug.LogWarning($"Breeding pool {poolId} not found");
                return new List<GeneticProfile>();
            }

            var playerProfile = _playerProfiles[playerId];
            if (playerProfile.SocialRating < pool.RequiredSocialRating)
            {
                Debug.LogWarning($"Insufficient social rating for breeding pool {poolId}");
                return new List<GeneticProfile>();
            }

            // Simulate accessing rare genetics
            var availableGenetics = new List<GeneticProfile>();
            for (int i = 0; i < 5; i++)
            {
                availableGenetics.Add(GenerateRareGeneticProfile(pool.PoolId));
            }

            await UniTask.Delay(100);

            Debug.Log($"🧬 Accessed breeding pool: {pool.Name} with {availableGenetics.Count} genetics");
            return availableGenetics;
        }

        /// <summary>
        /// Contribute genetics to a community pool
        /// </summary>
        public bool ContributeToBreedingPool(string poolId, GeneticProfile genetics, string playerId)
        {
            var pool = _breedingPools.FirstOrDefault(p => p.PoolId == poolId);
            if (pool == null) return false;

            // Add contribution to pool
            pool.ContributedGenetics.Add(new PoolContribution
            {
                ContributorId = playerId,
                Genetics = genetics,
                ContributedAt = DateTime.UtcNow
            });

            // Reward contributor
            UpdateSocialRating(playerId, 25);

            Debug.Log($"🧬 Contributed genetics to breeding pool: {pool.Name}");
            return true;
        }

        private GeneticProfile GenerateRareGeneticProfile(string poolType)
        {
            // This would integrate with the existing genetic system
            // For now, return a placeholder
            return new GeneticProfile
            {
                Id = Guid.NewGuid().ToString(),
                Source = $"Community Pool: {poolType}",
                Rarity = GeneticRarity.Legendary
            };
        }

        #endregion

        #region Social Achievements

        private void CheckSocialAchievements(string playerId)
        {
            if (!_socialAchievements.ContainsKey(playerId))
                _socialAchievements[playerId] = new List<SocialAchievement>();

            var achievements = _socialAchievements[playerId];
            var friendCount = GetFriendList(playerId).Count;
            var profile = _playerProfiles[playerId];

            // Check first friend achievement
            if (friendCount >= 1 && !achievements.Any(a => a.AchievementId == "first_friend"))
            {
                UnlockSocialAchievement(playerId, "first_friend", "Made your first friend!", "Social butterfly begins");
            }

            // Check popular achievement
            if (friendCount >= 10 && !achievements.Any(a => a.AchievementId == "popular"))
            {
                UnlockSocialAchievement(playerId, "popular", "Popular Monster Trainer", "Made 10 friends");
            }

            // Check trading achievements
            if (profile.SuccessfulTrades >= 1 && !achievements.Any(a => a.AchievementId == "first_trade"))
            {
                UnlockSocialAchievement(playerId, "first_trade", "First Trade", "Completed your first trade");
            }
        }

        private void UnlockSocialAchievement(string playerId, string achievementId, string title, string description)
        {
            var achievement = new SocialAchievement
            {
                AchievementId = achievementId,
                Title = title,
                Description = description,
                UnlockedAt = DateTime.UtcNow,
                Rewards = new TownResources { coins = 100 }
            };

            _socialAchievements[playerId].Add(achievement);
            OnSocialAchievementUnlocked?.Invoke(achievement);

            Debug.Log($"🏆 Social achievement unlocked: {title}");
        }

        #endregion

        #region Utility Methods

        private MonsterPerformance CalculateTournamentPerformance(Monster monster, ActivityType activityType)
        {
            // Calculate base performance for tournament
            var performance = new MonsterPerformance
            {
                basePerformance = 0.5f + (monster.Stats.GetAverageStats() / 200f),
                geneticBonus = CalculateGeneticBonus(monster, activityType),
                equipmentBonus = CalculateEquipmentBonus(monster, activityType),
                experienceBonus = monster.GetActivityExperience(activityType) * 0.001f,
                happinessModifier = (monster.Happiness - 0.5f) * 0.2f
            };

            return performance;
        }

        private float CalculateGeneticBonus(Monster monster, ActivityType activityType)
        {
            // Simplified genetic bonus calculation
            return UnityEngine.Random.Range(0f, 0.2f);
        }

        private float CalculateEquipmentBonus(Monster monster, ActivityType activityType)
        {
            // Simplified equipment bonus calculation
            return monster.Equipment.Count * 0.05f;
        }

        private int CalculateRanking(Tournament tournament, float score)
        {
            var higherScores = tournament.Participants.Count(p => p.Score > score);
            return higherScores + 1;
        }

        private TownResources CalculateTournamentRewards(Tournament tournament, float score)
        {
            var ranking = CalculateRanking(tournament, score);

            return ranking switch
            {
                1 => tournament.Rewards.FirstPlace,
                2 => tournament.Rewards.SecondPlace,
                3 => tournament.Rewards.ThirdPlace,
                _ => tournament.Rewards.ParticipationReward
            };
        }

        private void UpdateSocialRating(string playerId, int points)
        {
            if (_playerProfiles.TryGetValue(playerId, out var profile))
            {
                profile.SocialRating += points;
                Debug.Log($"🌟 Social rating updated: {playerId} (+{points}) = {profile.SocialRating}");
            }
        }

        /// <summary>
        /// Get social statistics for UI display
        /// </summary>
        public SocialStatistics GetSocialStatistics(string playerId)
        {
            var profile = _playerProfiles.TryGetValue(playerId, out var p) ? p : new PlayerProfile();
            var friendCount = GetFriendList(playerId).Count;
            var achievements = _socialAchievements.TryGetValue(playerId, out var a) ? a.Count : 0;

            return new SocialStatistics
            {
                FriendCount = friendCount,
                SocialRating = profile.SocialRating,
                TournamentWins = profile.TournamentWins,
                SuccessfulTrades = profile.SuccessfulTrades,
                AchievementsUnlocked = achievements
            };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Social system configuration
    /// </summary>
    [Serializable]
    public class SocialConfig
    {
        [Header("Friend System")]
        public int maxFriends = 50;
        public bool allowFriendRequests = true;
        public bool enableCrossPlayFriends = true;

        [Header("Tournament System")]
        public bool enableTournaments = true;
        public float defaultTournamentDuration = 3600f;
        public int maxTournamentParticipants = 100;

        [Header("Trading System")]
        public bool enableTrading = true;
        public float tradingFeePercentage = 0.05f;
        public float tradeOfferDuration = 86400f;
    }

    /// <summary>
    /// Player profile data
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string PlayerId;
        public string PlayerName;
        public DateTime JoinedDate;
        public int SocialRating;
        public int TournamentWins;
        public int SuccessfulTrades;
        public List<string> Badges = new();
    }

    /// <summary>
    /// Friend request data
    /// </summary>
    [Serializable]
    public class FriendRequest
    {
        public string RequestId;
        public string FromPlayerId;
        public string ToPlayerId;
        public string Message;
        public DateTime SentAt;
        public FriendRequestStatus Status;
    }

    /// <summary>
    /// Tournament data
    /// </summary>
    [Serializable]
    public class Tournament
    {
        public string TournamentId;
        public string Name;
        public ActivityType ActivityType;
        public DateTime StartTime;
        public DateTime EndTime;
        public int MaxParticipants;
        public TournamentRewards Rewards;
        public TournamentStatus Status;
        public List<TournamentParticipant> Participants = new();
    }

    /// <summary>
    /// Tournament participant
    /// </summary>
    [Serializable]
    public class TournamentParticipant
    {
        public string PlayerId;
        public Monster Monster;
        public DateTime JoinedAt;
        public float Score;
    }

    /// <summary>
    /// Tournament rewards structure
    /// </summary>
    [Serializable]
    public class TournamentRewards
    {
        public TownResources FirstPlace;
        public TownResources SecondPlace;
        public TownResources ThirdPlace;
        public TownResources ParticipationReward;
    }

    /// <summary>
    /// Tournament results
    /// </summary>
    [Serializable]
    public class TournamentResults
    {
        public string TournamentId;
        public string ParticipantId;
        public float Score;
        public int Ranking;
        public TownResources RewardsEarned;
    }

    /// <summary>
    /// Trade offer data
    /// </summary>
    [Serializable]
    public class TradeOffer
    {
        public string OfferId;
        public string OffererId;
        public string AccepterId;
        public List<EquipmentItem> OfferedItems = new();
        public List<EquipmentItem> RequestedItems = new();
        public TownResources OfferedCurrency;
        public TownResources RequestedCurrency;
        public DateTime CreatedAt;
        public DateTime ExpiresAt;
        public DateTime CompletedAt;
        public TradeOfferStatus Status;
    }

    /// <summary>
    /// Trade offer creation data
    /// </summary>
    [Serializable]
    public class TradeOfferData
    {
        public List<EquipmentItem> OfferedItems = new();
        public List<EquipmentItem> RequestedItems = new();
        public TownResources OfferedCurrency;
        public TownResources RequestedCurrency;
    }

    /// <summary>
    /// Community breeding pool
    /// </summary>
    [Serializable]
    public class CommunityBreedingPool
    {
        public string PoolId;
        public string Name;
        public string Description;
        public int RequiredSocialRating;
        public TownResources AccessCost;
        public List<PoolContribution> ContributedGenetics = new();
    }

    /// <summary>
    /// Breeding pool contribution
    /// </summary>
    [Serializable]
    public class PoolContribution
    {
        public string ContributorId;
        public GeneticProfile Genetics;
        public DateTime ContributedAt;
    }

    /// <summary>
    /// Social achievement
    /// </summary>
    [Serializable]
    public class SocialAchievement
    {
        public string AchievementId;
        public string Title;
        public string Description;
        public DateTime UnlockedAt;
        public TownResources Rewards;
    }

    /// <summary>
    /// Social statistics for UI
    /// </summary>
    [Serializable]
    public struct SocialStatistics
    {
        public int FriendCount;
        public int SocialRating;
        public int TournamentWins;
        public int SuccessfulTrades;
        public int AchievementsUnlocked;
    }

    /// <summary>
    /// Genetic profile for breeding pools
    /// </summary>
    [Serializable]
    public class GeneticProfile
    {
        public string Id;
        public string Source;
        public GeneticRarity Rarity;
        public Dictionary<string, float> Traits = new();
    }

    /// <summary>
    /// Status enums
    /// </summary>
    public enum FriendRequestStatus { Pending, Accepted, Declined, Expired }
    public enum TournamentStatus { Registration, Active, Completed, Cancelled }
    public enum TradeOfferStatus { Active, Completed, Cancelled, Expired }
    public enum GeneticRarity { Common, Uncommon, Rare, Epic, Legendary }

    #endregion
}