using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
// FIXME: tidyup after 8/29
public class MiniMapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera miniMapCamera = null!;
    [SerializeField] private RectTransform markerContainer = null!; // UI container for markers
    [SerializeField] private CanvasGroup minimapCanvasGroup = null!; // For overall minimap fade, optional

    [Header("Settings")]
    [SerializeField, Min(1f)] private float zoomSpeed = 2f;
    [SerializeField, Min(0.1f)] private float minZoom = 10f;
    [SerializeField, Min(0.1f)] private float maxZoom = 100f;
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private float smoothTime = 0.2f;

    [Header("Map Boundaries")]
    [SerializeField] private Vector2 minBoundary = new Vector2(-50, -50);
    [SerializeField] private Vector2 maxBoundary = new Vector2(50, 50);

    [Header("Prefabs")]
    [SerializeField] private GameObject playerMarkerPrefab = null!;
   [SerializeField] private GameObject enemyMarkerPrefabSmall = null!;
    [SerializeField] private GameObject enemyMarkerPrefabLarge = null!;

    [Header("Click to Move")]
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private Transform playerTransform = null!; // Player to move


    private PlayerControls _controls = null!;

    private Vector3 _panOrigin;
    private bool _isPanning = false;

    private readonly Dictionary<Transform, MarkerInstance> _activeMarkers = new();
    private readonly Queue<MarkerInstance> _playerMarkerPool = new();
    private readonly Queue<MarkerInstance> _enemyMarkerPool = new();
    private readonly Queue<MarkerInstance> _enemyMarkerPoolLarge = new();

    // For smooth movement
    private Vector3 _panVelocity = Vector3.zero;
    private float _zoomVelocity = 0f;

    private void Awake()
    {
        _controls = new PlayerControls();

        _controls.MiniMap.Zoom.performed += ctx => OnZoomInput(ctx.ReadValue<float>());
        _controls.MiniMap.Pan.started += ctx => StartPan(ctx.ReadValue<Vector2>());
        _controls.MiniMap.Pan.canceled += ctx => EndPan();
        _controls.MiniMap.Pan.performed += ctx => OnPanInput(ctx.ReadValue<Vector2>());
        _controls.MiniMap.Click.performed += ctx => OnMiniMapClick(ctx.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        _controls.Enable();
    }

    private void OnDisable()
    {
        _controls.Disable();
    }

    private void Update()
    {
        UpdateMarkers();
    }

    private void OnZoomInput(float zoomDelta)
    {
        if (miniMapCamera == null) return;

        float targetZoom = Mathf.Clamp(miniMapCamera.orthographicSize - zoomDelta * zoomSpeed, minZoom, maxZoom);
        SmoothZoom(targetZoom).Forget();
    }

    private async UniTask SmoothZoom(float targetZoom)
    {
        float startZoom = miniMapCamera.orthographicSize;
        float elapsed = 0f;

        while (elapsed < smoothTime)
        {
            elapsed += Time.unscaledDeltaTime;
            miniMapCamera.orthographicSize = Mathf.SmoothStep(startZoom, targetZoom, elapsed / smoothTime);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        miniMapCamera.orthographicSize = targetZoom;
    }

    private void StartPan(Vector2 startPos)
    {
        _isPanning = true;
        _panOrigin = ScreenToWorldXZ(startPos);
    }

    private void EndPan()
    {
        _isPanning = false;
    }

    private void OnPanInput(Vector2 currentPos)
    {
        if (!_isPanning) return;

        Vector3 currentWorldPos = ScreenToWorldXZ(currentPos);
        Vector3 difference = _panOrigin - currentWorldPos;
        Vector3 targetPos = miniMapCamera.transform.position + difference;

        targetPos = ClampPositionToBounds(targetPos);

        SmoothPan(targetPos).Forget();
    }

    private async UniTask SmoothPan(Vector3 targetPos)
    {
        Vector3 startPos = miniMapCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < smoothTime)
        {
            elapsed += Time.unscaledDeltaTime;
            miniMapCamera.transform.position = Vector3.SmoothDamp(miniMapCamera.transform.position, targetPos, ref _panVelocity, smoothTime);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        miniMapCamera.transform.position = targetPos;
    }

    private Vector3 ClampPositionToBounds(Vector3 position)
    {
        float clampedX = Mathf.Clamp(position.x, minBoundary.x, maxBoundary.x);
        float clampedZ = Mathf.Clamp(position.z, minBoundary.y, maxBoundary.y);

        return new Vector3(clampedX, position.y, clampedZ);
    }

    private Vector3 ScreenToWorldXZ(Vector2 screenPos)
    {
        // Convert screen position to world position on XZ plane at camera height
        Ray ray = miniMapCamera.ScreenPointToRay(screenPos);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, miniMapCamera.transform.position.y, 0));
        if (groundPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return miniMapCamera.transform.position;
    }

     #region Marker Management

   /// <summary>
    /// Register an entity to track.
    /// </summary>
    /// <param name="worldTransform">Entity's world transform.</param>
    /// <param name="isPlayer">True if player, false if enemy.</param>
    /// <param name="isLargeEnemy">If enemy, true for large enemy marker size.</param>
    public void RegisterTrackedEntity(Transform worldTransform, bool isPlayer, bool isLargeEnemy = false)
    {
        if (_activeMarkers.ContainsKey(worldTransform))
            return;

        MarkerInstance marker = GetMarkerFromPool(isPlayer, isLargeEnemy);
        marker.GameObject.SetActive(true);
        marker.IsPlayerMarker = isPlayer;
        _activeMarkers.Add(worldTransform, marker);
        marker.WorldTransform = worldTransform;

        // Start fade-in
        FadeMarker(marker, true).Forget();
    }

    public void UnregisterTrackedEntity(Transform worldTransform)
    {
        if (!_activeMarkers.TryGetValue(worldTransform, out var marker))
            return;

        // Start fade-out and then deactivate and pool
        FadeMarker(marker, false).ContinueWith(() =>
        {
            marker.GameObject.SetActive(false);
            ReturnMarkerToPool(marker);
        }).Forget();

        _activeMarkers.Remove(worldTransform);
    }

    private void UpdateMarkers()
    {
        foreach (var kvp in _activeMarkers)
        {
            Transform worldTransform = kvp.Key;
            MarkerInstance marker = kvp.Value;

            if (worldTransform == null)
            {
                UnregisterTrackedEntity(worldTransform);
                continue;
            }

            Vector3 localPos = WorldToMiniMapPosition(worldTransform.position);
            marker.RectTransform.anchoredPosition = localPos;

            // Rotate marker to match facing direction
            Vector3 forward = worldTransform.forward;
            float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            marker.RectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
        }
    }

    private Vector3 WorldToMiniMapPosition(Vector3 worldPos)
    {
        Vector3 viewportPos = miniMapCamera.WorldToViewportPoint(worldPos);
        viewportPos.x = Mathf.Clamp01(viewportPos.x);
        viewportPos.y = Mathf.Clamp01(viewportPos.y);

        float x = (viewportPos.x - 0.5f) * markerContainer.rect.width;
        float y = (viewportPos.y - 0.5f) * markerContainer.rect.height;
        return new Vector3(x, y, 0);
    }

    private MarkerInstance GetMarkerFromPool(bool isPlayer, bool isLargeEnemy)
    {
        if (isPlayer)
        {
            if (_playerMarkerPool.Count > 0)
                return _playerMarkerPool.Dequeue();
            else
                return CreateNewMarker(playerMarkerPrefab, true);
        }
        else
        {
            var pool = isLargeEnemy ? _enemyMarkerPoolLarge : enemyMarkerPoolSmall;
            if (pool.Count > 0)
                return pool.Dequeue();
            else
            {
                var prefab = isLargeEnemy ? enemyMarkerPrefabLarge : enemyMarkerPrefabSmall;
                return CreateNewMarker(prefab, false);
            }
        }
    }

    private MarkerInstance CreateNewMarker(GameObject prefab, bool isPlayer)
    {
        var go = Instantiate(prefab, markerContainer);
        return new MarkerInstance(go, isPlayer);
    }

    private void ReturnMarkerToPool(MarkerInstance marker)
    {
        if (marker.IsPlayerMarker)
            _playerMarkerPool.Enqueue(marker);
        else
        {
            if (marker.GameObject == enemyMarkerPrefabLarge) _enemyMarkerPoolLarge.Enqueue(marker);
            else enemyMarkerPoolSmall.Enqueue(marker);
        }
    }

    private async UniTask FadeMarker(MarkerInstance marker, bool fadeIn)
    {
        CanvasGroup cg = marker.CanvasGroup;
        if (cg == null)
        {
            cg = marker.GameObject.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = marker.GameObject.AddComponent<CanvasGroup>();
            }
            marker.CanvasGroup = cg;
        }

        float duration = 0.3f;
        float elapsed = 0f;
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;

        cg.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            await UniTask.Yield();
        }

        cg.alpha = endAlpha;
    }

    #endregion

    #region Click to Move

    private void OnMiniMapClick(Vector2 screenPosition)
    {
        Vector3 worldPos = ScreenToWorldPosition(screenPosition);
        if (worldPos == Vector3.zero) return;

        // Example: Move player to clicked position
        // You may want to use your movement system or send network commands instead
        playerTransform.position = worldPos;
    }

    private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
    {
        Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, miniMapCamera.transform.position.y);
        Ray ray = miniMapCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    #endregion

    private class MarkerInstance
    {
        public GameObject GameObject;
        public RectTransform RectTransform;
        public Transform WorldTransform = null!;
        public bool IsPlayerMarker;
        public CanvasGroup CanvasGroup;

        public MarkerInstance(GameObject go, bool isPlayer)
        {
            GameObject = go;
            RectTransform = go.GetComponent<RectTransform>();
            IsPlayerMarker = isPlayer;
            CanvasGroup = go.GetComponent<CanvasGroup>();
        }
    }
}
