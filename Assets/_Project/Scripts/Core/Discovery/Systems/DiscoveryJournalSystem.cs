using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Discovery.Data;
using Laboratory.Core.Discovery.Types;
using Laboratory.Core.Discovery.Services;
using Laboratory.Core.MonsterTown;

namespace Laboratory.Core.Discovery.Systems
{
    /// <summary>
    /// Core Discovery Journal System - Documents genetic findings, breeding successes, and scientific discoveries
    /// </summary>
    public class DiscoveryJournalSystem : MonoBehaviour
    {
        [Header("📔 Journal Configuration")]
        [SerializeField] private JournalConfig journalConfig;
        [SerializeField] private bool enableAutoDocumentation = true;
        [SerializeField] private bool enablePlayerNotes = true;
        [SerializeField] private bool enableResearchProjects = true;

        [Header("🏆 Achievement Settings")]
        [SerializeField] private AchievementDatabase achievementDatabase;
        [SerializeField] private bool enableAchievements = true;

        [Header("🔬 Research Features")]
        [SerializeField] private int maxActiveResearchProjects = 5;
        [SerializeField] private float researchProjectDuration = 604800f; // 7 days

        // Services
        private JournalEntryService journalEntryService;
        private BreedingAnalysisService breedingAnalysisService;
        private AchievementService achievementService;
        private ResearchProjectService researchProjectService;

        // Events
        public event Action<JournalEntry> OnJournalEntryAdded;
        public event Action<Achievement> OnAchievementUnlocked;
        public event Action<GeneticDiscovery> OnGeneticDiscoveryMade;
        public event Action<ResearchProject> OnResearchProjectCompleted;

        private void Awake()
        {
            InitializeServices();
        }

        private void InitializeServices()
        {
            // Get the main DiscoveryJournalSystem from the Discovery namespace
            var mainDiscoverySystem = FindFirstObjectByType<Laboratory.Core.Discovery.DiscoveryJournalSystem>();
            if (mainDiscoverySystem == null)
            {
                Debug.LogWarning("Main DiscoveryJournalSystem not found. Creating services with null reference.");
            }

            journalEntryService = new JournalEntryService(mainDiscoverySystem);
            breedingAnalysisService = new BreedingAnalysisService(mainDiscoverySystem);
            achievementService = new AchievementService(mainDiscoverySystem, achievementDatabase);
            researchProjectService = new ResearchProjectService(mainDiscoverySystem);
        }

        #region Public API

        public void InitializeDiscoverySystem(JournalConfig config, AchievementDatabase achievements)
        {
            journalConfig = config;
            achievementDatabase = achievements;

            journalEntryService.Initialize(config);
            achievementService.Initialize(achievements);
            researchProjectService.Initialize();

            Debug.Log("📔 Discovery Journal System initialized");
        }

        public JournalEntry AddJournalEntry(string playerId, JournalEntryType entryType, string title, string content, object associatedData = null)
        {
            var entry = journalEntryService.AddJournalEntry(playerId, entryType, title, content, associatedData);
            OnJournalEntryAdded?.Invoke(entry);
            return entry;
        }

        public void DocumentBreedingResult(string playerId, Monster parent1, Monster parent2, Monster offspring)
        {
            breedingAnalysisService.DocumentBreedingResult(playerId, parent1, parent2, offspring);
        }

        public void DocumentGeneticDiscovery(string playerId, GeneticDiscovery discovery)
        {
            journalEntryService.DocumentGeneticDiscovery(playerId, discovery);
            OnGeneticDiscoveryMade?.Invoke(discovery);
        }

        public JournalEntry AddPlayerObservation(string playerId, string observation, string hypothesis = "")
        {
            return journalEntryService.AddPlayerObservation(playerId, observation, hypothesis);
        }

        public bool StartResearchProject(string playerId, string projectId)
        {
            return researchProjectService.StartResearchProject(playerId, projectId);
        }

        public void UpdateResearchProgress(string playerId, ResearchObjectiveType objectiveType, object data = null)
        {
            researchProjectService.UpdateResearchProgress(playerId, objectiveType, data);
        }

        #endregion

        #region Query Methods

        public List<JournalEntry> GetJournalEntries(string playerId, JournalEntryType? entryType = null, int limit = 50)
        {
            return journalEntryService.GetJournalEntries(playerId, entryType, limit);
        }

        public List<Achievement> GetUnlockedAchievements(string playerId)
        {
            return achievementService.GetUnlockedAchievements(playerId);
        }

        public List<ResearchProject> GetActiveResearchProjects(string playerId)
        {
            return researchProjectService.GetActiveResearchProjects(playerId);
        }

        public DiscoveryStatistics GetDiscoveryStatistics(string playerId)
        {
            return journalEntryService.GetDiscoveryStatistics(playerId);
        }

        public List<JournalEntry> SearchJournal(string playerId, string searchTerm)
        {
            return journalEntryService.SearchJournal(playerId, searchTerm);
        }

        #endregion

        #region Internal Event Triggers

        internal void TriggerAchievementUnlocked(Achievement achievement)
        {
            OnAchievementUnlocked?.Invoke(achievement);
        }

        internal void TriggerResearchProjectCompleted(ResearchProject project)
        {
            OnResearchProjectCompleted?.Invoke(project);
        }

        #endregion

        #region Properties

        public bool EnableAutoDocumentation => enableAutoDocumentation;
        public bool EnablePlayerNotes => enablePlayerNotes;
        public bool EnableResearchProjects => enableResearchProjects;
        public bool EnableAchievements => enableAchievements;
        public int MaxActiveResearchProjects => maxActiveResearchProjects;
        public float ResearchProjectDuration => researchProjectDuration;

        #endregion
    }
}