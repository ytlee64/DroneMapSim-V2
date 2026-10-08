using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DroneMapGCS
{
    public partial class MainWindowVM : ObservableObject
    {
        private const string CaptureSubFolder = "DroneCaptures";
        private static readonly string DefaultCapturePath = ResolveCapturePath(CaptureSubFolder);

        private const string YoloModelRelativePath = @"apps\AI\weights\best.onnx";
        private static readonly string YoloModelPath = ResolveRepoRootPath(YoloModelRelativePath);

        private readonly ImageService _imageService;
        private readonly CommTelemetryService _telemetryService;
        private readonly CommCmdService _commandService;

        private TelemtryPacket? _latestTelemetry;

        // ⭐️ 접미사 VM 통일: TacticalMapVM 소유
        public TacticalMapVM Map { get; } = new TacticalMapVM();

        // FPV 영상 및 계측 상태
        [ObservableProperty] private BitmapImage? _currentFrame;
        [ObservableProperty] private string _statusText = "대기 중...";
        [ObservableProperty] private string _currentFileName = "-";
        [ObservableProperty] private string _resolutionText = "0 x 0";
        [ObservableProperty] private int _totalCaptureCount = 0;
        [ObservableProperty] private string _telemetryText = "X: 0.0m | Y: 0.0m | Alt: 0.0m [MANUAL]";
        [ObservableProperty] private string _lastCapturePosText = "마지막 캡처 위치: 없음";
        [ObservableProperty] private string _lastActionNotice = "조종 대기 (화면 클릭 후 키보드 조작 가능)";

        // GCS 조종 패널 바인딩
        [ObservableProperty] private string _flightModeText = "MANUAL";
        [ObservableProperty] private string _bankAngleText = "0°";
        [ObservableProperty] private string _gimbalPitchText = "P: -45.0°";
        [ObservableProperty] private string _gimbalYawText = "Y: 0.0°";

        private int AutoNavEnable = 0;
        private float _currentGimbalPitch = -45.0f;
        private float _currentGimbalYaw = 0.0f;

        public MainWindowVM()
        {
            _imageService = new ImageService(DefaultCapturePath, YoloModelPath);
            _imageService.ImageCaptured += OnImageCaptured;

            _telemetryService = new CommTelemetryService();
            _telemetryService.TelemetryReceived += OnTelemetryReceived;
            _telemetryService.Start(9001);

            _commandService = new CommCmdService("127.0.0.1", 9000);
        }

        private static string ResolveCapturePath(string subFolder)
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (dir.EnumerateFiles("*.uproject").Any())
                    return Path.GetFullPath(Path.Combine(dir.FullName, "Saved", subFolder));
                dir = dir.Parent;
            }
            return Path.GetFullPath(Path.Combine("..", "..", "..", "..", "..", "engine", "Saved", subFolder), AppContext.BaseDirectory);
        }

        private static string ResolveRepoRootPath(string relativePath)
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                    return Path.GetFullPath(Path.Combine(dir.FullName, relativePath));
                dir = dir.Parent;
            }
            return Path.GetFullPath(Path.Combine("..", "..", "..", "..", "..", relativePath), AppContext.BaseDirectory);
        }

        // =========================================================
        // 비행 제어 및 모드 전환 RelayCommands
        // =========================================================
        [RelayCommand]
        private void ToggleViewMode()
        {
            _commandService.SendJsonCommand(new ObserverCommand());
            StatusText = $"[{DateTime.Now:HH:mm:ss}] ⟲ 뷰 모드 전환 [V] 전송 완료";
        }

        [RelayCommand]
        private void ResetCounter()
        {
            TotalCaptureCount = 0;
            StatusText = "수집 카운터가 리셋되었습니다.";
        }

        [RelayCommand]
        private void ClearTrajectory() => Map.ClearTrajectory();

        // =========================================================
        // 키보드 조작 처리
        // =========================================================
        public void HandleKeyDown(Key key)
        {
            string desc = "";
            switch (key)
            {
                case Key.I:
                    _commandService.SendJsonCommand(new TeleportCommand { LocX = 0, LocY = 0, LocZ = 100 });
                    Thread.Sleep(50);
                    _commandService.SendJsonCommand(new GimbalCommand { Up = -180, Right = 0 });
                    desc = "Drone Init[I]";
                    break;
                case Key.V:
                    ToggleViewMode();
                    return;
                case Key.Up:
                    _currentGimbalPitch = Math.Clamp(_currentGimbalPitch + 5.0f, -90.0f, 20.0f);
                    GimbalPitchText = $"P: {_currentGimbalPitch:F1}°";
                    _commandService.SendJsonCommand(new GimbalCommand { Up = 5, Right = 0 });
                    desc = "GIMBAL Up [Up]";
                    break;
                case Key.Down:
                    _currentGimbalPitch = Math.Clamp(_currentGimbalPitch - 5.0f, -90.0f, 20.0f);
                    GimbalPitchText = $"P: {_currentGimbalPitch:F1}°";
                    _commandService.SendJsonCommand(new GimbalCommand { Up = -5, Right = 0 });
                    desc = "GIMBAL Down [Down]";
                    break;
                case Key.Left:
                    _currentGimbalYaw = (_currentGimbalYaw - 5.0f) % 360.0f;
                    GimbalYawText = $"Y: {_currentGimbalYaw:F1}°";
                    _commandService.SendJsonCommand(new GimbalCommand { Up = 0, Right = -5 });
                    desc = "GIMBAL Left [Left]";
                    break;
                case Key.Right:
                    _currentGimbalYaw = (_currentGimbalYaw + 5.0f) % 360.0f;
                    GimbalYawText = $"Y: {_currentGimbalYaw:F1}°";
                    _commandService.SendJsonCommand(new GimbalCommand { Up = 0, Right = 5 });
                    desc = "GIMBAL Right [Right]";
                    break;
                case Key.W: SendFlightControl(1.0f, 0.0f, 0.0f, 0.0f); desc = "가속 [W]"; break;
                case Key.S: SendFlightControl(-1.0f, 0.0f, 0.0f, 0.0f); desc = "감속 [S]"; break;
                case Key.A: SendFlightControl(0.0f, -1.0f, 0.0f, 0.0f); desc = "좌선회 [A]"; break;
                case Key.D: SendFlightControl(0.0f, 1.0f, 0.0f, 0.0f); desc = "우선회 [D]"; break;
                case Key.E: SendFlightControl(0.0f, 0.0f, 1.0f, 0.0f); desc = "상승 [E]"; break;
                case Key.Q: SendFlightControl(0.0f, 0.0f, -1.0f, 0.0f); desc = "하강 [Q]"; break;

                case Key.D1:
                    AutoNavEnable = (AutoNavEnable + 1) % 2;
                    _commandService.SendJsonCommand(new AutoNavCommand { Enable = AutoNavEnable });
                    FlightModeText = AutoNavEnable == 1 ? "AUTO NAV" : "MANUAL";
                    desc = AutoNavEnable == 1 ? "자동 항법[1]" : "수동 모드[1]";
                    break;
                default: return;
            }
            LastActionNotice = desc;
            StatusText = $"[{DateTime.Now:HH:mm:ss}] {desc} 전송 완료";
        }

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
                    SendFlightControl(0.0f, 0.0f, 0.0f, 0.0f);
                    break;
            }
        }

        private void SendFlightControl(float forward, float right, float pitch, float yaw)
        {
            _commandService.SendJsonCommand(new MoveCommand(forward, right, pitch, yaw));
        }

        // =========================================================
        // 패킷 수신 처리
        // =========================================================
        private void OnTelemetryReceived(TelemtryPacket packet)
        {
            _latestTelemetry = packet;
            Application.Current?.Dispatcher.Invoke(() =>
            {
                TelemetryText = $"Count:{packet.Seq} X: {packet.LocX:F1}m  Y: {packet.LocY:F1}m  Alt: {packet.LocZ:F1}m | {packet.Speed:F1} m/s (Roll:{packet.RotRoll:F1}° Yaw:{packet.RotYaw:F1}°) [{packet.Mode}]";
                BankAngleText = $"{packet.RotRoll:F0}°";
                if (!string.IsNullOrEmpty(packet.Mode)) FlightModeText = packet.Mode;

                // ⭐️ Map(TacticalMapVM)으로 텔레메트리 전달
                _imageService.SyncCaptureFromTelemetry(packet.LastCapture);
                Map.UpdateTelemetryMap((float)packet.LocX, (float)packet.LocY, (float)packet.LocZ, (float)packet.RotYaw);
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
                LastCapturePosText = $"마지막 캡처: X={capturePos?.LocX:F1}m, Y={capturePos?.LocY:F1}m";
                StatusText = $"[정상 수신 #{TotalCaptureCount}] {DateTime.Now:HH:mm:ss.fff}";
            });
        }
    }
}