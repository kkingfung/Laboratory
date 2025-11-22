using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.MonsterTown;

namespace Laboratory.Core.Education
{
    /// <summary>
    /// Educational Content System - Teaches real genetics and biology through gameplay
    ///
    /// Key Features:
    /// - Real genetics explanations integrated with monster breeding
    /// - Adaptive learning content that adjusts to player knowledge
    /// - Scientific method teaching through breeding experiments
    /// - Biology concepts connected to monster performance
    /// - Educational assessments and progress tracking
    /// - Teacher tools for classroom integration
    /// </summary>
    public class EducationalContentSystem : MonoBehaviour
    {
        [Header("📚 Educational Configuration")]
        [SerializeField] private EducationalConfig educationalConfig;
        [SerializeField] private bool enableEducationalMode = true;
        [SerializeField] private LearningLevel defaultLearningLevel = LearningLevel.Beginner;
        [SerializeField] private bool enableAssessments = true;

        [Header("🎓 Learning Progress")]
        [SerializeField] private bool trackLearningProgress = true;
        [SerializeField] private float knowledgeRetentionPeriod = 2592000f; // 30 days
        [SerializeField] private int conceptReviewThreshold = 3;

        [Header("👨‍🏫 Teacher Tools")]
        [SerializeField] private bool enableTeacherMode = false;
        [SerializeField] private ClassroomConfig classroomConfig;

        // Educational state
        private Dictionary<string, PlayerLearningProfile> _learningProfiles = new();
        private Dictionary<string, EducationalContent> _educationalContent = new();
        private Dictionary<string, LearningAssessment> _assessments = new();
        private Dictionary<string, List<BreedingExperiment>> _experiments = new();
        private List<ConceptExplanation> _conceptLibrary = new();

        // Events
        public event Action<ConceptExplanation> OnConceptLearned;
        public event Action<LearningAssessment> OnAssessmentCompleted;
        public event Action<BreedingExperiment> OnExperimentConcluded;
        public event Action<PlayerLearningProfile> OnLearningProgressUpdated;

        #region Initialization

        public void InitializeEducationalSystem(EducationalConfig config)
        {
            educationalConfig = config;
            InitializeConceptLibrary();
            InitializeLearningProfiles();
            InitializeAssessments();

            if (enableTeacherMode)
            {
                InitializeTeacherTools();
            }

            Debug.Log("📚 Educational Content System initialized");
        }

