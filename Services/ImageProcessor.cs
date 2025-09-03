using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using ImageProcessingEngine;
using System;

namespace ImageProcessing.Services
{
    public class ImageProcessor
    {
        private readonly ImageEngine _engine = new ImageEngine();
        private readonly Stack<BitmapImage> _undoStack = new Stack<BitmapImage>();
        private readonly Stack<BitmapImage> _redoStack = new Stack<BitmapImage>();
        private BitmapImage _currentImage;

        // 되돌리기/다시 실행 가능 여부를 외부에 노출하는 속성
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        // FFT 상태 확인
        public bool HasFFTData => _engine.HasFFTData();

        // Helper method to convert BitmapImage to byte array and back
        private BitmapImage ProcessImage(BitmapImage source, Action<byte[], int, int> processAction)
        {
            if (source == null) return null;

            _undoStack.Push(source);
            _redoStack.Clear();

            var bitmap = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            bitmap.CopyPixels(pixels, stride, 0);

            processAction(pixels, width, height);

            var processedBitmap = BitmapSource.Create(width, height, 96, 96,
                PixelFormats.Bgra32, null, pixels, stride);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(processedBitmap));
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                var result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                _currentImage = result;
                return _currentImage;
            }
        }

        // ------------------ 기존 필터 ------------------
        public BitmapImage ApplyGrayscale(BitmapImage source)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyGrayscale(pixels, width, height));
        }

        public BitmapImage ApplyGaussianBlur(BitmapImage source)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyGaussianBlur(pixels, width, height));
        }

        public BitmapImage ApplySobel(BitmapImage source)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplySobel(pixels, width, height));
        }

        public BitmapImage ApplyLaplacian(BitmapImage source)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyLaplacian(pixels, width, height));
        }

        public BitmapImage ApplyBinarization(BitmapImage source, int param=128)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyBinarization(pixels, width, height, param));
        }

        public BitmapImage ApplyDilation(BitmapImage source, int param = 3)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyDilation(pixels, width, height, param));
        }

        public BitmapImage ApplyErosion(BitmapImage source, int param = 3)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyErosion(pixels, width, height, param));
        }

        public BitmapImage ApplyMedianFilter(BitmapImage source, int param = 3)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyMedianFilter(pixels, width, height, param));
        }

        // ------------------ FFT 관련 ------------------
        public BitmapImage ApplyFFT(BitmapImage source)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyFFT(pixels, width, height));
        }

        public BitmapImage ApplyIFFT(BitmapImage source)
        {
            if (!HasFFTData)
                throw new InvalidOperationException("FFT 데이터가 없습니다. 먼저 푸리에 변환을 수행해주세요.");

            return ProcessImage(source, (pixels, width, height) => _engine.ApplyIFFT(pixels, width, height));
        }

        public void ClearFFTData()
        {
            _engine.ClearFFTData();
        }

        // ------------------ Undo / Redo ------------------
        public BitmapImage Undo()
        {
            if (_undoStack.Count > 0)
            {
                _redoStack.Push(_currentImage);
                _currentImage = _undoStack.Pop();
            }
            return _currentImage;
        }

        public BitmapImage Redo()
        {
            if (_redoStack.Count > 0)
            {
                _undoStack.Push(_currentImage);
                _currentImage = _redoStack.Pop();
            }
            return _currentImage;
        }
    }
}
