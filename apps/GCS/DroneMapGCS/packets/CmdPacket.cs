using System.Text.Json.Serialization;

namespace DroneMapGCS
{
    // =========================================================================
    // 부모 클래스: Id 및 시퀀스 번호 관리
    // =========================================================================
    public abstract class CmdPacket
    {
        [JsonPropertyName("id")]
        public string Id { get; }

        [JsonPropertyName("seq")]
        public uint Seq { get; set; }

        protected CmdPacket(string id)
        {
            Id = id;
        }
    }

    // =========================================================================
    // 1. 텔레포트 명령 (미터 단위)
    // =========================================================================
    public class TeleportCommand : CmdPacket
    {
        public TeleportCommand() : base("TELEPORT") { }

        [JsonPropertyName("loc_x")]
        public float LocX { get; set; }

        [JsonPropertyName("loc_y")]
        public float LocY { get; set; }

        [JsonPropertyName("loc_z")]
        public float LocZ { get; set; }
    }

    // =========================================================================
    // 2. 옵저버 시점 전환 명령
    // =========================================================================
    public class ObserverCommand : CmdPacket
    {
        public ObserverCommand() : base("OBSERVER") { }

        [JsonPropertyName("mode")]
        public uint ObserverMode { get; set; } = 0;
    }

    // =========================================================================
    // 3. 2축 짐벌 제어 명령 (상/하 Up, 좌/우 Right 상대 각도)
    // =========================================================================
    public class GimbalCommand : CmdPacket
    {
        public GimbalCommand() : base("GIMBAL") { }

        [JsonPropertyName("up")]
        public int Up { get; set; } = 0;

        [JsonPropertyName("right")]
        public int Right { get; set; } = 0;
    }

    // =========================================================================
    // 4. 즉시 사진 캡처 명령
    // =========================================================================
    public class CaptureCommand : CmdPacket
    {
        public CaptureCommand() : base("CAPTURE") { }
    }

    // =========================================================================
    // 5. ⭐️ 수동 비행 조종 명령 (고정익 & 멀티 공용 표준 규격)
    // =========================================================================
    public class MoveCommand : CmdPacket
    {
        public MoveCommand() : base("MOVE") { }

        public MoveCommand(float forward, float right, float pitch, float yaw) : base("MOVE")
        {
            Forward = forward;
            Right = right;
            Pitch = pitch;
            Yaw = yaw;
        }

        // 스로틀 전진 가감속 (가속: +1.0, 감속: -1.0, 중립: 0.0)
        [JsonPropertyName("forward")]
        public float Forward { get; set; } = 0.0f;

        // 좌/우 날개 뱅킹 롤 (우뱅킹: +1.0, 좌뱅킹: -1.0, 중립: 0.0)
        [JsonPropertyName("right")]
        public float Right { get; set; } = 0.0f;

        // 기수 승강타 피치 (기수들기: +1.0, 기수내리기: -1.0, 중립: 0.0)
        [JsonPropertyName("pitch")]
        public float Pitch { get; set; } = 0.0f;

        // 러더 방향타 요 (우회전: +1.0, 좌회전: -1.0, 중립: 0.0)
        [JsonPropertyName("yaw")]
        public float Yaw { get; set; } = 0.0f;
    }

    // =========================================================================
    // 6. ⭐️ 자동 비행 조종 명령 
    // =========================================================================
    public class AutoNavCommand : CmdPacket
    {
        public AutoNavCommand() : base("AUTONAV") { }
    }
}