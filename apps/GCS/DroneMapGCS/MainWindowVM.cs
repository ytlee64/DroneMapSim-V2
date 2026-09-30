using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DroneMapGCS
{
    public partial class MainWindowVM : ObservableObject
    {
        private const string DefaultCapturePath = @"D:\ytlee\ureal\DroneMapSim\Saved\DroneCaptures";
        private readonly ImageWatcherService _watcherService;
        private readonly CommTelemetryService _telemetryService;
        private readonly CommCmdService _commandService;

        private TelemtryPacket? _latestTelemetry;

        [ObservableProperty] private BitmapImage? _currentFrame;
        [ObservableProperty] private string _statusText = "대기 중...";
        [ObservableProperty] private string _currentFileName = "-";
        [ObservableProperty] private string _resolutionText = "0 x 0";
        [ObservableProperty] private int _totalCaptureCount = 0;
        [ObservableProperty] private string _telemetryText = "X: 0.0m | Y: 0.0m | Alt: 0.0m [MANUAL]";
        [ObservableProperty] private string _lastCapturePosText = "마지막 캡처 위치: 없음";

        // 마지막으로 전송한 조종 명령 알림
        [ObservableProperty] private string _lastActionNotice = "조종 대기 (화면 클릭 후 키보드 조작 가능)";

        public MainWindowVM()
        {
            _watcherService = new ImageWatcherService(DefaultCapturePath);
            _watcherService.ImageCaptured += OnImageCaptured;

            _telemetryService = new CommTelemetryService();
            _telemetryService.TelemetryReceived += OnTelemetryReceived;
            _telemetryService.Start(9001);

            // 언리얼 명령 포트 (9000번) 송신기 초기화
            _commandService = new CommCmdService("127.0.0.1", 9000);

            UpdateCanvasGeometry(800, 800);
        }

        /// <summary>
        /// 키보드 누름(KeyDown) 이벤트 처리
        /// </summary>
        public void HandleKeyDown(Key key)
        {
            string desc = "";

            switch (key)
            {
                case Key.I:
                    _commandService.SendJsonCommand(new TeleportCommand
                    {
                        LocX = 0,
                        LocY = 0,
                        LocZ = 100,
                    });
                    Thread.Sleep(50);
                    _commandService.SendJsonCommand(new GimbalCommand
                    {
                        Up = -180,
                        Right = 0
                    });
                    desc = "Drone Init[I]";
                    break;

                case Key.Space:
                    _commandService.SendJsonCommand(new CaptureCommand());
                    desc = "CAPTURE_IMAGE [Space]";
                    break;

                // 🎥 시점 전환 (CYCLE)
                case Key.V:
                case Key.O:
                    _commandService.SendJsonCommand(new ObserverCommand());
                    desc = "Observer / View Change [O/V]";
                    break;

                // 🎥 짐벌 상/하/좌/우 조작
                case Key.Up:
                    _commandService.SendJsonCommand(new GimbalCommand { Up = 5, Right = 0 });
                    desc = "GIMBAL Up [Up]";
                    break;

                case Key.Down:
                    _commandService.SendJsonCommand(new GimbalCommand { Up = -5, Right = 0 });
                    desc = "GIMBAL Down [Down]";
                    break;

                case Key.Left:
                    _commandService.SendJsonCommand(new GimbalCommand { Up = 0, Right = -5 });
                    desc = "GIMBAL Left [Left]";
                    break;

                case Key.Right:
                    // ⭐️ [버그 수정]: 기존 -5 -> +5로 정상 우회전 반영
                    _commandService.SendJsonCommand(new GimbalCommand { Up = 0, Right = 5 });
                    desc = "GIMBAL Right [Right]";
                    break;

                // ===================================================
                // 🕹️ 고정익 수동 비행 조종 (Forward, Right/Roll, Pitch, Yaw)
                // ===================================================
                // W/S: 스로틀 가감속
                case Key.W:
                    SendFlightControl(forward: 1.0f, right: 0.0f, pitch: 0.0f, yaw: 0.0f);
                    desc = "가속 (Throttle Up) [W]";
                    break;
                case Key.S:
                    SendFlightControl(forward: -1.0f, right: 0.0f, pitch: 0.0f, yaw: 0.0f);
                    desc = "감속 (Throttle Down) [S]";
                    break;

                // A/D: 좌우 날개 뱅킹 턴 (Roll)
                case Key.A:
                    SendFlightControl(forward: 0.0f, right: -1.0f, pitch: 0.0f, yaw: 0.0f);
                    desc = "좌측 뱅킹 선회 (Roll Left) [A]";
                    break;
                case Key.D:
                    SendFlightControl(forward: 0.0f, right: 1.0f, pitch: 0.0f, yaw: 0.0f);
                    desc = "우측 뱅킹 선회 (Roll Right) [D]";
                    break;

                // E/Q: 기수 상승/하강 (Pitch)
                case Key.E:
                    SendFlightControl(forward: 0.0f, right: 0.0f, pitch: 1.0f, yaw: 0.0f);
                    desc = "기수 상승 (Pitch Up) [E]";
                    break;
                case Key.Q:
                    SendFlightControl(forward: 0.0f, right: 0.0f, pitch: -1.0f, yaw: 0.0f);
                    desc = "기수 하강 (Pitch Down) [Q]";
                    break;

                // Z/C: 방향타 러더 조향 (Yaw)
                case Key.Z:
                    SendFlightControl(forward: 0.0f, right: 0.0f, pitch: 0.0f, yaw: -1.0f);
                    desc = "좌측 러더 (Rudder Left) [Z]";
                    break;
                case Key.C:
                    SendFlightControl(forward: 0.0f, right: 0.0f, pitch: 0.0f, yaw: 1.0f);
                    desc = "우측 러더 (Rudder Right) [C]";
                    break;

                default:
                    return;
            }

            LastActionNotice = desc;
            StatusText = $"[{DateTime.Now:HH:mm:ss}] {desc} 전송 완료";
        }

        /// <summary>
        /// ⭐️ 키를 뗐을 때(KeyUp) 스틱 중립 복귀 처리
        /// </summary>
        public void HandleKeyUp(Key key)
        {
            switch (key)
            {
                case Key.W:
                case Key.S:
                case Key.A:
                case Key.D:
                case Key.E:
                case Key.Q:
                case Key.Z:
                case Key.C:
                    // 비행 키에서 손을 떼면 중립(0.0) 패킷 전송 -> 안정 수평 순항 유지!
                    SendFlightControl(0.0f, 0.0f, 0.0f, 0.0f);
                    break;
            }
        }

        /// <summary>
        /// 고정익 비행 제어 JSON 패킷 송신 헬퍼
        /// </summary>
        private void SendFlightControl(float forward, float right, float pitch, float yaw)
        {
            // MoveCommand 클래스 구조에 따라 아래 중 맞는 방식을 쓰시면 됩니다:
            // 1. MoveCommand(float f, float r, float p, float y) 가 있는 경우:
            _commandService.SendJsonCommand(new MoveCommand(forward, right, pitch, yaw));

            // 또는 직접 DTO 객체로 보낼 경우:
            // _commandService.SendJsonCommand(new { id = "MOVE", forward = forward, right = right, pitch = pitch, yaw = yaw });
        }

        private void OnTelemetryReceived(TelemtryPacket packet)
        {
            _latestTelemetry = packet;
            Application.Current?.Dispatcher.Invoke(() =>
            {
                TelemetryText = $"Count:{packet.Seq} X: {packet.LocX:F1}m  Y: {packet.LocY:F1}m  Alt: {packet.LocZ:F1}m | {packet.Speed:F1} m/s (Roll:{packet.RotRoll:F1}° Yaw:{packet.RotYaw:F1}°) [{packet.Mode}]";
                UpdateTelemetryMap((float)packet.LocX, (float)packet.LocY, (float)packet.LocZ, (float)packet.RotYaw);
            });
        }

        private void OnImageCaptured(BitmapImage bitmap, string fullPath, long fileSizeBytes)
        {
            var capturePos = _latestTelemetry;
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CurrentFrame = bitmap;
                CurrentFileName = Path.GetFileName(fullPath);
                ResolutionText = $"{bitmap.PixelWidth} x {bitmap.PixelHeight} px";
                TotalCaptureCount++;
                StatusText = $"[정상 수신 #{TotalCaptureCount}] {DateTime.Now:HH:mm:ss.fff}";
            });
        }

        [RelayCommand]
        private void ResetCounter()
        {
            TotalCaptureCount = 0;
            StatusText = "카운트가 리셋되었습니다.";
        }
    }
}