using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

namespace DroneMapGCS
{
    /// <summary>
    /// GroundTruth_Targets.json 역직렬화용 DTO
    /// </summary>
    public class GroundTruthTargetDto
    {
        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }

        [JsonPropertyName("class_name")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public double[] Location { get; set; } = Array.Empty<double>();
    }

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

        // 실시간 궤적선 (PathGeometry) - 기존 그대로 유지!
        [ObservableProperty] private PathGeometry _trajectoryGeometry = new PathGeometry();
        private PathFigure? _currentFigure = null;
        private bool _isFirstPoint = true;

        // ⭐️ Ground Truth 표적 렌더링용 프로퍼티 추가
        [ObservableProperty] private Geometry _groundTruthGeometry = Geometry.Empty;
        [ObservableProperty] private int _groundTruthCount;
        [ObservableProperty] private bool _showGroundTruth = true;

        // 사각형 웨이포인트 가이드라인 (Polygon)
        public PointCollection WaypointGuidePoints { get; } = new PointCollection();

        private double _meterScale = 1.0;
        private double _centerX = 0.0;
        private double _centerY = 0.0;

        // 창 크기가 변경되어도 궤적이 깨지지 않도록 미터 원본 좌표 보관
        private readonly List<Point> _trajectoryWorldPoints = new List<Point>();

        // ⭐️ Ground Truth 타겟들의 미터(m) 원본 좌표 보관
        private readonly List<Point> _groundTruthWorldPoints = new List<Point>();

        public TacticalMapVM()
        {
            LoadGroundTruthTargets();
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
            RebuildGroundTruthGeometry(); // ⭐️ 캔버스 크기에 맞춰 GT 타겟 마커들도 재투영
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
        /// ⭐️ GroundTruth_Targets.json 좌표들을 드론과 동일한 ToScreenPoint()로 변환해 마커 생성
        /// </summary>
        private void RebuildGroundTruthGeometry()
        {
            if (_centerX <= 0 || _centerY <= 0 || _groundTruthWorldPoints.Count == 0)
            {
                GroundTruthGeometry = Geometry.Empty;
                return;
            }

            var group = new GeometryGroup();
            foreach (Point worldPt in _groundTruthWorldPoints)
            {
                Point screenPt = ToScreenPoint(worldPt);
                group.Children.Add(new EllipseGeometry(screenPt, 3.5, 3.5));
            }
            group.Freeze();
            GroundTruthGeometry = group;
        }

        /// <summary>
        /// ⭐️ Saved/Datasets/GroundTruth_Targets.json 파일을 찾아 미터(m) 단위로 로드
        /// </summary>
        public void LoadGroundTruthTargets(string? customJsonPath = null)
        {
            _groundTruthWorldPoints.Clear();

            string? jsonPath = customJsonPath ?? FindGroundTruthJsonPath();
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
            {
                GroundTruthCount = 0;
                GroundTruthGeometry = Geometry.Empty;
                return;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                var targets = JsonSerializer.Deserialize<List<GroundTruthTargetDto>>(json);

                if (targets != null)
                {
                    foreach (var t in targets)
                    {
                        if (t.Location != null && t.Location.Length >= 2)
                        {
                            // 언리얼 cm 단위를 미터(m) 단위로 변환 (100cm = 1m)
                            double xMeters = t.Location[0] / 100.0;
                            double yMeters = t.Location[1] / 100.0;
                            _groundTruthWorldPoints.Add(new Point(xMeters, yMeters));
                        }
                    }
                }

                GroundTruthCount = _groundTruthWorldPoints.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TacticalMapVM] GroundTruth JSON 로드 오류: {ex.Message}");
                GroundTruthCount = 0;
            }
        }

        private static string? FindGroundTruthJsonPath()
        {
            string rel1 = Path.Combine("Saved", "Datasets", "GroundTruth_Targets.json");
            string rel2 = Path.Combine("apps", "engine", "Saved", "Datasets", "GroundTruth_Targets.json");

            DirectoryInfo? dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                string c1 = Path.Combine(dir.FullName, rel1);
                if (File.Exists(c1)) return c1;

                string c2 = Path.Combine(dir.FullName, rel2);
                if (File.Exists(c2)) return c2;

                dir = dir.Parent;
            }
            return null;
        }

        [RelayCommand]
        public void ReloadGroundTruth()
        {
            LoadGroundTruthTargets();
            RebuildGroundTruthGeometry();
        }

        /// <summary>
        /// 언리얼 엔진에서 텔레메트리가 올 때마다 마커와 궤적 갱신 (기존 코드 100% 동일)
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