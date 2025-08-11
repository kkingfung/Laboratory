using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagingPipe;

#nullable enable

namespace Infrastructure
{
    /// <summary>
    /// Simple async TCP Network Client using UniTask and MessagingPipe for events.
    /// </summary>
    public class NetworkClient : IDisposable
    {
        #region Fields

        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private CancellationTokenSource? _cts;

        private readonly IMessageBroker _messageBroker;

        private bool _isConnected = false;

        #endregion

        #region Events

        public event Action? Connected;
        public event Action? Disconnected;
        public event Action<byte[]>? DataReceived;

        #endregion

        #region Constructor

        /// <summary>
        /// Requires an IMessageBroker for event publishing.
        /// </summary>
        public NetworkClient(IMessageBroker? messageBroker = null)
        {
            _messageBroker = messageBroker ?? new MessageBroker();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Connect to server asynchronously.
        /// </summary>
        public async UniTask ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            if (_isConnected) return;

            _tcpClient = new TcpClient();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                await _tcpClient.ConnectAsync(host, port).ToUniTask(cancellationToken: _cts.Token);
                _networkStream = _tcpClient.GetStream();
                _isConnected = true;

                Connected?.Invoke();
                _messageBroker.Publish(new NetworkConnectedMessage());

                _ = ReceiveLoopAsync(_cts.Token).Forget();
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _messageBroker.Publish(new NetworkErrorMessage(ex));
                throw;
            }
        }

        /// <summary>
        /// Disconnect gracefully.
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
        /// Send data asynchronously.
        /// </summary>
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

        #region IDisposable Support

        private bool _disposed = false;

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

    #region MessagingPipe Messages

    public record NetworkConnectedMessage();

    public record NetworkDisconnectedMessage();

    public record NetworkDataReceivedMessage(byte[] Data);

    public record NetworkErrorMessage(Exception Exception);

    #endregion
}
