using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;

namespace Laboratory.Core.MonsterTown.Systems
{
    /// <summary>
    /// Multiplayer Manager - handles multiplayer features for Monster Town
    /// Manages friend systems, trading, and collaborative features
    /// </summary>
    public class MultiplayerManager : MonoBehaviour
    {
        [Header("Multiplayer Configuration")]
        [SerializeField] private bool enableMultiplayer = true;
        [SerializeField] private int maxFriends = 50;
        [SerializeField] private bool enableTrading = true;
        [SerializeField] private bool enableVisiting = true;

        [Header("Trading")]
        [SerializeField] private float tradingCooldownHours = 24f;
        [SerializeField] private int maxActiveTradesPerPlayer = 5;

        // System dependencies
        private IEventBus eventBus;

        // Multiplayer state
        private Dictionary<string, PlayerInfo> connectedPlayers = new();
        private Dictionary<string, List<string>> friendLists = new();
        private Dictionary<string, List<TradeOffer>> activeTradeOffers = new();
        private Dictionary<string, DateTime> lastTradeTime = new();

        #region Unity Lifecycle

        private void Awake()
        {
            eventBus = ServiceContainer.Instance?.ResolveService<IEventBus>();
        }

        private void Start()
        {
            if (enableMultiplayer)
            {
                InitializeMultiplayer();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Add friend to player's friend list
        /// </summary>
        public bool AddFriend(string playerId, string friendId)
        {
            if (!enableMultiplayer) return false;

            if (!friendLists.ContainsKey(playerId))
            {
                friendLists[playerId] = new List<string>();
            }

            var friendList = friendLists[playerId];

            if (friendList.Contains(friendId))
            {
                Debug.LogWarning($"Player {friendId} is already a friend of {playerId}");
                return false;
            }

            if (friendList.Count >= maxFriends)
            {
                Debug.LogWarning($"Friend list full for player {playerId}");
                return false;
            }

            friendList.Add(friendId);

            // Add reciprocal friendship
            if (!friendLists.ContainsKey(friendId))
            {
                friendLists[friendId] = new List<string>();
            }

            if (!friendLists[friendId].Contains(playerId))
            {
                friendLists[friendId].Add(playerId);
            }

            eventBus?.Publish(new FriendAddedEvent(playerId, friendId));

            Debug.Log($"👫 {playerId} and {friendId} are now friends");
            return true;
        }

        /// <summary>
        /// Remove friend from player's friend list
        /// </summary>
        public bool RemoveFriend(string playerId, string friendId)
        {
            if (!enableMultiplayer) return false;

            bool removed = false;

            if (friendLists.TryGetValue(playerId, out var friendList))
            {
                removed = friendList.Remove(friendId);
            }

            if (friendLists.TryGetValue(friendId, out var reciprocalList))
            {
                reciprocalList.Remove(playerId);
            }

            if (removed)
            {
                eventBus?.Publish(new FriendRemovedEvent(playerId, friendId));
                Debug.Log($"💔 {playerId} and {friendId} are no longer friends");
            }

            return removed;
        }

        /// <summary>
        /// Create a trade offer between players
        /// </summary>
        public bool CreateTradeOffer(string fromPlayerId, string toPlayerId, TradeOffer offer)
        {
            if (!enableMultiplayer || !enableTrading) return false;

            // Check trading cooldown
            if (IsOnTradingCooldown(fromPlayerId))
            {
                Debug.LogWarning($"Player {fromPlayerId} is on trading cooldown");
                return false;
            }

            // Check if players are friends
            if (!AreFriends(fromPlayerId, toPlayerId))
            {
                Debug.LogWarning($"Players {fromPlayerId} and {toPlayerId} are not friends");
                return false;
            }

            // Check active trade limit
            if (GetActiveTradeCount(fromPlayerId) >= maxActiveTradesPerPlayer)
            {
                Debug.LogWarning($"Player {fromPlayerId} has reached active trade limit");
                return false;
            }

            if (!activeTradeOffers.ContainsKey(toPlayerId))
            {
                activeTradeOffers[toPlayerId] = new List<TradeOffer>();
            }

            offer.Id = System.Guid.NewGuid().ToString();
            offer.FromPlayerId = fromPlayerId;
            offer.ToPlayerId = toPlayerId;
            offer.CreatedTime = DateTime.Now;
            offer.Status = TradeStatus.Pending;

            activeTradeOffers[toPlayerId].Add(offer);

            eventBus?.Publish(new TradeOfferCreatedEvent(offer));

            Debug.Log($"💱 Trade offer created from {fromPlayerId} to {toPlayerId}");
            return true;
        }

        /// <summary>
        /// Accept a trade offer
        /// </summary>
        public bool AcceptTradeOffer(string playerId, string tradeOfferId)
        {
            if (!enableMultiplayer || !enableTrading) return false;

            var offer = GetTradeOffer(playerId, tradeOfferId);
            if (offer == null)
            {
                Debug.LogWarning($"Trade offer {tradeOfferId} not found");
                return false;
            }

            if (offer.Status != TradeStatus.Pending)
            {
                Debug.LogWarning($"Trade offer {tradeOfferId} is not pending");
                return false;
            }

            // Execute the trade
            if (ExecuteTrade(offer))
            {
                offer.Status = TradeStatus.Completed;
                offer.CompletedTime = DateTime.Now;

                // Record trading time for cooldown
                lastTradeTime[offer.FromPlayerId] = DateTime.Now;
                lastTradeTime[offer.ToPlayerId] = DateTime.Now;

                eventBus?.Publish(new TradeCompletedEvent(offer));

                Debug.Log($"✅ Trade completed between {offer.FromPlayerId} and {offer.ToPlayerId}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reject a trade offer
        /// </summary>
        public bool RejectTradeOffer(string playerId, string tradeOfferId)
        {
            if (!enableMultiplayer || !enableTrading) return false;

            var offer = GetTradeOffer(playerId, tradeOfferId);
            if (offer == null) return false;

            offer.Status = TradeStatus.Rejected;
            eventBus?.Publish(new TradeRejectedEvent(offer));

            Debug.Log($"❌ Trade offer rejected by {playerId}");
            return true;
        }

        /// <summary>
        /// Visit another player's town
        /// </summary>
        public bool VisitPlayerTown(string visitorId, string hostId)
        {
            if (!enableMultiplayer || !enableVisiting) return false;

            if (!AreFriends(visitorId, hostId))
            {
                Debug.LogWarning($"Player {visitorId} cannot visit {hostId} - not friends");
                return false;
            }

            eventBus?.Publish(new PlayerTownVisitEvent(visitorId, hostId));

            Debug.Log($"🏠 {visitorId} is visiting {hostId}'s town");
            return true;
        }

        /// <summary>
        /// Get friend list for player
        /// </summary>
        public List<string> GetFriends(string playerId)
        {
            return friendLists.TryGetValue(playerId, out var friends)
                ? new List<string>(friends)
                : new List<string>();
        }

        /// <summary>
        /// Get pending trade offers for player
        /// </summary>
        public List<TradeOffer> GetPendingTradeOffers(string playerId)
        {
            if (!activeTradeOffers.TryGetValue(playerId, out var offers))
                return new List<TradeOffer>();

            return offers.Where(o => o.Status == TradeStatus.Pending).ToList();
        }

        /// <summary>
        /// Check if two players are friends
        /// </summary>
        public bool AreFriends(string playerId1, string playerId2)
        {
            if (!friendLists.TryGetValue(playerId1, out var friends))
                return false;

            return friends.Contains(playerId2);
        }

        /// <summary>
        /// Update multiplayer systems - handles periodic multiplayer maintenance
        /// Called from MonsterTownGameCore Update loop when multiplayer is enabled
        /// </summary>
        public void UpdateMultiplayer()
        {
            if (!enableMultiplayer) return;

            // Clean up expired trade offers
            CleanupExpiredTrades();

            // Update player connection status
            UpdatePlayerConnections();

            // Process any pending multiplayer events
            ProcessPendingEvents();
        }

        #endregion

        #region Private Methods

        private void InitializeMultiplayer()
        {
            Debug.Log("🌐 Multiplayer system initialized");

            // Subscribe to relevant events
            if (eventBus != null)
            {
                eventBus.Subscribe<PlayerConnectedEvent>(OnPlayerConnected);
                eventBus.Subscribe<PlayerDisconnectedEvent>(OnPlayerDisconnected);
            }
        }

        private bool IsOnTradingCooldown(string playerId)
        {
            if (!lastTradeTime.TryGetValue(playerId, out var lastTrade))
                return false;

            var timeSinceLastTrade = DateTime.Now - lastTrade;
            return timeSinceLastTrade.TotalHours < tradingCooldownHours;
        }

        private int GetActiveTradeCount(string playerId)
        {
            int count = 0;

            foreach (var offerList in activeTradeOffers.Values)
            {
                count += offerList.Count(o => o.FromPlayerId == playerId && o.Status == TradeStatus.Pending);
            }

            return count;
        }

        private TradeOffer GetTradeOffer(string playerId, string tradeOfferId)
        {
            if (!activeTradeOffers.TryGetValue(playerId, out var offers))
                return null;

            return offers.FirstOrDefault(o => o.Id == tradeOfferId);
        }

        private bool ExecuteTrade(TradeOffer offer)
        {
            // This would integrate with the actual inventory/resource systems
            // For now, just simulate a successful trade
            Debug.Log($"Executing trade: {offer.FromPlayerId} -> {offer.ToPlayerId}");

            // In a real implementation, this would:
            // 1. Validate that both players have the offered items
            // 2. Remove items from sender's inventory
            // 3. Add items to receiver's inventory
            // 4. Handle any resource transfers

            return true; // Simplified for this implementation
        }

        private void OnPlayerConnected(PlayerConnectedEvent evt)
        {
            connectedPlayers[evt.PlayerId] = new PlayerInfo
            {
                PlayerId = evt.PlayerId,
                ConnectedTime = DateTime.Now,
                IsOnline = true
            };

            Debug.Log($"🟢 Player connected: {evt.PlayerId}");
        }

        private void OnPlayerDisconnected(PlayerDisconnectedEvent evt)
        {
            if (connectedPlayers.TryGetValue(evt.PlayerId, out var playerInfo))
            {
                playerInfo.IsOnline = false;
                playerInfo.LastSeen = DateTime.Now;
            }

            Debug.Log($"🔴 Player disconnected: {evt.PlayerId}");
        }

        private void CleanupExpiredTrades()
        {
            var expiredOffers = new List<TradeOffer>();

            foreach (var playerOffers in activeTradeOffers.Values)
            {
                foreach (var offer in playerOffers)
                {
                    if (offer.Status == TradeStatus.Pending)
                    {
                        // Expire trades older than 24 hours
                        var ageInHours = (DateTime.Now - offer.CreatedTime).TotalHours;
                        if (ageInHours > 24)
                        {
                            offer.Status = TradeStatus.Expired;
                            expiredOffers.Add(offer);
                        }
                    }
                }
            }

            // Publish events for expired trades
            foreach (var expiredOffer in expiredOffers)
            {
                eventBus?.Publish(new TradeExpiredEvent(expiredOffer));
            }

            if (expiredOffers.Count > 0)
            {
                Debug.Log($"🕐 Cleaned up {expiredOffers.Count} expired trade offers");
            }
        }

        private void UpdatePlayerConnections()
        {
            var currentTime = DateTime.Now;
            var disconnectedPlayers = new List<string>();

            foreach (var kvp in connectedPlayers)
            {
                var playerId = kvp.Key;
                var playerInfo = kvp.Value;

                if (playerInfo.IsOnline)
                {
                    // Check for connection timeout (simulate network health check)
                    var connectionDuration = currentTime - playerInfo.ConnectedTime;
                    if (connectionDuration.TotalHours > 12) // Auto-disconnect after 12 hours
                    {
                        playerInfo.IsOnline = false;
                        playerInfo.LastSeen = currentTime;
                        disconnectedPlayers.Add(playerId);
                    }
                }
            }

            // Publish disconnect events for timed-out players
            foreach (var playerId in disconnectedPlayers)
            {
                eventBus?.Publish(new PlayerDisconnectedEvent(playerId));
            }
        }

        private void ProcessPendingEvents()
        {
            // Process any background multiplayer operations
            // This could include:
            // - Syncing friend status updates
            // - Processing delayed multiplayer notifications
            // - Handling background connection maintenance

            // For now, this is a placeholder for future multiplayer event processing
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class PlayerInfo
    {
        public string PlayerId;
        public DateTime ConnectedTime;
        public DateTime LastSeen;
        public bool IsOnline;
    }

    [System.Serializable]
    public class TradeOffer
    {
        public string Id;
        public string FromPlayerId;
        public string ToPlayerId;
        public List<TradeItem> OfferedItems = new();
        public List<TradeItem> RequestedItems = new();
        public TradeStatus Status;
        public DateTime CreatedTime;
        public DateTime CompletedTime;
        public string Message;
    }

    [System.Serializable]
    public class TradeItem
    {
        public string ItemType; // "monster", "resource", "equipment"
        public string ItemId;
        public string ItemName;
        public int Quantity;
        public Dictionary<string, object> Metadata = new();
    }

    public enum TradeStatus
    {
        Pending,
        Accepted,
        Rejected,
        Completed,
        Expired
    }

    // Multiplayer Events
    public class FriendAddedEvent
    {
        public string PlayerId { get; }
        public string FriendId { get; }
        public DateTime Timestamp { get; }

        public FriendAddedEvent(string playerId, string friendId)
        {
            PlayerId = playerId;
            FriendId = friendId;
            Timestamp = DateTime.Now;
        }
    }

    public class FriendRemovedEvent
    {
        public string PlayerId { get; }
        public string FriendId { get; }
        public DateTime Timestamp { get; }

        public FriendRemovedEvent(string playerId, string friendId)
        {
            PlayerId = playerId;
            FriendId = friendId;
            Timestamp = DateTime.Now;
        }
    }

    public class TradeOfferCreatedEvent
    {
        public TradeOffer Offer { get; }
        public DateTime Timestamp { get; }

        public TradeOfferCreatedEvent(TradeOffer offer)
        {
            Offer = offer;
            Timestamp = DateTime.Now;
        }
    }

    public class TradeCompletedEvent
    {
        public TradeOffer Offer { get; }
        public DateTime Timestamp { get; }

        public TradeCompletedEvent(TradeOffer offer)
        {
            Offer = offer;
            Timestamp = DateTime.Now;
        }
    }

    public class TradeRejectedEvent
    {
        public TradeOffer Offer { get; }
        public DateTime Timestamp { get; }

        public TradeRejectedEvent(TradeOffer offer)
        {
            Offer = offer;
            Timestamp = DateTime.Now;
        }
    }

    public class PlayerTownVisitEvent
    {
        public string VisitorId { get; }
        public string HostId { get; }
        public DateTime Timestamp { get; }

        public PlayerTownVisitEvent(string visitorId, string hostId)
        {
            VisitorId = visitorId;
            HostId = hostId;
            Timestamp = DateTime.Now;
        }
    }

    public class PlayerConnectedEvent
    {
        public string PlayerId { get; }
        public DateTime Timestamp { get; }

        public PlayerConnectedEvent(string playerId)
        {
            PlayerId = playerId;
            Timestamp = DateTime.Now;
        }
    }

    public class PlayerDisconnectedEvent
    {
        public string PlayerId { get; }
        public DateTime Timestamp { get; }

        public PlayerDisconnectedEvent(string playerId)
        {
            PlayerId = playerId;
            Timestamp = DateTime.Now;
        }
    }

    public class TradeExpiredEvent
    {
        public TradeOffer Offer { get; }
        public DateTime Timestamp { get; }

        public TradeExpiredEvent(TradeOffer offer)
        {
            Offer = offer;
            Timestamp = DateTime.Now;
        }
    }

    #endregion
}