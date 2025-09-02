using ImageProcessing.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace ImageProcessing.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
    }
}