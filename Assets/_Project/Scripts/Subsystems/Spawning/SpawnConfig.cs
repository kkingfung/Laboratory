using UnityEngine;

namespace Laboratory.Subsystems.Spawning
{
    /// <summary>
    /// Configuration for spawning behavior
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnConfig", menuName = "Laboratory/Spawning/Spawn Config")]
    public class SpawnConfig : ScriptableObject
    {
        [Header("Spawn Behavior")]
        public bool ApplyScale = false;
        public Vector3 SpawnScale = Vector3.one;
        
        [Header("Physics")]
        public bool ApplyInitialVelocity = false;
        public Vector3 InitialVelocity = Vector3.zero;
        
        [Header("Object Properties")]
        public bool OverrideLayer = false;
        public int SpawnLayer = 0;
        
        [Header("Timing")]
        public float SpawnDelay = 0f;
        public bool DestroyAfterTime = false;
        public float LifeTime = 10f;
        
        [Header("Effects")]
        public GameObject SpawnEffect;
        public AudioClip SpawnSound;
        
        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool IsValid()
        {
            return SpawnScale.x > 0 && SpawnScale.y > 0 && SpawnScale.z > 0;
        }
    }
}
