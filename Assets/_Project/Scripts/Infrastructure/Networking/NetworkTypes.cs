using System;
using System.Threading;
using UnityEngine;

#nullable enable

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Configuration settings for network operations.
    /// Contains timeouts, retry policies, and connection parameters.
    /// </summary>
    [Serializable]
    public class NetworkConfiguration
    {
        [Header("Connection Settings")]
        [Tooltip("Default host address for network connections")]
        public string DefaultHost = "localhost";
        
        [Tooltip("Default port for network connections")]
        public int DefaultPort = 7777;
        
        [Header("Timeout Settings")]
        [Tooltip("Connection timeout in seconds")]
        public float ConnectionTimeout = 30f;
        
        [Tooltip("Maximum time to wait for server response")]
        public float ResponseTimeout = 10f;
        
        [Header("Retry Settings")]
        [Tooltip("Maximum number of connection retry attempts")]
        public int MaxRetryAttempts = 3;
        
        [Tooltip("Delay between retry attempts in seconds")]
        public float RetryDelay = 2f;
        
        [Header("Performance Settings")]
        [Tooltip("Maximum number of packets to process per frame")]
        public int MaxPacketsPerFrame = 100;
        
        [Tooltip("Maximum bandwidth usage in bytes per second")]
        public int MaxBandwidthBps = 1048576; // 1MB/s
        
        [Header("Security Settings")]
        [Tooltip("Whether to use encrypted connections")]
        public bool UseEncryption = true;
        
        [Tooltip("Authentication token for secure connections")]
        public string AuthToken = "";
    }
    
    /// <summary>
    /// Represents the current status of a network connection.
    /// </summary>
    public enum NetworkConnectionStatus
    {
        /// <summary>Not connected to any server.</summary>
        Disconnected = 0,
        
        /// <summary>Currently attempting to connect.</summary>
        Connecting = 1,
        
        /// <summary>Successfully connected to server.</summary>
        Connected = 2,
        
        /// <summary>Connection attempt failed.</summary>
        Failed = 3,
        
        /// <summary>Connection was lost unexpectedly.</summary>
        Lost = 4,
        
        /// <summary>Connection is being terminated.</summary>
        Disconnecting = 5
    }
    
    /// <summary>
    /// Network statistics for monitoring connection quality and performance.
    /// </summary>
    [Serializable]
    public class NetworkStatistics
    {
        /// <summary>Total bytes sent since connection started.</summary>
        public long BytesSent { get; set; }
        
        /// <summary>Total bytes received since connection started.</summary>
        public long BytesReceived { get; set; }
        
        /// <summary>Total packets sent since connection started.</summary>
        public long PacketsSent { get; set; }
        
        /// <summary>Total packets received since connection started.</summary>
        public long PacketsReceived { get; set; }
        
        /// <summary>Current round-trip time in milliseconds.</summary>
        public float RoundTripTime { get; set; }
        
        /// <summary>Packet loss percentage (0-100).</summary>
        public float PacketLoss { get; set; }
        
        /// <summary>Duration of current connection in seconds.</summary>
        public float ConnectionDuration { get; set; }
        
        /// <summary>Current bandwidth utilization as percentage of max.</summary>
        public float BandwidthUtilization { get; set; }
        
        /// <summary>Number of connection drops since start.</summary>
        public int ConnectionDrops { get; set; }
        
        /// <summary>
        /// Calculates the current data transfer rate in bytes per second.
        /// </summary>
        public float GetTransferRate()
        {
            if (ConnectionDuration <= 0f) return 0f;
            return (BytesSent + BytesReceived) / ConnectionDuration;
        }
        
        /// <summary>
        /// Gets a human-readable summary of network statistics.
        /// </summary>
        public string GetSummary()
        {
            return $"RTT: {RoundTripTime:F1}ms | Loss: {PacketLoss:F1}% | " +
                   $"Sent: {FormatBytes(BytesSent)} | Received: {FormatBytes(BytesReceived)} | " +
                   $"Rate: {FormatBytes((long)GetTransferRate())}/s";
        }
        
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int suffixIndex = 0;
            double size = bytes;
            
            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }
            
            return $"{size:F1}{suffixes[suffixIndex]}";
        }
    }
    
    /// <summary>
    /// Interface for network service implementations.
    /// Provides a unified API for different networking solutions.
    /// </summary>
    public interface INetworkService : IDisposable
    {
        /// <summary>Current connection status.</summary>
        NetworkConnectionStatus ConnectionStatus { get; }
        
        /// <summary>Whether currently connected to a server.</summary>
        bool IsConnected { get; }
        
        /// <summary>Event fired when connection status changes.</summary>
        event Action<NetworkConnectionStatus>? OnConnectionStatusChanged;
        
        /// <summary>Event fired when a player connects.</summary>
        event Action<int>? OnPlayerConnected;
        
        /// <summary>Event fired when a player disconnects.</summary>
        event Action<int>? OnPlayerDisconnected;
        
        /// <summary>
        /// Initializes the network service with the given configuration.
        /// </summary>
        System.Threading.Tasks.Task InitializeAsync(NetworkConfiguration config, CancellationToken cancellation = default);
        
        /// <summary>
        /// Connects to the specified host and port.
        /// </summary>
        System.Threading.Tasks.Task ConnectAsync(string host, int port, CancellationToken cancellation = default);
        
        /// <summary>
        /// Disconnects from the current server.
        /// </summary>
        System.Threading.Tasks.Task DisconnectAsync();
        
        /// <summary>
        /// Sends data to the server.
        /// </summary>
        System.Threading.Tasks.Task SendAsync(byte[] data, CancellationToken cancellation = default);
        
        /// <summary>
        /// Gets current network statistics.
        /// </summary>
        NetworkStatistics GetStatistics();
    }
}
