using System;

namespace ImageProcessingApp.Models
{
    public class AppSettings
    {
        public double DefaultGaussianSigma { get; set; } = 1.0;
        public int DefaultKernelSize { get; set; } = 3;
        public double DefaultThreshold { get; set; } = 0.5;
        public string DefaultImagePath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public bool AutoCommitEnabled { get; set; } = true;
        public string GitRepositoryPath { get; set; } = ".";
    }
}
