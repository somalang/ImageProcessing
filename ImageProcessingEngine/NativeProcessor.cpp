#include "pch.h" // 미리 컴파일된 헤더
#include "NativeProcessor.h"

void NativeProcessor::ToGrayscale(unsigned char* pixels, int width, int height)
{
    int stride = width * 4; // BGRA 픽셀 형식 기준

    for (int y = 0; y < height; ++y)
    {
        for (int x = 0; x < width; ++x)
        {
            // 현재 픽셀의 주소를 계산
            unsigned char* currentPixel = pixels + (y * stride) + (x * 4);

            // B, G, R 값의 평균으로 Gray 값을 계산
            unsigned char gray = (currentPixel[0] + currentPixel[1] + currentPixel[2]) / 3;

            // B, G, R 값을 모두 gray 값으로 변경
            currentPixel[0] = gray; // Blue
            currentPixel[1] = gray; // Green
            currentPixel[2] = gray; // Red
        }
    }
}