        private void InitializeConceptLibrary()
        {
            _conceptLibrary = new List<ConceptExplanation>
            {
                // Basic Genetics Concepts
                new ConceptExplanation
                {
                    ConceptId = "basic_genetics",
                    Title = "Introduction to Genetics",
                    Level = LearningLevel.Beginner,
                    Category = EducationalCategory.Genetics,
                    ShortDescription = "What are genes and how do they work?",
                    DetailedExplanation = @"Genes are like instruction manuals that tell living things how to grow and what traits they'll have.
                        In our monsters, genes control things like strength, speed, and even appearance.
                        Every monster gets half their genes from each parent, just like real animals!",
                    RealWorldConnection = "This is exactly how genetics works in real life - you get half your genes from your mom and half from your dad.",
                    InteractiveElements = new List<string> { "breeding_demonstration", "trait_inheritance_simulator" },
                    Prerequisites = new List<string>(),
                    NextConcepts = new List<string> { "dominant_recessive", "genetic_variation" }
                },

                new ConceptExplanation
                {
                    ConceptId = "dominant_recessive",
                    Title = "Dominant and Recessive Traits",
                    Level = LearningLevel.Intermediate,
                    Category = EducationalCategory.Genetics,
                    ShortDescription = "Why some traits show up and others hide",
                    DetailedExplanation = @"Some genes are 'dominant' - they're like loud voices that always get heard.
                        Others are 'recessive' - they're quiet and only show up when both parents have them.
                        In monster breeding, a strong parent might pass on dominant strength genes that override weak genes from the other parent.",
                    RealWorldConnection = "Eye color in humans follows this pattern - brown eyes are usually dominant over blue eyes.",
                    InteractiveElements = new List<string> { "trait_expression_simulator", "breeding_outcome_predictor" },
                    Prerequisites = new List<string> { "basic_genetics" },
                    NextConcepts = new List<string> { "genetic_probability", "mutations" }
                },

                new ConceptExplanation
                {
                    ConceptId = "genetic_variation",
                    Title = "Genetic Diversity and Variation",
                    Level = LearningLevel.Intermediate,
                    Category = EducationalCategory.Evolution,
                    ShortDescription = "Why genetic diversity makes populations stronger",
                    DetailedExplanation = @"Genetic diversity is like having many different tools in a toolbox.
                        When monsters have varied genetics, the population can adapt to different challenges.
                        Inbreeding reduces this diversity and can cause problems, which is why mixing different genetic lines is important.",
                    RealWorldConnection = "Wild animal populations need genetic diversity to survive diseases and environmental changes.",
                    InteractiveElements = new List<string> { "population_simulator", "diversity_calculator" },
                    Prerequisites = new List<string> { "basic_genetics" },
                    NextConcepts = new List<string> { "population_genetics", "conservation_genetics" }
                },

                // Advanced Concepts
                new ConceptExplanation
                {
                    ConceptId = "mutations",
                    Title = "Genetic Mutations",
                    Level = LearningLevel.Advanced,
                    Category = EducationalCategory.Genetics,
                    ShortDescription = "How new traits appear and spread",
                    DetailedExplanation = @"Mutations are changes in genes that create new traits. Most mutations are neutral or harmful,
                        but occasionally one appears that gives an advantage. In monster breeding, rare beneficial mutations
                        can create entirely new abilities or enhance existing ones.",
                    RealWorldConnection = "Evolution depends on mutations to create new traits that help species adapt.",
                    InteractiveElements = new List<string> { "mutation_simulator", "evolution_timeline" },
                    Prerequisites = new List<string> { "dominant_recessive", "genetic_variation" },
                    NextConcepts = new List<string> { "evolutionary_adaptation" }
                },

                // Biology Integration
                new ConceptExplanation
                {
                    ConceptId = "phenotype_genotype",
                    Title = "Genotype vs Phenotype",
                    Level = LearningLevel.Intermediate,
                    Category = EducationalCategory.Biology,
                    ShortDescription = "The difference between genetic code and observable traits",
                    DetailedExplanation = @"Genotype is the genetic 'recipe' - the actual genes a monster has.
                        Phenotype is what you can see and measure - the monster's actual strength, color, and behavior.
                        The environment can influence how genes are expressed, so two monsters with similar genes might look different.",
                    RealWorldConnection = "Identical twins have the same genotype but can have different phenotypes due to environmental factors.",
                    InteractiveElements = new List<string> { "genotype_analyzer", "environmental_factors_simulator" },
                    Prerequisites = new List<string> { "basic_genetics" },
                    NextConcepts = new List<string> { "environmental_genetics", "gene_expression" }
                }
            };

            foreach (var concept in _conceptLibrary)
            {
                _educationalContent[concept.ConceptId] = new EducationalContent
                {
                    ContentId = concept.ConceptId,
                    Explanation = concept,
                    LearningActivities = GenerateLearningActivities(concept),
                    AssessmentQuestions = GenerateAssessmentQuestions(concept)
                };
            }
        }

        private void InitializeLearningProfiles()
        {
            // Create default learning profile for local player
            var defaultProfile = new PlayerLearningProfile
            {
                PlayerId = "LocalPlayer",
                CurrentLevel = defaultLearningLevel,
                ConceptsLearned = new Dictionary<string, ConceptProgress>(),
                LearningStrengths = new List<LearningStyle> { LearningStyle.Visual, LearningStyle.Interactive },
                PreferredPace = LearningPace.SelfPaced,
                LastActiveDate = DateTime.UtcNow
            };

            _learningProfiles["LocalPlayer"] = defaultProfile;
        }

        private void InitializeAssessments()
        {
            // Create assessments for each educational concept
            foreach (var concept in _conceptLibrary)
            {
                var assessment = new LearningAssessment
                {
                    AssessmentId = $"{concept.ConceptId}_assessment",
                    ConceptId = concept.ConceptId,
                    Title = $"{concept.Title} Understanding Check",
                    Questions = GenerateAssessmentQuestions(concept),
                    PassingScore = 0.7f,
                    MaxAttempts = 3
                };

                _assessments[assessment.AssessmentId] = assessment;
            }
        }

        private void InitializeTeacherTools()
        {
            if (classroomConfig == null)
            {
                classroomConfig = new ClassroomConfig
                {
                    ClassName = "Demo Biology Class",
                    GradeLevel = "Middle School",
                    LearningObjectives = new List<string>
                    {
                        "Understand basic genetic inheritance",
                        "Apply scientific method to breeding experiments",
                        "Analyze the relationship between genetics and traits"
                    }
                };
            }

            Debug.Log("👨‍🏫 Teacher tools initialized for classroom mode");
        }

        #endregion

        #region Educational Content Delivery

