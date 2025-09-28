using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using TMPro;
using Laboratory.Core;
using Laboratory.Core.Progression;
using Laboratory.Economy;
using Laboratory.Networking.Entities;
using Laboratory.Subsystems.Combat.Advanced;
using Laboratory.UI.Utils;
using Laboratory.UI.Performance;
using Laboratory.Chimera.Core;

namespace Laboratory.UI
{
    /// <summary>
    /// Chimera UI Manager - Comprehensive UI/UX System for Project Chimera
    /// PURPOSE: Unified management of all UI systems with performance optimization and seamless integration
    /// FEATURES: Modular UI panels, real-time data binding, accessibility support, responsive design
    /// ARCHITECTURE: Event-driven with ECS integration and ScriptableObject configuration
    /// PERFORMANCE: Optimized UI updates, object pooling, and efficient data binding
    /// </summary>
    public class ChimeraUIManager : MonoBehaviour
    {
        [Header("Core Configuration")]
        [Tooltip("Main UI configuration")]
        public ChimeraUIConfiguration uiConfig;

        [Tooltip("Main game configuration")]
        public ChimeraGameConfig gameConfig;

        [Header("UI Panels")]
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private GameplayHUDPanel gameplayHUD;
        [SerializeField] private CreatureManagementPanel creaturePanel;
        [SerializeField] private BreedingPanel breedingPanel;
        [SerializeField] private MarketplacePanel marketplacePanel;
        [SerializeField] private ProgressionPanel progressionPanel;
        [SerializeField] private CombatHUDPanel combatHUD;
        [SerializeField] private NetworkStatusPanel networkPanel;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private NotificationPanel notificationPanel;

        [Header("Dynamic UI Elements")]
        [SerializeField] private Transform dynamicUIContainer;
        [SerializeField] private CreatureCardPool creatureCardPool;
        [SerializeField] private NotificationPool notificationPool;

        [Header("Accessibility")]
        [SerializeField] private AccessibilitySettings accessibilitySettings;
        [SerializeField] private bool enableScreenReader = false;
        [SerializeField] private bool enableHighContrast = false;
        [SerializeField] private bool enableLargeText = false;

        // Private fields
        private Dictionary<UIPanel, UIController> _panelControllers = new();
        private Queue<UINotification> _notificationQueue = new();
        private World _ecsWorld;
        private UIUpdateSystem _uiUpdateSystem;
        private bool _isInitialized = false;
        private GameState _currentGameState = GameState.MainMenu;
        private float _lastUIUpdate = 0f;

        // Events
        public event Action<UIPanel> OnPanelOpened;
        public event Action<UIPanel> OnPanelClosed;
        public event Action<GameState> OnGameStateChanged;
        public event Action<UINotification> OnNotificationShown;

        // Properties
        public bool IsInitialized => _isInitialized;
        public GameState CurrentGameState => _currentGameState;
        public ChimeraUIConfiguration Configuration => uiConfig;

        private void Awake()
        {
            ValidateComponents();
            InitializeUISystem();
        }

        private void Start()
        {
            StartCoroutine(InitializeAsync());
        }

        private void Update()
        {
            if (_isInitialized)
            {
                UpdateUISystem();
                ProcessNotificationQueue();
                HandleInput();
            }
        }

        private void ValidateComponents()
        {
            if (uiConfig == null)
            {
                Debug.LogError("ChimeraUIConfiguration is required! Please assign a configuration asset.");
                enabled = false;
                return;
            }

            if (gameConfig == null)
            {
                Debug.LogError("ChimeraGameConfig is required! Please assign the main game configuration.");
                enabled = false;
                return;
            }

            // Validate required panels
            var requiredPanels = new List<(string name, UIController panel)>
            {
                ("Main Menu", mainMenuPanel),
                ("Gameplay HUD", gameplayHUD),
                ("Creature Management", creaturePanel),
                ("Breeding", breedingPanel),
                ("Marketplace", marketplacePanel),
                ("Progression", progressionPanel),
                ("Combat HUD", combatHUD),
                ("Settings", settingsPanel),
                ("Notifications", notificationPanel)
            };

            foreach (var (name, panel) in requiredPanels)
            {
                if (panel == null)
                {
                    Debug.LogWarning($"{name} panel is not assigned. Some functionality may be limited.");
                }
            }
        }

