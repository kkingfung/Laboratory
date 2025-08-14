using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;

public class MiniMapUI : MonoBehaviour
{
    #region References

    [Header("Core")]
    [SerializeField] private Camera miniMapCamera;
    [SerializeField] private RectTransform markerContainer;
    [SerializeField] private CanvasGroup minimapCanvasGroup;

    [Header("Prefabs")]
    [SerializeField] private MarkerInstance playerMarkerPrefab;
    [SerializeField] private MarkerInstance enemyMarkerPrefabSmall;
    [SerializeField] private MarkerInstance enemyMarkerPrefabLarge;

    [Header("Click to Move")]
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private Transform playerTransform;

    #endregion

    #region Settings

    [Header("Map Settings")]
    [SerializeField, Min(1f)] private float zoomSpeed = 2f;
    [SerializeField, Min(0.1f)] private float minZoom = 10f;
    [SerializeField, Min(0.1f)] private float maxZoom = 100f;
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private float smoothTime = 0.2f;

    [Header("Boundaries")]
    [SerializeField] private Vector2 minBoundary = new(-50, -50);
    [SerializeField] private Vector2 maxBoundary = new(50, 50);

    #endregion

    #region Fields

    private PlayerControls _controls;
    private Vector3 _panOrigin;
    private bool _isPanning;

    private readonly Dictionary<Transform, MarkerInstance> _activeMarkers = new();
    private readonly Queue<MarkerInstance> _playerMarkerPool = new();
    private readonly Queue<MarkerInstance> _enemyMarkerPool = new();
    private readonly Queue<MarkerInstance> _enemyMarkerPoolLarge = new();

