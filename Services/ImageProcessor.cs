using System.Windows.Media;
using System.Windows.Media.Imaging;
// C++ 엔진의 네임스페이스를 using 합니다.
using ImageProcessingEngine;

namespace ImageProcessing.Services
{
    public class ImageProcessor
    {
        // C++ 엔진 인스턴스
        private readonly ImageEngine _engine = new ImageEngine();

        public BitmapImage ApplyGrayscale(BitmapImage source)
        {
            // 1. BitmapImage를 byte 배열로 변환
            var bitmap = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            bitmap.CopyPixels(pixels, stride, 0);

            // 2. C++ 엔진의 그레이스케일 함수 호출
            _engine.ApplyGrayscale(pixels, width, height);

            // 3. 결과 byte 배열을 다시 BitmapImage로 변환
            var processedBitmap = BitmapSource.Create(width, height, 96, 96,
                PixelFormats.Bgra32, null, pixels, stride);

            // BitmapSource를 BitmapImage로 변환 (기존에 만들었던 로직 활용)
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(processedBitmap));
            using (var stream = new System.IO.MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                var result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                return result;
            }
        }

        // TODO: ApplyGaussianBlur, ApplySobel 등 다른 메소드들도 위와 같은 방식으로 구현
    }
}