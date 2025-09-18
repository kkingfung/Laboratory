using System;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.DI;

#nullable enable

namespace Laboratory.Infrastructure.AsyncUtils
{
    /// <summary>
    /// Asynchronous TCP network client using UniTask for high-performance networking.
    /// Publishes network events through the UnifiedEventBus system.
    /// </summary>
    public class NetworkClient : IDisposable
    {
        #region Fields

        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private CancellationTokenSource? _cts;
        private bool _isConnected = false;
        private IEventBus? _eventBus;

        #endregion

        #region Events

        public event Action? Connected;
        public event Action? Disconnected;
        public event Action<byte[]>? DataReceived;

        #endregion

        #region Constructor

        public NetworkClient()
        {
            // Initialize event bus
            if (GlobalServiceProvider.IsInitialized)
            {
                _eventBus = GlobalServiceProvider.Resolve<IEventBus>();
            }
        }

        #endregion

        #region Properties

        public bool IsConnected => _isConnected;

        #endregion

        #region Public Methods

        /// <summary>
        /// Connect to the specified host and port asynchronously.
        /// </summary>
        public async UniTask<bool> ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            if (_isConnected) return true;

            try
            {
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _tcpClient = new TcpClient();

                await _tcpClient.ConnectAsync(host, port);
                _networkStream = _tcpClient.GetStream();
                _isConnected = true;

                // Start receiving data
                _ = ReceiveDataAsync(_cts.Token);

                // Notify connection
                Connected?.Invoke();
                _eventBus?.Publish(new NetworkConnectedMessage { Host = host, Port = port });

                Debug.Log($"[NetworkClient] Connected to {host}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkClient] Connection failed: {ex.Message}");
                _eventBus?.Publish(new NetworkErrorMessage { Error = ex.Message, Exception = ex });
                await DisconnectAsync();
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the server asynchronously.
        /// </summary>
        public async UniTask DisconnectAsync()
        {
            if (!_isConnected) return;

            try
            {
                _isConnected = false;
                _cts?.Cancel();

                _networkStream?.Close();
                _tcpClient?.Close();

                await UniTask.Delay(100); // Small delay for cleanup

                // Notify disconnection
                Disconnected?.Invoke();
                _eventBus?.Publish(new NetworkDisconnectedMessage());

                Debug.Log("[NetworkClient] Disconnected");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkClient] Disconnection error: {ex.Message}");
                _eventBus?.Publish(new NetworkErrorMessage { Error = ex.Message, Exception = ex });
            }
        }

        /// <summary>
        /// Send data to the server asynchronously.
        /// </summary>
        public async UniTask<bool> SendDataAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!_isConnected || _networkStream == null) return false;

            try
            {
                await _networkStream.WriteAsync(data, 0, data.Length, cancellationToken);
                await _networkStream.FlushAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkClient] Send data error: {ex.Message}");
                _eventBus?.Publish(new NetworkErrorMessage { Error = ex.Message, Exception = ex });
                return false;
            }
        }

        /// <summary>
        /// Send string data to the server asynchronously.
        /// </summary>
        public async UniTask<bool> SendStringAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(message)) return false;

            var data = System.Text.Encoding.UTF8.GetBytes(message);
            return await SendDataAsync(data, cancellationToken);
        }

        #endregion

        #region Private Methods

        private async UniTask ReceiveDataAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            try
            {
                while (_isConnected && !cancellationToken.IsCancellationRequested)
                {
                    if (_networkStream == null) break;

                    var bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0)
                    {
                        // Server closed connection
                        break;
                    }

                    var receivedData = new byte[bytesRead];
                    Array.Copy(buffer, receivedData, bytesRead);

                    // Notify data received
                    DataReceived?.Invoke(receivedData);
                    _eventBus?.Publish(new NetworkDataReceivedMessage { Data = receivedData });
                }
            }
            catch (ObjectDisposedException)
            {
                // Expected when disposing
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkClient] Receive data error: {ex.Message}");
                _eventBus?.Publish(new NetworkErrorMessage { Error = ex.Message, Exception = ex });
            }
            finally
            {
                if (_isConnected)
                {
                    await DisconnectAsync();
                }
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            _ = DisconnectAsync();
            _cts?.Dispose();
            _networkStream?.Dispose();
            _tcpClient?.Dispose();
        }

        #endregion
    }

    #region Network Event Messages

    /// <summary>
    /// Message published when network connection is established
    /// </summary>
    public class NetworkConnectedMessage
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public DateTime ConnectedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Message published when network connection is lost
    /// </summary>
    public class NetworkDisconnectedMessage
    {
        public DateTime DisconnectedAt { get; set; } = DateTime.Now;
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Message published when data is received from network
    /// </summary>
    public class NetworkDataReceivedMessage
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime ReceivedAt { get; set; } = DateTime.Now;
        public int Size => Data.Length;
    }

    /// <summary>
    /// Message published when network error occurs
    /// </summary>
    public class NetworkErrorMessage
    {
        public string Error { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public DateTime ErrorTime { get; set; } = DateTime.Now;
    }

    #endregion
}
