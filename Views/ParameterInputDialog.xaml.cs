using System.Windows;

namespace ImageProcessing.Views
{
    public partial class ParameterInputDialog : Window
    {
        // 이 창을 통해 최종적으로 전달될 값
        public string UserInput { get; private set; }

        public ParameterInputDialog(string title, string prompt, string defaultValue)
        {
            InitializeComponent();
            Title = title;
            // DataContext를 자기 자신으로 설정하여 Title, Prompt 바인딩을 가능하게 함
            DataContext = this;
            InputTextBox.Text = defaultValue;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            UserInput = InputTextBox.Text;
            // DialogResult를 true로 설정하면, 이 창을 띄운 쪽에서 OK 버튼을 눌렀음을 알 수 있음
            DialogResult = true;
        }
    }
}