using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListEntryPrefab;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private Dictionary<ulong, PlayerListEntry> playerEntries = new();

    // Example player data structure for UI
    private class PlayerListEntry
    {
        public GameObject gameObject;
        public TextMeshProUGUI playerNameText;
        public Image readyIndicator;

        public PlayerListEntry(GameObject obj)
        {
            gameObject = obj;
            playerNameText = obj.transform.Find("PlayerNameText").GetComponent<TextMeshProUGUI>();
            readyIndicator = obj.transform.Find("ReadyIndicator").GetComponent<Image>();
        }

        public void SetReady(bool ready)
        {
            readyIndicator.color = ready ? Color.green : Color.red;
        }

        public void SetName(string name)
        {
            playerNameText.text = name;
        }
    }

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

    /// <summary>
    /// Call when a player joins the lobby.
    /// </summary>
    public void AddPlayer(ulong clientId, string playerName, bool isReady)
    {
        if (playerEntries.ContainsKey(clientId)) return;

        GameObject entryObj = Instantiate(playerListEntryPrefab, playerListContent);
        PlayerListEntry entry = new(entryObj);
        entry.SetName(playerName);
        entry.SetReady(isReady);

        playerEntries.Add(clientId, entry);

        UpdateStatus();
    }

    /// <summary>
    /// Call when a player leaves the lobby.
    /// </summary>
    public void RemovePlayer(ulong clientId)
    {
        if (playerEntries.TryGetValue(clientId, out PlayerListEntry entry))
        {
            Destroy(entry.gameObject);
            playerEntries.Remove(clientId);
            UpdateStatus();
        }
    }

    /// <summary>
    /// Call to update a player's ready status.
    /// </summary>
    public void UpdatePlayerReadyStatus(ulong clientId, bool isReady)
    {
        if (playerEntries.TryGetValue(clientId, out PlayerListEntry entry))
        {
            entry.SetReady(isReady);
            UpdateStatus();
        }
    }

    private void UpdateStatus()
    {
        if (playerEntries.Count == 0)
        {
            statusText.text = "Waiting for players...";
            UpdateStartGameButtonVisibility(false);
            return;
        }

        bool allReady = true;
        foreach (var entry in playerEntries.Values)
        {
            if (entry.readyIndicator.color != Color.green)
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
        foreach (var item in playerItems.Values)
        {
            Destroy(item);
        }
        playerItems.Clear();

        // Get current lobby players from LobbyManager (you need to expose this)
        foreach (var playerEntry in LobbyManager.Instance.GetAllPlayers())
        {
            var item = Instantiate(playerListItemPrefab, playerListContainer);
            var playerNameText = item.transform.Find("PlayerNameText").GetComponent<Text>();
            var readyToggle = item.transform.Find("ReadyToggle").GetComponent<Toggle>();

            playerNameText.text = playerEntry.PlayerName;
            readyToggle.isOn = playerEntry.IsReady.Value;

            // If this is local player, enable toggle, else disable
            if (playerEntry.ClientId == NetworkManager.Singleton.LocalClientId)
            {
                readyToggle.interactable = true;
                readyToggle.onValueChanged.AddListener(isReady =>
                {
                    LobbyManager.Instance.RequestReadyStatusServerRpc(isReady);
                });
            }
            else
            {
                readyToggle.interactable = false;
            }

            playerItems[playerEntry.ClientId] = item;
        }
    }
}

    /// <summary>
    /// Call to update Start Game button visibility (host only).
    /// </summary>
    private void UpdateStartGameButtonVisibility(bool visible)
    {
        // TODO: Implement host check here to enable/disable button.
        startGameButton.gameObject.SetActive(visible);
    }

    private void OnReadyButtonClicked()
    {
        // TODO: Send ready/unready status to server.

        // Example toggle local ready status:
        // bool newReadyStatus = !currentReadyStatus;
        // UpdatePlayerReadyStatus(localClientId, newReadyStatus);
    }

    private void OnStartGameButtonClicked()
    {
        // TODO: Notify server to start the game.
    }

    private void OnEnable()
    {
        // Subscribe to network events or custom events to update UI
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeft;

        RefreshPlayerList();
    }

    private void OnDisable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerLeft;
    }
    
        private void OnPlayerJoined(ulong clientId)
    {
        RefreshPlayerList();
    }

    private void OnPlayerLeft(ulong clientId)
    {
        RefreshPlayerList();
    }
}
