using UnityEngine;
using UnityEngine.InputSystem;

namespace Laboratory.Infrastructure.Input
{
    /// <summary>
    /// Singleton manager for handling input rebinding functionality.
    /// Manages saving, loading, and resetting of custom key bindings using Unity's Input System.
    /// </summary>
    public class InputRebindManager : MonoBehaviour
    {
        #region Constants
        
        private const string REBINDS_KEY = "input_rebinds";
        
        #endregion
        
        #region Static Properties
        
        /// <summary>
        /// Singleton instance of the InputRebindManager.
        /// </summary>
        public static InputRebindManager Instance { get; private set; }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Input Configuration")]
        [SerializeField] private InputActionAsset inputActions;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Initializes the singleton instance and loads saved rebindings.
        /// </summary>
        private void Awake()
        {
            InitializeSingleton();
        }
        
        #endregion
        
        #region Public Methods - Save/Load Operations
        
        /// <summary>
        /// Saves the current input rebindings to PlayerPrefs.
        /// </summary>
        public void SaveRebinds()
        {
            if (inputActions == null)
            {
                Debug.LogError("InputActionAsset is not assigned!", this);
                return;
            }
            
            string rebinds = inputActions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(REBINDS_KEY, rebinds);
            PlayerPrefs.Save();
            
            Debug.Log("Input rebindings saved successfully.");
        }

        /// <summary>
        /// Loads input rebindings from PlayerPrefs if they exist.
        /// </summary>
        public void LoadRebinds()
        {
            if (inputActions == null)
            {
                Debug.LogError("InputActionAsset is not assigned!", this);
                return;
            }
            
            if (PlayerPrefs.HasKey(REBINDS_KEY))
            {
                string rebinds = PlayerPrefs.GetString(REBINDS_KEY);
                inputActions.LoadBindingOverridesFromJson(rebinds);
                Debug.Log("Input rebindings loaded successfully.");
            }
        }

        /// <summary>
        /// Resets all input rebindings to their default values and removes saved data.
        /// </summary>
        public void ResetRebinds()
        {
            if (inputActions == null)
            {
                Debug.LogError("InputActionAsset is not assigned!", this);
                return;
            }
            
            inputActions.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(REBINDS_KEY);
            PlayerPrefs.Save();
            
            Debug.Log("Input rebindings reset to defaults.");
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Initializes the singleton pattern and sets up the instance.
        /// </summary>
        private void InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple InputRebindManager instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadRebinds();
        }
        
        #endregion
    }
}
