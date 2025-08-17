using Unity.Entities;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Singleton component used to control whether gameplay systems should run.
    /// ISystem systems can query this component to determine if they should execute their logic.
    /// This is the recommended approach for controlling ISystem behavior in Unity Entities 1.0+
    /// since ISystem systems cannot be dynamically enabled/disabled like SystemBase systems.
    /// </summary>
    public struct GameStateControlComponent : IComponentData
    {
        /// <summary>
        /// True if gameplay systems should run, false if they should skip execution
        /// </summary>
        public bool GameplayEnabled;
    }
}
