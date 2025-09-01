#pragma once

class NativeProcessor
{
public:
    // 실제 픽셀 데이터를 받아 그레이스케일로 변환하는 함수
    void ToGrayscale(unsigned char* pixels, int width, int height);

    // TODO: 여기에 다른 영상처리 함수들을 선언합니다.
    // 예: void Sobel(unsigned char* pixels, int width, int height);
};