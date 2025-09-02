using ImageProcessing.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace ImageProcessing.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // DataContext는 XAML에서 이미 설정됨
            // DataContext = new MainViewModel();
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.StartSelection(e.GetPosition(sender as IInputElement));
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var currentPoint = e.GetPosition(sender as IInputElement);
                viewModel.UpdateCoordinates(currentPoint); // 좌표 업데이트

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
                viewModel.ClearCoordinates(); // 좌표 초기화
            }
        }
    }
}