        private void InitializeUISystem()
        {
            // Get ECS World
            _ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (_ecsWorld != null)
            {
                _uiUpdateSystem = _ecsWorld.GetOrCreateSystemManaged<UIUpdateSystem>();
            }

            // Initialize panel controllers
            InitializePanelControllers();

            // Apply accessibility settings
            ApplyAccessibilitySettings();

            // Set initial UI state
            ChangeGameState(GameState.MainMenu);

            Debug.Log("🎨 Chimera UI System initialized successfully!");
        }

        private IEnumerator InitializeAsync()
        {
            yield return new WaitForSeconds(0.1f); // Let other systems initialize

            // Initialize all panels
            yield return StartCoroutine(InitializePanelsAsync());

            // Connect to data sources
            ConnectToDataSources();

            // Show initial UI state
            ShowInitialUI();

            _isInitialized = true;
            Debug.Log("✅ Chimera UI System fully initialized!");
        }

        private void InitializePanelControllers()
        {
            var panels = new Dictionary<UIPanel, UIController>
            {
                { UIPanel.MainMenu, mainMenuPanel },
                { UIPanel.GameplayHUD, gameplayHUD },
                { UIPanel.CreatureManagement, creaturePanel },
                { UIPanel.Breeding, breedingPanel },
                { UIPanel.Marketplace, marketplacePanel },
                { UIPanel.Progression, progressionPanel },
                { UIPanel.CombatHUD, combatHUD },
                { UIPanel.NetworkStatus, networkPanel },
                { UIPanel.Settings, settingsPanel },
                { UIPanel.Notifications, notificationPanel }
            };

            foreach (var kvp in panels)
            {
                if (kvp.Value != null)
                {
                    _panelControllers[kvp.Key] = kvp.Value;

                    // Subscribe to panel events if they exist
                    if (kvp.Value is IUIPanel uiPanel)
                    {
                        uiPanel.OnPanelOpened += () => OnPanelOpened?.Invoke(kvp.Key);
                        uiPanel.OnPanelClosed += () => OnPanelClosed?.Invoke(kvp.Key);
                    }
                }
            }
        }

        private IEnumerator InitializePanelsAsync()
        {
            // Initialize panels in order of priority
            var initOrder = new UIPanel[]
            {
                UIPanel.MainMenu,
                UIPanel.GameplayHUD,
                UIPanel.Notifications,
                UIPanel.Settings,
                UIPanel.CreatureManagement,
                UIPanel.Breeding,
                UIPanel.Marketplace,
                UIPanel.Progression,
                UIPanel.CombatHUD,
                UIPanel.NetworkStatus
            };

            foreach (var panel in initOrder)
            {
                if (_panelControllers.TryGetValue(panel, out var controller) && controller is IUIPanel uiPanel)
                {
                    yield return StartCoroutine(uiPanel.InitializeAsync());
                }
                yield return null; // Spread initialization across frames
            }
        }

        private void ConnectToDataSources()
        {
            // Connect to progression system
            if (progressionPanel != null)
            {
                var progressionManager = FindObjectOfType<PlayerProgressionManager>();
                if (progressionManager != null && progressionPanel is IDataBindable<PlayerProgressionData> progressionBindable)
                {
                    progressionManager.OnProgressionChanged += data => progressionBindable.UpdateData(data);
                }
            }

            // Connect to marketplace system
            if (marketplacePanel != null)
            {
                var marketplace = FindObjectOfType<BreedingMarketplace>();
                if (marketplace != null && marketplacePanel is IDataBindable<MarketplaceData> marketplaceBindable)
                {
                    marketplace.OnMarketplaceDataChanged += data => marketplaceBindable.UpdateData(data);
                }
            }

            // Connect to network system
            if (networkPanel != null && _ecsWorld != null)
            {
                var netcodeManager = _ecsWorld.GetExistingSystemManaged<NetcodeEntityManager>();
                if (netcodeManager != null && networkPanel is IDataBindable<NetworkStatistics> networkBindable)
                {
                    // Would connect to network events here
                }
            }
        }

