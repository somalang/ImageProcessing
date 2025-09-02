// ===== ImageProcessingEngine.cpp 최상단 근처에 추가/수정 =====
#include "pch.h"
#include "ImageProcessingEngine.h"
#include "NativeProcessor.h"
#include <cmath>
#include <vector>
#include <algorithm>   // std::min/max
#include <vcclr.h>

using namespace System;
using namespace ImageProcessingEngine;

// ---- 표준이 아닌 M_PI를 직접 정의 (기존 int M_PI = 3.14; 는 삭제하세요) ----
#ifndef M_PI
#define M_PI 3.14159265358979323846264338327950288
#endif

// ---- 로컬 clamp 유틸 (std::clamp 없이도 동작) ----
template <typename T>
static inline T clamp_val(T v, T lo, T hi)
{
    return (v < lo) ? lo : ((v > hi) ? hi : v);
}

static inline unsigned char clamp_u8_from_float(float v)
{
    // 0..255 범위로 클램프 + 반올림
    v = (v < 0.f) ? 0.f : ((v > 255.f) ? 255.f : v);
    return static_cast<unsigned char>(v + 0.5f);
}

// =====================================================
//              FFT 전역 데이터 (float 버전)
// =====================================================
static std::vector<float> g_fftRealData;
static std::vector<float> g_fftImagData;
static int g_fftWidth = 0;
static int g_fftHeight = 0;

// 2의 제곱수 찾기
static inline int nextPowerOf2(int n)
{
    int power = 1;
    while (power < n) power <<= 1;
    return power;
}

// ==================== 1D FFT (Cooley–Tukey) ====================
static void fft1d(std::vector<float>& real, std::vector<float>& imag, bool inverse)
{
    int n = static_cast<int>(real.size());
    if (n <= 1) return;
    // 2의 거듭제곱만 처리
    if ((n & (n - 1)) != 0) return;

    // Bit reversal
    for (int i = 1, j = 0; i < n; ++i)
    {
        int bit = n >> 1;
        for (; j & bit; bit >>= 1) j ^= bit;
        j ^= bit;
        if (i < j)
        {
            std::swap(real[i], real[j]);
            std::swap(imag[i], imag[j]);
        }
    }

    // 스테이지
    for (int len = 2; len <= n; len <<= 1)
    {
        float ang = static_cast<float>(2.0 * M_PI) / len * (inverse ? 1.f : -1.f);
        float wlenReal = std::cos(ang), wlenImag = std::sin(ang);

        for (int i = 0; i < n; i += len)
        {
            float wReal = 1.f, wImag = 0.f;
            int half = len >> 1;
            for (int j = 0; j < half; ++j)
            {
                float uReal = real[i + j], uImag = imag[i + j];
                float tReal = real[i + j + half], tImag = imag[i + j + half];

                float vReal = tReal * wReal - tImag * wImag;
                float vImag = tReal * wImag + tImag * wReal;

                real[i + j] = uReal + vReal;
                imag[i + j] = uImag + vImag;
                real[i + j + half] = uReal - vReal;
                imag[i + j + half] = uImag - vImag;

                float nextWReal = wReal * wlenReal - wImag * wlenImag;
                float nextWImag = wReal * wlenImag + wImag * wlenReal;
                wReal = nextWReal; wImag = nextWImag;
            }
        }
    }

    if (inverse)
    {
        float invN = 1.f / n;
        for (int i = 0; i < n; ++i)
        {
            real[i] *= invN;
            imag[i] *= invN;
        }
    }
}

// ==================== Grayscale (빠르고 안전) ====================
bool ImageEngine::ApplyGrayscale(array<unsigned char>^ pixelBuffer, int width, int height)
{
    try
    {
        // BGRA 순서
        const float wR = 0.299f, wG = 0.587f, wB = 0.114f;
        int total = width * height;

        // OpenMP 병렬화 (미사용 환경에서도 pragma는 무시됨)
#ifdef _OPENMP
#pragma omp parallel for
#endif
        for (int i = 0; i < total; ++i)
        {
            int idx = i * 4;
            float b = pixelBuffer[idx + 0];
            float g = pixelBuffer[idx + 1];
            float r = pixelBuffer[idx + 2];

            unsigned char gray = clamp_u8_from_float(r * wR + g * wG + b * wB);

            pixelBuffer[idx + 0] = gray;
            pixelBuffer[idx + 1] = gray;
            pixelBuffer[idx + 2] = gray;
            // alpha는 보존
        }
        return true;
    }
    catch (...)
    {
        return false;
    }
}

