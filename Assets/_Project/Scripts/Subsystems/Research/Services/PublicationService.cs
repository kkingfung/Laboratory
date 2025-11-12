using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.Research
{
    /// <summary>
    /// Publication service for creating and managing research publications.
    /// Extracted from ResearchSubsystemManager for single responsibility.
    /// </summary>
    public class PublicationService : IPublicationService
    {
        private readonly ResearchSubsystemConfig _config;
        private readonly Func<Dictionary<string, ResearchPublication>> _getPublications;
        private readonly Action<PublicationEvent> _onPublicationCreated;
        private readonly List<string> _pendingPublications = new();

        public PublicationService(ResearchSubsystemConfig config, Func<Dictionary<string, ResearchPublication>> getPublications, Action<PublicationEvent> onPublicationCreated)
        {
            _config = config;
            _getPublications = getPublications;
            _onPublicationCreated = onPublicationCreated;
        }

        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
            Debug.Log("[PublicationService] Initialized successfully");
        }

        public async Task<string> CreatePublicationAsync(string authorId, string title, string content)
        {
            try
            {
                if (string.IsNullOrEmpty(authorId) || string.IsNullOrEmpty(title))
                    return null;

                var publicationId = Guid.NewGuid().ToString();
                var publication = new ResearchPublication
                {
                    id = publicationId,
                    publicationId = publicationId,
                    title = title,
                    content = content,
                    authorId = authorId,
                    publicationType = PublicationType.Research,
                    publicationDate = DateTime.Now,
                    keywords = ExtractKeywords(content),
                    abstractText = GenerateAbstract(content),
                    coAuthors = new List<string>()
                };

                var publications = _getPublications();
                publications[publicationId] = publication;

                // Fire publication event
                var publicationEvent = new PublicationEvent
                {
                    PublicationId = publicationId,
                    Title = title,
                    AuthorId = authorId,
                    publication = publication,
                    Timestamp = DateTime.Now
                };

                _onPublicationCreated?.Invoke(publicationEvent);

                await Task.CompletedTask;
                return publicationId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PublicationService] Failed to create publication: {ex.Message}");
                return null;
            }
        }

        public async Task<List<ResearchPublication>> GetPublicationsAsync()
        {
            try
            {
                var publications = _getPublications();
                var result = new List<ResearchPublication>(publications.Values);

                // Sort by publication date, newest first
                result.Sort((a, b) => b.publicationDate.CompareTo(a.publicationDate));

                await Task.CompletedTask;
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PublicationService] Failed to get publications: {ex.Message}");
                return new List<ResearchPublication>();
            }
        }

        public void ProcessPendingPublications()
        {
            // Process any pending publications that need review or formatting
            while (_pendingPublications.Count > 0)
            {
                var publicationId = _pendingPublications[0];
                _pendingPublications.RemoveAt(0);

                var publications = _getPublications();
                if (publications.TryGetValue(publicationId, out var publication))
                {
                    // Perform any background processing on the publication
                    Debug.Log($"[PublicationService] Processed pending publication: {publication.title}");
                }
            }
        }

        private List<string> ExtractKeywords(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new List<string>();

            // Simple keyword extraction - look for common research terms
            var keywords = new List<string>();
            var commonTerms = new[] { "genetics", "mutation", "breeding", "trait", "species", "evolution", "phenotype", "genotype" };

            foreach (var term in commonTerms)
            {
                if (content.ToLower().Contains(term))
                    keywords.Add(term);
            }

            return keywords;
        }

        private string GenerateAbstract(string content)
        {
            if (string.IsNullOrEmpty(content))
                return "";

            // Generate a simple abstract from the first few sentences
            var sentences = content.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (sentences.Length > 0)
            {
                var abstractText = sentences[0].Trim();
                if (sentences.Length > 1)
                    abstractText += ". " + sentences[1].Trim();
                return abstractText + ".";
            }

            return content.Length > 200 ? content.Substring(0, 200) + "..." : content;
        }
    }
}
