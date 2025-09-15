using System;
using System.Threading;
using Cysharp.Threading.Tasks;

#nullable enable

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Basic network client implementation for handling connections.
    /// This is a simplified implementation for the laboratory project.
    /// </summary>
    public class NetworkClient : IDisposable
    {
        #region Events
        
        public event Action? Connected;
        public event Action? Disconnected;
        public event Action<byte[]>? DataReceived;
        
        #endregion
        
        #region Fields
        
        private readonly object? _connectedPublisher;
        private readonly object? _disconnectedPublisher;
        private readonly object? _dataPublisher;
        private readonly object? _errorPublisher;
        
        private bool _isConnected = false;
        private bool _disposed = false;
        
        #endregion
        
        #region Constructor
        
        public NetworkClient(
            object? connectedPublisher = null,
            object? disconnectedPublisher = null,
            object? dataPublisher = null,
            object? errorPublisher = null)
        {
            _connectedPublisher = connectedPublisher;
            _disconnectedPublisher = disconnectedPublisher;
            _dataPublisher = dataPublisher;
            _errorPublisher = errorPublisher;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Connects to the specified host and port.
        /// </summary>
        public async UniTask ConnectAsync(string host, int port, CancellationToken cancellation = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NetworkClient));
                
            if (_isConnected)
                return;
            
            try
            {
                // Simulate connection process
                await UniTask.Delay(200, cancellationToken: cancellation);
                
                _isConnected = true;
                Connected?.Invoke();
            }
            catch (Exception)
            {
                _isConnected = false;
                throw;
            }
        }
        
        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected) return;
            
            _isConnected = false;
            Disconnected?.Invoke();
        }
        
        /// <summary>
        /// Sends data to the connected server.
        /// </summary>
        public async UniTask SendAsync(byte[] data, CancellationToken cancellation = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NetworkClient));
                
            if (!_isConnected)
                throw new InvalidOperationException("Not connected");
            
            if (data == null || data.Length == 0)
                return;
            
            // Simulate sending data
            await UniTask.Delay(10, cancellationToken: cancellation);
            
            // In a real implementation, this would send data over the network
            UnityEngine.Debug.Log($"[NetworkClient] Sent {data.Length} bytes");
            
            // Simulate receiving an echo response to trigger DataReceived event
            await UniTask.Delay(50, cancellationToken: cancellation);
            DataReceived?.Invoke(new byte[] { 0x01, 0x02, 0x03 }); // Dummy response
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            if (_disposed) return;
            
            if (_isConnected)
            {
                Disconnect();
            }
            
            _disposed = true;
        }
        
        #endregion
    }
}