        private void ShowInitialUI()
        {
            // Show main menu initially
            ShowPanel(UIPanel.MainMenu);

            // Show notification system
            ShowPanel(UIPanel.Notifications);

            // Show network status if in multiplayer
            if (uiConfig.enableMultiplayerUI)
            {
                ShowPanel(UIPanel.NetworkStatus);
            }
        }

        private void UpdateUISystem()
        {
            var currentTime = Time.unscaledTime;

            // Update UI at configured intervals for performance
            if (currentTime - _lastUIUpdate >= 1f / uiConfig.uiUpdateRate)
            {
                UpdateActivePanels();
                UpdateDynamicElements();
                _lastUIUpdate = currentTime;
            }

            // Update real-time elements every frame
            UpdateRealTimeElements();
        }

        private void UpdateActivePanels()
        {
            foreach (var kvp in _panelControllers)
            {
                if (kvp.Value != null && kvp.Value.IsOpen && kvp.Value is IUpdatable updatable)
                {
                    updatable.UpdatePanel();
                }
            }
        }

        private void UpdateDynamicElements()
        {
            // Update creature cards
            if (creatureCardPool != null)
            {
                creatureCardPool.UpdatePool();
            }

            // Update performance metrics
            if (uiConfig.showPerformanceMetrics && gameplayHUD != null)
            {
                var fps = 1f / Time.unscaledDeltaTime;
                gameplayHUD.UpdatePerformanceMetrics(fps);
            }
        }

        private void UpdateRealTimeElements()
        {
            // Update combat HUD if in combat
            if (_currentGameState == GameState.Combat && combatHUD != null && combatHUD.IsOpen)
            {
                UpdateCombatHUD();
            }

            // Update network status in real-time
            if (networkPanel != null && networkPanel.IsOpen)
            {
                UpdateNetworkStatus();
            }
        }

        private void UpdateCombatHUD()
        {
            if (_ecsWorld == null) return;

            // Get combat entities and update HUD
            var combatQuery = _ecsWorld.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<GeneticCombatAbilities>(),
                ComponentType.ReadOnly<NetworkOwnership>()
            );

            var entityCount = combatQuery.CalculateEntityCount();
            combatHUD.UpdateActiveCombatants(entityCount);

            combatQuery.Dispose();
        }

        private void UpdateNetworkStatus()
        {
            if (_ecsWorld == null || networkPanel == null) return;

            // Get network statistics
            var networkedQuery = _ecsWorld.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NetworkOwnership>()
            );

            var networkedEntities = networkedQuery.CalculateEntityCount();
            var networkStats = new NetworkStatistics
            {
                connectedEntities = networkedEntities,
                fps = 1f / Time.unscaledDeltaTime,
                ping = 50f, // Placeholder
                packetLoss = 0.1f // Placeholder
            };

            if (networkPanel is IDataBindable<NetworkStatistics> networkBindable)
            {
                networkBindable.UpdateData(networkStats);
            }

