using System.Text.Json.Serialization;

namespace DroneMapGCS
{
    // 부모 클래스: Id는 오직 여기서 딱 1번만 정의합니다.
    public abstract class DroneCommandPacket
    {
        [JsonPropertyName("id")]
        public string Id { get; }

        [JsonPropertyName("seq")]
        public uint Seq { get; set; }

        // 자식이 넘겨준 id 값을 부모가 세팅
        protected DroneCommandPacket(string id)
        {
            Id = id;
        }
    }

    // 1. 텔레포트 명령
    public class TeleportCommand : DroneCommandPacket
    {
        public TeleportCommand() : base("TELEPORT") { }

        [JsonPropertyName("loc_x")]
        public int LocX { get; set; }

        [JsonPropertyName("loc_y")]
        public int LocY { get; set; }

        [JsonPropertyName("loc_z")]
        public int LocZ { get; set; }
    }

    // 2. 옵저버 명령
    public class ObserverCommand : DroneCommandPacket
    {
        public ObserverCommand() : base("OBSERVER") { }

        [JsonPropertyName("mode")]
        public uint ObserverMode { get; set; } = 0;// "CHASE","TOPTDOWN","FREE";
    }

    // 3. 짐벌 명령
    public class GimbalCommand : DroneCommandPacket
    {
        public GimbalCommand() : base("GIMBAL") { }

        [JsonPropertyName("up")]
        public int Up { get; set; } = 0;

        [JsonPropertyName("right")]
        public int Right { get; set; } = 0;
    }

    public class CaptureCommand : DroneCommandPacket
    {
        public CaptureCommand() : base("CAPTURE") { }
    }

    public class MoveCommand : DroneCommandPacket
    {
        public MoveCommand() : base("MOVE") { }

        public MoveCommand(int forward, int right, int up, int yaw) : base("MOVE")
        {
            Forward = forward;
            Right = right;
            Up = up;
            Yaw = yaw;
        }

        // 앞/뒤 전후진 (앞: +1.0, 뒤: -1.0)
        [JsonPropertyName("forward")]
        public int Forward { get; set; } = 0;

        // 좌/우 이동 (우: +1.0, 좌: -1.0)
        [JsonPropertyName("right")]
        public int Right { get; set; } = 0;

        // 상/하 고도 이동 (상승: +1.0, 하강: -1.0)
        [JsonPropertyName("up")]
        public int Up { get; set; } = 0;

        // 요(Yaw) 제자리 회전 (우회전: +1.0, 좌회전: -1.0)
        [JsonPropertyName("yaw")]
        public int Yaw { get; set; } = 0;
    }



}