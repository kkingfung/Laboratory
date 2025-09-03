using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Laboratory.UI.Utils
{
    /// <summary>
    /// Automatically closes UI panels when clicking outside the designated area or pressing Escape key.
    /// Implements IPointerClickHandler to detect clicks outside the popup area.
    /// </summary>
    public class UIAutoClose : MonoBehaviour, IPointerClickHandler
    {
        #region Fields

        [Header("Auto Close Configuration")]
        [Tooltip("The RectTransform area to consider 'inside' the popup.")]
        [SerializeField] private RectTransform popupArea;

        [Tooltip("Close this GameObject when triggered.")]
        [SerializeField] private bool disableOnClose = true;

        [Header("Events")]
        [Tooltip("Optional callback when popup closes.")]
        public Action OnClose;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Check for Escape key input to close the popup.
        /// </summary>
        private void Update()
        {
            if (Laboratory.UI.Input.InputSystem.GetKeyDown(KeyCode.Escape))
            {
                ClosePopup();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Detects clicks outside popupArea to close the popup.
        /// </summary>
        /// <param name="eventData">Pointer event data containing click information</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (popupArea == null)
            {
                Debug.LogWarning("UIAutoClose: popupArea not assigned.");
                return;
            }

            Vector2 localMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                popupArea, 
                eventData.position, 
                eventData.pressEventCamera, 
                out localMousePos
            );

            if (!popupArea.rect.Contains(localMousePos))
            {
                ClosePopup();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Closes the popup by invoking callbacks and optionally disabling the GameObject.
        /// </summary>
        private void ClosePopup()
        {
            OnClose?.Invoke();

            if (disableOnClose)
            {
                gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
