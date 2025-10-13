using System;
using System.Threading.Tasks;
using Laboratory.Core.Infrastructure;

namespace Laboratory.Core.GameModes
{
    /// <summary>
    /// Enumeration of supported game genres in Project Chimera.
    /// Each genre represents a different way to interact with the genetic breeding system.
    /// </summary>
    public enum GameGenre
    {
        Exploration,        // Default breeding/exploration mode
        Strategy,          // RTS-style civilization management
        Racing,            // Creature racing with genetic optimization
        Puzzle,            // Genetic pattern matching and breeding puzzles
        TowerDefense,      // Evolving defenders against waves
        BattleRoyale,      // Survival with genetic adaptation
        CityBuilder,       // Population genetics and ecosystem management
        Detective,         // Genetic investigation and mysteries
        Economics,         // Genetic trait trading and speculation
        Sports             // Competitive genetics olympics
    }

    /// <summary>
    /// Configuration data for genre-specific gameplay parameters.
    /// </summary>
    [Serializable]
    public class GenreConfig
    {
        public GameGenre genre;
        public string configName;
        public float difficultyModifier = 1.0f;
        public bool allowGeneticModification = true;
        public int maxCreatures = 10;
        public float timeScale = 1.0f;

        // Genre-specific parameters can be added here
        public object genreSpecificData;
    }

    /// <summary>
    /// Interface for subsystem managers that support genre-specific gameplay modes.
    /// Extends the base ISubsystemManager to add genre switching capabilities.
    /// </summary>
    public interface IGenreSubsystemManager : ISubsystemManager
    {
        /// <summary>
        /// The primary genre this subsystem manager supports
        /// </summary>
        GameGenre SupportedGenre { get; }

        /// <summary>
        /// Determines if this manager can activate for the specified genre
        /// </summary>
        bool CanActivateForGenre(GameGenre genre);

        /// <summary>
        /// Activates genre-specific functionality with the provided configuration
        /// </summary>
        Task ActivateGenreMode(GameGenre genre, GenreConfig config);

        /// <summary>
        /// Deactivates genre-specific functionality and returns to default state
        /// </summary>
        Task DeactivateGenreMode();

        /// <summary>
        /// Gets the current active genre (None if not in genre mode)
        /// </summary>
        GameGenre CurrentActiveGenre { get; }

        /// <summary>
        /// Event fired when genre mode activation/deactivation occurs
        /// </summary>
        event Action<GameGenre, bool> GenreModeChanged;
    }

    /// <summary>
    /// Base implementation for genre subsystem managers with common functionality
    /// </summary>
    public abstract class GenreSubsystemManagerBase : IGenreSubsystemManager
    {
        public abstract string SubsystemName { get; }
        public abstract bool IsInitialized { get; }
        public abstract float InitializationProgress { get; }
        public abstract GameGenre SupportedGenre { get; }

        public GameGenre CurrentActiveGenre { get; private set; } = GameGenre.Exploration;

        public event Action<GameGenre, bool> GenreModeChanged;

        public virtual bool CanActivateForGenre(GameGenre genre)
        {
            return genre == SupportedGenre || genre == GameGenre.Exploration;
        }

        public async Task ActivateGenreMode(GameGenre genre, GenreConfig config)
        {
            if (!CanActivateForGenre(genre))
            {
                throw new InvalidOperationException($"Cannot activate {genre} mode on {GetType().Name}");
            }

            await OnActivateGenreMode(genre, config);
            CurrentActiveGenre = genre;
            GenreModeChanged?.Invoke(genre, true);
        }

        public async Task DeactivateGenreMode()
        {
            var previousGenre = CurrentActiveGenre;
            await OnDeactivateGenreMode(previousGenre);
            CurrentActiveGenre = GameGenre.Exploration;
            GenreModeChanged?.Invoke(previousGenre, false);
        }

        /// <summary>
        /// Override this method to implement genre-specific activation logic
        /// </summary>
        protected abstract Task OnActivateGenreMode(GameGenre genre, GenreConfig config);

        /// <summary>
        /// Override this method to implement genre-specific deactivation logic
        /// </summary>
        protected abstract Task OnDeactivateGenreMode(GameGenre previousGenre);
    }
}