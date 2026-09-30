using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DroneMapGCS
{
    public class TelemtryPacket
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "TELEMETRY";

        [JsonPropertyName("seq")]
        public long Seq { get; set; }

        // =========================================================
        // 1. 드론 위치 좌표 (m 단위)
        // 언리얼의 "loc_x", "loc_y", "loc_z" 필드와 1:1 매핑
        // =========================================================
        [JsonPropertyName("loc_x")]
        public double LocX { get; set; }

        [JsonPropertyName("loc_y")]
        public double LocY { get; set; }

        [JsonPropertyName("loc_z")]
        public double LocZ { get; set; }

        // =========================================================
        // 2. 드론 기체 자세 각도 (Pitch, Yaw, Roll, deg 단위)
        // 언리얼의 "rot_pitch", "rot_yaw", "rot_roll" 필드와 1:1 매핑
        // =========================================================
        [JsonPropertyName("rot_pitch")]
        public double RotPitch { get; set; }

        [JsonPropertyName("rot_yaw")]
        public double RotYaw { get; set; }

        [JsonPropertyName("rot_roll")]
        public double RotRoll { get; set; }

        // =========================================================
        // 3. 짐벌 각도 (Pitch, Yaw, deg 단위)
        // 언리얼의 "gimbal_pitch", "gimbal_yaw" 필드와 1:1 매핑
        // =========================================================
        [JsonPropertyName("gimbal_pitch")]
        public double GimbalPitch { get; set; }

        [JsonPropertyName("gimbal_yaw")]
        public double GimbalYaw { get; set; }

        // =========================================================
        // 4. 비행 속도 및 운용 모드
        // =========================================================
        [JsonPropertyName("speed")]
        public double Speed { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "MANUAL";

        // 선택적 필드 (언리얼에서 확장 시 대비)
        [JsonPropertyName("reached")]
        public bool Reached { get; set; }

        [JsonPropertyName("observer_mode")]
        public string ObserverMode { get; set; } = "";

        [JsonPropertyName("gimbal_mode")]
        public string GimbalMode { get; set; } = "";

    }
}