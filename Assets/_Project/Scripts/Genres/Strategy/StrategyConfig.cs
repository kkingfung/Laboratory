using UnityEngine;

namespace Laboratory.Genres.Strategy
{
    /// <summary>
    /// ScriptableObject configuration for Strategy RTS genre
    /// Designer-friendly settings for RTS gameplay
    /// </summary>
    [CreateAssetMenu(fileName = "StrategyConfig", menuName = "Chimera/Genres/Strategy Config")]
    public class StrategyConfig : ScriptableObject
    {
        [Header("Resources")]
        [SerializeField] private int startingGold = 1000;
        [SerializeField] private int startingFood = 500;
        [SerializeField] private int startingWood = 500;
        [SerializeField] private int populationLimit = 200;

        [Header("Unit Selection")]
        [SerializeField] private bool enableMultiSelect = true;
        [SerializeField] private bool enableGroupSelection = true;
        [SerializeField] private int maxUnitsSelected = 50;
        [SerializeField] private Color selectionColor = Color.green;

        [Header("Building")]
        [SerializeField] private bool enableGridSnapping = true;
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private bool checkBuildingPlacement = true;
        [SerializeField] private LayerMask buildableLayer;

        [Header("Combat")]
        [SerializeField] private bool enableAutoAttack = true;
        [SerializeField] private float autoAttackRange = 2f;
        [SerializeField] private bool enableFogOfWar = true;
        [SerializeField] private float visionRadius = 10f;

        [Header("AI")]
        [SerializeField] private float aiUpdateInterval = 0.5f;
        [SerializeField] private int aiStartingAdvantage = 0; // % bonus resources
        [SerializeField] private StrategyDifficulty aiDifficulty = StrategyDifficulty.Medium;

        [Header("Victory Conditions")]
        [SerializeField] private bool enableDomination = true; // Destroy all enemies
        [SerializeField] private bool enableEconomy = false; // Reach resource threshold
        [SerializeField] private int economyVictoryThreshold = 10000;
        [SerializeField] private bool enableWonder = false; // Build wonder and defend
        [SerializeField] private float wonderVictoryTime = 300f; // 5 minutes

        [Header("Game Settings")]
        [SerializeField] private float gameSpeed = 1f;
        [SerializeField] private bool enablePause = true;
        [SerializeField] private bool enableSaveLoad = false;

        // Properties
        public int StartingGold => startingGold;
        public int StartingFood => startingFood;
        public int StartingWood => startingWood;
        public int PopulationLimit => populationLimit;

        public bool EnableMultiSelect => enableMultiSelect;
        public bool EnableGroupSelection => enableGroupSelection;
        public int MaxUnitsSelected => maxUnitsSelected;
        public Color SelectionColor => selectionColor;

        public bool EnableGridSnapping => enableGridSnapping;
        public float GridSize => gridSize;
        public bool CheckBuildingPlacement => checkBuildingPlacement;
        public LayerMask BuildableLayer => buildableLayer;

        public bool EnableAutoAttack => enableAutoAttack;
        public float AutoAttackRange => autoAttackRange;
        public bool EnableFogOfWar => enableFogOfWar;
        public float VisionRadius => visionRadius;

        public float AIUpdateInterval => aiUpdateInterval;
        public int AIStartingAdvantage => aiStartingAdvantage;
        public StrategyDifficulty AIDifficulty => aiDifficulty;

        public bool EnableDomination => enableDomination;
        public bool EnableEconomy => enableEconomy;
        public int EconomyVictoryThreshold => economyVictoryThreshold;
        public bool EnableWonder => enableWonder;
        public float WonderVictoryTime => wonderVictoryTime;

        public float GameSpeed => gameSpeed;
        public bool EnablePause => enablePause;
        public bool EnableSaveLoad => enableSaveLoad;
    }

    /// <summary>
    /// Strategy AI difficulty levels
    /// </summary>
    public enum StrategyDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert
    }
}
