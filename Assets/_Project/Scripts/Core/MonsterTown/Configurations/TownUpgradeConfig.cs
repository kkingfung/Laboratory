using System;
using UnityEngine;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Town upgrade configuration
    /// </summary>
    [Serializable]
    public struct TownUpgradeConfig
    {
        public int level;
        public string upgradeName;
        public TownResourcesConfig cost;
        public string[] unlockedFeatures;
        public int populationIncrease;
        public string description;
    }

    /// <summary>
    /// Building unlock configuration
    /// </summary>
    [Serializable]
    public struct BuildingUnlockConfig
    {
        public BuildingType buildingType;
        public int requiredTownLevel;
        public TownResourcesConfig unlockCost;
        public BuildingType[] prerequisiteBuildings;
        public string unlockCondition;
    }
}