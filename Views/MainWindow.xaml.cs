using ImageProcessing.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ImageProcessing.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is MainViewModel vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
            }
        }


        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var startPoint = e.GetPosition(sender as IInputElement);
                viewModel.StartSelection(startPoint);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var currentPoint = e.GetPosition(sender as IInputElement);
                viewModel.UpdateCoordinates(currentPoint);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    viewModel.UpdateSelection(currentPoint);
                }
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.EndSelection();
            }
        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ClearCoordinates();
            }
        }
        private void DisplayImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ImageControlSize = new Size(DisplayImage.ActualWidth, DisplayImage.ActualHeight);
            }
        }
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.ZoomLevel))
            {
                var vm = DataContext as MainViewModel;
                if (vm != null)
                {
                    ImageScaleTransform.ScaleX = vm.ZoomLevel;
                    ImageScaleTransform.ScaleY = vm.ZoomLevel;
                }
            }
        }

    }
}