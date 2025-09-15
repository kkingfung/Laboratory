using UnityEngine;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Network context component to provide IsServer functionality
    /// </summary>
    public class NetworkContext : MonoBehaviour
    {
        public bool IsServer { get; set; } = true;
        public bool IsClient { get; set; } = true;
        public bool IsHost { get; set; } = true;
        
        private static NetworkContext _instance;
        public static NetworkContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("NetworkContext");
                    _instance = go.AddComponent<NetworkContext>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
