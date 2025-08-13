using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Netcode.Transports.UTP;

namespace Laboratory.Gameplay.Lobby
{
    /// <summary>
    /// Handles matchmaking logic, queueing, and starting matches.
    /// </summary>
    public class MatchmakingManager : MonoBehaviour
    {
        #region Fields

        [Header("Matchmaking Settings")]
        [SerializeField] private float matchmakingTimeout = 30f;

        private float _timer;
        private bool _isSearching;

        private CancellationTokenSource cts = new();

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if matchmaking is currently in progress.
        /// </summary>
        public bool IsSearching => _isSearching;

        public event Action<MatchmakingState>? OnMatchmakingStateChanged;

        public MatchmakingState CurrentState { get; private set; } = MatchmakingState.Idle;

        #endregion

        #region Unity Override Methods

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

        private void Update()
        {
            if (_isSearching)
            {
                _timer += Time.deltaTime;
                if (_timer >= matchmakingTimeout)
                {
                    CancelMatchmaking();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the matchmaking process.
        /// </summary>
        public async void StartMatchmaking()
        {
            if (_isSearching) return;

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

        /// <summary>
        /// Cancels the matchmaking process.
        /// </summary>
        public void CancelMatchmaking()
        {
            if (!_isSearching) return;

            cts.Cancel();

            SetState(MatchmakingState.Idle);
        }

        /// <summary>
        /// Called when a match is found.
        /// </summary>
        public void OnMatchFound()
        {
            _isSearching = false;
            _timer = 0f;
            // Add logic to transition to game, notify players, etc.
        }

        #endregion

        #region Private Methods

        private void SetState(MatchmakingState state)
        {
            CurrentState = state;
            OnMatchmakingStateChanged?.Invoke(state);
        }

        private void SetupHost(CreateAllocationResponse allocation)
        {
            // Configure Unity Netcode with Relay server data for host
            var relayServerData = new UnityTransport.RelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData);

            var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
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
            var relayServerData = new UnityTransport.RelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData);

            var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            unityTransport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
        }

        private async UniTask InitializeUnityServicesAsync()
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        private void OnDestroy()
        {
            cts.Cancel();
            cts.Dispose();
        }

        #endregion

        #region Inner Classes, Enums

        // No inner classes or enums currently.

        #endregion

        public static MatchmakingManager Instance { get; private set; } = null!;
    }
}
