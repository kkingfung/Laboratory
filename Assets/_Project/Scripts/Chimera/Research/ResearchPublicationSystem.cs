using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Discovery;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;

namespace Laboratory.Chimera.Research
{
    /// <summary>
    /// Research Publication and Peer Review System for Project Chimera.
    /// Enables players to document their genetic discoveries, submit research papers,
    /// participate in peer review, and build scientific credibility within the community.
    ///
    /// Features:
    /// - Research Paper Creation: Document genetic discoveries with evidence and analysis
    /// - Peer Review Process: Community validation of research findings
    /// - Academic Reputation: Build scientific credibility through quality research
    /// - Collaborative Research: Multi-player research projects and co-authorship
    /// - Research Grants: Funding system for ambitious research projects
    /// - Publication Archive: Searchable database of all community research
    /// </summary>
    public class ResearchPublicationSystem : MonoBehaviour
    {
        [Header("System Configuration")]
        [SerializeField] private ResearchPublicationConfig config;

        [Header("Review Settings")]
        [SerializeField] private int minReviewersPerPaper = 3;
        [SerializeField] private float reviewTimeoutDays = 7f;
        [SerializeField] private float reputationRequiredToReview = 50f;

        [Header("Quality Control")]
        [SerializeField] private float minimumDataQuality = 0.6f;
        [SerializeField] private int minimumEvidenceSamples = 5;
        [SerializeField] private float plagiarismThreshold = 0.8f;

        // Research management
        private Dictionary<string, ResearchPaper> publishedPapers = new Dictionary<string, ResearchPaper>();
        private Dictionary<string, ResearchPaper> pendingReview = new Dictionary<string, ResearchPaper>();
        private List<ResearchPaper> draftPapers = new List<ResearchPaper>();

        // Peer review management
        private Dictionary<string, List<PeerReview>> activePeerReviews = new Dictionary<string, List<PeerReview>>();
        private Dictionary<string, ReviewAssignment> reviewAssignments = new Dictionary<string, ReviewAssignment>();

        // Researcher profiles and reputation
        private Dictionary<string, ResearcherProfile> researcherProfiles = new Dictionary<string, ResearcherProfile>();
        private Dictionary<string, List<string>> collaborationNetwork = new Dictionary<string, List<string>>();

        // Research grants and funding
        private List<ResearchGrant> availableGrants = new List<ResearchGrant>();
        private Dictionary<string, GrantApplication> grantApplications = new Dictionary<string, GrantApplication>();

        // Events
        public static event Action<ResearchPaper> OnPaperPublished;
        public static event Action<ResearchPaper> OnPaperSubmitted;
        public static event Action<PeerReview> OnReviewCompleted;
        public static event Action<ResearchGrant> OnGrantAwarded;
        public static event Action<string, float> OnReputationChanged; // Researcher ID, New Reputation

        void Start()
        {
            InitializeResearchSystem();
            InvokeRepeating(nameof(ProcessPeerReviews), 60f, 3600f); // Process reviews hourly
            InvokeRepeating(nameof(UpdateResearchGrants), 300f, 86400f); // Update grants daily
        }

        #region Initialization

        private void InitializeResearchSystem()
        {
            LoadResearchDatabase();
            SeedInitialGrants();
            UnityEngine.Debug.Log("Research Publication System initialized - Ready for scientific collaboration!");
        }

        private void SeedInitialGrants()
        {
            // Create some initial research grants
            availableGrants.AddRange(new[]
            {
                new ResearchGrant
                {
                    id = Guid.NewGuid().ToString(),
                    title = "Rare Genetic Combination Research Grant",
                    description = "Funding for research into unusual genetic combinations and their phenotypic expressions",
                    fundingAmount = 10000,
                    duration = 90,
                    requirements = new[] { "minimum 5 published papers", "reputation > 200" },
                    topic = ResearchTopic.GeneticCombinations,
                    deadline = Time.time + 2592000f, // 30 days
                    isActive = true
                },
                new ResearchGrant
                {
                    id = Guid.NewGuid().ToString(),
                    title = "Ecosystem Impact Study",
                    description = "Research the effects of breeding programs on wild populations and ecosystem health",
                    fundingAmount = 15000,
                    duration = 120,
                    requirements = new[] { "conservation focus", "reputation > 150" },
                    topic = ResearchTopic.EcosystemImpact,
                    deadline = Time.time + 2592000f,
                    isActive = true
                }
            });
        }

