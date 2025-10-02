using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Random = UnityEngine.Random;

namespace Laboratory.Chimera.Detective
{
    /// <summary>
    /// Genetic Detective and Mystery System for Project Chimera.
    /// Creates engaging mystery scenarios where players solve genetic puzzles,
    /// track down bloodlines, investigate breeding crimes, and uncover genetic secrets.
    ///
    /// Features:
    /// - Mystery Creatures: Wild creatures with unknown genetic backgrounds to analyze
    /// - Breeding Crime Investigation: Solve cases of illegal genetic manipulation
    /// - Bloodline Detective Work: Track down the origins of exceptional creatures
    /// - Genetic Forensics: Use DNA analysis to solve mysteries
    /// - Cold Cases: Investigate historical genetic mysteries
    /// - Evidence Collection: Gather and analyze genetic clues
    /// </summary>
    public class GeneticDetectiveSystem : MonoBehaviour
    {
        [Header("System Configuration")]
        [SerializeField] private GeneticDetectiveConfig config;
        [SerializeField] private bool enableMysteryGeneration = true;
        [SerializeField] private bool enableBreedingCrimes = true;
        [SerializeField] private bool enableBloodlineInvestigations = true;
        [SerializeField] private bool enableColdCases = true;

        [Header("Mystery Generation")]
        [SerializeField] private float mysteryGenerationRate = 0.02f; // 2% chance per day
        [SerializeField] private int maxActiveMysteries = 5;
        [SerializeField] private float caseTimeoutDays = 30f;

        [Header("Investigation Tools")]
        [SerializeField] private float evidenceDecayRate = 0.05f; // Evidence quality degrades over time
        [SerializeField] private float forensicAccuracy = 0.85f;

        // Active mysteries and cases
        private Dictionary<string, GeneticMystery> activeMysteries = new Dictionary<string, GeneticMystery>();
        private Dictionary<string, DetectiveCase> activeInvestigations = new Dictionary<string, DetectiveCase>();
        private List<ColdCase> coldCases = new List<ColdCase>();

        // Evidence and clue tracking
        private Dictionary<string, List<GeneticEvidence>> caseEvidence = new Dictionary<string, List<GeneticEvidence>>();
        private Dictionary<string, BloodlineTrace> bloodlineDatabase = new Dictionary<string, BloodlineTrace>();

        // Player detective profiles
        private Dictionary<string, DetectiveProfile> detectiveProfiles = new Dictionary<string, DetectiveProfile>();

        // Events
        public static event Action<GeneticMystery> OnMysteryGenerated;
        public static event Action<DetectiveCase> OnCaseOpened;
        public static event Action<DetectiveCase> OnCaseSolved;
        public static event Action<GeneticEvidence> OnEvidenceDiscovered;
        public static event Action<string, float> OnDetectiveReputationChanged;

        void Start()
        {
            InitializeDetectiveSystem();
            InvokeRepeating(nameof(GenerateMysteries), 300f, 86400f); // Check daily for new mysteries
            InvokeRepeating(nameof(UpdateActiveCases), 60f, 3600f); // Update cases hourly
            InvokeRepeating(nameof(ProcessColdCases), 3600f, 86400f); // Process cold cases daily
        }

        #region Initialization

        private void InitializeDetectiveSystem()
        {
            LoadDetectiveDatabase();
            SeedInitialMysteries();
            CreateSomeColdCases();

            UnityEngine.Debug.Log("Genetic Detective System initialized - Mysteries await investigation!");
        }

        private void SeedInitialMysteries()
        {
            // Create a few starter mysteries for immediate gameplay
            for (int i = 0; i < 2; i++)
            {
                GenerateRandomMystery();
            }
        }

        private void CreateSomeColdCases()
        {
            // Create historical cold cases for players to investigate
            coldCases.AddRange(new[]
            {
                new ColdCase
                {
                    id = Guid.NewGuid().ToString(),
                    title = "The Vanished Lineage",
                    description = "A legendary bloodline disappeared 50 generations ago. What happened to the Aurora Wing genetic line?",
                    originalDate = Time.time - (86400f * 365f * 2f), // 2 years ago
                    mysteryType = MysteryType.BloodlineInvestigation,
                    difficulty = CaseDifficulty.Legendary,
                    reward = 5000,
                    isActive = true,
                    clues = new List<string>
                    {
                        "Last seen in the Northern Forests",
                        "Known for distinctive bioluminescent wing patterns",
                        "Associated with the Eclipse Moon breeding event"
                    }
                },
                new ColdCase
                {
                    id = Guid.NewGuid().ToString(),
                    title = "The Genetic Sabotage",
                    description = "Someone deliberately contaminated a breeding facility with mutagens. Can you find the culprit?",
                    originalDate = Time.time - (86400f * 180f), // 6 months ago
                    mysteryType = MysteryType.BreedingCrime,
                    difficulty = CaseDifficulty.Expert,
                    reward = 3000,
                    isActive = true,
                    clues = new List<string>
                    {
                        "Mutations appeared suddenly in multiple species",
                        "Security footage shows a hooded figure",
                        "Chemical residue found in water supply"
                    }
                }
            });
        }

        #endregion

        #region Mystery Generation

        private void GenerateMysteries()
        {
            if (!enableMysteryGeneration || activeMysteries.Count >= maxActiveMysteries) return;

            if (Random.value < mysteryGenerationRate)
            {
                GenerateRandomMystery();
            }
        }

