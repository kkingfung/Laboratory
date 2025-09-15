using System;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;

#nullable enable

namespace Laboratory.Infrastructure.AsyncUtils
{
    /// <summary>
    /// Asynchronous TCP network client using UniTask for high-performance networking.
    /// Publishes network events through MessagePipe publishers.
    /// </summary>
    public class NetworkClient : IDisposable
    {
        #region Fields

        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private CancellationTokenSource? _cts;
        private bool _isConnected = false;

        private readonly IPublisher<NetworkConnectedMessage> _connectedPublisher;
        private readonly IPublisher<NetworkDisconnectedMessage> _disconnectedPublisher;
        private readonly IPublisher<NetworkDataReceivedMessage> _dataPublisher;
        private readonly IPublisher<NetworkErrorMessage> _errorPublisher;

        #endregion

        #region Events

        public event Action? Connected;
        public event Action? Disconnected;
        public event Action<byte[]>? DataReceived;

        #endregion

        #region Constructor

        public NetworkClient(
            IPublisher<NetworkConnectedMessage> connectedPublisher,
            IPublisher<NetworkDisconnectedMessage> disconnectedPublisher,
            IPublisher<NetworkDataReceivedMessage> dataPublisher,
            IPublisher<NetworkErrorMessage> errorPublisher)
        {
            _connectedPublisher = connectedPublisher;
            _disconnectedPublisher = disconnectedPublisher;
            _dataPublisher = dataPublisher;
            _errorPublisher = errorPublisher;
        }

        #endregion

        #region Properties

        public bool IsConnected => _isConnected;

        #endregion

        #region Public Methods

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
                _connectedPublisher.Publish(new NetworkConnectedMessage());

                ReceiveLoopAsync(_cts.Token).Forget();
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _errorPublisher.Publish(new NetworkErrorMessage(ex));
                throw;
            }
        }

        public void Disconnect()
        {
            if (!_isConnected) return;

            _cts?.Cancel();

            _networkStream?.Close();
            _tcpClient?.Close();

            _isConnected = false;
            Disconnected?.Invoke();
            _disconnectedPublisher.Publish(new NetworkDisconnectedMessage());
        }

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
                        Disconnect();
                        break;
                    }

                    var data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);

                    DataReceived?.Invoke(data);
                    _dataPublisher.Publish(new NetworkDataReceivedMessage(data));
                }
            }
            catch (OperationCanceledException)
            {
                // expected on cancellation
            }
            catch (Exception ex)
            {
                _errorPublisher.Publish(new NetworkErrorMessage(ex));
                Disconnect();
            }
        }

        #endregion

        #region IDisposable

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

    #region MessagePipe Events

    public class NetworkConnectedMessage { }
    public class NetworkDisconnectedMessage { }
    public class NetworkDataReceivedMessage 
    { 
        public byte[] Data { get; }
        public NetworkDataReceivedMessage(byte[] data) => Data = data;
    }
    public class NetworkErrorMessage 
    { 
        public Exception Exception { get; }
        public NetworkErrorMessage(Exception exception) => Exception = exception;
    }

    #endregion
}
