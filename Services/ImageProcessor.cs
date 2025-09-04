using ImageProcessingEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        public BitmapSource Crop(BitmapSource source, Rect rect)
        {
            if (rect.IsEmpty || rect.Width == 0 || rect.Height == 0)
            {
                return null;
            }

            // Rect가 이미지 경계를 벗어나지 않도록 조정
            rect.Intersect(new Rect(0, 0, source.PixelWidth, source.PixelHeight));

            if (rect.IsEmpty)
            {
                return null;
            }

            return new CroppedBitmap(source, new Int32Rect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
        }

        public BitmapSource ClearSelection(BitmapSource source, Rect rect)
        {
            if (rect.IsEmpty || rect.Width == 0 || rect.Height == 0)
            {
                return source;
            }

            // Rect가 이미지 경계를 벗어나지 않도록 조정
            rect.Intersect(new Rect(0, 0, source.PixelWidth, source.PixelHeight));

            if (rect.IsEmpty)
            {
                return source;
            }
            var formatted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            var writeableBitmap = new WriteableBitmap(formatted);

            //var writeableBitmap = new WriteableBitmap(source);
            int stride = writeableBitmap.PixelWidth * (writeableBitmap.Format.BitsPerPixel / 8);
            byte[] pixelData = new byte[stride * writeableBitmap.PixelHeight];
            writeableBitmap.CopyPixels(pixelData, stride, 0);

            for (int y = (int)rect.Y; y < (int)(rect.Y + rect.Height); y++)
            {
                for (int x = (int)rect.X; x < (int)(rect.X + rect.Width); x++)
                {
                    int index = y * stride + x * 4; // BGRA32 가정

                    // 투명하게 처리
                    pixelData[index] = 0;       // Blue
                    pixelData[index + 1] = 0;   // Green
                    pixelData[index + 2] = 0;   // Red
                    pixelData[index + 3] = 0;   // Alpha - 투명

                    // 붉은색 처리 - 확인용
                    //pixelData[index] = 0;       // Blue
                    //pixelData[index + 1] = 0;   // Green
                    //pixelData[index + 2] = 255; // Red
                    //pixelData[index + 3] = 255; // Opaque

                }
            }

            writeableBitmap.WritePixels(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight), pixelData, stride, 0);
            return writeableBitmap;
        }
        public BitmapSource Paste(BitmapSource destination, BitmapSource source, Point location)
        {
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // 기존 이미지 그리기
                drawingContext.DrawImage(destination, new Rect(0, 0, destination.PixelWidth, destination.PixelHeight));
                // 붙여넣을 이미지 그리기
                drawingContext.DrawImage(source, new Rect(location.X, location.Y, source.PixelWidth, source.PixelHeight));
            }

            var renderTargetBitmap = new RenderTargetBitmap(destination.PixelWidth, destination.PixelHeight, destination.DpiX, destination.DpiY, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            renderTargetBitmap.Freeze();
            return renderTargetBitmap;
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
