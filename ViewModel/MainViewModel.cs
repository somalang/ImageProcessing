using ImageProcessing.ViewModels;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageProcessing.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private BitmapImage _loadedImage;
        private string _lastFilePath;

        public BitmapImage LoadedImage
        {
            get => _loadedImage;
            set
            {
                _loadedImage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasImage));
            }
        }

        public bool HasImage => LoadedImage != null;

        // 파일 관련 명령
        public RelayCommand LoadImageCommand { get; }
        public RelayCommand DeleteImageCommand { get; }
        public RelayCommand ReloadImageCommand { get; }
        public RelayCommand SaveImageCommand { get; }
        public RelayCommand ExitCommand { get; }

        // 편집 관련
        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }

        // 필터
        public RelayCommand GrayscaleCommand { get; }
        public RelayCommand GaussianBlurCommand { get; }
        public RelayCommand MedianFilterCommand { get; }

        // FFT
        public RelayCommand FFTCommand { get; }
        public RelayCommand IFFTCommand { get; }

        // 매칭
        public RelayCommand TemplateMatchCommand { get; }

        // 설정
        public RelayCommand OpenSettingsCommand { get; }

        public MainViewModel()
        {
            // 파일
            LoadImageCommand = new RelayCommand(_ => LoadImage());
            DeleteImageCommand = new RelayCommand(_ => DeleteImage(), _ => HasImage);
            ReloadImageCommand = new RelayCommand(_ => ReloadImage(), _ => !string.IsNullOrEmpty(_lastFilePath));
            SaveImageCommand = new RelayCommand(_ => SaveImage(), _ => HasImage);
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());

            // 편집 (Undo/Redo는 스택 구현 필요, 여기선 비워둠)
            UndoCommand = new RelayCommand(_ => MessageBox.Show("되돌리기 실행"));
            RedoCommand = new RelayCommand(_ => MessageBox.Show("다시하기 실행"));

            // 필터
            GrayscaleCommand = new RelayCommand(_ => MessageBox.Show("그레이스케일 실행"));
            GaussianBlurCommand = new RelayCommand(_ => MessageBox.Show("가우시안 블러 실행"));
            MedianFilterCommand = new RelayCommand(_ => MessageBox.Show("미디언 필터 실행"));

            // FFT
            FFTCommand = new RelayCommand(_ => MessageBox.Show("FFT 실행"));
            IFFTCommand = new RelayCommand(_ => MessageBox.Show("역 FFT 실행"));

            // 매칭
            TemplateMatchCommand = new RelayCommand(_ => MessageBox.Show("템플릿 매칭 실행"));

            // 설정
            OpenSettingsCommand = new RelayCommand(_ => MessageBox.Show("환경 설정 창 열기"));
        }

        private void LoadImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                _lastFilePath = dialog.FileName;
                LoadBitmapFromPath(_lastFilePath);
            }
        }

        private void ReloadImage()
        {
            if (!string.IsNullOrEmpty(_lastFilePath))
            {
                LoadBitmapFromPath(_lastFilePath);
            }
        }

        private void LoadBitmapFromPath(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            LoadedImage = bitmap;
        }

        private void SaveImage()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PNG|*.png|JPEG|*.jpg|Bitmap|*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show($"이미지를 {dialog.FileName} 으로 저장");
                // TODO: ImageProcessor.SaveBitmap(LoadedImage, dialog.FileName);
            }
        }

        private void DeleteImage()
        {
            LoadedImage = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
