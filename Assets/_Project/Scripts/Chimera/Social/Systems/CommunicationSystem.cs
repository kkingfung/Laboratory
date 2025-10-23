using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Chimera.Social.Systems
{
    /// <summary>
    /// Communication and language evolution system
    /// </summary>
    public class CommunicationSystem : MonoBehaviour
    {
        [Header("Communication Configuration")]
        [SerializeField] private float communicationEfficiency = 0.8f;
        [SerializeField] private bool enableLanguageEvolution = true;
        [SerializeField] private int maxVocabularySize = 500;
        [SerializeField] private float languageEvolutionRate = 0.01f;

        private Dictionary<uint, Laboratory.Chimera.Social.Data.CommunicationProfile> agentProfiles = new();
        private List<CommunicationEvent> recentCommunications = new();
        private LanguageEvolutionEngine languageEngine;

        public event Action<uint, uint, string> OnCommunicationSent;
        public event Action<string> OnLanguageEvolution;

        private void Awake()
        {
            languageEngine = new LanguageEvolutionEngine(maxVocabularySize, languageEvolutionRate);
        }

        public void RegisterAgent(uint agentId, Laboratory.Chimera.Social.Data.CommunicationProfile profile)
        {
            agentProfiles[agentId] = profile;
            UnityEngine.Debug.Log($"Registered communication profile for agent {agentId}");
        }

        public bool SendCommunication(uint senderId, uint receiverId, string message, string context = "")
        {
            if (!agentProfiles.ContainsKey(senderId) || !agentProfiles.ContainsKey(receiverId))
                return false;

            var senderProfile = agentProfiles[senderId];
            var receiverProfile = agentProfiles[receiverId];

            var communicationEvent = new CommunicationEvent
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                Context = context,
                Success = CalculateCommunicationSuccess(senderProfile, receiverProfile),
                Timestamp = DateTime.UtcNow
            };

            recentCommunications.Add(communicationEvent);

            if (communicationEvent.Success)
            {
                ProcessSuccessfulCommunication(communicationEvent);
                OnCommunicationSent?.Invoke(senderId, receiverId, message);
            }

            // Evolve language based on communication patterns
            if (enableLanguageEvolution)
            {
                languageEngine.ProcessCommunication(message, communicationEvent.Success);
            }

            return communicationEvent.Success;
        }

        public void BroadcastToGroup(uint senderId, uint groupId, string message)
        {
            var groupSystem = FindObjectOfType<GroupDynamicsSystem>();
            var group = groupSystem?.GetGroup(groupId);

            if (group != null)
            {
                foreach (var memberId in group.Members)
                {
                    if (memberId != senderId)
                    {
                        SendCommunication(senderId, memberId, message, $"group_broadcast_{groupId}");
                    }
                }
            }
        }

        private bool CalculateCommunicationSuccess(Laboratory.Chimera.Social.Data.CommunicationProfile sender, Laboratory.Chimera.Social.Data.CommunicationProfile receiver)
        {
            float compatibilityScore = CalculateStyleCompatibility(sender.Style, receiver.Style);
            float languageOverlap = CalculateLanguageOverlap(sender, receiver);
            float expressivenessFactor = (sender.Expressiveness + receiver.Receptiveness) / 2f;

            float successProbability = (compatibilityScore * 0.4f + languageOverlap * 0.4f + expressivenessFactor * 0.2f) * communicationEfficiency;

            return UnityEngine.Random.value < successProbability;
        }

        private float CalculateStyleCompatibility(Laboratory.Chimera.Social.Types.CommunicationStyle senderStyle, Laboratory.Chimera.Social.Types.CommunicationStyle receiverStyle)
        {
            // Some communication styles work better together
            return (senderStyle, receiverStyle) switch
            {
                (Laboratory.Chimera.Social.Types.CommunicationStyle.Direct, Laboratory.Chimera.Social.Types.CommunicationStyle.Direct) => 0.9f,
                (Laboratory.Chimera.Social.Types.CommunicationStyle.Diplomatic, Laboratory.Chimera.Social.Types.CommunicationStyle.Diplomatic) => 0.9f,
                (Laboratory.Chimera.Social.Types.CommunicationStyle.Charismatic, _) => 0.8f,
                (_, Laboratory.Chimera.Social.Types.CommunicationStyle.Charismatic) => 0.8f,
                (Laboratory.Chimera.Social.Types.CommunicationStyle.Aggressive, Laboratory.Chimera.Social.Types.CommunicationStyle.Passive) => 0.3f,
                (Laboratory.Chimera.Social.Types.CommunicationStyle.Passive, Laboratory.Chimera.Social.Types.CommunicationStyle.Aggressive) => 0.3f,
                _ => 0.6f
            };
        }

        private float CalculateLanguageOverlap(Laboratory.Chimera.Social.Data.CommunicationProfile sender, Laboratory.Chimera.Social.Data.CommunicationProfile receiver)
        {
            if (sender.LanguageProficiency.Count == 0 || receiver.LanguageProficiency.Count == 0)
                return 0.5f; // Default if no language data

            float totalOverlap = 0f;
            int sharedLanguages = 0;

            foreach (var senderLang in sender.LanguageProficiency)
            {
                if (receiver.LanguageProficiency.TryGetValue(senderLang.Key, out var receiverProficiency))
                {
                    totalOverlap += Mathf.Min(senderLang.Value, receiverProficiency);
                    sharedLanguages++;
                }
            }

            return sharedLanguages > 0 ? totalOverlap / sharedLanguages : 0.1f;
        }

        private void ProcessSuccessfulCommunication(CommunicationEvent communication)
        {
            // Update language evolution
            if (enableLanguageEvolution)
            {
                languageEngine.RecordSuccessfulCommunication(communication.SenderId, communication.ReceiverId, communication.Message);
            }

            // Update agent communication profiles
            var senderProfile = agentProfiles[communication.SenderId];
            var receiverProfile = agentProfiles[communication.ReceiverId];

            // Slightly improve communication effectiveness over time
            senderProfile.Expressiveness += 0.001f;
            receiverProfile.Receptiveness += 0.001f;

            senderProfile.Expressiveness = Mathf.Clamp01(senderProfile.Expressiveness);
            receiverProfile.Receptiveness = Mathf.Clamp01(receiverProfile.Receptiveness);
        }

        public List<CommunicationEvent> GetRecentCommunications(int count = 10)
        {
            return recentCommunications.TakeLast(count).ToList();
        }

        public Laboratory.Chimera.Social.Data.CommunicationProfile GetAgentProfile(uint agentId)
        {
            return agentProfiles.TryGetValue(agentId, out var profile) ? profile : null;
        }

        public void UpdateLanguageEvolution()
        {
            if (enableLanguageEvolution)
            {
                var evolutionResult = languageEngine.UpdateEvolution();
                if (!string.IsNullOrEmpty(evolutionResult))
                {
                    OnLanguageEvolution?.Invoke(evolutionResult);
                }
            }
        }
    }

    /// <summary>
    /// Language evolution engine
    /// </summary>
    public class LanguageEvolutionEngine
    {
        private readonly int maxVocabularySize;
        private readonly float evolutionRate;
        private Dictionary<string, float> vocabulary = new();
        private Dictionary<string, int> wordUsage = new();
        private List<string> emergingWords = new();

        public LanguageEvolutionEngine(int maxVocabulary, float evolutionRate)
        {
            this.maxVocabularySize = maxVocabulary;
            this.evolutionRate = evolutionRate;
            InitializeBaseVocabulary();
        }

        private void InitializeBaseVocabulary()
        {
            var baseWords = new[] { "hello", "food", "danger", "friend", "group", "leader", "happy", "sad", "help", "play" };
            foreach (var word in baseWords)
            {
                vocabulary[word] = 1.0f;
                wordUsage[word] = 0;
            }
        }

        public void ProcessCommunication(string message, bool successful)
        {
            var words = message.ToLower().Split(' ');
            foreach (var word in words)
            {
                if (vocabulary.ContainsKey(word))
                {
                    wordUsage[word]++;
                    if (successful)
                    {
                        vocabulary[word] += evolutionRate;
                    }
                    else
                    {
                        vocabulary[word] -= evolutionRate * 0.5f;
                    }
                }
                else if (successful && vocabulary.Count < maxVocabularySize)
                {
                    // New word emerges from successful communication
                    vocabulary[word] = 0.1f;
                    wordUsage[word] = 1;
                    emergingWords.Add(word);
                }
            }
        }

        public void RecordSuccessfulCommunication(uint senderId, uint receiverId, string message)
        {
            ProcessCommunication(message, true);
        }

        public string UpdateEvolution()
        {
            string evolutionReport = "";

            // Remove unused words
            var wordsToRemove = vocabulary.Where(kvp => kvp.Value < 0.05f).Select(kvp => kvp.Key).ToList();
            foreach (var word in wordsToRemove)
            {
                vocabulary.Remove(word);
                wordUsage.Remove(word);
                evolutionReport += $"Word '{word}' has fallen out of use. ";
            }

            // Promote emerging words
            var wordsToPromote = emergingWords.Where(word => vocabulary.ContainsKey(word) && vocabulary[word] > 0.5f).ToList();
            foreach (var word in wordsToPromote)
            {
                emergingWords.Remove(word);
                evolutionReport += $"New word '{word}' established in language. ";
            }

            return evolutionReport;
        }

        public Dictionary<string, float> GetCurrentVocabulary()
        {
            return new Dictionary<string, float>(vocabulary);
        }
    }

    /// <summary>
    /// Communication event data structure
    /// </summary>
    [Serializable]
    public class CommunicationEvent
    {
        public uint SenderId;
        public uint ReceiverId;
        public string Message;
        public string Context;
        public bool Success;
        public DateTime Timestamp;
        public float EffectivenessScore;
    }
}