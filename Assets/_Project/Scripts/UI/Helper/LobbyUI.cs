using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode; // Assuming you're using Netcode for GameObjects

public class LobbyUI : MonoBehaviour
{
    #region Fields

    [Header("UI References")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private PlayerListEntryUI playerListEntryPrefab; // Strongly typed prefab
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly Dictionary<ulong, PlayerListEntryUI> _playerEntries = new();

    #endregion

    #region Unity Override Methods

    private void Awake()
    {
        readyButton.onClick.AddListener(OnReadyButtonClicked);
        startGameButton.onClick.AddListener(OnStartGameButtonClicked);
    }

    private void Start()
    {
        UpdateStartGameButtonVisibility(false);
        statusText.text = "Waiting for players...";
    }

    private void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeft;

        RefreshPlayerList();
    }

    private void OnDisable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerLeft;
    }

    #endregion

    #region Public Methods

    public void AddPlayer(ulong clientId, string playerName, bool isReady)
    {
        if (_playerEntries.ContainsKey(clientId)) return;

        var entry = Instantiate(playerListEntryPrefab, playerListContent);
        entry.SetName(playerName);
        entry.SetReady(isReady);

        _playerEntries.Add(clientId, entry);
        UpdateStatus();
    }

    public void RemovePlayer(ulong clientId)
    {
        if (_playerEntries.TryGetValue(clientId, out var entry))
        {
            Destroy(entry.gameObject);
            _playerEntries.Remove(clientId);
            UpdateStatus();
        }
    }

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

    private void UpdateStatus()
    {
        if (_playerEntries.Count == 0)
        {
            statusText.text = "Waiting for players...";
            UpdateStartGameButtonVisibility(false);
            return;
        }

        bool allReady = true;
        foreach (var entry in _playerEntries.Values)
        {
            if (!entry.IsReady)
            {
                allReady = false;
                break;
            }
        }

        statusText.text = allReady ? "All players ready!" : "Waiting for players to be ready...";
        UpdateStartGameButtonVisibility(allReady);
    }

    private void RefreshPlayerList()
    {
        // Clear old UI
        foreach (var entry in _playerEntries.Values)
        {
            Destroy(entry.gameObject);
        }
        _playerEntries.Clear();

        // Get current lobby players from LobbyManager (must be provided by your game)
        foreach (var playerEntry in LobbyManager.Instance.GetAllPlayers())
        {
            var item = Instantiate(playerListEntryPrefab, playerListContent);
            item.SetName(playerEntry.PlayerName);
            item.SetReady(playerEntry.IsReady.Value);

            // Local player toggle
            item.SetInteractable(playerEntry.ClientId == NetworkManager.Singleton.LocalClientId);
            if (playerEntry.ClientId == NetworkManager.Singleton.LocalClientId)
            {
                item.OnReadyChanged += isReady =>
                {
                    LobbyManager.Instance.RequestReadyStatusServerRpc(isReady);
                };
            }

            _playerEntries[playerEntry.ClientId] = item;
        }
    }

    private void UpdateStartGameButtonVisibility(bool visible)
    {
        // TODO: Implement host check here to enable/disable button
        startGameButton.gameObject.SetActive(visible);
    }

    private void OnReadyButtonClicked()
    {
        // TODO: Send ready/unready status to server
    }

    private void OnStartGameButtonClicked()
    {
        // TODO: Notify server to start the game
    }

    private void OnPlayerJoined(ulong clientId) => RefreshPlayerList();

    private void OnPlayerLeft(ulong clientId) => RefreshPlayerList();

    #endregion
}

/// <summary>
/// UI component for a single player list entry.
/// </summary>
[Serializable]
public class PlayerListEntryUI : MonoBehaviour
{
    #region Fields

    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image readyIndicator;
    [SerializeField] private Toggle readyToggle;

    public event Action<bool> OnReadyChanged;

    public bool IsReady { get; private set; }

    #endregion

    #region Public Methods

    public void SetName(string name)
    {
        playerNameText.text = name;
    }

    public void SetReady(bool ready)
    {
        IsReady = ready;
        readyIndicator.color = ready ? Color.green : Color.red;
        if (readyToggle != null)
            readyToggle.isOn = ready;
    }

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
