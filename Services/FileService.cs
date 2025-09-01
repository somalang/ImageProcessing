using ImageProcessing.Models;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace ImageProcessing.Services
{
    public class FileService
    {
        public ImageModel OpenImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.png;*.bmp;*.jpeg"
            };

            if (dialog.ShowDialog() == true)
            {
                var image = new BitmapImage(new Uri(dialog.FileName));
                return new ImageModel
                {
                    OriginalImage = image,
                    FilePath = dialog.FileName,
                    FileName = Path.GetFileName(dialog.FileName),
                    Width = image.PixelWidth,
                    Height = image.PixelHeight,
                    Format = image.Format.ToString(),
                    LoadedTime = DateTime.Now
                };
            }
            return null;
        }

        public void SaveImage(BitmapImage image)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg"
            };

            if (dialog.ShowDialog() == true)
            {
                using var fileStream = new FileStream(dialog.FileName, FileMode.Create);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
        }
    }
}