    private Vector3 _panVelocity;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        _controls = new PlayerControls();
        _controls.MiniMap.Zoom.performed += ctx => OnZoomInput(ctx.ReadValue<float>());
        _controls.MiniMap.Pan.started += ctx => StartPan(ctx.ReadValue<Vector2>());
        _controls.MiniMap.Pan.canceled += ctx => EndPan();
        _controls.MiniMap.Pan.performed += ctx => OnPanInput(ctx.ReadValue<Vector2>());
        _controls.MiniMap.Click.performed += ctx => OnMiniMapClick(ctx.ReadValue<Vector2>());
    }

    private void OnEnable() => _controls.Enable();
    private void OnDisable() => _controls.Disable();
    private void Update() => UpdateMarkers();

    #endregion

    #region Zoom & Pan

    private void OnZoomInput(float zoomDelta)
    {
        if (!miniMapCamera) return;

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

    private void EndPan() => _isPanning = false;

    private void OnPanInput(Vector2 currentPos)
    {
        if (!_isPanning) return;

        Vector3 targetPos = miniMapCamera.transform.position + (_panOrigin - ScreenToWorldXZ(currentPos));
        targetPos = ClampPositionToBounds(targetPos);

        SmoothPan(targetPos).Forget();
    }

    private async UniTask SmoothPan(Vector3 targetPos)
    {
        while ((miniMapCamera.transform.position - targetPos).sqrMagnitude > 0.001f)
        {
            miniMapCamera.transform.position = Vector3.SmoothDamp(
                miniMapCamera.transform.position,
                targetPos,
                ref _panVelocity,
                smoothTime
            );
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        miniMapCamera.transform.position = targetPos;
    }

    private Vector3 ClampPositionToBounds(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, minBoundary.x, maxBoundary.x),
            position.y,
            Mathf.Clamp(position.z, minBoundary.y, maxBoundary.y)
        );
    }

    private Vector3 ScreenToWorldXZ(Vector2 screenPos)
    {
        Ray ray = miniMapCamera.ScreenPointToRay(screenPos);
        Plane groundPlane = new(Vector3.up, new Vector3(0, miniMapCamera.transform.position.y, 0));

        return groundPlane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : miniMapCamera.transform.position;
    }

    #endregion

    #region Marker Management

   /// <summary>
    /// Register an entity to track.
    /// </summary>
    /// <param name="worldTransform">Entity's world transform.</param>
    /// <param name="isPlayer">True if player, false if enemy.</param>
    /// <param name="isLargeEnemy">If enemy, true for large enemy marker size.</param>
    public void RegisterTrackedEntity(Transform worldTransform, bool isPlayer, bool isLargeEnemy = false)
    {
        if (_activeMarkers.ContainsKey(worldTransform)) return;

        MarkerInstance marker = GetMarkerFromPool(isPlayer, isLargeEnemy);
        marker.GameObject.SetActive(true);
        marker.IsPlayerMarker = isPlayer;
        marker.WorldTransform = worldTransform;

        _activeMarkers.Add(worldTransform, marker);
        FadeMarker(marker, true).Forget();
    }

    public void UnregisterTrackedEntity(Transform worldTransform)
    {
        if (!_activeMarkers.TryGetValue(worldTransform, out var marker)) return;

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
            if (!kvp.Key) { UnregisterTrackedEntity(kvp.Key); continue; }

            Vector3 pos = WorldToMiniMapPosition(kvp.Key.position);
            kvp.Value.RectTransform.anchoredPosition = pos;

            float angle = Mathf.Atan2(kvp.Key.forward.x, kvp.Key.forward.z) * Mathf.Rad2Deg;
            kvp.Value.RectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
        }
    }

    private Vector3 WorldToMiniMapPosition(Vector3 worldPos)
    {
        Vector3 vp = miniMapCamera.WorldToViewportPoint(worldPos);
        vp.x = Mathf.Clamp01(vp.x);
        vp.y = Mathf.Clamp01(vp.y);

        return new Vector3(
            (vp.x - 0.5f) * markerContainer.rect.width,
            (vp.y - 0.5f) * markerContainer.rect.height,
            0
        );
    }

    private MarkerInstance GetMarkerFromPool(bool isPlayer, bool isLargeEnemy)
    {
        if (isPlayer) return _playerMarkerPool.Count > 0 ? _playerMarkerPool.Dequeue() : Instantiate(playerMarkerPrefab, markerContainer);
        var pool = isLargeEnemy ? _enemyMarkerPoolLarge : _enemyMarkerPool;
        if (pool.Count > 0) return pool.Dequeue();
        var prefab = isLargeEnemy ? enemyMarkerPrefabLarge : enemyMarkerPrefabSmall;
        return Instantiate(prefab, markerContainer);
    }

    private void ReturnMarkerToPool(MarkerInstance marker)
    {
        if (marker.IsPlayerMarker) _playerMarkerPool.Enqueue(marker);
        else if (marker == enemyMarkerPrefabLarge) _enemyMarkerPoolLarge.Enqueue(marker);
        else _enemyMarkerPool.Enqueue(marker);
    }

    private async UniTask FadeMarker(MarkerInstance marker, bool fadeIn)
    {
        CanvasGroup cg = marker.CanvasGroup;
        cg.alpha = fadeIn ? 0f : 1f;

        float elapsed = 0f;
        const float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(fadeIn ? 0f : 1f, fadeIn ? 1f : 0f, elapsed / duration);
            await UniTask.Yield();
        }

        cg.alpha = fadeIn ? 1f : 0f;
    }

    #endregion

    #region Click-to-Move

    private void OnMiniMapClick(Vector2 screenPosition)
    {
        Vector3 worldPos = ScreenToWorldPosition(screenPosition);
        if (worldPos != Vector3.zero) playerTransform.position = worldPos;
    }

    private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
    {
        Ray ray = miniMapCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask)) return hit.point;
        return Vector3.zero;
    }

    #endregion
}

[System.Serializable]
public class MarkerInstance : MonoBehaviour
{
    public GameObject GameObject => gameObject;
    public RectTransform RectTransform => (RectTransform)transform;
    public CanvasGroup CanvasGroup;
    [HideInInspector] public Transform WorldTransform;
    [HideInInspector] public bool IsPlayerMarker;
}
