using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; } = null!;

    public class PlayerData : NetworkBehaviour
    {
        public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false);
        public string PlayerName = "Unknown";
    }

    private readonly Dictionary<ulong, PlayerData> connectedPlayers = new();

    private void Awake()
    {
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

    public void SetPlayerReady(ulong clientId, bool ready)
    {
        if (!IsServer) return;

        if (connectedPlayers.TryGetValue(clientId, out var player))
        {
            player.IsReady.Value = ready;
            UpdateLobbyClients();
        }
    }

    private void UpdateLobbyClients()
    {
        // Here you would update NetworkVariables or send ClientRPCs to update lobby UI on clients
    }
}
