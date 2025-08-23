// This file has been deprecated and replaced with Laboratory.Core.Health.Components.NetworkHealthComponent
// 
// Migration Guide:
// 1. Replace NetworkHealth component with NetworkHealthComponent
// 2. Update any direct references to use IHealthComponent interface
// 3. Use DamageManager.Instance.ApplyDamage() for consistent damage application
// 4. Subscribe to events via UnifiedEventBus instead of direct component references
//
// The new unified system provides:
// - Better separation of concerns
// - Network synchronization with server authority
// - Event-driven architecture via UnifiedEventBus
// - Consistent API across all health components
// - Integration with centralized DamageManager
//
// For migration assistance, see:
// Assets/_Project/Scripts/MIGRATION_GUIDE.md

using Laboratory.Core.Health.Components;

namespace Laboratory.Infrastructure.Networking
{
    [System.Obsolete("Use Laboratory.Core.Health.Components.NetworkHealthComponent instead. See MIGRATION_GUIDE.md for details.")]
    public class NetworkHealth : NetworkHealthComponent
    {
        // This class now inherits from the new unified NetworkHealthComponent
        // All functionality has been moved to the new implementation
        // This wrapper is provided for backward compatibility during migration
    }
}
