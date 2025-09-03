using ImageProcessing.Services;
using ImageProcessing.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using ImageProcessing.Models;
using ImageProcessing.Views; // LogWindow 사용을 위해 추가

namespace ImageProcessing.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        // --- 기존 필드 ---
        private BitmapImage _currentBitmapImage;
        private readonly ImageProcessor _imageProcessor;
        private readonly FileService _fileService;
        private readonly SettingService _settingService;
        private string _lastImagePath;
        private readonly LogService _logService;
        private string _processingTime;
        private BitmapImage _originalImage;
        private Views.OriginalImageView _originalImageView;
        private BitmapImage _loadedImage;
        private string _currentCoordinates;

        // --- 로그 창 인스턴스 관리를 위한 필드 추가 ---
        private LogWindow _logWindow;

        // --- 선택 영역 관련 필드 추가 ---
        private Visibility _selectionVisibility;
        private Rect _selectionRect;
        private bool _isSelecting;
        private Point _startPoint;

        // --- 속성 ---
        public BitmapImage CurrentBitmapImage
        {
            get => _currentBitmapImage;
            set
            {
                if (SetProperty(ref _currentBitmapImage, value))
                {
                    OnPropertyChanged(nameof(CanUndo));
                    OnPropertyChanged(nameof(CanRedo));
                }
            }
        }
        public BitmapImage LoadedImage { get => _loadedImage; set => SetProperty(ref _loadedImage, value); }
        public string CurrentCoordinates { get => _currentCoordinates; set => SetProperty(ref _currentCoordinates, value); }
        public string ProcessingTime { get => _processingTime; set => SetProperty(ref _processingTime, value); }

        // --- 선택 영역 관련 속성 추가 ---
        public Visibility SelectionVisibility
        {
            get => _selectionVisibility;
            set => SetProperty(ref _selectionVisibility, value);
        }
        public Rect SelectionRect
        {
            get => _selectionRect;
            set => SetProperty(ref _selectionRect, value);
        }

        // --- Command 속성 ---
        public ICommand LoadImageCommand { get; }
        public ICommand SaveImageCommand { get; }
        public ICommand ShowOriginalImageCommand { get; }
        public ICommand DeleteImageCommand { get; }
        public ICommand ReloadImageCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand CutSelectionCommand { get; }
        public ICommand CopySelectionCommand { get; }
        public ICommand DeleteSelectionCommand { get; }
        public ICommand ApplyGrayscaleCommand { get; }
        public ICommand ApplyGaussianBlurCommand { get; }
        public ICommand ApplyMedianFilterCommand { get; }
        public ICommand ApplyLaplacianCommand { get; }
        public ICommand ApplySobelCommand { get; }
        public ICommand ApplyBinarizationCommand { get; }
        public ICommand ApplyDilationCommand { get; }
        public ICommand ApplyErosionCommand { get; }
        public ICommand FFTCommand { get; }
        public ICommand IFFTCommand { get; }
        public ICommand TemplateMatchCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        // --- ShowLogWindowCommand 속성 추가 ---
        public ICommand ShowLogWindowCommand { get; }

        public bool CanUndo => _imageProcessor.CanUndo;
        public bool CanRedo => _imageProcessor.CanRedo;

        public MainViewModel()
        {
            _imageProcessor = new ImageProcessor();
            _fileService = new FileService();
            _settingService = new SettingService();
            _logService = new LogService();

            _lastImagePath = _settingService.GetLastImagePath();
            ProcessingTime = "Process Time: 0 ms";
            CurrentCoordinates = "좌표: X=0, Y=0";

            // --- 선택 영역 관련 초기화 추가 ---
            _isSelecting = false;
            SelectionVisibility = Visibility.Collapsed;
            SelectionRect = new Rect(0, 0, 0, 0);

            LoadImageCommand = new RelayCommand(async _ => await LoadImageAsync());
            SaveImageCommand = new RelayCommand(async _ => await SaveImageAsync(), _ => CurrentBitmapImage != null);

            ApplyGrayscaleCommand = new RelayCommand(_ => ApplyFilter(p => p.ApplyGrayscale(CurrentBitmapImage), "Grayscale"));
            ApplyGaussianBlurCommand = new RelayCommand(_ => ApplyFilter(p => p.ApplyGaussianBlur(CurrentBitmapImage), "Gaussian Blur"));
            ApplySobelCommand = new RelayCommand(_ => ApplyFilter(p => p.ApplySobel(CurrentBitmapImage), "Sobel"));
            ApplyLaplacianCommand = new RelayCommand(_ => ApplyFilter(p => p.ApplyLaplacian(CurrentBitmapImage), "Laplacian"));
            ApplyBinarizationCommand = new RelayCommand(_ => ApplyFilter(p => p.ApplyBinarization(CurrentBitmapImage), "Binarization"));
            ApplyDilationCommand = new RelayCommand(_ => ApplyFilter(p => p.ApplyDilation(CurrentBitmapImage), "Dilation"));
            ApplyErosionCommand = new RelayCommand(_ => ApplyFilter(p => p.ApplyErosion(CurrentBitmapImage), "Erosion"));
            ApplyMedianFilterCommand = new RelayCommand(_ => ApplyFilter(p => p.ApplyMedianFilter(CurrentBitmapImage), "Median Filter"));

            FFTCommand = new RelayCommand(_ => ApplyFilter(p => p.ApplyFFT(CurrentBitmapImage), "FFT"), _ => CurrentBitmapImage != null);
            IFFTCommand = new RelayCommand(_ => ApplyIFFT(), _ => CurrentBitmapImage != null && _imageProcessor.HasFFTData);

            UndoCommand = new RelayCommand(_ => ExecuteUndo(), _ => CanUndo);
            RedoCommand = new RelayCommand(_ => ExecuteRedo(), _ => CanRedo);

            ShowOriginalImageCommand = new RelayCommand(_ => ShowOriginalImage(), _ => _originalImage != null);
            DeleteImageCommand = new RelayCommand(_ => DeleteImage(), _ => CurrentBitmapImage != null);
            ReloadImageCommand = new RelayCommand(async _ => await ReloadImageAsync(), _ => _originalImage != null || !string.IsNullOrEmpty(_lastImagePath));
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());

            // --- ShowLogWindowCommand 초기화 추가 ---
            ShowLogWindowCommand = new RelayCommand(_ => ShowLogWindow());

            CutSelectionCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
            CopySelectionCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
            DeleteSelectionCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
            TemplateMatchCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
            OpenSettingsCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
        }

        private async Task LoadImageAsync()
        {
            var filePath = _fileService.OpenImageFileDialog();
            if (!string.IsNullOrEmpty(filePath))
            {
                await LoadImageFromPathAsync(filePath);
            }
        }

        private async Task LoadImageFromPathAsync(string filePath)
        {
            LoadedImage = await _fileService.LoadImage(filePath);
            _originalImage = LoadedImage;
            CurrentBitmapImage = LoadedImage;
            _lastImagePath = filePath;
            _settingService.SaveLastImagePath(filePath);
        }

        private void ShowOriginalImage()
        {
            if (_originalImageView == null)
            {
                _originalImageView = new Views.OriginalImageView(_originalImage);
                _originalImageView.Owner = Application.Current.MainWindow;
                _originalImageView.Closed += (s, e) => _originalImageView = null;
                _originalImageView.Show();
            }
            else
            {
                _originalImageView.Activate();
            }
        }

        private void DeleteImage()
        {
            _originalImageView?.Close();
            CurrentBitmapImage = null;
            LoadedImage = null;
            _originalImage = null;
            ClearCoordinates();
        }

        private async Task ReloadImageAsync()
        {
            if (_originalImage != null)
            {
                CurrentBitmapImage = _originalImage;
                LoadedImage = _originalImage;
            }
            else if (!string.IsNullOrEmpty(_lastImagePath) && File.Exists(_lastImagePath))
            {
                await LoadImageFromPathAsync(_lastImagePath);
            }
        }

        private async Task SaveImageAsync()
        {
            var filePath = _fileService.SaveImageFileDialog();
            if (filePath != null && CurrentBitmapImage != null)
            {
                await _fileService.SaveImage(CurrentBitmapImage, filePath);
            }
        }

        private void ApplyFilter(Func<ImageProcessor, BitmapImage> filterAction, string operationName)
        {
            if (CurrentBitmapImage == null) return;

            var stopwatch = Stopwatch.StartNew();
            CurrentBitmapImage = filterAction(_imageProcessor);
            stopwatch.Stop();

            LoadedImage = CurrentBitmapImage;

            long elapsedMs = stopwatch.ElapsedMilliseconds;
            ProcessingTime = $"Process Time: {elapsedMs} ms";
            _logService.AddLog(operationName, elapsedMs);
        }

        private void ApplyIFFT()
        {
            if (!_imageProcessor.HasFFTData)
            {
                MessageBox.Show("FFT 데이터가 없습니다. 먼저 푸리에 변환을 수행해주세요.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var stopwatch = Stopwatch.StartNew();
            CurrentBitmapImage = _imageProcessor.ApplyIFFT(CurrentBitmapImage);
            stopwatch.Stop();

            _imageProcessor.ClearFFTData();
            LoadedImage = CurrentBitmapImage;

            long elapsedMs = stopwatch.ElapsedMilliseconds;
            ProcessingTime = $"Process Time: {elapsedMs} ms";
            _logService.AddLog("IFFT", elapsedMs);
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

        public void UpdateCoordinates(Point point)
        {
            CurrentCoordinates = $"좌표: X={point.X:F0}, Y={point.Y:F0}";
        }

        public void ClearCoordinates()
        {
            CurrentCoordinates = "좌표: X=0, Y=0";
        }

        // --- 선택 영역 관련 메서드 추가 ---
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

        // --- ShowLogWindow 메서드 추가 ---
        private void ShowLogWindow()
        {
            if (_logWindow == null)
            {
                _logWindow = new LogWindow(_logService);
                _logWindow.Owner = Application.Current.MainWindow;
                _logWindow.Closed += (s, e) => _logWindow = null;
                _logWindow.Show();
            }
            else
            {
                _logWindow.Activate();
            }
        }
    }
}