        private void GenerateRandomMystery()
        {
            var mysteryTypes = Enum.GetValues(typeof(MysteryType)).Cast<MysteryType>().ToArray();
            var mysteryType = mysteryTypes[Random.Range(0, mysteryTypes.Length)];

            var mystery = mysteryType switch
            {
                MysteryType.MysteryCreature => GenerateMysteryCreature(),
                MysteryType.BreedingCrime => GenerateBreedingCrime(),
                MysteryType.BloodlineInvestigation => GenerateBloodlineInvestigation(),
                MysteryType.GeneticAnomaly => GenerateGeneticAnomaly(),
                MysteryType.IllegalExperimentation => GenerateIllegalExperimentation(),
                _ => GenerateMysteryCreature()
            };

            if (mystery.HasValue)
            {
                var mysteryValue = mystery.Value;
                activeMysteries[mysteryValue.id] = mysteryValue;
                caseEvidence[mysteryValue.id] = new List<GeneticEvidence>();
                OnMysteryGenerated?.Invoke(mysteryValue);

                UnityEngine.Debug.Log($"New mystery generated: {mysteryValue.title} ({mysteryValue.type})");
            }
        }

        private GeneticMystery GenerateMysteryCreature()
        {
            var mystery = new GeneticMystery
            {
                id = Guid.NewGuid().ToString(),
                type = MysteryType.MysteryCreature,
                title = GenerateMysteryTitle(MysteryType.MysteryCreature),
                description = "A creature with unusual genetic markers has been discovered. Can you determine its origins?",
                difficulty = (CaseDifficulty)Random.Range(1, 4),
                discoveryLocation = GenerateRandomLocation(),
                discoveryTime = Time.time,
                isActive = true,
                timeLimit = Time.time + (caseTimeoutDays * 86400f),
                mysteryCreature = GenerateUnknownCreature()
            };

            mystery.reward = CalculateMysteryReward(mystery);
            mystery.expectedSolutionSteps = Random.Range(3, 8);

            return mystery;
        }

        private GeneticMystery? GenerateBreedingCrime()
        {
            if (!enableBreedingCrimes) return null;

            var crimeTypes = new[]
            {
                "Illegal genetic modification detected in local breeding facility",
                "Suspected genetic theft from private research laboratory",
                "Unauthorized hybridization experiments reported",
                "Black market genetic material trading investigation"
            };

            var mystery = new GeneticMystery
            {
                id = Guid.NewGuid().ToString(),
                type = MysteryType.BreedingCrime,
                title = "Breeding Crime Investigation",
                description = crimeTypes[Random.Range(0, crimeTypes.Length)],
                difficulty = (CaseDifficulty)Random.Range(2, 5),
                discoveryLocation = GenerateRandomLocation(),
                discoveryTime = Time.time,
                isActive = true,
                timeLimit = Time.time + (caseTimeoutDays * 86400f),
                suspectPool = GenerateSuspectPool()
            };

            mystery.reward = CalculateMysteryReward(mystery);
            mystery.expectedSolutionSteps = Random.Range(5, 12);

            return mystery;
        }

        private GeneticMystery? GenerateBloodlineInvestigation()
        {
            if (!enableBloodlineInvestigations) return null;

            var investigationTypes = new[]
            {
                "Track the lineage of this exceptional creature",
                "Determine the breeding history of mysterious bloodline",
                "Investigate claims of legendary ancestor",
                "Verify pedigree authenticity for rare specimen"
            };

            var mystery = new GeneticMystery
            {
                id = Guid.NewGuid().ToString(),
                type = MysteryType.BloodlineInvestigation,
                title = "Bloodline Investigation",
                description = investigationTypes[Random.Range(0, investigationTypes.Length)],
                difficulty = (CaseDifficulty)Random.Range(2, 4),
                discoveryLocation = GenerateRandomLocation(),
                discoveryTime = Time.time,
                isActive = true,
                timeLimit = Time.time + (caseTimeoutDays * 86400f),
                targetBloodline = GenerateMysterousBloodline()
            };

            mystery.reward = CalculateMysteryReward(mystery);
            mystery.expectedSolutionSteps = Random.Range(4, 10);

            return mystery;
        }

        private GeneticMystery GenerateGeneticAnomaly()
        {
            var anomalyTypes = new[]
            {
                "Impossible genetic combination discovered in wild",
                "Creature displaying traits from extinct lineage",
                "Genetic markers suggest temporal anomaly",
                "DNA analysis reveals contradictory results"
            };

            var mystery = new GeneticMystery
            {
                id = Guid.NewGuid().ToString(),
                type = MysteryType.GeneticAnomaly,
                title = "Genetic Anomaly Investigation",
                description = anomalyTypes[Random.Range(0, anomalyTypes.Length)],
                difficulty = (CaseDifficulty)Random.Range(3, 5),
                discoveryLocation = GenerateRandomLocation(),
                discoveryTime = Time.time,
                isActive = true,
                timeLimit = Time.time + (caseTimeoutDays * 86400f),
                anomalyData = GenerateAnomalyData()
            };

            mystery.reward = CalculateMysteryReward(mystery);
            mystery.expectedSolutionSteps = Random.Range(6, 15);

            return mystery;
        }

        private GeneticMystery GenerateIllegalExperimentation()
        {
            var experimentTypes = new[]
            {
                "Evidence of unauthorized genetic experiments",
                "Suspected bio-weapon development program",
                "Illegal enhancement drug testing on creatures",
                "Underground genetic modification ring"
            };

            var mystery = new GeneticMystery
            {
                id = Guid.NewGuid().ToString(),
                type = MysteryType.IllegalExperimentation,
                title = "Illegal Experimentation Case",
                description = experimentTypes[Random.Range(0, experimentTypes.Length)],
                difficulty = CaseDifficulty.Legendary,
                discoveryLocation = GenerateRandomLocation(),
                discoveryTime = Time.time,
                isActive = true,
                timeLimit = Time.time + (caseTimeoutDays * 86400f),
                evidenceTrail = GenerateExperimentEvidence()
            };

            mystery.reward = CalculateMysteryReward(mystery);
            mystery.expectedSolutionSteps = Random.Range(8, 20);

            return mystery;
        }

