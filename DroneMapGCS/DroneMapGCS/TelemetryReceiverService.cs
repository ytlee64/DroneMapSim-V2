using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DroneMapGCS
{
    public class TelemetryReceiverService : IDisposable
    {
        private UdpClient? _udpClient;
        private CancellationTokenSource? _cts;
        public event Action<DroneTelemetryPacket>? TelemetryReceived;

        public void Start(int port = 9001)
        {
            Stop();
            _cts = new CancellationTokenSource();

            try
            {
                _udpClient = new UdpClient();
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

                Task.Run(() => ReceiveLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UDP 오류] {ex.Message}");
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _udpClient != null)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync(token);
                    string json = Encoding.UTF8.GetString(result.Buffer);

                    var packet = JsonSerializer.Deserialize<DroneTelemetryPacket>(json);
                    if (packet != null)
                    {
                        TelemetryReceived?.Invoke(packet);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception) { /* 단일 패킷 깨짐 시 무시 후 다음 패킷 대기 */ }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }

        public void Dispose() => Stop();
    }
}