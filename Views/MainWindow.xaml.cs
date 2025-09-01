using ImageProcessing.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace ImageProcessing.Views   // ← XAML의 x:Class 네임스페이스와 동일해야 함
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();  // ← XAML이 연결되면 정상 인식됨
            DataContext = new MainViewModel();
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
            if (DataContext is MainViewModel viewModel && e.LeftButton == MouseButtonState.Pressed)
            {
                viewModel.UpdateSelection(e.GetPosition(sender as IInputElement));
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.EndSelection();
            }
        }
    }
}