        #endregion

        #region Investigation Mechanics

        /// <summary>
        /// Starts a new investigation into a mystery
        /// </summary>
        public DetectiveCase? StartInvestigation(string mysteryId, string investigatorId)
        {
            if (!activeMysteries.ContainsKey(mysteryId)) return null;

            var mystery = activeMysteries[mysteryId];
            var detectiveCase = new DetectiveCase
            {
                id = Guid.NewGuid().ToString(),
                mysteryId = mysteryId,
                investigatorId = investigatorId,
                startTime = Time.time,
                status = CaseStatus.Active,
                progress = 0f,
                discoveredClues = new List<string>(),
                collectedEvidence = new List<string>(),
                hypotheses = new List<CaseHypothesis>(),
                interviews = new List<Interview>()
            };

            activeInvestigations[detectiveCase.id] = detectiveCase;
            EnsureDetectiveProfile(investigatorId);

            OnCaseOpened?.Invoke(detectiveCase);
            UnityEngine.Debug.Log($"Investigation started: {mystery.title} by {GetDetectiveName(investigatorId)}");

            return detectiveCase;
        }

        /// <summary>
        /// Analyzes a genetic sample for evidence
        /// </summary>
        public GeneticEvidence? AnalyzeGeneticSample(string caseId, GeneticProfile sample, Vector3 collectionLocation)
        {
            if (!activeInvestigations.ContainsKey(caseId)) return null;

            var evidence = new GeneticEvidence
            {
                id = Guid.NewGuid().ToString(),
                caseId = caseId,
                type = EvidenceType.GeneticSample,
                geneticProfile = sample,
                collectionLocation = collectionLocation,
                collectionTime = Time.time,
                quality = CalculateEvidenceQuality(sample),
                reliability = forensicAccuracy,
                analysisResults = PerformGeneticAnalysis(sample)
            };

            // Add to case evidence
            if (!caseEvidence.ContainsKey(caseId))
                caseEvidence[caseId] = new List<GeneticEvidence>();

            caseEvidence[caseId].Add(evidence);

            // Update case progress
            var detectiveCase = activeInvestigations[caseId];
            UpdateCaseProgress(detectiveCase, evidence);

            OnEvidenceDiscovered?.Invoke(evidence);
            UnityEngine.Debug.Log($"Genetic evidence analyzed for case {caseId}: Quality {evidence.quality:F2}");

            return evidence;
        }

        /// <summary>
        /// Investigates a specific location for clues
        /// </summary>
        public List<LocationClue> InvestigateLocation(string caseId, Vector3 location, float searchRadius)
        {
            if (!activeInvestigations.ContainsKey(caseId)) return new List<LocationClue>();

            var clues = new List<LocationClue>();
            var detectiveCase = activeInvestigations[caseId];
            var mystery = activeMysteries[detectiveCase.mysteryId];

            // Generate location-specific clues based on mystery type
            var clueCount = Random.Range(1, 4);
            for (int i = 0; i < clueCount; i++)
            {
                var clue = GenerateLocationClue(mystery, location, searchRadius);
                if (clue.HasValue)
                {
                    clues.Add(clue.Value);
                    detectiveCase.discoveredClues.Add(clue.Value.description);
                }
            }

            // Update case progress
            UpdateCaseProgressFromClues(detectiveCase, clues);

            UnityEngine.Debug.Log($"Location investigation found {clues.Count} clues at {location}");
            return clues;
        }

        /// <summary>
        /// Conducts an interview with a suspect or witness
        /// </summary>
        public Interview? ConductInterview(string caseId, string subjectId, List<string> questions)
        {
            if (!activeInvestigations.ContainsKey(caseId)) return null;

            var detectiveCase = activeInvestigations[caseId];
            var mystery = activeMysteries[detectiveCase.mysteryId];

            var interview = new Interview
            {
                id = Guid.NewGuid().ToString(),
                caseId = caseId,
                subjectId = subjectId,
                conductedTime = Time.time,
                questions = questions,
                responses = GenerateInterviewResponses(mystery, subjectId, questions),
                truthfulness = CalculateSubjectTruthfulness(mystery, subjectId),
                newInformation = ExtractNewInformation(mystery, subjectId, questions)
            };

            detectiveCase.interviews.Add(interview);

            // Update case progress based on interview quality
            UpdateCaseProgressFromInterview(detectiveCase, interview);

            UnityEngine.Debug.Log($"Interview conducted with {subjectId} for case {caseId}");
            return interview;
        }

        /// <summary>
        /// Submits a hypothesis about the case solution
        /// </summary>
        public HypothesisResult? SubmitHypothesis(string caseId, string hypothesis, List<string> supportingEvidence)
        {
            if (!activeInvestigations.ContainsKey(caseId)) return null;

            var detectiveCase = activeInvestigations[caseId];
            var mystery = activeMysteries[detectiveCase.mysteryId];

            var caseHypothesis = new CaseHypothesis
            {
                id = Guid.NewGuid().ToString(),
                hypothesis = hypothesis,
                supportingEvidence = supportingEvidence,
                submissionTime = Time.time,
                confidence = CalculateHypothesisConfidence(mystery, hypothesis, supportingEvidence),
                accuracy = CalculateHypothesisAccuracy(mystery, hypothesis, supportingEvidence)
            };

            detectiveCase.hypotheses.Add(caseHypothesis);

            var result = new HypothesisResult
            {
                hypothesis = caseHypothesis,
                feedback = GenerateHypothesisFeedback(caseHypothesis),
                isSolution = caseHypothesis.accuracy > 0.85f,
                partialCredit = Mathf.Clamp01(caseHypothesis.accuracy)
            };

            if (result.isSolution)
            {
                SolveCase(detectiveCase, caseHypothesis);
            }

            UnityEngine.Debug.Log($"Hypothesis submitted for case {caseId}: Accuracy {caseHypothesis.accuracy:F2}");
            return result;
        }