        #endregion

        #region Research Paper Creation

        /// <summary>
        /// Creates a new research paper draft
        /// </summary>
        public ResearchPaper CreateResearchDraft(string authorId, string title, ResearchTopic topic)
        {
            var paper = new ResearchPaper
            {
                id = Guid.NewGuid().ToString(),
                title = title,
                authors = new List<string> { authorId },
                topic = topic,
                status = PaperStatus.Draft,
                creationTime = Time.time,
                abstractText = "",
                methodology = "",
                results = new List<ResearchEvidence>(),
                conclusions = "",
                keywords = new List<string>(),
                citedPapers = new List<string>()
            };

            draftPapers.Add(paper);

            // Initialize author profile if needed
            EnsureResearcherProfile(authorId);

            UnityEngine.Debug.Log($"Research draft '{title}' created by {GetResearcherName(authorId)}");
            return paper;
        }

        /// <summary>
        /// Adds genetic evidence to a research paper
        /// </summary>
        public void AddGeneticEvidence(string paperId, GeneticProfile profile, string description, Vector3 location)
        {
            var paper = FindPaperById(paperId);
            if (paper == null || paper.status != PaperStatus.Draft) return;

            var evidence = new ResearchEvidence
            {
                id = Guid.NewGuid().ToString(),
                type = EvidenceType.GeneticData,
                description = description,
                geneticProfile = profile,
                location = location,
                timestamp = Time.time,
                quality = CalculateEvidenceQuality(profile),
                verified = false
            };

            paper.results.Add(evidence);
            UnityEngine.Debug.Log($"Genetic evidence added to paper '{paper.title}': {description}");
        }

        /// <summary>
        /// Adds breeding outcome evidence to a research paper
        /// </summary>
        public void AddBreedingEvidence(string paperId, GeneticProfile parent1, GeneticProfile parent2,
            GeneticProfile offspring, string outcomeDescription)
        {
            var paper = FindPaperById(paperId);
            if (paper == null || paper.status != PaperStatus.Draft) return;

            var evidence = new ResearchEvidence
            {
                id = Guid.NewGuid().ToString(),
                type = EvidenceType.BreedingOutcome,
                description = outcomeDescription,
                parentProfile1 = parent1,
                parentProfile2 = parent2,
                geneticProfile = offspring,
                timestamp = Time.time,
                quality = CalculateBreedingEvidenceQuality(parent1, parent2, offspring),
                verified = false
            };

            paper.results.Add(evidence);
            UnityEngine.Debug.Log($"Breeding evidence added to paper '{paper.title}': {outcomeDescription}");
        }

        /// <summary>
        /// Adds population data evidence
        /// </summary>
        public void AddPopulationEvidence(string paperId, string speciesId, int populationCount,
            float healthMetric, BiomeType biome, string analysis)
        {
            var paper = FindPaperById(paperId);
            if (paper == null || paper.status != PaperStatus.Draft) return;

            var evidence = new ResearchEvidence
            {
                id = Guid.NewGuid().ToString(),
                type = EvidenceType.PopulationData,
                description = analysis,
                speciesId = speciesId,
                populationCount = populationCount,
                healthMetric = healthMetric,
                biome = biome,
                timestamp = Time.time,
                quality = CalculatePopulationEvidenceQuality(populationCount, healthMetric),
                verified = false
            };

            paper.results.Add(evidence);
            UnityEngine.Debug.Log($"Population evidence added to paper '{paper.title}': {analysis}");
        }

        private float CalculateEvidenceQuality(GeneticProfile profile)
        {
            if (profile?.Genes == null) return 0f;

            float quality = 0.5f; // Base quality

            // Higher generation creatures provide better data
            quality += profile.Generation * 0.02f;

            // More active genes = better data
            var activeGenes = profile.Genes.Count(g => g.isActive);
            quality += activeGenes * 0.05f;

            // Mutations add research value
            quality += profile.Mutations.Count() * 0.1f;

            // Genetic purity affects reliability
            quality += profile.GetGeneticPurity() * 0.3f;

            return Mathf.Clamp01(quality);
        }

