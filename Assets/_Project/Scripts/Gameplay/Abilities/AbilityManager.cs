using UnityEngine;

namespace Laboratory.Gameplay.Abilities
{
    /// <summary>
    /// Stub implementation for AbilityManager to resolve compilation errors.
    /// Replace with actual implementation when ready.
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        [SerializeField] private bool isActive = true;
        
        public bool IsActive => isActive;
        
        public void Initialize()
        {
            Debug.Log("AbilityManager stub initialized");
        }
        
        public void Shutdown()
        {
            Debug.Log("AbilityManager stub shutdown");
        }
    }
}
