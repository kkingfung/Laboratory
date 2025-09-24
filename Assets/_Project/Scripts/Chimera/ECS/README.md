# 🐉 Project Chimera - ECS Integration

## Overview

This folder contains the **Unity ECS integration** for Project Chimera's creature simulation systems. It bridges your existing sophisticated genetics and AI systems with high-performance ECS components and systems.

## 🏗️ Architecture

### **Components**
- `CreatureECSComponents.cs` - All ECS components for creature simulation
- `CreatureAuthoring.cs` - MonoBehaviour authoring component that converts to ECS
- `CreatureSceneBootstrap.cs` - Scene setup utility for testing

### **Systems** 
- `CreatureSimulationSystems.cs` - Core ECS systems (aging, metabolism, etc.)
- Additional systems to be added: genetics expression, environmental adaptation, breeding

### **Configuration**
- `EnhancedCreatureSpeciesConfig.cs` - ScriptableObject for species configuration
- Integrates with existing CreatureDefinition system

## 🚀 Quick Start

1. **Add CreatureSceneBootstrap** to an empty GameObject in your scene
2. **Configure** the bootstrap with desired creature count and biome
3. **Create** an EnhancedCreatureSpeciesConfig asset for your creature species
4. **Hit Play** and watch ECS creatures simulate in real-time!

## 🔗 Integration Points

### **With Existing Systems:**
- **BreedingSystem**: Creates ECS entities from breeding results
- **GeneticProfile**: Converts to optimized ECS components  
- **ChimeraMonsterAI**: Reads genetic behavior modifiers from ECS
- **CreatureInstance**: Seamless conversion to/from ECS representation

### **Performance Benefits:**
- Simulate **1000+ creatures** at 60 FPS
- Parallel processing of aging, metabolism, and behavior
- Efficient memory layout for genetic data
- Batch operations for environmental effects

## 🧬 Key Features

- **Real-time aging and lifecycle simulation**
- **Genetic trait expression affecting behavior**
- **Environmental adaptation and selection pressure**
- **Player-creature bonding and relationship dynamics**
- **Breeding compatibility and fertility management**
- **Biome-specific environmental effects**

## 📊 Data Flow

```
CreatureInstance → CreatureAuthoring → ECS Components → ECS Systems → Game Logic
                                    ↓
                              Visual Updates & UI
```

## 🛠️ Extending the System

### Adding New Traits
1. Add trait to `CreatureGeneticsComponent`
2. Update genetic profile conversion
3. Create system to process new trait
4. Connect to visual/behavioral effects

### Creating New Systems
1. Inherit from `ISystem`
2. Query for relevant components  
3. Implement parallel job processing
4. Update in appropriate system group

## 🔍 Debug & Testing

- Use **CreatureSceneBootstrap** for rapid testing
- Enable **debug logging** in CreatureAuthoring
- Check **ECS debugger** in Window → Analysis → Systems
- Use **Scene view gizmos** to visualize creature data

## 📈 Performance Notes

- ECS systems update at **10 Hz** by default for simulation stability
- Creature aging is **accelerated 30x** for development testing
- Genetic calculations are **cached** in ECS components for efficiency
- Environmental effects apply to **all creatures simultaneously**

---

**This ECS integration enables Project Chimera to scale from dozens to thousands of creatures while maintaining the sophisticated genetics and behavioral systems you've already built!**