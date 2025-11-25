using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Laboratory.Genres.Strategy
{
    /// <summary>
    /// Strategy RTS UI displaying resources, selected units, and game state
    /// </summary>
    public class StrategyUI : MonoBehaviour
    {
        [Header("Resource Display")]
        [SerializeField] private Text goldText;
        [SerializeField] private Text foodText;
        [SerializeField] private Text woodText;
        [SerializeField] private Text populationText;

        [Header("Selection Display")]
        [SerializeField] private Text selectionText;
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Text selectionDetailsText;

        [Header("Game State")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Text victoryText;
        [SerializeField] private Text gameStatusText;

        [Header("Minimap")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private Camera minimapCamera;

        [Header("References")]
        [SerializeField] private StrategyGameMode gameMode;
        [SerializeField] private int playerTeamId = 0;

        private void Start()
        {
            // Find game mode if not assigned
            if (gameMode == null)
            {
                gameMode = FindFirstObjectByType<StrategyGameMode>();
            }

            // Subscribe to events
            if (gameMode != null)
            {
                gameMode.OnResourcesChanged += HandleResourcesChanged;
                gameMode.OnSelectionChanged += HandleSelectionChanged;
                gameMode.OnVictory += HandleVictory;
            }

            // Hide panels
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }

            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }

            // Setup minimap
            SetupMinimap();

            // Initial display
            UpdateDisplay();
        }

        private void Update()
        {
            UpdateGameStatusDisplay();
        }

        /// <summary>
        /// Setup minimap camera
        /// </summary>
        private void SetupMinimap()
        {
            if (minimapCamera != null && minimapImage != null)
            {
                // Create render texture for minimap
                RenderTexture minimapRT = new RenderTexture(256, 256, 16);
                minimapCamera.targetTexture = minimapRT;
                minimapImage.texture = minimapRT;

                // Position camera above map
                minimapCamera.transform.position = new Vector3(0f, 50f, 0f);
                minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                minimapCamera.orthographic = true;
                minimapCamera.orthographicSize = 50f;
            }
        }

        /// <summary>
        /// Update all display elements
        /// </summary>
        private void UpdateDisplay()
        {
            UpdateResourceDisplay();
            UpdateSelectionDisplay();
        }

        /// <summary>
        /// Update resource display
        /// </summary>
        private void UpdateResourceDisplay()
        {
            if (gameMode == null) return;

            PlayerResources resources = gameMode.GetTeamResources(playerTeamId);
            if (resources == null) return;

            if (goldText != null)
            {
                goldText.text = $"Gold: {resources.Gold}";
            }

            if (foodText != null)
            {
                foodText.text = $"Food: {resources.Food}";
            }

            if (woodText != null)
            {
                woodText.text = $"Wood: {resources.Wood}";
            }

            if (populationText != null)
            {
                populationText.text = $"Population: {resources.Population}/{resources.PopulationLimit}";

                // Color code if near limit
                if (resources.Population >= resources.PopulationLimit)
                {
                    populationText.color = Color.red;
                }
                else if (resources.Population >= resources.PopulationLimit * 0.8f)
                {
                    populationText.color = Color.yellow;
                }
                else
                {
                    populationText.color = Color.white;
                }
            }
        }

        /// <summary>
        /// Update selection display
        /// </summary>
        private void UpdateSelectionDisplay()
        {
            if (gameMode == null) return;

            List<RTSUnit> selectedUnits = gameMode.GetSelectedUnits();

            if (selectedUnits == null || selectedUnits.Count == 0)
            {
                if (selectionPanel != null)
                {
                    selectionPanel.SetActive(false);
                }

                if (selectionText != null)
                {
                    selectionText.text = "";
                }
                return;
            }

            // Show selection panel
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }

            // Update selection count
            if (selectionText != null)
            {
                selectionText.text = $"Selected: {selectedUnits.Count}";
            }

            // Update selection details
            if (selectionDetailsText != null)
            {
                if (selectedUnits.Count == 1)
                {
                    RTSUnit unit = selectedUnits[0];
                    selectionDetailsText.text = GetUnitDetails(unit);
                }
                else
                {
                    selectionDetailsText.text = GetMultiUnitDetails(selectedUnits);
                }
            }
        }

        /// <summary>
        /// Get details for single unit
        /// </summary>
        private string GetUnitDetails(RTSUnit unit)
        {
            return $"Unit: {unit.name}\n" +
                   $"Health: {unit.GetCurrentHealth():F0}/{unit.GetMaxHealth()}\n" +
                   $"Team: {unit.GetTeamId()}";
        }

        /// <summary>
        /// Get details for multiple units
        /// </summary>
        private string GetMultiUnitDetails(List<RTSUnit> units)
        {
            float totalHealth = 0f;
            float maxHealth = 0f;

            foreach (RTSUnit unit in units)
            {
                totalHealth += unit.GetCurrentHealth();
                maxHealth += unit.GetMaxHealth();
            }

            return $"Units: {units.Count}\n" +
                   $"Total Health: {totalHealth:F0}/{maxHealth:F0}";
        }

        /// <summary>
        /// Update game status display
        /// </summary>
        private void UpdateGameStatusDisplay()
        {
            if (gameStatusText == null || gameMode == null) return;

            if (gameMode.IsGameActive())
            {
                gameStatusText.text = "Game Active";
                gameStatusText.color = Color.green;
            }
            else
            {
                gameStatusText.text = "Game Ended";
                gameStatusText.color = Color.red;
            }
        }

        // Event handlers
        private void HandleResourcesChanged(int teamId, PlayerResources resources)
        {
            if (teamId == playerTeamId)
            {
                UpdateResourceDisplay();
            }
        }

        private void HandleSelectionChanged(List<RTSUnit> selectedUnits)
        {
            UpdateSelectionDisplay();
        }

        private void HandleVictory(int winningTeam)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);

                if (victoryText != null)
                {
                    if (winningTeam == playerTeamId)
                    {
                        victoryText.text = "VICTORY!";
                        victoryText.color = Color.green;
                    }
                    else
                    {
                        victoryText.text = "DEFEAT";
                        victoryText.color = Color.red;
                    }
                }
            }

            Debug.Log($"[StrategyUI] Game ended. Winner: Team {winningTeam}");
        }

        /// <summary>
        /// Build button handler (example)
        /// </summary>
        public void OnBuildUnitClicked()
        {
            if (gameMode == null) return;

            // Example: Try to spend resources for a unit (cost: 100 gold, 50 food, 0 wood)
            bool success = gameMode.SpendResources(playerTeamId, 100, 50, 0);

            if (success)
            {
                Debug.Log("[StrategyUI] Unit built!");
            }
            else
            {
                Debug.Log("[StrategyUI] Not enough resources!");
            }
        }

        /// <summary>
        /// Collect resources button handler (example)
        /// </summary>
        public void OnCollectResourcesClicked()
        {
            if (gameMode == null) return;

            // Example: Add resources (100 gold, 50 food, 25 wood)
            gameMode.AddResources(playerTeamId, 100, 50, 25);
            Debug.Log("[StrategyUI] Resources collected!");
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (gameMode != null)
            {
                gameMode.OnResourcesChanged -= HandleResourcesChanged;
                gameMode.OnSelectionChanged -= HandleSelectionChanged;
                gameMode.OnVictory -= HandleVictory;
            }

            // Cleanup render texture
            if (minimapCamera != null && minimapCamera.targetTexture != null)
            {
                minimapCamera.targetTexture.Release();
            }
        }
    }
}
