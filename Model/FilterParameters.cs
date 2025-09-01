namespace ImageProcessingApp.Models
{
    public class FilterParameters
    {
        public double GaussianSigma { get; set; } = 1.0;
        public int KernelSize { get; set; } = 3;
        public double Threshold { get; set; } = 0.5;
        public bool IsEnabled { get; set; } = true;
    }
}
