using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagePipe;

#nullable enable

namespace Laboratory.Infrastructure.AsyncUtils
{
    /// <summary>
    /// Asynchronous TCP network client using UniTask for high-performance networking.
    /// Provides event-driven communication with message broker integration.
    /// </summary>
    public class NetworkClient : IDisposable
    {
        #region Fields

        /// <summary>Underlying TCP client connection.</summary>
        private TcpClient? _tcpClient;
        
        /// <summary>Network stream for data transmission.</summary>
        private NetworkStream? _networkStream;
        
        /// <summary>Cancellation token source for operation cancellation.</summary>
        private CancellationTokenSource? _cts;

        /// <summary>Message broker for publishing network events.</summary>
        private readonly IMessageBroker _messageBroker;

        /// <summary>Current connection state.</summary>
        private bool _isConnected = false;

        #endregion

        #region Events

        /// <summary>Raised when successfully connected to server.</summary>
        public event Action? Connected;
        
        /// <summary>Raised when disconnected from server.</summary>
        public event Action? Disconnected;
        
        /// <summary>Raised when data is received from server.</summary>
        public event Action<byte[]>? DataReceived;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new NetworkClient with optional message broker.
        /// </summary>
        /// <param name="messageBroker">Message broker for event publishing. Creates default if null.</param>
        public NetworkClient(IMessageBroker? messageBroker = null)
        {
            _messageBroker = messageBroker ?? new MessageBroker();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the client is currently connected to the server.
        /// </summary>
        public bool IsConnected => _isConnected;

        #endregion

        #region Public Methods

        /// <summary>
        /// Establishes connection to server asynchronously.
        /// </summary>
        /// <param name="host">Server host address.</param>
        /// <param name="port">Server port number.</param>
        /// <param name="cancellationToken">Cancellation token for operation.</param>
        /// <returns>Task representing the connection operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when already connected.</exception>
        public async UniTask ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            if (_isConnected) return;

            _tcpClient = new TcpClient();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                await _tcpClient.ConnectAsync(host, port).AsUniTask();
                _networkStream = _tcpClient.GetStream();
                _isConnected = true;

                Connected?.Invoke();
                _messageBroker.Publish(new NetworkConnectedMessage());

                ReceiveLoopAsync(_cts.Token).Forget();
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _messageBroker.Publish(new NetworkErrorMessage(ex));
                throw;
            }
        }

        /// <summary>
        /// Gracefully disconnects from server.
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected) return;

            _cts?.Cancel();

            _networkStream?.Close();
            _tcpClient?.Close();

            _isConnected = false;
            Disconnected?.Invoke();
            _messageBroker.Publish(new NetworkDisconnectedMessage());
        }

        /// <summary>
        /// Sends data to server asynchronously.
        /// </summary>
        /// <param name="data">Data bytes to send.</param>
        /// <param name="cancellationToken">Cancellation token for operation.</param>
        /// <returns>Task representing the send operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when not connected to server.</exception>
        public async UniTask SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!_isConnected || _networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server.");
            }

            await _networkStream.WriteAsync(data, 0, data.Length, cancellationToken);
            await _networkStream.FlushAsync(cancellationToken);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Continuously receives data from server in background loop.
        /// </summary>
        /// <param name="token">Cancellation token for loop termination.</param>
        /// <returns>UniTaskVoid representing the receive loop.</returns>
        private async UniTaskVoid ReceiveLoopAsync(CancellationToken token)
        {
            var buffer = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested && _networkStream != null)
                {
                    int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0)
                    {
                        // Server disconnected
                        Disconnect();
                        break;
                    }

                    var data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);

                    DataReceived?.Invoke(data);
                    _messageBroker.Publish(new NetworkDataReceivedMessage(data));
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }
            catch (Exception ex)
            {
                _messageBroker.Publish(new NetworkErrorMessage(ex));
                Disconnect();
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>Tracks disposal state to prevent double disposal.</summary>
        private bool _disposed = false;

        /// <summary>
        /// Disposes all resources and disconnects from server.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Disconnect();

            _cts?.Dispose();
            _networkStream?.Dispose();
            _tcpClient?.Dispose();
        }

        #endregion
    }

    #region Message Broker Events

    /// <summary>Event published when network connection is established.</summary>
    public record NetworkConnectedMessage();

    /// <summary>Event published when network connection is lost.</summary>
    public record NetworkDisconnectedMessage();

    /// <summary>Event published when data is received from server.</summary>
    /// <param name="Data">The received data bytes.</param>
    public record NetworkDataReceivedMessage(byte[] Data);

    /// <summary>Event published when a network error occurs.</summary>
    /// <param name="Exception">The exception that occurred.</param>
    public record NetworkErrorMessage(Exception Exception);

    #endregion
}