        #endregion

        #region Evidence Analysis

        private float CalculateEvidenceQuality(GeneticProfile sample)
        {
            if (sample?.Genes == null) return 0f;

            float quality = 0.5f;

            // More genes = better evidence
            quality += sample.Genes.Count() * 0.02f;

            // Mutations are interesting for forensics
            quality += sample.Mutations.Count() * 0.1f;

            // Generation affects reliability
            quality += Mathf.Min(sample.Generation * 0.05f, 0.3f);

            // Genetic purity affects analysis accuracy
            quality += sample.GetGeneticPurity() * 0.2f;

            return Mathf.Clamp01(quality);
        }

        private Dictionary<string, object> PerformGeneticAnalysis(GeneticProfile sample)
        {
            var results = new Dictionary<string, object>();

            // Basic genetic information
            results["generation"] = sample.Generation;
            results["genetic_purity"] = sample.GetGeneticPurity();
            results["mutation_count"] = sample.Mutations.Count();
            results["trait_count"] = sample.Genes.Count();

            // Dominant traits
            var dominantTraits = sample.Genes
                .Where(g => g.isActive && g.value.HasValue && g.value.Value > 0.7f)
                .Select(g => g.traitName)
                .ToArray();
            results["dominant_traits"] = dominantTraits;

            // Lineage estimation
            results["estimated_lineage_age"] = EstimateLineageAge(sample);

            // Breeding compatibility indicators
            results["breeding_potential"] = CalculateBreedingPotential(sample);

            // Forensic markers
            results["unique_markers"] = ExtractUniqueMarkers(sample);

            return results;
        }

        private int EstimateLineageAge(GeneticProfile sample)
        {
            // Estimate how old this genetic line is based on complexity and mutations
            float complexity = sample.Genes.Count() + sample.Mutations.Count() * 2;
            return Mathf.RoundToInt(complexity * 0.5f) + Random.Range(-2, 3);
        }

        private float CalculateBreedingPotential(GeneticProfile sample)
        {
            float potential = sample.GetGeneticPurity();

            // High-value traits increase potential
            var valuableTraits = sample.Genes.Count(g => g.value.HasValue && g.value.Value > 0.8f);
            potential += valuableTraits * 0.1f;

            return Mathf.Clamp01(potential);
        }

        private List<string> ExtractUniqueMarkers(GeneticProfile sample)
        {
            var markers = new List<string>();

            // Create unique identifiers based on genetic signature
            foreach (var gene in sample.Genes.Where(g => g.isActive && g.value.HasValue))
            {
                if (gene.value.Value > 0.9f || gene.isMutation)
                {
                    markers.Add($"{gene.traitName}:{gene.value.Value:F3}");
                }
            }

            return markers;
        }

        #endregion

        #region Case Management

        private void UpdateCaseProgress(DetectiveCase detectiveCase, GeneticEvidence evidence)
        {
            var mystery = activeMysteries[detectiveCase.mysteryId];

            // Progress based on evidence quality and relevance
            float progressGain = evidence.quality * 0.1f;

            // Bonus for high-quality evidence
            if (evidence.quality > 0.8f)
                progressGain *= 1.5f;

            detectiveCase.progress = Mathf.Min(1f, detectiveCase.progress + progressGain);

            // Check if case is ready for solution
            if (detectiveCase.progress > 0.7f && detectiveCase.discoveredClues.Count >= mystery.expectedSolutionSteps * 0.6f)
            {
                detectiveCase.status = CaseStatus.ReadyForSolution;
            }
        }

        private void UpdateCaseProgressFromClues(DetectiveCase detectiveCase, List<LocationClue> clues)
        {
            var mystery = activeMysteries[detectiveCase.mysteryId];

            foreach (var clue in clues)
            {
                float progressGain = clue.importance * 0.05f;
                detectiveCase.progress = Mathf.Min(1f, detectiveCase.progress + progressGain);
            }
        }

        private void UpdateCaseProgressFromInterview(DetectiveCase detectiveCase, Interview interview)
        {
            float progressGain = interview.newInformation.Count * 0.03f;
            progressGain *= interview.truthfulness; // Truthful interviews provide more progress

            detectiveCase.progress = Mathf.Min(1f, detectiveCase.progress + progressGain);
        }

        private float CalculateHypothesisAccuracy(GeneticMystery mystery, string hypothesis, List<string> evidence)
        {
            // Simplified accuracy calculation based on evidence quality and hypothesis relevance
            float baseAccuracy = 0.3f;

            // Evidence quality affects accuracy
            var evidenceList = caseEvidence.ContainsKey(mystery.id) ? caseEvidence[mystery.id] : new List<GeneticEvidence>();
            if (evidenceList.Count > 0)
            {
                baseAccuracy += evidenceList.Average(e => e.quality) * 0.4f;
            }

            // Hypothesis length and detail affects accuracy
            baseAccuracy += Mathf.Min(hypothesis.Length / 500f, 0.2f);

            // Supporting evidence count
            baseAccuracy += Mathf.Min(evidence.Count * 0.05f, 0.3f);

            return Mathf.Clamp01(baseAccuracy + Random.Range(-0.1f, 0.1f));
        }

