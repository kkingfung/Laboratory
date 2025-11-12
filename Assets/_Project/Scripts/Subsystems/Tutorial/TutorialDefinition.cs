using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Laboratory.Tutorial
{
    /// <summary>
    /// ScriptableObject-based tutorial definition for designer-friendly tutorial creation.
    /// Allows non-programmers to create tutorials through the Unity Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTutorial", menuName = "Chimera/Tutorial Definition")]
    public class TutorialDefinition : ScriptableObject
    {
        [Header("Tutorial Info")]
        [Tooltip("Unique identifier for this tutorial")]
        public string tutorialId;

        [Tooltip("Display name shown to players")]
        public string tutorialName;

        [TextArea(2, 4)]
        [Tooltip("Brief description of what this tutorial teaches")]
        public string description;

        [Tooltip("Allow players to replay this tutorial after completion")]
        public bool allowReplay = true;

        [Header("Steps")]
        [Tooltip("Sequential steps that make up this tutorial")]
        public List<TutorialStepDefinition> steps = new List<TutorialStepDefinition>();

        [Header("Auto-Start")]
        [Tooltip("Automatically start this tutorial when conditions are met")]
        public bool autoStart = false;

        [Tooltip("Trigger to start tutorial (scene name, event, etc.)")]
        public string autoStartTrigger;

        /// <summary>
        /// Convert this definition to a runtime Tutorial object.
        /// </summary>
        public Tutorial ToTutorial()
        {
            var tutorial = new Tutorial
            {
                tutorialId = tutorialId,
                tutorialName = tutorialName,
                description = description,
                allowReplay = allowReplay
            };

            foreach (var stepDef in steps)
            {
                tutorial.steps.Add(stepDef.ToTutorialStep());
            }

            return tutorial;
        }

        /// <summary>
        /// Register this tutorial with the TutorialSystem.
        /// </summary>
        public void Register()
        {
            if (TutorialSystem.Instance != null)
            {
                TutorialSystem.Instance.RegisterTutorial(ToTutorial());
            }
            else
            {
                Debug.LogWarning($"[TutorialDefinition] TutorialSystem not found. Cannot register: {tutorialName}");
            }
        }

        private void OnValidate()
        {
            // Auto-generate ID from name if empty
            if (string.IsNullOrEmpty(tutorialId) && !string.IsNullOrEmpty(tutorialName))
            {
                tutorialId = tutorialName.ToLower().Replace(" ", "_");
            }
        }
    }

    /// <summary>
    /// A single tutorial step definition.
    /// </summary>
    [System.Serializable]
    public class TutorialStepDefinition
    {
        [Header("Content")]
        [Tooltip("Step title shown in UI")]
        public string title;

        [TextArea(3, 6)]
        [Tooltip("Detailed instructions for this step")]
        public string description;

        [Header("Completion")]
        [Tooltip("Type of completion required for this step")]
        public StepCompletionType completionType = StepCompletionType.Manual;

        [Tooltip("Auto-advance after this many seconds (0 = disabled)")]
        public float autoAdvanceDelay = 0f;

        [Tooltip("Event name to wait for (if using EventCompletion type)")]
        public string completionEventName;

        [Header("Highlighting")]
        [Tooltip("GameObject to highlight during this step")]
        public GameObject highlightTarget;

        [Tooltip("Block player interaction until step completes")]
        public bool blockInteraction = false;

        [Header("Events")]
        [Tooltip("Actions to perform when step starts")]
        public UnityEvent onStepStart;

        [Tooltip("Actions to perform when step completes")]
        public UnityEvent onStepComplete;

        /// <summary>
        /// Convert this definition to a runtime TutorialStep.
        /// </summary>
        public TutorialStep ToTutorialStep()
        {
            var step = new TutorialStep
            {
                title = title,
                description = description,
                autoAdvanceAfterDelay = autoAdvanceDelay,
                highlightTarget = highlightTarget,
                blockInteraction = blockInteraction,
                onStepStart = onStepStart,
                onStepComplete = onStepComplete
            };

            // Setup completion condition based on type
            switch (completionType)
            {
                case StepCompletionType.Manual:
                    step.requiresCondition = false;
                    break;

                case StepCompletionType.EventTriggered:
                    step.requiresCondition = true;
                    step.completionCondition = () => CheckEventTriggered(completionEventName);
                    break;

                case StepCompletionType.AutoAdvance:
                    step.requiresCondition = false;
                    break;
            }

            return step;
        }

        private bool CheckEventTriggered(string eventName)
        {
            // In production, implement event checking system
            // For now, this is a placeholder
            return false;
        }
    }

    /// <summary>
    /// Types of step completion.
    /// </summary>
    public enum StepCompletionType
    {
        Manual,          // Player clicks Next button
        EventTriggered,  // Waits for specific event
        AutoAdvance      // Automatically advances after delay
    }
}
