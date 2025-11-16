using UnityEngine;
using Laboratory.Chimera.Social.Systems;

namespace Laboratory.Chimera.Social.Core
{
    /// <summary>
    /// Service locator pattern for social systems.
    /// Provides O(1) access to social subsystems without FindObjectOfType overhead.
    /// Replaces expensive FindObjectOfType calls with efficient static references.
    /// </summary>
    public static class SocialServiceLocator
    {
        private static SocialNetworkSystem _networkSystem;
        private static GroupDynamicsSystem _groupDynamicsSystem;
        private static Systems.CommunicationSystem _communicationSystem;

        // Social Network System
        public static void RegisterSocialNetwork(SocialNetworkSystem system)
        {
            if (_networkSystem != null && _networkSystem != system)
                Debug.LogWarning($"[SocialServiceLocator] Replacing existing SocialNetworkSystem");
            _networkSystem = system;
        }

        public static SocialNetworkSystem SocialNetwork
        {
            get
            {
                if (_networkSystem == null)
                    Debug.LogError("[SocialServiceLocator] SocialNetworkSystem not registered. Ensure it's initialized in Awake()");
                return _networkSystem;
            }
        }

        // Group Dynamics System
        public static void RegisterGroupDynamics(GroupDynamicsSystem system)
        {
            if (_groupDynamicsSystem != null && _groupDynamicsSystem != system)
                Debug.LogWarning($"[SocialServiceLocator] Replacing existing GroupDynamicsSystem");
            _groupDynamicsSystem = system;
        }

        public static GroupDynamicsSystem GroupDynamics
        {
            get
            {
                if (_groupDynamicsSystem == null)
                    Debug.LogError("[SocialServiceLocator] GroupDynamicsSystem not registered. Ensure it's initialized in Awake()");
                return _groupDynamicsSystem;
            }
        }

        // Communication System
        public static void RegisterCommunication(Systems.CommunicationSystem system)
        {
            if (_communicationSystem != null && _communicationSystem != system)
                Debug.LogWarning($"[SocialServiceLocator] Replacing existing CommunicationSystem");
            _communicationSystem = system;
        }

        public static Systems.CommunicationSystem Communication
        {
            get
            {
                if (_communicationSystem == null)
                    Debug.LogError("[SocialServiceLocator] CommunicationSystem not registered. Ensure it's initialized in Awake()");
                return _communicationSystem;
            }
        }

        /// <summary>
        /// Check if all essential systems are registered
        /// </summary>
        public static bool AreEssentialSystemsRegistered()
        {
            return _networkSystem != null &&
                   _groupDynamicsSystem != null &&
                   _communicationSystem != null;
        }

        /// <summary>
        /// Clear all registrations (useful for scene unload/cleanup)
        /// </summary>
        public static void Clear()
        {
            _networkSystem = null;
            _groupDynamicsSystem = null;
            _communicationSystem = null;
        }

        /// <summary>
        /// Get status of all registered systems for debugging
        /// </summary>
        public static string GetRegistrationStatus()
        {
            return $"Social Service Locator Status:\n" +
                   $"  Social Network: {(_networkSystem != null ? "✓" : "✗")}\n" +
                   $"  Group Dynamics: {(_groupDynamicsSystem != null ? "✓" : "✗")}\n" +
                   $"  Communication: {(_communicationSystem != null ? "✓" : "✗")}";
        }
    }
}