        private float CalculateHypothesisConfidence(GeneticMystery mystery, string hypothesis, List<string> evidence)
        {
            // Confidence based on available evidence and case progress
            var investigation = activeInvestigations.Values.FirstOrDefault(inv => inv.mysteryId == mystery.id);
            if (string.IsNullOrEmpty(investigation.id)) return 0.5f;

            float confidence = investigation.progress * 0.6f;
            confidence += Mathf.Min(evidence.Count * 0.1f, 0.4f);

            return Mathf.Clamp01(confidence);
        }

        private void SolveCase(DetectiveCase detectiveCase, CaseHypothesis solution)
        {
            detectiveCase.status = CaseStatus.Solved;
            detectiveCase.solutionTime = Time.time;
            detectiveCase.finalSolution = solution;

            var mystery = activeMysteries[detectiveCase.mysteryId];

            // Award reputation and rewards
            var detective = detectiveProfiles[detectiveCase.investigatorId];
            var reputationGain = CalculateSolutionReputation(mystery, detectiveCase);
            AwardDetectiveReputation(detectiveCase.investigatorId, reputationGain);

            // Award mystery reward
            detective.totalRewards += mystery.reward;
            detective.solvedCases++;

            if (mystery.difficulty == CaseDifficulty.Legendary)
                detective.legendaryLevelCases++;

            OnCaseSolved?.Invoke(detectiveCase);

            // Remove from active mysteries
            activeMysteries.Remove(detectiveCase.mysteryId);

            UnityEngine.Debug.Log($"Case solved: {mystery.title} by {GetDetectiveName(detectiveCase.investigatorId)}");
        }

        private float CalculateSolutionReputation(GeneticMystery mystery, DetectiveCase detectiveCase)
        {
            float baseReputation = 50f;

            // Difficulty multiplier
            baseReputation *= mystery.difficulty switch
            {
                CaseDifficulty.Novice => 0.5f,
                CaseDifficulty.Intermediate => 1f,
                CaseDifficulty.Expert => 1.5f,
                CaseDifficulty.Master => 2f,
                CaseDifficulty.Legendary => 3f,
                _ => 1f
            };

            // Speed bonus
            var solveTime = detectiveCase.solutionTime - detectiveCase.startTime;
            var timeLimit = mystery.timeLimit - mystery.discoveryTime;
            if (solveTime < timeLimit * 0.5f)
                baseReputation *= 1.3f; // Solved quickly

            // Evidence quality bonus
            var evidenceList = caseEvidence.ContainsKey(mystery.id) ? caseEvidence[mystery.id] : new List<GeneticEvidence>();
            if (evidenceList.Count > 0)
            {
                var avgQuality = evidenceList.Average(e => e.quality);
                baseReputation *= (0.7f + avgQuality * 0.6f);
            }

            return baseReputation;
        }

        #endregion

        #region Helper Methods

        private Vector3 GenerateRandomLocation()
        {
            return new Vector3(
                Random.Range(-1000f, 1000f),
                Random.Range(0f, 100f),
                Random.Range(-1000f, 1000f)
            );
        }

        private string GenerateMysteryTitle(MysteryType type)
        {
            return type switch
            {
                MysteryType.MysteryCreature => "Unknown Creature Analysis",
                MysteryType.BreedingCrime => "Breeding Facility Investigation",
                MysteryType.BloodlineInvestigation => "Bloodline Verification",
                MysteryType.GeneticAnomaly => "Genetic Anomaly Study",
                MysteryType.IllegalExperimentation => "Illegal Research Investigation",
                _ => "Genetic Mystery"
            };
        }

        private GeneticProfile GenerateUnknownCreature()
        {
            // Create a creature with mysterious genetic markers
            var unknownGenes = new List<Gene>();

            // Add some normal traits
            var normalTraits = new[] { "Strength", "Agility", "Intelligence", "Vitality" };
            foreach (var trait in normalTraits)
            {
                unknownGenes.Add(new Gene
                {
                    traitName = trait,
                    traitType = TraitType.Physical,
                    value = Random.Range(0.3f, 0.9f),
                    dominance = Random.Range(0.4f, 0.8f),
                    isActive = true,
                    expression = GeneExpression.Normal
                });
            }

            // Add some mysterious traits
            var mysteriousTraits = new[] { "Unknown Marker A", "Anomalous Signature", "Temporal Echo" };
            foreach (var trait in mysteriousTraits)
            {
                if (Random.value < 0.6f) // 60% chance
                {
                    unknownGenes.Add(new Gene
                    {
                        traitName = trait,
                        traitType = TraitType.Physical,
                        value = Random.Range(0.5f, 1f),
                        dominance = Random.Range(0.2f, 0.9f),
                        isActive = true,
                        expression = GeneExpression.Enhanced,
                        isMutation = true
                    });
                }
            }

            return new GeneticProfile(unknownGenes.ToArray(), Random.Range(3, 15));
        }

        private List<string> GenerateSuspectPool()
        {
            var suspects = new List<string>();
            var suspectNames = new[]
            {
                "Dr. Suspicious", "Unknown Trader", "Rogue Scientist", "Black Market Dealer",
                "Corrupt Official", "Mad Researcher", "Bio Terrorist", "Corporate Spy"
            };

            var suspectCount = Random.Range(3, 6);
            for (int i = 0; i < suspectCount; i++)
            {
                suspects.Add(suspectNames[Random.Range(0, suspectNames.Length)] + $" #{Random.Range(100, 999)}");
            }

            return suspects;
        }

        private BloodlineTrace GenerateMysterousBloodline()
        {
            return new BloodlineTrace
            {
                lineageId = Guid.NewGuid().ToString(),
                originalFounder = $"Legendary_{Random.Range(1000, 9999)}",
                generationsBack = Random.Range(5, 50),
                knownTraits = new List<string> { "Ancient Wisdom", "Primal Power", "Mystic Resonance" },
                lastKnownLocation = GenerateRandomLocation(),
                isLegendary = true,
                mysteryLevel = Random.Range(0.6f, 1f)
            };
        }