        /// <summary>
        /// Get educational content triggered by a breeding result
        /// </summary>
        public EducationalContent GetBreedingEducationContent(BreedingResult breedingResult, string playerId)
        {
            // Check if educational mode is enabled
            if (!enableEducationalMode)
            {
                return null;
            }

            var learningProfile = GetLearningProfile(playerId);
            var appropriateConcepts = FindAppropriateConcepts(breedingResult, learningProfile);

            if (appropriateConcepts.Any())
            {
                var selectedConcept = SelectBestConcept(appropriateConcepts, learningProfile);
                var content = _educationalContent[selectedConcept.ConceptId];

                // Customize content based on the specific breeding result
                content.ContextualizedContent = ContextualizeContent(content, breedingResult);

                // Record learning interaction
                RecordLearningInteraction(playerId, selectedConcept.ConceptId, LearningInteractionType.BreedingTriggered);

                Debug.Log($"📚 Providing educational content: {selectedConcept.Title}");
                return content;
            }

            return null;
        }

        /// <summary>
        /// Get educational content for a specific concept
        /// </summary>
        public EducationalContent GetConceptContent(string conceptId, string playerId)
        {
            // Check if educational mode is enabled
            if (!enableEducationalMode)
            {
                return null;
            }

            if (!_educationalContent.TryGetValue(conceptId, out var content))
            {
                Debug.LogWarning($"Educational content not found for concept: {conceptId}");
                return null;
            }

            var learningProfile = GetLearningProfile(playerId);

            // Adapt content to player's learning level and style
            content = AdaptContentToLearner(content, learningProfile);

            // Record learning interaction
            RecordLearningInteraction(playerId, conceptId, LearningInteractionType.DirectAccess);

            return content;
        }

        /// <summary>
        /// Provide explanation for activity performance based on genetics
        /// </summary>
        public PerformanceExplanation ExplainActivityPerformance(Monster monster, ActivityType activityType, MonsterPerformance performance)
        {
            var explanation = new PerformanceExplanation
            {
                Monster = monster,
                ActivityType = activityType,
                Performance = performance,
                GeneticFactors = AnalyzeGeneticFactors(monster, activityType),
                EducationalInsights = GenerateEducationalInsights(monster, activityType, performance),
                ImprovementSuggestions = GenerateImprovementSuggestions(monster, activityType)
            };

            return explanation;
        }

        private List<ConceptExplanation> FindAppropriateConcepts(BreedingResult breedingResult, PlayerLearningProfile learningProfile)
        {
            var appropriateConcepts = new List<ConceptExplanation>();

            // Find concepts that are relevant to this breeding result and appropriate for the player's level
            foreach (var concept in _conceptLibrary)
            {
                if (IsConceptAppropriate(concept, breedingResult, learningProfile))
                {
                    appropriateConcepts.Add(concept);
                }
            }

            return appropriateConcepts;
        }

        private bool IsConceptAppropriate(ConceptExplanation concept, BreedingResult breedingResult, PlayerLearningProfile learningProfile)
        {
            // Check if concept is at appropriate level
            if (concept.Level > learningProfile.CurrentLevel + 1) return false;

            // Check if prerequisites are met
            foreach (var prerequisite in concept.Prerequisites)
            {
                if (!learningProfile.ConceptsLearned.ContainsKey(prerequisite))
                    return false;
            }

            // Check if concept is relevant to this breeding result
            if (concept.Category == EducationalCategory.Genetics && breedingResult.IsGeneticallyInteresting)
                return true;

            if (concept.ConceptId == "mutations" && breedingResult.HasMutation)
                return true;

            if (concept.ConceptId == "dominant_recessive" && breedingResult.ShowsInheritancePatterns)
                return true;

            return false;
        }

        #endregion

        #region Learning Progress Tracking

        /// <summary>
        /// Record a learning interaction
        /// </summary>
        public void RecordLearningInteraction(string playerId, string conceptId, LearningInteractionType interactionType)
        {
            // Check if learning progress tracking is enabled
            if (!trackLearningProgress)
            {
                return;
            }

            var learningProfile = GetLearningProfile(playerId);

            if (!learningProfile.ConceptsLearned.ContainsKey(conceptId))
            {
                learningProfile.ConceptsLearned[conceptId] = new ConceptProgress
                {
                    ConceptId = conceptId,
                    FirstEncounterDate = DateTime.UtcNow,
                    EncounterCount = 0,
                    MasteryLevel = 0f
                };
            }

            var progress = learningProfile.ConceptsLearned[conceptId];
            progress.EncounterCount++;
            progress.LastEncounterDate = DateTime.UtcNow;

            // Update mastery level based on interaction type
            var masteryIncrease = interactionType switch
            {
                LearningInteractionType.BreedingTriggered => 0.2f,
                LearningInteractionType.DirectAccess => 0.1f,
                LearningInteractionType.AssessmentPassed => 0.3f,
                LearningInteractionType.ExperimentConducted => 0.25f,
                _ => 0.1f
            };

            progress.MasteryLevel = Mathf.Min(1f, progress.MasteryLevel + masteryIncrease);

            // Check if concept needs review based on retention period
            CheckKnowledgeRetention(progress);

            // Check if concept is now mastered
            if (progress.MasteryLevel >= 0.8f && !progress.IsMastered)
            {
                progress.IsMastered = true;
                progress.MasteryDate = DateTime.UtcNow;

                OnConceptLearned?.Invoke(_conceptLibrary.First(c => c.ConceptId == conceptId));

                // Update learning level if appropriate
                UpdateLearningLevel(learningProfile);
            }

            OnLearningProgressUpdated?.Invoke(learningProfile);

            Debug.Log($"📈 Learning progress: {conceptId} mastery now {progress.MasteryLevel:P0}");
        }

