using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DroneMapGCS
{
    public class ImageWatcherService : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly YoloDetectorService _detector;
        public event Action<BitmapImage, string, long>? ImageCaptured;

        public ImageWatcherService(string watchPath, string yoloModelPath)
        {
            _detector = new YoloDetectorService(yoloModelPath);

            watchPath = watchPath.Trim();
            if (!Directory.Exists(watchPath))
            {
                Directory.CreateDirectory(watchPath);
            }

            _watcher = new FileSystemWatcher(watchPath)
            {
                Filter = "*.png", // png 파일 집중 감시
                InternalBufferSize = 65536,
                NotifyFilter = NotifyFilters.FileName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.CreationTime
                             | NotifyFilters.Size,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            // 1. 일반 생성 감지
            _watcher.Created += (s, e) => OnFileDetected(e.FullPath);

            // 2. 내용 수정/덮어쓰기 감지
            _watcher.Changed += (s, e) => OnFileDetected(e.FullPath);

            // 3. ? 언리얼 엔진 전용: 임시 파일(.tmp)에서 .png로 이름 바뀔 때 감지!
            _watcher.Renamed += (s, e) => OnFileDetected(e.FullPath);

            _watcher.Error += (s, e) =>
            {
                try
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.EnableRaisingEvents = true;
                }
                catch { }
            };
        }

        private void OnFileDetected(string fullPath)
        {
            if (!fullPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return;

            // 백그라운드 태스크로 파일 로드 (언리얼 저장 완료 대기)
            _ = Task.Run(async () => await LoadImageSafeAsync(fullPath));
        }

        private async Task LoadImageSafeAsync(string filePath)
        {
            BitmapImage? bitmap = null;
            long fileSizeBytes = 0;

            // 언리얼 엔진이 파일 핸들을 완전히 닫을(Flush/Close) 때까지 안전 재시도
            for (int i = 0; i < 40; i++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        // 파일이 비어있지 않고 기록이 끝났는지 확인
                        if (fileInfo.Length > 0)
                        {
                            // FileShare.ReadWrite로 열어서 언리얼의 쓰기 락과 충돌 회피
                            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                using (var ms = new MemoryStream())
                                {
                                    await fs.CopyToAsync(ms);
                                    ms.Position = 0;
                                    fileSizeBytes = ms.Length;

                                    var bmp = new BitmapImage();
                                    bmp.BeginInit();
                                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                                    bmp.StreamSource = ms;
                                    bmp.EndInit();
                                    bmp.Freeze(); // UI 스레드 전송용 Freeze

                                    bitmap = bmp;
                                }
                            }
                            break; // 읽기 성공 시 루프 탈출
                        }
                    }
                }
                catch (IOException)
                {
                    // 언리얼이 아직 파일을 닫지 않았으면 0.05초 대기
                    await Task.Delay(50);
                }
                catch
                {
                    break;
                }
            }

            if (bitmap != null)
            {
                bitmap = DrawBoundingBoxes(bitmap, filePath);
                ImageCaptured?.Invoke(bitmap, filePath, fileSizeBytes);
            }
        }

        /// <summary>
        /// 1) 이미지와 동일한 이름의 YOLO 정답(Ground-Truth) 라벨(.txt) 파일을 읽어
        ///    정규화된 바운딩 박스를 녹색 사각형으로 그리고,
        /// 2) YOLO 모델(best.onnx)로 실시간 추론한 탐지 결과를 빨간색 사각형으로 그려
        /// 두 가지를 함께 표시한 새 비트맵을 반환합니다.
        /// (둘 다 없으면 원본 비트맵을 그대로 반환)
        /// </summary>
        private BitmapImage DrawBoundingBoxes(BitmapImage source, string imageFilePath)
        {
            int pixelWidth = source.PixelWidth;
            int pixelHeight = source.PixelHeight;
            if (pixelWidth <= 0 || pixelHeight <= 0) return source;

            string labelFilePath = Path.ChangeExtension(imageFilePath, ".txt");
            bool hasGroundTruth = File.Exists(labelFilePath);

            List<DetectionBox> detections = _detector.IsAvailable
                ? _detector.Detect(source)
                : new List<DetectionBox>();

            // 1. Ground Truth 파싱
            var groundTruths = new List<Rect>();
            if (hasGroundTruth)
            {
                foreach (string line in File.ReadAllLines(labelFilePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 5) continue;

                    if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double cx) &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double cy) &&
                        double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double w) &&
                        double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double h))
                    {
                        double bw = w * pixelWidth;
                        double bh = h * pixelHeight;
                        groundTruths.Add(new Rect((cx * pixelWidth) - (bw * 0.5), (cy * pixelHeight) - (bh * 0.5), bw, bh));
                    }
                }
            }

            if (groundTruths.Count == 0 && detections.Count == 0) return source;

            var drawingVisual = new DrawingVisual();
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawImage(source, new Rect(0, 0, pixelWidth, pixelHeight));

                // 펜 정의 (동결하여 성능 최적화)
                var hitPen = new Pen(Brushes.Cyan, 2.5);          // ⭐️ 적중 (TP) - 선명한 청록색
                hitPen.Freeze();

                var missedPen = new Pen(Brushes.Yellow, 2.0);      // ⭐️ 미탐 (FN) - 노란색 점선 (못 찾은 정답)
                missedPen.DashStyle = DashStyles.Dash;
                missedPen.Freeze();

                var falseAlarmPen = new Pen(Brushes.Red, 2.0);    // ⭐️ 오탐 (FP) - 빨간색 (잘못 찾음)
                falseAlarmPen.Freeze();

                // IoU 매칭 추적용 배열
                bool[] gtMatched = new bool[groundTruths.Count];

                // 2. AI 추론값들을 정답과 비교
                foreach (DetectionBox det in detections)
                {
                    bool isHit = false;
                    for (int i = 0; i < groundTruths.Count; i++)
                    {
                        // IoU(교집합/합집합) 계산
                        if (CalculateIoU(det.Rect, groundTruths[i]) >= 0.4) // 40% 이상 일치 시 정답 인정
                        {
                            isHit = true;
                            gtMatched[i] = true;
                            break;
                        }
                    }

                    // 맞췄으면 청록색(Cyan), 헛다리면 빨간색(Red)
                    dc.DrawRectangle(null, isHit ? hitPen : falseAlarmPen, det.Rect);
                }

                // 3. AI가 끝내 찾지 못한 정답(미탐)은 노란색 점선으로 표시
                for (int i = 0; i < groundTruths.Count; i++)
                {
                    if (!gtMatched[i])
                    {
                        dc.DrawRectangle(null, missedPen, groundTruths[i]);
                    }
                }
            }

            var renderTarget = new RenderTargetBitmap(pixelWidth, pixelHeight, source.DpiX, source.DpiY, PixelFormats.Pbgra32);
            renderTarget.Render(drawingVisual);
            renderTarget.Freeze();

            var result = new BitmapImage();
            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));
                encoder.Save(ms);
                ms.Position = 0;

                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = ms;
                result.EndInit();
                result.Freeze();
            }

            return result;
        }

        /// <summary>
        /// 두 박스의 겹치는 비율 (Intersection over Union) 계산 헬퍼
        /// </summary>
        private static double CalculateIoU(Rect r1, Rect r2)
        {
            Rect intersect = Rect.Intersect(r1, r2);
            if (intersect.IsEmpty) return 0.0;

            double intersectArea = intersect.Width * intersect.Height;
            double unionArea = (r1.Width * r1.Height) + (r2.Width * r2.Height) - intersectArea;
            return unionArea <= 0 ? 0.0 : intersectArea / unionArea;
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _detector?.Dispose();
        }
    }
}