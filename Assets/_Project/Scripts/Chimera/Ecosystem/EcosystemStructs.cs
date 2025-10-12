using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Laboratory.Chimera.Ecosystem
{
    /// <summary>
    /// Shared structs for the ecosystem and conservation systems
    /// </summary>

    [Serializable]
    public struct ConservationEmergency
    {
        public int emergencyId;
        public EmergencyType emergencyType;
        public EmergencySeverity severity;
        public string title;
        public string description;
        public float timeRemaining;
        [MarshalAs(UnmanagedType.U1)]
        public bool isActive;
        public Vector3 location;
        public int affectedSpeciesCount;
        public float progressPercentage;
        public float originalDuration;
        [MarshalAs(UnmanagedType.U1)]
        public bool hasEscalated;
    }

    [Serializable]
    public struct EmergencyAction
    {
        public EmergencyActionType actionType;
        public string title;
        public string description;
        public float cost;
        public float effectiveness;
        public float duration;
        [MarshalAs(UnmanagedType.U1)]
        public bool isAvailable;

        // Additional properties expected by EmergencyConservationConfig
        public EmergencyActionType type { get { return actionType; } set { actionType = value; } }
        public string name { get { return title; } set { title = value; } }
        public float resourceRequirement { get { return cost; } set { cost = value; } }
        public float timeRequirement { get { return duration; } set { duration = value; } }
        public string[] prerequisites;
    }
}