namespace Laboratory.Core.Customization
{
    /// <summary>
    /// Interface for customization systems
    /// </summary>
    public interface ICustomizationSystem
    {
        /// <summary>
        /// Sets the active state of the customization system
        /// </summary>
        void SetActive(bool active);
        
        /// <summary>
        /// Gets whether the customization system is currently active
        /// </summary>
        bool IsActive { get; }
    }
}
