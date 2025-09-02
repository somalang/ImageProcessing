using ImageProcessing.Services;
using ImageProcessing.Views;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls; // For Image control if needed, though not directly used in VM

namespace ImageProcessing.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // --- Fields ---
        private readonly FileService _fileService = new FileService();
        private readonly ImageProcessor _imageProcessor = new ImageProcessor();

        private OriginalImageView _originalImageViewer;
        private BitmapImage _originalImage;
        private BitmapImage _loadedImage;
        private string _lastFilePath;
        private Rect _selectionRect;
        private Visibility _selectionVisibility = Visibility.Collapsed;
        private Point _startPoint;
        private bool _isSelecting = false;
        private string _currentCoordinates; // 좌표 표시를 위한 필드 추가

        // --- Properties ---
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

        public Rect SelectionRect
        {
            get => _selectionRect;
            set { _selectionRect = value; OnPropertyChanged(); }
        }

        public Visibility SelectionVisibility
        {
            get => _selectionVisibility;
            set { _selectionVisibility = value; OnPropertyChanged(); }
        }
        public string CurrentCoordinates
        {
            get => _currentCoordinates;
            set
            {
                _currentCoordinates = value;
                OnPropertyChanged();
            }
        }
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
        public RelayCommand SobelCommand { get; }
        public RelayCommand LaplacianCommand { get; }
        public RelayCommand BinarizationCommand { get; }
        public RelayCommand DilationCommand { get; }
        public RelayCommand ErosionCommand { get; }
        public RelayCommand MedianFilterCommand { get; }
        public RelayCommand FFTCommand { get; }
        public RelayCommand IFFTCommand { get; }
        public RelayCommand TemplateMatchCommand { get; }
        public RelayCommand OpenSettingsCommand { get; }
        public RelayCommand CutSelectionCommand { get; }
        public RelayCommand CopySelectionCommand { get; }
        public RelayCommand DeleteSelectionCommand { get; }

        // --- Constructor ---
        public MainViewModel()
        {
            // File Commands
            LoadImageCommand = new RelayCommand(_ => LoadImage());
            ShowOriginalImageCommand = new RelayCommand(_ => ShowOriginalImage(), _ => _originalImage != null);
            DeleteImageCommand = new RelayCommand(_ => DeleteImage(), _ => HasImage);
            ReloadImageCommand = new RelayCommand(_ => ReloadImage(), _ => !string.IsNullOrEmpty(_lastFilePath));
            SaveImageCommand = new RelayCommand(_ => SaveImage(), _ => HasImage);
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());

            // Edit Commands
            UndoCommand = new RelayCommand(_ => MessageBox.Show("되돌리기 실행 (구현 필요)"));
            RedoCommand = new RelayCommand(_ => MessageBox.Show("다시하기 실행 (구현 필요)"));
            CutSelectionCommand = new RelayCommand(_ => CutSelection(), _ => HasSelection());
            CopySelectionCommand = new RelayCommand(_ => CopySelection(), _ => HasSelection());
            DeleteSelectionCommand = new RelayCommand(_ => DeleteSelection(), _ => HasSelection());

            // Filter Commands
            GrayscaleCommand = new RelayCommand(_ => ApplyFilter(img => _imageProcessor.ApplyGrayscale(img)), _ => HasImage);
            GaussianBlurCommand = new RelayCommand(_ => ApplyFilter(img => _imageProcessor.ApplyGaussianBlur(img)), _ => HasImage);
            SobelCommand = new RelayCommand(_ => ApplyFilter(img => _imageProcessor.ApplySobel(img)), _ => HasImage);
            LaplacianCommand = new RelayCommand(_ => ApplyFilter(img => _imageProcessor.ApplyLaplacian(img)), _ => HasImage);
            BinarizationCommand = new RelayCommand(_ => ApplyBinarization(), _ => HasImage);
            DilationCommand = new RelayCommand(_ => ApplyDilation(), _ => HasImage);
            ErosionCommand = new RelayCommand(_ => ApplyErosion(), _ => HasImage);

            // Placeholder Commands
            MedianFilterCommand = new RelayCommand(_ => MessageBox.Show("미디언 필터 실행 (구현 필요)"));
            FFTCommand = new RelayCommand(_ => MessageBox.Show("FFT 실행 (구현 필요)"));
            IFFTCommand = new RelayCommand(_ => MessageBox.Show("역 FFT 실행 (구현 필요)"));
            TemplateMatchCommand = new RelayCommand(_ => MessageBox.Show("템플릿 매칭 실행 (구현 필요)"));
            OpenSettingsCommand = new RelayCommand(_ => MessageBox.Show("환경 설정 창 열기 (구현 필요)"));
        }

        // --- Public Methods ---
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
            if (SelectionRect.Width < 2 || SelectionRect.Height < 2)
            {
                SelectionVisibility = Visibility.Collapsed;
            }
        }
        // 좌표 업데이트 메서드 추가
        public void UpdateCoordinates(Point currentPoint)
        {
            CurrentCoordinates = $"X: {currentPoint.X:F0}, Y: {currentPoint.Y:F0}";
        }

        // 좌표 초기화 메서드 추가
        public void ClearCoordinates()
        {
            CurrentCoordinates = string.Empty;
        }
        // --- Private Methods ---
        private void ApplyFilter(Func<BitmapImage, BitmapImage> filter)
        {
            if (LoadedImage == null) return;
            LoadedImage = filter(LoadedImage);
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
            bitmap.Freeze(); // UI Thread Safety
            _originalImage = bitmap;
            LoadedImage = bitmap.Clone();
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
        private void ApplyBinarization()
        {
            var dialog = new ParameterInputDialog("이진화", "임계값을 입력하세요 (0-255):", "128");

            // ShowDialog()가 true를 반환하면 '확인' 버튼을 누른 것
            if (dialog.ShowDialog() == true)
            {
                // 사용자가 입력한 값을 정수로 변환 (실패 시 기본값 사용)
                if (int.TryParse(dialog.UserInput, out int threshold))
                {
                    // TODO: ImageProcessor와 C++ 엔진에 threshold 값을 전달
                    MessageBox.Show($"이진화 실행! 임계값: {threshold}");
                    // 예시: LoadedImage = _imageProcessor.ApplyBinarization(LoadedImage, threshold);
                }
                else
                {
                    MessageBox.Show("올바른 숫자를 입력해주세요.");
                }
            }
        }

        // 팽창 적용 메소드
        private void ApplyDilation()
        {
            var dialog = new ParameterInputDialog("팽창", "커널 크기를 입력하세요 (홀수):", "3");
            if (dialog.ShowDialog() == true)
            {
                if (int.TryParse(dialog.UserInput, out int kernelSize))
                {
                    MessageBox.Show($"팽창 실행! 커널 크기: {kernelSize}x{kernelSize}");
                    // 예시: LoadedImage = _imageProcessor.ApplyDilation(LoadedImage, kernelSize);
                }
                else
                {
                    MessageBox.Show("올바른 숫자를 입력해주세요.");
                }
            }
        }

        // 침식 적용 메소드 (팽창과 거의 동일)
        private void ApplyErosion()
        {
            var dialog = new ParameterInputDialog("침식", "커널 크기를 입력하세요 (홀수):", "3");
            if (dialog.ShowDialog() == true)
            {
                if (int.TryParse(dialog.UserInput, out int kernelSize))
                {
                    MessageBox.Show($"침식 실행! 커널 크기: {kernelSize}x{kernelSize}");
                    // 예시: LoadedImage = _imageProcessor.ApplyErosion(LoadedImage, kernelSize);
                }
                else
                {
                    MessageBox.Show("올바른 숫자를 입력해주세요.");
                }
            }
        }
        private bool HasSelection() => HasImage && SelectionVisibility == Visibility.Visible;

        private void CutSelection()
        {
            MessageBox.Show($"영역 {SelectionRect} 오려두기 실행 (구현 필요)");
            // TODO: Implement C++ engine call for cutting selection
        }

        private void CopySelection()
        {
            MessageBox.Show($"영역 {SelectionRect} 복제 실행 (구현 필요)");
            // TODO: Implement C++ engine call for copying selection
        }

        private void DeleteSelection()
        {
            MessageBox.Show($"영역 {SelectionRect} 삭제 실행 (구현 필요)");
            // TODO: Implement C++ engine call for deleting selection
            SelectionVisibility = Visibility.Collapsed;
        }

        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}