// ==================== 2D FFT (행/열 분리 + 병렬) ====================
bool ImageEngine::ApplyFFT(array<unsigned char>^ pixelBuffer, int width, int height)
{
    try
    {
        int paddedWidth = nextPowerOf2(width);
        int paddedHeight = nextPowerOf2(height);

        g_fftWidth = paddedWidth;
        g_fftHeight = paddedHeight;
        g_fftRealData.assign(paddedWidth * paddedHeight, 0.f);
        g_fftImagData.assign(paddedWidth * paddedHeight, 0.f);

        // 그레이스케일 + 패딩
        const float wR = 0.299f, wG = 0.587f, wB = 0.114f;

#ifdef _OPENMP
#pragma omp parallel for
#endif
        for (int y = 0; y < paddedHeight; ++y)
        {
            for (int x = 0; x < paddedWidth; ++x)
            {
                int pIdx = y * paddedWidth + x;
                if (x < width && y < height)
                {
                    int idx = (y * width + x) * 4;
                    float gray = pixelBuffer[idx + 2] * wR + pixelBuffer[idx + 1] * wG + pixelBuffer[idx + 0] * wB;
                    g_fftRealData[pIdx] = gray;
                }
                else
                {
                    g_fftRealData[pIdx] = 0.f;
                }
                g_fftImagData[pIdx] = 0.f;
            }
        }

        // 행별 FFT
#ifdef _OPENMP
#pragma omp parallel for
#endif
        for (int y = 0; y < paddedHeight; ++y)
        {
            std::vector<float> r(paddedWidth), im(paddedWidth);
            for (int x = 0; x < paddedWidth; ++x)
            {
                r[x] = g_fftRealData[y * paddedWidth + x];
                im[x] = g_fftImagData[y * paddedWidth + x];
            }
            fft1d(r, im, false);
            for (int x = 0; x < paddedWidth; ++x)
            {
                g_fftRealData[y * paddedWidth + x] = r[x];
                g_fftImagData[y * paddedWidth + x] = im[x];
            }
        }

        // 열별 FFT
#ifdef _OPENMP
#pragma omp parallel for
#endif
        for (int x = 0; x < paddedWidth; ++x)
        {
            std::vector<float> r(paddedHeight), im(paddedHeight);
            for (int y = 0; y < paddedHeight; ++y)
            {
                r[y] = g_fftRealData[y * paddedWidth + x];
                im[y] = g_fftImagData[y * paddedWidth + x];
            }
            fft1d(r, im, false);
            for (int y = 0; y < paddedHeight; ++y)
            {
                g_fftRealData[y * paddedWidth + x] = r[y];
                g_fftImagData[y * paddedWidth + x] = im[y];
            }
        }

        // Magnitude → log scale → 0..255 정규화
        float maxMag = 0.f;
        std::vector<float> mag(width * height, 0.f);

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                int o = y * width + x;
                int p = y * paddedWidth + x;
                float re = g_fftRealData[p], im = g_fftImagData[p];
                float m = std::sqrt(re * re + im * im);
                mag[o] = m;
                if (m > maxMag) maxMag = m;
            }
        }

        if (maxMag > 0.f)
        {
            float denom = std::log1p(maxMag); // log(1+max)
            int total = width * height;

#ifdef _OPENMP
#pragma omp parallel for
#endif
            for (int i = 0; i < total; ++i)
            {
                float val = std::log1p(mag[i]) / denom * 255.f;
                unsigned char v = clamp_u8_from_float(val);
                int idx = i * 4;
                pixelBuffer[idx + 0] = v;
                pixelBuffer[idx + 1] = v;
                pixelBuffer[idx + 2] = v;
                pixelBuffer[idx + 3] = 255;
            }
        }
        return true;
    }
    catch (...)
    {
        return false;
    }
}

bool ImageEngine::ApplyIFFT(array<unsigned char>^ pixelBuffer, int width, int height)
{
    try
    {
        if (g_fftRealData.empty() || g_fftImagData.empty()) return false;

        // 복사본으로 작업
        std::vector<float> r = g_fftRealData;
        std::vector<float> im = g_fftImagData;

        // 열별 IFFT
#ifdef _OPENMP
#pragma omp parallel for
#endif
        for (int x = 0; x < g_fftWidth; ++x)
        {
            std::vector<float> tr(g_fftHeight), ti(g_fftHeight);
            for (int y = 0; y < g_fftHeight; ++y)
            {
                tr[y] = r[y * g_fftWidth + x];
                ti[y] = im[y * g_fftWidth + x];
            }
            fft1d(tr, ti, true);
            for (int y = 0; y < g_fftHeight; ++y)
            {
                r[y * g_fftWidth + x] = tr[y];
                im[y * g_fftWidth + x] = ti[y];
            }
        }

        // 행별 IFFT
#ifdef _OPENMP
#pragma omp parallel for
#endif
        for (int y = 0; y < g_fftHeight; ++y)
        {
            std::vector<float> tr(g_fftWidth), ti(g_fftWidth);
            for (int x = 0; x < g_fftWidth; ++x)
            {
                tr[x] = r[y * g_fftWidth + x];
                ti[x] = im[y * g_fftWidth + x];
            }
            fft1d(tr, ti, true);
            for (int x = 0; x < g_fftWidth; ++x)
            {
                r[y * g_fftWidth + x] = tr[x];
                im[y * g_fftWidth + x] = ti[x];
            }
        }

        // 원래 크기만 써서 복원 이미지 작성
#ifdef _OPENMP
#pragma omp parallel for
#endif
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                int idx = (y * width + x) * 4;
                int p = y * g_fftWidth + x;
                unsigned char v = clamp_u8_from_float(std::fabs(r[p]));
                pixelBuffer[idx + 0] = v;
                pixelBuffer[idx + 1] = v;
                pixelBuffer[idx + 2] = v;
                pixelBuffer[idx + 3] = 255;
            }
        }
        return true;
    }
    catch (...)
    {
        return false;
    }
}

