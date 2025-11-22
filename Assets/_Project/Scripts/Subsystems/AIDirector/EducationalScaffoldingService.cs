using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Subsystems.AIDirector
{
    /// <summary>
    /// Concrete implementation of educational scaffolding service
    /// Provides adaptive learning support, hints, and educational guidance
    /// </summary>
    public class EducationalScaffoldingService : IEducationalScaffoldingService
    {
        #region Fields

        private readonly AIDirectorSubsystemConfig _config;
        private Dictionary<string, List<EducationalScaffolding>> _playerScaffolding;
        private Dictionary<string, Queue<HintSystem>> _pendingHints;
        private Dictionary<string, ScaffoldingHistory> _scaffoldingHistory;
        private List<ScaffoldingTemplate> _scaffoldingTemplates;
        private Dictionary<string, float> _scaffoldingEffectiveness;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public EducationalScaffoldingService(AIDirectorSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IEducationalScaffoldingService Implementation

        public async Task<bool> InitializeAsync()
        {
            await Task.CompletedTask; // Synchronous initialization, but async for interface compatibility

            try
            {
                _playerScaffolding = new Dictionary<string, List<EducationalScaffolding>>();
                _pendingHints = new Dictionary<string, Queue<HintSystem>>();
                _scaffoldingHistory = new Dictionary<string, ScaffoldingHistory>();
                _scaffoldingTemplates = new List<ScaffoldingTemplate>();
                _scaffoldingEffectiveness = new Dictionary<string, float>();

                // Initialize scaffolding templates
                InitializeScaffoldingTemplates();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[EducationalScaffoldingService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EducationalScaffoldingService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void ProvideSupport(string playerId, string struggleType)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            var scaffolding = GenerateScaffolding(playerId, struggleType);
            if (scaffolding != null)
            {
                // Add to player scaffolding
                if (!_playerScaffolding.ContainsKey(playerId))
                    _playerScaffolding[playerId] = new List<EducationalScaffolding>();

                _playerScaffolding[playerId].Add(scaffolding);

                // Update scaffolding history
                UpdateScaffoldingHistory(playerId, scaffolding);

                if (_config.enableDebugLogging)
                    Debug.Log($"[EducationalScaffoldingService] Provided {scaffolding.scaffoldingType} support for {playerId}: {struggleType}");
            }
        }

        public void ProvideHint(string playerId, string hintType, string hintContent)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            var hint = new HintSystem
            {
                hintId = Guid.NewGuid().ToString(),
                playerId = playerId,
                hintType = GetHintTypeFromString(hintType),
                hintContent = hintContent,
                urgency = CalculateHintUrgency(playerId, hintType),
                isContextual = true,
                availableTime = DateTime.Now,
                expirationTime = TimeSpan.FromMinutes(10)
            };

            // Add to pending hints
            if (!_pendingHints.ContainsKey(playerId))
                _pendingHints[playerId] = new Queue<HintSystem>();

            _pendingHints[playerId].Enqueue(hint);

            // Keep hint queue manageable
            while (_pendingHints[playerId].Count > 5)
            {
                _pendingHints[playerId].Dequeue();
            }

            if (_config.enableDebugLogging)
                Debug.Log($"[EducationalScaffoldingService] Provided {hintType} hint for {playerId}: {hintContent}");
        }

        public EducationalScaffolding GenerateScaffolding(string playerId, string context)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return null;

            var template = FindAppropriateTemplate(context);
            if (template == null)
                return null;

            var scaffolding = new EducationalScaffolding
            {
                scaffoldingId = Guid.NewGuid().ToString(),
                playerId = playerId,
                scaffoldingType = template.scaffoldingType,
                content = ProcessScaffoldingContent(template.contentTemplate, context),
                providedTime = DateTime.Now,
                wasAccepted = false,
                wasEffective = false,
                scaffoldingData = new Dictionary<string, object>
                {
                    ["context"] = context,
                    ["templateId"] = template.templateId,
                    ["adaptationLevel"] = CalculateAdaptationLevel(playerId),
                    ["urgency"] = CalculateScaffoldingUrgency(context)
                }
            };

            return scaffolding;
        }

        public bool ShouldProvideScaffolding(string playerId, PlayerProfile profile, DirectorContext context)
        {
            if (!_isInitialized || profile == null || context == null)
                return false;

            // Check if player needs scaffolding based on multiple factors
            var needsSupport = false;

            // Low engagement indicates need for support
            if (context.engagement < 0.4f)
                needsSupport = true;

            // High frustration indicates need for support
            if (context.frustrationLevel > 0.6f)
                needsSupport = true;

            // Poor progress rate indicates need for support
            if (context.progressRate < 0.3f)
                needsSupport = true;

            // Low confidence indicates need for support
            if (profile.confidenceLevel < 0.4f)
                needsSupport = true;

            // Check if player is in educational context
            if (profile.isEducationalContext)
            {
                // More liberal scaffolding in educational contexts
                if (context.engagement < 0.6f || context.progressRate < 0.5f)
                    needsSupport = true;
            }

            // Check recent scaffolding history to avoid over-scaffolding
            if (needsSupport && HasRecentScaffolding(playerId))
            {
                needsSupport = false;
            }

            if (_config.enableDebugLogging && needsSupport)
                Debug.Log($"[EducationalScaffoldingService] Scaffolding recommended for {playerId}");

            return needsSupport;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets pending hints for a player
        /// </summary>
        public List<HintSystem> GetPendingHints(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return new List<HintSystem>();

            if (_pendingHints.TryGetValue(playerId, out var hints))
            {
                return new List<HintSystem>(hints);
            }

            return new List<HintSystem>();
        }

        /// <summary>
        /// Marks a scaffolding as accepted by the player
        /// </summary>
        public void MarkScaffoldingAccepted(string scaffoldingId, bool wasEffective = false)
        {
            if (!_isInitialized || string.IsNullOrEmpty(scaffoldingId))
                return;

            foreach (var playerScaffoldings in _playerScaffolding.Values)
            {
                var scaffolding = playerScaffoldings.Find(s => s.scaffoldingId == scaffoldingId);
                if (scaffolding != null)
                {
                    scaffolding.wasAccepted = true;
                    scaffolding.wasEffective = wasEffective;

                    // Update effectiveness metrics
                    var scaffoldingType = scaffolding.scaffoldingType.ToString();
                    var currentEffectiveness = _scaffoldingEffectiveness.GetValueOrDefault(scaffoldingType, 0.5f);
                    _scaffoldingEffectiveness[scaffoldingType] = (currentEffectiveness + (wasEffective ? 1f : 0f)) / 2f;

                    if (_config.enableDebugLogging)
                        Debug.Log($"[EducationalScaffoldingService] Scaffolding {scaffoldingId} marked as {(wasEffective ? "effective" : "accepted")}");

                    break;
                }
            }
        }

        /// <summary>
        /// Gets scaffolding history for a player
        /// </summary>
        public ScaffoldingHistory GetScaffoldingHistory(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return new ScaffoldingHistory();

            return _scaffoldingHistory.GetValueOrDefault(playerId, new ScaffoldingHistory());
        }

        #endregion

        #region Private Methods

        private void InitializeScaffoldingTemplates()
        {
            _scaffoldingTemplates.AddRange(new[]
            {
                // Hint templates
                new ScaffoldingTemplate
                {
                    templateId = "procedural_hint",
                    scaffoldingType = ScaffoldingType.Hint,
                    contextTriggers = new List<string> { "breeding_difficulty", "research_confusion", "navigation_lost" },
                    contentTemplate = "Try this approach: {specific_suggestion}. This method often works well for {context_type}.",
                    adaptationLevel = 1
                },

                // Explanation templates
                new ScaffoldingTemplate
                {
                    templateId = "concept_explanation",
                    scaffoldingType = ScaffoldingType.Explanation,
                    contextTriggers = new List<string> { "genetic_confusion", "inheritance_misunderstanding", "trait_complexity" },
                    contentTemplate = "Let me explain {concept}: {detailed_explanation}. This concept is important because {importance}.",
                    adaptationLevel = 2
                },

                // Example templates
                new ScaffoldingTemplate
                {
                    templateId = "worked_example",
                    scaffoldingType = ScaffoldingType.Example,
                    contextTriggers = new List<string> { "breeding_failure", "research_methodology", "data_analysis" },
                    contentTemplate = "Here's an example of how to {task}: {step_by_step_example}. Notice how {key_insight}.",
                    adaptationLevel = 2
                },

                // Guidance templates
                new ScaffoldingTemplate
                {
                    templateId = "strategic_guidance",
                    scaffoldingType = ScaffoldingType.Guidance,
                    contextTriggers = new List<string> { "goal_confusion", "overwhelming_options", "decision_paralysis" },
                    contentTemplate = "A good strategy here is to {strategic_approach}. Focus on {priority_area} first, then {next_step}.",
                    adaptationLevel = 3
                },

                // Support templates
                new ScaffoldingTemplate
                {
                    templateId = "emotional_support",
                    scaffoldingType = ScaffoldingType.Support,
                    contextTriggers = new List<string> { "frustration", "repeated_failure", "low_confidence" },
                    contentTemplate = "Don't worry, {reassurance}. Many researchers face this challenge. {encouraging_perspective}.",
                    adaptationLevel = 1
                },

                // Encouragement templates
                new ScaffoldingTemplate
                {
                    templateId = "motivational_encouragement",
                    scaffoldingType = ScaffoldingType.Encouragement,
                    contextTriggers = new List<string> { "progress_plateau", "engagement_drop", "session_fatigue" },
                    contentTemplate = "You're making good progress! {achievement_recognition}. Keep going - {motivational_message}.",
                    adaptationLevel = 1
                },

                // Clarification templates
                new ScaffoldingTemplate
                {
                    templateId = "concept_clarification",
                    scaffoldingType = ScaffoldingType.Clarification,
                    contextTriggers = new List<string> { "terminology_confusion", "process_uncertainty", "result_interpretation" },
                    contentTemplate = "To clarify: {clear_explanation}. The key difference is {distinguishing_factor}.",
                    adaptationLevel = 2
                }
            });
        }

        private ScaffoldingTemplate FindAppropriateTemplate(string context)
        {
            var candidateTemplates = _scaffoldingTemplates.FindAll(t =>
                t.contextTriggers.Contains(context) || t.contextTriggers.Any(trigger => context.Contains(trigger)));

            if (candidateTemplates.Count == 0)
            {
                // Fallback to generic support template
                candidateTemplates = _scaffoldingTemplates.FindAll(t => t.scaffoldingType == ScaffoldingType.Support);
            }

            if (candidateTemplates.Count == 0)
                return null;

            // Prefer templates with higher effectiveness
            candidateTemplates.Sort((a, b) =>
            {
                var effectivenessA = _scaffoldingEffectiveness.GetValueOrDefault(a.scaffoldingType.ToString(), 0.5f);
                var effectivenessB = _scaffoldingEffectiveness.GetValueOrDefault(b.scaffoldingType.ToString(), 0.5f);
                return effectivenessB.CompareTo(effectivenessA);
            });

            return candidateTemplates[0];
        }

        private string ProcessScaffoldingContent(string template, string context)
        {
            var processed = template;

            // Replace context-specific placeholders
            processed = processed.Replace("{context}", context);
            processed = processed.Replace("{context_type}", GetContextType(context));
            processed = processed.Replace("{specific_suggestion}", GenerateSpecificSuggestion(context));
            processed = processed.Replace("{concept}", GetContextConcept(context));
            processed = processed.Replace("{detailed_explanation}", GenerateDetailedExplanation(context));
            processed = processed.Replace("{importance}", GenerateImportanceStatement(context));
            processed = processed.Replace("{task}", GetTaskFromContext(context));
            processed = processed.Replace("{step_by_step_example}", GenerateStepByStepExample(context));
            processed = processed.Replace("{key_insight}", GenerateKeyInsight(context));
            processed = processed.Replace("{strategic_approach}", GenerateStrategicApproach(context));
            processed = processed.Replace("{priority_area}", GetPriorityArea(context));
            processed = processed.Replace("{next_step}", GetNextStep(context));
            processed = processed.Replace("{reassurance}", GenerateReassurance(context));
            processed = processed.Replace("{encouraging_perspective}", GenerateEncouragingPerspective(context));
            processed = processed.Replace("{achievement_recognition}", GenerateAchievementRecognition(context));
            processed = processed.Replace("{motivational_message}", GenerateMotivationalMessage(context));
            processed = processed.Replace("{clear_explanation}", GenerateClearExplanation(context));
            processed = processed.Replace("{distinguishing_factor}", GenerateDistinguishingFactor(context));

            return processed;
        }

        // Content generation methods
        private string GetContextType(string context) => context switch
        {
            var c when c.Contains("breeding") => "genetic breeding",
            var c when c.Contains("research") => "scientific research",
            var c when c.Contains("navigation") => "system navigation",
            var c when c.Contains("genetic") => "genetic analysis",
            _ => "general research"
        };

        private string GenerateSpecificSuggestion(string context) => context switch
        {
            "breeding_difficulty" => "Select creatures with complementary traits and check their compatibility ratings",
            "research_confusion" => "Break down your research question into smaller, manageable parts",
            "navigation_lost" => "Use the help menu or return to the main dashboard to reorient yourself",
            "genetic_confusion" => "Focus on one trait at a time and observe how it changes across generations",
            _ => "Take a step back and consider alternative approaches to your current challenge"
        };

        private string GetContextConcept(string context) => context switch
        {
            "genetic_confusion" => "genetic inheritance",
            "inheritance_misunderstanding" => "trait inheritance patterns",
            "trait_complexity" => "complex trait interactions",
            _ => "the underlying concept"
        };

        private string GenerateDetailedExplanation(string context) => context switch
        {
            "genetic_confusion" => "Genetic inheritance follows predictable patterns where traits from parent organisms are passed to offspring through genes. Each gene contributes to specific characteristics.",
            "inheritance_misunderstanding" => "Traits are inherited through genes from both parents. Some traits are dominant (more likely to appear) while others are recessive (only appear when inherited from both parents).",
            "trait_complexity" => "Complex traits often result from multiple genes working together. These interactions can create surprising combinations and new characteristics.",
            _ => "This concept involves multiple interconnected factors that work together to produce observable outcomes."
        };

        private string GenerateImportanceStatement(string context) => context switch
        {
            "genetic_confusion" => "it forms the foundation of all breeding and evolutionary processes",
            "inheritance_misunderstanding" => "it helps predict and plan breeding outcomes",
            "trait_complexity" => "it explains why genetic diversity is crucial for healthy populations",
            _ => "it helps you understand how systems work together"
        };

        private string GetTaskFromContext(string context) => context switch
        {
            "breeding_failure" => "successfully breed creatures",
            "research_methodology" => "conduct systematic research",
            "data_analysis" => "analyze research data",
            _ => "approach this challenge"
        };

        private string GenerateStepByStepExample(string context) => context switch
        {
            "breeding_failure" => "1) Select parent creatures with desired traits, 2) Check compatibility in the breeding interface, 3) Monitor offspring for trait expression, 4) Record results for future reference",
            "research_methodology" => "1) Define your research question clearly, 2) Gather relevant data systematically, 3) Analyze patterns in your findings, 4) Draw conclusions based on evidence",
            "data_analysis" => "1) Organize your data into categories, 2) Look for patterns and trends, 3) Compare results across different conditions, 4) Interpret what the patterns mean",
            _ => "1) Break the task into smaller parts, 2) Address each part systematically, 3) Check your progress regularly, 4) Adjust your approach as needed"
        };

        private string GenerateKeyInsight(string context) => context switch
        {
            "breeding_failure" => "genetic compatibility is just as important as individual traits",
            "research_methodology" => "systematic approaches yield more reliable results than random experimentation",
            "data_analysis" => "patterns often become clear when you look at data from multiple perspectives",
            _ => "patience and systematic observation lead to better understanding"
        };

        private string GenerateStrategicApproach(string context) => context switch
        {
            "goal_confusion" => "start with smaller, achievable goals and build toward larger objectives",
            "overwhelming_options" => "focus on one area at a time and gradually expand your scope",
            "decision_paralysis" => "gather just enough information to make an informed choice, then act",
            _ => "take a systematic, step-by-step approach"
        };

        private string GetPriorityArea(string context) => context switch
        {
            "goal_confusion" => "clarifying your immediate objectives",
            "overwhelming_options" => "the area that interests you most",
            "decision_paralysis" => "gathering essential information",
            _ => "understanding the basics"
        };

        private string GetNextStep(string context) => context switch
        {
            "goal_confusion" => "create a plan for achieving your main objective",
            "overwhelming_options" => "explore related areas that complement your primary focus",
            "decision_paralysis" => "make a decision and adjust course if needed",
            _ => "build on what you've learned"
        };

        private string GenerateReassurance(string context) => context switch
        {
            "frustration" => "these feelings are completely normal when learning complex systems",
            "repeated_failure" => "each attempt teaches you something valuable, even if it doesn't succeed immediately",
            "low_confidence" => "everyone starts somewhere, and your persistence shows real dedication",
            _ => "this is a normal part of the learning process"
        };

        private string GenerateEncouragingPerspective(string context) => context switch
        {
            "frustration" => "Remember that breakthrough discoveries often come after periods of challenge and persistence",
            "repeated_failure" => "Many successful researchers had to try multiple approaches before finding what worked",
            "low_confidence" => "Your questions and curiosity are signs of an engaged, thoughtful researcher",
            _ => "Every expert was once a beginner who kept trying"
        };

        private string GenerateAchievementRecognition(string context) => context switch
        {
            "progress_plateau" => "You've already accomplished more than when you started",
            "engagement_drop" => "You've shown excellent persistence in tackling challenging problems",
            "session_fatigue" => "You've put in substantial effort and made meaningful progress",
            _ => "Your dedication to learning is evident in your work"
        };

        private string GenerateMotivationalMessage(string context) => context switch
        {
            "progress_plateau" => "plateaus often come before major breakthroughs",
            "engagement_drop" => "taking breaks can help you return with fresh perspective",
            "session_fatigue" => "rest is an important part of the learning process",
            _ => "your continued effort will lead to success"
        };

        private string GenerateClearExplanation(string context) => context switch
        {
            "terminology_confusion" => "terminology in genetics can be complex, but each term has a specific, useful meaning",
            "process_uncertainty" => "this process follows a logical sequence that becomes clearer with practice",
            "result_interpretation" => "results often make more sense when viewed in the context of your research goals",
            _ => "this concept has specific characteristics that distinguish it from similar ideas"
        };

        private string GenerateDistinguishingFactor(string context) => context switch
        {
            "terminology_confusion" => "the specific context in which each term is used",
            "process_uncertainty" => "the timing and sequence of steps",
            "result_interpretation" => "the relationship between your actions and the observed outcomes",
            _ => "the specific conditions under which different rules apply"
        };

        private HintType GetHintTypeFromString(string hintType) => hintType.ToLower() switch
        {
            "procedural" => HintType.Procedural,
            "conceptual" => HintType.Conceptual,
            "strategic" => HintType.Strategic,
            "motivational" => HintType.Motivational,
            "navigational" => HintType.Navigational,
            "social" => HintType.Social,
            _ => HintType.Procedural
        };

        private float CalculateHintUrgency(string playerId, string hintType)
        {
            // Calculate urgency based on player state and hint type
            var baseUrgency = 0.5f;

            if (HasRecentScaffolding(playerId))
                baseUrgency -= 0.2f; // Lower urgency if recent scaffolding provided

            if (hintType.Contains("critical") || hintType.Contains("error"))
                baseUrgency += 0.3f; // Higher urgency for critical hints

            return math.clamp(baseUrgency, 0f, 1f);
        }

        private int CalculateAdaptationLevel(string playerId)
        {
            // Calculate adaptation level based on player's scaffolding history
            var history = _scaffoldingHistory.GetValueOrDefault(playerId, new ScaffoldingHistory());

            if (history.totalScaffoldingProvided < 3)
                return 1; // Basic level for new players
            else if (history.averageEffectiveness > 0.7f)
                return 3; // Advanced level for responsive players
            else
                return 2; // Intermediate level
        }

        private float CalculateScaffoldingUrgency(string context)
        {
            return context switch
            {
                var c when c.Contains("critical") => 0.9f,
                var c when c.Contains("error") => 0.8f,
                var c when c.Contains("frustration") => 0.7f,
                var c when c.Contains("confusion") => 0.6f,
                var c when c.Contains("difficulty") => 0.5f,
                _ => 0.4f
            };
        }

        private bool HasRecentScaffolding(string playerId)
        {
            if (!_playerScaffolding.TryGetValue(playerId, out var scaffoldings))
                return false;

            var recentThreshold = DateTime.Now.AddMinutes(-5); // 5 minutes ago
            return scaffoldings.Any(s => s.providedTime > recentThreshold);
        }

        private void UpdateScaffoldingHistory(string playerId, EducationalScaffolding scaffolding)
        {
            if (!_scaffoldingHistory.ContainsKey(playerId))
            {
                _scaffoldingHistory[playerId] = new ScaffoldingHistory
                {
                    playerId = playerId,
                    totalScaffoldingProvided = 0,
                    acceptedScaffolding = 0,
                    effectiveScaffolding = 0,
                    averageEffectiveness = 0f,
                    lastScaffoldingTime = DateTime.Now,
                    scaffoldingsByType = new Dictionary<ScaffoldingType, int>()
                };
            }

            var history = _scaffoldingHistory[playerId];
            history.totalScaffoldingProvided++;
            history.lastScaffoldingTime = scaffolding.providedTime;

            if (!history.scaffoldingsByType.ContainsKey(scaffolding.scaffoldingType))
                history.scaffoldingsByType[scaffolding.scaffoldingType] = 0;

            history.scaffoldingsByType[scaffolding.scaffoldingType]++;

            // Update effectiveness when scaffolding is marked as accepted/effective
            if (scaffolding.wasAccepted)
            {
                history.acceptedScaffolding++;
                if (scaffolding.wasEffective)
                {
                    history.effectiveScaffolding++;
                }
                history.averageEffectiveness = (float)history.effectiveScaffolding / history.acceptedScaffolding;
            }
        }

        #endregion

        #region Helper Classes

        private class ScaffoldingTemplate
        {
            public string templateId;
            public ScaffoldingType scaffoldingType;
            public List<string> contextTriggers = new();
            public string contentTemplate;
            public int adaptationLevel;
        }

        public class ScaffoldingHistory
        {
            public string playerId;
            public int totalScaffoldingProvided;
            public int acceptedScaffolding;
            public int effectiveScaffolding;
            public float averageEffectiveness;
            public DateTime lastScaffoldingTime;
            public Dictionary<ScaffoldingType, int> scaffoldingsByType = new();
        }

        #endregion
    }
}