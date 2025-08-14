using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// UI component for individual input rebinding controls.
    /// Handles the interactive rebinding process for a specific input action and displays the current binding.
    /// </summary>
    public class InputRebindUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Input Configuration")]
        [SerializeField] private InputActionReference actionReference;
        [SerializeField] private int bindingIndex;
        
        [Header("UI References")]
        [SerializeField] private Text displayText;
        
        #endregion
        
        #region Private Fields
        
        private Button button;
        private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Initializes the UI component and sets up event listeners.
        /// </summary>
        private void Awake()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Cleans up rebinding operation when the component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            CleanupRebindingOperation();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Starts the interactive rebinding process for the assigned input action.
        /// </summary>
        public void StartRebind()
        {
            if (actionReference?.action == null)
            {
                Debug.LogError("ActionReference or its action is null!", this);
                return;
            }
            
            if (displayText != null)
            {
                displayText.text = "Press a key...";
            }
            
            // Disable the action during rebinding
            actionReference.action.Disable();
            
            // Clean up any existing rebinding operation
            CleanupRebindingOperation();
            
            // Start the rebinding operation
            rebindingOperation = actionReference.action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position") // Exclude mouse position to avoid accidental rebinds
                .WithCancelingThrough("<Keyboard>/escape") // Allow escape to cancel
                .OnMatchWaitForAnother(0.1f) // Wait briefly for additional input to handle composite bindings
                .OnComplete(OnRebindComplete)
                .OnCancel(OnRebindCancel)
                .Start();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Initializes the button component and display text.
        /// </summary>
        private void InitializeComponent()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(StartRebind);
            }
            else
            {
                Debug.LogError("InputRebindUI requires a Button component!", this);
            }
            
            UpdateDisplay();
        }
        
        /// <summary>
        /// Updates the display text to show the current key binding.
        /// </summary>
        private void UpdateDisplay()
        {
            if (displayText == null || actionReference?.action == null) return;
            
            if (bindingIndex >= 0 && bindingIndex < actionReference.action.bindings.Count)
            {
                displayText.text = actionReference.action.bindings[bindingIndex].ToDisplayString();
            }
            else
            {
                displayText.text = "Invalid Binding";
                Debug.LogError($"Binding index {bindingIndex} is out of range for action {actionReference.action.name}!", this);
            }
        }
        
        /// <summary>
        /// Called when the rebinding operation completes successfully.
        /// </summary>
        /// <param name="operation">The completed rebinding operation</param>
        private void OnRebindComplete(InputActionRebindingExtensions.RebindingOperation operation)
        {
            // Re-enable the action
            actionReference.action.Enable();
            
            // Save the rebindings
            if (Laboratory.Infrastructure.Input.InputRebindManager.Instance != null)
            {
                Laboratory.Infrastructure.Input.InputRebindManager.Instance.SaveRebinds();
            }
            
            // Update the display and clean up
            UpdateDisplay();
            CleanupRebindingOperation();
        }
        
        /// <summary>
        /// Called when the rebinding operation is cancelled.
        /// </summary>
        /// <param name="operation">The cancelled rebinding operation</param>
        private void OnRebindCancel(InputActionRebindingExtensions.RebindingOperation operation)
        {
            // Re-enable the action
            actionReference.action.Enable();
            
            // Update the display and clean up
            UpdateDisplay();
            CleanupRebindingOperation();
        }
        
        /// <summary>
        /// Cleans up the current rebinding operation if it exists.
        /// </summary>
        private void CleanupRebindingOperation()
        {
            if (rebindingOperation != null)
            {
                rebindingOperation.Dispose();
                rebindingOperation = null;
            }
        }
        
        #endregion
    }
}
