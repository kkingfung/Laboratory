using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.Research
{
    /// <summary>
    /// Peer review service for submitting and retrieving publication reviews.
    /// Extracted from ResearchSubsystemManager for single responsibility.
    /// </summary>
    public class PeerReviewService : IPeerReviewService
    {
        private readonly ResearchSubsystemConfig _config;
        private readonly Func<Dictionary<string, ResearchPublication>> _getPublications;
        private readonly Action<PeerReviewEvent> _onReviewSubmitted;
        private readonly Dictionary<string, List<PeerReview>> _publicationReviews = new();

        public PeerReviewService(ResearchSubsystemConfig config, Func<Dictionary<string, ResearchPublication>> getPublications, Action<PeerReviewEvent> onReviewSubmitted)
        {
            _config = config;
            _getPublications = getPublications;
            _onReviewSubmitted = onReviewSubmitted;
        }

        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
            Debug.Log("[PeerReviewService] Initialized successfully");
        }

        public async Task<string> SubmitReviewAsync(string publicationId, string reviewerId, int rating, string comments)
        {
            try
            {
                if (string.IsNullOrEmpty(publicationId) || string.IsNullOrEmpty(reviewerId))
                    return null;

                // Validate that the publication exists
                var publications = _getPublications();
                if (!publications.ContainsKey(publicationId))
                {
                    Debug.LogWarning($"[PeerReviewService] Publication {publicationId} not found");
                    return null;
                }

                // Validate rating range
                if (rating < 1 || rating > 5)
                {
                    Debug.LogWarning($"[PeerReviewService] Invalid rating {rating}. Must be between 1-5");
                    return null;
                }

                var reviewId = Guid.NewGuid().ToString();
                var review = new PeerReview
                {
                    Id = reviewId,
                    PublicationId = publicationId,
                    ReviewerId = reviewerId,
                    Rating = rating,
                    Comments = comments ?? "",
                    SubmittedDate = DateTime.Now
                };

                // Add review to the collection
                if (!_publicationReviews.ContainsKey(publicationId))
                    _publicationReviews[publicationId] = new List<PeerReview>();

                _publicationReviews[publicationId].Add(review);

                // Fire review event
                var reviewEvent = new PeerReviewEvent
                {
                    ReviewId = reviewId,
                    PublicationId = publicationId,
                    ReviewerId = reviewerId,
                    Rating = rating,
                    review = review,
                    Timestamp = DateTime.Now
                };

                _onReviewSubmitted?.Invoke(reviewEvent);

                await Task.CompletedTask;
                return reviewId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PeerReviewService] Failed to submit review: {ex.Message}");
                return null;
            }
        }

        public async Task<List<PeerReview>> GetReviewsForPublicationAsync(string publicationId)
        {
            try
            {
                if (string.IsNullOrEmpty(publicationId))
                    return new List<PeerReview>();

                if (_publicationReviews.TryGetValue(publicationId, out var reviews))
                {
                    // Return a copy of the reviews sorted by submission date
                    var sortedReviews = new List<PeerReview>(reviews);
                    sortedReviews.Sort((a, b) => b.SubmittedDate.CompareTo(a.SubmittedDate));

                    await Task.CompletedTask;
                    return sortedReviews;
                }

                await Task.CompletedTask;
                return new List<PeerReview>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PeerReviewService] Failed to get reviews: {ex.Message}");
                return new List<PeerReview>();
            }
        }
    }
}