        private float CalculateBreedingEvidenceQuality(GeneticProfile parent1, GeneticProfile parent2, GeneticProfile offspring)
        {
            float quality = (CalculateEvidenceQuality(parent1) + CalculateEvidenceQuality(parent2) +
                           CalculateEvidenceQuality(offspring)) / 3f;

            // Bonus for interesting genetic combinations
            var similarity = parent1.GetGeneticSimilarity(parent2);
            if (similarity < 0.3f) quality += 0.2f; // Very different parents = more interesting

            // Bonus for successful trait inheritance
            var inheritedTraits = CountInheritedTraits(parent1, parent2, offspring);
            quality += inheritedTraits * 0.05f;

            return Mathf.Clamp01(quality);
        }

        private float CalculatePopulationEvidenceQuality(int populationCount, float healthMetric)
        {
            float quality = 0.3f;

            // Larger populations provide better statistical data
            quality += Mathf.Clamp01(populationCount / 1000f) * 0.4f;

            // Health metric reliability
            quality += healthMetric * 0.3f;

            return Mathf.Clamp01(quality);
        }

        private int CountInheritedTraits(GeneticProfile parent1, GeneticProfile parent2, GeneticProfile offspring)
        {
            var parentTraits = parent1.Genes.Concat(parent2.Genes).Select(g => g.traitName).Distinct().ToList();
            var offspringTraits = offspring.Genes.Select(g => g.traitName).ToList();

            return parentTraits.Count(trait => offspringTraits.Contains(trait));
        }

        #endregion

        #region Paper Submission and Review

        /// <summary>
        /// Submits a research paper for peer review
        /// </summary>
        public bool SubmitPaperForReview(string paperId)
        {
            var paper = FindPaperById(paperId);
            if (paper == null || paper.status != PaperStatus.Draft) return false;

            // Validate paper quality
            if (!ValidatePaperForSubmission(paper)) return false;

            // Move to pending review
            paper.status = PaperStatus.UnderReview;
            paper.submissionTime = Time.time;
            pendingReview[paper.id] = paper;
            draftPapers.Remove(paper);

            // Assign peer reviewers
            AssignPeerReviewers(paper);

            OnPaperSubmitted?.Invoke(paper);
            UnityEngine.Debug.Log($"Paper '{paper.title}' submitted for peer review");

            return true;
        }

        private bool ValidatePaperForSubmission(ResearchPaper paper)
        {
            // Check minimum requirements
            if (string.IsNullOrEmpty(paper.title) || paper.title.Length < 10)
            {
                UnityEngine.Debug.Log("Paper title too short for submission");
                return false;
            }

            if (string.IsNullOrEmpty(paper.abstractText) || paper.abstractText.Length < 100)
            {
                UnityEngine.Debug.Log("Paper abstract too short for submission");
                return false;
            }

            if (paper.results.Count < minimumEvidenceSamples)
            {
                UnityEngine.Debug.Log($"Insufficient evidence samples: {paper.results.Count} < {minimumEvidenceSamples}");
                return false;
            }

            // Check data quality
            var avgQuality = paper.results.Average(r => r.quality);
            if (avgQuality < minimumDataQuality)
            {
                UnityEngine.Debug.Log($"Data quality too low: {avgQuality:F2} < {minimumDataQuality:F2}");
                return false;
            }

            // Check for plagiarism (simplified)
            if (CheckForPlagiarism(paper))
            {
                UnityEngine.Debug.Log("Potential plagiarism detected");
                return false;
            }

            return true;
        }

        private bool CheckForPlagiarism(ResearchPaper paper)
        {
            // Simplified plagiarism check - compare abstracts and conclusions
            foreach (var publishedPaper in publishedPapers.Values)
            {
                var similarity = CalculateTextSimilarity(paper.abstractText, publishedPaper.abstractText);
                if (similarity > plagiarismThreshold)
                    return true;

                similarity = CalculateTextSimilarity(paper.conclusions, publishedPaper.conclusions);
                if (similarity > plagiarismThreshold)
                    return true;
            }

            return false;
        }

