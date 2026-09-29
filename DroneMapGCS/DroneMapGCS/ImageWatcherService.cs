using System;
using System.IO;
using System.Threading.Tasks;
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
                ImageCaptured?.Invoke(bitmap, filePath, fileSizeBytes);
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
}