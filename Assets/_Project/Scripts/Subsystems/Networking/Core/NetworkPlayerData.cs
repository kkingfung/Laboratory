using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using Laboratory.Core.Services;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Network-synchronized player data container managing player information across clients.
    /// Stores player statistics, metadata, and gameplay metrics with server authority.
    /// </summary>
    public class NetworkPlayerData : NetworkBehaviour
    {
        #region Fields

        private IEventBus _eventBus;
        private IAssetService _assetService;

        [Header("Player Identity")]
        /// <summary>Network-synchronized player display name.</summary>
        public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(
            default, 
            NetworkVariableReadPermission.Everyone);

        [Header("Game Statistics")]
        /// <summary>Player's current score in the game session.</summary>
        public NetworkVariable<int> Score = new NetworkVariable<int>(
            0, 
            NetworkVariableReadPermission.Everyone);

        /// <summary>Number of eliminations achieved by this player.</summary>
        public NetworkVariable<int> Kills = new NetworkVariable<int>(
            0, 
            NetworkVariableReadPermission.Everyone);

        /// <summary>Number of times this player has been eliminated.</summary>
        public NetworkVariable<int> Deaths = new NetworkVariable<int>(
            0, 
            NetworkVariableReadPermission.Everyone);

        /// <summary>Number of assists this player has contributed to.</summary>
        public NetworkVariable<int> Assists = new NetworkVariable<int>(
            0, 
            NetworkVariableReadPermission.Everyone);

        [Header("Network Metrics")]
        /// <summary>Current network latency for this player in milliseconds.</summary>
        public NetworkVariable<int> Ping = new NetworkVariable<int>(
            0, 
            NetworkVariableReadPermission.Everyone);

        [Header("Player Customization")]
        /// <summary>Player's avatar/profile picture sprite ID. Use string ID to reference sprite assets.</summary>
        public NetworkVariable<FixedString64Bytes> AvatarId = new NetworkVariable<FixedString64Bytes>(
            default, 
            NetworkVariableReadPermission.Everyone);

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the actual Sprite object from the avatar ID.
        /// Loads sprites from the asset management system using the configured AssetService.
        /// </summary>
        /// <returns>The sprite corresponding to the current avatar ID, or null if not found.</returns>
        public Sprite GetAvatarSprite()
        {
            if (AvatarId.Value.IsEmpty)
                return null;

            // Try to get from cache first for performance
            if (_assetService == null)
            {
                var serviceContainer = ServiceContainer.Instance;
                _assetService = serviceContainer?.ResolveService<IAssetService>();
            }
            
            if (_assetService == null)
            {
                Debug.LogWarning("AssetService not available. Cannot load avatar sprite.");
                return null;
            }

            var avatarKey = $"Avatars/{AvatarId.Value}";
            
            // Check cache first
            var cachedSprite = _assetService.GetCachedAsset<Sprite>(avatarKey);
            if (cachedSprite != null)
                return cachedSprite;

            // If not cached, try to load synchronously from Resources as fallback
            // For better performance, consider using async loading in your UI code
            var sprite = Resources.Load<Sprite>(avatarKey);
            if (sprite != null)
            {
                Debug.Log($"Loaded avatar sprite from Resources: {avatarKey}");
                return sprite;
            }

            Debug.LogWarning($"Avatar sprite not found: {avatarKey}. Ensure the sprite exists in Resources/Avatars/ or is properly configured in Addressables.");
            return null;
        }

        #endregion

        #region Properties

        /// <summary>Player's kill-death ratio. Returns 0 if no deaths recorded.</summary>
        public float KillDeathRatio => Deaths.Value > 0 ? (float)Kills.Value / Deaths.Value : Kills.Value;

        /// <summary>Whether this player has a custom avatar set.</summary>
        public bool HasCustomAvatar => !AvatarId.Value.IsEmpty;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize player data when spawned on network.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            // Initialize services
            InitializeServices();
            
            // Subscribe to value changes for local updates
            PlayerName.OnValueChanged += OnPlayerNameChanged;
            Score.OnValueChanged += OnScoreChanged;
            Kills.OnValueChanged += OnKillsChanged;
            Deaths.OnValueChanged += OnDeathsChanged;
            Assists.OnValueChanged += OnAssistsChanged;
        }

        /// <summary>
        /// Cleanup subscriptions when despawned from network.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            PlayerName.OnValueChanged -= OnPlayerNameChanged;
            Score.OnValueChanged -= OnScoreChanged;
            Kills.OnValueChanged -= OnKillsChanged;
            Deaths.OnValueChanged -= OnDeathsChanged;
            Assists.OnValueChanged -= OnAssistsChanged;
        }

        #endregion

        #region Server RPC Methods

        /// <summary>
        /// Updates the player's score. Server authority required.
        /// </summary>
        /// <param name="newScore">New score value to set.</param>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void SetScoreServerRpc(int newScore)
        {
            if (!IsServer) return;
            Score.Value = newScore;
        }

        /// <summary>
        /// Updates the player's network ping. Server authority required.
        /// </summary>
        /// <param name="ping">New ping value in milliseconds.</param>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void SetPingServerRpc(int ping)
        {
            if (!IsServer) return;
            Ping.Value = UnityEngine.Mathf.Max(0, ping);
        }

        /// <summary>
        /// Sets the player's display name. Server authority required.
        /// </summary>
        /// <param name="name">New player name (max 32 characters).</param>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void SetPlayerNameServerRpc(FixedString32Bytes name)
        {
            if (!IsServer) return;
            PlayerName.Value = name;
        }

        /// <summary>
        /// Increments the player's kill count. Server authority required.
        /// </summary>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void IncrementKillsServerRpc()
        {
            if (!IsServer) return;
            Kills.Value++;
        }

        /// <summary>
        /// Increments the player's death count. Server authority required.
        /// </summary>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void IncrementDeathsServerRpc()
        {
            if (!IsServer) return;
            Deaths.Value++;
        }

        /// <summary>
        /// Increments the player's assist count. Server authority required.
        /// </summary>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void IncrementAssistsServerRpc()
        {
            if (!IsServer) return;
            Assists.Value++;
        }

        /// <summary>
        /// Sets the player's avatar ID. Server authority required.
        /// </summary>
        /// <param name="avatarId">Avatar sprite identifier.</param>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void SetAvatarIdServerRpc(FixedString64Bytes avatarId)
        {
            if (!IsServer) return;
            AvatarId.Value = avatarId;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when player name changes across network.
        /// </summary>
        /// <param name="oldValue">Previous player name.</param>
        /// <param name="newValue">New player name.</param>
        private void OnPlayerNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
        {
            Debug.Log($"Player name changed from '{oldValue}' to '{newValue}'");
            
            // Publish event for UI updates
            _eventBus?.Publish(new PlayerNameChangedEvent(
                NetworkObjectId,
                oldValue.ToString(),
                newValue.ToString()
            ));
        }

        /// <summary>
        /// Called when player score changes across network.
        /// </summary>
        /// <param name="oldValue">Previous score value.</param>
        /// <param name="newValue">New score value.</param>
        private void OnScoreChanged(int oldValue, int newValue)
        {
            Debug.Log($"Player {PlayerName.Value} score changed from {oldValue} to {newValue}");
            
            // Publish event for scoreboard updates
            _eventBus?.Publish(new PlayerScoreChangedEvent(
                NetworkObjectId,
                PlayerName.Value.ToString(),
                oldValue,
                newValue
            ));
            
            // Also publish general statistics event
            PublishStatisticsEvent("Score");
        }

        /// <summary>
        /// Called when player kill count changes across network.
        /// </summary>
        /// <param name="oldValue">Previous kill count.</param>
        /// <param name="newValue">New kill count.</param>
        private void OnKillsChanged(int oldValue, int newValue)
        {
            Debug.Log($"Player {PlayerName.Value} kills: {oldValue} -> {newValue}");
            
            // Publish event for statistics UI updates
            _eventBus?.Publish(new PlayerKillsChangedEvent(
                NetworkObjectId,
                PlayerName.Value.ToString(),
                oldValue,
                newValue
            ));
            
            // Also publish general statistics event
            PublishStatisticsEvent("Kills");
        }

        /// <summary>
        /// Called when player death count changes across network.
        /// </summary>
        /// <param name="oldValue">Previous death count.</param>
        /// <param name="newValue">New death count.</param>
        private void OnDeathsChanged(int oldValue, int newValue)
        {
            Debug.Log($"Player {PlayerName.Value} deaths: {oldValue} -> {newValue}");
            
            // Publish event for statistics UI updates
            _eventBus?.Publish(new PlayerDeathsChangedEvent(
                NetworkObjectId,
                PlayerName.Value.ToString(),
                oldValue,
                newValue
            ));
            
            // Also publish general statistics event
            PublishStatisticsEvent("Deaths");
        }

        /// <summary>
        /// Called when player assist count changes across network.
        /// </summary>
        /// <param name="oldValue">Previous assist count.</param>
        /// <param name="newValue">New assist count.</param>
        private void OnAssistsChanged(int oldValue, int newValue)
        {
            Debug.Log($"Player {PlayerName.Value} assists: {oldValue} -> {newValue}");
            
            // Publish event for statistics UI updates
            _eventBus?.Publish(new PlayerAssistsChangedEvent(
                NetworkObjectId,
                PlayerName.Value.ToString(),
                oldValue,
                newValue
            ));
            
            // Also publish general statistics event
            PublishStatisticsEvent("Assists");
        }
        
        /// <summary>
        /// Initializes the required services from the global service provider.
        /// </summary>
        private void InitializeServices()
        {
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                _eventBus = serviceContainer.ResolveService<IEventBus>();
                _assetService = serviceContainer.ResolveService<IAssetService>();

                if (_eventBus == null)
                    Debug.LogWarning("EventBus service not found. Events will not be published.");
                if (_assetService == null)
                    Debug.LogWarning("AssetService not found. Avatar loading may be limited.");
            }
            else
            {
                Debug.LogWarning("ServiceContainer not initialized. NetworkPlayerData events and asset loading will be limited.");
            }
        }
        
        /// <summary>
        /// Publishes a general player statistics changed event.
        /// </summary>
        /// <param name="statisticType">The type of statistic that changed.</param>
        private void PublishStatisticsEvent(string statisticType)
        {
            _eventBus?.Publish(new PlayerStatisticsChangedEvent(
                NetworkObjectId,
                PlayerName.Value.ToString(),
                Score.Value,
                Kills.Value,
                Deaths.Value,
                Assists.Value,
                KillDeathRatio,
                statisticType
            ));
        }

        #endregion
    }
}
