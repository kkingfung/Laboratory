using UnityEngine;

namespace Laboratory.Subsystems.Camera
{
    /// <summary>
    /// Subsystem manager for Camera system
    /// Follows Project Chimera architecture pattern
    /// </summary>
    public class CameraSubsystemManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CameraConfig config;

        [Header("Prefabs")]
        [SerializeField] private GameObject cameraPrefab;

        // Singleton
        private static CameraSubsystemManager _instance;
        public static CameraSubsystemManager Instance => _instance;

        // Active camera controller
        private CameraController _activeCamera;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeSubsystem();
        }

        /// <summary>
        /// Initialize camera subsystem
        /// </summary>
        private void InitializeSubsystem()
        {
            if (_activeCamera == null)
            {
                // Find existing camera controller or create one
                _activeCamera = FindFirstObjectByType<CameraController>();

                if (_activeCamera == null && cameraPrefab != null)
                {
                    GameObject cameraObj = Instantiate(cameraPrefab);
                    _activeCamera = cameraObj.GetComponent<CameraController>();
                }
            }

            Debug.Log("[CameraSubsystem] Initialized");
        }

        /// <summary>
        /// Get active camera controller
        /// </summary>
        public CameraController GetActiveCamera()
        {
            return _activeCamera;
        }

        /// <summary>
        /// Set active camera controller
        /// </summary>
        public void SetActiveCamera(CameraController camera)
        {
            _activeCamera = camera;
        }

        /// <summary>
        /// Get configuration
        /// </summary>
        public CameraConfig GetConfig()
        {
            return config;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
