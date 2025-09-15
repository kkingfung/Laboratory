using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Laboratory.Infrastructure.Networking;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// Adapter class that implements INetworkPlayerData interface for NetworkPlayerData
    /// This resolves the interface mismatch between NetworkPlayerData and the expected interface
    /// </summary>
    public class NetworkPlayerDataAdapter : INetworkPlayerData
    {
        private readonly NetworkPlayerData _networkPlayerData;
        
        /// <summary>
        /// Constructor for the adapter
        /// </summary>
        /// <param name="networkPlayerData">The NetworkPlayerData instance to adapt</param>
        public NetworkPlayerDataAdapter(NetworkPlayerData networkPlayerData)
        {
            _networkPlayerData = networkPlayerData;
        }
        
        /// <summary>
        /// Adapted Score property
        /// </summary>
        public INetworkVariable<int> Score => new NetworkVariableAdapter<int>(_networkPlayerData.Score);
        
        /// <summary>
        /// Adapted PlayerName property (converts FixedString32Bytes to string)
        /// </summary>
        public INetworkVariable<string> PlayerName => new NetworkVariableStringAdapter(_networkPlayerData.PlayerName);
        
        /// <summary>
        /// Adapted Kills property
        /// </summary>
        public INetworkVariable<int> Kills => new NetworkVariableAdapter<int>(_networkPlayerData.Kills);
        
        /// <summary>
        /// Adapted Deaths property
        /// </summary>
        public INetworkVariable<int> Deaths => new NetworkVariableAdapter<int>(_networkPlayerData.Deaths);
        
        /// <summary>
        /// Adapted Assists property
        /// </summary>
        public INetworkVariable<int> Assists => new NetworkVariableAdapter<int>(_networkPlayerData.Assists);
        
        /// <summary>
        /// Adapted Ping property
        /// </summary>
        public INetworkVariable<int> Ping => new NetworkVariableAdapter<int>(_networkPlayerData.Ping);
        
        /// <summary>
        /// Adapted PlayerAvatar property
        /// </summary>
        public Sprite PlayerAvatar => _networkPlayerData.GetAvatarSprite();
    }
    
    /// <summary>
    /// Generic adapter for NetworkVariable to INetworkVariable
    /// </summary>
    /// <typeparam name="T">The type of the network variable</typeparam>
    public class NetworkVariableAdapter<T> : INetworkVariable<T> where T : unmanaged
    {
        private readonly NetworkVariable<T> _networkVariable;
        private event System.Action<T, T> _onValueChanged;
        
        public NetworkVariableAdapter(NetworkVariable<T> networkVariable)
        {
            _networkVariable = networkVariable;
            _networkVariable.OnValueChanged += OnNetworkValueChanged;
        }
        
        public T Value => _networkVariable.Value;
        
        public event System.Action<T, T> OnValueChanged
        {
            add => _onValueChanged += value;
            remove => _onValueChanged -= value;
        }
        
        private void OnNetworkValueChanged(T oldValue, T newValue)
        {
            _onValueChanged?.Invoke(oldValue, newValue);
        }
    }
    
    /// <summary>
    /// Specialized adapter for FixedString32Bytes to string NetworkVariable
    /// </summary>
    public class NetworkVariableStringAdapter : INetworkVariable<string>
    {
        private readonly NetworkVariable<FixedString32Bytes> _networkVariable;
        private event System.Action<string, string> _onValueChanged;
        
        public NetworkVariableStringAdapter(NetworkVariable<FixedString32Bytes> networkVariable)
        {
            _networkVariable = networkVariable;
            _networkVariable.OnValueChanged += OnNetworkValueChanged;
        }
        
        public string Value => _networkVariable.Value.ToString();
        
        public event System.Action<string, string> OnValueChanged
        {
            add => _onValueChanged += value;
            remove => _onValueChanged -= value;
        }
        
        private void OnNetworkValueChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
        {
            _onValueChanged?.Invoke(oldValue.ToString(), newValue.ToString());
        }
    }
}
