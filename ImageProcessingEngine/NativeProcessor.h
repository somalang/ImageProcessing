#pragma once

class NativeProcessor
{
public:
    // �ȼ� �����͸� �޾� �׷��̽����Ϸ� ��ȯ�ϴ� �Լ�
    void ToGrayscale(unsigned char* pixels, int width, int height);

    // --- ���� �߰��� �Լ� ���� ---

    // ����þ� �� (������ ����)
    void ApplyGaussianBlur(unsigned char* pixels, int width, int height);

    // �Һ� ���� ����
    void ApplySobel(unsigned char* pixels, int width, int height);

    // ���ö�þ� ���� ����
    void ApplyLaplacian(unsigned char* pixels, int width, int height);

    // ����ȭ (�Ӱ谪 ó��)
    void ApplyBinarization(unsigned char* pixels, int width, int height, int threshold);

    // ��â ���� (Morphology)
    void ApplyDilation(unsigned char* pixels, int width, int height, int kernelSize);

    // ħ�� ���� (Morphology)
    void ApplyErosion(unsigned char* pixels, int width, int height, int kernelSize);

    // �߾Ӱ� ����
    void ApplyMedianFilter(unsigned char* pixels, int width, int height, int kernelSize);

    void Binarize(unsigned char* pixels, int width, int height, int threshold);
    void Dilate(unsigned char* pixels, int width, int height, int kernelSize);
};