        /// <summary>
        /// Check if knowledge needs review based on retention period
        /// </summary>
        private void CheckKnowledgeRetention(ConceptProgress progress)
        {
            // Calculate time since last encounter
            var timeSinceLastEncounter = (DateTime.UtcNow - progress.LastEncounterDate).TotalSeconds;

            // If beyond retention period, reduce mastery
            if (timeSinceLastEncounter > knowledgeRetentionPeriod)
            {
                var decayFactor = 0.1f; // Decay 10% per retention period exceeded
                progress.MasteryLevel = Mathf.Max(0f, progress.MasteryLevel - decayFactor);
                progress.NeedsReview = true;
                Debug.LogWarning($"⏰ Concept {progress.ConceptId} needs review (last encounter: {timeSinceLastEncounter / 86400:F1} days ago)");
            }

            // Mark for review if encountered threshold times but not mastered
            if (progress.EncounterCount >= conceptReviewThreshold && progress.MasteryLevel < 0.8f)
            {
                progress.NeedsReview = true;
                Debug.Log($"📖 Concept {progress.ConceptId} needs review (encountered {progress.EncounterCount} times, mastery: {progress.MasteryLevel:P0})");
            }
        }

        /// <summary>
        /// Conduct a learning assessment
        /// </summary>
        public System.Threading.Tasks.Task<AssessmentResult> ConductAssessment(string assessmentId, string playerId, List<string> answers)
        {
            // Check if assessments are enabled
            if (!enableAssessments)
            {
                Debug.LogWarning("Assessments are disabled");
                return System.Threading.Tasks.Task.FromResult<AssessmentResult>(null);
            }

            if (!_assessments.TryGetValue(assessmentId, out var assessment))
            {
                Debug.LogWarning($"Assessment not found: {assessmentId}");
                return System.Threading.Tasks.Task.FromResult<AssessmentResult>(null);
            }

            var score = CalculateAssessmentScore(assessment, answers);
            var passed = score >= assessment.PassingScore;

            var result = new AssessmentResult
            {
                AssessmentId = assessmentId,
                PlayerId = playerId,
                Score = score,
                Passed = passed,
                CompletedAt = DateTime.UtcNow,
                DetailedFeedback = GenerateAssessmentFeedback(assessment, answers, score)
            };

            if (passed)
            {
                RecordLearningInteraction(playerId, assessment.ConceptId, LearningInteractionType.AssessmentPassed);
            }

            OnAssessmentCompleted?.Invoke(assessment);

            Debug.Log($"📝 Assessment completed: {assessmentId}, Score: {score:P0}, Passed: {passed}");
            return System.Threading.Tasks.Task.FromResult(result);
        }

        /// <summary>
        /// Start a breeding experiment for educational purposes
        /// </summary>
        public BreedingExperiment StartBreedingExperiment(string playerId, string hypothesis, Monster parent1, Monster parent2)
        {
            var experiment = new BreedingExperiment
            {
                ExperimentId = Guid.NewGuid().ToString(),
                PlayerId = playerId,
                Hypothesis = hypothesis,
                Parent1 = parent1,
                Parent2 = parent2,
                StartDate = DateTime.UtcNow,
                Status = ExperimentStatus.InProgress,
                PredictedOutcomes = GenerateBreedingPredictions(parent1, parent2)
            };

            if (!_experiments.ContainsKey(playerId))
                _experiments[playerId] = new List<BreedingExperiment>();

            _experiments[playerId].Add(experiment);

            Debug.Log($"🔬 Breeding experiment started: {hypothesis}");
            return experiment;
        }

        /// <summary>
        /// Complete a breeding experiment with results
        /// </summary>
        public void CompleteBreedingExperiment(string experimentId, Monster offspring)
        {
            foreach (var playerExperiments in _experiments.Values)
            {
                var experiment = playerExperiments.FirstOrDefault(e => e.ExperimentId == experimentId);
                if (experiment != null)
                {
                    experiment.ActualOffspring = offspring;
                    experiment.CompletionDate = DateTime.UtcNow;
                    experiment.Status = ExperimentStatus.Completed;
                    experiment.Results = AnalyzeExperimentResults(experiment);

                    // Award learning progress
                    RecordLearningInteraction(experiment.PlayerId, "scientific_method", LearningInteractionType.ExperimentConducted);

                    OnExperimentConcluded?.Invoke(experiment);

                    Debug.Log($"🔬 Breeding experiment completed: {experiment.Hypothesis}");
                    break;
                }
            }
        }

