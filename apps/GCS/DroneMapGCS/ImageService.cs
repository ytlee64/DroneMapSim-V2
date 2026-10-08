using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DroneMapGCS
{
    public class ImageService : IDisposable
    {
        private readonly string _captureDir;
        private readonly YoloDetectorService _detector;

        private string _lastProcessedFileName = string.Empty;
        private int _isProcessing = 0;

        public event Action<BitmapImage, string, long>? ImageCaptured;

        public ImageService(string captureDir, string yoloModelPath)
        {
            _detector = new YoloDetectorService(yoloModelPath);

            _captureDir = captureDir.Trim();
            if (!Directory.Exists(_captureDir))
            {
                Directory.CreateDirectory(_captureDir);
            }
        }

        /// <summary>
        /// ⭐️ 오직 텔레메트리에서 전달된 캡처 파일명(packet.LastCapture)만을 기준으로 동작합니다.
        /// (패킷에 .png가 없어도 자동으로 붙여서 처리하며, 이미 처리한 파일명이면 즉시 반환합니다.)
        /// </summary>
        public void SyncCaptureFromTelemetry(string? telemetryCaptureFileName)
        {
            if (string.IsNullOrWhiteSpace(telemetryCaptureFileName))
                return;

            string fileName = telemetryCaptureFileName.Trim();
            if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".png";
            }

            // 1. 이미 처리 완료한 캡처 파일이면 즉시 종료
            if (string.Equals(fileName, _lastProcessedFileName, StringComparison.OrdinalIgnoreCase))
                return;

            // 2. 중복 진입 방지
            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0)
                return;

            string targetPngPath = Path.IsPathRooted(fileName)
                ? fileName
                : Path.Combine(_captureDir, fileName);

            _ = Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                Debug.WriteLine($"[ImageService] [{DateTime.Now:HH:mm:ss.fff}] 🔔 텔레메트리 신규 캡처 감지 -> '{fileName}' 로드 시작");

                try
                {
                    for (int attempt = 1; attempt <= 10; attempt++)
                    {
                        if (TryLoadCompletedImage(targetPngPath, attempt, out BitmapImage? bitmap, out long fileSize) && bitmap != null)
                        {
                            sw.Stop();
                            _lastProcessedFileName = fileName;

                            Debug.WriteLine(
                                $"[ImageService] [{DateTime.Now:HH:mm:ss.fff}] ✅ 로드 완료! " +
                                $"(시도: {attempt}회차, 총 소요: {sw.ElapsedMilliseconds}ms, 크기: {fileSize:N0} bytes, 해상도: {bitmap.PixelWidth}x{bitmap.PixelHeight})");

                            BitmapImage finalImage = DrawBoundingBoxes(bitmap, targetPngPath);
                            ImageCaptured?.Invoke(finalImage, targetPngPath, fileSize);
                            return;
                        }

                        await Task.Delay(50);
                    }

                    sw.Stop();
                    Debug.WriteLine($"[ImageService] [{DateTime.Now:HH:mm:ss.fff}] ❌ 최종 로드 실패 (10회 시도 초과, {sw.ElapsedMilliseconds}ms 경과) | 경로: {targetPngPath}");
                }
                finally
                {
                    Interlocked.Exchange(ref _isProcessing, 0);
                }
            });
        }

        private static bool TryLoadCompletedImage(string filePath, int attempt, out BitmapImage? bitmap, out long fileSizeBytes)
        {
            bitmap = null;
            fileSizeBytes = 0;
            string shortName = Path.GetFileName(filePath);

            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists)
                {
                    Debug.WriteLine($"[ImageService] [{DateTime.Now:HH:mm:ss.fff}] ⏳ [{attempt}회차] 파일이 아직 디스크에 없음: {shortName}");
                    return false;
                }

                if (info.Length <= 0)
                {
                    Debug.WriteLine($"[ImageService] [{DateTime.Now:HH:mm:ss.fff}] ⏳ [{attempt}회차] 파일 생성됨 (크기 0 byte - 언리얼 쓰기 시작 전): {shortName}");
                    return false;
                }

                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length <= 0)
                {
                    Debug.WriteLine($"[ImageService] [{DateTime.Now:HH:mm:ss.fff}] ⏳ [{attempt}회차] 스트림 길이 0 byte: {shortName}");
                    return false;
                }

                using var ms = new MemoryStream();
                fs.CopyTo(ms);
                ms.Position = 0;
                fileSizeBytes = ms.Length;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();

                bitmap = bmp;
                return true;
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine($"[ImageService] [{DateTime.Now:HH:mm:ss.fff}] 🔒 [{attempt}회차] 파일 쓰기 잠금(IOException) 대기 중: {shortName} ({ioEx.Message})");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageService] [{DateTime.Now:HH:mm:ss.fff}] ⚠️ [{attempt}회차] PNG 디코딩 미완료({ex.GetType().Name}): {shortName} (언리얼이 아직 쓰는 중)");
                return false;
            }
        }

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

            var groundTruths = new List<Rect>();
            if (hasGroundTruth)
            {
                try
                {
                    using var fs = new FileStream(labelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs);
                    string? line;
                    while ((line = sr.ReadLine()) != null)
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
                catch { }
            }

            if (groundTruths.Count == 0 && detections.Count == 0) return source;

            var drawingVisual = new DrawingVisual();
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawImage(source, new Rect(0, 0, pixelWidth, pixelHeight));

                var hitPen = new Pen(Brushes.Cyan, 2.5);
                hitPen.Freeze();

                var missedPen = new Pen(Brushes.Yellow, 2.0);
                missedPen.DashStyle = DashStyles.Dash;
                missedPen.Freeze();

                var falseAlarmPen = new Pen(Brushes.Red, 2.0);
                falseAlarmPen.Freeze();

                bool[] gtMatched = new bool[groundTruths.Count];

                foreach (DetectionBox det in detections)
                {
                    bool isHit = false;
                    for (int i = 0; i < groundTruths.Count; i++)
                    {
                        if (CalculateIoU(det.Rect, groundTruths[i]) >= 0.4)
                        {
                            isHit = true;
                            gtMatched[i] = true;
                            break;
                        }
                    }

                    dc.DrawRectangle(null, isHit ? hitPen : falseAlarmPen, det.Rect);
                }

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
            _detector?.Dispose();
        }
    }
}