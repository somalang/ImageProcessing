#include "pch.h"
#include "NativeProcessor.h"
#include <vector>
#include <cmath>
#include <algorithm>

// 기존 그레이스케일 함수
void NativeProcessor::ToGrayscale(unsigned char* pixels, int width, int height)
{
    int stride = width * 4;
    for (int y = 0; y < height; ++y)
    {
        for (int x = 0; x < width; ++x)
        {
            unsigned char* p = pixels + y * stride + x * 4;
            unsigned char gray = static_cast<unsigned char>(p[0] * 0.114 + p[1] * 0.587 + p[2] * 0.299);
            p[0] = p[1] = p[2] = gray;
        }
    }
}

// --- 여기서부터 새로 구현된 함수들 ---

// 컨볼루션 헬퍼 함수
void Convolve(const unsigned char* src, unsigned char* dst, int width, int height, const std::vector<float>& kernel, int kSize) {
    int kHalf = kSize / 2;
    int stride = width * 4;

    for (int y = kHalf; y < height - kHalf; ++y) {
        for (int x = kHalf; x < width - kHalf; ++x) {
            float sum_b = 0, sum_g = 0, sum_r = 0;
            for (int ky = -kHalf; ky <= kHalf; ++ky) {
                for (int kx = -kHalf; kx <= kHalf; ++kx) {
                    const unsigned char* p = src + (y + ky) * stride + (x + kx) * 4;
                    float k_val = kernel[(ky + kHalf) * kSize + (kx + kHalf)];
                    sum_b += p[0] * k_val;
                    sum_g += p[1] * k_val;
                    sum_r += p[2] * k_val;
                }
            }
            unsigned char* out_p = dst + y * stride + x * 4;
            out_p[0] = static_cast<unsigned char>(std::max(0.0f, std::min(255.0f, sum_b)));
            out_p[1] = static_cast<unsigned char>(std::max(0.0f, std::min(255.0f, sum_g)));
            out_p[2] = static_cast<unsigned char>(std::max(0.0f, std::min(255.0f, sum_r)));
            out_p[3] = 255; // Alpha
        }
    }
}


// 가우시안 블러: Wafer 표면의 미세 노이즈를 제거하여 결함 검출 정확도를 높입니다.
void NativeProcessor::ApplyGaussianBlur(unsigned char* pixels, int width, int height)
{
    // 5x5 가우시안 커널
    std::vector<float> kernel = {
        1, 4, 7, 4, 1,
        4, 16, 26, 16, 4,
        7, 26, 41, 26, 7,
        4, 16, 26, 16, 4,
        1, 4, 7, 4, 1
    };
    float kernelSum = 273.0f;
    for (float& val : kernel) {
        val /= kernelSum;
    }

    std::vector<unsigned char> temp(width * height * 4);
    memcpy(temp.data(), pixels, width * height * 4);

    Convolve(temp.data(), pixels, width, height, kernel, 5);
}

// 소벨 엣지 검출: 반도체 회로 패턴의 경계를 명확하게 추출하는 데 사용됩니다.
void NativeProcessor::ApplySobel(unsigned char* pixels, int width, int height)
{
    ToGrayscale(pixels, width, height); // 먼저 그레이스케일로 변환

    int stride = width * 4;
    std::vector<unsigned char> temp(width * height * 4);
    memcpy(temp.data(), pixels, width * height * 4);

    int Gx, Gy;
    int G;

    int sobel_x[3][3] = { {-1, 0, 1}, {-2, 0, 2}, {-1, 0, 1} };
    int sobel_y[3][3] = { {1, 2, 1}, {0, 0, 0}, {-1, -2, -1} };

    for (int y = 1; y < height - 1; ++y) {
        for (int x = 1; x < width - 1; ++x) {
            Gx = 0; Gy = 0;
            for (int i = -1; i <= 1; ++i) {
                for (int j = -1; j <= 1; ++j) {
                    int pixel_val = temp[(y + i) * stride + (x + j) * 4];
                    Gx += pixel_val * sobel_x[i + 1][j + 1];
                    Gy += pixel_val * sobel_y[i + 1][j + 1];
                }
            }
            G = static_cast<int>(sqrt(Gx * Gx + Gy * Gy));
            G = std::min(255, std::max(0, G));

            unsigned char* p = pixels + y * stride + x * 4;
            p[0] = p[1] = p[2] = G;
        }
    }
}

