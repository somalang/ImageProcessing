#include "pch.h"
#include "ImageProcessingEngine.h"
#include "NativeProcessor.h"
#include <vcclr.h>

bool ImageProcessingEngine::ImageEngine::ApplyGrayscale(array<unsigned char>^ pixelBuffer, int width, int height)
{
    pin_ptr<unsigned char> nativePixels = &pixelBuffer[0];
    NativeProcessor processor;
    processor.ToGrayscale(nativePixels, width, height);
    return true;
}

// --- ���⼭���� ���� �߰��� ���� �Լ��� ---

bool ImageProcessingEngine::ImageEngine::ApplyGaussianBlur(array<unsigned char>^ pixelBuffer, int width, int height)
{
    pin_ptr<unsigned char> nativePixels = &pixelBuffer[0];
    NativeProcessor processor;
    processor.ApplyGaussianBlur(nativePixels, width, height);
    return true;
}

bool ImageProcessingEngine::ImageEngine::ApplySobel(array<unsigned char>^ pixelBuffer, int width, int height)
{
    pin_ptr<unsigned char> nativePixels = &pixelBuffer[0];
    NativeProcessor processor;
    processor.ApplySobel(nativePixels, width, height);
    return true;
}

bool ImageProcessingEngine::ImageEngine::ApplyLaplacian(array<unsigned char>^ pixelBuffer, int width, int height)
{
    pin_ptr<unsigned char> nativePixels = &pixelBuffer[0];
    NativeProcessor processor;
    processor.ApplyLaplacian(nativePixels, width, height);
    return true;
}

bool ImageProcessingEngine::ImageEngine::ApplyBinarization(array<unsigned char>^ pixelBuffer, int width, int height, int threshold)
{
    pin_ptr<unsigned char> nativePixels = &pixelBuffer[0];
    NativeProcessor processor;
    processor.ApplyBinarization(nativePixels, width, height, threshold);
    return true;
}

bool ImageProcessingEngine::ImageEngine::ApplyDilation(array<unsigned char>^ pixelBuffer, int width, int height, int kernelSize)
{
    pin_ptr<unsigned char> nativePixels = &pixelBuffer[0];
    NativeProcessor processor;
    processor.ApplyDilation(nativePixels, width, height, kernelSize);
    return true;
}

bool ImageProcessingEngine::ImageEngine::ApplyErosion(array<unsigned char>^ pixelBuffer, int width, int height, int kernelSize)
{
    pin_ptr<unsigned char> nativePixels = &pixelBuffer[0];
    NativeProcessor processor;
    processor.ApplyErosion(nativePixels, width, height, kernelSize);
    return true;
}

// �߾Ӱ� ���� ���� �Լ� �߰�
bool ImageProcessingEngine::ImageEngine::ApplyMedianFilter(array<unsigned char>^ pixelBuffer, int width, int height, int kernelSize)
{
    pin_ptr<unsigned char> nativePixels = &pixelBuffer[0];
    NativeProcessor processor;
    processor.ApplyMedianFilter(nativePixels, width, height, kernelSize);
    return true;
}