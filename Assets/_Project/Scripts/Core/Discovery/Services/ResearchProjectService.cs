using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Discovery.Data;
using Laboratory.Core.Discovery.Types;
using Laboratory.Core.Discovery.Systems;

namespace Laboratory.Core.Discovery.Services
{
    /// <summary>
    /// Service for managing research projects and progress
    /// </summary>
    public class ResearchProjectService
    {
        private readonly DiscoveryJournalSystem discoverySystem;
        private Dictionary<string, List<ResearchProject>> activeProjects = new();
        private Dictionary<string, List<ResearchProject>> completedProjects = new();

        public ResearchProjectService(DiscoveryJournalSystem system)
        {
            discoverySystem = system;
        }

        public void Initialize()
        {
            activeProjects["LocalPlayer"] = new List<ResearchProject>();
            completedProjects["LocalPlayer"] = new List<ResearchProject>();

            CreateDefaultResearchProjects();
        }

        public bool StartResearchProject(string playerId, string projectId)
        {
            if (!discoverySystem.EnableResearchProjects) return false;

            var existingProject = GetAvailableResearchProject(projectId);
            if (existingProject == null) return false;

            var playerProjects = activeProjects[playerId];
            if (playerProjects.Count >= discoverySystem.MaxActiveResearchProjects)
            {
                Debug.LogWarning("Cannot start research project: Maximum active projects reached");
                return false;
            }

            var project = new ResearchProject
            {
                ProjectId = existingProject.ProjectId,
                Title = existingProject.Title,
                Description = existingProject.Description,
                ObjectiveType = existingProject.ObjectiveType,
                Status = ResearchStatus.InProgress,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddSeconds(discoverySystem.ResearchProjectDuration),
                CurrentProgress = 0,
                RequiredBreedings = existingProject.RequiredBreedings,
                RequiredSamples = existingProject.RequiredSamples,
                RequiredTests = existingProject.RequiredTests,
                Findings = new List<string>()
            };

            playerProjects.Add(project);
            Debug.Log($"🔬 Started research project: {project.Title}");
            return true;
        }

        public void UpdateResearchProgress(string playerId, ResearchObjectiveType objectiveType, object data = null)
        {
            var playerProjects = activeProjects[playerId];
            var relevantProjects = playerProjects.Where(p =>
                p.ObjectiveType == objectiveType && p.Status == ResearchStatus.InProgress).ToList();

            foreach (var project in relevantProjects)
            {
                UpdateProjectProgress(playerId, project, data);
            }
        }

        private void UpdateProjectProgress(string playerId, ResearchProject project, object data)
        {
            switch (project.ObjectiveType)
            {
                case ResearchObjectiveType.BreedingAnalysis:
                    project.CurrentProgress++;
                    if (project.CurrentProgress >= project.RequiredBreedings)
                    {
                        CompleteResearchProject(playerId, project);
                    }
                    break;

                case ResearchObjectiveType.TraitAnalysis:
                    project.CurrentProgress++;
                    if (project.CurrentProgress >= project.RequiredSamples)
                    {
                        CompleteResearchProject(playerId, project);
                    }
                    break;

                case ResearchObjectiveType.PerformanceStudy:
                    project.CurrentProgress++;
                    if (project.CurrentProgress >= project.RequiredTests)
                    {
                        CompleteResearchProject(playerId, project);
                    }
                    break;

                case ResearchObjectiveType.PopulationStudy:
                    // This would track population metrics over time
                    project.CurrentProgress++;
                    break;
            }

            // Check if time-based completion
            if (DateTime.UtcNow >= project.EndDate && project.Status == ResearchStatus.InProgress)
            {
                CompleteResearchProject(playerId, project);
            }
        }

