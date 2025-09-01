using ImageProcessingApp.Models;
using ImageProcessingApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ImageProcessingApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly FileService _fileService;
        private readonly ImageProcessor _imageProcessor;

        private ImageModel _currentImage;
        public ImageModel CurrentImage
        {
            get => _currentImage;
            set => SetProperty(ref _currentImage, value);
        }

        public ObservableCollection<ImageModel> ImageHistory { get; } = new();

        public ICommand OpenImageCommand { get; }
        public ICommand SaveImageCommand { get; }
        public ICommand ApplyFilterCommand { get; }

        public MainViewModel()
        {
            _fileService = new FileService();
            _imageProcessor = new ImageProcessor();

            OpenImageCommand = new RelayCommand(_ => OpenImage());
            SaveImageCommand = new RelayCommand(_ => SaveImage(), _ => CurrentImage != null);
            ApplyFilterCommand = new RelayCommand(_ => ApplyFilter(), _ => CurrentImage != null);
        }

        private void OpenImage()
        {
            var image = _fileService.OpenImage();
            if (image != null)
            {
                CurrentImage = image;
                ImageHistory.Add(image);
            }
        }

        private void SaveImage()
        {
            if (CurrentImage?.ProcessedImage != null)
                _fileService.SaveImage(CurrentImage.ProcessedImage);
        }

        private void ApplyFilter()
        {
            if (CurrentImage?.OriginalImage != null)
                CurrentImage.ProcessedImage = _imageProcessor.ApplyGaussianBlur(CurrentImage.OriginalImage, 1.5);
        }
    }
}
