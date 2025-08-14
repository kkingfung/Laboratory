using UnityEngine;

namespace Gameplay.Targeting
{
    /// <summary>
    /// Simple 2D outline by duplicating the sprite with offset and color.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TargetOutline2D : MonoBehaviour
    {
        [Header("Outline Settings")]
        [SerializeField] private Color outlineColor = Color.yellow;
        [SerializeField] private float outlineThickness = 0.05f;
        [SerializeField] private bool startDisabled = true;

        private SpriteRenderer _mainRenderer;
        private SpriteRenderer _outlineRenderer;
        private GameObject _outlineObject;

        private void Awake()
        {
            _mainRenderer = GetComponent<SpriteRenderer>();

            // Create outline object
            _outlineObject = new GameObject("Outline");
            _outlineObject.transform.SetParent(transform);
            _outlineObject.transform.localPosition = Vector3.zero;
            _outlineObject.transform.localScale = Vector3.one * (1 + outlineThickness);

            _outlineRenderer = _outlineObject.AddComponent<SpriteRenderer>();
            _outlineRenderer.sprite = _mainRenderer.sprite;
            _outlineRenderer.color = outlineColor;
            _outlineRenderer.sortingOrder = _mainRenderer.sortingOrder - 1;

            SetOutlineActive(!startDisabled);
        }

        public void SetOutlineActive(bool active)
        {
            if (_outlineObject != null)
                _outlineObject.SetActive(active);
        }

        public void SetOutlineColor(Color color)
        {
            outlineColor = color;
            if (_outlineRenderer != null)
                _outlineRenderer.color = outlineColor;
        }
    }
}
