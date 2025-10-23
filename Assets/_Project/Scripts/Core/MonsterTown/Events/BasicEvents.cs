using System;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Basic breeding event for core systems to avoid Chimera dependency
    /// </summary>
    public class BreedingSuccessfulEvent
    {
        public MonsterInstance Parent1 { get; set; }
        public MonsterInstance Parent2 { get; set; }
        public MonsterInstance Offspring { get; set; }
        public DateTime BreedingTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Basic species config interface for core systems
    /// </summary>
    public interface ISpeciesConfig
    {
        string SpeciesId { get; }
        string SpeciesName { get; }
        float BaseHappiness { get; }
    }

    /// <summary>
    /// Simple species config implementation
    /// </summary>
    public class BasicSpeciesConfig : ISpeciesConfig
    {
        public string SpeciesId { get; set; } = "basic_species";
        public string SpeciesName { get; set; } = "Basic Monster";
        public float BaseHappiness { get; set; } = 0.7f;
    }

    /// <summary>
    /// Basic scene bootstrap interface
    /// </summary>
    public interface ISceneBootstrap
    {
        void InitializeScene();
        bool IsInitialized { get; }
    }
}