        private GeneticAnomalyData GenerateAnomalyData()
        {
            return new GeneticAnomalyData
            {
                anomalyType = "Temporal Genetic Echo",
                severity = Random.Range(0.5f, 1f),
                affectedTraits = new List<string> { "Chronos Gene", "Temporal Shift", "Reality Anchor" },
                possibleCauses = new List<string>
                {
                    "Dimensional rift exposure",
                    "Temporal experiment gone wrong",
                    "Ancient genetic activation"
                },
                riskLevel = Random.Range(0.3f, 0.9f)
            };
        }

        private List<ExperimentEvidence> GenerateExperimentEvidence()
        {
            return new List<ExperimentEvidence>
            {
                new ExperimentEvidence
                {
                    evidenceType = "Chemical Residue",
                    description = "Unknown mutagen compounds found",
                    location = GenerateRandomLocation(),
                    significance = Random.Range(0.5f, 0.9f)
                },
                new ExperimentEvidence
                {
                    evidenceType = "Equipment Traces",
                    description = "Specialized genetic modification equipment detected",
                    location = GenerateRandomLocation(),
                    significance = Random.Range(0.6f, 1f)
                }
            };
        }

        private int CalculateMysteryReward(GeneticMystery mystery)
        {
            int baseReward = 100;

            // Difficulty multiplier
            baseReward *= mystery.difficulty switch
            {
                CaseDifficulty.Novice => 1,
                CaseDifficulty.Intermediate => 2,
                CaseDifficulty.Expert => 4,
                CaseDifficulty.Master => 7,
                CaseDifficulty.Legendary => 12,
                _ => 1
            };

            // Type multiplier
            baseReward += mystery.type switch
            {
                MysteryType.MysteryCreature => 50,
                MysteryType.BreedingCrime => 200,
                MysteryType.BloodlineInvestigation => 150,
                MysteryType.GeneticAnomaly => 300,
                MysteryType.IllegalExperimentation => 500,
                _ => 100
            };

            return baseReward;
        }

        private LocationClue? GenerateLocationClue(GeneticMystery mystery, Vector3 location, float searchRadius)
        {
            var clueTypes = new[]
            {
                "Genetic material traces", "Footprint patterns", "Environmental disturbance",
                "Chemical residue", "Equipment fragments", "Biological samples"
            };

            return new LocationClue
            {
                id = Guid.NewGuid().ToString(),
                type = clueTypes[Random.Range(0, clueTypes.Length)],
                description = GenerateClueDescription(mystery.type),
                location = location + Random.insideUnitSphere * searchRadius,
                importance = Random.Range(0.3f, 0.9f),
                discoveryTime = Time.time,
                relatedEvidence = new List<string>()
            };
        }

        private string GenerateClueDescription(MysteryType mysteryType)
        {
            return mysteryType switch
            {
                MysteryType.MysteryCreature => "Strange hair samples with unknown genetic markers",
                MysteryType.BreedingCrime => "Security camera showing suspicious activity",
                MysteryType.BloodlineInvestigation => "Ancient breeding records with matching signatures",
                MysteryType.GeneticAnomaly => "Radiation readings suggesting temporal interference",
                MysteryType.IllegalExperimentation => "Hidden laboratory equipment traces",
                _ => "Unusual evidence requiring further analysis"
            };
        }

        private List<string> GenerateInterviewResponses(GeneticMystery mystery, string subjectId, List<string> questions)
        {
            var responses = new List<string>();

            foreach (var question in questions)
            {
                var response = GenerateInterviewResponse(mystery, subjectId, question);
                responses.Add(response);
            }

            return responses;
        }

        private string GenerateInterviewResponse(GeneticMystery mystery, string subjectId, string question)
        {
            var responseTypes = new[]
            {
                "I don't know anything about that.",
                "I saw something strange last week...",
                "You should ask Dr. Jenkins about that.",
                "There have been rumors circulating...",
                "I'm not at liberty to discuss that.",
                "That's classified information.",
                "I might have seen something, but..."
            };

            return responseTypes[Random.Range(0, responseTypes.Length)];
        }

        private float CalculateSubjectTruthfulness(GeneticMystery mystery, string subjectId)
        {
            // In a real implementation, this would be based on character traits and mystery type
            return Random.Range(0.3f, 0.9f);
        }

        private List<string> ExtractNewInformation(GeneticMystery mystery, string subjectId, List<string> questions)
        {
            var newInfo = new List<string>();

            // Randomly generate new information based on questions asked
            for (int i = 0; i < Random.Range(0, 3); i++)
            {
                newInfo.Add($"New clue from {subjectId}: {GenerateRandomClue(mystery.type)}");
            }

            return newInfo;
        }

        private string GenerateRandomClue(MysteryType mysteryType)
        {
            return mysteryType switch
            {
                MysteryType.MysteryCreature => "Creature was seen near the old research facility",
                MysteryType.BreedingCrime => "Security was disabled between 2-4 AM",
                MysteryType.BloodlineInvestigation => "Records mention a hidden breeding program",
                MysteryType.GeneticAnomaly => "Similar cases reported in other regions",
                MysteryType.IllegalExperimentation => "Shipments of unusual chemicals were delivered",
                _ => "Additional evidence requires investigation"
            };
        }

