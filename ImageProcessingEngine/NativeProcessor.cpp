#include "pch.h" // �̸� �����ϵ� ���
#include "NativeProcessor.h"

void NativeProcessor::ToGrayscale(unsigned char* pixels, int width, int height)
{
    int stride = width * 4; // BGRA �ȼ� ���� ����

    for (int y = 0; y < height; ++y)
    {
        for (int x = 0; x < width; ++x)
        {
            // ���� �ȼ��� �ּҸ� ���
            unsigned char* currentPixel = pixels + (y * stride) + (x * 4);

            // B, G, R ���� ������� Gray ���� ���
            unsigned char gray = (currentPixel[0] + currentPixel[1] + currentPixel[2]) / 3;

            // B, G, R ���� ��� gray ������ ����
            currentPixel[0] = gray; // Blue
            currentPixel[1] = gray; // Green
            currentPixel[2] = gray; // Red
        }
    }
}