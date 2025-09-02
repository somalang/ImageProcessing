#pragma once

class NativeProcessor
{
public:
    // 픽셀 데이터를 받아 그레이스케일로 변환하는 함수
    void ToGrayscale(unsigned char* pixels, int width, int height);

    // --- 새로 추가된 함수 선언 ---

    // 가우시안 블러 (노이즈 제거)
    void ApplyGaussianBlur(unsigned char* pixels, int width, int height);

    // 소벨 엣지 검출
    void ApplySobel(unsigned char* pixels, int width, int height);

    // 라플라시안 엣지 검출
    void ApplyLaplacian(unsigned char* pixels, int width, int height);

    // 이진화 (임계값 처리)
    void ApplyBinarization(unsigned char* pixels, int width, int height, int threshold);

    // 팽창 연산 (Morphology)
    void ApplyDilation(unsigned char* pixels, int width, int height, int kernelSize);

    // 침식 연산 (Morphology)
    void ApplyErosion(unsigned char* pixels, int width, int height, int kernelSize);

    // 중앙값 필터
    void ApplyMedianFilter(unsigned char* pixels, int width, int height, int kernelSize);

    void Binarize(unsigned char* pixels, int width, int height, int threshold);
    void Dilate(unsigned char* pixels, int width, int height, int kernelSize);
};