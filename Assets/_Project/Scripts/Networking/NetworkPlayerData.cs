using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Network-synchronized player data container managing player information across clients.
    /// Stores player statistics, metadata, and gameplay metrics with server authority.
    /// </summary>
    public class NetworkPlayerData : NetworkBehaviour
    {
        #region Fields

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
        /// This method should be implemented to load sprites from your asset management system.
        /// </summary>
        /// <returns>The sprite corresponding to the current avatar ID, or null if not found.</returns>
        public Sprite GetAvatarSprite()
        {
            if (AvatarId.Value.IsEmpty)
                return null;

            // TODO: Implement sprite loading from your asset management system
            // Example:
            // return Resources.Load<Sprite>($"Avatars/{AvatarId.Value}");
            // or use Addressables:
            // return Addressables.LoadAssetAsync<Sprite>(AvatarId.Value.ToString()).WaitForCompletion();
            
            Debug.LogWarning($"GetAvatarSprite not fully implemented. Avatar ID: {AvatarId.Value}");
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
            // Subscribe to value changes for local updates
            PlayerName.OnValueChanged += OnPlayerNameChanged;
            Score.OnValueChanged += OnScoreChanged;
            Kills.OnValueChanged += OnKillsChanged;
            Deaths.OnValueChanged += OnDeathsChanged;
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
        }

        #endregion

        #region Server RPC Methods

        /// <summary>
        /// Updates the player's score. Server authority required.
        /// </summary>
        /// <param name="newScore">New score value to set.</param>
        [ServerRpc(RequireOwnership = false)]
        public void SetScoreServerRpc(int newScore)
        {
            if (!IsServer) return;
            Score.Value = newScore;
        }

        /// <summary>
        /// Updates the player's network ping. Server authority required.
        /// </summary>
        /// <param name="ping">New ping value in milliseconds.</param>
        [ServerRpc(RequireOwnership = false)]
        public void SetPingServerRpc(int ping)
        {
            if (!IsServer) return;
            Ping.Value = UnityEngine.Mathf.Max(0, ping);
        }

        /// <summary>
        /// Sets the player's display name. Server authority required.
        /// </summary>
        /// <param name="name">New player name (max 32 characters).</param>
        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerNameServerRpc(FixedString32Bytes name)
        {
            if (!IsServer) return;
            PlayerName.Value = name;
        }

        /// <summary>
        /// Increments the player's kill count. Server authority required.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void IncrementKillsServerRpc()
        {
            if (!IsServer) return;
            Kills.Value++;
        }

        /// <summary>
        /// Increments the player's death count. Server authority required.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void IncrementDeathsServerRpc()
        {
            if (!IsServer) return;
            Deaths.Value++;
        }

        /// <summary>
        /// Increments the player's assist count. Server authority required.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void IncrementAssistsServerRpc()
        {
            if (!IsServer) return;
            Assists.Value++;
        }

        /// <summary>
        /// Sets the player's avatar ID. Server authority required.
        /// </summary>
        /// <param name="avatarId">Avatar sprite identifier.</param>
        [ServerRpc(RequireOwnership = false)]
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
            // TODO: Publish event for UI updates
        }

        /// <summary>
        /// Called when player score changes across network.
        /// </summary>
        /// <param name="oldValue">Previous score value.</param>
        /// <param name="newValue">New score value.</param>
        private void OnScoreChanged(int oldValue, int newValue)
        {
            Debug.Log($"Player {PlayerName.Value} score changed from {oldValue} to {newValue}");
            // TODO: Publish event for scoreboard updates
        }

        /// <summary>
        /// Called when player kill count changes across network.
        /// </summary>
        /// <param name="oldValue">Previous kill count.</param>
        /// <param name="newValue">New kill count.</param>
        private void OnKillsChanged(int oldValue, int newValue)
        {
            Debug.Log($"Player {PlayerName.Value} kills: {oldValue} -> {newValue}");
            // TODO: Publish event for statistics UI updates
        }

        /// <summary>
        /// Called when player death count changes across network.
        /// </summary>
        /// <param name="oldValue">Previous death count.</param>
        /// <param name="newValue">New death count.</param>
        private void OnDeathsChanged(int oldValue, int newValue)
        {
            Debug.Log($"Player {PlayerName.Value} deaths: {oldValue} -> {newValue}");
            // TODO: Publish event for statistics UI updates
        }

        #endregion
    }
}
