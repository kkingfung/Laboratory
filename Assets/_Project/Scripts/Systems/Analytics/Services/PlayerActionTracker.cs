using UnityEngine;
using System.Collections.Generic;
using Laboratory.Shared.Types;
using Laboratory.Core;
using Laboratory.Core.Enums;

namespace Laboratory.Systems.Analytics.Services
{
    /// <summary>
    /// Tracks player actions and interactions with detailed metadata.
    /// Extracted from PlayerAnalyticsTracker for single responsibility.
    /// </summary>
    public class PlayerActionTracker
    {
        // Events
        public System.Action<PlayerAction> OnActionTracked;

        // Action tracking
        private Queue<PlayerAction> _recentActions = new Queue<PlayerAction>(100);
        private Dictionary<ActionType, int> _actionTypeCounters = new Dictionary<ActionType, int>();
        private float _lastActionTime;
        private float _actionTrackingInterval;
        private int _totalActionCount;

        // Configuration
        private bool _trackDetailedMetrics;

        public IReadOnlyCollection<PlayerAction> RecentActions => _recentActions;
        public int TotalActionCount => _totalActionCount;
        public float TimeSinceLastAction => Time.time - _lastActionTime;

        public PlayerActionTracker(float actionTrackingInterval = GameConstants.ACTION_TRACKING_INTERVAL, bool trackDetailedMetrics = true)
        {
            _actionTrackingInterval = actionTrackingInterval;
            _trackDetailedMetrics = trackDetailedMetrics;
        }

        /// <summary>
        /// Tracks a player action with metadata
        /// </summary>
        public PlayerAction TrackAction(string actionType, string context, Dictionary<ParamKey, object> parameters = null)
        {
            var action = new PlayerAction
            {
                actionId = (uint)(_totalActionCount + 1),
                actionType = actionType,
                timestamp = Time.time,
                context = context,
                parameters = parameters ?? new Dictionary<ParamKey, object>()
            };

            RecordAction(action);
            return action;
        }

        /// <summary>
        /// Tracks a gameplay choice
        /// </summary>
        public PlayerAction TrackGameplayChoice(ChoiceCategory category, string choice, Dictionary<ParamKey, object> context = null)
        {
            var parameters = context ?? new Dictionary<ParamKey, object>();
            parameters[ParamKey.Category] = category;
            parameters[ParamKey.Choice] = choice;

            return TrackAction("GameplayChoice", $"Choice: {choice}", parameters);
        }

        /// <summary>
        /// Tracks a UI interaction
        /// </summary>
        public PlayerAction TrackUIInteraction(string elementName, string interactionType, Dictionary<ParamKey, object> context = null)
        {
            var parameters = context ?? new Dictionary<ParamKey, object>();
            parameters[ParamKey.ElementName] = elementName;
            parameters[ParamKey.InteractionType] = interactionType;

            return TrackAction("UIInteraction", $"{interactionType} on {elementName}", parameters);
        }

        /// <summary>
        /// Records a player action
        /// </summary>
        private void RecordAction(PlayerAction action)
        {
            // Rate limiting check
            if (Time.time - _lastActionTime < _actionTrackingInterval)
            {
                return;
            }

            // Update counters
            _totalActionCount++;
            _lastActionTime = Time.time;

            // Track action type count
            if (!string.IsNullOrEmpty(action.actionType))
            {
                if (System.Enum.TryParse<ActionType>(action.actionType, out var actionType))
                {
                    if (!_actionTypeCounters.ContainsKey(actionType))
                    {
                        _actionTypeCounters[actionType] = 0;
                    }
                    _actionTypeCounters[actionType]++;
                }
            }

            // Add to recent actions queue
            _recentActions.Enqueue(action);
            if (_recentActions.Count > 100)
            {
                _recentActions.Dequeue();
            }

            // Notify listeners
            OnActionTracked?.Invoke(action);
        }

        /// <summary>
        /// Gets the count for a specific action type
        /// </summary>
        public int GetActionTypeCount(ActionType actionType)
        {
            return _actionTypeCounters.ContainsKey(actionType) ? _actionTypeCounters[actionType] : 0;
        }

        /// <summary>
        /// Gets all action type counts
        /// </summary>
        public IReadOnlyDictionary<ActionType, int> GetActionTypeCounts()
        {
            return _actionTypeCounters;
        }

        /// <summary>
        /// Calculates actions per minute
        /// </summary>
        public float CalculateActionsPerMinute(float duration)
        {
            return duration > 0 ? (_totalActionCount / (duration / 60f)) : 0f;
        }

        /// <summary>
        /// Resets action tracking (for new session)
        /// </summary>
        public void Reset()
        {
            _recentActions.Clear();
            _actionTypeCounters.Clear();
            _totalActionCount = 0;
            _lastActionTime = 0f;
        }

        /// <summary>
        /// Gets action statistics
        /// </summary>
        public ActionStats GetActionStats()
        {
            return new ActionStats
            {
                totalActions = _totalActionCount,
                uniqueActionTypes = _actionTypeCounters.Count,
                mostCommonActionType = GetMostCommonActionType(),
                recentActionCount = _recentActions.Count,
                timeSinceLastAction = TimeSinceLastAction
            };
        }

        private ActionType GetMostCommonActionType()
        {
            if (_actionTypeCounters.Count == 0)
                return ActionType.Exploration;

            ActionType mostCommon = ActionType.Exploration;
            int maxCount = 0;

            foreach (var kvp in _actionTypeCounters)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    mostCommon = kvp.Key;
                }
            }

            return mostCommon;
        }
    }

    /// <summary>
    /// Action statistics summary
    /// </summary>
    public struct ActionStats
    {
        public int totalActions;
        public int uniqueActionTypes;
        public ActionType mostCommonActionType;
        public int recentActionCount;
        public float timeSinceLastAction;
    }
}
