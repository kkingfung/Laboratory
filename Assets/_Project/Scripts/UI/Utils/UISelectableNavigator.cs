using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Laboratory.UI.Utils
{
    /// <summary>
    /// Provides keyboard/gamepad navigation for UI Selectable elements with configurable input handling.
    /// Supports wrap-around navigation and customizable input delays to prevent overscrolling.
    /// </summary>
    public class UISelectableNavigator : MonoBehaviour
    {
        #region Fields

        [Header("Navigation Configuration")]
        [Tooltip("List of Selectable UI elements in navigation order.")]
        [SerializeField] private List<Selectable> selectables = new List<Selectable>();

        [Tooltip("Enable wrap-around navigation (from last to first and vice versa).")]
        [SerializeField] private bool wrapAround = true;

        [Header("Input Settings")]
        [Tooltip("Input axis name for horizontal navigation.")]
        [SerializeField] private string horizontalAxis = "Horizontal";

        [Tooltip("Input axis name for vertical navigation.")]
        [SerializeField] private string verticalAxis = "Vertical";

        [Tooltip("Time delay between navigation inputs to prevent overscrolling.")]
        [SerializeField] private float inputDelay = 0.2f;

        [Tooltip("Minimum input threshold to register as navigation input.")]
        [SerializeField] private float inputThreshold = 0.5f;

        private int _currentIndex = 0;
        private float _inputTimer = 0f;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the currently selected index in the selectables list.
        /// </summary>
        public int CurrentIndex => _currentIndex;

        /// <summary>
        /// Gets the currently selected Selectable component, or null if none.
        /// </summary>
        public Selectable CurrentSelectable => 
            _currentIndex >= 0 && _currentIndex < selectables.Count ? selectables[_currentIndex] : null;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize navigation by selecting the first valid selectable.
        /// </summary>
        private void Start()
        {
            if (selectables.Count == 0)
            {
                Debug.LogWarning("UISelectableNavigator: No selectables assigned.", this);
                return;
            }

            SetSelected(_currentIndex);
        }

        /// <summary>
        /// Handle navigation input with timing delays.
        /// </summary>
        private void Update()
        {
            if (selectables.Count == 0) 
                return;

            UpdateInputTimer();
            ProcessNavigationInput();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually navigates to the specified index.
        /// </summary>
        /// <param name="index">Target index in the selectables list</param>
        public void NavigateToIndex(int index)
        {
            if (index < 0 || index >= selectables.Count)
            {
                Debug.LogWarning($"UISelectableNavigator: Index {index} is out of range.", this);
                return;
            }

            _currentIndex = index;
            SetSelected(_currentIndex);
            ResetInputTimer();
        }

        /// <summary>
        /// Navigates to the next selectable in the list.
        /// </summary>
        public void NavigateNext()
        {
            Navigate(1);
        }

        /// <summary>
        /// Navigates to the previous selectable in the list.
        /// </summary>
        public void NavigatePrevious()
        {
            Navigate(-1);
        }

        /// <summary>
        /// Adds a new selectable to the navigation list.
        /// </summary>
        /// <param name="selectable">Selectable to add</param>
        public void AddSelectable(Selectable selectable)
        {
            if (selectable != null && !selectables.Contains(selectable))
            {
                selectables.Add(selectable);
            }
        }

        /// <summary>
        /// Removes a selectable from the navigation list.
        /// </summary>
        /// <param name="selectable">Selectable to remove</param>
        public void RemoveSelectable(Selectable selectable)
        {
            if (selectables.Contains(selectable))
            {
                int removedIndex = selectables.IndexOf(selectable);
                selectables.Remove(selectable);

                // Adjust current index if necessary
                if (_currentIndex >= removedIndex && _currentIndex > 0)
                {
                    _currentIndex--;
                }

                // Ensure we still have a valid selection
                if (selectables.Count > 0)
                {
                    _currentIndex = Mathf.Clamp(_currentIndex, 0, selectables.Count - 1);
                    SetSelected(_currentIndex);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the input timer to handle navigation delays.
        /// </summary>
        private void UpdateInputTimer()
        {
            _inputTimer -= Time.unscaledDeltaTime;
        }

        /// <summary>
        /// Processes input for navigation and triggers movement when appropriate.
        /// </summary>
        private void ProcessNavigationInput()
        {
            if (_inputTimer > 0f) 
                return;

            float verticalInput = Input.GetAxisRaw(verticalAxis);
            float horizontalInput = Input.GetAxisRaw(horizontalAxis);

            if (Mathf.Abs(verticalInput) > inputThreshold)
            {
                Navigate(verticalInput > 0 ? -1 : 1);
            }
            else if (Mathf.Abs(horizontalInput) > inputThreshold)
            {
                Navigate(horizontalInput > 0 ? 1 : -1);
            }
        }

        /// <summary>
        /// Navigates in the specified direction with wrap-around support.
        /// </summary>
        /// <param name="direction">Navigation direction (-1 for previous, 1 for next)</param>
        private void Navigate(int direction)
        {
            _currentIndex += direction;

            if (wrapAround)
            {
                if (_currentIndex < 0) 
                    _currentIndex = selectables.Count - 1;
                else if (_currentIndex >= selectables.Count) 
                    _currentIndex = 0;
            }
            else
            {
                _currentIndex = Mathf.Clamp(_currentIndex, 0, selectables.Count - 1);
            }

            SetSelected(_currentIndex);
            ResetInputTimer();
        }

        /// <summary>
        /// Sets the specified index as selected in the EventSystem.
        /// </summary>
        /// <param name="index">Index of selectable to select</param>
        private void SetSelected(int index)
        {
            if (index < 0 || index >= selectables.Count)
                return;

            Selectable selectable = selectables[index];
            if (IsSelectableValid(selectable))
            {
                EventSystem.current?.SetSelectedGameObject(selectable.gameObject);
            }
            else
            {
                // If current selectable is invalid, try to find a valid one
                TryFindValidSelectable();
            }
        }

        /// <summary>
        /// Checks if a selectable is valid for selection.
        /// </summary>
        /// <param name="selectable">Selectable to validate</param>
        /// <returns>True if selectable is valid and can be selected</returns>
        private bool IsSelectableValid(Selectable selectable)
        {
            return selectable != null && 
                   selectable.IsInteractable() && 
                   selectable.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Attempts to find and select a valid selectable when current selection is invalid.
        /// </summary>
        private void TryFindValidSelectable()
        {
            for (int i = 0; i < selectables.Count; i++)
            {
                if (IsSelectableValid(selectables[i]))
                {
                    _currentIndex = i;
                    EventSystem.current?.SetSelectedGameObject(selectables[i].gameObject);
                    return;
                }
            }
        }

        /// <summary>
        /// Resets the input timer to prevent immediate re-navigation.
        /// </summary>
        private void ResetInputTimer()
        {
            _inputTimer = inputDelay;
        }

        #endregion
    }
}
