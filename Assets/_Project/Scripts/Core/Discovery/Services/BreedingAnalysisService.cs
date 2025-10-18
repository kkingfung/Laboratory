using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Discovery.Data;
using Laboratory.Core.Discovery.Types;
using Laboratory.Core.Discovery.Systems;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Core.Discovery.Services
{
    /// <summary>
    /// Service for analyzing breeding results and generating discoveries
    /// </summary>
    public class BreedingAnalysisService
    {
        private readonly DiscoveryJournalSystem discoverySystem;
        private Dictionary<string, List<GeneticDiscovery>> geneticDiscoveries = new();
        private Dictionary<string, List<BreedingSuccess>> breedingSuccesses = new();

        public BreedingAnalysisService(DiscoveryJournalSystem system)
        {
            discoverySystem = system;
        }

        public void DocumentBreedingResult(string playerId, Monster parent1, Monster parent2, Monster offspring)
        {
            if (!discoverySystem.EnableAutoDocumentation) return;

            var analysis = AnalyzeBreedingResult(parent1, parent2, offspring);
            var content = GenerateBreedingEntryContent(parent1, parent2, offspring, analysis);

            var entry = discoverySystem.AddJournalEntry(playerId, JournalEntryType.BreedingResult,
                $"Breeding: {parent1.Name} × {parent2.Name}", content,
                new BreedingData { Parent1 = parent1, Parent2 = parent2, Offspring = offspring });

            // Check for genetic discoveries
            CheckForGeneticDiscoveries(playerId, analysis, entry);

            // Update breeding success tracking
            TrackBreedingSuccess(playerId, parent1, parent2, offspring, analysis);
        }

        private BreedingAnalysis AnalyzeBreedingResult(Monster parent1, Monster parent2, Monster offspring)
        {
            var analysis = new BreedingAnalysis
            {
                InheritancePatterns = new Dictionary<string, InheritanceType>(),
                TraitComparisons = new Dictionary<string, TraitComparison>(),
                NotableObservations = new List<string>(),
                GeneticNovelty = CalculateGeneticNovelty(parent1, parent2, offspring)
            };

            // Analyze stat inheritance
            var statNames = new[] { "Strength", "Agility", "Vitality", "Intelligence", "Speed", "Social" };
            var parentStats = new[] { parent1.Stats.strength, parent1.Stats.agility, parent1.Stats.vitality,
                                    parent1.Stats.intelligence, parent1.Stats.speed, parent1.Stats.social };
            var parent2Stats = new[] { parent2.Stats.strength, parent2.Stats.agility, parent2.Stats.vitality,
                                     parent2.Stats.intelligence, parent2.Stats.speed, parent2.Stats.social };
            var offspringStats = new[] { offspring.Stats.strength, offspring.Stats.agility, offspring.Stats.vitality,
                                       offspring.Stats.intelligence, offspring.Stats.speed, offspring.Stats.social };

            for (int i = 0; i < statNames.Length; i++)
            {
                var comparison = new TraitComparison
                {
                    Parent1Value = parentStats[i],
                    Parent2Value = parent2Stats[i],
                    OffspringValue = offspringStats[i],
                    InheritanceType = DetermineInheritanceType(parentStats[i], parent2Stats[i], offspringStats[i])
                };

                analysis.TraitComparisons[statNames[i]] = comparison;
                analysis.InheritancePatterns[statNames[i]] = comparison.InheritanceType;
            }

            // Generate observations
            analysis.NotableObservations = GenerateBreedingObservations(analysis);

            return analysis;
        }

        private InheritanceType DetermineInheritanceType(float parent1Stat, float parent2Stat, float offspringStat)
        {
            var midpoint = (parent1Stat + parent2Stat) / 2f;
            var tolerance = Math.Abs(parent1Stat - parent2Stat) * 0.1f;

            if (Math.Abs(offspringStat - midpoint) <= tolerance)
                return InheritanceType.Blended;
            else if (Math.Abs(offspringStat - parent1Stat) < Math.Abs(offspringStat - parent2Stat))
                return InheritanceType.DominantFromParent1;
            else
                return InheritanceType.DominantFromParent2;
        }

        private float CalculateGeneticNovelty(Monster parent1, Monster parent2, Monster offspring)
        {
            // Calculate how different the offspring is from both parents
            var statDifferences = new[]
            {
                Math.Abs(offspring.Stats.strength - (parent1.Stats.strength + parent2.Stats.strength) / 2f),
                Math.Abs(offspring.Stats.agility - (parent1.Stats.agility + parent2.Stats.agility) / 2f),
                Math.Abs(offspring.Stats.vitality - (parent1.Stats.vitality + parent2.Stats.vitality) / 2f),
                Math.Abs(offspring.Stats.intelligence - (parent1.Stats.intelligence + parent2.Stats.intelligence) / 2f),
                Math.Abs(offspring.Stats.speed - (parent1.Stats.speed + parent2.Stats.speed) / 2f),
                Math.Abs(offspring.Stats.social - (parent1.Stats.social + parent2.Stats.social) / 2f)
            };

            float noveltyScore = statDifferences.Average() / 100f; // Normalize to 0-1 range
            return Mathf.Clamp01(noveltyScore);
        }

        private List<string> GenerateBreedingObservations(BreedingAnalysis analysis)
        {
            var observations = new List<string>();

            // Check for interesting inheritance patterns
            var dominantTraits = analysis.InheritancePatterns.Where(kvp =>
                kvp.Value == InheritanceType.DominantFromParent1 ||
                kvp.Value == InheritanceType.DominantFromParent2).ToList();

            if (dominantTraits.Count >= 3)
            {
                observations.Add("Strong dominant inheritance patterns observed in multiple traits");
            }

            var blendedTraits = analysis.InheritancePatterns.Where(kvp =>
                kvp.Value == InheritanceType.Blended).Count();

            if (blendedTraits >= 4)
            {
                observations.Add("Most traits show blended inheritance from both parents");
            }

            // Check for high genetic novelty
            if (analysis.GeneticNovelty > 0.3f)
            {
                observations.Add("Offspring shows significant genetic variation from expected patterns");
            }

            return observations;
        }

        private void CheckForGeneticDiscoveries(string playerId, BreedingAnalysis analysis, JournalEntry entry)
        {
            // Check for discoveries based on breeding analysis
            if (analysis.GeneticNovelty > 0.4f)
            {
                var discovery = new GeneticDiscovery
                {
                    DiscoveryId = Guid.NewGuid().ToString(),
                    DiscoveryName = "Unexpected Genetic Variation",
                    Description = "Observed significant genetic variation beyond normal inheritance patterns",
                    DiscoveryType = DiscoveryType.InheritancePattern,
                    Significance = DiscoverySignificance.Notable,
                    RelatedJournalEntry = entry.EntryId,
                    DiscoveryDate = DateTime.UtcNow
                };

                discoverySystem.DocumentGeneticDiscovery(playerId, discovery);
            }

            // Check for rare trait combinations
            var exceptionalTraits = analysis.TraitComparisons.Where(kvp =>
                kvp.Value.OffspringValue > Math.Max(kvp.Value.Parent1Value, kvp.Value.Parent2Value) + 10f);

            if (exceptionalTraits.Count() >= 2)
            {
                var discovery = new GeneticDiscovery
                {
                    DiscoveryId = Guid.NewGuid().ToString(),
                    DiscoveryName = "Hybrid Vigor Expression",
                    Description = "Offspring exceeded both parents in multiple traits, showing hybrid vigor",
                    DiscoveryType = DiscoveryType.TraitExpression,
                    Significance = DiscoverySignificance.Significant,
                    RelatedJournalEntry = entry.EntryId,
                    DiscoveryDate = DateTime.UtcNow
                };

                discoverySystem.DocumentGeneticDiscovery(playerId, discovery);
            }
        }

        private void TrackBreedingSuccess(string playerId, Monster parent1, Monster parent2, Monster offspring, BreedingAnalysis analysis)
        {
            var success = new BreedingSuccess
            {
                SuccessId = Guid.NewGuid().ToString(),
                Parent1Species = parent1.Name,
                Parent2Species = parent2.Name,
                OffspringQualities = CalculateOffspringQualities(offspring),
                GeneticNovelty = analysis.GeneticNovelty,
                SuccessDate = DateTime.UtcNow
            };

            if (!breedingSuccesses.ContainsKey(playerId))
            {
                breedingSuccesses[playerId] = new List<BreedingSuccess>();
            }

            breedingSuccesses[playerId].Add(success);
        }

        private float CalculateOffspringQualities(Monster offspring)
        {
            // Calculate overall quality score based on stats
            var avgStats = (offspring.Stats.strength + offspring.Stats.agility + offspring.Stats.vitality +
                          offspring.Stats.intelligence + offspring.Stats.speed + offspring.Stats.social) / 6f;
            return avgStats / 100f; // Normalize to 0-1
        }

        private string GenerateBreedingEntryContent(Monster parent1, Monster parent2, Monster offspring, BreedingAnalysis analysis)
        {
            var content = $"Breeding Experiment: {parent1.Name} × {parent2.Name}\n\n";
            content += $"Result: {offspring.Name}\n\n";
            content += "Trait Analysis:\n";

            foreach (var trait in analysis.TraitComparisons)
            {
                content += $"• {trait.Key}: {trait.Value.Parent1Value:F1} + {trait.Value.Parent2Value:F1} → {trait.Value.OffspringValue:F1} ({trait.Value.InheritanceType})\n";
            }

            if (analysis.NotableObservations.Any())
            {
                content += "\nNotable Observations:\n";
                foreach (var observation in analysis.NotableObservations)
                {
                    content += $"• {observation}\n";
                }
            }

            content += $"\nGenetic Novelty Score: {analysis.GeneticNovelty:P1}";

            return content;
        }
    }
}