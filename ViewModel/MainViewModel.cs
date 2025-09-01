using ImageProcessing.Services;
using ImageProcessing.Views;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows; // Rect, Point 사용
using System.Windows.Controls; // Image 컨트롤 사용

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
        // --- (새로 추가) 선택 영역 관련 속성 ---
        private Rect _selectionRect;
        public Rect SelectionRect
        {
            get => _selectionRect;
            set { _selectionRect = value; OnPropertyChanged(); }
        }

        private Visibility _selectionVisibility = Visibility.Collapsed;
        public Visibility SelectionVisibility
        {
            get => _selectionVisibility;
            set { _selectionVisibility = value; OnPropertyChanged(); }
        }

        private Point _startPoint;
        private bool _isSelecting = false;


        // --- (새로 추가) 편집 관련 명령 ---
        public RelayCommand CutSelectionCommand { get; }
        public RelayCommand CopySelectionCommand { get; }
        public RelayCommand DeleteSelectionCommand { get; }

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
            CutSelectionCommand = new RelayCommand(_ => CutSelection(), _ => HasSelection());
            CopySelectionCommand = new RelayCommand(_ => CopySelection(), _ => HasSelection());
            DeleteSelectionCommand = new RelayCommand(_ => DeleteSelection(), _ => HasSelection());
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
        // --- (새로 추가) 마우스 이벤트 처리 메소드 ---
        public void StartSelection(Point startPoint)
        {
            if (!HasImage) return;
            _isSelecting = true;
            _startPoint = startPoint;
            SelectionRect = new Rect(startPoint, startPoint);
            SelectionVisibility = Visibility.Visible;
        }

        public void UpdateSelection(Point currentPoint)
        {
            if (!_isSelecting) return;

            var x = Math.Min(_startPoint.X, currentPoint.X);
            var y = Math.Min(_startPoint.Y, currentPoint.Y);
            var width = Math.Abs(_startPoint.X - currentPoint.X);
            var height = Math.Abs(_startPoint.Y - currentPoint.Y);
            SelectionRect = new Rect(x, y, width, height);
        }

        public void EndSelection()
        {
            _isSelecting = false;
            // 선택 영역이 너무 작으면 선택 취소
            if (SelectionRect.Width < 2 || SelectionRect.Height < 2)
            {
                SelectionVisibility = Visibility.Collapsed;
            }
        }

        // --- (새로 추가) 편집 명령 실행 메소드 ---
        private bool HasSelection() => HasImage && SelectionVisibility == Visibility.Visible;

        private void CutSelection()
        {
            MessageBox.Show($"영역 {SelectionRect} 오려두기 실행 (구현 필요)");
            // TODO: C++ 엔진에 선택 영역 좌표를 넘겨 픽셀 처리
        }
        private void CopySelection()
        {
            MessageBox.Show($"영역 {SelectionRect} 복제 실행 (구현 필요)");
            // TODO: C++ 엔진에 선택 영역 좌표를 넘겨 픽셀 처리
        }
        private void DeleteSelection()
        {
            MessageBox.Show($"영역 {SelectionRect} 삭제 실행 (구현 필요)");
            // TODO: C++ 엔진에 선택 영역 좌표를 넘겨 픽셀 처리
            SelectionVisibility = Visibility.Collapsed; // 삭제 후 선택 영역 숨기기
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}