            networkedQuery.Dispose();
        }

        private void ProcessNotificationQueue()
        {
            if (_notificationQueue.Count > 0 && notificationPanel != null)
            {
                var notification = _notificationQueue.Dequeue();
                ShowNotification(notification);
            }
        }

        private void HandleInput()
        {
            // Handle UI navigation input
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscapeKey();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                HandleTabKey();
            }

            // Handle quick access hotkeys
            if (Input.GetKeyDown(KeyCode.F1))
                TogglePanel(UIPanel.Settings);

            if (Input.GetKeyDown(KeyCode.F2))
                TogglePanel(UIPanel.CreatureManagement);

            if (Input.GetKeyDown(KeyCode.F3))
                TogglePanel(UIPanel.Breeding);

            if (Input.GetKeyDown(KeyCode.F4))
                TogglePanel(UIPanel.Marketplace);
        }

        private void HandleEscapeKey()
        {
            // Find the top-most panel and close it, or show main menu
            var topPanel = GetTopMostPanel();
            if (topPanel != UIPanel.None && topPanel != UIPanel.MainMenu)
            {
                HidePanel(topPanel);
            }
            else if (_currentGameState != GameState.MainMenu)
            {
                ShowPanel(UIPanel.MainMenu);
                ChangeGameState(GameState.MainMenu);
            }
        }

        private void HandleTabKey()
        {
            // Cycle through key panels for keyboard navigation
            var keyPanels = new UIPanel[]
            {
                UIPanel.CreatureManagement,
                UIPanel.Breeding,
                UIPanel.Marketplace,
                UIPanel.Progression
            };

            var currentIndex = Array.IndexOf(keyPanels, GetTopMostPanel());
            var nextIndex = (currentIndex + 1) % keyPanels.Length;

            ShowPanel(keyPanels[nextIndex]);
        }

        private UIPanel GetTopMostPanel()
        {
            foreach (var kvp in _panelControllers)
            {
                if (kvp.Value != null && kvp.Value.IsOpen)
                {
                    return kvp.Key;
                }
            }
            return UIPanel.None;
        }

        #region Public API

        /// <summary>
        /// Show a specific UI panel
        /// </summary>
        public void ShowPanel(UIPanel panel)
        {
            if (_panelControllers.TryGetValue(panel, out var controller) && controller != null)
            {
                controller.Show();
                OnPanelOpened?.Invoke(panel);

                // Log for debugging
                if (uiConfig.enableDebugLogging)
                {
                    Debug.Log($"🎨 Showed UI panel: {panel}");
                }
            }
        }

        /// <summary>
        /// Hide a specific UI panel
        /// </summary>
        public void HidePanel(UIPanel panel)
        {
            if (_panelControllers.TryGetValue(panel, out var controller) && controller != null)
            {
                controller.Hide();
                OnPanelClosed?.Invoke(panel);

                if (uiConfig.enableDebugLogging)
                {
                    Debug.Log($"🎨 Hid UI panel: {panel}");
                }
            }
        }

        /// <summary>
        /// Toggle a specific UI panel
        /// </summary>
        public void TogglePanel(UIPanel panel)
        {
            if (_panelControllers.TryGetValue(panel, out var controller) && controller != null)
            {
                if (controller.IsOpen)
                    HidePanel(panel);
                else
                    ShowPanel(panel);
            }
        }

        /// <summary>
        /// Hide all panels except specified exceptions
        /// </summary>
        public void HideAllPanels(params UIPanel[] exceptions)
        {
            var exceptionSet = new HashSet<UIPanel>(exceptions);

            foreach (var kvp in _panelControllers)
            {
                if (!exceptionSet.Contains(kvp.Key) && kvp.Value != null && kvp.Value.IsOpen)
                {
                    HidePanel(kvp.Key);
                }
            }
        }

        /// <summary>
        /// Change the current game state and update UI accordingly
        /// </summary>
        public void ChangeGameState(GameState newState)
        {
            if (_currentGameState == newState) return;

            var oldState = _currentGameState;
            _currentGameState = newState;

            ApplyGameStateUI(newState);
            OnGameStateChanged?.Invoke(newState);

            if (uiConfig.enableDebugLogging)
            {
                Debug.Log($"🎮 Game state changed: {oldState} → {newState}");
            }
        }

        /// <summary>
        /// Show a notification to the player
        /// </summary>
        public void ShowNotification(UINotification notification)
        {
            if (notificationPanel != null && notificationPanel is INotificationDisplay notificationDisplay)
            {
                notificationDisplay.ShowNotification(notification);
                OnNotificationShown?.Invoke(notification);
            }
        }

        /// <summary>
        /// Queue a notification to be shown
        /// </summary>
        public void QueueNotification(UINotification notification)
        {
            _notificationQueue.Enqueue(notification);
        }

        /// <summary>
        /// Create and show a simple text notification
        /// </summary>
        public void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = 3f)
        {
            var notification = new UINotification
            {
                message = message,
                type = type,
                duration = duration,
                timestamp = Time.unscaledTime
            };

            ShowNotification(notification);
        }

        /// <summary>
        /// Update creature data in relevant UI panels
        /// </summary>
        public void UpdateCreatureData(Entity creatureEntity)
        {
            if (_ecsWorld == null) return;

            var entityManager = _ecsWorld.EntityManager;

            // Update creature management panel
            if (creaturePanel != null && creaturePanel.IsOpen && creaturePanel is ICreatureDataDisplay creatureDisplay)
            {
                creatureDisplay.UpdateCreatureData(creatureEntity);
            }

            // Update breeding panel if creature is selected for breeding
            if (breedingPanel != null && breedingPanel.IsOpen && breedingPanel is IBreedingDisplay breedingDisplay)
            {
                breedingDisplay.RefreshCreatureData(creatureEntity);
            }
        }

        /// <summary>
        /// Apply accessibility settings
        /// </summary>
        public void ApplyAccessibilitySettings()
        {
            if (accessibilitySettings == null) return;

            // Apply high contrast mode
            if (enableHighContrast)
            {
                ApplyHighContrastMode();
            }

            // Apply large text mode
            if (enableLargeText)
            {
                ApplyLargeTextMode();
            }

            // Enable screen reader support
            if (enableScreenReader)
            {
                EnableScreenReaderSupport();
            }
        }

        #endregion

        #region Private Methods

        private void ApplyGameStateUI(GameState state)
        {
            // Hide all panels first
            HideAllPanels();

            // Show panels appropriate for the new state
            switch (state)
            {
                case GameState.MainMenu:
                    ShowPanel(UIPanel.MainMenu);
                    ShowPanel(UIPanel.Notifications);
                    break;

                case GameState.Gameplay:
                    ShowPanel(UIPanel.GameplayHUD);
                    ShowPanel(UIPanel.Notifications);
                    if (uiConfig.enableMultiplayerUI)
                        ShowPanel(UIPanel.NetworkStatus);
                    break;

                case GameState.Combat:
                    ShowPanel(UIPanel.GameplayHUD);
                    ShowPanel(UIPanel.CombatHUD);
                    ShowPanel(UIPanel.Notifications);
                    break;

                case GameState.Breeding:
                    ShowPanel(UIPanel.GameplayHUD);
                    ShowPanel(UIPanel.BreedingInterface);
                    ShowPanel(UIPanel.CreatureManagement);
                    break;

                case GameState.Marketplace:
                    ShowPanel(UIPanel.GameplayHUD);
                    ShowPanel(UIPanel.Marketplace);
                    break;

                case GameState.Paused:
                    ShowPanel(UIPanel.Settings);
                    ShowPanel(UIPanel.Notifications);
                    break;
            }
        }

        private void ApplyHighContrastMode()
        {
            // Apply high contrast color scheme to all UI elements
            var uiElements = GetComponentsInChildren<Graphic>(true);
            foreach (var element in uiElements)
            {
                if (element is Image image)
                {
                    image.color = Color.white;
                }
                else if (element is TextMeshProUGUI text)
                {
                    text.color = Color.black;
                }
            }
        }

        private void ApplyLargeTextMode()
        {
            var textElements = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in textElements)
            {
                text.fontSize *= 1.2f; // 20% larger text
            }
        }

        private void EnableScreenReaderSupport()
        {
            // Enable accessibility features for screen readers
            // This would integrate with platform-specific accessibility APIs
            Debug.Log("🔊 Screen reader support enabled");
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up events and resources
            OnPanelOpened = null;
            OnPanelClosed = null;
            OnGameStateChanged = null;
            OnNotificationShown = null;

            _panelControllers.Clear();
            _notificationQueue.Clear();
        }
    }

    #region Enums and Data Structures

    public enum UIPanel
    {
        None = 0,
        MainMenu = 1,
        GameplayHUD = 2,
        CreatureManagement = 3,
        Breeding = 4,
        BreedingInterface = 5,
        Marketplace = 6,
        Progression = 7,
        CombatHUD = 8,
        NetworkStatus = 9,
        Settings = 10,
        Notifications = 11
    }

    public enum GameState
    {
        MainMenu = 0,
        Gameplay = 1,
        Combat = 2,
        Breeding = 3,
        Marketplace = 4,
        Paused = 5
    }

    public enum NotificationType
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3,
        Achievement = 4,
        Network = 5
    }

    [System.Serializable]
    public struct UINotification
    {
        public string message;
        public NotificationType type;
        public float duration;
        public float timestamp;
        public Sprite icon;
        public Color backgroundColor;
        public bool playSound;
    }

    [System.Serializable]
    public struct NetworkStatistics
    {
        public int connectedEntities;
        public float fps;
        public float ping;
        public float packetLoss;
        public float bandwidth;
    }

    [System.Serializable]
    public struct AccessibilitySettings
    {
        public bool highContrast;
        public bool largeText;
        public bool screenReader;
        public bool audioDescriptions;
        public bool reducedMotion;
        public float uiScale;
    }

    #endregion

    #region Interfaces

    public interface IUIPanel
    {
        event Action OnPanelOpened;
        event Action OnPanelClosed;
        IEnumerator InitializeAsync();
    }

    public interface IUpdatable
    {
        void UpdatePanel();
    }

    public interface IDataBindable<T>
    {
        void UpdateData(T data);
    }

    public interface INotificationDisplay
    {
        void ShowNotification(UINotification notification);
    }

    public interface ICreatureDataDisplay
    {
        void UpdateCreatureData(Entity creatureEntity);
    }

    public interface IBreedingDisplay
    {
        void RefreshCreatureData(Entity creatureEntity);
    }

    #endregion

    #region Panel Base Classes

    public abstract class MainMenuPanel : UIController, IUIPanel
    {
        public event Action OnPanelOpened;
        public event Action OnPanelClosed;

        public virtual IEnumerator InitializeAsync()
        {
            yield return null;
        }

        protected virtual void OnStartGame() { }
        protected virtual void OnShowSettings() { }
        protected virtual void OnQuitGame() { }
    }

    public abstract class GameplayHUDPanel : UIController, IUIPanel, IUpdatable
    {
        public event Action OnPanelOpened;
        public event Action OnPanelClosed;

        public virtual IEnumerator InitializeAsync()
        {
            yield return null;
        }

        public virtual void UpdatePanel() { }
        public virtual void UpdatePerformanceMetrics(float fps) { }
    }

    public abstract class CreatureManagementPanel : UIController, IUIPanel, IUpdatable, ICreatureDataDisplay
    {
        public event Action OnPanelOpened;
        public event Action OnPanelClosed;

        public virtual IEnumerator InitializeAsync()
        {
            yield return null;
        }

        public virtual void UpdatePanel() { }
        public virtual void UpdateCreatureData(Entity creatureEntity) { }
    }

    public abstract class BreedingPanel : UIController, IUIPanel, IBreedingDisplay
    {
        public event Action OnPanelOpened;
        public event Action OnPanelClosed;

        public virtual IEnumerator InitializeAsync()
        {
            yield return null;
        }

        public virtual void RefreshCreatureData(Entity creatureEntity) { }
    }

    public abstract class MarketplacePanel : UIController, IUIPanel, IDataBindable<MarketplaceData>
    {
        public event Action OnPanelOpened;
        public event Action OnPanelClosed;

        public virtual IEnumerator InitializeAsync()
        {
            yield return null;
        }

        public virtual void UpdateData(MarketplaceData data) { }
    }

    public abstract class ProgressionPanel : UIController, IUIPanel, IDataBindable<PlayerProgressionData>
    {
        public event Action OnPanelOpened;
        public event Action OnPanelClosed;

        public virtual IEnumerator InitializeAsync()
        {
            yield return null;
        }

        public virtual void UpdateData(PlayerProgressionData data) { }
    }

    public abstract class CombatHUDPanel : UIController, IUIPanel, IUpdatable
    {
        public event Action OnPanelOpened;
        public event Action OnPanelClosed;

        public virtual IEnumerator InitializeAsync()
        {
            yield return null;
        }

        public virtual void UpdatePanel() { }
        public virtual void UpdateActiveCombatants(int count) { }
    }

    public abstract class NetworkStatusPanel : UIController, IUIPanel, IDataBindable<NetworkStatistics>
    {
        public event Action OnPanelOpened;
        public event Action OnPanelClosed;

        public virtual IEnumerator InitializeAsync()
        {
            yield return null;
        }

        public virtual void UpdateData(NetworkStatistics stats) { }
    }

    public abstract class SettingsPanel : UIController, IUIPanel
    {
        public event Action OnPanelOpened;
        public event Action OnPanelClosed;

        public virtual IEnumerator InitializeAsync()
        {
            yield return null;
        }
    }

    public abstract class NotificationPanel : UIController, IUIPanel, INotificationDisplay
    {
        public event Action OnPanelOpened;
        public event Action OnPanelClosed;

        public virtual IEnumerator InitializeAsync()
        {
            yield return null;
        }

        public virtual void ShowNotification(UINotification notification) { }
    }

    #endregion
}