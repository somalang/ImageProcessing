using ImageProcessing.Models;

namespace ImageProcessing.Services
{
    public class SettingsService
    {
        public AppSettings LoadSettings()
        {
            // TODO: 파일에서 불러오기
            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            // TODO: 파일에 저장
        }
    }
}
