#pragma once

class NativeProcessor
{
public:
    // ���� �ȼ� �����͸� �޾� �׷��̽����Ϸ� ��ȯ�ϴ� �Լ�
    void ToGrayscale(unsigned char* pixels, int width, int height);

    // TODO: ���⿡ �ٸ� ����ó�� �Լ����� �����մϴ�.
    // ��: void Sobel(unsigned char* pixels, int width, int height);
};