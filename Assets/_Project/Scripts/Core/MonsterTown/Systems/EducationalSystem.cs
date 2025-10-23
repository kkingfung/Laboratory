using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;

namespace Laboratory.Core.MonsterTown.Systems
{
    /// <summary>
    /// Educational System - provides learning experiences and skill development
    /// Integrates educational content with monster care and activities
    /// </summary>
    public class EducationalSystem : MonoBehaviour
    {
        [Header("Educational Configuration")]
        [SerializeField] private bool enableEducationalContent = true;
        [SerializeField] private float learningProgressRate = 1f;
        [SerializeField] private int maxLessonsPerDay = 5;

        [Header("Subject Areas")]
        [SerializeField] private SubjectConfig[] subjects;

        [Header("Skill Development")]
        [SerializeField] private float skillGainRate = 0.1f;
        [SerializeField] private float knowledgeRetentionRate = 0.95f; // Per day

        // System dependencies
        private IEventBus eventBus;

        // Educational tracking
        private Dictionary<string, PlayerEducationProgress> playerProgress = new();
        private Dictionary<string, List<Lesson>> completedLessons = new();
        private Dictionary<string, int> dailyLessonCount = new();
        private DateTime lastDayReset = DateTime.Now.Date;
        private DateTime lastEducationalCheck = DateTime.Now;

        #region Unity Lifecycle

        private void Awake()
        {
            eventBus = ServiceContainer.Instance?.ResolveService<IEventBus>();
        }

        private void Start()
        {
            InitializeSubjects();
            InvokeRepeating(nameof(UpdateEducationalProgress), 300f, 300f); // Update every 5 minutes
        }

