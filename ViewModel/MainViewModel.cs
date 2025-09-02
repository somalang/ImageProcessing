using ImageProcessing.Services;
using ImageProcessing.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageProcessing.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private BitmapImage _currentBitmapImage;
        private readonly ImageProcessor _imageProcessor;
        private readonly FileService _fileService;
        private readonly SettingService _settingService; // <-- 이 줄을 추가하세요.
        private string _lastImagePath; // <-- 이 줄을 추가하세요.

        private BitmapImage _originalImage; // <-- 이 줄을 추가하세요.
        private Views.OriginalImageView _originalImageView; // <-- 이 줄을 추가하세요.

        private BitmapImage _loadedImage;
        private string _currentCoordinates;
        private Visibility _selectionVisibility;
        private Rect _selectionRect;
        private bool _isSelecting;
        private Point _startPoint;

        public BitmapImage CurrentBitmapImage
        {
            get => _currentBitmapImage;
            set
            {
                if (_currentBitmapImage != value)
                {
                    _currentBitmapImage = value;
                    OnPropertyChanged(nameof(CurrentBitmapImage));
                    OnPropertyChanged(nameof(CanUndo));
                    OnPropertyChanged(nameof(CanRedo));
                }
            }
        }

        public BitmapImage LoadedImage
        {
            get => _loadedImage;
            set
            {
                _loadedImage = value;
                OnPropertyChanged(nameof(LoadedImage));
            }
        }

        public string CurrentCoordinates
        {
            get => _currentCoordinates;
            set
            {
                _currentCoordinates = value;
                OnPropertyChanged(nameof(CurrentCoordinates));
            }
        }

        public Visibility SelectionVisibility
        {
            get => _selectionVisibility;
            set
            {
                _selectionVisibility = value;
                OnPropertyChanged(nameof(SelectionVisibility));
            }
        }

        public Rect SelectionRect
        {
            get => _selectionRect;
            set
            {
                _selectionRect = value;
                OnPropertyChanged(nameof(SelectionRect));
            }
        }

        public ICommand LoadImageCommand { get; }
        public ICommand SaveImageCommand { get; }
        public ICommand ApplyGrayscaleCommand { get; }
        public ICommand ApplyGaussianBlurCommand { get; }
        public ICommand ApplySobelCommand { get; }
        public ICommand ApplyLaplacianCommand { get; }
        public ICommand ApplyBinarizationCommand { get; }
        public ICommand ApplyDilationCommand { get; }
        public ICommand ApplyErosionCommand { get; }

        public ICommand ApplyMedianFilterCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        public ICommand ShowOriginalImageCommand { get; }
        public ICommand DeleteImageCommand { get; }
        public ICommand ReloadImageCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand CutSelectionCommand { get; }
        public ICommand CopySelectionCommand { get; }
        public ICommand DeleteSelectionCommand { get; }
        public ICommand FFTCommand { get; }
        public ICommand IFFTCommand { get; }
        public ICommand TemplateMatchCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        public bool CanUndo => _imageProcessor.CanUndo;
        public bool CanRedo => _imageProcessor.CanRedo;

        public MainViewModel()
        {
            _imageProcessor = new ImageProcessor();
            _fileService = new FileService();
            _settingService = new SettingService(); // SettingService 인스턴스 생성

            _isSelecting = false;
            // 프로그램 시작 시 마지막 이미지 경로 불러오기
            
            _lastImagePath = _settingService.GetLastImagePath();

            SelectionVisibility = Visibility.Collapsed;
            // Rect.Empty 대신 명시적으로 0으로 초기화
            SelectionRect = new Rect(0, 0, 0, 0);
            CurrentCoordinates = "좌표: X=0, Y=0";

            LoadImageCommand = new RelayCommand(async _ => await LoadImageAsync());
            SaveImageCommand = new RelayCommand(async _ => await SaveImageAsync(), _ => CurrentBitmapImage != null);

            ApplyGrayscaleCommand = new RelayCommand(_ => ApplyFilter(filter => filter.ApplyGrayscale(CurrentBitmapImage)));
            ApplyGaussianBlurCommand = new RelayCommand(_ => ApplyFilter(filter => filter.ApplyGaussianBlur(CurrentBitmapImage)));
            ApplySobelCommand = new RelayCommand(_ => ApplyFilter(filter => filter.ApplySobel(CurrentBitmapImage)));
            ApplyLaplacianCommand = new RelayCommand(_ => ApplyFilter(filter => filter.ApplyLaplacian(CurrentBitmapImage)));
            ApplyBinarizationCommand = new RelayCommand(_ => ApplyFilter(filter => filter.ApplyBinarization(CurrentBitmapImage)));
            ApplyDilationCommand = new RelayCommand(_ => ApplyFilter(filter => filter.ApplyDilation(CurrentBitmapImage)));
            ApplyErosionCommand = new RelayCommand(_ => ApplyFilter(filter => filter.ApplyErosion(CurrentBitmapImage)));
            ApplyMedianFilterCommand = new RelayCommand(_ => ApplyFilter(filter => filter.ApplyMedianFilter(CurrentBitmapImage)));

            UndoCommand = new RelayCommand(_ => ExecuteUndo(), _ => CanUndo);
            RedoCommand = new RelayCommand(_ => ExecuteRedo(), _ => CanRedo);

            ShowOriginalImageCommand = new RelayCommand(_ =>
            {
                if (_originalImage != null)
                {
                    if (_originalImageView == null)
                    {
                        _originalImageView = new Views.OriginalImageView(_originalImage);
                        // 창이 닫힐 때 참조를 null로 설정합니다.
                        _originalImageView.Closed += (s, e) => _originalImageView = null;
                        _originalImageView.Show();
                    }
                    else
                    {
                        // 창이 이미 열려있다면 활성화합니다.
                        _originalImageView.Activate();
                    }
                }
            }, _ => _originalImage != null);

            DeleteImageCommand = new RelayCommand(_ => DeleteImage(), _ => CurrentBitmapImage != null);
            ReloadImageCommand = new RelayCommand(
                    async _ => await ReloadImageAsync(),
                    _ => _originalImage != null || !string.IsNullOrEmpty(_lastImagePath)
                );
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            CutSelectionCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
            CopySelectionCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
            DeleteSelectionCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
            FFTCommand = new RelayCommand(_ => ApplyFFT(), _ => CurrentBitmapImage != null);
            IFFTCommand = new RelayCommand(_ => ApplyIFFT(), _ => CurrentBitmapImage != null && _imageProcessor.HasFFTData);
            TemplateMatchCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
            OpenSettingsCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
        }

        // ViewModel/MainViewModel.cs

        private async Task LoadImageAsync()
        {
            var filePath = _fileService.OpenImageFileDialog();
            if (!string.IsNullOrEmpty(filePath))
            {
                await LoadImageFromPathAsync(filePath);
            }
        }

        // 아래 새 메서드를 추가합니다.
        private async Task LoadImageFromPathAsync(string filePath)
        {
            LoadedImage = await _fileService.LoadImage(filePath);
            _originalImage = LoadedImage;
            CurrentBitmapImage = LoadedImage;

            // 마지막 이미지 경로를 저장합니다.
            _lastImagePath = filePath;
            _settingService.SaveLastImagePath(filePath);
        }

        // ReloadImageAsync 메서드를 아래 코드로 교체합니다.
        private async Task ReloadImageAsync()
        {
            // 1. 현재 작업 중인 원본 이미지가 있으면 그걸로 되돌립니다.
            if (_originalImage != null)
            {
                CurrentBitmapImage = _originalImage;
                LoadedImage = _originalImage;
                //_imageProcessor.ClearHistory();
            }
            // 2. 작업 중인 이미지는 없지만 마지막 파일 경로가 있다면 해당 파일을 불러옵니다.
            else if (!string.IsNullOrEmpty(_lastImagePath) && File.Exists(_lastImagePath))
            {
                await LoadImageFromPathAsync(_lastImagePath);
            }
        }

        // DeleteImage 메서드를 아래 코드로 교체합니다.
        private void DeleteImage()
        {
            // 1. 열려있는 원본 이미지 창을 닫습니다.
            _originalImageView?.Close();

            // 2. 현재 작업 중인 이미지만 null로 초기화합니다.
            CurrentBitmapImage = null;
            LoadedImage = null;
            _originalImage = null;

            // 3. 실행 취소/다시 실행 내역을 초기화합니다.
            //_imageProcessor.ClearHistory();

            // 4. 좌표 정보를 초기화합니다.
            ClearCoordinates();

            // 마지막 이미지 경로를 지우는 코드를 제거하여, Reload 버튼이 활성화되도록 둡니다.
            // _lastImagePath = null;  <- 이 줄을 제거하거나 주석 처리
            // _settingService.SaveLastImagePath(null); <- 이 줄을 제거하거나 주석 처리
        }

        private async Task SaveImageAsync()
        {
            var filePath = _fileService.SaveImageFileDialog();
            if (filePath != null && CurrentBitmapImage != null)
            {
                await _fileService.SaveImage(CurrentBitmapImage, filePath);
            }
        }
        private void ApplyFilter(System.Func<ImageProcessor, BitmapImage> filterAction)
        {
            if (CurrentBitmapImage == null) return;
            CurrentBitmapImage = filterAction(_imageProcessor);
            LoadedImage = CurrentBitmapImage;
        }

        private void ExecuteUndo()
        {
            CurrentBitmapImage = _imageProcessor.Undo();
            LoadedImage = CurrentBitmapImage;
        }

        private void ExecuteRedo()
        {
            CurrentBitmapImage = _imageProcessor.Redo();
            LoadedImage = CurrentBitmapImage;
        }

        public void StartSelection(Point startPoint)
        {
            _isSelecting = true;
            _startPoint = startPoint;
            SelectionRect = new Rect(startPoint, new Size(0, 0));
            SelectionVisibility = Visibility.Visible;
        }

        public void UpdateSelection(Point currentPoint)
        {
            if (_isSelecting)
            {
                var x = Math.Min(_startPoint.X, currentPoint.X);
                var y = Math.Min(_startPoint.Y, currentPoint.Y);
                var width = Math.Abs(_startPoint.X - currentPoint.X);
                var height = Math.Abs(_startPoint.Y - currentPoint.Y);
                SelectionRect = new Rect(x, y, width, height);
            }
        }

        public void EndSelection()
        {
            _isSelecting = false;
        }

        public void UpdateCoordinates(Point point)
        {
            CurrentCoordinates = $"좌표: X={point.X:F0}, Y={point.Y:F0}";
        }

        public void ClearCoordinates()
        {
            CurrentCoordinates = "좌표: X=0, Y=0";
        }

        // FFT 메서드들 (기존 ApplyFilter 패턴 사용)
        private void ApplyFFT()
        {
            ApplyFilter(filter => filter.ApplyFFT(CurrentBitmapImage));
        }

        private void ApplyIFFT()
        {
            if (!_imageProcessor.HasFFTData)
            {
                System.Windows.MessageBox.Show("FFT 데이터가 없습니다. 먼저 푸리에 변환을 수행해주세요.", "경고");
                return;
            }
            ApplyFilter(filter => filter.ApplyIFFT(CurrentBitmapImage));
            _imageProcessor.ClearFFTData();
        }
    }
}