using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DroneMapGCS
{
  

    public class CommCmdService : IDisposable
    {
        private readonly IPEndPoint _remoteEndPoint;

        public CommCmdService(string targetIp = "127.0.0.1", int targetPort = 9000)
        {
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(targetIp), targetPort);
        }

        public void SendJsonCommand(CmdPacket packet)
        {
            try
            {
                string json = JsonSerializer.Serialize(packet, packet.GetType());
                // 언리얼 소켓 파서를 위한 개행문자
                byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");

                using (var client = new UdpClient())
                {
                    client.Send(bytes, bytes.Length, _remoteEndPoint);
                }

                System.Diagnostics.Debug.WriteLine($"[UDP TX -> 9000] {json}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[명령 전송 실패] {ex.Message}");
            }
        }

        public void Dispose() { }
    }
}