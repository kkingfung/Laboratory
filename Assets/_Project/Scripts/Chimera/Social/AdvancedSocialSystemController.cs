using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Chimera.Social.Systems;
using SocialTypes = Laboratory.Chimera.Social.Types;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Main controller for the advanced social system - coordinates all social subsystems
    /// This replaces the monolithic AdvancedSocialSystem.cs file
    /// </summary>
    [CreateAssetMenu(fileName = "AdvancedSocialSystemController", menuName = "Chimera/Social/Advanced Social System Controller")]
    public class AdvancedSocialSystemController : ScriptableObject
    {
        [Header("Social Network Configuration")]
        [SerializeField] private float relationshipDecayRate = 0.001f;
        [SerializeField] private float empathyDevelopmentRate = 0.02f;
        [SerializeField] private bool enableCulturalEvolution = true;

        [Header("Group Dynamics")]
        [SerializeField] private int maxGroupSize = 25;
        [SerializeField] private float groupCohesionThreshold = 0.7f;
        [SerializeField] private float leadershipEmergenceRate = 0.05f;
        [SerializeField] private bool enableHierarchyFormation = true;

        [Header("Communication System")]
        [SerializeField] private float communicationEfficiency = 0.8f;
        [SerializeField] private bool enableLanguageEvolution = true;
        [SerializeField] private int maxVocabularySize = 500;

        [Header("Cultural Parameters")]
        [SerializeField] private float culturalTransmissionRate = 0.3f;
        [SerializeField] private float innovationRate = 0.01f;

        [Header("Emotional Intelligence")]
        [SerializeField] private float empathyRange = 50f;
        [SerializeField] private bool enableEmotionalContagion = true;
        [SerializeField] private float emotionalSynchronizationRate = 0.1f;

        // System references (will be found/created at runtime)
        private SocialNetworkSystem networkSystem;
        private GroupDynamicsSystem groupSystem;
        private Laboratory.Chimera.Social.Systems.CommunicationSystem communicationSystem;
        private CulturalEvolutionSystem cultureSystem;
        private EmotionalContagionSystem emotionalSystem;

        // Events
        public event Action<uint, uint, SocialTypes.RelationshipType> OnRelationshipFormed;
        public event Action<uint, Laboratory.Chimera.Social.Data.SocialGroup> OnGroupFormed;
        public event Action<uint, uint> OnLeadershipEmergence;
        public event Action<Laboratory.Chimera.Social.Data.CulturalTrait> OnCulturalInnovation;
        public event Action<uint, uint, SocialTypes.EmotionalState> OnEmotionalContagion;

        private bool isInitialized = false;

        public void Initialize()
        {
            if (isInitialized) return;

            FindOrCreateSocialSystems();
            ConnectSystemEvents();
            ConfigureSystems();

            isInitialized = true;
            UnityEngine.Debug.Log("🤝 Advanced Social System Controller initialized with modular architecture");
        }

        private void FindOrCreateSocialSystems()
        {
            // Find existing systems or create them
            var socialSystemsParent = GameObject.Find("SocialSystems");
            if (socialSystemsParent == null)
            {
                socialSystemsParent = new GameObject("SocialSystems");
            }

            networkSystem = socialSystemsParent.GetComponent<SocialNetworkSystem>() ??
                           socialSystemsParent.AddComponent<SocialNetworkSystem>();

            groupSystem = socialSystemsParent.GetComponent<GroupDynamicsSystem>() ??
                         socialSystemsParent.AddComponent<GroupDynamicsSystem>();

            communicationSystem = socialSystemsParent.GetComponent<Laboratory.Chimera.Social.Systems.CommunicationSystem>() ??
                                socialSystemsParent.AddComponent<Laboratory.Chimera.Social.Systems.CommunicationSystem>();

            cultureSystem = socialSystemsParent.GetComponent<CulturalEvolutionSystem>() ??
                           socialSystemsParent.AddComponent<CulturalEvolutionSystem>();

            emotionalSystem = socialSystemsParent.GetComponent<EmotionalContagionSystem>() ??
                            socialSystemsParent.AddComponent<EmotionalContagionSystem>();
        }

        private void ConnectSystemEvents()
        {
            // Connect subsystem events to main controller events
            if (networkSystem != null)
            {
                networkSystem.OnRelationshipFormed += (uint agent1, uint agent2, Laboratory.Chimera.Social.Types.RelationshipType type) => OnRelationshipFormed?.Invoke(agent1, agent2, type);
            }

            if (groupSystem != null)
            {
                groupSystem.OnGroupFormed += (groupId, group) => OnGroupFormed?.Invoke(groupId, group);
                groupSystem.OnLeadershipEmergence += (leaderId, groupId) => OnLeadershipEmergence?.Invoke(leaderId, groupId);
            }

            if (cultureSystem != null)
            {
                cultureSystem.OnCulturalInnovation += (trait) => OnCulturalInnovation?.Invoke(trait);
            }

            if (emotionalSystem != null)
            {
                emotionalSystem.OnEmotionalContagion += (source, target, emotion) => OnEmotionalContagion?.Invoke(source, target, emotion);
            }
        }

        private void ConfigureSystems()
        {
            // Apply configuration to each subsystem
            // This would set the serialized field values on each system
            UnityEngine.Debug.Log("Social systems configured with controller parameters");
        }

        #region Public API Methods

        public void RegisterSocialAgent(Laboratory.Chimera.Social.Data.SocialAgent agent)
        {
            if (!isInitialized) Initialize();

            networkSystem?.RegisterAgent(agent);
            emotionalSystem?.RegisterAgent(agent.AgentId, agent.CurrentEmotionalState, agent.Empathy);

            var commProfile = agent.CommunicationProfile ?? CreateDefaultCommunicationProfile();
            communicationSystem?.RegisterAgent(agent.AgentId, commProfile);

            UnityEngine.Debug.Log($"🤝 Registered social agent: {agent.Name} (ID: {agent.AgentId})");
        }

        public void ProcessSocialInteraction(uint agent1Id, uint agent2Id, SocialTypes.InteractionType interactionType, SocialTypes.InteractionOutcome outcome, float intensity = 1.0f)
        {
            if (!isInitialized) return;

            // Update relationships
            networkSystem?.UpdateRelationship(agent1Id, agent2Id, (Laboratory.Chimera.Social.InteractionOutcome)(int)outcome, intensity);

            // Process cultural transmission
            cultureSystem?.ProcessCulturalInteraction(agent1Id, agent2Id, interactionType, outcome);

            // Handle emotional responses
            if (outcome == SocialTypes.InteractionOutcome.Positive)
            {
                emotionalSystem?.UpdateSocialEmotionalState(agent1Id, SocialTypes.EmotionalState.Happy, intensity * 0.5f);
                emotionalSystem?.UpdateSocialEmotionalState(agent2Id, SocialTypes.EmotionalState.Happy, intensity * 0.3f);
            }
            else if (outcome == SocialTypes.InteractionOutcome.Negative)
            {
                emotionalSystem?.UpdateSocialEmotionalState(agent1Id, SocialTypes.EmotionalState.Angry, intensity * 0.4f);
                emotionalSystem?.UpdateSocialEmotionalState(agent2Id, SocialTypes.EmotionalState.Sad, intensity * 0.3f);
            }

            UnityEngine.Debug.Log($"🤝 Processed social interaction: {agent1Id} → {agent2Id} ({interactionType}, {outcome})");
        }

        public void SendCommunication(uint senderId, uint receiverId, string message, string context = "")
        {
            if (!isInitialized) return;

            bool success = communicationSystem?.SendCommunication(senderId, receiverId, message, context) ?? false;

            if (success)
            {
                // Successful communication improves relationship
                networkSystem?.UpdateRelationship(senderId, receiverId, (Laboratory.Chimera.Social.InteractionOutcome)(int)SocialTypes.InteractionOutcome.Positive, 0.3f);
            }
        }

        public void FormGroup(List<uint> memberIds, string groupName = "")
        {
            if (!isInitialized) return;

            groupSystem?.FormGroup(memberIds, groupName);
        }

        public void UpdateSocialSystems()
        {
            if (!isInitialized) return;

            // Update all subsystems
            networkSystem?.UpdateNetworkDecay();
            groupSystem?.UpdateGroupDynamics();
            communicationSystem?.UpdateLanguageEvolution();
            cultureSystem?.UpdateCulturalEvolution();
            emotionalSystem?.UpdateEmotionalDecay();
        }

        #endregion

        #region Query Methods

        public Laboratory.Chimera.Social.Data.SocialRelationship GetRelationship(uint agent1Id, uint agent2Id)
        {
            return networkSystem?.GetRelationship(agent1Id, agent2Id);
        }

        public List<Laboratory.Chimera.Social.Data.SocialGroup> GetAllGroups()
        {
            return groupSystem?.GetAllGroups() ?? new List<Laboratory.Chimera.Social.Data.SocialGroup>();
        }

        public List<Laboratory.Chimera.Social.Data.CulturalTrait> GetGlobalCulture()
        {
            return cultureSystem?.GetGlobalCulture() ?? new List<Laboratory.Chimera.Social.Data.CulturalTrait>();
        }

        public SocialTypes.EmotionalState GetAgentEmotionalState(uint agentId)
        {
            return emotionalSystem?.GetAgentSocialEmotionalState(agentId) ?? SocialTypes.EmotionalState.Neutral;
        }

        public Laboratory.Chimera.Social.Systems.EmpathyNetworkAnalysis GetEmpathyNetworkAnalysis()
        {
            return emotionalSystem?.GetEmpathyNetworkAnalysis();
        }

        #endregion

        private Laboratory.Chimera.Social.Data.CommunicationProfile CreateDefaultCommunicationProfile()
        {
            return new Laboratory.Chimera.Social.Data.CommunicationProfile
            {
                Style = SocialTypes.CommunicationStyle.Direct,
                Expressiveness = 0.5f,
                Receptiveness = 0.5f,
                PreferredTopics = new List<string> { "general", "social" },
                LanguageProficiency = new Dictionary<string, float> { { "common", 1.0f } },
                NonVerbalCommunication = 0.5f
            };
        }

        #region Properties

        public bool IsInitialized => isInitialized;
        public bool EnableCulturalEvolution => enableCulturalEvolution;
        public bool EnableEmotionalContagion => enableEmotionalContagion;
        public bool EnableLanguageEvolution => enableLanguageEvolution;
        public float EmpathyRange => empathyRange;
        public int MaxGroupSize => maxGroupSize;

        #endregion
    }
}