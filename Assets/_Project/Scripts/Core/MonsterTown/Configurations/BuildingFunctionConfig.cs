using System;
using UnityEngine;
using Laboratory.Core.TownBuilding.Types;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Building function configuration
    /// </summary>
    [Serializable]
    public struct BuildingFunction
    {
        public BuildingType functionType;
        public float efficiency;
        public int capacity;
        public TownResourcesConfig resourceCost;
        public string description;
    }
}