        #endregion

        #region Utility Methods

        private PlayerLearningProfile GetLearningProfile(string playerId)
        {
            if (!_learningProfiles.ContainsKey(playerId))
            {
                _learningProfiles[playerId] = CreateDefaultLearningProfile(playerId);
            }

            return _learningProfiles[playerId];
        }

        private PlayerLearningProfile CreateDefaultLearningProfile(string playerId)
        {
            return new PlayerLearningProfile
            {
                PlayerId = playerId,
                CurrentLevel = defaultLearningLevel,
                ConceptsLearned = new Dictionary<string, ConceptProgress>(),
                LearningStrengths = new List<LearningStyle> { LearningStyle.Interactive },
                PreferredPace = LearningPace.SelfPaced,
                LastActiveDate = DateTime.UtcNow
            };
        }

        private ConceptExplanation SelectBestConcept(List<ConceptExplanation> concepts, PlayerLearningProfile learningProfile)
        {
            // Prioritize concepts the player hasn't seen recently
            var scoredConcepts = concepts.Select(c => new
            {
                Concept = c,
                Score = CalculateConceptRelevanceScore(c, learningProfile)
            }).OrderByDescending(x => x.Score);

            return scoredConcepts.First().Concept;
        }

        private float CalculateConceptRelevanceScore(ConceptExplanation concept, PlayerLearningProfile learningProfile)
        {
            float score = 1f;

            // Prefer concepts at appropriate level
            if (concept.Level == learningProfile.CurrentLevel) score += 0.5f;

            // Prefer concepts not recently learned
            if (learningProfile.ConceptsLearned.TryGetValue(concept.ConceptId, out var progress))
            {
                var daysSinceLastEncounter = (DateTime.UtcNow - progress.LastEncounterDate).TotalDays;
                score += (float)daysSinceLastEncounter * 0.1f;
            }
            else
            {
                score += 1f; // New concepts get bonus
            }

            return score;
        }

        private EducationalContent AdaptContentToLearner(EducationalContent content, PlayerLearningProfile learningProfile)
        {
            // Adapt content based on learning style preferences
            var adaptedContent = new EducationalContent
            {
                ContentId = content.ContentId,
                Explanation = content.Explanation,
                LearningActivities = content.LearningActivities,
                AssessmentQuestions = content.AssessmentQuestions
            };

            // Customize explanation complexity
            if (learningProfile.CurrentLevel == LearningLevel.Beginner)
            {
                adaptedContent.Explanation.DetailedExplanation = SimplifyExplanation(content.Explanation.DetailedExplanation);
            }

            return adaptedContent;
        }

        private string ContextualizeContent(EducationalContent content, BreedingResult breedingResult)
        {
            var contextual = content.Explanation.DetailedExplanation;

            // Add specific examples from the breeding result
            contextual += $"\n\nIn your recent breeding, you can see this concept in action: ";

            if (breedingResult.ShowsInheritancePatterns)
            {
                contextual += "Notice how certain traits from the parents appeared in the offspring. ";
            }

            if (breedingResult.HasMutation)
            {
                contextual += "The rare mutation that appeared demonstrates how genetic variation occurs naturally. ";
            }

            return contextual;
        }

        private string SimplifyExplanation(string explanation)
        {
            // Simple text processing to make explanations more beginner-friendly
            return explanation.Replace("genetic", "hereditary")
                            .Replace("alleles", "gene variants")
                            .Replace("phenotype", "observable traits");
        }

        private void UpdateLearningLevel(PlayerLearningProfile learningProfile)
        {
            var masteredConcepts = learningProfile.ConceptsLearned.Values.Count(c => c.IsMastered);

            var newLevel = masteredConcepts switch
            {
                >= 8 => LearningLevel.Expert,
                >= 5 => LearningLevel.Advanced,
                >= 2 => LearningLevel.Intermediate,
                _ => LearningLevel.Beginner
            };

            if (newLevel > learningProfile.CurrentLevel)
            {
                learningProfile.CurrentLevel = newLevel;
                Debug.Log($"🎓 Learning level increased to {newLevel}");
            }
        }

        private List<LearningActivity> GenerateLearningActivities(ConceptExplanation concept)
        {
            var activities = new List<LearningActivity>
            {
                new LearningActivity
                {
                    ActivityId = $"{concept.ConceptId}_simulation",
                    Title = $"{concept.Title} Simulation",
                    Type = ActivityType.Interactive,
                    Description = $"Interactive simulation to explore {concept.Title.ToLower()}",
                    EstimatedDuration = 300f // 5 minutes
                },
                new LearningActivity
                {
                    ActivityId = $"{concept.ConceptId}_quiz",
                    Title = $"{concept.Title} Quick Check",
                    Type = ActivityType.Assessment,
                    Description = $"Test your understanding of {concept.Title.ToLower()}",
                    EstimatedDuration = 120f // 2 minutes
                }
            };

            return activities;
        }

