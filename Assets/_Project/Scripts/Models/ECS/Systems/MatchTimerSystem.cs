using Unity.Entities;
using UnityEngine;
using Laboratory.Core;
using Laboratory.Core.DI;
using Laboratory.Infrastructure.AsyncUtils;
using Laboratory.Gameplay.Lobby;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// System responsible for managing match timer functionality during gameplay.
    /// This system updates the match timer each frame and handles timer-related
    /// operations for match duration tracking and time-based game mechanics.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class MatchTimerSystem : SystemBase
    {
        #region Fields
        
        /// <summary>
        /// Reference to the match timer component for tracking game duration and time-based events
        /// </summary>
        private MatchTimer _matchTimer = null!;
        
        /// <summary>
        /// Flag indicating whether the system is properly initialized
        /// </summary>
        private bool _isInitialized = false;
        
        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called when the system is created. Initializes the match timer dependency
        /// and sets up the system for timer operations.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            InitializeMatchTimer();
        }

        /// <summary>
        /// Called every frame during simulation. Updates the match timer with delta time
        /// and processes timer-related game logic.
        /// </summary>
        protected override void OnUpdate()
        {
            if (!_isInitialized || _matchTimer == null)
            {
                return;
            }

            UpdateMatchTimer();
        }

        /// <summary>
        /// Called when the system is destroyed. Cleans up timer resources and references.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupResources();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the match timer dependency from the service locator
        /// </summary>
        private void InitializeMatchTimer()
        {
            try
            {
                _matchTimer = GlobalServiceProvider.Resolve<MatchTimer>();
                
                if (_matchTimer == null)
                {
                    throw new System.InvalidOperationException("MatchTimer could not be resolved from ServiceLocator");
                }
                
                _isInitialized = true;
                Debug.Log("MatchTimerSystem initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize MatchTimerSystem: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Updates the match timer with the current frame's delta time
        /// </summary>
        private void UpdateMatchTimer()
        {
            try
            {
                float deltaTime = (float)SystemAPI.Time.DeltaTime;
                
                // Validate delta time to prevent timer corruption
                if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                {
                    Debug.LogWarning($"Invalid delta time detected: {deltaTime}, skipping timer update");
                    return;
                }
                
                // Update the match timer with validated delta time
                _matchTimer.Tick(deltaTime);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating match timer: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up timer resources and resets system state
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                _matchTimer = null!;
                _isInitialized = false;
                Debug.Log("MatchTimerSystem resources cleaned up");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during MatchTimerSystem cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the current match timer instance for external access
        /// </summary>
        /// <returns>The current match timer instance, or null if not initialized</returns>
        public MatchTimer GetMatchTimer()
        {
            return _isInitialized ? _matchTimer : null!;
        }

        /// <summary>
        /// Checks if the match timer system is properly initialized and ready for use
        /// </summary>
        /// <returns>True if the system is initialized and functional</returns>
        public bool IsInitialized()
        {
            return _isInitialized && _matchTimer != null;
        }

        #endregion
    }
}
