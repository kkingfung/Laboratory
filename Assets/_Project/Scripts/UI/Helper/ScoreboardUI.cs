using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Cysharp.Threading.Tasks;
using MessagePipe;
using VContainer;
using Laboratory.Infrastructure.Networking;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// Manages the multiplayer scoreboard UI with player ranking, pagination, and real-time updates.
    /// Handles player connection/disconnection events and provides sorted leaderboard display.
    /// </summary>
    public class ScoreboardUI : MonoBehaviour, IDisposable
    {
        #region Fields

        [Header("UI References")]
        #pragma warning disable 0414 // Field assigned but never used - intended for future use
        [SerializeField] private Transform playerListContainer = null!;
        [SerializeField] private PlayerRowUI playerRowPrefab = null!;
        #pragma warning restore 0414
        [SerializeField] private int itemsPerPage = 10;
        [SerializeField] private Button nextPageButton = null!;
        [SerializeField] private Button prevPageButton = null!;
        [SerializeField] private TextMeshProUGUI pageIndicatorText = null!;

        private readonly Dictionary<ulong, PlayerRowUI> _playerRows = new();
        private List<PlayerRowUI> _sortedPlayers = new();
        private int _currentPage = 0;
        private int _totalPages = 1;

        private IDisposable _scoreboardUpdateSubscription;
        private IPublisher<ScoreboardUpdateEvent> _scoreboardUpdatePublisher;
        private ISubscriber<ScoreboardUpdateEvent> _scoreboardUpdateSubscriber;

        private readonly SortCriterion[] _sortPriority =
        {
            SortCriterion.Kills,
            SortCriterion.Deaths,
            SortCriterion.Assists,
            SortCriterion.Score
        };

        #endregion

        #region Enums

        /// <summary>
        /// Defines the sorting criteria for player ranking
        /// </summary>
        public enum SortCriterion
        {
            Score,
            Kills,
            Deaths,
            Assists,
            Custom
        }

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize UI components and event handlers
        /// </summary>
        private void Awake()
        {
            nextPageButton.onClick.AddListener(() => ChangePage(_currentPage + 1));
            prevPageButton.onClick.AddListener(() => ChangePage(_currentPage - 1));
        }

        /// <summary>
        /// Setup network event handlers and start periodic updates
        /// </summary>
        private void OnEnable()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                AddPlayer(client.ClientId);
            }

            PeriodicSortLoop().Forget();
        }

        /// <summary>
        /// Cleanup network event handlers and clear players
        /// </summary>
        private void OnDisable()
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;

            _scoreboardUpdateSubscription?.Dispose();
            ClearAllPlayers();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inject dependencies for scoreboard messaging system
        /// </summary>
        /// <param name="scoreboardUpdatePublisher">Publisher for scoreboard update events</param>
        /// <param name="scoreboardUpdateSubscriber">Subscriber for scoreboard update events</param>
        [Inject]
        public void Construct(
            IPublisher<ScoreboardUpdateEvent> scoreboardUpdatePublisher,
            ISubscriber<ScoreboardUpdateEvent> scoreboardUpdateSubscriber)
        {
            _scoreboardUpdatePublisher = scoreboardUpdatePublisher;
            _scoreboardUpdateSubscriber = scoreboardUpdateSubscriber;
        }

        /// <summary>
        /// Dispose of resources and cleanup
        /// </summary>
        public void Dispose()
        {
            ClearAllPlayers();
            _scoreboardUpdateSubscription?.Dispose();
        }

        #endregion

        #region Player Management

        /// <summary>
        /// Handle player connection events
        /// </summary>
        /// <param name="clientId">Connected client ID</param>
        private void OnPlayerConnected(ulong clientId) => AddPlayer(clientId);

        /// <summary>
        /// Handle player disconnection events
        /// </summary>
        /// <param name="clientId">Disconnected client ID</param>
        private void OnPlayerDisconnected(ulong clientId) => RemovePlayer(clientId);

        /// <summary>
        /// Add a new player to the scoreboard
        /// </summary>
        /// <param name="clientId">Client ID of the player to add</param>
        private void AddPlayer(ulong clientId)
        {
            if (_playerRows.ContainsKey(clientId)) return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient)) return;
            var playerObj = networkClient.PlayerObject;
            if (playerObj == null) return;

            var playerData = playerObj.GetComponent<NetworkPlayerData>();
            if (playerData == null) 
            {
                // Fallback: create a stub data object for testing
                Debug.LogWarning($"NetworkPlayerData component not found for client {clientId}, using stub data");
                return;
            }

            // Create adapter to resolve interface mismatch
            var adaptedPlayerData = new NetworkPlayerDataAdapter(playerData);
            
            var rowUI = Instantiate(playerRowPrefab, playerListContainer);
            rowUI.Initialize(adaptedPlayerData, this);
            rowUI.Bind(() => _scoreboardUpdatePublisher?.Publish(new ScoreboardUpdateEvent()));
            _playerRows.Add(clientId, rowUI);
            
            Debug.Log($"Player {clientId} added to scoreboard successfully");
            RefreshAllPlayers();
        }

        /// <summary>
        /// Remove a player from the scoreboard
        /// </summary>
        /// <param name="clientId">Client ID of the player to remove</param>
        private void RemovePlayer(ulong clientId)
        {
            if (!_playerRows.TryGetValue(clientId, out var rowUI)) return;

            rowUI.Unbind();
            Destroy(rowUI.gameObject);
            _playerRows.Remove(clientId);

            RefreshAllPlayers();
        }

        /// <summary>
        /// Clear all players from the scoreboard
        /// </summary>
        private void ClearAllPlayers()
        {
            foreach (var row in _playerRows.Values)
            {
                row.Unbind();
                Destroy(row.gameObject);
            }
            _playerRows.Clear();
            _sortedPlayers.Clear();
        }

        #endregion

        #region UI Refresh & Sorting

        /// <summary>
        /// Continuous sorting loop for real-time scoreboard updates
        /// </summary>
        /// <returns>UniTaskVoid for async operation</returns>
        private async UniTaskVoid PeriodicSortLoop()
        {
            while (true)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5));
                RefreshAllPlayers();
            }
        }

        /// <summary>
        /// Refresh and sort all players in the scoreboard
        /// </summary>
        private void RefreshAllPlayers()
        {
            _sortedPlayers = _playerRows.Values
                .OrderBy(row => row, Comparer<PlayerRowUI>.Create(ComparePlayers))
                .ToList();

            _totalPages = Mathf.Max(1, Mathf.CeilToInt((float)_sortedPlayers.Count / itemsPerPage));
            _currentPage = Mathf.Clamp(_currentPage, 0, _totalPages - 1);

            UpdatePageIndicator();
            DisplayCurrentPage();
        }

        /// <summary>
        /// Update pagination UI elements
        /// </summary>
        private void UpdatePageIndicator()
        {
            pageIndicatorText.text = $"Page {_currentPage + 1} / {_totalPages}";
            prevPageButton.interactable = _currentPage > 0;
            nextPageButton.interactable = _currentPage < _totalPages - 1;
        }

        /// <summary>
        /// Display the current page of players
        /// </summary>
        private void DisplayCurrentPage()
        {
            foreach (var row in _playerRows.Values)
            {
                row.gameObject.SetActive(false);
            }

            int start = _currentPage * itemsPerPage;
            int end = Mathf.Min(start + itemsPerPage, _sortedPlayers.Count);

            for (int i = start; i < end; i++)
            {
                var row = _sortedPlayers[i];
                row.gameObject.SetActive(true);
                row.AnimateRankChange(i + 1);
            }
        }

        /// <summary>
        /// Change to a specific page
        /// </summary>
        /// <param name="newPage">Target page index</param>
        private void ChangePage(int newPage)
        {
            _currentPage = Mathf.Clamp(newPage, 0, _totalPages - 1);
            UpdatePageIndicator();
            DisplayCurrentPage();
        }

        /// <summary>
        /// Compare two players based on sorting criteria
        /// </summary>
        /// <param name="a">First player to compare</param>
        /// <param name="b">Second player to compare</param>
        /// <returns>Comparison result</returns>
        private int ComparePlayers(PlayerRowUI a, PlayerRowUI b)
        {
            foreach (var criterion in _sortPriority)
            {
                int cmp = criterion switch
                {
                    SortCriterion.Score => b.PlayerData.Score.Value.CompareTo(a.PlayerData.Score.Value),
                    SortCriterion.Kills => b.PlayerData.Kills.Value.CompareTo(a.PlayerData.Kills.Value),
                    SortCriterion.Deaths => a.PlayerData.Deaths.Value.CompareTo(b.PlayerData.Deaths.Value),
                    SortCriterion.Assists => b.PlayerData.Assists.Value.CompareTo(a.PlayerData.Assists.Value),
                    _ => 0
                };

                if (cmp != 0) return cmp;
            }
            return 0;
        }

        #endregion
    }

    /// <summary>
    /// Represents a single player row in the scoreboard with data binding and animations
    /// </summary>
    [Serializable]
    public class PlayerRowUI : MonoBehaviour
    {
        #region Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText = null!;
        [SerializeField] private TextMeshProUGUI scoreText = null!;
        [SerializeField] private TextMeshProUGUI rankText = null!;
        #pragma warning disable 0414 // Field assigned but never used - intended for future avatar display
        [SerializeField] private Image avatarImage = null!;
        #pragma warning restore 0414
        [SerializeField] private TextMeshProUGUI pingText = null!;
        [SerializeField] private Image pingBar = null!;

        private int _currentRank = 0;
        private Coroutine _rankAnimCoroutine;
        private Action _onDataChanged;
        private MonoBehaviour _coroutineRunner = null!;

        #endregion

        #region Properties

        /// <summary>
        /// Player network data reference
        /// </summary>
        public INetworkPlayerData PlayerData { get; private set; } = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the player row with data and coroutine runner
        /// </summary>
        /// <param name="playerData">Player network data</param>
        /// <param name="coroutineRunner">MonoBehaviour for running coroutines</param>
        public void Initialize(INetworkPlayerData playerData, MonoBehaviour coroutineRunner)
        {
            PlayerData = playerData;
            _coroutineRunner = coroutineRunner;
            UpdateUI();
        }

        /// <summary>
        /// Bind to player data change events
        /// </summary>
        /// <param name="onDataChanged">Callback for data changes</param>
        public void Bind(Action onDataChanged)
        {
            _onDataChanged = onDataChanged;

            PlayerData.Score.OnValueChanged += OnValueChanged;
            PlayerData.PlayerName.OnValueChanged += OnNameChanged;
            PlayerData.Kills.OnValueChanged += OnValueChanged;
            PlayerData.Deaths.OnValueChanged += OnValueChanged;
            PlayerData.Assists.OnValueChanged += OnValueChanged;
            PlayerData.Ping.OnValueChanged += OnValueChanged;
        }

        /// <summary>
        /// Unbind from player data events
        /// </summary>
        public void Unbind()
        {
            PlayerData.Score.OnValueChanged -= OnValueChanged;
            PlayerData.PlayerName.OnValueChanged -= OnNameChanged;
            PlayerData.Kills.OnValueChanged -= OnValueChanged;
            PlayerData.Deaths.OnValueChanged -= OnValueChanged;
            PlayerData.Assists.OnValueChanged -= OnValueChanged;
            PlayerData.Ping.OnValueChanged -= OnValueChanged;
        }

        /// <summary>
        /// Animate rank change with smooth transition
        /// </summary>
        /// <param name="newRank">New rank to animate to</param>
        public void AnimateRankChange(int newRank)
        {
            if (_rankAnimCoroutine != null)
                _coroutineRunner.StopCoroutine(_rankAnimCoroutine);
            _rankAnimCoroutine = _coroutineRunner.StartCoroutine(AnimateRank(newRank));
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Handle value changed events and update UI
        /// </summary>
        /// <param name="oldVal">Previous value</param>
        /// <param name="newVal">New value</param>
        private void OnValueChanged(int oldVal, int newVal)
        {
            UpdateUI();
            _onDataChanged?.Invoke();
        }

        /// <summary>
        /// Handle name changed events and update UI
        /// </summary>
        /// <param name="oldVal">Previous value</param>
        /// <param name="newVal">New value</param>
        private void OnNameChanged(string oldVal, string newVal)
        {
            UpdateUI();
            _onDataChanged?.Invoke();
        }

        /// <summary>
        /// Update all UI elements with current player data
        /// </summary>
        public void UpdateUI()
        {
            nameText.text = PlayerData.PlayerName.Value.ToString();
            scoreText.text = PlayerData.Score.Value.ToString();
            pingText.text = $"{PlayerData.Ping.Value} ms";
            UpdatePingBar(PlayerData.Ping.Value);
            LoadPlayerAvatar();
        }

        /// <summary>
        /// Load and display player avatar
        /// </summary>
        private void LoadPlayerAvatar()
        {
            if (PlayerData.PlayerAvatar != null)
            {
                avatarImage.sprite = PlayerData.PlayerAvatar;
                avatarImage.gameObject.SetActive(true);
            }
            else
            {
                // Use default avatar or hide the image
                avatarImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Update ping bar visual based on ping value
        /// </summary>
        /// <param name="ping">Current ping value</param>
        private void UpdatePingBar(int ping)
        {
            int clampedPing = Mathf.Clamp(ping, 0, 300);
            float widthPercent = 1f - (clampedPing / 300f);
            pingBar.rectTransform.localScale = new Vector3(widthPercent, 1f, 1f);

            pingBar.color = clampedPing switch
            {
                < 75 => Color.green,
                < 150 => Color.yellow,
                _ => Color.red
            };
        }

        #endregion

        #region Rank Animation

        /// <summary>
        /// Animate rank number transition
        /// </summary>
        /// <param name="newRank">Target rank</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator AnimateRank(int newRank)
        {
            const float duration = 0.5f;
            float elapsed = 0f;
            int startRank = _currentRank == 0 ? newRank : _currentRank;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int displayRank = Mathf.RoundToInt(Mathf.Lerp(startRank, newRank, t));
                rankText.text = displayRank.ToString();
                yield return null;
            }

            rankText.text = newRank.ToString();
            _currentRank = newRank;
        }

        #endregion
    }

    /// <summary>
    /// Event record for scoreboard updates
    /// </summary>
    public record ScoreboardUpdateEvent();

    /// <summary>
    /// Interface for NetworkPlayerData to provide abstraction and testability
    /// </summary>
    public interface INetworkPlayerData
    {
        INetworkVariable<int> Score { get; }
        INetworkVariable<string> PlayerName { get; }
        INetworkVariable<int> Kills { get; }
        INetworkVariable<int> Deaths { get; }
        INetworkVariable<int> Assists { get; }
        INetworkVariable<int> Ping { get; }
        Sprite PlayerAvatar { get; }
    }

    /// <summary>
    /// Interface for NetworkVariable to provide abstraction
    /// </summary>
    public interface INetworkVariable<T>
    {
        T Value { get; }
        event System.Action<T, T> OnValueChanged;
    }

    /// <summary>
    /// Stub implementation for NetworkPlayerData to resolve compilation errors.
    /// This should be replaced with actual NetworkPlayerData implementation.
    /// </summary>
    public class NetworkPlayerDataStub : INetworkPlayerData
    {
        public class NetworkVariableStub<T> : INetworkVariable<T>
        {
            public T Value { get; set; } = default(T);
            public event System.Action<T, T> OnValueChanged = delegate { };
        }
        
        public INetworkVariable<int> Score { get; } = new NetworkVariableStub<int>();
        public INetworkVariable<string> PlayerName { get; } = new NetworkVariableStub<string>();
        public INetworkVariable<int> Kills { get; } = new NetworkVariableStub<int>();
        public INetworkVariable<int> Deaths { get; } = new NetworkVariableStub<int>();
        public INetworkVariable<int> Assists { get; } = new NetworkVariableStub<int>();
        public INetworkVariable<int> Ping { get; } = new NetworkVariableStub<int>();
        public Sprite PlayerAvatar { get; set; } = null;
    }
}
