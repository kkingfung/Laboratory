using Unity.Entities;
using UnityEngine;

namespace Laboratory.Chimera.Activities.Puzzle
{
    /// <summary>
    /// ECS system that registers and manages puzzle activity
    /// Integrates with the core ActivitySystem
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ActivitySystem))]
    public partial class PuzzleActivitySystem : SystemBase
    {
        private PuzzleActivity _puzzleActivity;
        private PuzzleConfig _puzzleConfig;
        private bool _isInitialized;

        protected override void OnCreate()
        {
            // Load puzzle configuration
            _puzzleConfig = Resources.Load<PuzzleConfig>("Configs/Activities/PuzzleConfig");

            if (_puzzleConfig == null)
            {
                Debug.LogWarning("PuzzleConfig not found at Resources/Configs/Activities/PuzzleConfig. " +
                                "Puzzle activity will use default settings.");
                return;
            }

            // Create puzzle activity implementation
            _puzzleActivity = new PuzzleActivity(_puzzleConfig);

            Debug.Log($"Puzzle Activity System initialized: {_puzzleConfig.activityName}");
            _isInitialized = true;
        }

        protected override void OnStartRunning()
        {
            if (!_isInitialized || _puzzleActivity == null)
                return;

            // Register puzzle activity with the core activity system
            var activitySystem = World.GetExistingSystemManaged<ActivitySystem>();
            if (activitySystem != null)
            {
                activitySystem.RegisterActivity(ActivityType.Puzzle, _puzzleActivity);
                Debug.Log("Puzzle activity registered with ActivitySystem");
            }
            else
            {
                Debug.LogError("ActivitySystem not found. Puzzle activity cannot be registered.");
            }
        }

        protected override void OnUpdate()
        {
            // This system primarily handles initialization and registration
            // The core ActivitySystem handles the actual activity execution
            // Future: Could add puzzle-specific UI, hints system, or collaborative features here
        }

        /// <summary>
        /// Gets the puzzle configuration (for external systems to query)
        /// </summary>
        public PuzzleConfig GetConfig()
        {
            return _puzzleConfig;
        }

        /// <summary>
        /// Creates a puzzle activity request entity
        /// </summary>
        public Entity CreatePuzzleRequest(Entity monsterEntity, ActivityDifficulty difficulty)
        {
            var requestEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(requestEntity, new StartActivityRequest
            {
                monsterEntity = monsterEntity,
                activityType = ActivityType.Puzzle,
                difficulty = difficulty,
                requestTime = (float)SystemAPI.Time.ElapsedTime
            });

            Debug.Log($"Created puzzle request for monster {monsterEntity.Index} at difficulty {difficulty}");

            return requestEntity;
        }

        /// <summary>
        /// Creates a collaborative puzzle request (multi-monster)
        /// </summary>
        public Entity CreateCollaborativePuzzleRequest(Entity[] monsterEntities, ActivityDifficulty difficulty)
        {
            if (!_puzzleConfig.enableCollaboration)
            {
                Debug.LogWarning("Collaborative puzzles are disabled in config");
                return Entity.Null;
            }

            if (monsterEntities == null || monsterEntities.Length == 0)
            {
                Debug.LogWarning("No monsters provided for collaborative puzzle");
                return Entity.Null;
            }

            // For now, create request for first monster (full collaboration system is future work)
            return CreatePuzzleRequest(monsterEntities[0], difficulty);
        }
    }
}
