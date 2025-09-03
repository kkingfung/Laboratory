using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
// using Unity.Services.Core;
// using Unity.Services.Relay;
// using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Netcode.Transports.UTP;

#nullable enable

namespace Laboratory.Gameplay.Lobby
{
    /// <summary>
    /// Enum representing different matchmaking states.
    /// </summary>
    public enum MatchmakingState
    {
        Idle,
        Searching,
        MatchFound,
        Failed
    }

    /// <summary>
    /// Handles matchmaking logic, queueing, and starting matches.
    /// </summary>
    public class MatchmakingManager : MonoBehaviour
    {
        #region Singleton

        public static MatchmakingManager? Instance { get; private set; }

        #endregion

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
                // TODO: Implement Relay integration when Unity Services packages are available
                // Create allocation with max 2 players for example
                // var allocation = await RelayService.Instance.CreateAllocationAsync(1);
                
                // string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                
                Debug.Log("Matchmaking started - Relay integration disabled");

                // Simulate matchmaking delay
                await UniTask.Delay(1000, cancellationToken: cts.Token);

                SetState(MatchmakingState.MatchFound);

                // Setup host directly without Relay
                NetworkManager.Singleton.StartHost();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Matchmaking cancelled.");
                SetState(MatchmakingState.Idle);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Matchmaking failed: {ex}");
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

        private void SetupHost()
        {
            // TODO: Configure Unity Netcode with Relay server data when services are available
            // For now, start host directly
            NetworkManager.Singleton.StartHost();
        }

        public async UniTask JoinMatch(string joinCode)
        {
            if (CurrentState != MatchmakingState.Idle) return;

            cts = new CancellationTokenSource();

            SetState(MatchmakingState.Searching);

            try
            {
                // TODO: Implement Relay join when Unity Services packages are available
                // var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                Debug.Log($"Join match requested with code: {joinCode} - Relay integration disabled");

                // Simulate connection delay
                await UniTask.Delay(500, cancellationToken: cts.Token);

                // Setup client directly
                NetworkManager.Singleton.StartClient();

                SetState(MatchmakingState.MatchFound);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Join matchmaking cancelled.");
                SetState(MatchmakingState.Idle);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Join failed: {ex}");
                SetState(MatchmakingState.Failed);
            }
        }

        private void SetupClient()
        {
            // TODO: Configure Unity Netcode with Relay server data when services are available
            // For now, start client directly
            NetworkManager.Singleton.StartClient();
        }

        private async UniTask InitializeUnityServicesAsync()
        {
            // TODO: Initialize Unity Services when packages are available
            // await UnityServices.InitializeAsync();
            
            // if (!AuthenticationService.Instance.IsSignedIn)
            // {
            //     await AuthenticationService.Instance.SignInAnonymouslyAsync();
            // }

            Debug.Log("Unity Services initialization skipped - packages not available");
            await UniTask.CompletedTask;
        }

        #endregion
    }
}