// 라플라시안 필터: Wafer의 미세한 스크래치나 크랙 같은 결함을 강조하는 데 유용합니다.
void NativeProcessor::ApplyLaplacian(unsigned char* pixels, int width, int height)
{
    ToGrayscale(pixels, width, height); // 먼저 그레이스케일로 변환

    std::vector<float> kernel = {
        0, -1, 0,
       -1,  4, -1,
        0, -1, 0
    };

    std::vector<unsigned char> temp(width * height * 4);
    memcpy(temp.data(), pixels, width * height * 4);

    Convolve(temp.data(), pixels, width, height, kernel, 3);
}


// 이진화: 회로 패턴과 배경을 명확하게 분리하여 패턴의 폭이나 간격을 측정하는 데 사용됩니다.
void NativeProcessor::ApplyBinarization(unsigned char* pixels, int width, int height, int threshold)
{
    ToGrayscale(pixels, width, height); // 먼저 그레이스케일로 변환
    int stride = width * 4;
    for (int y = 0; y < height; ++y)
    {
        for (int x = 0; x < width; ++x)
        {
            unsigned char* p = pixels + y * stride + x * 4;
            unsigned char gray = p[0];
            unsigned char binary = (gray > threshold) ? 255 : 0;
            p[0] = p[1] = p[2] = binary;
        }
    }
}

// 팽창(Dilation): 끊어진 회로 패턴을 연결하거나 작은 노이즈(먼지 등)를 제거하는 데 사용됩니다.
void NativeProcessor::ApplyDilation(unsigned char* pixels, int width, int height, int kernelSize)
{
    int stride = width * 4;
    std::vector<unsigned char> temp(width * height * 4);
    memcpy(temp.data(), pixels, width * height * 4);

    int kHalf = kernelSize / 2;

    for (int y = kHalf; y < height - kHalf; ++y) {
        for (int x = kHalf; x < width - kHalf; ++x) {
            unsigned char maxVal = 0;
            for (int ky = -kHalf; ky <= kHalf; ++ky) {
                for (int kx = -kHalf; kx <= kHalf; ++kx) {
                    maxVal = std::max(maxVal, temp[(y + ky) * stride + (x + kx) * 4]);
                }
            }
            unsigned char* p = pixels + y * stride + x * 4;
            p[0] = p[1] = p[2] = maxVal;
        }
    }
}


// 침식(Erosion): 회로 패턴의 얇은 부분을 제거하거나 붙어있는 객체를 분리하는 데 사용됩니다.
void NativeProcessor::ApplyErosion(unsigned char* pixels, int width, int height, int kernelSize)
{
    int stride = width * 4;
    std::vector<unsigned char> temp(width * height * 4);
    memcpy(temp.data(), pixels, width * height * 4);

    int kHalf = kernelSize / 2;

    for (int y = kHalf; y < height - kHalf; ++y) {
        for (int x = kHalf; x < width - kHalf; ++x) {
            unsigned char minVal = 255;
            for (int ky = -kHalf; ky <= kHalf; ++ky) {
                for (int kx = -kHalf; kx <= kHalf; ++kx) {
                    minVal = std::min(minVal, temp[(y + ky) * stride + (x + kx) * 4]);
                }
            }
            unsigned char* p = pixels + y * stride + x * 4;
            p[0] = p[1] = p[2] = minVal;
        }
    }
}