        private string GenerateHypothesisFeedback(CaseHypothesis hypothesis)
        {
            if (hypothesis.accuracy > 0.85f)
                return "Excellent deduction! Your hypothesis aligns perfectly with the evidence.";
            else if (hypothesis.accuracy > 0.6f)
                return "Good reasoning, but some aspects need more investigation.";
            else if (hypothesis.accuracy > 0.4f)
                return "Interesting theory, but the evidence doesn't fully support it.";
            else
                return "This hypothesis doesn't match the available evidence. Keep investigating.";
        }

        private void UpdateActiveCases()
        {
            // Handle case timeouts and evidence decay
            foreach (var kvp in activeMysteries.ToArray())
            {
                var mystery = kvp.Value;
                if (Time.time > mystery.timeLimit)
                {
                    // Move to cold cases
                    var coldCase = new ColdCase
                    {
                        id = mystery.id,
                        title = mystery.title,
                        description = mystery.description,
                        originalDate = mystery.discoveryTime,
                        mysteryType = mystery.type,
                        difficulty = mystery.difficulty,
                        reward = mystery.reward,
                        isActive = true,
                        clues = new List<string>()
                    };

                    coldCases.Add(coldCase);
                    activeMysteries.Remove(kvp.Key);

                    UnityEngine.Debug.Log($"Mystery '{mystery.title}' moved to cold cases due to timeout");
                }
            }

            // Apply evidence decay
            foreach (var evidenceList in caseEvidence.Values)
            {
                for (int i = 0; i < evidenceList.Count; i++)
                {
                    var evidence = evidenceList[i];
                    evidence.quality = Mathf.Max(0.1f, evidence.quality - evidenceDecayRate * Time.deltaTime);
                    evidenceList[i] = evidence;
                }
            }
        }

        private void ProcessColdCases()
        {
            if (!enableColdCases) return;

            // Occasionally reactivate cold cases with new evidence
            foreach (var coldCase in coldCases.Where(c => c.isActive))
            {
                if (Random.value < 0.05f) // 5% chance per day
                {
                    ReactivateColdCase(coldCase);
                }
            }
        }

        private void ReactivateColdCase(ColdCase coldCase)
        {
            // Convert cold case back to active mystery with new evidence
            var mystery = new GeneticMystery
            {
                id = Guid.NewGuid().ToString(),
                type = coldCase.mysteryType,
                title = $"Cold Case: {coldCase.title}",
                description = $"New evidence has emerged in this cold case: {coldCase.description}",
                difficulty = coldCase.difficulty,
                discoveryLocation = GenerateRandomLocation(),
                discoveryTime = Time.time,
                isActive = true,
                timeLimit = Time.time + (caseTimeoutDays * 86400f),
                reward = coldCase.reward + 500 // Bonus for cold case
            };

            activeMysteries[mystery.id] = mystery;
            coldCase.isActive = false;

            OnMysteryGenerated?.Invoke(mystery);
            UnityEngine.Debug.Log($"Cold case reactivated: {coldCase.title}");
        }

        private void EnsureDetectiveProfile(string detectiveId)
        {
            if (!detectiveProfiles.ContainsKey(detectiveId))
            {
                detectiveProfiles[detectiveId] = new DetectiveProfile
                {
                    detectiveId = detectiveId,
                    name = GetDetectiveName(detectiveId),
                    reputation = 10f,
                    solvedCases = 0,
                    activeCases = 0,
                    specialization = DetectiveSpecialization.Generalist,
                    totalRewards = 0,
                    legendaryLevelCases = 0,
                    joinDate = Time.time
                };
            }
        }

        private string GetDetectiveName(string detectiveId)
        {
            return $"Detective_{detectiveId[..Math.Min(8, detectiveId.Length)]}";
        }

        private void AwardDetectiveReputation(string detectiveId, float amount)
        {
            EnsureDetectiveProfile(detectiveId);
            var detective = detectiveProfiles[detectiveId];

            detective.reputation += amount;
            detective.reputation = Mathf.Max(0f, detective.reputation);

            OnDetectiveReputationChanged?.Invoke(detectiveId, detective.reputation);
        }

        private void LoadDetectiveDatabase()
        {
            // Load saved detective data from persistent storage
        }

