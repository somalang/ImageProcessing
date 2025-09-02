// Services/SettingService.cs

using System;
using System.IO;

namespace ImageProcessing.Services
{
    public class SettingService
    {
        private readonly string _settingsFilePath;

        public SettingService()
        {
            // 설정 파일을 사용자 AppData 폴더에 저장합니다.
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolderPath = Path.Combine(appDataPath, "ImageProcessingApp");
            Directory.CreateDirectory(appFolderPath); // 폴더가 없으면 생성합니다.
            _settingsFilePath = Path.Combine(appFolderPath, "settings.txt");
        }

        public void SaveLastImagePath(string path)
        {
            File.WriteAllText(_settingsFilePath, path ?? string.Empty);
        }

        public string GetLastImagePath()
        {
            if (File.Exists(_settingsFilePath))
            {
                return File.ReadAllText(_settingsFilePath);
            }
            return null;
        }
    }
}