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
        bool ApplyErosion(array<unsigned char>^ pixelBuffer, int width, int height, int kernelSize);
        // 이진화: threshold 파라미터 추가
        bool ApplyBinarization(array<unsigned char>^ pixelBuffer, int width, int height, int threshold);

        // 팽창: kernelSize 파라미터 추가
        bool ApplyDilation(array<unsigned char>^ pixelBuffer, int width, int height, int kernelSize);

        // 중앙값 필터: kernelSize 파라미터 추가
        bool ApplyMedianFilter(array<unsigned char>^ pixelBuffer, int width, int height, int kernelSize);
    };
}