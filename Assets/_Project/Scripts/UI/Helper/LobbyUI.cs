using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Laboratory.Gameplay.Lobby;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// UI component for a single player list entry in the lobby.
    /// </summary>
    [Serializable]
    public class PlayerListEntryUI : MonoBehaviour
    {
        #region Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Image readyIndicator;
        [SerializeField] private Toggle readyToggle;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this player is ready.
        /// </summary>
        public bool IsReady { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when ready status changes.
        /// </summary>
        public event Action<bool> OnReadyChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the player name display.
        /// </summary>
        /// <param name="name">Player name to display</param>
        public void SetName(string name)
        {
            playerNameText.text = name;
        }

        /// <summary>
        /// Set the ready status and update visual indicators.
        /// </summary>
        /// <param name="ready">Whether player is ready</param>
        public void SetReady(bool ready)
        {
            IsReady = ready;
            readyIndicator.color = ready ? Color.green : Color.red;
            
            if (readyToggle != null)
                readyToggle.isOn = ready;
        }

        /// <summary>
        /// Set whether this entry can be interacted with (for local player).
        /// </summary>
        /// <param name="interactable">Whether entry should be interactable</param>
        public void SetInteractable(bool interactable)
        {
            if (readyToggle != null)
            {
                readyToggle.interactable = interactable;
                readyToggle.onValueChanged.RemoveAllListeners();
                
                if (interactable)
                {
                    readyToggle.onValueChanged.AddListener(value =>
                    {
                        SetReady(value);
                        OnReadyChanged?.Invoke(value);
                    });
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// UI component for managing lobby player list and game start functionality.
    /// Handles player join/leave events, ready status, and game start conditions.
    /// </summary>
    public class LobbyUI : NetworkBehaviour
    {
        #region Fields

        [Header("UI References")]
        [SerializeField] private Transform playerListContent;
        [SerializeField] private PlayerListEntryUI playerListEntryPrefab;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TextMeshProUGUI statusText;

        private readonly Dictionary<ulong, PlayerListEntryUI> _playerEntries = new();

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize UI components and setup event handlers.
        /// </summary>
        private void Awake()
        {
            SetupButtonHandlers();
            InitializeUI();
        }

        /// <summary>
        /// Subscribe to network events when enabled.
        /// </summary>
        private void OnEnable()
        {
            SubscribeToNetworkEvents();
            RefreshPlayerList();
        }

        /// <summary>
        /// Unsubscribe from network events when disabled.
        /// </summary>
        private void OnDisable()
        {
            UnsubscribeFromNetworkEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add a player to the lobby UI.
        /// </summary>
        /// <param name="clientId">Client ID of the player</param>
        /// <param name="playerName">Player's display name</param>
        /// <param name="isReady">Whether player is ready</param>
        public void AddPlayer(ulong clientId, string playerName, bool isReady)
        {
            if (_playerEntries.ContainsKey(clientId)) return;

            var entry = CreatePlayerEntry(clientId, playerName, isReady);
            _playerEntries.Add(clientId, entry);
            UpdateStatus();
        }

        /// <summary>
        /// Remove a player from the lobby UI.
        /// </summary>
        /// <param name="clientId">Client ID of the player to remove</param>
        public void RemovePlayer(ulong clientId)
        {
            if (_playerEntries.TryGetValue(clientId, out var entry))
            {
                Destroy(entry.gameObject);
                _playerEntries.Remove(clientId);
                UpdateStatus();
            }
        }

        /// <summary>
        /// Update a player's ready status.
        /// </summary>
        /// <param name="clientId">Client ID of the player</param>
        /// <param name="isReady">New ready status</param>
        public void UpdatePlayerReadyStatus(ulong clientId, bool isReady)
        {
            if (_playerEntries.TryGetValue(clientId, out var entry))
            {
                entry.SetReady(isReady);
                UpdateStatus();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Setup button event handlers.
        /// </summary>
        private void SetupButtonHandlers()
        {
            readyButton.onClick.AddListener(OnReadyButtonClicked);
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        }

        /// <summary>
        /// Initialize UI to default state.
        /// </summary>
        private void InitializeUI()
        {
            UpdateStartGameButtonVisibility(false);
            statusText.text = "Waiting for players...";
        }

        /// <summary>
        /// Subscribe to network manager events.
        /// </summary>
        private void SubscribeToNetworkEvents()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeft;
            }
        }

        /// <summary>
        /// Unsubscribe from network manager events.
        /// </summary>
        private void UnsubscribeFromNetworkEvents()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerJoined;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerLeft;
            }
        }

        /// <summary>
        /// Create a new player list entry UI component.
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <param name="playerName">Player name</param>
        /// <param name="isReady">Ready status</param>
        /// <returns>Created player entry UI</returns>
        private PlayerListEntryUI CreatePlayerEntry(ulong clientId, string playerName, bool isReady)
        {
            var entry = Instantiate(playerListEntryPrefab, playerListContent);
            entry.SetName(playerName);
            entry.SetReady(isReady);

            // Configure interactability for local player
            bool isLocalPlayer = NetworkManager.Singleton != null && 
                               clientId == NetworkManager.Singleton.LocalClientId;
            entry.SetInteractable(isLocalPlayer);

            if (isLocalPlayer)
            {
                entry.OnReadyChanged += HandleLocalPlayerReadyChange;
            }

            return entry;
        }

        /// <summary>
        /// Handle local player ready status change.
        /// </summary>
        /// <param name="isReady">New ready status</param>
        private void HandleLocalPlayerReadyChange(bool isReady)
        {
            // Send ready status to server via LobbyManager
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.RequestReadyStatusServerRpc(isReady);
                Debug.Log($"[LobbyUI] Sent ready status to server: {isReady}");
            }
            else
            {
                Debug.LogError("[LobbyUI] LobbyManager.Instance is null. Cannot send ready status to server.");
            }
        }

        /// <summary>
        /// Update lobby status text and UI state.
        /// </summary>
        private void UpdateStatus()
        {
            if (_playerEntries.Count == 0)
            {
                statusText.text = "Waiting for players...";
                UpdateStartGameButtonVisibility(false);
                return;
            }

            bool allReady = AreAllPlayersReady();
            statusText.text = allReady ? "All players ready!" : "Waiting for players to be ready...";
            UpdateStartGameButtonVisibility(allReady);
        }

        /// <summary>
        /// Check if all players in the lobby are ready.
        /// </summary>
        /// <returns>True if all players are ready</returns>
        private bool AreAllPlayersReady()
        {
            foreach (var entry in _playerEntries.Values)
            {
                if (!entry.IsReady)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Refresh the entire player list from lobby manager data.
        /// </summary>
        private void RefreshPlayerList()
        {
            ClearPlayerList();

            // Get current lobby players from LobbyManager
            if (LobbyManager.Instance != null)
            {
                PopulatePlayerListFromLobbyManager();
                Debug.Log($"[LobbyUI] Player list refreshed with {_playerEntries.Count} players");
            }
            else
            {
                Debug.LogWarning("[LobbyUI] LobbyManager.Instance is null. Cannot refresh player list.");
            }
        }

        /// <summary>
        /// Clear all player entries from the UI.
        /// </summary>
        private void ClearPlayerList()
        {
            foreach (var entry in _playerEntries.Values)
            {
                Destroy(entry.gameObject);
            }
            _playerEntries.Clear();
        }

        /// <summary>
        /// Populate player list from lobby manager data.
        /// </summary>
        private void PopulatePlayerListFromLobbyManager()
        {
            foreach (var (clientId, playerData) in LobbyManager.Instance.GetAllPlayers())
            {
                var item = CreatePlayerEntry(
                    clientId, 
                    playerData.PlayerName, 
                    playerData.IsReady.Value);

                _playerEntries[clientId] = item;
            }
        }

        /// <summary>
        /// Update start game button visibility based on game state.
        /// </summary>
        /// <param name="visible">Whether button should be visible</param>
        private void UpdateStartGameButtonVisibility(bool visible)
        {
            // Add host check here to enable/disable button
            bool isHost = NetworkManager.Singleton != null && 
                         NetworkManager.Singleton.IsHost;
            
            // Only show start game button to host when all players are ready
            bool shouldShowButton = visible && isHost;
            
            startGameButton.gameObject.SetActive(shouldShowButton);
            
            if (visible && !isHost)
            {
                Debug.Log("[LobbyUI] All players ready, but local client is not the host. Start button hidden.");
            }
            else if (shouldShowButton)
            {
                Debug.Log("[LobbyUI] All players ready and local client is host. Start button visible.");
            }
        }

        /// <summary>
        /// Handle ready button click.
        /// </summary>
        private void OnReadyButtonClicked()
        {
            // Toggle local player ready status
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[LobbyUI] NetworkManager.Singleton is null. Cannot toggle ready status.");
                return;
            }
            
            var localClientId = NetworkManager.Singleton.LocalClientId;
            
            // Find local player entry and toggle ready status
            if (_playerEntries.TryGetValue(localClientId, out var localPlayerEntry))
            {
                bool newReadyStatus = !localPlayerEntry.IsReady;
                
                // Send the new ready status to server
                if (LobbyManager.Instance != null)
                {
                    LobbyManager.Instance.RequestReadyStatusServerRpc(newReadyStatus);
                    Debug.Log($"[LobbyUI] Local player ready status toggled to: {newReadyStatus}");
                }
                else
                {
                    Debug.LogError("[LobbyUI] LobbyManager.Instance is null. Cannot toggle ready status.");
                }
            }
            else
            {
                Debug.LogWarning($"[LobbyUI] Local player entry not found for client ID: {localClientId}");
            }
        }

        /// <summary>
        /// Handle start game button click.
        /// </summary>
        private void OnStartGameButtonClicked()
        {
            // Notify server to start the game
            if (!NetworkManager.Singleton.IsHost)
            {
                Debug.LogError("[LobbyUI] Only the host can start the game.");
                return;
            }
            
            if (!AreAllPlayersReady())
            {
                Debug.LogWarning("[LobbyUI] Cannot start game - not all players are ready.");
                return;
            }
            
            if (LobbyManager.Instance != null)
            {
                // For now, we'll implement a simple game start mechanism
                // In a more complex system, this would trigger scene loading or game state change
                StartGameServerRpc();
                Debug.Log("[LobbyUI] Game start requested by host.");
            }
            else
            {
                Debug.LogError("[LobbyUI] LobbyManager.Instance is null. Cannot start game.");
            }
        }
        
        /// <summary>
        /// ServerRpc to handle game start from the host.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void StartGameServerRpc()
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                Debug.LogError("[LobbyUI] StartGameServerRpc called by non-host client.");
                return;
            }
            
            // Notify all clients that the game is starting
            NotifyGameStartClientRpc();
            
            // Here you would typically:
            // 1. Load the game scene
            // 2. Initialize game state
            // 3. Spawn player objects
            // 4. Start the game timer
            
            Debug.Log("[LobbyUI] Game started by host. Notifying all clients.");
            
            // For demonstration, let's log the current players
            if (LobbyManager.Instance != null)
            {
                var playerCount = 0;
                foreach (var (clientId, playerData) in LobbyManager.Instance.GetAllPlayers())
                {
                    playerCount++;
                    Debug.Log($"[LobbyUI] Starting game with player: {playerData.PlayerName} (ID: {clientId})");
                }
                Debug.Log($"[LobbyUI] Total players starting game: {playerCount}");
            }
        }
        
        /// <summary>
        /// ClientRpc to notify all clients that the game is starting.
        /// </summary>
        [ClientRpc]
        private void NotifyGameStartClientRpc()
        {
            Debug.Log("[LobbyUI] Game is starting! Preparing for gameplay...");
            
            // Update UI to show game starting state
            statusText.text = "Game Starting...";
            
            // Disable lobby UI elements
            readyButton.interactable = false;
            startGameButton.interactable = false;
            
            // Here you would typically:
            // 1. Show loading screen
            // 2. Prepare player for scene transition
            // 3. Initialize client-side game state
            
            // For demonstration, we'll just update the status
            Invoke(nameof(SimulateGameStart), 2f);
        }
        
        /// <summary>
        /// Simulates the game start process for demonstration purposes.
        /// </summary>
        private void SimulateGameStart()
        {
            statusText.text = "Game Started! (This is a simulation)";
            Debug.Log("[LobbyUI] Game start simulation complete. In a real game, players would now be in the gameplay scene.");
        }

        /// <summary>
        /// Handle player joined event.
        /// </summary>
        /// <param name="clientId">ID of joined client</param>
        private void OnPlayerJoined(ulong clientId) => RefreshPlayerList();

        /// <summary>
        /// Handle player left event.
        /// </summary>
        /// <param name="clientId">ID of left client</param>
        private void OnPlayerLeft(ulong clientId) => RefreshPlayerList();

        #endregion
    }
}
