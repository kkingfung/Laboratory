# Contributing to Project Chimera 🧬

Thank you for your interest in contributing to Project Chimera! This document provides guidelines and information for contributors.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Coding Standards](#coding-standards)
- [Contributing Process](#contributing-process)
- [Issue Guidelines](#issue-guidelines)
- [Pull Request Guidelines](#pull-request-guidelines)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)
- [Release Process](#release-process)

## 🤝 Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inspiring community for all. Please read and follow our Code of Conduct:

- **Be respectful** and inclusive
- **Be collaborative** and constructive
- **Be patient** with newcomers
- **Be professional** in all interactions
- **Be supportive** of different skill levels

### Reporting Issues

If you experience or witness unacceptable behavior, please report it to the project maintainers.

## 🚀 Getting Started

### Prerequisites

- **Unity 6000.2.0b7** or compatible version
- **Visual Studio 2022** or **JetBrains Rider** (recommended)
- **Git** for version control
- **GitHub account** for collaboration

### Quick Start

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/Laboratory.git
   cd Laboratory
   ```
3. **Add upstream remote**:
   ```bash
   git remote add upstream https://github.com/ORIGINAL_OWNER/Laboratory.git
   ```
4. **Open in Unity** and let it import all packages
5. **Create a new branch** for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## 🛠️ Development Setup

### Unity Configuration

1. **Open Unity Hub** and add the project
2. **Select Unity 6000.2.0b7** as the project version
3. **Import packages** automatically when prompted
4. **Configure build settings** for your target platform

### IDE Setup

#### Visual Studio 2022
- Install **Unity development tools**
- Configure **IntelliSense** for Unity
- Enable **XML documentation** support

#### JetBrains Rider
- Install **Unity plugin**
- Configure **Unity integration**
- Enable **code analysis** tools

### Package Dependencies

The project uses several key packages:
- **Unity ECS** - Entity Component System
- **Unity Netcode** - Multiplayer networking
- **Unity AI Navigation** - AI pathfinding
- **Unity Input System** - Input handling
- **Unity Addressables** - Asset management

## 🏗️ Project Structure

```
Assets/
├── _Project/                 # Main game code
│   ├── Scripts/
│   │   ├── Core/            # Core systems and managers
│   │   ├── Chimera/         # Chimera monster systems
│   │   ├── Subsystems/      # Game subsystems
│   │   └── UI/              # User interface
│   ├── Prefabs/             # Game prefabs
│   ├── Materials/           # Materials and shaders
│   └── Scenes/              # Game scenes
├── Tests/                    # Unit tests
├── Editor/                   # Editor scripts
└── StreamingAssets/         # Runtime assets
```

### Key Directories

- **`Assets/_Project/Scripts/Core/`** - Core game systems
- **`Assets/_Project/Scripts/Chimera/`** - Monster breeding and AI
- **`Assets/_Project/Scripts/Subsystems/`** - Modular game systems
- **`Assets/Tests/`** - Unit and integration tests
- **`Assets/Editor/`** - Custom editor tools

## 📝 Coding Standards

### C# Style Guide

#### Naming Conventions
```csharp
// Classes: PascalCase
public class ChimeraMonsterAI { }

// Methods: PascalCase
public void UpdateBehavior() { }

// Properties: PascalCase
public float Health { get; set; }

// Fields: camelCase (private), PascalCase (public)
private float currentHealth;
public float MaxHealth { get; set; }

// Constants: UPPER_CASE
public const float MAX_SPEED = 10.0f;

// Enums: PascalCase
public enum ChimeraBehaviorType { }
```

#### Code Organization
```csharp
using System;
using UnityEngine;
using Unity.Entities;

namespace ProjectChimera.Chimera
{
    /// <summary>
    /// Brief description of the class purpose.
    /// </summary>
    public class ChimeraMonsterAI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private float health;
        #endregion

        #region Properties
        public float Health => health;
        #endregion

        #region Unity Methods
        private void Start() { }
        private void Update() { }
        #endregion

        #region Public Methods
        public void UpdateBehavior() { }
        #endregion

        #region Private Methods
        private void CalculateStats() { }
        #endregion
    }
}
```

### Documentation Standards

#### XML Documentation
```csharp
/// <summary>
/// Updates the Chimera monster's behavior based on current state.
/// </summary>
/// <param name="deltaTime">Time elapsed since last update</param>
/// <param name="playerPosition">Current player position for AI targeting</param>
/// <returns>True if behavior was successfully updated</returns>
public bool UpdateBehavior(float deltaTime, Vector3 playerPosition)
{
    // Implementation
}
```

#### Inline Comments
```csharp
// Calculate genetic inheritance based on parent DNA
var inheritedTraits = CalculateGeneticTraits(parent1DNA, parent2DNA);

// Apply visual genetics to mesh renderer
ApplyVisualGenetics(inheritedTraits);
```

## 🔄 Contributing Process

### 1. Issue Creation
- **Check existing issues** before creating new ones
- **Use appropriate templates** (Bug, Feature, etc.)
- **Provide detailed information** and reproduction steps
- **Label appropriately** for easy categorization

### 2. Development Workflow
```bash
# 1. Create feature branch
git checkout -b feature/chimera-breeding-improvements

# 2. Make changes and commit
git add .
git commit -m "feat: improve chimera breeding algorithm"

# 3. Push to your fork
git push origin feature/chimera-breeding-improvements

# 4. Create pull request
# Use the PR template and fill out all sections
```

### 3. Commit Message Format
```
type(scope): description

[optional body]

[optional footer]
```

#### Types
- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation changes
- **style**: Code style changes
- **refactor**: Code refactoring
- **test**: Test additions/changes
- **chore**: Maintenance tasks

#### Examples
```
feat(chimera): add genetic mutation system
fix(combat): resolve damage calculation bug
docs(api): update breeding system documentation
test(ai): add unit tests for behavior states
```

## 🐛 Issue Guidelines

### Bug Reports
- **Clear title** describing the issue
- **Detailed description** of the problem
- **Steps to reproduce** the issue
- **Expected vs actual behavior**
- **Environment details** (Unity version, OS, etc.)
- **Screenshots/videos** if applicable

### Feature Requests
- **Clear title** describing the feature
- **Detailed description** of the proposed feature
- **Use case** and motivation
- **Proposed implementation** (if known)
- **Alternative solutions** considered

### Enhancement Requests
- **Current behavior** description
- **Proposed improvement** details
- **Benefits** of the change
- **Potential drawbacks** or considerations

## 🔀 Pull Request Guidelines

### Before Submitting
- [ ] **Code compiles** without warnings
- [ ] **Tests pass** (existing and new)
- [ ] **Documentation updated** if needed
- [ ] **Code follows** project standards
- [ ] **Performance impact** considered
- [ ] **Breaking changes** documented

### PR Requirements
- **Use the PR template** and fill out all sections
- **Link related issues** using keywords
- **Include screenshots/videos** for visual changes
- **Add tests** for new functionality
- **Update documentation** as needed

### Review Process
1. **Automated checks** must pass
2. **Code review** by maintainers
3. **Testing** in Unity Editor
4. **Approval** from at least one maintainer
5. **Merge** after approval

## 🧪 Testing Guidelines

### Unit Tests
- **Test all public methods** and properties
- **Use descriptive test names**:
  ```csharp
  [Test]
  public void ChimeraBreeding_WithValidParents_ShouldProduceOffspring()
  {
      // Arrange
      var parent1 = CreateTestChimera();
      var parent2 = CreateTestChimera();
      
      // Act
      var offspring = BreedingSystem.Breed(parent1, parent2);
      
      // Assert
      Assert.IsNotNull(offspring);
      Assert.IsTrue(offspring.HasValidDNA());
  }
  ```

### Integration Tests
- **Test system interactions**
- **Verify data flow** between components
- **Test error handling** and edge cases

### Performance Tests
- **Measure execution time** for critical paths
- **Monitor memory usage** for potential leaks
- **Test with large datasets** (many monsters, etc.)

## 📚 Documentation

### Code Documentation
- **XML documentation** for all public APIs
- **Inline comments** for complex logic
- **README updates** for new features
- **API documentation** for external interfaces

### User Documentation
- **Game mechanics** explanation
- **Breeding system** guide
- **Multiplayer features** documentation
- **Troubleshooting** guides

## 🚀 Release Process

### Version Numbering
We use **Semantic Versioning** (SemVer):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Release Checklist
- [ ] **All tests pass**
- [ ] **Documentation updated**
- [ ] **Version number** incremented
- [ ] **Changelog** updated
- [ ] **Release notes** prepared
- [ ] **Build artifacts** created
- [ ] **Deployment** completed

## 🎯 Areas for Contribution

### High Priority
- **Chimera AI improvements** - Better behavior patterns
- **Breeding system** - Genetic algorithm enhancements
- **Multiplayer stability** - Network synchronization
- **Performance optimization** - ECS system improvements
- **UI/UX enhancements** - Better user experience

### Medium Priority
- **New monster types** - Additional creature varieties
- **Combat system** - Battle mechanics improvements
- **Inventory system** - Item management features
- **World generation** - Procedural content creation
- **Audio system** - Sound effects and music

### Low Priority
- **Documentation** - Code comments and guides
- **Testing** - Additional unit tests
- **Tools** - Editor utilities and helpers
- **Localization** - Multi-language support
- **Accessibility** - UI accessibility features

## 🆘 Getting Help

### Resources
- **GitHub Discussions** - General questions and ideas
- **GitHub Issues** - Bug reports and feature requests
- **Unity Forums** - Unity-specific questions
- **Discord Server** - Real-time community chat

### Contact Maintainers
- **GitHub Issues** - For project-related questions
- **Email** - For sensitive or private matters
- **Discord** - For quick questions and community chat

## 🏆 Recognition

### Contributors
- **Contributors list** - GitHub automatically tracks contributors
- **Release notes** - Major contributors mentioned in releases
- **Community recognition** - Highlighted in project updates

### Types of Contributions
- **Code contributions** - Bug fixes, features, improvements
- **Documentation** - Guides, tutorials, API docs
- **Testing** - Bug reports, test cases, quality assurance
- **Community** - Helping others, answering questions
- **Design** - UI/UX improvements, visual assets

---

## 📄 License

By contributing to Project Chimera, you agree that your contributions will be licensed under the same license as the project.

---

**Thank you for contributing to Project Chimera! 🧬✨**

*Together, we're building an amazing monster breeding experience!*
