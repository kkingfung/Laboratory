using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Laboratory.Gameplay.Lobby
{
    /// <summary>
    /// Manages the game lobby, player list, and lobby events.
    /// </summary>
    public class LobbyManager : NetworkBehaviour
    {
        #region Fields

        [Header("Lobby Settings")]
        [SerializeField] private int maxPlayers = 4;

        private int _currentPlayers;

        public static LobbyManager Instance { get; private set; } = null!;

        public class PlayerData : NetworkBehaviour
        {
            public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false);
            public string PlayerName = "Unknown";
        }

        private readonly Dictionary<ulong, PlayerData> connectedPlayers = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current number of players in the lobby.
        /// </summary>
        public int CurrentPlayers => _currentPlayers;

        /// <summary>
        /// Gets the maximum number of players allowed in the lobby.
        /// </summary>
        public int MaxPlayers => maxPlayers;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            _currentPlayers = 0;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a player to the lobby if space is available.
        /// </summary>
        public bool AddPlayer()
        {
            if (_currentPlayers < maxPlayers)
            {
                _currentPlayers++;
                // Raise lobby event here if needed
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a player from the lobby.
        /// </summary>
        public bool RemovePlayer()
        {
            if (_currentPlayers > 0)
            {
                _currentPlayers--;
                // Raise lobby event here if needed
                return true;
            }
            return false;
        }

        public void SetPlayerReady(ulong clientId, bool ready)
        {
            if (!IsServer) return;

            if (connectedPlayers.TryGetValue(clientId, out var player))
            {
                player.IsReady.Value = ready;
                UpdateLobbyClients();
            }
        }

        #endregion

        #region Private Methods

        private void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;

            // Add player with default not ready
            connectedPlayers[clientId] = new PlayerData
            {
                PlayerName = $"Player {clientId}"
            };

            UpdateLobbyClients();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!IsServer) return;

            connectedPlayers.Remove(clientId);

            UpdateLobbyClients();
        }

        private void UpdateLobbyClients()
        {
            // Here you would update NetworkVariables or send ClientRPCs to update lobby UI on clients
        }

        #endregion

        #region Inner Classes, Enums

        // No inner classes or enums currently.

        #endregion
    }
}
