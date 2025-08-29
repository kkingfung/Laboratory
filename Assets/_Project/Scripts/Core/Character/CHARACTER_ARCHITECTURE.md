# 🎮 Character & Movement Subsystem Documentation

## Overview

The Character & Movement subsystem provides comprehensive character control, aiming, targeting, and climbing functionality with modern Unity integration. It implements clean architecture patterns with dependency injection, event-driven communication, and configurable behavior.

## 📋 Table of Contents

- [Architecture Overview](#architecture-overview)
- [Core Components](#core-components)
- [Character Controllers](#character-controllers)
- [Configuration System](#configuration-system)
- [Event Integration](#event-integration)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)
- [Testing](#testing)

## Architecture Overview

### 🏗️ Design Principles

- **Unified Controllers**: Single controllers handle multiple related systems (e.g., head, chest, spine aiming)
- **Interface-Based Design**: All controllers implement standard interfaces for consistency
- **Event-Driven Communication**: Controllers communicate through events, not direct references
- **Configuration-Driven**: Behavior controlled through ScriptableObject configurations
- **Service Integration**: Full dependency injection and service container integration

### 📁 File Structure

```
Core/Character/
├── Interfaces/
│   ├── ICharacterController.cs          # Base controller interface
│   ├── IAimController.cs                # Aiming system interface
│   ├── ITargetSelector.cs               # Target selection interface
│   └── ICharacterStateManager.cs        # State management interface
├── Controllers/
│   ├── UnifiedAimController.cs          # Consolidated aiming controller
│   └── ClimbingController.cs            # Climbing system controller
├── Systems/
│   ├── AdvancedTargetSelector.cs        # Enhanced target selection
│   └── CharacterCustomizationSystem.cs  # Character customization
├── Events/
│   └── CharacterEvents.cs               # Character-related events
├── Configuration/
│   ├── CharacterAimSettings.cs          # Aiming behavior settings
│   └── CustomizationSettings.cs         # Customization system settings
└── Tests/
    └── UnifiedAimControllerTests.cs     # Unit tests
```

## Core Components

### 1. Base Interfaces

#### ICharacterController
Base interface for all character control components.

```csharp
public interface ICharacterController : IDisposable
{
    bool IsActive { get; }
    void SetActive(bool active);
    void Initialize(IServiceContainer services);
    void UpdateController();
}
```

#### IAimController
Interface for character aiming systems.

```csharp
public interface IAimController : ICharacterController
{
    Transform CurrentTarget { get; }
    bool IsAiming { get; }
    float AimWeight { get; }
    float MaxAimDistance { get; set; }
    
    void SetTarget(Transform target);
    void ClearTarget();
    void SetAimWeight(float weight);
    void SetAutoTargeting(bool enabled);
    bool IsValidTarget(Transform target);
}
```

## Character Controllers

### 1. UnifiedAimController

Consolidates head, chest, spine, and shoulder aiming into a single controller.

**Key Features:**
- Animation Rigging constraint management
- Automatic IK fallback when rigging unavailable
- Smooth weight interpolation
- Target validation and distance checking
- Event publishing for state changes

**Usage:**
```csharp
// Basic setup
var aimController = GetComponent<UnifiedAimController>();
aimController.Initialize(GlobalServiceProvider.Instance);

// Set target
aimController.SetTarget(enemyTransform);

// Adjust aim weight
aimController.SetAimWeight(0.8f);

// Enable auto-targeting
aimController.SetAutoTargeting(true);
```

### 2. ClimbingController

Handles wall climbing, ledge mantling, and wall jumping.

**Key Features:**
- Multi-state climbing system (Grounded, Climbing, Mantling, WallJumping)
- Stamina management with drain and recovery
- Physics-based movement
- Camera tilt adjustment
- Surface detection and validation

**Usage:**
```csharp
// Basic setup
var climbController = GetComponent<ClimbingController>();
climbController.Initialize(GlobalServiceProvider.Instance);

// Check if can climb
if (climbController.CanStartClimbing())
{
    Debug.Log("Ready to climb!");
}

// Monitor state changes
climbController.OnClimbStateChanged += (state) => 
{
    Debug.Log($"Climb state: {state}");
};
```

### 3. AdvancedTargetSelector

Enhanced target selection with multiple detection modes and intelligent scoring.

**Key Features:**
- Proximity and raycast detection modes
- Intelligent target scoring system
- Line of sight validation
- Performance-optimized updates
- Event notifications for target changes

**Usage:**
```csharp
// Setup
var targetSelector = GetComponent<AdvancedTargetSelector>();
targetSelector.Initialize(GlobalServiceProvider.Instance);

// Configure detection
targetSelector.DetectionRadius = 15f;
targetSelector.TargetLayers = enemyLayers;

// Listen for target changes
targetSelector.OnTargetChanged += OnNewTargetSelected;
targetSelector.OnTargetDetected += OnTargetSpotted;
```

## Configuration System

### CharacterAimSettings

ScriptableObject for configuring aiming behavior.

```csharp
[CreateAssetMenu(menuName = "Laboratory/Character/Aim Settings")]
public class CharacterAimSettings : ScriptableObject
{
    [Header("Targeting")]
    public float maxAimDistance = 15f;
    public LayerMask targetLayers = -1;
    public float aimSpeed = 5f;

    [Header("Constraint Weights")]
    public float headWeight = 0.8f;
    public float chestWeight = 0.5f;
    public float shoulderWeight = 0.3f;
    
    // ... additional settings
}
```

### CustomizationSettings

Configuration for character customization system.

```csharp
[CreateAssetMenu(menuName = "Laboratory/Character/Customization Settings")]
public class CustomizationSettings : ScriptableObject
{
    public string assetBasePath = "Character/Customization";
    public bool useAddressables = true;
    public string[] availableHairIds;
    public string[] availableClothingIds;
    public Color[] availableHairColors;
    
    // ... additional settings
}
```

## Event Integration

### Character Events

All character systems publish events through the unified event bus.

```csharp
// Aiming events
public class CharacterAimStartedEvent : CharacterEventBase
{
    public Transform Target { get; }
}

public class CharacterAimStoppedEvent : CharacterEventBase
{
    public Transform PreviousTarget { get; }
}

// State change events
public class CharacterStateChangedEvent : CharacterEventBase
{
    public CharacterState PreviousState { get; }
    public CharacterState NewState { get; }
    public float TimeInPreviousState { get; }
}

// Climbing events
public class CharacterClimbStateChangedEvent : CharacterEventBase
{
    public ClimbState ClimbState { get; }
    public bool IsClimbing { get; }
}
```

### Event Usage

```csharp
// Subscribe to character events
eventBus.Subscribe<CharacterAimStartedEvent>(OnAimStarted);
eventBus.Subscribe<CharacterClimbStateChangedEvent>(OnClimbStateChanged);

// Events are automatically published by controllers
private void OnAimStarted(CharacterAimStartedEvent evt)
{
    Debug.Log($"{evt.Character.name} started aiming at {evt.Target.name}");
}
```

## Usage Examples

### 1. Setting Up Complete Character Control

```csharp
public class PlayerController : MonoBehaviour
{
    private UnifiedAimController _aimController;
    private ClimbingController _climbController;
    private AdvancedTargetSelector _targetSelector;

    private void Start()
    {
        // Get components
        _aimController = GetComponent<UnifiedAimController>();
        _climbController = GetComponent<ClimbingController>();
        _targetSelector = GetComponent<AdvancedTargetSelector>();

        // Initialize with services
        var services = GlobalServiceProvider.Instance;
        _aimController.Initialize(services);
        _climbController.Initialize(services);
        _targetSelector.Initialize(services);

        // Setup auto-targeting
        _aimController.SetAutoTargeting(true);
        
        // Configure target selector for aim controller
        _targetSelector.OnTargetChanged += _aimController.SetTarget;
    }

    private void Update()
    {
        // Controllers handle their own updates automatically
        // Additional game-specific logic here
    }
}
```

### 2. Custom Target Scoring

```csharp
public class CustomTargetSelector : AdvancedTargetSelector
{
    protected override float CalculateTargetScore(Transform target)
    {
        float baseScore = base.CalculateTargetScore(target);
        
        // Add custom scoring logic
        var enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Prioritize low-health enemies
            float healthBonus = (1f - enemy.HealthPercentage) * 0.5f;
            baseScore += healthBonus;
        }
        
        return baseScore;
    }
}
```

### 3. Event-Driven UI Updates

```csharp
public class CharacterHUD : MonoBehaviour
{
    [SerializeField] private Slider _staminaBar;
    [SerializeField] private Image _crosshair;
    
    private ClimbingController _climbController;
    private UnifiedAimController _aimController;

    private void Start()
    {
        _climbController = FindObjectOfType<ClimbingController>();
        _aimController = FindObjectOfType<UnifiedAimController>();
        
        // Subscribe to events
        var eventBus = GlobalServiceProvider.Resolve<IEventBus>();
        eventBus.Subscribe<CharacterAimStartedEvent>(OnAimStarted);
        eventBus.Subscribe<CharacterClimbStateChangedEvent>(OnClimbStateChanged);
    }

    private void Update()
    {
        // Update stamina bar
        if (_climbController != null)
        {
            _staminaBar.value = _climbController.Stamina / 100f;
        }
    }

    private void OnAimStarted(CharacterAimStartedEvent evt)
    {
        _crosshair.color = Color.red; // Show targeting
    }
    
    private void OnClimbStateChanged(CharacterClimbStateChangedEvent evt)
    {
        _staminaBar.gameObject.SetActive(evt.IsClimbing);
    }
}
```

## Best Practices

### 🎯 Controller Design

- **Single Responsibility**: Each controller handles one primary concern
- **Interface Compliance**: Always implement the appropriate interfaces
- **Event Publishing**: Publish events for all significant state changes
- **Configuration**: Use ScriptableObjects for tweakable parameters
- **Error Handling**: Gracefully handle missing components and null references

### ⚡ Performance

- **Update Throttling**: Limit expensive operations to fixed intervals
- **Object Pooling**: Cache frequently created objects
- **Batch Operations**: Group related operations together
- **Early Exits**: Return early from expensive checks when possible

```csharp
// Good: Throttled updates
private void Update()
{
    if (Time.time - _lastUpdateTime < _updateInterval) return;
    _lastUpdateTime = Time.time;
    
    UpdateExpensiveLogic();
}

// Good: Early exit
public bool ValidateTarget(Transform target)
{
    if (target == null || target == transform) return false;
    
    float distance = Vector3.Distance(transform.position, target.position);
    if (distance > _maxDistance) return false;
    
    // Additional expensive checks only if basic checks pass...
}
```

### 🔧 Configuration

- **Editor Validation**: Use OnValidate() to clamp values
- **Default Values**: Provide sensible defaults for all settings
- **Runtime Modification**: Support runtime parameter changes
- **Documentation**: Add tooltips to all serialized fields

### 🧪 Testing

- **Unit Tests**: Test individual controller methods
- **Integration Tests**: Test controller interactions
- **Mock Services**: Use mock service containers for isolation
- **Edge Cases**: Test null inputs, extreme values, and error conditions

## Testing

### Unit Test Example

```csharp
[Test]
public void SetTarget_WithValidTarget_UpdatesCurrentTarget()
{
    // Arrange
    var target = new GameObject("Target").transform;
    target.position = Vector3.forward * 5f;

    // Act
    _aimController.SetTarget(target);

    // Assert
    Assert.AreEqual(target, _aimController.CurrentTarget);
    Assert.IsTrue(_aimController.IsAiming);

    // Cleanup
    Object.DestroyImmediate(target.gameObject);
}
```

### Integration Test Pattern

```csharp
[UnityTest]
public IEnumerator AimController_WithTargetSelector_AutoTargetsCorrectly()
{
    // Setup complete character with both components
    var character = SetupTestCharacter();
    var aimController = character.GetComponent<UnifiedAimController>();
    var targetSelector = character.GetComponent<AdvancedTargetSelector>();
    
    // Create test targets
    var targets = CreateTestTargets();
    
    // Wait for target detection
    yield return new WaitForSeconds(0.5f);
    
    // Verify auto-targeting worked
    Assert.IsNotNull(aimController.CurrentTarget);
    Assert.Contains(aimController.CurrentTarget, targets);
}
```

## Migration from Legacy Systems

### Replacing Old Components

1. **Backup existing prefabs** before migration
2. **Replace old aim controllers** with UnifiedAimController
3. **Update references** to use new interfaces
4. **Migrate settings** to new ScriptableObject configurations
5. **Test thoroughly** in representative scenarios

### Legacy Compatibility

The new system provides compatibility helpers:

```csharp
// Legacy support methods
public void LegacySetLookTarget(Transform target) => SetTarget(target);
public bool LegacyIsAiming() => IsAiming;
public void LegacySetAimEnabled(bool enabled) => SetActive(enabled);
```

---

*This documentation covers the improved Character & Movement subsystem for the Laboratory Unity project. The system provides robust, extensible, and well-tested character control with modern Unity integration.*