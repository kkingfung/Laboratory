using System;
using UnityEngine;
using Laboratory.Core.Events;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Resource management system for Monster Town.
    /// Handles all resource operations, validation, and events.
    /// </summary>
    public class ResourceManager : IResourceManager
    {
        private readonly IEventBus _eventBus;
        private TownResources _currentResources;
        private TownResources _resourceLimits;
        private bool _isInitialized = false;

        // Resource generation tracking
        private float _lastUpdateTime;
        private TownResources _pendingGeneration;

        // Resource transaction history
        private readonly CircularBuffer<ResourceTransaction> _transactionHistory = new(100);

        public event Action<TownResources> OnResourcesChanged;

        public ResourceManager(IEventBus eventBus)
        {
            _eventBus = eventBus;
            _resourceLimits = GetDefaultResourceLimits();
        }

        #region IResourceManager Implementation

        public void InitializeResources(TownResources startingResources)
        {
            _currentResources = ValidateResourceLimits(startingResources);
            _lastUpdateTime = Time.time;
            _isInitialized = true;

            LogTransaction(ResourceTransactionType.Initialize, TownResources.Zero, _currentResources, "Resource system initialized");

            OnResourcesChanged?.Invoke(_currentResources);
            _eventBus?.Publish(new ResourcesChangedEvent(TownResources.Zero, _currentResources));

            Debug.Log($"💰 Resource Manager initialized with: {_currentResources}");
        }

        public void UpdateResources(TownResources newResources)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("ResourceManager not initialized!");
                return;
            }

            var oldResources = _currentResources;
            _currentResources = ValidateResourceLimits(newResources);

            if (!ResourcesEqual(oldResources, _currentResources))
            {
                LogTransaction(ResourceTransactionType.Update, oldResources, _currentResources, "Resources updated externally");

                OnResourcesChanged?.Invoke(_currentResources);
                _eventBus?.Publish(new ResourcesChangedEvent(oldResources, _currentResources));
            }
        }

        public bool CanAfford(TownResources cost)
        {
            if (!_isInitialized) return false;
            return _currentResources.CanAfford(cost);
        }

        public void AddResources(TownResources resources)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("ResourceManager not initialized!");
                return;
            }

            var oldResources = _currentResources;
            _currentResources = ValidateResourceLimits(_currentResources + resources);

            LogTransaction(ResourceTransactionType.Add, oldResources, _currentResources, $"Added resources: {resources}");

            OnResourcesChanged?.Invoke(_currentResources);
            _eventBus?.Publish(new ResourcesChangedEvent(oldResources, _currentResources));

            Debug.Log($"💰 Added resources: {resources} -> Total: {_currentResources}");
        }

        public void DeductResources(TownResources cost)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("ResourceManager not initialized!");
                return;
            }

            if (!CanAfford(cost))
            {
                Debug.LogWarning($"Cannot afford cost: {cost}. Current resources: {_currentResources}");
                _eventBus?.Publish(new InsufficientResourcesEvent(cost, _currentResources));
                return;
            }

            var oldResources = _currentResources;
            _currentResources = _currentResources - cost;

            LogTransaction(ResourceTransactionType.Deduct, oldResources, _currentResources, $"Deducted resources: {cost}");

            OnResourcesChanged?.Invoke(_currentResources);
            _eventBus?.Publish(new ResourcesChangedEvent(oldResources, _currentResources));

            Debug.Log($"💰 Deducted resources: {cost} -> Remaining: {_currentResources}");
        }

        public TownResources GetCurrentResources()
        {
            return _currentResources;
        }

        public void Dispose()
        {
            _isInitialized = false;
            OnResourcesChanged = null;
            _transactionHistory.Clear();

            Debug.Log("💰 Resource Manager disposed");
        }

        #endregion

        #region Extended Resource Management

        /// <summary>
        /// Set resource limits to prevent overflow
        /// </summary>
        public void SetResourceLimits(TownResources limits)
        {
            _resourceLimits = limits;

            // Clamp current resources to new limits
            _currentResources = ValidateResourceLimits(_currentResources);

            Debug.Log($"💰 Resource limits updated: {_resourceLimits}");
        }

        /// <summary>
        /// Get current resource limits
        /// </summary>
        public TownResources GetResourceLimits()
        {
            return _resourceLimits;
        }

        /// <summary>
        /// Check if resources are at or near capacity
        /// </summary>
        public bool IsNearResourceLimit(ResourceType resourceType, float threshold = 0.9f)
        {
            var current = GetResourceAmount(resourceType, _currentResources);
            var limit = GetResourceAmount(resourceType, _resourceLimits);

            return limit > 0 && (current / (float)limit) >= threshold;
        }

        /// <summary>
        /// Get percentage of resource capacity used
        /// </summary>
        public float GetResourceCapacityUsage(ResourceType resourceType)
        {
            var current = GetResourceAmount(resourceType, _currentResources);
            var limit = GetResourceAmount(resourceType, _resourceLimits);

            return limit > 0 ? (current / (float)limit) : 0f;
        }

        /// <summary>
        /// Exchange one resource type for another
        /// </summary>
        public bool ExchangeResources(ResourceType fromType, ResourceType toType, int amount, float exchangeRate = 1f)
        {
            var fromCost = CreateResourcesOfType(fromType, amount);
            var toGain = CreateResourcesOfType(toType, Mathf.RoundToInt(amount * exchangeRate));

            if (!CanAfford(fromCost))
                return false;

            var oldResources = _currentResources;

            DeductResources(fromCost);
            AddResources(toGain);

            LogTransaction(ResourceTransactionType.Exchange, oldResources, _currentResources,
                $"Exchanged {amount} {fromType} for {toGain} {toType} (rate: {exchangeRate})");

            Debug.Log($"💱 Exchanged {amount} {fromType} for {Mathf.RoundToInt(amount * exchangeRate)} {toType}");
            return true;
        }

        /// <summary>
        /// Generate resources over time (called by town management)
        /// </summary>
        public void ProcessResourceGeneration(TownResources generationRate)
        {
            if (!_isInitialized) return;

            var currentTime = Time.time;
            var deltaTime = currentTime - _lastUpdateTime;

            if (deltaTime >= 1f) // Generate every second
            {
                var generated = MultiplyResources(generationRate, deltaTime);

                if (generated.HasAnyResource())
                {
                    AddResources(generated);
                    LogTransaction(ResourceTransactionType.Generate, _currentResources - generated, _currentResources,
                        $"Generated resources over {deltaTime:F1}s: {generated}");
                }

                _lastUpdateTime = currentTime;
            }
        }

        /// <summary>
        /// Get transaction history for debugging/UI
        /// </summary>
        public ResourceTransaction[] GetTransactionHistory()
        {
            return _transactionHistory.ToArray();
        }

        /// <summary>
        /// Calculate resource generation potential based on current buildings
        /// </summary>
        public TownResources CalculatePotentialGeneration(TownResources baseGeneration, float happinessMultiplier = 1f)
        {
            var potential = MultiplyResources(baseGeneration, happinessMultiplier);
            return potential;
        }

        #endregion

        #region Helper Methods

        private TownResources ValidateResourceLimits(TownResources resources)
        {
            return new TownResources
            {
                coins = Math.Min(Math.Max(0, resources.coins), _resourceLimits.coins),
                gems = Math.Min(Math.Max(0, resources.gems), _resourceLimits.gems),
                activityTokens = Math.Min(Math.Max(0, resources.activityTokens), _resourceLimits.activityTokens),
                geneticSamples = Math.Min(Math.Max(0, resources.geneticSamples), _resourceLimits.geneticSamples),
                materials = Math.Min(Math.Max(0, resources.materials), _resourceLimits.materials),
                energy = Math.Min(Math.Max(0, resources.energy), _resourceLimits.energy)
            };
        }

        private TownResources GetDefaultResourceLimits()
        {
            return new TownResources
            {
                coins = 999999,
                gems = 99999,
                activityTokens = 9999,
                geneticSamples = 999,
                materials = 9999,
                energy = 999
            };
        }

        private bool ResourcesEqual(TownResources a, TownResources b)
        {
            return a.coins == b.coins &&
                   a.gems == b.gems &&
                   a.activityTokens == b.activityTokens &&
                   a.geneticSamples == b.geneticSamples &&
                   a.materials == b.materials &&
                   a.energy == b.energy;
        }

        private int GetResourceAmount(ResourceType resourceType, TownResources resources)
        {
            return resourceType switch
            {
                ResourceType.Coins => resources.coins,
                ResourceType.Gems => resources.gems,
                ResourceType.ActivityTokens => resources.activityTokens,
                ResourceType.GeneticSamples => resources.geneticSamples,
                ResourceType.Materials => resources.materials,
                ResourceType.Energy => resources.energy,
                _ => 0
            };
        }

        private TownResources CreateResourcesOfType(ResourceType resourceType, int amount)
        {
            return resourceType switch
            {
                ResourceType.Coins => new TownResources { coins = amount },
                ResourceType.Gems => new TownResources { gems = amount },
                ResourceType.ActivityTokens => new TownResources { activityTokens = amount },
                ResourceType.GeneticSamples => new TownResources { geneticSamples = amount },
                ResourceType.Materials => new TownResources { materials = amount },
                ResourceType.Energy => new TownResources { energy = amount },
                _ => TownResources.Zero
            };
        }

        private TownResources MultiplyResources(TownResources resources, float multiplier)
        {
            return new TownResources
            {
                coins = Mathf.RoundToInt(resources.coins * multiplier),
                gems = Mathf.RoundToInt(resources.gems * multiplier),
                activityTokens = Mathf.RoundToInt(resources.activityTokens * multiplier),
                geneticSamples = Mathf.RoundToInt(resources.geneticSamples * multiplier),
                materials = Mathf.RoundToInt(resources.materials * multiplier),
                energy = Mathf.RoundToInt(resources.energy * multiplier)
            };
        }

        private void LogTransaction(ResourceTransactionType type, TownResources before, TownResources after, string description)
        {
            var transaction = new ResourceTransaction
            {
                Type = type,
                Timestamp = DateTime.UtcNow,
                ResourcesBefore = before,
                ResourcesAfter = after,
                Description = description
            };

            _transactionHistory.Add(transaction);
        }

        #endregion
    }

    #region Supporting Data Structures

    /// <summary>
    /// Resource types enumeration
    /// </summary>
    public enum ResourceType
    {
        Coins,
        Gems,
        ActivityTokens,
        GeneticSamples,
        Materials,
        Energy
    }

    /// <summary>
    /// Resource transaction types
    /// </summary>
    public enum ResourceTransactionType
    {
        Initialize,
        Add,
        Deduct,
        Update,
        Exchange,
        Generate
    }

    /// <summary>
    /// Resource transaction record
    /// </summary>
    [Serializable]
    public struct ResourceTransaction
    {
        public ResourceTransactionType Type;
        public DateTime Timestamp;
        public TownResources ResourcesBefore;
        public TownResources ResourcesAfter;
        public string Description;

        public TownResources GetChange()
        {
            return ResourcesAfter - ResourcesBefore;
        }
    }

    /// <summary>
    /// Circular buffer for efficient transaction history storage
    /// </summary>
    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _head = 0;
        private int _count = 0;

        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
        }

        public void Add(T item)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;

            if (_count < _buffer.Length)
                _count++;
        }

        public T[] ToArray()
        {
            var result = new T[_count];

            if (_count == 0) return result;

            var startIndex = _count < _buffer.Length ? 0 : _head;

            for (int i = 0; i < _count; i++)
            {
                var bufferIndex = (startIndex + i) % _buffer.Length;
                result[i] = _buffer[bufferIndex];
            }

            return result;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Insufficient resources event
    /// </summary>
    public class InsufficientResourcesEvent
    {
        public TownResources RequiredResources { get; private set; }
        public TownResources CurrentResources { get; private set; }
        public TownResources MissingResources { get; private set; }

        public InsufficientResourcesEvent(TownResources required, TownResources current)
        {
            RequiredResources = required;
            CurrentResources = current;
            MissingResources = required - current;
        }
    }

    #endregion
}