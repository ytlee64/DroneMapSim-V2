using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace DroneMapGCS
{
    /// <summary>
    /// YOLOv8(onnx로 변환된 best.onnx) 모델을 로드하여 캡처된 FPV 이미지에서
    /// 차량(vehicle)을 탐지하는 서비스. 학습은 apps\AI\Yolo\train_yolo.py로 수행되었고,
    /// apps\AI\Yolo\runs\detect\drone_vehicle_model\weights\best.onnx 를 사용합니다.
    /// </summary>
    public class YoloDetectorService : IDisposable
    {
        private readonly InferenceSession? _session;
        private readonly int _inputSize;
        private readonly float _confThreshold;
        private readonly float _iouThreshold;
        private readonly string _inputName;

        public bool IsAvailable => _session != null;

        public YoloDetectorService(string modelPath, int inputSize = 640, float confThreshold = 0.35f, float iouThreshold = 0.45f)
        {
            _inputSize = inputSize;
            _confThreshold = confThreshold;
            _iouThreshold = iouThreshold;

            if (File.Exists(modelPath))
            {
                try
                {
                    _session = new InferenceSession(modelPath);
                    _inputName = _session.InputMetadata.Keys.First();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[YOLO 모델 로드 실패] {ex.Message}");
                    _session = null;
                    _inputName = string.Empty;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[YOLO 모델 없음] {modelPath}");
                _inputName = string.Empty;
            }
        }

        /// <summary>
        /// 이미지를 추론하여 탐지된 박스 목록(원본 픽셀 좌표계)을 반환합니다.
        /// </summary>
        public List<DetectionBox> Detect(BitmapSource source)
        {
            var results = new List<DetectionBox>();
            if (_session == null) return results;

            int origWidth = source.PixelWidth;
            int origHeight = source.PixelHeight;

            // 1. 레터박스(Letterbox) 리사이즈: 종횡비 유지 + 패딩(114,114,114)
            float scale = Math.Min((float)_inputSize / origWidth, (float)_inputSize / origHeight);
            int scaledWidth = (int)Math.Round(origWidth * scale);
            int scaledHeight = (int)Math.Round(origHeight * scale);
            int padX = (_inputSize - scaledWidth) / 2;
            int padY = (_inputSize - scaledHeight) / 2;

            var letterboxed = CreateLetterboxBitmap(source, scaledWidth, scaledHeight, padX, padY, _inputSize);

            // 2. 픽셀 추출 -> NCHW float 텐서 (RGB, 0~1 정규화)
            var input = new DenseTensor<float>(new[] { 1, 3, _inputSize, _inputSize });
            FillInputTensor(letterboxed, input);

            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, input) };

            using var outputs = _session.Run(inputs);
            var outputTensor = outputs.First().AsTensor<float>();
            // 출력 shape: (1, 4+nc, numAnchors) - ultralytics YOLOv8 표준
            int channels = outputTensor.Dimensions[1];
            int numAnchors = outputTensor.Dimensions[2];
            int numClasses = channels - 4;

            var candidates = new List<DetectionBox>();

            for (int i = 0; i < numAnchors; i++)
            {
                float bestScore = 0f;
                int bestClass = 0;
                for (int c = 0; c < numClasses; c++)
                {
                    float score = outputTensor[0, 4 + c, i];
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestClass = c;
                    }
                }

                if (bestScore < _confThreshold) continue;

                float cx = outputTensor[0, 0, i];
                float cy = outputTensor[0, 1, i];
                float w = outputTensor[0, 2, i];
                float h = outputTensor[0, 3, i];

                // 패딩 제거 후 원본 스케일로 역변환
                float x1 = (cx - w * 0.5f - padX) / scale;
                float y1 = (cy - h * 0.5f - padY) / scale;
                float x2 = (cx + w * 0.5f - padX) / scale;
                float y2 = (cy + h * 0.5f - padY) / scale;

                x1 = Math.Clamp(x1, 0, origWidth);
                y1 = Math.Clamp(y1, 0, origHeight);
                x2 = Math.Clamp(x2, 0, origWidth);
                y2 = Math.Clamp(y2, 0, origHeight);

                if (x2 - x1 < 1 || y2 - y1 < 1) continue;

                candidates.Add(new DetectionBox
                {
                    Rect = new Rect(x1, y1, x2 - x1, y2 - y1),
                    Confidence = bestScore,
                    ClassId = bestClass
                });
            }

            return NonMaxSuppression(candidates, _iouThreshold);
        }

        private static WriteableBitmap CreateLetterboxBitmap(BitmapSource source, int scaledWidth, int scaledHeight, int padX, int padY, int targetSize)
        {
            var resized = new TransformedBitmap(source, new ScaleTransform(
                (double)scaledWidth / source.PixelWidth,
                (double)scaledHeight / source.PixelHeight));

            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(114, 114, 114)), null, new Rect(0, 0, targetSize, targetSize));
                dc.DrawImage(resized, new Rect(padX, padY, scaledWidth, scaledHeight));
            }

            var rtb = new RenderTargetBitmap(targetSize, targetSize, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            var wb = new WriteableBitmap(new FormatConvertedBitmap(rtb, PixelFormats.Bgr24, null, 0));
            return wb;
        }

        private static void FillInputTensor(WriteableBitmap bitmap, DenseTensor<float> tensor)
        {
            int size = bitmap.PixelWidth;
            int stride = bitmap.BackBufferStride;
            byte[] pixels = new byte[stride * bitmap.PixelHeight];
            bitmap.CopyPixels(pixels, stride, 0);

            for (int y = 0; y < size; y++)
            {
                int rowOffset = y * stride;
                for (int x = 0; x < size; x++)
                {
                    int idx = rowOffset + x * 3;
                    byte b = pixels[idx];
                    byte g = pixels[idx + 1];
                    byte r = pixels[idx + 2];

                    tensor[0, 0, y, x] = r / 255f;
                    tensor[0, 1, y, x] = g / 255f;
                    tensor[0, 2, y, x] = b / 255f;
                }
            }
        }

        private static List<DetectionBox> NonMaxSuppression(List<DetectionBox> boxes, float iouThreshold)
        {
            var result = new List<DetectionBox>();
            var sorted = boxes.OrderByDescending(b => b.Confidence).ToList();

            while (sorted.Count > 0)
            {
                var best = sorted[0];
                result.Add(best);
                sorted.RemoveAt(0);

                sorted.RemoveAll(b => IoU(best.Rect, b.Rect) > iouThreshold);
            }

            return result;
        }

        private static double IoU(Rect a, Rect b)
        {
            Rect intersection = Rect.Intersect(a, b);
            if (intersection.IsEmpty) return 0;

            double interArea = intersection.Width * intersection.Height;
            double unionArea = (a.Width * a.Height) + (b.Width * b.Height) - interArea;
            return unionArea <= 0 ? 0 : interArea / unionArea;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }

    public class DetectionBox
    {
        public Rect Rect { get; set; }
        public float Confidence { get; set; }
        public int ClassId { get; set; }
    }
}
