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

namespace Game.UI
{
    public class ScoreboardUI : MonoBehaviour, IDisposable
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private Transform playerListContainer = null!;
        [SerializeField] private PlayerRowUI playerRowPrefab = null!; // Prefab assigned in Inspector
        [SerializeField] private int itemsPerPage = 10;
        [SerializeField] private Button nextPageButton = null!;
        [SerializeField] private Button prevPageButton = null!;
        [SerializeField] private TextMeshProUGUI pageIndicatorText = null!;

        #endregion

        #region Private Fields

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

        public enum SortCriterion
        {
            Score,
            Kills,
            Deaths,
            Assists,
            Custom
        }

        #endregion

        #region Unity Methods

        private void Awake()
        {
            nextPageButton.onClick.AddListener(() => ChangePage(_currentPage + 1));
            prevPageButton.onClick.AddListener(() => ChangePage(_currentPage - 1));
        }

        [Inject]
        public void Construct(
            IPublisher<ScoreboardUpdateEvent> scoreboardUpdatePublisher,
            ISubscriber<ScoreboardUpdateEvent> scoreboardUpdateSubscriber)
        {
            _scoreboardUpdatePublisher = scoreboardUpdatePublisher;
            _scoreboardUpdateSubscriber = scoreboardUpdateSubscriber;
        }

        private void OnEnable()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                AddPlayer(client.ClientId);
            }

            _scoreboardUpdateSubscription = _scoreboardUpdateSubscriber.Subscribe(evt =>
            {
                RefreshAllPlayers();
            });

            PeriodicSortLoop().Forget();
        }

        private void OnDisable()
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;

            _scoreboardUpdateSubscription?.Dispose();
            ClearAllPlayers();
        }

        #endregion

        #region Player Management

        private void OnPlayerConnected(ulong clientId) => AddPlayer(clientId);
        private void OnPlayerDisconnected(ulong clientId) => RemovePlayer(clientId);

        private void AddPlayer(ulong clientId)
        {
            if (_playerRows.ContainsKey(clientId)) return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient)) return;
            var playerObj = networkClient.PlayerObject;
            if (playerObj == null) return;

            var playerData = playerObj.GetComponent<NetworkPlayerData>();
            if (playerData == null) return;

            var rowUI = Instantiate(playerRowPrefab, playerListContainer);
            rowUI.Initialize(playerData, this);
            rowUI.Bind(() => _scoreboardUpdatePublisher?.Publish(new ScoreboardUpdateEvent()));

            _playerRows.Add(clientId, rowUI);
            RefreshAllPlayers();
        }

        private void RemovePlayer(ulong clientId)
        {
            if (!_playerRows.TryGetValue(clientId, out var rowUI)) return;

            rowUI.Unbind();
            Destroy(rowUI.gameObject);
            _playerRows.Remove(clientId);

            RefreshAllPlayers();
        }

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

        private async UniTaskVoid PeriodicSortLoop()
        {
            while (true)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5));
                RefreshAllPlayers();
            }
        }

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

        private void UpdatePageIndicator()
        {
            pageIndicatorText.text = $"Page {_currentPage + 1} / {_totalPages}";
            prevPageButton.interactable = _currentPage > 0;
            nextPageButton.interactable = _currentPage < _totalPages - 1;
        }

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

        private void ChangePage(int newPage)
        {
            _currentPage = Mathf.Clamp(newPage, 0, _totalPages - 1);
            UpdatePageIndicator();
            DisplayCurrentPage();
        }

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

        public void Dispose()
        {
            ClearAllPlayers();
            _scoreboardUpdateSubscription?.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Strongly typed player row prefab.
    /// </summary>
    [Serializable]
    public class PlayerRowUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText = null!;
        [SerializeField] private TextMeshProUGUI scoreText = null!;
        [SerializeField] private TextMeshProUGUI rankText = null!;
        [SerializeField] private Image avatarImage = null!;
        [SerializeField] private TextMeshProUGUI pingText = null!;
        [SerializeField] private Image pingBar = null!;

        #endregion

        #region Properties & Private Fields

        public NetworkPlayerData PlayerData { get; private set; } = null!;
        private MonoBehaviour _coroutineRunner = null!;
        private int _currentRank = 0;
        private Coroutine _rankAnimCoroutine;
        private Action _onDataChanged;

        #endregion

        #region Initialization

        public void Initialize(NetworkPlayerData playerData, MonoBehaviour coroutineRunner)
        {
            PlayerData = playerData;
            _coroutineRunner = coroutineRunner;
            UpdateUI();
        }

        public void Bind(Action onDataChanged)
        {
            _onDataChanged = onDataChanged;

            PlayerData.Score.OnValueChanged += OnValueChanged;
            PlayerData.PlayerName.OnValueChanged += OnValueChanged;
            PlayerData.Kills.OnValueChanged += OnValueChanged;
            PlayerData.Deaths.OnValueChanged += OnValueChanged;
            PlayerData.Assists.OnValueChanged += OnValueChanged;
            PlayerData.Ping.OnValueChanged += (_, _) => UpdateUI();
        }

        public void Unbind()
        {
            PlayerData.Score.OnValueChanged = null;
            PlayerData.PlayerName.OnValueChanged = null;
            PlayerData.Kills.OnValueChanged = null;
            PlayerData.Deaths.OnValueChanged = null;
            PlayerData.Assists.OnValueChanged = null;
            PlayerData.Ping.OnValueChanged = null;
        }

        #endregion

        #region UI Updates

        private void OnValueChanged<T>(T oldVal, T newVal)
        {
            UpdateUI();
            _onDataChanged?.Invoke();
        }

        public void UpdateUI()
        {
            nameText.text = PlayerData.PlayerName.Value;
            scoreText.text = PlayerData.Score.Value.ToString();
            pingText.text = $"{PlayerData.Ping.Value} ms";
            UpdatePingBar(PlayerData.Ping.Value);
            // TODO: Avatar loading
        }

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

        public void AnimateRankChange(int newRank)
        {
            if (_rankAnimCoroutine != null)
                _coroutineRunner.StopCoroutine(_rankAnimCoroutine);
            _rankAnimCoroutine = _coroutineRunner.StartCoroutine(AnimateRank(newRank));
        }

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

    public record ScoreboardUpdateEvent();
}
