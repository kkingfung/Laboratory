using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class MatchmakingManager : MonoBehaviour
{
    public static MatchmakingManager Instance { get; private set; } = null!;

    public event Action<MatchmakingState>? OnMatchmakingStateChanged;

    public MatchmakingState CurrentState { get; private set; } = MatchmakingState.Idle;

    private CancellationTokenSource cts = new();

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeUnityServicesAsync();
    }

    private async UniTask InitializeUnityServicesAsync()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void StartMatchmaking()
    {
        if (CurrentState != MatchmakingState.Idle) return;

        cts = new CancellationTokenSource();

        SetState(MatchmakingState.Searching);

        try
        {
            // Create allocation with max 2 players for example
            var allocation = await RelayService.Instance.CreateAllocationAsync(1, cancellationToken: cts.Token);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"Relay allocation created. Join code: {joinCode}");

            SetState(MatchmakingState.MatchFound);

            // Setup host with Relay data
            SetupHost(allocation);

            // Optionally show join code to player or send to matchmaking backend
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Matchmaking cancelled.");
            SetState(MatchmakingState.Idle);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Relay matchmaking failed: {ex}");
            SetState(MatchmakingState.Failed);
        }
    }

    public void CancelMatchmaking()
    {
        if (CurrentState != MatchmakingState.Searching) return;

        cts.Cancel();

        SetState(MatchmakingState.Idle);
    }

    private void SetState(MatchmakingState state)
    {
        CurrentState = state;
        OnMatchmakingStateChanged?.Invoke(state);
    }

    private void SetupHost(CreateAllocationResponse allocation)
    {
        // Configure Unity Netcode with Relay server data for host
        var relayServerData = new Unity.Netcode.Transports.UTP.UnityTransport.RelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData);

        var unityTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        unityTransport.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartHost();
    }

    public async UniTask JoinMatch(string joinCode)
    {
        if (CurrentState != MatchmakingState.Idle) return;

        cts = new CancellationTokenSource();

        SetState(MatchmakingState.Searching);

        try
        {
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode, cancellationToken: cts.Token);

            Debug.Log("Relay join allocation successful.");

            SetupClient(joinAllocation);

            SetState(MatchmakingState.MatchFound);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Join matchmaking cancelled.");
            SetState(MatchmakingState.Idle);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Relay join failed: {ex}");
            SetState(MatchmakingState.Failed);
        }
    }

    private void SetupClient(JoinAllocationResponse joinAllocation)
    {
        // Configure Unity Netcode with Relay server data for client
        var relayServerData = new Unity.Netcode.Transports.UTP.UnityTransport.RelayServerData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData);

        var unityTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        unityTransport.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient();
    }

    private void OnDestroy()
    {
        cts.Cancel();
        cts.Dispose();
    }
}
