using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DroneMapGCS
{
    public partial class MainWindowVM : ObservableObject
    {
        // 10km x 10km 영역 (m)
        public const double AreaWidthMeters = 2000.0;
        public const double AreaHeightMeters = 2000.0;

        [ObservableProperty]
        private float _droneX;

        [ObservableProperty]
        private float _droneY;

        [ObservableProperty]
        private float _droneAlt = 35.0f;

        [ObservableProperty]
        private float _droneYaw;

        [ObservableProperty]
        private string _statusCoordinateText = "POS: X: 0.0m | Y: 0.0m | Alt: 35.0m (Yaw: 0°)";

        // ---------------------------------------------------------------------
        // 2. 캔버스 화면 렌더링용 바인딩 프로퍼티
        // ---------------------------------------------------------------------
        [ObservableProperty]
        private double _canvasWidth = 800.0;

        [ObservableProperty]
        private double _canvasHeight = 800.0;

        [ObservableProperty]
        private double _markerLeft;

        [ObservableProperty]
        private double _markerTop;

        [ObservableProperty]
        private double _headingX1;

        [ObservableProperty]
        private double _headingY1;

        [ObservableProperty]
        private double _headingX2;

        [ObservableProperty]
        private double _headingY2;

        [ObservableProperty]
        private double _boundaryLeft;

        [ObservableProperty]
        private double _boundaryTop;

        [ObservableProperty]
        private double _boundarySize;

        [ObservableProperty]
        private PathGeometry _trajectoryGeometry = new PathGeometry();

        private PathFigure? _currentFigure = null;
        private bool _isFirstPoint = true;

        // 사각형 웨이포인트 가이드라인 (Polygon.Points 바인딩)
        public PointCollection WaypointGuidePoints { get; } = new PointCollection();

        private double _meterScale = 1.0;
        private double _centerX = 0.0;
        private double _centerY = 0.0;

        // 화면 크기 변경에 영향을 받지 않도록 궤적을 월드(미터) 좌표로 보관
        private readonly List<Point> _trajectoryWorldPoints = new List<Point>();

        public void UpdateCanvasGeometry(double width, double height)
        {
            if (width <= 50 || height <= 50) return;

            CanvasWidth = width;
            CanvasHeight = height;

            _centerX = width * 0.5;
            _centerY = height * 0.5;

            // 작은 쪽에 맞추어 10km 정사각형 영역 스케일 산출
            double usableDim = Math.Min(width, height) - 40.0;
            _meterScale = usableDim / AreaWidthMeters;

            // 10km 외곽 테두리 갱신
            BoundarySize = 10000.0 * _meterScale;
            BoundaryLeft = _centerX - (BoundarySize * 0.5);
            BoundaryTop = _centerY - (BoundarySize * 0.5);

            // 사각형 웨이포인트 가이드라인 다시 계산
            //RebuildWaypointGuide();

            // 스케일/중심이 바뀌었으므로 기존 궤적을 새 스케일로 다시 그림
            RebuildTrajectoryGeometry();
        }

        private Point ToScreenPoint(Point worldPoint)
        {
            double screenX = _centerX + (worldPoint.Y * _meterScale);
            double screenY = _centerY - (worldPoint.X * _meterScale);
            return new Point(screenX, screenY);
        }

        private void RebuildTrajectoryGeometry()
        {
            TrajectoryGeometry.Figures.Clear();
            _currentFigure = null;

            if (_trajectoryWorldPoints.Count == 0)
            {
                return;
            }

            _currentFigure = new PathFigure
            {
                StartPoint = ToScreenPoint(_trajectoryWorldPoints[0]),
                IsClosed = false,
                IsFilled = false
            };

            for (int i = 1; i < _trajectoryWorldPoints.Count; i++)
            {
                _currentFigure.Segments.Add(new LineSegment(ToScreenPoint(_trajectoryWorldPoints[i]), true));
            }

            TrajectoryGeometry.Figures.Add(_currentFigure);
        }

        public void UpdateTelemetryMap(float locX_m, float locY_m, float locZ_m, float yaw_deg)
        {
            DroneX = locX_m;
            DroneY = locY_m;
            DroneAlt = locZ_m;
            DroneYaw = yaw_deg;

            System.Console.WriteLine($"POS: X: {locX_m:F1}m | Y: {locY_m:F1}m | Alt: {locZ_m:F1}m (Yaw: {yaw_deg:F0}°)");

            if (_centerX <= 0 || _centerY <= 0) return;

            // 좌표 변환: 언리얼 X=북쪽(-Y화면), Y=동쪽(+X화면)
            double screenX = _centerX + (locY_m * _meterScale);
            double screenY = _centerY - (locX_m * _meterScale);

     
            // 2. 드론 마커 오프셋 (마커 크기 14px 기준 중심 정렬)
            MarkerLeft = screenX - 7.0;
            MarkerTop = screenY - 7.0;

            // 3. 기수 헤딩선 계산 (길이 22px)
            double rad = (yaw_deg - 90.0) * (Math.PI / 180.0);
            HeadingX1 = screenX;
            HeadingY1 = screenY;
            HeadingX2 = screenX + Math.Cos(rad) * 22.0;
            HeadingY2 = screenY + Math.Sin(rad) * 22.0;

            Point newPt = new Point(screenX, screenY);

            // 월드(미터) 좌표로 저장해두어 화면 크기 변경 시 재계산 가능하도록 함
            _trajectoryWorldPoints.Add(new Point(locX_m, locY_m));
            if (_trajectoryWorldPoints.Count > 3000)
            {
                _trajectoryWorldPoints.RemoveAt(0);
            }

            if (_isFirstPoint || _currentFigure == null)
            {
                // 첫 점일 때는 선의 시작점(StartPoint) 생성
                _currentFigure = new PathFigure
                {
                    StartPoint = newPt,
                    IsClosed = false,
                    IsFilled = false
                };
                TrajectoryGeometry.Figures.Add(_currentFigure);
                _isFirstPoint = false;
            }
            else
            {
                // 이전 점에서 현재 점까지 선분(Segment) 연결! (WPF가 100% 즉각 반응함)
                _currentFigure.Segments.Add(new LineSegment(newPt, true));

                // 최대 3000개 초과 시 메모리 최적화
                if (_currentFigure.Segments.Count > 3000)
                {
                    _currentFigure.Segments.RemoveAt(0);
                }
            }
        }

        [RelayCommand]
        private void ClearTrajectory()
        {
            TrajectoryGeometry.Figures.Clear();
            _currentFigure = null;
            _isFirstPoint = true;
            _trajectoryWorldPoints.Clear();
        }
    }
}