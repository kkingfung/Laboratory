namespace Laboratory.Shared.Interfaces
{
    /// <summary>
    /// Interface for creature components that can be reset/initialized
    /// This allows Core assembly to work with creature components without direct dependencies
    /// </summary>
    public interface ICreatureComponent
    {
        void ResetToDefaults();
        void InitializeFromPool();
        bool IsInitialized { get; }
    }

    /// <summary>
    /// Interface for poolable objects
    /// </summary>
    public interface IPoolable
    {
        void OnReturnToPool();
        void OnGetFromPool();
        bool IsAvailableForPool { get; }
    }
}