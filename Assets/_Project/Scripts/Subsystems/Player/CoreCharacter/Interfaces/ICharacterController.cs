using System;
using Laboratory.Core.DI;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Base interface for all character control components.
    /// Provides common functionality and lifecycle management.
    /// </summary>
    public interface ICharacterController : IDisposable
    {
        /// <summary>
        /// Whether this controller is currently active and processing updates
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Enables or disables the controller
        /// </summary>
        /// <param name="active">True to enable, false to disable</param>
        void SetActive(bool active);

        /// <summary>
        /// Initializes the controller with dependency injection services
        /// </summary>
        /// <param name="services">Service container for dependencies</param>
        void Initialize(IServiceContainer services);

        /// <summary>
        /// Updates the controller state. Called every frame when active.
        /// </summary>
        void UpdateController();
    }
}
