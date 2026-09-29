using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DroneMapGCS
{
    public class WaypointItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        // 좌표 단위: 미터 (m)  -> Unreal Engine에서 100배(cm)로 변환
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }

        // 비행 속도 (m/s) (고정익 최소 실속속도 고려: 11~15 m/s)
        [JsonPropertyName("speed")]
        public float Speed { get; set; } = 15.0f;
    }

    // =========================================================================
    // 3. 웨이포인트 리스트 전송 명령 (고정익 직사각형 비행 & 선회반경 40m)
    // =========================================================================
    public class WaypointListCommand : CmdPacket
    {
        public WaypointListCommand() : base("WAYPOINT_LIST") { }

        // 항공기 유형: 고정익 (FIXED_WING)
        [JsonPropertyName("aircraft_type")]
        public string AircraftType { get; set; } = "FIXED_WING";

        // 마지막 지점 또는 홀딩 시 선회 반경 (기본 40.0m)
        [JsonPropertyName("default_loiter_radius_m")]
        public float DefaultLoiterRadiusM { get; set; } = 40.0f;

        // 주기적 사진 촬영 활성화 여부
        [JsonPropertyName("periodic_capture")]
        public bool PeriodicCapture { get; set; } = true;

        // 주기적 촬영 시간 간격 (초 단위, 예: 2.5초마다 촬영)
        [JsonPropertyName("capture_interval_sec")]
        public float CaptureIntervalSec { get; set; } = 2.5f;

        // 웨이포인트 개수
        [JsonPropertyName("count")]
        public int Count => Points.Count;

        // 웨이포인트 목록
        [JsonPropertyName("points")]
        public List<WaypointItem> Points { get; set; } = new List<WaypointItem>();
    }
}
