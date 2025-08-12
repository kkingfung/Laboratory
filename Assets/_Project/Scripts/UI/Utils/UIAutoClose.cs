using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIAutoClose : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("The RectTransform area to consider 'inside' the popup.")]
    [SerializeField] private RectTransform popupArea;

    [Tooltip("Close this GameObject when triggered.")]
    [SerializeField] private bool disableOnClose = true;

    [Tooltip("Optional callback when popup closes.")]
    public System.Action? OnClose;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePopup();
        }
    }

    /// <summary>
    /// Detect clicks outside popupArea to close.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (popupArea == null)
        {
            Debug.LogWarning("UIAutoClose: popupArea not assigned.");
            return;
        }

        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(popupArea, eventData.position, eventData.pressEventCamera, out localMousePos);

        if (!popupArea.rect.Contains(localMousePos))
        {
            ClosePopup();
        }
    }

    private void ClosePopup()
    {
        OnClose?.Invoke();

        if (disableOnClose)
        {
            gameObject.SetActive(false);
        }
    }
}