        private float CalculateTextSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2)) return 0f;

            // Simple word-based similarity calculation
            var words1 = text1.ToLower().Split(' ').Where(w => w.Length > 3).ToHashSet();
            var words2 = text2.ToLower().Split(' ').Where(w => w.Length > 3).ToHashSet();

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return union > 0 ? (float)intersection / union : 0f;
        }

        private void AssignPeerReviewers(ResearchPaper paper)
        {
            var eligibleReviewers = GetEligibleReviewers(paper);
            var selectedReviewers = SelectReviewers(eligibleReviewers, minReviewersPerPaper);

            var reviews = new List<PeerReview>();
            foreach (var reviewerId in selectedReviewers)
            {
                var review = new PeerReview
                {
                    id = Guid.NewGuid().ToString(),
                    paperId = paper.id,
                    reviewerId = reviewerId,
                    assignedTime = Time.time,
                    deadline = Time.time + (reviewTimeoutDays * 86400f),
                    status = ReviewStatus.Assigned,
                    recommendation = ReviewRecommendation.Pending
                };

                reviews.Add(review);

                // Create review assignment
                reviewAssignments[review.id] = new ReviewAssignment
                {
                    reviewId = review.id,
                    paperId = paper.id,
                    reviewerId = reviewerId,
                    isActive = true
                };
            }

            activePeerReviews[paper.id] = reviews;
            UnityEngine.Debug.Log($"Assigned {reviews.Count} peer reviewers to paper '{paper.title}'");
        }

        private List<string> GetEligibleReviewers(ResearchPaper paper)
        {
            var eligible = new List<string>();

            foreach (var kvp in researcherProfiles)
            {
                var researcher = kvp.Value;

                // Skip paper authors
                if (paper.authors.Contains(researcher.researcherId)) continue;

                // Check reputation requirement
                if (researcher.reputation < reputationRequiredToReview) continue;

                // Check expertise in topic
                if (researcher.expertiseAreas.Contains(paper.topic) || researcher.reputation > 200f)
                {
                    eligible.Add(researcher.researcherId);
                }
            }

            return eligible;
        }

        private List<string> SelectReviewers(List<string> eligible, int count)
        {
            if (eligible.Count <= count) return eligible;

            // Weight selection by reputation and availability
            var weighted = eligible.OrderByDescending(id => {
                var researcher = researcherProfiles[id];
                var availabilityPenalty = researcher.activeReviews * 0.1f; // Penalize overloaded reviewers
                return researcher.reputation - availabilityPenalty;
            }).Take(count).ToList();

            return weighted;
        }

        #endregion

        #region Peer Review Process

        /// <summary>
        /// Submits a peer review for a paper
        /// </summary>
        public bool SubmitPeerReview(string reviewId, float qualityScore, ReviewRecommendation recommendation,
            string comments, List<string> suggestions)
        {
            if (!activePeerReviews.Values.Any(reviews => reviews.Any(r => r.id == reviewId)))
                return false;

            var review = activePeerReviews.Values.SelectMany(r => r).FirstOrDefault(r => r.id == reviewId);
            if (string.IsNullOrEmpty(review.id) || review.status != ReviewStatus.Assigned) return false;

            // Update review
            review.status = ReviewStatus.Completed;
            review.completionTime = Time.time;
            review.qualityScore = Mathf.Clamp01(qualityScore);
            review.recommendation = recommendation;
            review.comments = comments;
            review.suggestions = suggestions ?? new List<string>();

            // Update reviewer profile
            var reviewer = researcherProfiles[review.reviewerId];
            reviewer.completedReviews++;
            reviewer.activeReviews = Mathf.Max(0, reviewer.activeReviews - 1);

            // Award reputation for quality review
            var reputationGain = CalculateReviewReputationGain(review);
            AwardReputation(review.reviewerId, reputationGain);

            OnReviewCompleted?.Invoke(review);

            // Check if all reviews are complete
            CheckPaperReviewCompletion(review.paperId);

            UnityEngine.Debug.Log($"Peer review completed for paper by {GetResearcherName(review.reviewerId)}");
            return true;
        }

        private float CalculateReviewReputationGain(PeerReview review)
        {
            float baseGain = 5f;

            // Bonus for timely reviews
            var reviewTime = review.completionTime - review.assignedTime;
            var timeBonus = reviewTime < (reviewTimeoutDays * 86400f * 0.5f) ? 2f : 0f;

            // Bonus for detailed feedback
            var detailBonus = review.comments.Length > 200 ? 3f : 0f;
            detailBonus += review.suggestions.Count * 1f;

            return baseGain + timeBonus + detailBonus;
        }

        private void CheckPaperReviewCompletion(string paperId)
        {
            if (!activePeerReviews.ContainsKey(paperId)) return;

            var reviews = activePeerReviews[paperId];
            var completedReviews = reviews.Where(r => r.status == ReviewStatus.Completed).ToList();

            if (completedReviews.Count >= minReviewersPerPaper)
            {
                ProcessPaperReviewResults(paperId, completedReviews);
            }
        }

        private void ProcessPaperReviewResults(string paperId, List<PeerReview> reviews)
        {
            if (!pendingReview.ContainsKey(paperId)) return;

            var paper = pendingReview[paperId];
            var avgQuality = reviews.Average(r => r.qualityScore);

            // Determine final decision based on reviews
            var acceptCount = reviews.Count(r => r.recommendation == ReviewRecommendation.Accept);
            var rejectCount = reviews.Count(r => r.recommendation == ReviewRecommendation.Reject);
            var reviseCount = reviews.Count(r => r.recommendation == ReviewRecommendation.MinorRevisions ||
                                                 r.recommendation == ReviewRecommendation.MajorRevisions);

            if (acceptCount >= reviews.Count * 0.6f && avgQuality >= 0.7f)
            {
                // Accept for publication
                PublishPaper(paper, reviews);
            }
            else if (rejectCount >= reviews.Count * 0.6f || avgQuality < 0.4f)
            {
                // Reject paper
                RejectPaper(paper, reviews);
            }
            else
            {
                // Request revisions
                RequestRevisions(paper, reviews);
            }
        }

        private void PublishPaper(ResearchPaper paper, List<PeerReview> reviews)
        {
            paper.status = PaperStatus.Published;
            paper.publicationTime = Time.time;
            paper.peerReviews = reviews;
            paper.qualityScore = reviews.Average(r => r.qualityScore);

            // Move to published papers
            publishedPapers[paper.id] = paper;
            pendingReview.Remove(paper.id);
            activePeerReviews.Remove(paper.id);

            // Award reputation to authors
            foreach (var authorId in paper.authors)
            {
                var reputationGain = CalculatePublicationReputationGain(paper);
                AwardReputation(authorId, reputationGain);

                var author = researcherProfiles[authorId];
                author.publishedPapers++;

                if (paper.qualityScore >= 0.8f)
                    author.highQualityPapers++;
            }

            OnPaperPublished?.Invoke(paper);
            UnityEngine.Debug.Log($"Paper '{paper.title}' published successfully!");
        }

        private float CalculatePublicationReputationGain(ResearchPaper paper)
        {
            float baseGain = 20f;

            // Quality bonus
            var qualityBonus = (paper.qualityScore - 0.5f) * 40f; // 0-20 bonus

            // Topic importance bonus
            var topicBonus = paper.topic switch
            {
                ResearchTopic.GeneticCombinations => 5f,
                ResearchTopic.EcosystemImpact => 10f,
                ResearchTopic.ConservationBiology => 8f,
                ResearchTopic.BehavioralGenetics => 6f,
                _ => 3f
            };

            // Evidence quality bonus
            var evidenceBonus = paper.results.Average(r => r.quality) * 10f;

            // Collaboration bonus
            var collaborationBonus = paper.authors.Count > 1 ? 5f : 0f;

            return baseGain + qualityBonus + topicBonus + evidenceBonus + collaborationBonus;
        }

        private void RejectPaper(ResearchPaper paper, List<PeerReview> reviews)
        {
            paper.status = PaperStatus.Rejected;
            paper.peerReviews = reviews;

            // Move back to drafts for potential revision
            draftPapers.Add(paper);
            pendingReview.Remove(paper.id);
            activePeerReviews.Remove(paper.id);

            UnityEngine.Debug.Log($"Paper '{paper.title}' rejected - returned to drafts");
        }

        private void RequestRevisions(ResearchPaper paper, List<PeerReview> reviews)
        {
            paper.status = PaperStatus.RequiresRevision;
            paper.peerReviews = reviews;

            // Move back to drafts for revision
            draftPapers.Add(paper);
            pendingReview.Remove(paper.id);
            activePeerReviews.Remove(paper.id);

            UnityEngine.Debug.Log($"Paper '{paper.title}' requires revisions");
        }

        #endregion

        #region Collaboration System

        /// <summary>
        /// Invites a researcher to collaborate on a paper
        /// </summary>
        public bool InviteCollaborator(string paperId, string inviteeId, string inviterMessage)
        {
            var paper = FindPaperById(paperId);
            if (paper == null || paper.status != PaperStatus.Draft) return false;

            if (paper.authors.Contains(inviteeId)) return false; // Already a collaborator

            // Create collaboration invitation
            var invitation = new CollaborationInvitation
            {
                id = Guid.NewGuid().ToString(),
                paperId = paperId,
                inviterId = paper.authors[0], // Primary author
                inviteeId = inviteeId,
                message = inviterMessage,
                timestamp = Time.time,
                status = InvitationStatus.Pending
            };

            // In a real implementation, this would be sent to the invitee
            UnityEngine.Debug.Log($"Collaboration invitation sent to {GetResearcherName(inviteeId)} for paper '{paper.title}'");
            return true;
        }

        /// <summary>
        /// Accepts a collaboration invitation
        /// </summary>
        public bool AcceptCollaboration(string invitationId)
        {
            // In a real implementation, this would look up the invitation
            // For now, we'll simulate accepting collaboration
            UnityEngine.Debug.Log("Collaboration invitation accepted");
            return true;
        }

        #endregion

        #region Reputation System

        private void AwardReputation(string researcherId, float amount)
        {
            EnsureResearcherProfile(researcherId);
            var researcher = researcherProfiles[researcherId];

            researcher.reputation += amount;
            researcher.reputation = Mathf.Max(0f, researcher.reputation);

            OnReputationChanged?.Invoke(researcherId, researcher.reputation);
        }

        private void EnsureResearcherProfile(string researcherId)
        {
            if (!researcherProfiles.ContainsKey(researcherId))
            {
                researcherProfiles[researcherId] = new ResearcherProfile
                {
                    researcherId = researcherId,
                    name = GetResearcherName(researcherId),
                    reputation = 10f, // Starting reputation
                    publishedPapers = 0,
                    completedReviews = 0,
                    highQualityPapers = 0,
                    activeReviews = 0,
                    joinDate = Time.time,
                    expertiseAreas = new List<ResearchTopic>(),
                    collaborators = new List<string>()
                };
            }
        }

        private string GetResearcherName(string researcherId)
        {
            // In a real implementation, this would look up the actual player name
            return $"Researcher_{researcherId[..Math.Min(8, researcherId.Length)]}";
        }

        #endregion

        #region Utility Methods

        private ResearchPaper FindPaperById(string paperId)
        {
            // Check drafts
            var draft = draftPapers.FirstOrDefault(p => p.id == paperId);
            if (draft != null) return draft;

            // Check pending review
            if (pendingReview.ContainsKey(paperId))
                return pendingReview[paperId];

            // Check published
            if (publishedPapers.ContainsKey(paperId))
                return publishedPapers[paperId];

            return null;
        }

        private void ProcessPeerReviews()
        {
            // Handle review timeouts and other periodic tasks
            foreach (var kvp in activePeerReviews.ToArray())
            {
                var reviews = kvp.Value;
                var expiredReviews = reviews.Where(r => r.status == ReviewStatus.Assigned &&
                                                       Time.time > r.deadline).ToList();

                for (int i = 0; i < expiredReviews.Count; i++)
                {
                    var review = expiredReviews[i];
                    review.status = ReviewStatus.Expired;
                    expiredReviews[i] = review;
                    UnityEngine.Debug.Log($"Review expired for paper {kvp.Key}");
                }
            }
        }

        private void UpdateResearchGrants()
        {
            // Update grant deadlines and create new grants
            for (int i = availableGrants.Count - 1; i >= 0; i--)
            {
                var grant = availableGrants[i];
                if (Time.time > grant.deadline)
                {
                    ProcessGrantApplications(grant);
                    availableGrants.RemoveAt(i);
                }
            }

            // Create new grants periodically
            if (UnityEngine.Random.value < 0.3f) // 30% chance
            {
                CreateRandomGrant();
            }
        }

        private void ProcessGrantApplications(ResearchGrant grant)
        {
            var applications = grantApplications.Values.Where(app => app.grantId == grant.id).ToList();
            if (applications.Count == 0) return;

            // Select best application based on researcher reputation and proposal quality
            var bestApplication = applications.OrderByDescending(app => {
                var researcher = researcherProfiles[app.applicantId];
                return researcher.reputation + app.proposalQuality * 50f;
            }).First();

            // Award grant
            OnGrantAwarded?.Invoke(grant);
            UnityEngine.Debug.Log($"Grant '{grant.title}' awarded to {GetResearcherName(bestApplication.applicantId)}");
        }

        private void CreateRandomGrant()
        {
            var topics = Enum.GetValues(typeof(ResearchTopic)).Cast<ResearchTopic>().ToArray();
            var randomTopic = topics[UnityEngine.Random.Range(0, topics.Length)];

            var grant = new ResearchGrant
            {
                id = Guid.NewGuid().ToString(),
                title = GenerateGrantTitle(randomTopic),
                description = GenerateGrantDescription(randomTopic),
                fundingAmount = UnityEngine.Random.Range(5000, 20000),
                duration = UnityEngine.Random.Range(60, 180),
                topic = randomTopic,
                deadline = Time.time + UnityEngine.Random.Range(1296000f, 2592000f), // 15-30 days
                isActive = true,
                requirements = GenerateGrantRequirements(randomTopic)
            };

            availableGrants.Add(grant);
            UnityEngine.Debug.Log($"New research grant available: {grant.title}");
        }

        private string GenerateGrantTitle(ResearchTopic topic)
        {
            return topic switch
            {
                ResearchTopic.GeneticCombinations => "Advanced Genetic Combination Studies",
                ResearchTopic.EcosystemImpact => "Ecosystem Health and Breeding Impact Research",
                ResearchTopic.ConservationBiology => "Species Conservation and Recovery Program",
                ResearchTopic.BehavioralGenetics => "Behavioral Expression of Genetic Traits",
                _ => "General Research Grant"
            };
        }

        private string GenerateGrantDescription(ResearchTopic topic)
        {
            return topic switch
            {
                ResearchTopic.GeneticCombinations => "Investigate rare and beneficial genetic combinations",
                ResearchTopic.EcosystemImpact => "Study the effects of breeding programs on wild ecosystems",
                ResearchTopic.ConservationBiology => "Develop strategies for species conservation and recovery",
                ResearchTopic.BehavioralGenetics => "Research the genetic basis of creature behavior patterns",
                _ => "Conduct research in assigned topic area"
            };
        }

        private string[] GenerateGrantRequirements(ResearchTopic topic)
        {
            return topic switch
            {
                ResearchTopic.GeneticCombinations => new[] { "minimum 3 published papers", "reputation > 100" },
                ResearchTopic.EcosystemImpact => new[] { "conservation focus", "field research experience" },
                ResearchTopic.ConservationBiology => new[] { "species conservation work", "reputation > 150" },
                ResearchTopic.BehavioralGenetics => new[] { "behavioral studies", "genetic analysis experience" },
                _ => new[] { "basic research experience" }
            };
        }

        private void LoadResearchDatabase()
        {
            // Load saved research data from persistent storage
        }

        private void SaveResearchDatabase()
        {
            // Save research data to persistent storage
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveResearchDatabase();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets all published papers
        /// </summary>
        public ResearchPaper[] GetPublishedPapers()
        {
            return publishedPapers.Values.OrderByDescending(p => p.publicationTime).ToArray();
        }

        /// <summary>
        /// Gets papers by a specific researcher
        /// </summary>
        public ResearchPaper[] GetPapersByResearcher(string researcherId)
        {
            return publishedPapers.Values.Where(p => p.authors.Contains(researcherId))
                .OrderByDescending(p => p.publicationTime).ToArray();
        }

        /// <summary>
        /// Gets researcher profile
        /// </summary>
        public ResearcherProfile GetResearcherProfile(string researcherId)
        {
            EnsureResearcherProfile(researcherId);
            return researcherProfiles[researcherId];
        }

        /// <summary>
        /// Gets available research grants
        /// </summary>
        public ResearchGrant[] GetAvailableGrants()
        {
            return availableGrants.Where(g => g.isActive && Time.time < g.deadline).ToArray();
        }

        /// <summary>
        /// Searches published papers by keyword
        /// </summary>
        public ResearchPaper[] SearchPapers(string keyword, ResearchTopic? topic = null)
        {
            var papers = publishedPapers.Values.AsEnumerable();

            if (topic.HasValue)
                papers = papers.Where(p => p.topic == topic.Value);

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                papers = papers.Where(p =>
                    p.title.ToLower().Contains(keyword) ||
                    p.abstractText.ToLower().Contains(keyword) ||
                    p.keywords.Any(k => k.ToLower().Contains(keyword)));
            }

            return papers.OrderByDescending(p => p.publicationTime).ToArray();
        }

        /// <summary>
        /// Gets research leaderboard
        /// </summary>
        public ResearcherProfile[] GetResearchLeaderboard(int maxCount = 10)
        {
            return researcherProfiles.Values.OrderByDescending(r => r.reputation)
                .Take(maxCount).ToArray();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Represents a research paper
    /// </summary>
    [Serializable]
    public class ResearchPaper
    {
        public string id;
        public string title;
        public List<string> authors;
        public ResearchTopic topic;
        public PaperStatus status;
        public float creationTime;
        public float submissionTime;
        public float publicationTime;
        public string abstractText;
        public string methodology;
        public List<ResearchEvidence> results;
        public string conclusions;
        public List<string> keywords;
        public List<string> citedPapers;
        public List<PeerReview> peerReviews;
        public float qualityScore;
    }

    /// <summary>
    /// Types of research topics
    /// </summary>
    public enum ResearchTopic
    {
        GeneticCombinations,
        EcosystemImpact,
        ConservationBiology,
        BehavioralGenetics,
        EvolutionaryBiology,
        PopulationDynamics
    }

    /// <summary>
    /// Paper status in the publication process
    /// </summary>
    public enum PaperStatus
    {
        Draft,
        UnderReview,
        RequiresRevision,
        Published,
        Rejected
    }

    /// <summary>
    /// Evidence supporting research findings
    /// </summary>
    [Serializable]
    public struct ResearchEvidence
    {
        public string id;
        public EvidenceType type;
        public string description;
        public GeneticProfile geneticProfile;
        public GeneticProfile parentProfile1;
        public GeneticProfile parentProfile2;
        public string speciesId;
        public int populationCount;
        public float healthMetric;
        public BiomeType biome;
        public Vector3 location;
        public float timestamp;
        public float quality;
        public bool verified;
    }

    /// <summary>
    /// Types of research evidence
    /// </summary>
    public enum EvidenceType
    {
        GeneticData,
        BreedingOutcome,
        PopulationData,
        BehavioralObservation,
        EnvironmentalMeasurement
    }

    /// <summary>
    /// Peer review of a research paper
    /// </summary>
    [Serializable]
    public struct PeerReview
    {
        public string id;
        public string paperId;
        public string reviewerId;
        public float assignedTime;
        public float deadline;
        public float completionTime;
        public ReviewStatus status;
        public float qualityScore;
        public ReviewRecommendation recommendation;
        public string comments;
        public List<string> suggestions;
    }

    /// <summary>
    /// Status of a peer review
    /// </summary>
    public enum ReviewStatus
    {
        Assigned,
        InProgress,
        Completed,
        Expired
    }

    /// <summary>
    /// Peer review recommendation
    /// </summary>
    public enum ReviewRecommendation
    {
        Pending,
        Accept,
        MinorRevisions,
        MajorRevisions,
        Reject
    }

    /// <summary>
    /// Researcher profile and reputation
    /// </summary>
    [Serializable]
    public struct ResearcherProfile
    {
        public string researcherId;
        public string name;
        public float reputation;
        public int publishedPapers;
        public int completedReviews;
        public int highQualityPapers;
        public int activeReviews;
        public float joinDate;
        public List<ResearchTopic> expertiseAreas;
        public List<string> collaborators;

        public string reputationTier => reputation switch
        {
            >= 1000f => "Distinguished Fellow",
            >= 500f => "Senior Researcher",
            >= 200f => "Research Associate",
            >= 100f => "Junior Researcher",
            >= 50f => "Research Assistant",
            _ => "Student Researcher"
        };
    }

    /// <summary>
    /// Research grant opportunity
    /// </summary>
    [Serializable]
    public struct ResearchGrant
    {
        public string id;
        public string title;
        public string description;
        public int fundingAmount;
        public int duration; // in days
        public ResearchTopic topic;
        public float deadline;
        public string[] requirements;
        public bool isActive;
    }

    /// <summary>
    /// Grant application
    /// </summary>
    [Serializable]
    public struct GrantApplication
    {
        public string id;
        public string grantId;
        public string applicantId;
        public string proposal;
        public float proposalQuality;
        public float applicationTime;
    }

    /// <summary>
    /// Review assignment tracking
    /// </summary>
    [Serializable]
    public struct ReviewAssignment
    {
        public string reviewId;
        public string paperId;
        public string reviewerId;
        public bool isActive;
    }

    /// <summary>
    /// Collaboration invitation
    /// </summary>
    [Serializable]
    public struct CollaborationInvitation
    {
        public string id;
        public string paperId;
        public string inviterId;
        public string inviteeId;
        public string message;
        public float timestamp;
        public InvitationStatus status;
    }

    /// <summary>
    /// Status of collaboration invitation
    /// </summary>
    public enum InvitationStatus
    {
        Pending,
        Accepted,
        Declined,
        Expired
    }

    #endregion
}