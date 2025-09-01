#include "pch.h"
#include "ImageProcessingEngine.h"
#include "NativeProcessor.h" // NativeProcessor Ŭ������ ����ϱ� ���� ����

// C++/CLI�� �޸� ����(pinning) ����� ����ϱ� ���� ���
#include <vcclr.h>

bool ImageProcessingEngine::ImageEngine::ApplyGrayscale(array<unsigned char>^ pixelBuffer, int width, int height)
{
    // C#�� �����Ǵ� �迭(managed array)�� C++�� ����Ƽ�� �����ͷ� ��ȯ�մϴ�.
    // pin_ptr�� GC(������ �÷���)�� �޸𸮸� �̵���Ű�� ���ϵ��� �����ϴ� ������ �մϴ�.
    pin_ptr<unsigned char> nativePixels = &pixelBuffer[0];

    // ���� C++ Ŭ������ �ν��Ͻ� ����
    NativeProcessor processor;

    // ����Ƽ�� �����͸� �Ѱ��־� ���� ���� ó���� ����
    processor.ToGrayscale(nativePixels, width, height);

    return true;
}