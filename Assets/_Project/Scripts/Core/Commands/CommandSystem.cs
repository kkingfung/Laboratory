using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Commands
{
    /// <summary>
    /// Command pattern implementation for undo/redo functionality
    /// Supports command history, batching, and transaction-based operations
    /// Essential for editor tools, breeding experiments, and player actions
    /// </summary>
    public class CommandSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxHistorySize = 100;
        [SerializeField] private bool enableLogging = true;

        [Header("Runtime Status")]
        [SerializeField] private int commandsExecuted = 0;
        [SerializeField] private int commandsUndone = 0;
        [SerializeField] private int commandsRedone = 0;

        // Command history stacks
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();

        // Batch command support
        private BatchCommand _currentBatch;
        private bool _isBatching = false;

        // Events
        public event Action<ICommand> OnCommandExecuted;
        public event Action<ICommand> OnCommandUndone;
        public event Action<ICommand> OnCommandRedone;
        public event Action OnHistoryChanged;

        private static CommandSystem _instance;
        public static CommandSystem Instance => _instance;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public int UndoStackSize => _undoStack.Count;
        public int RedoStackSize => _redoStack.Count;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Keyboard shortcuts
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        Redo(); // Ctrl+Shift+Z = Redo
                    }
                    else
                    {
                        Undo(); // Ctrl+Z = Undo
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Y))
                {
                    Redo(); // Ctrl+Y = Redo
                }
            }
        }

        #endregion

        #region Command Execution

        public bool Execute(ICommand command)
        {
            if (command == null)
            {
                Debug.LogError("[CommandSystem] Cannot execute null command");
                return false;
            }

            try
            {
                // If we're batching, add to current batch
                if (_isBatching && _currentBatch != null)
                {
                    _currentBatch.AddCommand(command);
                    return true;
                }

                // Execute the command
                bool success = command.Execute();

                if (success)
                {
                    // Add to undo stack
                    _undoStack.Push(command);

                    // Clear redo stack (new action invalidates redo)
                    _redoStack.Clear();

                    // Trim history if needed
                    TrimHistory();

                    // Update stats
                    commandsExecuted++;

                    if (enableLogging)
                        Debug.Log($"[CommandSystem] Executed: {command.GetDescription()}");

                    // Notify listeners
                    OnCommandExecuted?.Invoke(command);
                    OnHistoryChanged?.Invoke();
                }
                else
                {
                    Debug.LogError($"[CommandSystem] Failed to execute: {command.GetDescription()}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandSystem] Exception executing command: {ex.Message}");
                return false;
            }
        }

        public bool Undo()
        {
            if (!CanUndo)
            {
                if (enableLogging)
                    Debug.LogWarning("[CommandSystem] Nothing to undo");
                return false;
            }

            try
            {
                var command = _undoStack.Pop();
                bool success = command.Undo();

                if (success)
                {
                    _redoStack.Push(command);
                    commandsUndone++;

                    if (enableLogging)
                        Debug.Log($"[CommandSystem] Undone: {command.GetDescription()}");

                    OnCommandUndone?.Invoke(command);
                    OnHistoryChanged?.Invoke();
                }
                else
                {
                    // If undo failed, put it back
                    _undoStack.Push(command);
                    Debug.LogError($"[CommandSystem] Failed to undo: {command.GetDescription()}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandSystem] Exception during undo: {ex.Message}");
                return false;
            }
        }

        public bool Redo()
        {
            if (!CanRedo)
            {
                if (enableLogging)
                    Debug.LogWarning("[CommandSystem] Nothing to redo");
                return false;
            }

            try
            {
                var command = _redoStack.Pop();
                bool success = command.Execute();

                if (success)
                {
                    _undoStack.Push(command);
                    commandsRedone++;

                    if (enableLogging)
                        Debug.Log($"[CommandSystem] Redone: {command.GetDescription()}");

                    OnCommandRedone?.Invoke(command);
                    OnHistoryChanged?.Invoke();
                }
                else
                {
                    // If redo failed, put it back
                    _redoStack.Push(command);
                    Debug.LogError($"[CommandSystem] Failed to redo: {command.GetDescription()}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandSystem] Exception during redo: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Batch Commands

        public void BeginBatch(string description = "Batch Operation")
        {
            if (_isBatching)
            {
                Debug.LogWarning("[CommandSystem] Already batching commands");
                return;
            }

            _currentBatch = new BatchCommand(description);
            _isBatching = true;

            if (enableLogging)
                Debug.Log($"[CommandSystem] Started batch: {description}");
        }

        public void EndBatch()
        {
            if (!_isBatching)
            {
                Debug.LogWarning("[CommandSystem] Not currently batching");
                return;
            }

            _isBatching = false;

            if (_currentBatch != null && _currentBatch.CommandCount > 0)
            {
                // Execute the batch as a single command
                Execute(_currentBatch);

                if (enableLogging)
                    Debug.Log($"[CommandSystem] Ended batch: {_currentBatch.GetDescription()} ({_currentBatch.CommandCount} commands)");
            }

            _currentBatch = null;
        }

        public void CancelBatch()
        {
            if (!_isBatching)
            {
                Debug.LogWarning("[CommandSystem] Not currently batching");
                return;
            }

            _isBatching = false;
            _currentBatch = null;

            if (enableLogging)
                Debug.Log("[CommandSystem] Cancelled batch");
        }

        #endregion

        #region History Management

        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();

            OnHistoryChanged?.Invoke();

            if (enableLogging)
                Debug.Log("[CommandSystem] History cleared");
        }

        public List<string> GetUndoHistory()
        {
            var history = new List<string>();
            foreach (var command in _undoStack)
            {
                history.Add(command.GetDescription());
            }
            return history;
        }

        public List<string> GetRedoHistory()
        {
            var history = new List<string>();
            foreach (var command in _redoStack)
            {
                history.Add(command.GetDescription());
            }
            return history;
        }

        private void TrimHistory()
        {
            while (_undoStack.Count > maxHistorySize)
            {
                var commands = _undoStack.ToArray();
                _undoStack.Clear();

                // Keep the most recent commands
                for (int i = 1; i < commands.Length; i++)
                {
                    _undoStack.Push(commands[i]);
                }
            }
        }

        #endregion

        #region Public API

        public CommandStats GetStats()
        {
            return new CommandStats
            {
                commandsExecuted = commandsExecuted,
                commandsUndone = commandsUndone,
                commandsRedone = commandsRedone,
                undoStackSize = _undoStack.Count,
                redoStackSize = _redoStack.Count,
                maxHistorySize = maxHistorySize
            };
        }

        #endregion

        #region Context Menu

        [ContextMenu("Undo Last Command")]
        private void UndoDebug()
        {
            Undo();
        }

        [ContextMenu("Redo Last Command")]
        private void RedoDebug()
        {
            Redo();
        }

        [ContextMenu("Clear Command History")]
        private void ClearHistoryDebug()
        {
            ClearHistory();
        }

        [ContextMenu("Print Command History")]
        private void PrintHistoryDebug()
        {
            Debug.Log("=== UNDO STACK ===");
            foreach (var cmd in _undoStack)
            {
                Debug.Log($"  - {cmd.GetDescription()}");
            }

            Debug.Log("=== REDO STACK ===");
            foreach (var cmd in _redoStack)
            {
                Debug.Log($"  - {cmd.GetDescription()}");
            }
        }

        #endregion
    }

    #region Command Interfaces

    /// <summary>
    /// Base interface for all commands
    /// </summary>
    public interface ICommand
    {
        bool Execute();
        bool Undo();
        string GetDescription();
    }

    #endregion

    #region Batch Command

    /// <summary>
    /// Groups multiple commands into a single undoable/redoable operation
    /// </summary>
    public class BatchCommand : ICommand
    {
        private readonly List<ICommand> _commands = new List<ICommand>();
        private readonly string _description;

        public int CommandCount => _commands.Count;

        public BatchCommand(string description)
        {
            _description = description;
        }

        public void AddCommand(ICommand command)
        {
            _commands.Add(command);
        }

        public bool Execute()
        {
            bool allSucceeded = true;

            foreach (var command in _commands)
            {
                if (!command.Execute())
                {
                    allSucceeded = false;
                    // Continue executing remaining commands
                }
            }

            return allSucceeded;
        }

        public bool Undo()
        {
            bool allSucceeded = true;

            // Undo in reverse order
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                if (!_commands[i].Undo())
                {
                    allSucceeded = false;
                    // Continue undoing remaining commands
                }
            }

            return allSucceeded;
        }

        public string GetDescription()
        {
            return $"{_description} ({_commands.Count} operations)";
        }
    }

    #endregion

    #region Example Commands

    /// <summary>
    /// Example: Command to modify a creature's genetic traits
    /// </summary>
    public class ModifyGeneticTraitCommand : ICommand
    {
        private readonly string _creatureId;
        private readonly string _traitName;
        private readonly float _newValue;
        private float _previousValue;

        public ModifyGeneticTraitCommand(string creatureId, string traitName, float newValue)
        {
            _creatureId = creatureId;
            _traitName = traitName;
            _newValue = newValue;
        }

        public bool Execute()
        {
            // Store previous value
            _previousValue = GetTraitValue(_creatureId, _traitName);

            // Apply new value
            SetTraitValue(_creatureId, _traitName, _newValue);

            return true;
        }

        public bool Undo()
        {
            // Restore previous value
            SetTraitValue(_creatureId, _traitName, _previousValue);

            return true;
        }

        public string GetDescription()
        {
            return $"Modify {_traitName} on {_creatureId}";
        }

        // Mock methods - would integrate with actual genetics system
        private float GetTraitValue(string creatureId, string traitName) => 0.5f;
        private void SetTraitValue(string creatureId, string traitName, float value) { }
    }

    /// <summary>
    /// Example: Command to spawn a creature
    /// </summary>
    public class SpawnCreatureCommand : ICommand
    {
        private readonly string _speciesId;
        private readonly Vector3 _position;
        private string _spawnedCreatureId;

        public SpawnCreatureCommand(string speciesId, Vector3 position)
        {
            _speciesId = speciesId;
            _position = position;
        }

        public bool Execute()
        {
            // Spawn creature
            _spawnedCreatureId = SpawnCreature(_speciesId, _position);

            return !string.IsNullOrEmpty(_spawnedCreatureId);
        }

        public bool Undo()
        {
            // Despawn creature
            if (!string.IsNullOrEmpty(_spawnedCreatureId))
            {
                DespawnCreature(_spawnedCreatureId);
                return true;
            }

            return false;
        }

        public string GetDescription()
        {
            return $"Spawn {_speciesId} at {_position}";
        }

        // Mock methods - would integrate with actual spawning system
        private string SpawnCreature(string speciesId, Vector3 position) => Guid.NewGuid().ToString();
        private void DespawnCreature(string creatureId) { }
    }

    /// <summary>
    /// Example: Command to breed two creatures
    /// </summary>
    public class BreedCreaturesCommand : ICommand
    {
        private readonly string _parent1Id;
        private readonly string _parent2Id;
        private string _offspringId;

        public BreedCreaturesCommand(string parent1Id, string parent2Id)
        {
            _parent1Id = parent1Id;
            _parent2Id = parent2Id;
        }

        public bool Execute()
        {
            // Perform breeding
            _offspringId = BreedCreatures(_parent1Id, _parent2Id);

            return !string.IsNullOrEmpty(_offspringId);
        }

        public bool Undo()
        {
            // Remove offspring
            if (!string.IsNullOrEmpty(_offspringId))
            {
                RemoveCreature(_offspringId);
                return true;
            }

            return false;
        }

        public string GetDescription()
        {
            return $"Breed {_parent1Id} × {_parent2Id}";
        }

        // Mock methods - would integrate with actual breeding system
        private string BreedCreatures(string parent1Id, string parent2Id) => Guid.NewGuid().ToString();
        private void RemoveCreature(string creatureId) { }
    }

    #endregion

    #region Data Structures

    [Serializable]
    public struct CommandStats
    {
        public int commandsExecuted;
        public int commandsUndone;
        public int commandsRedone;
        public int undoStackSize;
        public int redoStackSize;
        public int maxHistorySize;
    }

    #endregion
}
