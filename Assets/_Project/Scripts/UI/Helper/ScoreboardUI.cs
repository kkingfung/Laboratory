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
        [Header("UI References")]
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerRowPrefab;
        [SerializeField] private int itemsPerPage = 10;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button prevPageButton;
        [SerializeField] private TextMeshProUGUI pageIndicatorText;

        private readonly Dictionary<ulong, PlayerRow> _playerRows = new();
        private List<PlayerRow> _sortedPlayers = new();
        private int _currentPage = 0;
        private int _totalPages = 1;

        private IDisposable _scoreboardUpdateSubscription;

        private IPublisher<ScoreboardUpdateEvent> _scoreboardUpdatePublisher;
        private ISubscriber<ScoreboardUpdateEvent> _scoreboardUpdateSubscriber;

        public enum SortCriterion
        {
            Score,
            Kills,
            Deaths,
            Assists,
            Custom // For extensibility
        }

        // Define sorting priority (primary to secondary)
        private SortCriterion[] _sortPriority = new SortCriterion[]
        {
            SortCriterion.Kills,
            SortCriterion.Deaths,
            SortCriterion.Assists,
            SortCriterion.Score
        };

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

            var rowGO = Instantiate(playerRowPrefab, playerListContainer);
            var row = new PlayerRow(rowGO, playerData);

            _playerRows.Add(clientId, row);
            row.Bind(() => _scoreboardUpdatePublisher?.Publish(new ScoreboardUpdateEvent()));

            RefreshAllPlayers();
        }

        private void RemovePlayer(ulong clientId)
        {
            if (!_playerRows.TryGetValue(clientId, out var row)) return;

            row.Unbind();
            Destroy(row.GameObject);
            _playerRows.Remove(clientId);

            RefreshAllPlayers();
        }

        private void ClearAllPlayers()
        {
            foreach (var row in _playerRows.Values)
            {
                row.Unbind();
                Destroy(row.GameObject);
            }
            _playerRows.Clear();
            _sortedPlayers.Clear();
        }

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
                .OrderBy(row => row, Comparer<PlayerRow>.Create(ComparePlayers))
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
                row.GameObject.SetActive(false);
            }

            int start = _currentPage * itemsPerPage;
            int end = Mathf.Min(start + itemsPerPage, _sortedPlayers.Count);

            for (int i = start; i < end; i++)
            {
                var row = _sortedPlayers[i];
                row.GameObject.SetActive(true);
                row.AnimateRankChange(i + 1);
            }
        }

        private void ChangePage(int newPage)
        {
            _currentPage = Mathf.Clamp(newPage, 0, _totalPages - 1);
            UpdatePageIndicator();
            DisplayCurrentPage();
        }

        private int ComparePlayers(PlayerRow a, PlayerRow b)
        {
            foreach (var criterion in _sortPriority)
            {
                int cmp = criterion switch
                {
                    SortCriterion.Score => b.PlayerData.Score.Value.CompareTo(a.PlayerData.Score.Value),
                    SortCriterion.Kills => b.PlayerData.Kills.Value.CompareTo(a.PlayerData.Kills.Value),
                    SortCriterion.Deaths => a.PlayerData.Deaths.Value.CompareTo(b.PlayerData.Deaths.Value), // fewer deaths better
                    SortCriterion.Assists => b.PlayerData.Assists.Value.CompareTo(a.PlayerData.Assists.Value),
                    _ => 0
                };

                if (cmp != 0)
                    return cmp;
            }
            return 0;
        }

        public void Dispose()
        {
            ClearAllPlayers();
            _scoreboardUpdateSubscription?.Dispose();
        }

        private class PlayerRow
        {
            public GameObject GameObject { get; }
            public NetworkPlayerData PlayerData { get; }

            private TextMeshProUGUI _nameText;
            private TextMeshProUGUI _scoreText;
            private TextMeshProUGUI _rankText;
            private Image _avatarImage;
            private TextMeshProUGUI _pingText;
            private Image _pingBar;

            private int _currentRank = 0;
            private Coroutine _rankAnimCoroutine;
            private MonoBehaviour _coroutineRunner;

            public PlayerRow(GameObject go, NetworkPlayerData playerData)
            {
                GameObject = go;
                PlayerData = playerData;

                _nameText = go.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                _scoreText = go.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>();
                _rankText = go.transform.Find("RankText").GetComponent<TextMeshProUGUI>();
                _avatarImage = go.transform.Find("AvatarImage").GetComponent<Image>();
                _pingText = go.transform.Find("PingText").GetComponent<TextMeshProUGUI>();
                _pingBar = go.transform.Find("PingBar").GetComponent<Image>();

                _coroutineRunner = go.GetComponent<MonoBehaviour>() ?? go.AddComponent<MonoBehaviour>();
            }

            public void Bind(Action onDataChanged)
            {
                UpdateUI();

                PlayerData.Score.OnValueChanged += (oldVal, newVal) => { UpdateUI(); onDataChanged?.Invoke(); };
                PlayerData.PlayerName.OnValueChanged += (oldVal, newVal) => { UpdateUI(); onDataChanged?.Invoke(); };
                PlayerData.Kills.OnValueChanged += (oldVal, newVal) => { UpdateUI(); onDataChanged?.Invoke(); };
                PlayerData.Deaths.OnValueChanged += (oldVal, newVal) => { UpdateUI(); onDataChanged?.Invoke(); };
                PlayerData.Assists.OnValueChanged += (oldVal, newVal) => { UpdateUI(); onDataChanged?.Invoke(); };
                PlayerData.Ping.OnValueChanged += (oldVal, newVal) => { UpdateUI(); };
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

            public void UpdateUI()
            {
                _nameText.text = PlayerData.PlayerName.Value.ToString();
                _scoreText.text = PlayerData.Score.Value.ToString();
                _pingText.text = $"{PlayerData.Ping.Value} ms";

                UpdatePingBar(PlayerData.Ping.Value);

                // TODO: Avatar loading (by ID or URL) can be implemented here
            }

            private void UpdatePingBar(int ping)
            {
                int clampedPing = Mathf.Clamp(ping, 0, 300);
                float widthPercent = 1f - (clampedPing / 300f);
                _pingBar.rectTransform.localScale = new Vector3(widthPercent, 1f, 1f);

                if (clampedPing < 75)
                    _pingBar.color = Color.green;
                else if (clampedPing < 150)
                    _pingBar.color = Color.yellow;
                else
                    _pingBar.color = Color.red;
            }

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
                int endRank = newRank;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    int displayRank = Mathf.RoundToInt(Mathf.Lerp(startRank, endRank, t));
                    _rankText.text = displayRank.ToString();
                    yield return null;
                }

                _rankText.text = newRank.ToString();
                _currentRank = newRank;
            }
        }
    }

    public record ScoreboardUpdateEvent();
}
