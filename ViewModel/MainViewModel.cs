using ImageProcessing.Services;
using ImageProcessing.Views;
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
        // "원본 보기" 창 참조
        private OriginalImageView _originalImageViewer;
        // 파일 서비스
        private readonly FileService _fileService = new FileService();

        private BitmapImage _originalImage;
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

        // --- Commands ---
        public RelayCommand LoadImageCommand { get; }
        public RelayCommand ShowOriginalImageCommand { get; }
        public RelayCommand DeleteImageCommand { get; }
        public RelayCommand ReloadImageCommand { get; }
        public RelayCommand SaveImageCommand { get; }
        public RelayCommand ExitCommand { get; }
        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }
        public RelayCommand GrayscaleCommand { get; }
        public RelayCommand GaussianBlurCommand { get; }
        public RelayCommand MedianFilterCommand { get; }
        public RelayCommand FFTCommand { get; }
        public RelayCommand IFFTCommand { get; }
        public RelayCommand TemplateMatchCommand { get; }
        public RelayCommand OpenSettingsCommand { get; }

        public MainViewModel()
        {
            LoadImageCommand = new RelayCommand(_ => LoadImage());
            ShowOriginalImageCommand = new RelayCommand(_ => ShowOriginalImage(), _ => _originalImage != null);
            DeleteImageCommand = new RelayCommand(_ => DeleteImage(), _ => HasImage);
            ReloadImageCommand = new RelayCommand(_ => ReloadImage(), _ => !string.IsNullOrEmpty(_lastFilePath));
            SaveImageCommand = new RelayCommand(_ => SaveImage(), _ => HasImage);
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            UndoCommand = new RelayCommand(_ => MessageBox.Show("되돌리기 실행"));
            RedoCommand = new RelayCommand(_ => MessageBox.Show("다시하기 실행"));
            GrayscaleCommand = new RelayCommand(_ => MessageBox.Show("그레이스케일 실행"));
            GaussianBlurCommand = new RelayCommand(_ => MessageBox.Show("가우시안 블러 실행"));
            MedianFilterCommand = new RelayCommand(_ => MessageBox.Show("미디언 필터 실행"));
            FFTCommand = new RelayCommand(_ => MessageBox.Show("FFT 실행"));
            IFFTCommand = new RelayCommand(_ => MessageBox.Show("역 FFT 실행"));
            TemplateMatchCommand = new RelayCommand(_ => MessageBox.Show("템플릿 매칭 실행"));
            OpenSettingsCommand = new RelayCommand(_ => MessageBox.Show("환경 설정 창 열기"));
        }

        private void LoadImage()
        {
            var dialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp" };
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
            _originalImage = bitmap;
            LoadedImage = bitmap;
        }

        private void SaveImage()
        {
            if (LoadedImage != null)
            {
                _fileService.SaveImage(LoadedImage);
            }
        }

        private void DeleteImage()
        {
            LoadedImage = null;
            _originalImage = null;
        }

        private void ShowOriginalImage()
        {
            if (_originalImageViewer == null)
            {
                _originalImageViewer = new OriginalImageView(_originalImage);
                _originalImageViewer.Closed += (s, e) => _originalImageViewer = null;
                _originalImageViewer.Show();
            }
            else
            {
                _originalImageViewer.Activate();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}