        private List<AssessmentQuestion> GenerateAssessmentQuestions(ConceptExplanation concept)
        {
            var questions = new List<AssessmentQuestion>();

            // Generate concept-specific questions
            switch (concept.ConceptId)
            {
                case "basic_genetics":
                    questions.Add(new AssessmentQuestion
                    {
                        QuestionText = "What determines a monster's traits?",
                        Options = new List<string> { "Their environment", "Their genes", "Their diet", "Random chance" },
                        CorrectAnswerIndex = 1,
                        Explanation = "Genes are the instructions that determine how traits develop in monsters."
                    });
                    break;

                case "dominant_recessive":
                    questions.Add(new AssessmentQuestion
                    {
                        QuestionText = "If a monster has one dominant and one recessive gene for strength, what will their strength be like?",
                        Options = new List<string> { "Weak", "Average", "Strong", "Unpredictable" },
                        CorrectAnswerIndex = 2,
                        Explanation = "Dominant genes override recessive ones, so the monster will be strong."
                    });
                    break;

                default:
                    questions.Add(new AssessmentQuestion
                    {
                        QuestionText = $"Which statement best describes {concept.Title.ToLower()}?",
                        Options = new List<string> { "Option A", "Option B", "Option C", "Option D" },
                        CorrectAnswerIndex = 0,
                        Explanation = "This is a general understanding check."
                    });
                    break;
            }

            return questions;
        }

        private float CalculateAssessmentScore(LearningAssessment assessment, List<string> answers)
        {
            if (answers.Count != assessment.Questions.Count)
                return 0f;

            int correctAnswers = 0;
            for (int i = 0; i < assessment.Questions.Count; i++)
            {
                var question = assessment.Questions[i];
                var answerIndex = question.Options.IndexOf(answers[i]);
                if (answerIndex == question.CorrectAnswerIndex)
                    correctAnswers++;
            }

            return (float)correctAnswers / assessment.Questions.Count;
        }

        private List<string> GenerateAssessmentFeedback(LearningAssessment assessment, List<string> answers, float score)
        {
            var feedback = new List<string>();

            if (score >= assessment.PassingScore)
            {
                feedback.Add("Great work! You've demonstrated good understanding of this concept.");
            }
            else
            {
                feedback.Add("You're making progress! Review the concept and try again.");
            }

            // Add specific feedback for each question
            for (int i = 0; i < assessment.Questions.Count && i < answers.Count; i++)
            {
                var question = assessment.Questions[i];
                var answerIndex = question.Options.IndexOf(answers[i]);

                if (answerIndex == question.CorrectAnswerIndex)
                {
                    feedback.Add($"Question {i + 1}: Correct! {question.Explanation}");
                }
                else
                {
                    feedback.Add($"Question {i + 1}: Not quite. {question.Explanation}");
                }
            }

            return feedback;
        }

        private List<GeneticFactor> AnalyzeGeneticFactors(Monster monster, ActivityType activityType)
        {
            var factors = new List<GeneticFactor>();

            // Analyze how monster's genetics affect performance in this activity
            switch (activityType)
            {
                case ActivityType.Racing:
                    factors.Add(new GeneticFactor
                    {
                        FactorName = "Speed Genes",
                        Influence = monster.Stats.speed / 100f,
                        Explanation = "Higher speed genes directly improve racing performance"
                    });
                    factors.Add(new GeneticFactor
                    {
                        FactorName = "Agility Genes",
                        Influence = monster.Stats.agility / 100f,
                        Explanation = "Agility genes help with quick turns and obstacle navigation"
                    });
                    break;

                case ActivityType.Combat:
                    factors.Add(new GeneticFactor
                    {
                        FactorName = "Strength Genes",
                        Influence = monster.Stats.strength / 100f,
                        Explanation = "Strength genes determine attack power and endurance"
                    });
                    break;
            }

            return factors;
        }

        private List<string> GenerateEducationalInsights(Monster monster, ActivityType activityType, MonsterPerformance performance)
        {
            var insights = new List<string>();

            insights.Add($"Your monster's performance was influenced by their genetic makeup:");
            insights.Add($"• Base genetic potential contributed {performance.geneticBonus:P0} to the result");
            insights.Add($"• This shows how inherited traits directly affect abilities");

            if (performance.equipmentBonus > 0)
            {
                insights.Add($"• Equipment provided an additional {performance.equipmentBonus:P0} boost");
                insights.Add($"• This demonstrates how environment (equipment) can enhance genetic potential");
            }

            return insights;
        }

