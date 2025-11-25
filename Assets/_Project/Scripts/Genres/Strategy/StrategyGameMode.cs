using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Genres.Strategy
{
    /// <summary>
    /// Strategy RTS game mode manager
    /// Handles resources, unit selection, and victory conditions
    /// </summary>
    public class StrategyGameMode : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private StrategyConfig config;

        [Header("Players")]
        [SerializeField] private int playerTeamId = 0;
        [SerializeField] private List<int> aiTeamIds = new List<int> { 1 };

        // Resources per team
        private Dictionary<int, PlayerResources> _teamResources = new Dictionary<int, PlayerResources>();

        // Unit tracking
        private List<RTSUnit> _allUnits = new List<RTSUnit>();
        private List<RTSUnit> _selectedUnits = new List<RTSUnit>();

        // Selection
        private Vector3 _selectionStartPos;
        private bool _isSelecting;

        // Game state
        private bool _isGameActive;
        private int _winningTeam = -1;

        // Events
        public event System.Action<int, PlayerResources> OnResourcesChanged; // teamId, resources
        public event System.Action<List<RTSUnit>> OnSelectionChanged;
        public event System.Action<int> OnVictory; // winning team

        private void Start()
        {
            InitializeGame();
        }

        private void Update()
        {
            if (!_isGameActive) return;

            HandleInput();
            CheckVictoryConditions();
        }

        /// <summary>
        /// Initialize game
        /// </summary>
        private void InitializeGame()
        {
            if (config == null)
            {
                Debug.LogError("[StrategyGameMode] No config assigned!");
                return;
            }

            // Initialize resources for all teams
            _teamResources[playerTeamId] = new PlayerResources
            {
                Gold = config.StartingGold,
                Food = config.StartingFood,
                Wood = config.StartingWood,
                Population = 0,
                PopulationLimit = config.PopulationLimit
            };

            foreach (int aiTeam in aiTeamIds)
            {
                int bonus = config.AIStartingAdvantage;
                _teamResources[aiTeam] = new PlayerResources
                {
                    Gold = config.StartingGold + (config.StartingGold * bonus / 100),
                    Food = config.StartingFood + (config.StartingFood * bonus / 100),
                    Wood = config.StartingWood + (config.StartingWood * bonus / 100),
                    Population = 0,
                    PopulationLimit = config.PopulationLimit
                };
            }

            _isGameActive = true;

            Debug.Log($"[StrategyGameMode] Game initialized. Player team: {playerTeamId}");
        }

        /// <summary>
        /// Handle player input
        /// </summary>
        private void HandleInput()
        {
            // Left click - Selection
            if (Input.GetMouseButtonDown(0))
            {
                StartSelection();
            }

            if (Input.GetMouseButton(0) && _isSelecting)
            {
                UpdateSelection();
            }

            if (Input.GetMouseButtonUp(0))
            {
                EndSelection();
            }

            // Right click - Move/Attack command
            if (Input.GetMouseButtonDown(1) && _selectedUnits.Count > 0)
            {
                IssueCommand();
            }
        }

        /// <summary>
        /// Start selection
        /// </summary>
        private void StartSelection()
        {
            _selectionStartPos = Input.mousePosition;
            _isSelecting = true;

            // Single click selection
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                RTSUnit unit = hit.collider.GetComponent<RTSUnit>();
                if (unit != null && unit.GetTeamId() == playerTeamId)
                {
                    // If shift not held, clear selection
                    if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                    {
                        ClearSelection();
                    }

                    SelectUnit(unit);
                }
                else if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    ClearSelection();
                }
            }
        }

        /// <summary>
        /// Update selection (drag selection)
        /// </summary>
        private void UpdateSelection()
        {
            // Box selection logic would go here
            // For simplicity, skipping drag selection implementation
        }

        /// <summary>
        /// End selection
        /// </summary>
        private void EndSelection()
        {
            _isSelecting = false;
        }

        /// <summary>
        /// Issue command to selected units
        /// </summary>
        private void IssueCommand()
        {
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check if clicking on enemy unit (attack)
                RTSUnit targetUnit = hit.collider.GetComponent<RTSUnit>();
                if (targetUnit != null && targetUnit.GetTeamId() != playerTeamId)
                {
                    // Attack command
                    foreach (RTSUnit unit in _selectedUnits)
                    {
                        unit.AttackUnit(targetUnit);
                    }

                    Debug.Log($"[StrategyGameMode] Attack command issued to {_selectedUnits.Count} units");
                }
                else
                {
                    // Move command
                    Vector3 targetPosition = hit.point;
                    foreach (RTSUnit unit in _selectedUnits)
                    {
                        unit.MoveToPosition(targetPosition);
                    }

                    Debug.Log($"[StrategyGameMode] Move command issued to {_selectedUnits.Count} units");
                }
            }
        }

        /// <summary>
        /// Select unit
        /// </summary>
        private void SelectUnit(RTSUnit unit)
        {
            if (!_selectedUnits.Contains(unit))
            {
                _selectedUnits.Add(unit);
                unit.Select();
            }

            OnSelectionChanged?.Invoke(_selectedUnits);
        }

        /// <summary>
        /// Clear selection
        /// </summary>
        private void ClearSelection()
        {
            foreach (RTSUnit unit in _selectedUnits)
            {
                unit.Deselect();
            }

            _selectedUnits.Clear();
            OnSelectionChanged?.Invoke(_selectedUnits);
        }

        /// <summary>
        /// Register unit
        /// </summary>
        public void RegisterUnit(RTSUnit unit)
        {
            if (!_allUnits.Contains(unit))
            {
                _allUnits.Add(unit);
                unit.OnDeath += HandleUnitDeath;
            }
        }

        /// <summary>
        /// Handle unit death
        /// </summary>
        private void HandleUnitDeath(RTSUnit unit)
        {
            _allUnits.Remove(unit);
            _selectedUnits.Remove(unit);

            OnSelectionChanged?.Invoke(_selectedUnits);
        }

        /// <summary>
        /// Add resources to team
        /// </summary>
        public void AddResources(int teamId, int gold, int food, int wood)
        {
            if (!_teamResources.ContainsKey(teamId)) return;

            PlayerResources resources = _teamResources[teamId];
            resources.Gold += gold;
            resources.Food += food;
            resources.Wood += wood;

            OnResourcesChanged?.Invoke(teamId, resources);
        }

        /// <summary>
        /// Spend resources for team
        /// </summary>
        public bool SpendResources(int teamId, int gold, int food, int wood)
        {
            if (!_teamResources.ContainsKey(teamId)) return false;

            PlayerResources resources = _teamResources[teamId];

            // Check if enough resources
            if (resources.Gold < gold || resources.Food < food || resources.Wood < wood)
            {
                return false;
            }

            resources.Gold -= gold;
            resources.Food -= food;
            resources.Wood -= wood;

            OnResourcesChanged?.Invoke(teamId, resources);
            return true;
        }

        /// <summary>
        /// Check victory conditions
        /// </summary>
        private void CheckVictoryConditions()
        {
            if (config == null || !_isGameActive) return;

            // Domination victory - eliminate all enemy units
            if (config.EnableDomination)
            {
                foreach (int teamId in _teamResources.Keys)
                {
                    int enemyCount = _allUnits.Count(u => u.GetTeamId() != teamId);
                    if (enemyCount == 0)
                    {
                        Victory(teamId);
                        return;
                    }
                }
            }

            // Economy victory - reach resource threshold
            if (config.EnableEconomy)
            {
                foreach (var kvp in _teamResources)
                {
                    PlayerResources resources = kvp.Value;
                    int totalResources = resources.Gold + resources.Food + resources.Wood;

                    if (totalResources >= config.EconomyVictoryThreshold)
                    {
                        Victory(kvp.Key);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Trigger victory
        /// </summary>
        private void Victory(int winningTeam)
        {
            _isGameActive = false;
            _winningTeam = winningTeam;

            OnVictory?.Invoke(winningTeam);

            string teamName = winningTeam == playerTeamId ? "Player" : $"AI Team {winningTeam}";
            Debug.Log($"[StrategyGameMode] {teamName} wins!");
        }

        // Getters
        public PlayerResources GetTeamResources(int teamId)
        {
            return _teamResources.ContainsKey(teamId) ? _teamResources[teamId] : null;
        }

        public List<RTSUnit> GetSelectedUnits() => _selectedUnits;
        public int GetWinningTeam() => _winningTeam;
        public bool IsGameActive() => _isGameActive;
    }

    /// <summary>
    /// Player resources structure
    /// </summary>
    public class PlayerResources
    {
        public int Gold;
        public int Food;
        public int Wood;
        public int Population;
        public int PopulationLimit;
    }
}