        private void CompleteResearchProject(string playerId, ResearchProject project)
        {
            project.Status = ResearchStatus.Completed;
            project.CompletionDate = DateTime.UtcNow;

            // Generate research findings
            project.Findings = GenerateResearchFindings(project);

            // Move from active to completed
            activeProjects[playerId].Remove(project);
            completedProjects[playerId].Add(project);

            // Create journal entry for completion
            var content = GenerateResearchCompletionContent(project);
            discoverySystem.AddJournalEntry(playerId, JournalEntryType.ResearchCompletion,
                $"Research Complete: {project.Title}", content, project);

            discoverySystem.TriggerResearchProjectCompleted(project);
            Debug.Log($"✅ Research project completed: {project.Title}");
        }

        private List<string> GenerateResearchFindings(ResearchProject project)
        {
            var findings = new List<string>();

            switch (project.ObjectiveType)
            {
                case ResearchObjectiveType.BreedingAnalysis:
                    findings.Add("Documented inheritance patterns across multiple breeding pairs");
                    findings.Add("Identified correlation between parent traits and offspring outcomes");
                    if (project.CurrentProgress > project.RequiredBreedings * 1.5f)
                    {
                        findings.Add("Exceeded research targets, uncovering additional trait correlations");
                    }
                    break;

                case ResearchObjectiveType.TraitAnalysis:
                    findings.Add("Analyzed trait distribution patterns across specimen population");
                    findings.Add("Identified potential genetic markers for enhanced traits");
                    break;

                case ResearchObjectiveType.PerformanceStudy:
                    findings.Add("Correlated genetic traits with performance in various activities");
                    findings.Add("Developed preliminary performance prediction models");
                    break;

                case ResearchObjectiveType.PopulationStudy:
                    findings.Add("Observed population genetic trends over study period");
                    findings.Add("Documented genetic diversity patterns within breeding population");
                    break;
            }

            return findings;
        }

        private string GenerateResearchCompletionContent(ResearchProject project)
        {
            var content = $"Research Project Completed: {project.Title}\n\n";
            content += $"Project Duration: {(project.CompletionDate - project.StartDate).Days} days\n";
            content += $"Objective: {project.ObjectiveType}\n";
            content += $"Progress: {project.CurrentProgress} completed\n\n";

            content += "Key Findings:\n";
            foreach (var finding in project.Findings)
            {
                content += $"• {finding}\n";
            }

            content += "\nThis research contributes to our scientific understanding of monster genetics and behavior.";

            return content;
        }

        public List<ResearchProject> GetActiveResearchProjects(string playerId)
        {
            return activeProjects.TryGetValue(playerId, out var projects)
                ? new List<ResearchProject>(projects)
                : new List<ResearchProject>();
        }

        private ResearchProject GetAvailableResearchProject(string projectId)
        {
            // This would typically come from a database or configuration
            var availableProjects = new List<ResearchProject>
            {
                new ResearchProject
                {
                    ProjectId = "breeding_inheritance_study",
                    Title = "Breeding Inheritance Study",
                    Description = "Study inheritance patterns across multiple breeding generations",
                    ObjectiveType = ResearchObjectiveType.BreedingAnalysis,
                    RequiredBreedings = 5,
                    Status = ResearchStatus.Available
                },
                new ResearchProject
                {
                    ProjectId = "trait_correlation_analysis",
                    Title = "Trait Correlation Analysis",
                    Description = "Analyze correlations between different genetic traits",
                    ObjectiveType = ResearchObjectiveType.TraitAnalysis,
                    RequiredSamples = 10,
                    Status = ResearchStatus.Available
                },
                new ResearchProject
                {
                    ProjectId = "performance_genetics_study",
                    Title = "Performance Genetics Study",
                    Description = "Study correlation between genetics and activity performance",
                    ObjectiveType = ResearchObjectiveType.PerformanceStudy,
                    RequiredTests = 8,
                    Status = ResearchStatus.Available
                }
            };

            return availableProjects.FirstOrDefault(p => p.ProjectId == projectId);
        }

        private void CreateDefaultResearchProjects()
        {
            // Initialize with some default research projects
            Debug.Log("🔬 Research project service initialized with default projects");
        }
    }
}