        private List<string> GenerateImprovementSuggestions(Monster monster, ActivityType activityType)
        {
            var suggestions = new List<string>();

            suggestions.Add("Ways to improve performance:");
            suggestions.Add("• Breed with monsters that have strong genetics for this activity");
            suggestions.Add("• Equip gear that enhances relevant stats");
            suggestions.Add("• Practice regularly to gain experience bonuses");

            return suggestions;
        }

        private List<BreedingPrediction> GenerateBreedingPredictions(Monster parent1, Monster parent2)
        {
            // Generate educational predictions about breeding outcomes
            var predictions = new List<BreedingPrediction>
            {
                new BreedingPrediction
                {
                    TraitName = "Strength",
                    MinValue = Mathf.Min(parent1.Stats.strength, parent2.Stats.strength),
                    MaxValue = Mathf.Max(parent1.Stats.strength, parent2.Stats.strength),
                    MostLikelyValue = (parent1.Stats.strength + parent2.Stats.strength) / 2f,
                    Explanation = "Strength will likely be between the parents' values, averaging around the middle"
                }
            };

            return predictions;
        }

        private ExperimentResults AnalyzeExperimentResults(BreedingExperiment experiment)
        {
            var results = new ExperimentResults
            {
                HypothesisCorrect = false, // Would analyze actual results vs predictions
                LearningOutcomes = new List<string>
                {
                    "Observed how parental traits influence offspring",
                    "Practiced forming and testing scientific hypotheses",
                    "Learned about genetic inheritance patterns"
                },
                ConceptsReinforced = new List<string> { "basic_genetics", "inheritance_patterns" }
            };

            return results;
        }

        /// <summary>
        /// Get learning progress summary for a player
        /// </summary>
        public LearningProgressSummary GetLearningProgress(string playerId)
        {
            var profile = GetLearningProfile(playerId);

            return new LearningProgressSummary
            {
                CurrentLevel = profile.CurrentLevel,
                ConceptsMastered = profile.ConceptsLearned.Values.Count(c => c.IsMastered),
                TotalConcepts = _conceptLibrary.Count,
                RecentAchievements = GetRecentLearningAchievements(playerId),
                NextRecommendedConcepts = GetNextRecommendedConcepts(profile)
            };
        }

        private List<string> GetRecentLearningAchievements(string playerId)
        {
            var profile = GetLearningProfile(playerId);
            var recentDate = DateTime.UtcNow.AddDays(-7); // Last week

            return profile.ConceptsLearned.Values
                .Where(c => c.IsMastered && c.MasteryDate > recentDate)
                .Select(c => $"Mastered: {_conceptLibrary.First(cl => cl.ConceptId == c.ConceptId).Title}")
                .ToList();
        }

