using System;
using System.Threading;
using Cysharp.Threading.Tasks;

#nullable enable

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Service interface for network operations and connectivity management.
    /// Provides unified networking functionality with connection management and event handling.
    /// </summary>
    public interface INetworkService : IDisposable
    {
        /// <summary>Gets whether the service is currently connected to a server.</summary>
        bool IsConnected { get; }
        
        /// <summary>Gets the current connection status.</summary>
        NetworkConnectionStatus ConnectionStatus { get; }
        
        /// <summary>Event fired when a player connects to the session.</summary>
        event Action<int> OnPlayerConnected;
        
        /// <summary>Event fired when a player disconnects from the session.</summary>
        event Action<int> OnPlayerDisconnected;
        
        /// <summary>Event fired when connection status changes.</summary>
        event Action<NetworkConnectionStatus> OnConnectionStatusChanged;
        
        /// <summary>Initializes the network service with the specified configuration.</summary>
        /// <param name="config">Network configuration settings</param>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>Task that completes when initialization is finished</returns>
        UniTask InitializeAsync(NetworkConfiguration config, CancellationToken cancellation = default);
        
        /// <summary>Connects to a server with the specified host and port.</summary>
        /// <param name="host">Server hostname or IP address</param>
        /// <param name="port">Server port number</param>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>Task that completes when connection is established</returns>
        UniTask ConnectAsync(string host, int port, CancellationToken cancellation = default);
        
        /// <summary>Disconnects from the current server.</summary>
        /// <returns>Task that completes when disconnection is finished</returns>
        UniTask DisconnectAsync();
        
        /// <summary>Sends data to the connected server.</summary>
        /// <param name="data">Data to send</param>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>Task that completes when data is sent</returns>
        UniTask SendAsync(byte[] data, CancellationToken cancellation = default);
        
        /// <summary>Gets network statistics and connection information.</summary>
        /// <returns>Current network statistics</returns>
        NetworkStatistics GetStatistics();
    }
    
    /// <summary>
    /// Network connection status enumeration.
    /// </summary>
    public enum NetworkConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Failed
    }
    
    /// <summary>
    /// Network configuration settings.
    /// </summary>
    [System.Serializable]
    public class NetworkConfiguration
    {
        public string DefaultHost { get; set; } = "localhost";
        public int DefaultPort { get; set; } = 7777;
        public int ConnectionTimeoutMs { get; set; } = 5000;
        public int ReconnectAttempts { get; set; } = 3;
        public bool EnableCompression { get; set; } = true;
        public bool EnableEncryption { get; set; } = false;
    }
    
    /// <summary>
    /// Network statistics and metrics.
    /// </summary>
    public struct NetworkStatistics
    {
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public int PacketsSent { get; set; }
        public int PacketsReceived { get; set; }
        public float ConnectionDuration { get; set; }
        public float Ping { get; set; }
    }
}
