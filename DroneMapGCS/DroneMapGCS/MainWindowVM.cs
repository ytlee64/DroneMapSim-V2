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
        private readonly TelemetryReceiverService _telemetryService;
        private readonly DroneCommandService _commandService;

        private DroneTelemetryPacket? _latestTelemetry;

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

            _telemetryService = new TelemetryReceiverService();
            _telemetryService.TelemetryReceived += OnTelemetryReceived;
            _telemetryService.Start(9001);

            // 언리얼 명령 포트 (9000번) 송신기 초기화
            _commandService = new DroneCommandService("127.0.0.1", 9000);


            UpdateCanvasGeometry(800, 800);
        }


        /// <summary>
        /// 파이썬 콘솔 컨트롤러 키 매핑을 1:1로 처리하는 메서드
        /// </summary>
        public void HandleKeyDown(Key key)
        {
            string desc = "";
            int moveStep = 3; // 1회 키 입력당 3m (300cm) 이동
            int yawStep = 5;   // 1회 회전당 5도

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

                    // 3) 짐벌 각도 전송
                    _commandService.SendJsonCommand(new GimbalCommand
                    {
                        Up= -180,
                        Right = 0
                    });

                    desc = "Drone Init[I]";
                    break;

                case Key.Space:
                    _commandService.SendJsonCommand(new CaptureCommand
                    {
                    });
                    desc = "CAPTURE_IMAGE [Space]";
                    break;

                
                // 🎥 시점 전환 (CYCLE)
                case Key.V:
                    _commandService.SendJsonCommand(new ObserverCommand()
                    {
                        ObserverMode = 1
                    });
                    desc = "SET_OBSERVER_MODE (CYCLE) [V]";
                    break;

                case Key.Up:
                    _commandService.SendJsonCommand(new GimbalCommand
                    { 
                        Up = 5,
                        Right = 0
                    });
                    desc = "GIMBAL [Up]";
                    break;

                case Key.Down:
                    _commandService.SendJsonCommand(new GimbalCommand
                    {
                        Up = -5,
                        Right = 0
                    });
                    desc = "GIMBAL [Down]";
                    break;

                case Key.Left:
                    _commandService.SendJsonCommand(new GimbalCommand
                    {
                        Up = 0,
                        Right = -5
                    });
                    desc = "GIMBAL [Left]";
                    break;

                case Key.Right:
                    _commandService.SendJsonCommand(new GimbalCommand
                    {
                        Up = 0,
                        Right = -5
                    });
                    desc = "GIMBAL [Right]";
                    break;

                case Key.O:
                    _commandService.SendJsonCommand(new ObserverCommand
                    {
                    });
                    desc = "Observer [O]";
                    break;

                case Key.N:
                    SendRectangleWaypointsPacket();
                    desc = "RectangleWaypoints [N]";
                    break;

                //// ===================================================
                //// 🕹️ 드론 수동 비행 (MANUAL_MOVE x, y, z, yaw_rate)
                //// ===================================================
                case Key.W: _commandService.SendJsonCommand(new MoveCommand(moveStep, 0, 0, 0)); desc = "전진 (X+300) [W]"; break;
                case Key.S: _commandService.SendJsonCommand(new MoveCommand(-moveStep, 0, 0, 0)); desc = "후진 (X-300) [S]"; break;
                case Key.A: _commandService.SendJsonCommand(new MoveCommand(0, -moveStep, 0, 0)); desc = "좌이동 (Y-300) [A]"; break;
                case Key.D: _commandService.SendJsonCommand(new MoveCommand(0, moveStep, 0, 0)); desc = "우이동 (Y+300) [D]"; break;
                case Key.E: _commandService.SendJsonCommand(new MoveCommand(0, 0, moveStep, 0)); desc = "상승 (Z+300) [E]"; break;
                case Key.Q: _commandService.SendJsonCommand(new MoveCommand(0, 0, -moveStep, 0)); desc = "하강 (Z-300) [Q]"; break;
                case Key.Z: _commandService.SendJsonCommand(new MoveCommand(0, 0, 0, -yawStep)); desc = "좌회전 (Yaw-15) [Z]"; break;
                case Key.C: _commandService.SendJsonCommand(new MoveCommand(0, 0, 0, yawStep)); desc = "우회전 (Yaw+15) [C]"; break;

                default:
                    return;
            }


         
            LastActionNotice = desc;
            StatusText = $"[{DateTime.Now:HH:mm:ss}] {desc} 전송 완료";
        }

        private void OnTelemetryReceived(DroneTelemetryPacket packet)
        {
            _latestTelemetry = packet;
            Application.Current?.Dispatcher.Invoke(() =>
            {
                TelemetryText = $"Count:{packet.Seq} X: {packet.LocX:F1}m  Y: {packet.LocY:F1}m  Alt: {packet.LocZ:F1}m | {packet.Speed:F1} km/h (Yaw: {packet.RotYaw:F1}°) [{packet.Mode}]";
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

                if (capturePos != null)
                {
                    //LastCapturePosText = $"촬영 위치: X={capturePos.X_meter:F1}m, Y={capturePos.Y_meter:F1}m, Alt={capturePos.Alt_meter:F1}m, Yaw={capturePos.Yaw:F1}°";
                }
            });
        }

        private void SendRectangleWaypointsPacket()
        {
            // 1. 고정익 직사각형 웨이포인트 패킷 생성 (선회 반경 40m 설정)
            var cmd = new WaypointListCommand
            {
                AircraftType = "FIXED_WING",
                DefaultLoiterRadiusM = 40.0f,
                PeriodicCapture = true,
                CaptureIntervalSec = 2.5f,
                Points = new List<WaypointItem>
                {
                    new WaypointItem { Id = 1, X = 0.0f, Y = -500.0f, Z = 100.0f, Speed = 15.0f },
                    new WaypointItem { Id = 2, X =  800.0f, Y = -500.0f, Z = 100.0f, Speed = 15.0f },
                    new WaypointItem { Id = 3, X =  800.0f, Y =  500.0f, Z = 100.0f, Speed = 15.0f },
                    new WaypointItem { Id = 4, X = 0.0f, Y =  500.0f, Z = 100.0f, Speed = 15.0f }
                }
            };

            _commandService.SendJsonCommand(cmd);
            
        }

        [RelayCommand]
        private void ResetCounter()
        {
            TotalCaptureCount = 0;
            StatusText = "카운트가 리셋되었습니다.";
        }
    }
}