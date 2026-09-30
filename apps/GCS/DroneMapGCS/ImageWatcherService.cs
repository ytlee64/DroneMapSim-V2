using System;
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
        public event Action<BitmapImage, string, long>? ImageCaptured;

        public ImageWatcherService(string watchPath)
        {
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

            // 3. ⭐ 언리얼 엔진 전용: 임시 파일(.tmp)에서 .png로 이름 바뀔 때 감지!
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
                bitmap = DrawYoloBoundingBoxes(bitmap, filePath);
                ImageCaptured?.Invoke(bitmap, filePath, fileSizeBytes);
            }
        }

        /// <summary>
        /// 이미지와 동일한 이름의 YOLO 라벨(.txt) 파일을 읽어
        /// 정규화된 바운딩 박스를 초록색 사각형으로 그려 새 비트맵을 반환합니다.
        /// (라벨 파일이 없으면 원본 비트맵을 그대로 반환)
        /// </summary>
        private static BitmapImage DrawYoloBoundingBoxes(BitmapImage source, string imageFilePath)
        {
            string labelFilePath = Path.ChangeExtension(imageFilePath, ".txt");
            if (!File.Exists(labelFilePath))
            {
                return source;
            }

            int pixelWidth = source.PixelWidth;
            int pixelHeight = source.PixelHeight;
            if (pixelWidth <= 0 || pixelHeight <= 0)
            {
                return source;
            }

            var drawingVisual = new DrawingVisual();
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawImage(source, new Rect(0, 0, pixelWidth, pixelHeight));

                var pen = new Pen(Brushes.LimeGreen, 2.0);
                pen.Freeze();

                foreach (string line in File.ReadAllLines(labelFilePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    // YOLO 포맷: class_id center_x center_y width height (모두 0~1 정규화 값)
                    if (parts.Length < 5) continue;

                    if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double centerXNorm) ||
                        !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double centerYNorm) ||
                        !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double widthNorm) ||
                        !double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double heightNorm))
                    {
                        continue;
                    }

                    double boxWidth = widthNorm * pixelWidth;
                    double boxHeight = heightNorm * pixelHeight;
                    double boxLeft = (centerXNorm * pixelWidth) - (boxWidth * 0.5);
                    double boxTop = (centerYNorm * pixelHeight) - (boxHeight * 0.5);

                    dc.DrawRectangle(null, pen, new Rect(boxLeft, boxTop, boxWidth, boxHeight));
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

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
}