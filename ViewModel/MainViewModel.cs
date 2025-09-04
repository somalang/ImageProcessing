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
using System.Linq;

namespace ImageProcessing.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        #region Constants
        private const double MIN_ZOOM_LEVEL = 0.5;
        private const double MAX_ZOOM_LEVEL = 3.0;
        private const double ZOOM_STEP = 0.1;
        private const int MIN_SELECTION_SIZE = 5;
        #endregion

        #region Fields
        // 서비스 필드
        private readonly ImageProcessor imageProcessor;
        private readonly FileService fileService;
        private readonly SettingService settingService;
        private readonly LogService logService;
        private readonly ClipboardService clipboardService;

        // 이미지 관련 필드
        private BitmapImage currentBitmapImage;
        private BitmapImage originalImage;
        private BitmapImage loadedImage;

        // 선택 영역 및 좌표 관련 필드
        private Visibility selectionVisibility;
        private Rect selectionRect;
        private bool isSelecting;
        private Point startPoint;
        private string currentCoordinates;

        // UI 및 상태 관련 필드
        private Size imageControlSize;
        private double zoomLevel = 1.0;

        // 기타
        private string lastImagePath;
        private string processingTime;

        // 윈도우 인스턴스
        private OriginalImageView originalImageView;
        private LogWindow logWindow;
        #endregion

        #region Properties
        // 이미지 관련 속성
        public BitmapImage CurrentBitmapImage
        {
            get => currentBitmapImage;
            set
            {
                if (SetProperty(ref currentBitmapImage, value))
                {
                    OnPropertyChanged(nameof(CanUndo));
                    OnPropertyChanged(nameof(CanRedo));
                }
            }
        }

        public BitmapImage LoadedImage
        {
            get => loadedImage;
            set => SetProperty(ref loadedImage, value);
        }

        // 선택 영역 및 좌표 속성
        public Visibility SelectionVisibility
        {
            get => selectionVisibility;
            set => SetProperty(ref selectionVisibility, value);
        }

        public Rect SelectionRect
        {
            get => selectionRect;
            set => SetProperty(ref selectionRect, value);
        }

        public string CurrentCoordinates
        {
            get => currentCoordinates;
            set => SetProperty(ref currentCoordinates, value);
        }

        // UI 및 상태 속성
        public Size ImageControlSize
        {
            get => imageControlSize;
            set => SetProperty(ref imageControlSize, value);
        }

        public string ProcessingTime
        {
            get => processingTime;
            set => SetProperty(ref processingTime, value);
        }

        public double ZoomLevel
        {
            get => zoomLevel;
            set
            {
                if (SetProperty(ref zoomLevel, value))
                {
                    if (zoomLevel < MIN_ZOOM_LEVEL) zoomLevel = MIN_ZOOM_LEVEL;
                    if (zoomLevel > MAX_ZOOM_LEVEL) zoomLevel = MAX_ZOOM_LEVEL;
                    OnPropertyChanged(nameof(ZoomPercentage));
                }
            }
        }

        public string ZoomPercentage => $"{ZoomLevel * 100:0}%";

        // Undo/Redo 상태
        public bool CanUndo => imageProcessor.CanUndo;
        public bool CanRedo => imageProcessor.CanRedo;

        // Command 속성
        public ICommand LoadImageCommand { get; private set; }
        public ICommand SaveImageCommand { get; private set; }
        public ICommand ShowOriginalImageCommand { get; private set; }
        public ICommand DeleteImageCommand { get; private set; }
        public ICommand ReloadImageCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }
        public ICommand UndoCommand { get; private set; }
        public ICommand RedoCommand { get; private set; }
        public ICommand CutSelectionCommand { get; private set; }
        public ICommand CopySelectionCommand { get; private set; }
        public ICommand PasteCommand { get; private set; }
        public ICommand DeleteSelectionCommand { get; private set; }
        public ICommand ApplyGrayscaleCommand { get; private set; }
        public ICommand ApplyGaussianBlurCommand { get; private set; }
        public ICommand ApplyMedianFilterCommand { get; private set; }
        public ICommand ApplyLaplacianCommand { get; private set; }
        public ICommand ApplySobelCommand { get; private set; }
        public ICommand ApplyBinarizationCommand { get; private set; }
        public ICommand ApplyDilationCommand { get; private set; }
        public ICommand ApplyErosionCommand { get; private set; }
        public ICommand FFTCommand { get; private set; }
        public ICommand IFFTCommand { get; private set; }
        public ICommand TemplateMatchCommand { get; private set; }
        public ICommand OpenSettingsCommand { get; private set; }
        public ICommand ShowLogWindowCommand { get; private set; }
        public ICommand ZoomInCommand { get; private set; }
        public ICommand ZoomOutCommand { get; private set; }
        #endregion

        #region Constructor
        public MainViewModel()
        {
            imageProcessor = new ImageProcessor();
            fileService = new FileService();
            settingService = new SettingService();
            logService = new LogService();
            clipboardService = new ClipboardService();

            lastImagePath = settingService.GetLastImagePath();
            ProcessingTime = "Process Time: 0 ms";
            CurrentCoordinates = "좌표: X=0, Y=0";

            isSelecting = false;
            SelectionVisibility = Visibility.Collapsed;
            SelectionRect = new Rect(0, 0, 0, 0);

            InitializeCommands();
        }
        #endregion

        #region Public Methods
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
            isSelecting = true;
            var imagePoint = ToImageCoordinates(startPoint, ImageControlSize, CurrentBitmapImage);
            var uiPoint = ConvertImageRectToUiRect(new Rect(imagePoint, new Size(0, 0)), ImageControlSize, CurrentBitmapImage).TopLeft;
            this.startPoint = uiPoint;
            SelectionRect = new Rect(uiPoint, new Size(0, 0));
            SelectionVisibility = Visibility.Visible;
        }

        public void UpdateSelection(Point currentPoint)
        {
            if (isSelecting)
            {
                var imagePoint = ToImageCoordinates(currentPoint, ImageControlSize, CurrentBitmapImage);
                var uiPoint = ConvertImageRectToUiRect(new Rect(imagePoint, new Size(0, 0)), ImageControlSize, CurrentBitmapImage).TopLeft;
                var x = Math.Min(startPoint.X, uiPoint.X);
                var y = Math.Min(startPoint.Y, uiPoint.Y);
                var width = Math.Abs(startPoint.X - uiPoint.X);
                var height = Math.Abs(startPoint.Y - uiPoint.Y);
                SelectionRect = new Rect(x, y, width, height);
            }
        }

        public void EndSelection()
        {
            isSelecting = false;
            if (SelectionRect.Width < MIN_SELECTION_SIZE || SelectionRect.Height < MIN_SELECTION_SIZE)
            {
                ResetSelection();
            }
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            LoadImageCommand = new RelayCommand(async _ => await LoadImageAsync());
            SaveImageCommand = new RelayCommand(async _ => await SaveImageAsync(), _ => CurrentBitmapImage != null);

            ApplyGrayscaleCommand = new RelayCommand(_ => ApplyFilter(() => imageProcessor.ApplyGrayscale(CurrentBitmapImage), "Grayscale"));
            ApplySobelCommand = new RelayCommand(_ => ApplyFilter(() => imageProcessor.ApplySobel(CurrentBitmapImage), "Sobel"));
            ApplyLaplacianCommand = new RelayCommand(_ => ApplyFilter(() => imageProcessor.ApplyLaplacian(CurrentBitmapImage), "Laplacian"));
            ApplyGaussianBlurCommand = new RelayCommand(_ => ApplyFilter(() => imageProcessor.ApplyGaussianBlur(CurrentBitmapImage), "Gaussian Blur"));
            ApplyBinarizationCommand = new RelayCommand(_ => ExecuteWithParameter("Binarization", (processor, value) => processor.ApplyBinarization(CurrentBitmapImage, value), "128"));
            ApplyDilationCommand = new RelayCommand(_ => ExecuteWithParameter("Dilation", (processor, value) => processor.ApplyDilation(CurrentBitmapImage, value), "3"));
            ApplyErosionCommand = new RelayCommand(_ => ExecuteWithParameter("Erosion", (processor, value) => processor.ApplyErosion(CurrentBitmapImage, value), "3"));
            ApplyMedianFilterCommand = new RelayCommand(_ => ExecuteWithParameter("Median Filter", (processor, value) => processor.ApplyMedianFilter(CurrentBitmapImage, value), "3"));
            FFTCommand = new RelayCommand(_ => ApplyFilter(() => imageProcessor.ApplyFFT(CurrentBitmapImage), "FFT"), _ => CurrentBitmapImage != null);
            IFFTCommand = new RelayCommand(_ => ApplyIFFT(), _ => CurrentBitmapImage != null && imageProcessor.HasFFTData);

            UndoCommand = new RelayCommand(_ => ExecuteUndo(), _ => CanUndo);
            RedoCommand = new RelayCommand(_ => ExecuteRedo(), _ => CanRedo);
            ShowOriginalImageCommand = new RelayCommand(_ => ShowOriginalImage(), _ => originalImage != null);
            DeleteImageCommand = new RelayCommand(_ => DeleteImage(), _ => CurrentBitmapImage != null);
            ReloadImageCommand = new RelayCommand(async _ => await ReloadImageAsync(), _ => originalImage != null || !string.IsNullOrEmpty(lastImagePath));
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            ShowLogWindowCommand = new RelayCommand(_ => ShowLogWindow());

            CutSelectionCommand = new RelayCommand(_ => CutSelection(), _ => HasValidSelection());
            CopySelectionCommand = new RelayCommand(_ => CopySelection(), _ => HasValidSelection());
            DeleteSelectionCommand = new RelayCommand(_ => DeleteSelection(), _ => HasValidSelection());
            PasteCommand = new RelayCommand(_ => ExecutePaste(), _ => CurrentBitmapImage != null && clipboardService.GetImage() != null);
            TemplateMatchCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });
            OpenSettingsCommand = new RelayCommand(_ => { /* 기능 구현 필요 */ });

            ZoomInCommand = new RelayCommand(_ => ZoomLevel += ZOOM_STEP);
            ZoomOutCommand = new RelayCommand(_ => ZoomLevel -= ZOOM_STEP);
        }

        private bool HasValidSelection()
        {
            return CurrentBitmapImage != null &&
                   SelectionVisibility == Visibility.Visible &&
                   SelectionRect.Width > MIN_SELECTION_SIZE &&
                   SelectionRect.Height > MIN_SELECTION_SIZE;
        }

        private async Task LoadImageAsync()
        {
            var filePath = fileService.OpenImageFileDialog();
            if (!string.IsNullOrEmpty(filePath))
            {
                await LoadImageFromPathAsync(filePath);
            }
        }

        private async Task LoadImageFromPathAsync(string filePath)
        {
            LoadedImage = await fileService.LoadImage(filePath);
            originalImage = LoadedImage;
            CurrentBitmapImage = LoadedImage;
            lastImagePath = filePath;
            settingService.SaveLastImagePath(filePath);
        }

        private void ShowOriginalImage()
        {
            if (originalImageView == null)
            {
                originalImageView = new OriginalImageView(originalImage);
                originalImageView.Owner = Application.Current.MainWindow;
                originalImageView.Closed += (sender, eventArgs) => originalImageView = null;
                originalImageView.Show();
            }
            else
            {
                originalImageView.Activate();
            }
        }

        private void DeleteImage()
        {
            originalImageView?.Close();
            CurrentBitmapImage = null;
            LoadedImage = null;
            originalImage = null;
            ClearCoordinates();
            ResetSelection();
        }

        private async Task ReloadImageAsync()
        {
            if (!string.IsNullOrEmpty(lastImagePath) && File.Exists(lastImagePath))
            {
                await LoadImageFromPathAsync(lastImagePath);
            }
        }

        private async Task SaveImageAsync()
        {
            var filePath = fileService.SaveImageFileDialog();
            if (filePath != null && CurrentBitmapImage != null)
            {
                await fileService.SaveImage(CurrentBitmapImage, filePath);
            }
        }

        private void CutSelection()
        {
            if (!HasValidSelection()) return;

            var imageSelectionRect = ConvertUiRectToImageRect(SelectionRect, ImageControlSize, CurrentBitmapImage);
            if (imageSelectionRect.IsEmpty) return;

            var croppedImage = imageProcessor.Crop(CurrentBitmapImage, imageSelectionRect);
            if (croppedImage != null)
            {
                clipboardService.SetImage(croppedImage);

                var clearedImage = imageProcessor.ClearSelection(CurrentBitmapImage, imageSelectionRect);
                CurrentBitmapImage = BitmapSourceToBitmapImage(clearedImage);
                LoadedImage = CurrentBitmapImage;
                ResetSelection();
                logService.AddLog("Cut Selection", 0);
            }
        }

        private void CopySelection()
        {
            if (!HasValidSelection()) return;

            var imageSelectionRect = ConvertUiRectToImageRect(SelectionRect, ImageControlSize, CurrentBitmapImage);
            if (imageSelectionRect.IsEmpty) return;

            var croppedImage = imageProcessor.Crop(CurrentBitmapImage, imageSelectionRect);
            if (croppedImage != null)
            {
                clipboardService.SetImage(croppedImage);
                logService.AddLog("Copy Selection", 0);
            }
        }

        private void ExecutePaste()
        {
            var clipboardImage = clipboardService.GetImage();
            if (CurrentBitmapImage == null || clipboardImage == null) return;

            Point pasteLocation = HasValidSelection()
                ? ToImageCoordinates(SelectionRect.TopLeft, ImageControlSize, CurrentBitmapImage)
                : new Point(0, 0);

            var stopwatch = Stopwatch.StartNew();
            var pastedImageSource = imageProcessor.Paste(CurrentBitmapImage, clipboardImage, pasteLocation);
            stopwatch.Stop();

            CurrentBitmapImage = BitmapSourceToBitmapImage(pastedImageSource);
            LoadedImage = CurrentBitmapImage;

            var pastedImageRect = new Rect(pasteLocation.X, pasteLocation.Y, clipboardImage.PixelWidth, clipboardImage.PixelHeight);
            SelectionRect = ConvertImageRectToUiRect(pastedImageRect, ImageControlSize, CurrentBitmapImage);
            SelectionVisibility = Visibility.Visible;

            ProcessingTime = $"Process Time: {stopwatch.ElapsedMilliseconds} ms";
            logService.AddLog("Paste", stopwatch.ElapsedMilliseconds);
        }

        private void DeleteSelection()
        {
            if (!HasValidSelection()) return;

            var imageSelectionRect = ConvertUiRectToImageRect(SelectionRect, ImageControlSize, CurrentBitmapImage);
            if (imageSelectionRect.IsEmpty) return;

            var clearedImage = imageProcessor.ClearSelection(CurrentBitmapImage, imageSelectionRect);
            CurrentBitmapImage = BitmapSourceToBitmapImage(clearedImage);
            LoadedImage = CurrentBitmapImage;
            ResetSelection();
            logService.AddLog("Delete Selection", 0);
        }

        private Point ToImageCoordinates(Point controlPoint, Size controlSize, BitmapSource imageSource)
        {
            if (imageSource == null || controlSize.Width == 0 || controlSize.Height == 0)
                return new Point(0, 0);

            double controlWidth = controlSize.Width;
            double controlHeight = controlSize.Height;
            double imageWidth = imageSource.PixelWidth;
            double imageHeight = imageSource.PixelHeight;

            double baseScale = Math.Min(controlWidth / imageWidth, controlHeight / imageHeight);
            double effectiveScale = baseScale * ZoomLevel;
            if (effectiveScale == 0) return new Point(0, 0);

            double xOffset = (controlWidth - imageWidth * effectiveScale) / 2;
            double yOffset = (controlHeight - imageHeight * effectiveScale) / 2;

            double x = (controlPoint.X - xOffset) / effectiveScale;
            double y = (controlPoint.Y - yOffset) / effectiveScale;

            x = Math.Max(0, Math.Min(x, imageWidth));
            y = Math.Max(0, Math.Min(y, imageHeight));

            return new Point(x, y);
        }

        private Rect ConvertUiRectToImageRect(Rect uiRect, Size controlSize, BitmapSource imageSource)
        {
            if (imageSource == null) return Rect.Empty;

            Point topLeft = ToImageCoordinates(uiRect.TopLeft, controlSize, imageSource);
            Point bottomRight = ToImageCoordinates(uiRect.BottomRight, controlSize, imageSource);

            return new Rect(topLeft, bottomRight);
        }

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

        private void ResetSelection()
        {
            SelectionVisibility = Visibility.Collapsed;
            SelectionRect = new Rect(0, 0, 0, 0);
        }

        private void ExecuteWithParameter(string operationName, Func<ImageProcessor, int, BitmapImage> filterAction, string defaultValue = "3")
        {
            if (CurrentBitmapImage == null) return;

            var dialog = new ParameterInputDialog($"{operationName} Parameter", "값을 입력하세요:", defaultValue)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                if (int.TryParse(dialog.InputValue, out int parameter))
                {
                    ApplyFilter(() => filterAction(imageProcessor, parameter), operationName);
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
                ProcessingTime = $"Process Time: {stopwatch.ElapsedMilliseconds} ms";
                logService.AddLog(operationName, stopwatch.ElapsedMilliseconds);
            }
        }

        private void ApplyIFFT()
        {
            if (!imageProcessor.HasFFTData)
            {
                MessageBox.Show("FFT 데이터가 없습니다. 먼저 푸리에 변환을 수행해주세요.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ApplyFilter(() => imageProcessor.ApplyIFFT(CurrentBitmapImage), "IFFT");
            imageProcessor.ClearFFTData();
        }

        private void ExecuteUndo()
        {
            CurrentBitmapImage = imageProcessor.Undo();
            LoadedImage = CurrentBitmapImage;
        }

        private void ExecuteRedo()
        {
            CurrentBitmapImage = imageProcessor.Redo();
            LoadedImage = CurrentBitmapImage;
        }

        private void ShowLogWindow()
        {
            if (logWindow == null)
            {
                logWindow = new LogWindow(logService)
                {
                    Owner = Application.Current.MainWindow
                };
                logWindow.Closed += (sender, eventArgs) => logWindow = null;
                logWindow.Show();
            }
            else
            {
                logWindow.Activate();
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
        #endregion
    }
}