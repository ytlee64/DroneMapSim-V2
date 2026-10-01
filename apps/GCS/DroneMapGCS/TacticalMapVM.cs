using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DroneMapGCS
{
    /// <summary>
    /// 2.3km 전술 맵 좌표 변환, 드론 마커, 기수선, 실시간 비행 궤적 렌더링 전담 ViewModel
    /// </summary>
    public partial class TacticalMapVM : ObservableObject
    {
        public const double AreaWidthMeters = 2000.0;
        public const double AreaHeightMeters = 2000.0;

        // 드론 위치 계측값
        [ObservableProperty] private float _droneX;
        [ObservableProperty] private float _droneY;
        [ObservableProperty] private float _droneAlt = 35.0f;
        [ObservableProperty] private float _droneYaw;
        [ObservableProperty] private string _statusCoordinateText = "POS: X: 0.0m | Y: 0.0m | Alt: 35.0m (Yaw: 0°)";

        // 캔버스 크기 및 화면 렌더링 바인딩
        [ObservableProperty] private double _canvasWidth = 800.0;
        [ObservableProperty] private double _canvasHeight = 800.0;
        [ObservableProperty] private double _markerLeft;
        [ObservableProperty] private double _markerTop;
        [ObservableProperty] private double _headingX1;
        [ObservableProperty] private double _headingY1;
        [ObservableProperty] private double _headingX2;
        [ObservableProperty] private double _headingY2;
        [ObservableProperty] private double _boundaryLeft;
        [ObservableProperty] private double _boundaryTop;
        [ObservableProperty] private double _boundarySize;

        // 실시간 궤적선 (PathGeometry)
        [ObservableProperty] private PathGeometry _trajectoryGeometry = new PathGeometry();
        private PathFigure? _currentFigure = null;
        private bool _isFirstPoint = true;

        // 사각형 웨이포인트 가이드라인 (Polygon)
        public PointCollection WaypointGuidePoints { get; } = new PointCollection();

        private double _meterScale = 1.0;
        private double _centerX = 0.0;
        private double _centerY = 0.0;

        // 창 크기가 변경되어도 궤적이 깨지지 않도록 미터 원본 좌표 보관
        private readonly List<Point> _trajectoryWorldPoints = new List<Point>();

        public TacticalMapVM()
        {
            UpdateCanvasGeometry(800, 800);
        }

        /// <summary>
        /// 캔버스 크기가 리사이즈될 때 화면 중심 및 미터당 픽셀 비율 재계산
        /// </summary>
        public void UpdateCanvasGeometry(double width, double height)
        {
            if (width <= 50 || height <= 50) return;

            CanvasWidth = width;
            CanvasHeight = height;

            _centerX = width * 0.5;
            _centerY = height * 0.5;

            double usableDim = Math.Min(width, height) - 40.0;
            _meterScale = usableDim / AreaWidthMeters;

            BoundarySize = 10000.0 * _meterScale;
            BoundaryLeft = _centerX - (BoundarySize * 0.5);
            BoundaryTop = _centerY - (BoundarySize * 0.5);

            RebuildTrajectoryGeometry();
        }

        private Point ToScreenPoint(Point worldPoint)
        {
            // 언리얼 좌표계: X=북쪽(-Y화면), Y=동쪽(+X화면)
            double screenX = _centerX + (worldPoint.Y * _meterScale);
            double screenY = _centerY - (worldPoint.X * _meterScale);
            return new Point(screenX, screenY);
        }

        private void RebuildTrajectoryGeometry()
        {
            TrajectoryGeometry.Figures.Clear();
            _currentFigure = null;

            if (_trajectoryWorldPoints.Count == 0) return;

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

        /// <summary>
        /// 언리얼 엔진에서 텔레메트리가 올 때마다 마커와 궤적 갱신
        /// </summary>
        public void UpdateTelemetryMap(float locX_m, float locY_m, float locZ_m, float yaw_deg)
        {
            DroneX = locX_m;
            DroneY = locY_m;
            DroneAlt = locZ_m;
            DroneYaw = yaw_deg;

            StatusCoordinateText = $"POS: X: {locX_m:F1}m | Y: {locY_m:F1}m | Alt: {locZ_m:F1}m (Yaw: {yaw_deg:F0}°)";

            if (_centerX <= 0 || _centerY <= 0) return;

            double screenX = _centerX + (locY_m * _meterScale);
            double screenY = _centerY - (locX_m * _meterScale);

            // 1. 드론 마커 중심 정렬 (14px)
            MarkerLeft = screenX - 7.0;
            MarkerTop = screenY - 7.0;

            // 2. 기수 헤딩 방향선 (길이 22px)
            double rad = (yaw_deg - 90.0) * (Math.PI / 180.0);
            HeadingX1 = screenX;
            HeadingY1 = screenY;
            HeadingX2 = screenX + Math.Cos(rad) * 22.0;
            HeadingY2 = screenY + Math.Sin(rad) * 22.0;

            Point newPt = new Point(screenX, screenY);
            _trajectoryWorldPoints.Add(new Point(locX_m, locY_m));
            if (_trajectoryWorldPoints.Count > 3000) _trajectoryWorldPoints.RemoveAt(0);

            // 3. 실시간 궤적선 연결
            if (_isFirstPoint || _currentFigure == null)
            {
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
                _currentFigure.Segments.Add(new LineSegment(newPt, true));
                if (_currentFigure.Segments.Count > 3000)
                {
                    _currentFigure.Segments.RemoveAt(0);
                }
            }
        }

        [RelayCommand]
        public void ClearTrajectory()
        {
            TrajectoryGeometry.Figures.Clear();
            _currentFigure = null;
            _isFirstPoint = true;
            _trajectoryWorldPoints.Clear();
        }
    }
}