        private void Update()
        {
            // Reset daily lesson counts at midnight
            if (DateTime.Now.Date > lastDayReset)
            {
                dailyLessonCount.Clear();
                lastDayReset = DateTime.Now.Date;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start a lesson for a player
        /// </summary>
        public bool StartLesson(string playerId, string subjectId, string lessonId)
        {
            if (!enableEducationalContent) return false;

            // Check daily lesson limit
            if (dailyLessonCount.GetValueOrDefault(playerId, 0) >= maxLessonsPerDay)
            {
                Debug.LogWarning($"Daily lesson limit reached for player {playerId}");
                return false;
            }

            var subject = GetSubject(subjectId);
            if (subject == null)
            {
                Debug.LogError($"Subject not found: {subjectId}");
                return false;
            }

            var lesson = GetLesson(subject, lessonId);
            if (lesson == null)
            {
                Debug.LogError($"Lesson not found: {lessonId}");
                return false;
            }

            // Check prerequisites
            if (!HasPrerequisites(playerId, lesson))
            {
                Debug.LogWarning($"Prerequisites not met for lesson {lessonId}");
                return false;
            }

            // Initialize player progress if needed
            if (!playerProgress.ContainsKey(playerId))
            {
                playerProgress[playerId] = new PlayerEducationProgress();
            }

            eventBus?.Publish(new LessonStartedEvent(playerId, subjectId, lessonId));

            Debug.Log($"📚 Started lesson: {lesson.title} for player {playerId}");
            return true;
        }

        /// <summary>
        /// Complete a lesson and award progress
        /// </summary>
        public bool CompleteLesson(string playerId, string subjectId, string lessonId, float completionScore)
        {
            if (!enableEducationalContent) return false;

            var subject = GetSubject(subjectId);
            var lesson = GetLesson(subject, lessonId);
            if (subject == null || lesson == null) return false;

            if (!playerProgress.TryGetValue(playerId, out var progress))
            {
                progress = new PlayerEducationProgress();
                playerProgress[playerId] = progress;
            }

            // Record lesson completion
            if (!completedLessons.ContainsKey(playerId))
            {
                completedLessons[playerId] = new List<Lesson>();
            }

            var completedLesson = new Lesson
            {
                id = lessonId,
                title = lesson.title,
                subject = subjectId,
                completionScore = completionScore,
                completedDate = DateTime.Now
            };

            completedLessons[playerId].Add(completedLesson);

            // Update progress
            if (!progress.subjectProgress.ContainsKey(subjectId))
            {
                progress.subjectProgress[subjectId] = 0f;
            }

            float progressGain = lesson.progressValue * (completionScore / 100f) * learningProgressRate;
            progress.subjectProgress[subjectId] += progressGain;

            // Update skills
            foreach (var skill in lesson.skillGains)
            {
                if (!progress.skills.ContainsKey(skill.Key))
                {
                    progress.skills[skill.Key] = 0f;
                }
                progress.skills[skill.Key] += skill.Value * skillGainRate;
            }

            // Update daily count
            dailyLessonCount[playerId] = dailyLessonCount.GetValueOrDefault(playerId, 0) + 1;

            // Check for subject mastery
            CheckSubjectMastery(playerId, subjectId, progress);

            eventBus?.Publish(new LessonCompletedEvent(playerId, completedLesson, progressGain));

            Debug.Log($"✅ Completed lesson: {lesson.title} - Score: {completionScore:F1}%");
            return true;
        }

        /// <summary>
        /// Get educational tip based on current activity
        /// </summary>
        public string GetEducationalTip(ActivityType activityType, MonsterInstance monster)
        {
            return activityType switch
            {
                ActivityType.Racing => GenerateRacingTip(monster),
                ActivityType.Combat => GenerateCombatTip(monster),
                ActivityType.Puzzle => GeneratePuzzleTip(monster),
                ActivityType.Strategy => GenerateStrategyTip(monster),
                _ => GenerateGeneralTip(monster)
            };
        }

        /// <summary>
        /// Get player's educational progress
        /// </summary>
        public PlayerEducationProgress GetProgress(string playerId)
        {
            return playerProgress.TryGetValue(playerId, out var progress)
                ? progress
                : new PlayerEducationProgress();
        }

        /// <summary>
        /// Get completed lessons for player
        /// </summary>
        public List<Lesson> GetCompletedLessons(string playerId)
        {
            return completedLessons.TryGetValue(playerId, out var lessons)
                ? new List<Lesson>(lessons)
                : new List<Lesson>();
        }

        /// <summary>
        /// Get available lessons for player
        /// </summary>
        public List<LessonInfo> GetAvailableLessons(string playerId)
        {
            var available = new List<LessonInfo>();

            foreach (var subject in subjects)
            {
                foreach (var lesson in subject.lessons)
                {
                    if (HasPrerequisites(playerId, lesson) && !IsLessonCompleted(playerId, lesson.id))
                    {
                        available.Add(lesson);
                    }
                }
            }

            return available;
        }

        /// <summary>
        /// Check for educational moments and provide appropriate guidance
        /// </summary>
        public void CheckForEducationalMoments()
        {
            if (!enableEducationalContent) return;

            // Check for teaching opportunities
            // This could be expanded to check various game states and offer relevant education
            var currentTime = DateTime.Now;
            var timeSinceLastCheck = currentTime - lastEducationalCheck;

            if (timeSinceLastCheck.TotalMinutes >= 5) // Check every 5 minutes
            {
                // Offer relevant educational content based on game state
                ConsiderEducationalOpportunities();
                lastEducationalCheck = currentTime;
            }
        }

        /// <summary>
        /// Show educational content about breeding genetics
        /// </summary>
        public void ShowBreedingEducation(MonsterInstance parent1, MonsterInstance parent2, MonsterInstance offspring)
        {
            if (!enableEducationalContent) return;

            var educationalContent = new BreedingEducationContent
            {
                Title = "Understanding Genetic Inheritance",
                Parent1Traits = GetTraitSummary(parent1.GeneticProfile),
                Parent2Traits = GetTraitSummary(parent2.GeneticProfile),
                OffspringTraits = GetTraitSummary(offspring.GeneticProfile),
                ExplanationText = GenerateBreedingExplanation(parent1, parent2, offspring),
                Timestamp = DateTime.Now
            };

            // Log educational content for display
            Debug.Log($"🧬 Breeding Education: {educationalContent.ExplanationText}");

            // Publish event for UI systems to display
            eventBus?.Publish(new EducationalContentEvent("breeding", educationalContent));
        }

        /// <summary>
        /// Show educational content about activity performance
        /// </summary>
        public void ShowActivityEducation(ActivityType activityType, ActivityResult result)
        {
            if (!enableEducationalContent) return;

            var educationalContent = new ActivityEducationContent
            {
                ActivityType = activityType,
                PerformanceScore = result.PerformanceRating,
                Success = result.IsSuccess,
                EducationalTip = GetEducationalTip(activityType, null),
                ImprovementSuggestions = GenerateImprovementSuggestions(activityType, result),
                Timestamp = DateTime.Now
            };

            Debug.Log($"📚 Activity Education: {educationalContent.EducationalTip}");

            // Publish event for UI systems
            eventBus?.Publish(new EducationalContentEvent("activity", educationalContent));
        }

        #endregion

        #region Private Methods

        private void InitializeSubjects()
        {
            if (subjects == null || subjects.Length == 0)
            {
                // Create default educational subjects
                subjects = new SubjectConfig[]
                {
                    new SubjectConfig
                    {
                        id = "genetics",
                        name = "Monster Genetics",
                        description = "Learn about heredity and breeding",
                        lessons = new LessonInfo[]
                        {
                            new LessonInfo
                            {
                                id = "genetics_basics",
                                title = "Introduction to Genetics",
                                description = "Basic concepts of heredity",
                                progressValue = 10f,
                                requiredLevel = 1,
                                prerequisites = new string[0],
                                skillGains = new Dictionary<string, float> { { "genetics", 5f } }
                            },
                            new LessonInfo
                            {
                                id = "breeding_strategies",
                                title = "Breeding Strategies",
                                description = "Advanced breeding techniques",
                                progressValue = 15f,
                                requiredLevel = 5,
                                prerequisites = new string[] { "genetics_basics" },
                                skillGains = new Dictionary<string, float> { { "genetics", 8f }, { "strategy", 3f } }
                            }
                        }
                    },
                    new SubjectConfig
                    {
                        id = "care",
                        name = "Monster Care",
                        description = "Learn to care for your monsters",
                        lessons = new LessonInfo[]
                        {
                            new LessonInfo
                            {
                                id = "feeding_basics",
                                title = "Proper Feeding",
                                description = "How to feed your monsters correctly",
                                progressValue = 8f,
                                requiredLevel = 1,
                                prerequisites = new string[0],
                                skillGains = new Dictionary<string, float> { { "care", 5f } }
                            },
                            new LessonInfo
                            {
                                id = "health_management",
                                title = "Health Management",
                                description = "Keeping your monsters healthy",
                                progressValue = 12f,
                                requiredLevel = 3,
                                prerequisites = new string[] { "feeding_basics" },
                                skillGains = new Dictionary<string, float> { { "care", 7f }, { "health", 4f } }
                            }
                        }
                    },
                    new SubjectConfig
                    {
                        id = "activities",
                        name = "Monster Activities",
                        description = "Training and activity management",
                        lessons = new LessonInfo[]
                        {
                            new LessonInfo
                            {
                                id = "training_basics",
                                title = "Basic Training",
                                description = "Introduction to monster training",
                                progressValue = 10f,
                                requiredLevel = 2,
                                prerequisites = new string[0],
                                skillGains = new Dictionary<string, float> { { "training", 6f } }
                            }
                        }
                    }
                };
            }
        }

        private SubjectConfig GetSubject(string subjectId)
        {
            foreach (var subject in subjects)
            {
                if (subject.id == subjectId)
                    return subject;
            }
            return null;
        }

        private LessonInfo GetLesson(SubjectConfig subject, string lessonId)
        {
            if (subject == null) return null;

            foreach (var lesson in subject.lessons)
            {
                if (lesson.id == lessonId)
                    return lesson;
            }
            return null;
        }

        private bool HasPrerequisites(string playerId, LessonInfo lesson)
        {
            if (lesson.prerequisites == null || lesson.prerequisites.Length == 0)
                return true;

            if (!completedLessons.TryGetValue(playerId, out var completed))
                return false;

            foreach (var prereq in lesson.prerequisites)
            {
                bool hasPrereq = false;
                foreach (var completedLesson in completed)
                {
                    if (completedLesson.id == prereq)
                    {
                        hasPrereq = true;
                        break;
                    }
                }
                if (!hasPrereq) return false;
            }

            return true;
        }

        private bool IsLessonCompleted(string playerId, string lessonId)
        {
            if (!completedLessons.TryGetValue(playerId, out var completed))
                return false;

            foreach (var lesson in completed)
            {
                if (lesson.id == lessonId)
                    return true;
            }
            return false;
        }

        private void CheckSubjectMastery(string playerId, string subjectId, PlayerEducationProgress progress)
        {
            var subject = GetSubject(subjectId);
            if (subject == null) return;

            float totalProgress = progress.subjectProgress.GetValueOrDefault(subjectId, 0f);
            float masteryThreshold = subject.lessons.Length * 15f; // Average 15 progress per lesson

            if (totalProgress >= masteryThreshold && !progress.masteredSubjects.Contains(subjectId))
            {
                progress.masteredSubjects.Add(subjectId);
                eventBus?.Publish(new SubjectMasteredEvent(playerId, subjectId, subject.name));

                Debug.Log($"🎓 Subject mastered: {subject.name}");
            }
        }

        private void UpdateEducationalProgress()
        {
            // Apply knowledge retention (slight decay over time)
            foreach (var progress in playerProgress.Values)
            {
                foreach (var skill in progress.skills.Keys.ToList())
                {
                    progress.skills[skill] *= knowledgeRetentionRate;
                }
            }
        }

        // Educational tip generators
        private string GenerateRacingTip(MonsterInstance monster)
        {
            var tips = new[]
            {
                "Higher agility monsters perform better in racing activities!",
                "Well-fed monsters have more energy for racing.",
                "Practice makes perfect - regular racing improves performance!",
                "Racing builds agility and endurance in your monsters."
            };
            return tips[UnityEngine.Random.Range(0, tips.Length)];
        }

        private string GenerateCombatTip(MonsterInstance monster)
        {
            var tips = new[]
            {
                "Strength and vitality are key stats for combat success!",
                "Equipment can provide significant combat bonuses.",
                "Strategic thinking improves combat performance over time.",
                "Different monsters excel in different combat styles."
            };
            return tips[UnityEngine.Random.Range(0, tips.Length)];
        }

        private string GeneratePuzzleTip(MonsterInstance monster)
        {
            var tips = new[]
            {
                "Intelligence and focus help solve puzzles faster!",
                "Puzzle-solving improves your monster's cognitive abilities.",
                "Different puzzle types challenge different mental skills.",
                "Patient monsters often perform better in complex puzzles."
            };
            return tips[UnityEngine.Random.Range(0, tips.Length)];
        }

        private string GenerateStrategyTip(MonsterInstance monster)
        {
            var tips = new[]
            {
                "Strategy games develop planning and analytical skills!",
                "Social monsters often excel in team-based strategies.",
                "Understanding your monster's strengths helps in strategic planning.",
                "Experience in strategy games improves decision-making abilities."
            };
            return tips[UnityEngine.Random.Range(0, tips.Length)];
        }

        private string GenerateGeneralTip(MonsterInstance monster)
        {
            var tips = new[]
            {
                "Regular care and attention help monsters reach their full potential!",
                "Each monster has unique strengths - discover them through activities!",
                "Breeding can combine the best traits of parent monsters.",
                "Happy monsters perform better in all activities!"
            };
            return tips[UnityEngine.Random.Range(0, tips.Length)];
        }

        private void ConsiderEducationalOpportunities()
        {
            // This method could analyze current game state and suggest educational content
            // For now, it's a placeholder for future enhancement
        }

        private Dictionary<string, float> GetTraitSummary(IGeneticProfile geneticProfile)
        {
            var summary = new Dictionary<string, float>();
            var traitNames = new[] { "Strength", "Agility", "Intelligence", "Vitality", "Social", "Adaptability" };

            foreach (var trait in traitNames)
            {
                summary[trait] = geneticProfile.GetTraitValue(trait);
            }

            return summary;
        }

        private string GenerateBreedingExplanation(MonsterInstance parent1, MonsterInstance parent2, MonsterInstance offspring)
        {
            var explanations = new[]
            {
                $"{offspring.Name} inherited traits from both {parent1.Name} and {parent2.Name}!",
                "Genetic inheritance allows offspring to combine the best traits of their parents.",
                "Each breeding produces unique genetic combinations that affect monster abilities.",
                "Understanding genetics helps you breed stronger and more capable monsters."
            };

            return explanations[UnityEngine.Random.Range(0, explanations.Length)];
        }

        private List<string> GenerateImprovementSuggestions(ActivityType activityType, ActivityResult result)
        {
            var suggestions = new List<string>();

            if (result.PerformanceRating < 0.5f)
            {
                suggestions.Add($"Consider training monsters with better {activityType} abilities");
                suggestions.Add("Equipment upgrades can significantly improve performance");
                suggestions.Add("Make sure your monster is well-rested and happy");
            }
            else if (result.PerformanceRating < 0.8f)
            {
                suggestions.Add("Your monster is doing well! Continue regular training");
                suggestions.Add("Try experimenting with different equipment combinations");
            }
            else
            {
                suggestions.Add("Excellent performance! Your monster is well-suited for this activity");
                suggestions.Add("Consider breeding this monster to pass on superior traits");
            }

            return suggestions;
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class SubjectConfig
    {
        public string id;
        public string name;
        public string description;
        public LessonInfo[] lessons;
    }

    [System.Serializable]
    public class LessonInfo
    {
        public string id;
        public string title;
        public string description;
        public float progressValue;
        public int requiredLevel;
        public string[] prerequisites;
        public Dictionary<string, float> skillGains = new();
    }

    [System.Serializable]
    public class PlayerEducationProgress
    {
        public Dictionary<string, float> subjectProgress = new();
        public Dictionary<string, float> skills = new();
        public List<string> masteredSubjects = new();
        public DateTime lastActive = DateTime.Now;
    }

    [System.Serializable]
    public class Lesson
    {
        public string id;
        public string title;
        public string subject;
        public float completionScore;
        public DateTime completedDate;
    }

    [System.Serializable]
    public class BreedingEducationContent
    {
        public string Title;
        public Dictionary<string, float> Parent1Traits;
        public Dictionary<string, float> Parent2Traits;
        public Dictionary<string, float> OffspringTraits;
        public string ExplanationText;
        public DateTime Timestamp;
    }

    [System.Serializable]
    public class ActivityEducationContent
    {
        public ActivityType ActivityType;
        public float PerformanceScore;
        public bool Success;
        public string EducationalTip;
        public List<string> ImprovementSuggestions;
        public DateTime Timestamp;
    }

    // Educational Events
    public class EducationalContentEvent
    {
        public string ContentType { get; }
        public object Content { get; }
        public DateTime Timestamp { get; }

        public EducationalContentEvent(string contentType, object content)
        {
            ContentType = contentType;
            Content = content;
            Timestamp = DateTime.Now;
        }
    }

    public class LessonStartedEvent
    {
        public string PlayerId { get; }
        public string SubjectId { get; }
        public string LessonId { get; }
        public DateTime Timestamp { get; }

        public LessonStartedEvent(string playerId, string subjectId, string lessonId)
        {
            PlayerId = playerId;
            SubjectId = subjectId;
            LessonId = lessonId;
            Timestamp = DateTime.Now;
        }
    }

    public class LessonCompletedEvent
    {
        public string PlayerId { get; }
        public Lesson Lesson { get; }
        public float ProgressGained { get; }
        public DateTime Timestamp { get; }

        public LessonCompletedEvent(string playerId, Lesson lesson, float progressGained)
        {
            PlayerId = playerId;
            Lesson = lesson;
            ProgressGained = progressGained;
            Timestamp = DateTime.Now;
        }
    }

    public class SubjectMasteredEvent
    {
        public string PlayerId { get; }
        public string SubjectId { get; }
        public string SubjectName { get; }
        public DateTime Timestamp { get; }

        public SubjectMasteredEvent(string playerId, string subjectId, string subjectName)
        {
            PlayerId = playerId;
            SubjectId = subjectId;
            SubjectName = subjectName;
            Timestamp = DateTime.Now;
        }
    }

    #endregion
}