        private List<string> GetNextRecommendedConcepts(PlayerLearningProfile profile)
        {
            return _conceptLibrary
                .Where(c => c.Level <= profile.CurrentLevel + 1)
                .Where(c => !profile.ConceptsLearned.ContainsKey(c.ConceptId))
                .Where(c => c.Prerequisites.All(p => profile.ConceptsLearned.ContainsKey(p)))
                .Select(c => c.Title)
                .Take(3)
                .ToList();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Educational system configuration
    /// </summary>
    [Serializable]
    public class EducationalConfig
    {
        [Header("Learning Settings")]
        public bool enableAdaptiveLearning = true;
        public bool enableAssessments = true;
        public bool enableTeacherTools = false;

        [Header("Content Delivery")]
        public float conceptTriggerProbability = 0.7f;
        public int maxConceptsPerSession = 3;
        public bool enableContextualTriggers = true;
    }

    /// <summary>
    /// Educational concept explanation
    /// </summary>
    [Serializable]
    public class ConceptExplanation
    {
        public string ConceptId;
        public string Title;
        public LearningLevel Level;
        public EducationalCategory Category;
        public string ShortDescription;
        [TextArea(5, 10)]
        public string DetailedExplanation;
        [TextArea(3, 5)]
        public string RealWorldConnection;
        public List<string> InteractiveElements = new();
        public List<string> Prerequisites = new();
        public List<string> NextConcepts = new();
    }

    /// <summary>
    /// Educational content package
    /// </summary>
    [Serializable]
    public class EducationalContent
    {
        public string ContentId;
        public ConceptExplanation Explanation;
        public List<LearningActivity> LearningActivities = new();
        public List<AssessmentQuestion> AssessmentQuestions = new();
        public string ContextualizedContent;
    }

    /// <summary>
    /// Player learning profile
    /// </summary>
    [Serializable]
    public class PlayerLearningProfile
    {
        public string PlayerId;
        public LearningLevel CurrentLevel;
        public Dictionary<string, ConceptProgress> ConceptsLearned = new();
        public List<LearningStyle> LearningStrengths = new();
        public LearningPace PreferredPace;
        public DateTime LastActiveDate;
    }

    /// <summary>
    /// Concept learning progress
    /// </summary>
    [Serializable]
    public class ConceptProgress
    {
        public string ConceptId;
        public DateTime FirstEncounterDate;
        public DateTime LastEncounterDate;
        public DateTime MasteryDate;
        public int EncounterCount;
        public float MasteryLevel; // 0-1
        public bool IsMastered;
    }

    /// <summary>
    /// Learning assessment
    /// </summary>
    [Serializable]
    public class LearningAssessment
    {
        public string AssessmentId;
        public string ConceptId;
        public string Title;
        public List<AssessmentQuestion> Questions = new();
        public float PassingScore = 0.7f;
        public int MaxAttempts = 3;
    }

    /// <summary>
    /// Assessment question
    /// </summary>
    [Serializable]
    public class AssessmentQuestion
    {
        public string QuestionText;
        public List<string> Options = new();
        public int CorrectAnswerIndex;
        public string Explanation;
    }

    /// <summary>
    /// Assessment result
    /// </summary>
    [Serializable]
    public class AssessmentResult
    {
        public string AssessmentId;
        public string PlayerId;
        public float Score;
        public bool Passed;
        public DateTime CompletedAt;
        public List<string> DetailedFeedback = new();
    }

    /// <summary>
    /// Breeding experiment for scientific learning
    /// </summary>
    [Serializable]
    public class BreedingExperiment
    {
        public string ExperimentId;
        public string PlayerId;
        public string Hypothesis;
        public Monster Parent1;
        public Monster Parent2;
        public Monster ActualOffspring;
        public DateTime StartDate;
        public DateTime CompletionDate;
        public ExperimentStatus Status;
        public List<BreedingPrediction> PredictedOutcomes = new();
        public ExperimentResults Results;
    }

    /// <summary>
    /// Performance explanation with educational content
    /// </summary>
    [Serializable]
    public class PerformanceExplanation
    {
        public Monster Monster;
        public ActivityType ActivityType;
        public MonsterPerformance Performance;
        public List<GeneticFactor> GeneticFactors = new();
        public List<string> EducationalInsights = new();
        public List<string> ImprovementSuggestions = new();
    }

    /// <summary>
    /// Genetic factor analysis
    /// </summary>
    [Serializable]
    public struct GeneticFactor
    {
        public string FactorName;
        public float Influence;
        public string Explanation;
    }

    /// <summary>
    /// Learning activity
    /// </summary>
    [Serializable]
    public class LearningActivity
    {
        public string ActivityId;
        public string Title;
        public ActivityType Type;
        public string Description;
        public float EstimatedDuration;
    }

    /// <summary>
    /// Learning progress summary
    /// </summary>
    [Serializable]
    public struct LearningProgressSummary
    {
        public LearningLevel CurrentLevel;
        public int ConceptsMastered;
        public int TotalConcepts;
        public List<string> RecentAchievements;
        public List<string> NextRecommendedConcepts;
    }

    /// <summary>
    /// Classroom configuration for teacher tools
    /// </summary>
    [Serializable]
    public class ClassroomConfig
    {
        public string ClassName;
        public string GradeLevel;
        public List<string> LearningObjectives = new();
        public int MaxStudents = 30;
        public bool EnableStudentProgress = true;
    }

    /// <summary>
    /// Breeding prediction for experiments
    /// </summary>
    [Serializable]
    public struct BreedingPrediction
    {
        public string TraitName;
        public float MinValue;
        public float MaxValue;
        public float MostLikelyValue;
        public string Explanation;
    }

    /// <summary>
    /// Experiment results analysis
    /// </summary>
    [Serializable]
    public class ExperimentResults
    {
        public bool HypothesisCorrect;
        public List<string> LearningOutcomes = new();
        public List<string> ConceptsReinforced = new();
    }

    /// <summary>
    /// Breeding result for educational triggers
    /// </summary>
    [Serializable]
    public class BreedingResult
    {
        public Monster Offspring;
        public bool IsGeneticallyInteresting;
        public bool HasMutation;
        public bool ShowsInheritancePatterns;
        public List<string> NotableTraits = new();
    }

    /// <summary>
    /// Enums for educational system
    /// </summary>
    public enum LearningLevel { Beginner, Intermediate, Advanced, Expert }
    public enum EducationalCategory { Genetics, Biology, Evolution, Ecology, Chemistry, Physics }
    public enum LearningStyle { Visual, Auditory, Kinesthetic, Interactive, Reading }
    public enum LearningPace { Fast, Moderate, SelfPaced, Slow }
    public enum LearningInteractionType { BreedingTriggered, DirectAccess, AssessmentPassed, ExperimentConducted }
    public enum ExperimentStatus { Planning, InProgress, Completed, Cancelled }

    #endregion
}