        private void SaveDetectiveDatabase()
        {
            // Save detective data to persistent storage
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveDetectiveDatabase();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets all active mysteries available for investigation
        /// </summary>
        public GeneticMystery[] GetActiveMysteries()
        {
            return activeMysteries.Values.OrderByDescending(m => m.discoveryTime).ToArray();
        }

        /// <summary>
        /// Gets all cold cases available for investigation
        /// </summary>
        public ColdCase[] GetColdCases()
        {
            return coldCases.Where(c => c.isActive).OrderByDescending(c => c.originalDate).ToArray();
        }

        /// <summary>
        /// Gets detective profile for a player
        /// </summary>
        public DetectiveProfile GetDetectiveProfile(string detectiveId)
        {
            EnsureDetectiveProfile(detectiveId);
            return detectiveProfiles[detectiveId];
        }

        /// <summary>
        /// Gets evidence collected for a specific case
        /// </summary>
        public GeneticEvidence[] GetCaseEvidence(string caseId)
        {
            return caseEvidence.ContainsKey(caseId) ? caseEvidence[caseId].ToArray() : new GeneticEvidence[0];
        }

        /// <summary>
        /// Gets detective leaderboard
        /// </summary>
        public DetectiveProfile[] GetDetectiveLeaderboard(int maxCount = 10)
        {
            return detectiveProfiles.Values.OrderByDescending(d => d.reputation)
                .Take(maxCount).ToArray();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Represents a genetic mystery to be solved
    /// </summary>
    [Serializable]
    public struct GeneticMystery
    {
        public string id;
        public MysteryType type;
        public string title;
        public string description;
        public CaseDifficulty difficulty;
        public Vector3 discoveryLocation;
        public float discoveryTime;
        public float timeLimit;
        public bool isActive;
        public int reward;
        public int expectedSolutionSteps;

        // Type-specific data
        public GeneticProfile mysteryCreature;
        public List<string> suspectPool;
        public BloodlineTrace targetBloodline;
        public GeneticAnomalyData anomalyData;
        public List<ExperimentEvidence> evidenceTrail;
    }

    /// <summary>
    /// Types of genetic mysteries
    /// </summary>
    public enum MysteryType
    {
        MysteryCreature,        // Unknown creature analysis
        BreedingCrime,          // Criminal breeding activity
        BloodlineInvestigation, // Bloodline verification
        GeneticAnomaly,         // Unexplained genetic phenomena
        IllegalExperimentation  // Illegal genetic research
    }

    /// <summary>
    /// Difficulty levels for detective cases
    /// </summary>
    public enum CaseDifficulty
    {
        Novice = 1,
        Intermediate = 2,
        Expert = 3,
        Master = 4,
        Legendary = 5
    }

    /// <summary>
    /// Represents an active detective investigation
    /// </summary>
    [Serializable]
    public struct DetectiveCase
    {
        public string id;
        public string mysteryId;
        public string investigatorId;
        public float startTime;
        public float solutionTime;
        public CaseStatus status;
        public float progress; // 0-1 completion
        public List<string> discoveredClues;
        public List<string> collectedEvidence;
        public List<CaseHypothesis> hypotheses;
        public List<Interview> interviews;
        public CaseHypothesis finalSolution;
    }

    /// <summary>
    /// Status of a detective case
    /// </summary>
    public enum CaseStatus
    {
        Active,
        ReadyForSolution,
        Solved,
        Abandoned,
        Cold
    }

    /// <summary>
    /// Genetic evidence collected during investigation
    /// </summary>
    [Serializable]
    public struct GeneticEvidence
    {
        public string id;
        public string caseId;
        public EvidenceType type;
        public GeneticProfile geneticProfile;
        public Vector3 collectionLocation;
        public float collectionTime;
        public float quality; // 0-1 reliability
        public float reliability; // Analysis accuracy
        public Dictionary<string, object> analysisResults;
    }

    /// <summary>
    /// Types of evidence
    /// </summary>
    public enum EvidenceType
    {
        GeneticSample,
        EnvironmentalTrace,
        BreedingRecord,
        WitnessTestimony,
        DocumentaryEvidence
    }

    /// <summary>
    /// Clue discovered at a location
    /// </summary>
    [Serializable]
    public struct LocationClue
    {
        public string id;
        public string type;
        public string description;
        public Vector3 location;
        public float importance; // 0-1 significance
        public float discoveryTime;
        public List<string> relatedEvidence;
    }

    /// <summary>
    /// Interview with a subject
    /// </summary>
    [Serializable]
    public struct Interview
    {
        public string id;
        public string caseId;
        public string subjectId;
        public float conductedTime;
        public List<string> questions;
        public List<string> responses;
        public float truthfulness; // 0-1 how honest they were
        public List<string> newInformation;
    }

    /// <summary>
    /// Hypothesis about case solution
    /// </summary>
    [Serializable]
    public struct CaseHypothesis
    {
        public string id;
        public string hypothesis;
        public List<string> supportingEvidence;
        public float submissionTime;
        public float confidence; // How confident the detective is
        public float accuracy; // How accurate the hypothesis is
    }

    /// <summary>
    /// Result of hypothesis submission
    /// </summary>
    [Serializable]
    public struct HypothesisResult
    {
        public CaseHypothesis hypothesis;
        public string feedback;
        public bool isSolution;
        public float partialCredit;
    }

    /// <summary>
    /// Cold case data
    /// </summary>
    [Serializable]
    public struct ColdCase
    {
        public string id;
        public string title;
        public string description;
        public float originalDate;
        public MysteryType mysteryType;
        public CaseDifficulty difficulty;
        public int reward;
        public bool isActive;
        public List<string> clues;
    }

    /// <summary>
    /// Detective profile and statistics
    /// </summary>
    [Serializable]
    public struct DetectiveProfile
    {
        public string detectiveId;
        public string name;
        public float reputation;
        public int solvedCases;
        public int activeCases;
        public DetectiveSpecialization specialization;
        public int totalRewards;
        public int legendaryLevelCases;
        public float joinDate;

        public string rank => reputation switch
        {
            >= 1000f => "Master Detective",
            >= 500f => "Senior Investigator",
            >= 200f => "Detective",
            >= 100f => "Investigator",
            >= 50f => "Junior Detective",
            _ => "Trainee"
        };
    }

    /// <summary>
    /// Detective specialization areas
    /// </summary>
    public enum DetectiveSpecialization
    {
        Generalist,
        GeneticForensics,
        BreedingCrimes,
        BloodlineExpert,
        AnomalySpecialist
    }

    /// <summary>
    /// Bloodline trace information
    /// </summary>
    [Serializable]
    public struct BloodlineTrace
    {
        public string lineageId;
        public string originalFounder;
        public int generationsBack;
        public List<string> knownTraits;
        public Vector3 lastKnownLocation;
        public bool isLegendary;
        public float mysteryLevel;
    }

    /// <summary>
    /// Genetic anomaly data
    /// </summary>
    [Serializable]
    public struct GeneticAnomalyData
    {
        public string anomalyType;
        public float severity;
        public List<string> affectedTraits;
        public List<string> possibleCauses;
        public float riskLevel;
    }

    /// <summary>
    /// Experimental evidence
    /// </summary>
    [Serializable]
    public struct ExperimentEvidence
    {
        public string evidenceType;
        public string description;
        public Vector3 location;
        public float significance;
    }

    #endregion
}