using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// Represents a marker instance on the minimap with associated UI components.
    /// </summary>
    [System.Serializable]
    public class MarkerInstance : MonoBehaviour
    {
        #region Properties

        /// <summary>
        /// GameObject reference for this marker.
        /// </summary>
        public GameObject GameObject => gameObject;

        /// <summary>
        /// RectTransform component for positioning.
        /// </summary>
        public RectTransform RectTransform => (RectTransform)transform;

        /// <summary>
        /// Canvas group for fade animations.
        /// </summary>
        public CanvasGroup CanvasGroup;

        /// <summary>
        /// World transform this marker is tracking.
        /// </summary>
        [HideInInspector] public Transform WorldTransform;

        /// <summary>
        /// Whether this is a player marker or enemy marker.
        /// </summary>
        [HideInInspector] public bool IsPlayerMarker;

        #endregion
    }

    /// <summary>
    /// UI component for minimap functionality including zoom, pan, marker tracking, and click-to-move.
    /// Provides real-time world position tracking with smooth camera controls.
    /// </summary>
    public class MiniMapUI : MonoBehaviour
    {
        #region Fields

        [Header("Core References")]
        [SerializeField] private Camera miniMapCamera;
        [SerializeField] private RectTransform markerContainer;
        [SerializeField] private CanvasGroup minimapCanvasGroup;

        [Header("Marker Prefabs")]
        [SerializeField] private MarkerInstance playerMarkerPrefab;
        [SerializeField] private MarkerInstance enemyMarkerPrefabSmall;
        [SerializeField] private MarkerInstance enemyMarkerPrefabLarge;

        [Header("Click to Move")]
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private Transform playerTransform;

        [Header("Camera Settings")]
        [SerializeField, Min(1f)] private float zoomSpeed = 2f;
        [SerializeField, Min(0.1f)] private float minZoom = 10f;
        [SerializeField, Min(0.1f)] private float maxZoom = 100f;
        [SerializeField, Tooltip("Speed multiplier for camera panning (higher values = faster panning)"), Min(0.1f)] 
        private float panSpeed = 10f;
        [SerializeField, Tooltip("Smooth transition time for camera movements"), Min(0.1f)] 
        private float smoothTime = 0.2f;

        [Header("Map Boundaries")]
        [SerializeField] private Vector2 minBoundary = new(-50, -50);
        [SerializeField] private Vector2 maxBoundary = new(50, 50);

        // Input handling variables
        private Vector3 _panOrigin;
        private bool _isPanning;
        private Vector3 _panVelocity;
        private Vector2 _lastMousePosition;

        private readonly Dictionary<Transform, MarkerInstance> _activeMarkers = new();
        private readonly Queue<MarkerInstance> _playerMarkerPool = new();
        private readonly Queue<MarkerInstance> _enemyMarkerPool = new();
        private readonly Queue<MarkerInstance> _enemyMarkerPoolLarge = new();

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize input controls and setup event handlers.
        /// </summary>
        private void Awake()
        {
            SetupInputControls();
        }

        /// <summary>
        /// Enable input controls.
        /// </summary>
        private void OnEnable() 
        {
            // Input system is handled in Update method
        }

        /// <summary>
        /// Disable input controls.
        /// </summary>
        private void OnDisable() 
        {
            _isPanning = false;
        }

        /// <summary>
        /// Cleanup input controls when destroyed.
        /// </summary>
        private void OnDestroy() 
        {
            _isPanning = false;
        }

        /// <summary>
        /// Update marker positions, rotations, and handle input.
        /// </summary>
        private void Update() 
        {
            UpdateMarkers();
            HandleInput();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Register an entity to track on the minimap.
        /// </summary>
        /// <param name="worldTransform">Entity's world transform</param>
        /// <param name="isPlayer">True if player, false if enemy</param>
        /// <param name="isLargeEnemy">If enemy, true for large enemy marker size</param>
        public void RegisterTrackedEntity(Transform worldTransform, bool isPlayer, bool isLargeEnemy = false)
        {
            if (_activeMarkers.ContainsKey(worldTransform)) return;

            MarkerInstance marker = GetMarkerFromPool(isPlayer, isLargeEnemy);
            ConfigureMarker(marker, worldTransform, isPlayer);

            _activeMarkers.Add(worldTransform, marker);
            _ = FadeMarker(marker, true);
        }

        /// <summary>
        /// Unregister an entity from minimap tracking.
        /// </summary>
        /// <param name="worldTransform">Entity's world transform</param>
        public void UnregisterTrackedEntity(Transform worldTransform)
        {
            if (!_activeMarkers.TryGetValue(worldTransform, out var marker)) return;

            FadeMarker(marker, false).ConfigureAwait(false);

            _activeMarkers.Remove(worldTransform);
        }

        /// <summary>
        /// Set the player transform for tracking and click-to-move functionality.
        /// </summary>
        /// <param name="playerTransform">The player's transform to track</param>
        public void SetPlayerTransform(Transform playerTransform)
        {
            this.playerTransform = playerTransform;
            
            // Update camera to follow player if we have one
            if (miniMapCamera != null && playerTransform != null)
            {
                Vector3 cameraPos = miniMapCamera.transform.position;
                cameraPos.x = playerTransform.position.x;
                cameraPos.z = playerTransform.position.z;
                miniMapCamera.transform.position = ClampPositionToBounds(cameraPos);
            }
        }

        /// <summary>
        /// Gets the current pan speed value.
        /// </summary>
        public float PanSpeed => panSpeed;

        /// <summary>
        /// Sets the pan speed for camera movement.
        /// </summary>
        /// <param name="speed">Pan speed multiplier (higher values = faster panning)</param>
        public void SetPanSpeed(float speed)
        {
            panSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// Sets the zoom limits for the minimap camera.
        /// </summary>
        /// <param name="min">Minimum zoom level (orthographic size)</param>
        /// <param name="max">Maximum zoom level (orthographic size)</param>
        public void SetZoomLimits(float min, float max)
        {
            minZoom = Mathf.Max(0.1f, min);
            maxZoom = Mathf.Max(minZoom, max);
        }

        /// <summary>
        /// Instantly pan the camera to a target position (bypasses smooth panning).
        /// </summary>
        /// <param name="targetPosition">World position to pan to</param>
        public void PanToPosition(Vector3 targetPosition)
        {
            if (miniMapCamera == null) return;
            
            Vector3 newPos = miniMapCamera.transform.position;
            newPos.x = targetPosition.x;
            newPos.z = targetPosition.z;
            miniMapCamera.transform.position = ClampPositionToBounds(newPos);
        }

        #endregion

        #region Private Methods - Input Setup

        /// <summary>
        /// Setup input control bindings.
        /// </summary>
        private void SetupInputControls()
        {
            _isPanning = false;
            _lastMousePosition = Vector2.zero;
        }

        /// <summary>
        /// Handle all input processing for the minimap.
        /// </summary>
        private void HandleInput()
        {
            if (!miniMapCamera) return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            bool isMouseOverUI = IsMouseOverMinimap(mousePosition);
            
            // Handle zoom input (mouse wheel)
            if (isMouseOverUI && Mouse.current.scroll.ReadValue().y != 0)
            {
                float scrollDelta = Mouse.current.scroll.ReadValue().y / 120f; // Normalize scroll wheel delta
                OnZoomInput(scrollDelta);
            }

            // Handle pan input (middle mouse button or right mouse button)
            if (Mouse.current.middleButton.wasPressedThisFrame || 
                (Mouse.current.rightButton.wasPressedThisFrame && isMouseOverUI))
            {
                StartPan(mousePosition);
            }
            
            if (_isPanning)
            {
                if (Mouse.current.middleButton.isPressed || Mouse.current.rightButton.isPressed)
                {
                    OnPanInput(mousePosition);
                }
                else
                {
                    EndPan();
                }
            }

            // Handle click-to-move (left mouse button)
            if (isMouseOverUI && Mouse.current.leftButton.wasPressedThisFrame)
            {
                OnMiniMapClick(mousePosition);
            }

            _lastMousePosition = mousePosition;
        }

        /// <summary>
        /// Check if mouse position is over the minimap UI element.
        /// </summary>
        /// <param name="mousePosition">Current mouse screen position</param>
        /// <returns>True if mouse is over minimap</returns>
        private bool IsMouseOverMinimap(Vector2 mousePosition)
        {
            if (!minimapCanvasGroup || !minimapCanvasGroup.gameObject.activeInHierarchy)
                return false;

            RectTransform minimapRect = minimapCanvasGroup.GetComponent<RectTransform>();
            if (!minimapRect) return false;

            return RectTransformUtility.RectangleContainsScreenPoint(minimapRect, mousePosition);
        }

        #endregion

        #region Private Methods - Camera Controls

        /// <summary>
        /// Handle zoom input from mouse wheel or gamepad.
        /// </summary>
        /// <param name="zoomDelta">Zoom input delta</param>
        private void OnZoomInput(float zoomDelta)
        {
            if (!miniMapCamera) return;

            float targetZoom = Mathf.Clamp(
                miniMapCamera.orthographicSize - zoomDelta * zoomSpeed, 
                minZoom, 
                maxZoom);
                
            _ = SmoothZoom(targetZoom);
        }

        /// <summary>
        /// Smoothly zoom the camera to target zoom level.
        /// </summary>
        /// <param name="targetZoom">Target orthographic size</param>
        private async Task SmoothZoom(float targetZoom)
        {
            float startZoom = miniMapCamera.orthographicSize;
            float elapsed = 0f;

            while (elapsed < smoothTime)
            {
                elapsed += Time.unscaledDeltaTime;
                miniMapCamera.orthographicSize = Mathf.SmoothStep(startZoom, targetZoom, elapsed / smoothTime);
                await Task.Yield();
            }

            miniMapCamera.orthographicSize = targetZoom;
        }

        /// <summary>
        /// Begin camera panning operation.
        /// </summary>
        /// <param name="startPos">Starting screen position</param>
        private void StartPan(Vector2 startPos)
        {
            _isPanning = true;
            _panOrigin = ScreenToWorldXZ(startPos);
        }

        /// <summary>
        /// End camera panning operation.
        /// </summary>
        private void EndPan() => _isPanning = false;

        /// <summary>
        /// Handle pan input during panning operation.
        /// </summary>
        /// <param name="currentPos">Current screen position</param>
        private void OnPanInput(Vector2 currentPos)
        {
            if (!_isPanning) return;

            Vector3 currentWorldPos = ScreenToWorldXZ(currentPos);
            Vector3 panDelta = _panOrigin - currentWorldPos;
            
            // Apply pan speed multiplier to the delta
            Vector3 targetPos = miniMapCamera.transform.position + (panDelta * panSpeed * Time.unscaledDeltaTime);
            targetPos = ClampPositionToBounds(targetPos);

            _ = SmoothPan(targetPos);
        }

        /// <summary>
        /// Smoothly pan camera to target position.
        /// </summary>
        /// <param name="targetPos">Target world position</param>
        private async Task SmoothPan(Vector3 targetPos)
        {
            while ((miniMapCamera.transform.position - targetPos).sqrMagnitude > 0.001f)
            {
                miniMapCamera.transform.position = Vector3.SmoothDamp(
                    miniMapCamera.transform.position,
                    targetPos,
                    ref _panVelocity,
                    smoothTime
                );
                await Task.Yield();
            }

            miniMapCamera.transform.position = targetPos;
        }

        /// <summary>
        /// Clamp camera position to defined boundaries.
        /// </summary>
        /// <param name="position">World position to clamp</param>
        /// <returns>Clamped position</returns>
        private Vector3 ClampPositionToBounds(Vector3 position)
        {
            return new Vector3(
                Mathf.Clamp(position.x, minBoundary.x, maxBoundary.x),
                position.y,
                Mathf.Clamp(position.z, minBoundary.y, maxBoundary.y)
            );
        }

        /// <summary>
        /// Convert screen position to world XZ coordinates.
        /// </summary>
        /// <param name="screenPos">Screen position</param>
        /// <returns>World position on XZ plane</returns>
        private Vector3 ScreenToWorldXZ(Vector2 screenPos)
        {
            Ray ray = miniMapCamera.ScreenPointToRay(screenPos);
            Plane groundPlane = new(Vector3.up, new Vector3(0, miniMapCamera.transform.position.y, 0));

            return groundPlane.Raycast(ray, out float enter) 
                ? ray.GetPoint(enter) 
                : miniMapCamera.transform.position;
        }

        #endregion

        #region Private Methods - Marker Management

        /// <summary>
        /// Configure marker properties and activation.
        /// </summary>
        /// <param name="marker">Marker to configure</param>
        /// <param name="worldTransform">World transform to track</param>
        /// <param name="isPlayer">Whether this is a player marker</param>
        private void ConfigureMarker(MarkerInstance marker, Transform worldTransform, bool isPlayer)
        {
            marker.GameObject.SetActive(true);
            marker.IsPlayerMarker = isPlayer;
            marker.WorldTransform = worldTransform;
        }

        /// <summary>
        /// Update all active marker positions and rotations.
        /// </summary>
        private void UpdateMarkers()
        {
            foreach (var kvp in _activeMarkers)
            {
                if (!kvp.Key) 
                { 
                    UnregisterTrackedEntity(kvp.Key); 
                    continue; 
                }

                UpdateMarkerTransform(kvp.Value, kvp.Key);
            }
        }

        /// <summary>
        /// Update individual marker position and rotation.
        /// </summary>
        /// <param name="marker">Marker to update</param>
        /// <param name="worldTransform">World transform being tracked</param>
        private void UpdateMarkerTransform(MarkerInstance marker, Transform worldTransform)
        {
            // Update position
            Vector3 pos = WorldToMiniMapPosition(worldTransform.position);
            marker.RectTransform.anchoredPosition = pos;

            // Update rotation
            float angle = Mathf.Atan2(worldTransform.forward.x, worldTransform.forward.z) * Mathf.Rad2Deg;
            marker.RectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
        }

        /// <summary>
        /// Convert world position to minimap UI coordinates.
        /// </summary>
        /// <param name="worldPos">World position to convert</param>
        /// <returns>Minimap UI position</returns>
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

        /// <summary>
        /// Get marker instance from appropriate object pool.
        /// </summary>
        /// <param name="isPlayer">Whether marker is for player</param>
        /// <param name="isLargeEnemy">Whether marker is for large enemy</param>
        /// <returns>Available marker instance</returns>
        private MarkerInstance GetMarkerFromPool(bool isPlayer, bool isLargeEnemy)
        {
            if (isPlayer)
            {
                return _playerMarkerPool.Count > 0 
                    ? _playerMarkerPool.Dequeue() 
                    : Instantiate(playerMarkerPrefab, markerContainer);
            }

            var pool = isLargeEnemy ? _enemyMarkerPoolLarge : _enemyMarkerPool;
            if (pool.Count > 0) return pool.Dequeue();

            var prefab = isLargeEnemy ? enemyMarkerPrefabLarge : enemyMarkerPrefabSmall;
            return Instantiate(prefab, markerContainer);
        }

        /// <summary>
        /// Return marker to appropriate object pool.
        /// </summary>
        /// <param name="marker">Marker to return to pool</param>
        private void ReturnMarkerToPool(MarkerInstance marker)
        {
            if (marker.IsPlayerMarker)
                _playerMarkerPool.Enqueue(marker);
            else if (marker == enemyMarkerPrefabLarge)
                _enemyMarkerPoolLarge.Enqueue(marker);
            else
                _enemyMarkerPool.Enqueue(marker);
        }

        /// <summary>
        /// Fade marker in or out with animation.
        /// </summary>
        /// <param name="marker">Marker to fade</param>
        /// <param name="fadeIn">True to fade in, false to fade out</param>
        private async Task FadeMarker(MarkerInstance marker, bool fadeIn)
        {
            CanvasGroup cg = marker.CanvasGroup;
            cg.alpha = fadeIn ? 0f : 1f;

            float elapsed = 0f;
            const float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(fadeIn ? 0f : 1f, fadeIn ? 1f : 0f, elapsed / duration);
                await Task.Delay(16); // ~60fps frame delay
            }

            cg.alpha = fadeIn ? 1f : 0f;
        }

        #endregion

        #region Private Methods - Click to Move

        /// <summary>
        /// Handle minimap click for player movement.
        /// </summary>
        /// <param name="screenPosition">Screen position of click</param>
        private void OnMiniMapClick(Vector2 screenPosition)
        {
            Vector3 worldPos = ScreenToWorldPosition(screenPosition);
            if (worldPos != Vector3.zero && playerTransform != null)
                playerTransform.position = worldPos;
        }

        /// <summary>
        /// Convert screen position to world position via raycast.
        /// </summary>
        /// <param name="screenPosition">Screen position to convert</param>
        /// <returns>World position or Vector3.zero if no hit</returns>
        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            Ray ray = miniMapCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
                return hit.point;
                
            return Vector3.zero;
        }

        #endregion
    }
}
