#pragma once

// C#의 System 네임스페이스 사용
using namespace System;

namespace ImageProcessingEngine {
    // public ref class는 C#에서 참조할 수 있는 클래스를 의미합니다.
    public ref class ImageEngine
    {
    public:
        // C#에서 호출할 그레이스케일 변환 함수
        // C#의 byte[]는 C++/CLI에서 array<unsigned char>^ 로 표현됩니다.
        bool ApplyGrayscale(array<unsigned char>^ pixelBuffer, int width, int height);

        // TODO: 여기에 Sobel, Gaussian, Binarization 등 C++로 구현할 함수들을 선언합니다.
        // 예: bool ApplySobel(array<unsigned char>^ pixelBuffer, int width, int height);
    };
}