bool ImageEngine::HasFFTData()
{
    return !g_fftRealData.empty() && !g_fftImagData.empty();
}

void ImageEngine::ClearFFTData()
{
    g_fftRealData.clear();
    g_fftImagData.clear();
    g_fftWidth = g_fftHeight = 0;
}

// ==================== Gaussian Blur (Separable + 정규화) ====================
static std::vector<float> makeGaussian1D(int radius, float sigma)
{
    int size = radius * 2 + 1;
    std::vector<float> k(size);
    float sum = 0.f;
    float inv2s2 = 1.f / (2.f * sigma * sigma);
    for (int i = -radius; i <= radius; ++i)
    {
        float v = std::exp(-(i * i) * inv2s2);
        k[i + radius] = v;
        sum += v;
    }
    // 정규화
    float inv = 1.f / sum;
    for (float& v : k) v *= inv;
    return k;
}

static inline int clamp_index(int v, int lo, int hi)
{
    return (v < lo) ? lo : ((v > hi) ? hi : v);
}

bool ImageProcessingEngine::ImageEngine::ApplyGaussianBlur(array<unsigned char>^ pixelBuffer, int width, int height)
{
    try
    {
        // 기본 파라미터(필요시 변경 가능)
        const int   radius = 2;          // 커널크기 5 (= 2*radius+1)
        const float sigma = 1.0f;

        // 임시 버퍼 (float로 누적 → 품질↑)
        const int N = width * height;
        std::vector<float> tmpB(N), tmpG(N), tmpR(N);

        // 수평 패스
        auto k = makeGaussian1D(radius, sigma);
#ifdef _OPENMP
#pragma omp parallel for
#endif
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                float sb = 0.f, sg = 0.f, sr = 0.f;
                for (int t = -radius; t <= radius; ++t)
                {
                    int xx = clamp_index(x + t, 0, width - 1);
                    int idx = (y * width + xx) * 4;
                    float w = k[t + radius];
                    sb += pixelBuffer[idx + 0] * w;
                    sg += pixelBuffer[idx + 1] * w;
                    sr += pixelBuffer[idx + 2] * w;
                }
                int o = y * width + x;
                tmpB[o] = sb; tmpG[o] = sg; tmpR[o] = sr;
            }
        }

        // 수직 패스 + 출력
#ifdef _OPENMP
#pragma omp parallel for
#endif
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                float sb = 0.f, sg = 0.f, sr = 0.f;
                for (int t = -radius; t <= radius; ++t)
                {
                    int yy = clamp_index(y + t, 0, height - 1);
                    int o = yy * width + x;
                    float w = k[t + radius];
                    sb += tmpB[o] * w;
                    sg += tmpG[o] * w;
                    sr += tmpR[o] * w;
                }
                int idx = (y * width + x) * 4;
                pixelBuffer[idx + 0] = clamp_u8_from_float(sb);
                pixelBuffer[idx + 1] = clamp_u8_from_float(sg);
                pixelBuffer[idx + 2] = clamp_u8_from_float(sr);
                // alpha는 그대로 두거나 255로 고정
                // pixelBuffer[idx + 3] = 255;
            }
        }
        return true;
    }
    catch (...)
    {
        return false;
    }
}

// ===== 이하 기존 래퍼들(Sobel, Laplacian, Morphology 등)은 그대로 유지 =====
// 
// // Sobel
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

// 중앙값 필터 래퍼 함수 추가
bool ImageProcessingEngine::ImageEngine::ApplyMedianFilter(array<unsigned char>^ pixelBuffer, int width, int height, int kernelSize)
{
    pin_ptr<unsigned char> nativePixels = &pixelBuffer[0];
    NativeProcessor processor;
    processor.ApplyMedianFilter(nativePixels, width, height, kernelSize);
    return true;
}

// 필요시 OpenMP로 바깥 y 루프에 parallel for만 달아줘도 체감 속도 올라갑니다.
