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
using ImageProcessing.Views;
using System.Windows.Media;

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
        private readonly ClipboardService _clipboardService;

        // --- 미리보기 관련 필드 ---
        //private BitmapImage _previewImage;
        //private bool _isPreviewing;
        //private Action _applyAction;


        // --- 로그 창 인스턴스 관리를 위한 필드 추가 ---
        private LogWindow _logWindow;

        // --- 선택 영역 관련 필드 추가 ---
        private Visibility _selectionVisibility;
        private Rect _selectionRect; // UI상의 선택 영역 (좌표, 크기)
        private bool _isSelecting;
        private Point _startPoint;

        // --- 속성 ---
        private Size _imageControlSize;
        public Size ImageControlSize
        {
            get => _imageControlSize;
            set => SetProperty(ref _imageControlSize, value);
        }

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

        // --- 미리보기 관련 속성 ---
        //public BitmapImage PreviewImage { get => _previewImage; set => SetProperty(ref _previewImage, value); }
        //public bool IsPreviewing { get => _isPreviewing; set => SetProperty(ref _isPreviewing, value); }

        // --- 줌 관련 속성 ---
        private double zoomLevel = 1.0;
        public double ZoomLevel
        {
            get => zoomLevel;
            set
            {
                if (SetProperty(ref zoomLevel, value))
                {
                    if (zoomLevel < 0.5) zoomLevel = 0.5;
                    if (zoomLevel > 3.0) zoomLevel = 3.0;
                    OnPropertyChanged(nameof(ZoomPercentage));
                }
            }
        }

        public string ZoomPercentage => $"{ZoomLevel * 100:0}%";

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
        public ICommand PasteCommand { get; }
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
        public ICommand ShowLogWindowCommand { get; }
        //public ICommand ApplyPreviewCommand { get; }
        //public ICommand CancelPreviewCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }


        public bool CanUndo => _imageProcessor.CanUndo;
        public bool CanRedo => _imageProcessor.CanRedo;

        public MainViewModel()
        {
            _imageProcessor = new ImageProcessor();
            _fileService = new FileService();
            _settingService = new SettingService();
            _logService = new LogService();
            _clipboardService = new ClipboardService();

            _lastImagePath = _settingService.GetLastImagePath();
            ProcessingTime = "Process Time: 0 ms";
            CurrentCoordinates = "좌표: X=0, Y=0";

            _isSelecting = false;
            SelectionVisibility = Visibility.Collapsed;
            SelectionRect = new Rect(0, 0, 0, 0);

            LoadImageCommand = new RelayCommand(async _ => await LoadImageAsync());
            SaveImageCommand = new RelayCommand(async _ => await SaveImageAsync(), _ => CurrentBitmapImage != null);

            // 필터 Command 초기화 (미리보기 방식 적용)
            ApplyGrayscaleCommand = new RelayCommand(_ => ApplyFilter(() => _imageProcessor.ApplyGrayscale(CurrentBitmapImage), "Grayscale"));
            ApplySobelCommand = new RelayCommand(_ => ApplyFilter(() => _imageProcessor.ApplySobel(CurrentBitmapImage), "Sobel"));
            ApplyLaplacianCommand = new RelayCommand(_ => ApplyFilter(() => _imageProcessor.ApplyLaplacian(CurrentBitmapImage), "Laplacian"));
            ApplyGaussianBlurCommand = new RelayCommand(_ => ApplyFilter(() => _imageProcessor.ApplyGaussianBlur(CurrentBitmapImage), "Gaussian Blur"));
            ApplyBinarizationCommand = new RelayCommand(_ => ExecuteWithParameter("Binarization", (p, value) => p.ApplyBinarization(CurrentBitmapImage, value), "128"));
            ApplyDilationCommand = new RelayCommand(_ => ExecuteWithParameter("Dilation", (p, value) => p.ApplyDilation(CurrentBitmapImage, value), "3"));
            ApplyErosionCommand = new RelayCommand(_ => ExecuteWithParameter("Erosion", (p, value) => p.ApplyErosion(CurrentBitmapImage, value), "3"));
            ApplyMedianFilterCommand = new RelayCommand(_ => ExecuteWithParameter("Median Filter", (p, value) => p.ApplyMedianFilter(CurrentBitmapImage, value), "3"));

            FFTCommand = new RelayCommand(_ => ApplyFilter(() => _imageProcessor.ApplyFFT(CurrentBitmapImage), "FFT"), _ => CurrentBitmapImage != null);
            IFFTCommand = new RelayCommand(_ => ApplyIFFT(), _ => CurrentBitmapImage != null && _imageProcessor.HasFFTData);

            UndoCommand = new RelayCommand(_ => ExecuteUndo(), _ => CanUndo);
            RedoCommand = new RelayCommand(_ => ExecuteRedo(), _ => CanRedo);

            ShowOriginalImageCommand = new RelayCommand(_ => ShowOriginalImage(), _ => _originalImage != null);
            DeleteImageCommand = new RelayCommand(_ => DeleteImage(), _ => CurrentBitmapImage != null);
            ReloadImageCommand = new RelayCommand(async _ => await ReloadImageAsync(), _ => _originalImage != null || !string.IsNullOrEmpty(_lastImagePath));
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());

            ShowLogWindowCommand = new RelayCommand(_ => ShowLogWindow());

            // --- 편집 Command 초기화 ---
            CutSelectionCommand = new RelayCommand(_ => CutSelection(), _ => HasValidSelection());
            CopySelectionCommand = new RelayCommand(_ => CopySelection(), _ => HasValidSelection());
            DeleteSelectionCommand = new RelayCommand(_ => DeleteSelection(), _ => HasValidSelection());
            PasteCommand = new RelayCommand(_ => ExecutePaste(), _ => CurrentBitmapImage != null && _clipboardService.GetImage() != null);

            TemplateMatchCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
            OpenSettingsCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });

            // --- 줌 Command 초기화 ---
            ZoomInCommand = new RelayCommand(_ => ZoomLevel += 0.1);
            ZoomOutCommand = new RelayCommand(_ => ZoomLevel -= 0.1);
        }
        
        
        private bool HasValidSelection()
        {
            return CurrentBitmapImage != null &&
                   SelectionVisibility == Visibility.Visible &&
                   SelectionRect.Width > 5 &&
                   SelectionRect.Height > 5;
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
            ResetSelection();
        }

        private async Task ReloadImageAsync()
        {
            if (!string.IsNullOrEmpty(_lastImagePath) && File.Exists(_lastImagePath))
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

        private void CutSelection()
        {
            if (!HasValidSelection()) return;

            var imageSelectionRect = ConvertUiRectToImageRect(SelectionRect, ImageControlSize, CurrentBitmapImage);
            if (imageSelectionRect.IsEmpty) return;

            // 1. 선택 영역을 클립보드에 복사
            var croppedImage = _imageProcessor.Crop(CurrentBitmapImage, imageSelectionRect);
            if (croppedImage != null)
            {
                _clipboardService.SetImage(croppedImage);

                // 2. 원본에서 선택 영역을 투명하게 처리
                var clearedImage = _imageProcessor.ClearSelection(CurrentBitmapImage, imageSelectionRect);
                CurrentBitmapImage = BitmapSourceToBitmapImage(clearedImage);
                LoadedImage = CurrentBitmapImage;

                ResetSelection();
                _logService.AddLog("Cut Selection", 0);
            }
        }

        private void CopySelection()
        {
            if (!HasValidSelection()) return;

            var imageSelectionRect = ConvertUiRectToImageRect(SelectionRect, ImageControlSize, CurrentBitmapImage);
            if (imageSelectionRect.IsEmpty) return;

            var croppedImage = _imageProcessor.Crop(CurrentBitmapImage, imageSelectionRect);
            if (croppedImage != null)
            {
                _clipboardService.SetImage(croppedImage);
                _logService.AddLog("Copy Selection", 0);
            }
        }

        private void ExecutePaste()
        {
            var clipboardImage = _clipboardService.GetImage();
            if (CurrentBitmapImage == null || clipboardImage == null) return;

            // 붙여넣을 위치(이미지 좌표계 기준) 결정
            Point pasteLocation;
            if (HasValidSelection())
            {
                // UI 선택 영역의 좌상단 지점을 이미지 좌표로 변환
                pasteLocation = ToImageCoordinates(SelectionRect.TopLeft, ImageControlSize, CurrentBitmapImage);
            }
            else
            {
                pasteLocation = new Point(0, 0); // 선택 영역 없으면 (0,0)
            }

            var stopwatch = Stopwatch.StartNew();
            var pastedImageSource = _imageProcessor.Paste(CurrentBitmapImage, clipboardImage, pasteLocation);
            stopwatch.Stop();

            CurrentBitmapImage = BitmapSourceToBitmapImage(pastedImageSource);
            LoadedImage = CurrentBitmapImage;

            // 붙여넣기 후, 붙여넣은 영역을 선택 상태로 만듬
            var pastedImageRect = new Rect(pasteLocation.X, pasteLocation.Y, clipboardImage.PixelWidth, clipboardImage.PixelHeight);
            SelectionRect = ConvertImageRectToUiRect(pastedImageRect, ImageControlSize, CurrentBitmapImage);
            SelectionVisibility = Visibility.Visible;

            long elapsedMs = stopwatch.ElapsedMilliseconds;
            ProcessingTime = $"Process Time: {elapsedMs} ms";
            _logService.AddLog("Paste", elapsedMs);
        }

        private void DeleteSelection()
        {
            if (!HasValidSelection()) return;

            var imageSelectionRect = ConvertUiRectToImageRect(SelectionRect, ImageControlSize, CurrentBitmapImage);
            if (imageSelectionRect.IsEmpty) return;

            var clearedImage = _imageProcessor.ClearSelection(CurrentBitmapImage, imageSelectionRect);
            CurrentBitmapImage = BitmapSourceToBitmapImage(clearedImage);
            LoadedImage = CurrentBitmapImage;

            ResetSelection();
            _logService.AddLog("Delete Selection", 0);
        }

        #region Coordinate Conversion Helpers

        /// <summary>
        /// UI 컨트롤의 좌표를 실제 이미지의 픽셀 좌표로 변환합니다.
        /// </summary>
        private double baseScale;

        private void InitializeBaseScale(Size controlSize, BitmapSource originalImage)
        {
            double controlWidth = controlSize.Width;
            double controlHeight = controlSize.Height;
            double imageWidth = originalImage.PixelWidth;
            double imageHeight = originalImage.PixelHeight;

            baseScale = Math.Min(controlWidth / imageWidth, controlHeight / imageHeight);
        }

        private Point ToImageCoordinates(Point controlPoint, Size controlSize, BitmapSource imageSource)
        {
            if (imageSource == null || controlSize.Width == 0 || controlSize.Height == 0)
                return new Point(0, 0);

            double controlWidth = controlSize.Width;
            double controlHeight = controlSize.Height;
            double imageWidth = imageSource.PixelWidth;
            double imageHeight = imageSource.PixelHeight;

            // baseScale은 고정, ZoomLevel만 반영
            double effectiveScale = baseScale * ZoomLevel;

            double xOffset = (controlWidth - imageWidth * effectiveScale) / 2;
            double yOffset = (controlHeight - imageHeight * effectiveScale) / 2;

            double x = (controlPoint.X - xOffset) / effectiveScale;
            double y = (controlPoint.Y - yOffset) / effectiveScale;

            x = Math.Max(0, Math.Min(x, imageWidth));
            y = Math.Max(0, Math.Min(y, imageHeight));

            return new Point(x, y);
        }


        /// <summary>
        /// UI 컨트롤의 선택 영역(Rect)을 실제 이미지의 픽셀 영역(Rect)으로 변환합니다.
        /// </summary>
        private Rect ConvertUiRectToImageRect(Rect uiRect, Size controlSize, BitmapSource imageSource)
        {
            if (imageSource == null) return Rect.Empty;

            Point topLeft = ToImageCoordinates(uiRect.TopLeft, controlSize, imageSource);
            Point bottomRight = ToImageCoordinates(uiRect.BottomRight, controlSize, imageSource);

            return new Rect(topLeft, bottomRight);
        }

        /// <summary>
        /// 실제 이미지의 픽셀 영역(Rect)을 UI 컨트롤의 영역(Rect)으로 변환합니다.
        /// </summary>
        private Rect ConvertImageRectToUiRect(Rect imageRect, Size controlSize, BitmapSource imageSource)
        {
            if (imageSource == null || controlSize.Width == 0 || controlSize.Height == 0)
                return Rect.Empty;

            double controlWidth = controlSize.Width;
            double controlHeight = controlSize.Height;
            double imageWidth = imageSource.PixelWidth;
            double imageHeight = imageSource.PixelHeight;

            double scale = Math.Min(controlWidth / imageWidth, controlHeight / imageHeight);
            if (scale == 0) return Rect.Empty;

            double xOffset = (controlWidth - imageWidth * scale) / 2;
            double yOffset = (controlHeight - imageHeight * scale) / 2;

            double uiX = imageRect.X * scale + xOffset;
            double uiY = imageRect.Y * scale + yOffset;
            double uiWidth = imageRect.Width * scale;
            double uiHeight = imageRect.Height * scale;

            return new Rect(uiX, uiY, uiWidth, uiHeight);
        }

        #endregion

        private void ResetSelection()
        {
            SelectionVisibility = Visibility.Collapsed;
            SelectionRect = new Rect(0, 0, 0, 0);
        }

        private void ExecuteWithParameter(string operationName, Func<ImageProcessor, int, BitmapImage> filterAction, string defaultValue = "3")
        {
            if (CurrentBitmapImage == null) return;

            var dialog = new ParameterInputDialog($"{operationName} Parameter", "값을 입력하세요:", defaultValue);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                if (int.TryParse(dialog.InputValue, out int param))
                {
                    ApplyFilter(() => filterAction(_imageProcessor, param), operationName);
                }
                else
                {
                    MessageBox.Show("숫자를 입력하세요.", "잘못된 입력", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ApplyFilter(Func<BitmapImage> filterAction, string operationName)
        {
            if (CurrentBitmapImage == null) return;

            var stopwatch = Stopwatch.StartNew();
            var newImage = filterAction();
            stopwatch.Stop();

            if (newImage != null)
            {
                CurrentBitmapImage = newImage;
                LoadedImage = CurrentBitmapImage;
                long elapsedMs = stopwatch.ElapsedMilliseconds;
                ProcessingTime = $"Process Time: {elapsedMs} ms";
                _logService.AddLog(operationName, elapsedMs);
            }
        }

        private void ApplyIFFT()
        {
            if (!_imageProcessor.HasFFTData)
            {
                MessageBox.Show("FFT 데이터가 없습니다. 먼저 푸리에 변환을 수행해주세요.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ApplyFilter(() => _imageProcessor.ApplyIFFT(CurrentBitmapImage), "IFFT");
            _imageProcessor.ClearFFTData();
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

        public void StartSelection(Point startPoint)
        {
            _isSelecting = true;

            // UI 좌표 → 이미지 좌표
            var imgPoint = ToImageCoordinates(startPoint, ImageControlSize, CurrentBitmapImage);

            // 이미지 좌표 → 다시 UI 좌표 (보정된 값)
            var uiPoint = ConvertImageRectToUiRect(new Rect(imgPoint, new Size(0, 0)), ImageControlSize, CurrentBitmapImage).TopLeft;

            _startPoint = uiPoint;
            SelectionRect = new Rect(uiPoint, new Size(0, 0));
            SelectionVisibility = Visibility.Visible;
        }

        public void UpdateSelection(Point currentPoint)
        {
            if (_isSelecting)
            {
                var imgPoint = ToImageCoordinates(currentPoint, ImageControlSize, CurrentBitmapImage);
                var uiPoint = ConvertImageRectToUiRect(new Rect(imgPoint, new Size(0, 0)), ImageControlSize, CurrentBitmapImage).TopLeft;

                var x = Math.Min(_startPoint.X, uiPoint.X);
                var y = Math.Min(_startPoint.Y, uiPoint.Y);
                var width = Math.Abs(_startPoint.X - uiPoint.X);
                var height = Math.Abs(_startPoint.Y - uiPoint.Y);

                SelectionRect = new Rect(x, y, width, height);
            }
        }


        public void EndSelection()
        {
            _isSelecting = false;
            if (SelectionRect.Width < 5 || SelectionRect.Height < 5)
            {
                ResetSelection();
            }
        }

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

        private BitmapImage BitmapSourceToBitmapImage(BitmapSource source)
        {
            if (source == null)
                return null;

            if (source is BitmapImage bitmapImage && bitmapImage.StreamSource == null)
            {
                return bitmapImage;
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                var result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }
    }
}