#include "pch.h"
#include "ImageProcessingEngine.h"
#include "NativeProcessor.h" // NativeProcessor 클래스를 사용하기 위해 포함

// C++/CLI의 메모리 고정(pinning) 기능을 사용하기 위한 헤더
#include <vcclr.h>

bool ImageProcessingEngine::ImageEngine::ApplyGrayscale(array<unsigned char>^ pixelBuffer, int width, int height)
{
    // C#의 관리되는 배열(managed array)을 C++의 네이티브 포인터로 변환합니다.
    // pin_ptr은 GC(가비지 컬렉터)가 메모리를 이동시키지 못하도록 고정하는 역할을 합니다.
    pin_ptr<unsigned char> nativePixels = &pixelBuffer[0];

    // 순수 C++ 클래스의 인스턴스 생성
    NativeProcessor processor;

    // 네이티브 포인터를 넘겨주어 실제 영상 처리를 수행
    processor.ToGrayscale(nativePixels, width, height);

    return true;
}