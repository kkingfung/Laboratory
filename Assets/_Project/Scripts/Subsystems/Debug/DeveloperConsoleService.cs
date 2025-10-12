using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Laboratory.Subsystems.Debug
{
    /// <summary>
    /// Concrete implementation of developer console service
    /// Handles command registration, parsing, execution, and history management
    /// </summary>
    public class DeveloperConsoleService : IDeveloperConsoleService
    {
        #region Fields

        private readonly DebugSubsystemConfig _config;
        private Dictionary<string, DebugCommand> _registeredCommands;
        private List<string> _commandHistory;
        private Dictionary<string, object> _variables;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public DeveloperConsoleService(DebugSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IDeveloperConsoleService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _registeredCommands = new Dictionary<string, DebugCommand>();
                _commandHistory = new List<string>();
                _variables = new Dictionary<string, object>();

                // Initialize default variables
                InitializeDefaultVariables();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[DeveloperConsoleService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeveloperConsoleService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void RegisterCommand(DebugCommand command)
        {
            if (!_isInitialized || command == null || string.IsNullOrEmpty(command.commandName))
                return;

            _registeredCommands[command.commandName.ToLower()] = command;

            if (_config.enableDebugLogging)
                Debug.Log($"[DeveloperConsoleService] Registered command: {command.commandName}");
        }

        public void UnregisterCommand(string commandName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(commandName))
                return;

            if (_registeredCommands.Remove(commandName.ToLower()))
            {
                if (_config.enableDebugLogging)
                    Debug.Log($"[DeveloperConsoleService] Unregistered command: {commandName}");
            }
        }

        public string ExecuteCommand(string commandText)
        {
            if (!_isInitialized || string.IsNullOrEmpty(commandText))
                return "Invalid command";

            try
            {
                // Add to command history
                AddToCommandHistory(commandText);

                // Parse command
                var parseResult = ParseCommand(commandText);
                if (!parseResult.success)
                    return parseResult.errorMessage;

                // Find and execute command
                var commandName = parseResult.commandName.ToLower();
                if (!_registeredCommands.TryGetValue(commandName, out var command))
                    return $"Unknown command: {parseResult.commandName}";

                // Check permission
                if (!CheckCommandPermission(command))
                    return $"Insufficient permissions to execute command: {command.commandName}";

                // Execute command
                var result = ExecuteCommandWithParameters(command, parseResult.parameters);
                return result.success ? (result.result ?? result.message) : result.errorMessage;
            }
            catch (Exception ex)
            {
                return $"Command execution error: {ex.Message}";
            }
        }

        public List<DebugCommand> GetAvailableCommands()
        {
            if (!_isInitialized)
                return new List<DebugCommand>();

            return new List<DebugCommand>(_registeredCommands.Values);
        }

        public List<string> GetCommandHistory()
        {
            if (!_isInitialized)
                return new List<string>();

            return new List<string>(_commandHistory);
        }

        public void ClearCommandHistory()
        {
            if (!_isInitialized)
                return;

            _commandHistory.Clear();

            if (_config.enableDebugLogging)
                Debug.Log("[DeveloperConsoleService] Command history cleared");
        }

        public void SetVariable(string name, object value)
        {
            if (!_isInitialized || string.IsNullOrEmpty(name))
                return;

            _variables[name] = value;

            if (_config.enableDebugLogging)
                Debug.Log($"[DeveloperConsoleService] Set variable {name} = {value}");
        }

        public object GetVariable(string name)
        {
            if (!_isInitialized || string.IsNullOrEmpty(name))
                return null;

            return _variables.TryGetValue(name, out var value) ? value : null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets command suggestions based on partial input
        /// </summary>
        public List<string> GetCommandSuggestions(string partialCommand)
        {
            if (!_isInitialized || string.IsNullOrEmpty(partialCommand))
                return new List<string>();

            var suggestions = new List<string>();
            var lowerPartial = partialCommand.ToLower();

            foreach (var command in _registeredCommands.Values)
            {
                if (command.commandName.ToLower().StartsWith(lowerPartial))
                {
                    suggestions.Add(command.commandName);
                }
            }

            return suggestions.OrderBy(s => s).ToList();
        }

        /// <summary>
        /// Gets command help text
        /// </summary>
        public string GetCommandHelp(string commandName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(commandName))
                return "Invalid command name";

            if (!_registeredCommands.TryGetValue(commandName.ToLower(), out var command))
                return $"Command '{commandName}' not found";

            var help = new StringBuilder();
            help.AppendLine($"Command: {command.commandName}");
            help.AppendLine($"Description: {command.description}");

            if (command.parameters.Count > 0)
            {
                help.AppendLine("Parameters:");
                foreach (var param in command.parameters)
                {
                    var required = param.isRequired ? "(required)" : "(optional)";
                    help.AppendLine($"  {param.name}: {param.description} {required}");
                }
            }

            if (!string.IsNullOrEmpty(command.category))
                help.AppendLine($"Category: {command.category}");

            help.AppendLine($"Permission: {command.permission}");

            return help.ToString();
        }

        #endregion

        #region Private Methods

        private void InitializeDefaultVariables()
        {
            _variables["debug_mode"] = true;
            _variables["log_level"] = "Info";
            _variables["max_fps"] = Application.targetFrameRate;
            _variables["quality_level"] = QualitySettings.GetQualityLevel();
            _variables["vsync"] = QualitySettings.vSyncCount;
        }

        private void AddToCommandHistory(string commandText)
        {
            // Avoid duplicate consecutive commands
            if (_commandHistory.Count > 0 && _commandHistory[_commandHistory.Count - 1] == commandText)
                return;

            _commandHistory.Add(commandText);

            // Keep history size manageable
            while (_commandHistory.Count > _config.maxCommandHistory)
            {
                _commandHistory.RemoveAt(0);
            }
        }

        private CommandParseResult ParseCommand(string commandText)
        {
            var parts = SplitCommandLine(commandText);
            if (parts.Count == 0)
                return new CommandParseResult { success = false, errorMessage = "Empty command" };

            var commandName = parts[0];
            var parameters = new Dictionary<string, object>();

            // Convert parameters to dictionary
            for (int i = 1; i < parts.Count; i++)
            {
                parameters[$"arg{i - 1}"] = parts[i];
            }

            return new CommandParseResult
            {
                success = true,
                commandName = commandName,
                parameters = parameters
            };
        }

        private List<string> SplitCommandLine(string commandLine)
        {
            var parts = new List<string>();
            var currentPart = new StringBuilder();
            var inQuotes = false;
            var escapeNext = false;

            foreach (char c in commandLine)
            {
                if (escapeNext)
                {
                    currentPart.Append(c);
                    escapeNext = false;
                }
                else if (c == '\\')
                {
                    escapeNext = true;
                }
                else if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (currentPart.Length > 0)
                    {
                        parts.Add(currentPart.ToString());
                        currentPart.Clear();
                    }
                }
                else
                {
                    currentPart.Append(c);
                }
            }

            if (currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString());
            }

            return parts;
        }

        private bool CheckCommandPermission(DebugCommand command)
        {
            // For now, allow all commands based on config permission level
            return command.permission <= _config.defaultPermissionLevel;
        }

        private CommandResult ExecuteCommandWithParameters(DebugCommand command, Dictionary<string, object> parameters)
        {
            try
            {
                // Convert parameter dictionary to list for compatibility with existing command actions
                var parameterList = new List<object>();
                foreach (var param in parameters.Values)
                {
                    parameterList.Add(param);
                }

                // Execute the command action if it exists
                if (command.executeAction != null)
                {
                    command.executeAction(parameters);
                    return new CommandResult
                    {
                        command = command,
                        success = true,
                        result = "Command executed successfully",
                        executionTime = DateTime.Now
                    };
                }
                else
                {
                    return new CommandResult
                    {
                        command = command,
                        success = false,
                        errorMessage = "Command has no implementation",
                        executionTime = DateTime.Now
                    };
                }
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    command = command,
                    success = false,
                    errorMessage = $"Command execution failed: {ex.Message}",
                    executionTime = DateTime.Now
                };
            }
        }

        #endregion

        #region Helper Classes

        private class CommandParseResult
        {
            public bool success;
            public string commandName;
            public Dictionary<string, object> parameters;
            public string errorMessage;
        }

        #endregion
    }
}