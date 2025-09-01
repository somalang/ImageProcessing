#pragma once

using namespace System;

namespace ImageProcessingEngine {
    public ref class ImageEngine
    {
    public:
        bool ApplyGrayscale(array<unsigned char>^ pixelBuffer, int width, int height);

        // --- 새로 추가된 함수 ---
        bool ApplyGaussianBlur(array<unsigned char>^ pixelBuffer, int width, int height);
        bool ApplySobel(array<unsigned char>^ pixelBuffer, int width, int height);
        bool ApplyLaplacian(array<unsigned char>^ pixelBuffer, int width, int height);
        bool ApplyBinarization(array<unsigned char>^ pixelBuffer, int width, int height, int threshold);
        bool ApplyDilation(array<unsigned char>^ pixelBuffer, int width, int height, int kernelSize);
        bool ApplyErosion(array<unsigned char>^ pixelBuffer, int width, int height, int kernelSize);
    };
}