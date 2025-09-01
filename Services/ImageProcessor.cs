using System.IO;
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

        // Helper method to convert BitmapImage to byte array and back
        private BitmapImage ProcessImage(BitmapImage source, System.Action<byte[], int, int> processAction)
        {
            if (source == null) return null;

            // 1. BitmapImage를 BGRA32 포맷의 byte 배열로 변환
            var bitmap = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            bitmap.CopyPixels(pixels, stride, 0);

            // 2. C++ 엔진의 함수 호출
            processAction(pixels, width, height);

            // 3. 결과 byte 배열을 다시 BitmapImage로 변환
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
                result.Freeze(); // UI 스레드 간 충돌 방지
                return result;
            }
        }

        public BitmapImage ApplyGrayscale(BitmapImage source)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyGrayscale(pixels, width, height));
        }

        // --- 새로 추가된 함수 ---
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

        // 이진화 (임계값 128로 고정)
        public BitmapImage ApplyBinarization(BitmapImage source)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyBinarization(pixels, width, height, 128));
        }

        // 팽창 (3x3 커널 사용)
        public BitmapImage ApplyDilation(BitmapImage source)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyDilation(pixels, width, height, 3));
        }

        // 침식 (3x3 커널 사용)
        public BitmapImage ApplyErosion(BitmapImage source)
        {
            return ProcessImage(source, (pixels, width, height) => _engine.ApplyErosion(pixels, width, height, 3));
        }

        // TODO: ApplyFFT, ApplyTemplateMatching 등은 복잡도가 높아 별도의 구현이 필요합니다.
    }
}